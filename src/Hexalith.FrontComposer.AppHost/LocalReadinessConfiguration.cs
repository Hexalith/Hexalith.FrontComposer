using System.Text.Json;

namespace Hexalith.FrontComposer.AppHost;

/// <summary>Reads the private, runner-owned configuration for disposable readiness verification.</summary>
internal sealed class LocalReadinessConfiguration
{
    /// <summary>Gets the configuration prefix; ordinary development never enables this mode.</summary>
    internal const string Prefix = "FrontComposerVerification";
    private static readonly string[] _componentFiles = ["configstore.yaml", "pubsub.yaml", "statestore.yaml"];
    private readonly JsonElement _settings;

    private LocalReadinessConfiguration(string directory, JsonElement settings)
    {
        DirectoryPath = directory;
        _settings = settings;
    }

    /// <summary>Gets the private verification directory.</summary>
    internal string DirectoryPath { get; }

    /// <summary>Gets the unique ownership identifier.</summary>
    internal string Owner => _settings.GetProperty("owner").GetString()!;

    /// <summary>Gets the fresh launch identity; full verification never reuses a private realm.</summary>
    internal string Instance => _settings.GetProperty("instance").GetString()!;

    /// <summary>Gets whether the runner explicitly selected the ordinary full application graph.</summary>
    internal bool FullTopology => _settings.TryGetProperty("topology", out JsonElement topology) && topology.GetString() == "full";

    /// <summary>Gets a runner-allocated loopback port.</summary>
    internal int Port(string name) => _settings.GetProperty("ports").GetProperty(name).GetInt32();

    /// <summary>Gets the installed Dapr runtime version used by the owned infrastructure.</summary>
    internal string DaprVersion => _settings.GetProperty("daprVersion").GetString()!;

    /// <summary>Resolves one fixed file inside the private verification directory.</summary>
    internal string FilePath(string name) => Path.Combine(DirectoryPath, name);

    /// <summary>Requires explicit matching selection; maintenance always remains gateway-only.</summary>
    internal bool UseFullTopology(string? selection, bool maintenance)
    {
        string selected = selection ?? "gateway";
        if (selected is not "gateway" and not "full" || (selected == "full") != FullTopology)
        {
            throw new InvalidOperationException("Disposable verification topology selection does not match its private configuration.");
        }
        return FullTopology && !maintenance;
    }

    /// <summary>Refuses full writers without the runner's successful persisted-activation readback.</summary>
    internal void RequireActivation()
    {
        string path = FilePath("activation.json");
        RequirePrivate(path);
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
        JsonElement activation = document.RootElement;
        if (activation.GetProperty("owner").GetString() != Owner
            || !activation.GetProperty("freshStoreVerified").GetBoolean()
            || activation.GetProperty("schemaVersion").GetInt32() != 1
            || activation.GetProperty("writerProtocolVersion").GetInt32() != 2
            || !System.Text.RegularExpressions.Regex.IsMatch(activation.GetProperty("cutoverCommit").GetString() ?? string.Empty,
                "^[0-9a-f]{40}$", System.Text.RegularExpressions.RegexOptions.CultureInvariant)
            || string.IsNullOrWhiteSpace(activation.GetProperty("activatedAt").GetString()))
        {
            throw new InvalidOperationException("Full disposable topology requires verified writer activation.");
        }
    }

    /// <summary>Refuses an enabled source integration before clearing its dormant disposable route.</summary>
    internal static void RequireDisabledMemories(string partiesProjectDirectory)
    {
        foreach (string name in new[] { "appsettings.json", "appsettings.Development.json" })
        {
            using JsonDocument settings = JsonDocument.Parse(File.ReadAllText(Path.Combine(partiesProjectDirectory, name)));
            if (settings.RootElement.TryGetProperty("Parties", out JsonElement parties)
                && parties.TryGetProperty("MemoriesSearch", out JsonElement search)
                && search.TryGetProperty("Enabled", out JsonElement enabled) && enabled.GetBoolean())
            {
                throw new InvalidOperationException("Disposable full topology refuses enabled Memories indexing.");
            }
        }
    }

    /// <summary>Validates all generated component and discovery files before infrastructure is composed.</summary>
    internal void ValidateIsolation(string appHostDirectory)
    {
        string password = _settings.GetProperty("redisPassword").GetString() ?? string.Empty;
        if (!System.Text.RegularExpressions.Regex.IsMatch(password, "^[0-9a-f]{64}$", System.Text.RegularExpressions.RegexOptions.CultureInvariant))
        {
            throw new InvalidOperationException("Disposable verification requires its private Redis credential.");
        }
        RequirePrivate(FilePath("redis.conf"));
        string redisConfiguration = $"bind 0.0.0.0\nprotected-mode yes\nrequirepass {password}\nsave \"\"\ndir /data\ndbfilename dump.rdb\n";
        if (File.ReadAllText(FilePath("redis.conf")).Replace("\r\n", "\n", StringComparison.Ordinal) != redisConfiguration)
            throw new InvalidOperationException("Disposable verification refuses altered Redis configuration.");
        foreach (string name in new[] { "components", "runtime", "empty" }) RequirePrivate(FilePath(name));
        if (FullTopology) RequirePrivate(FilePath("mtls"));
        if (Directory.EnumerateFileSystemEntries(FilePath("empty")).Any()
            || !Directory.EnumerateFileSystemEntries(FilePath("components")).Select(Path.GetFileName).Order(StringComparer.Ordinal)
                .SequenceEqual(_componentFiles, StringComparer.Ordinal))
        {
            throw new InvalidOperationException("Disposable verification refuses unexpected resource files.");
        }
        foreach ((string name, string type, string[] scopes) in new (string, string, string[])[]
        {
            ("statestore", "state.redis", ["eventstore", "eventstore-admin", "tenants", "parties"]),
            ("pubsub", "pubsub.redis", ["eventstore", "tenants", "parties"]),
            ("configstore", "configuration.redis", ["eventstore"]),
        })
        {
            List<object> metadata = [new { name = "redisHost", value = "127.0.0.1:" + Port("redis") }, new { name = "redisPassword", value = password }];
            if (name == "statestore")
            {
                metadata.Add(new { name = "actorStateStore", value = "true" });
                metadata.Add(new { name = "keyPrefix", value = "none" });
            }
            JsonElement expected = JsonSerializer.SerializeToElement(new
            {
                apiVersion = "dapr.io/v1alpha1", kind = "Component", metadata = new { name },
                spec = new { type, version = "v1", metadata }, scopes = FullTopology ? scopes : ["eventstore"],
            });
            string path = FilePath("components/" + name + ".yaml");
            RequirePrivate(path);
            using JsonDocument component = JsonDocument.Parse(File.ReadAllText(path));
            if (!JsonElement.DeepEquals(component.RootElement, expected))
            {
                throw new InvalidOperationException("Disposable verification refuses a shared or altered component.");
            }
        }
        foreach (string name in new[] { "accesscontrol.yaml", "accesscontrol.eventstore-admin.yaml", "accesscontrol.sample.yaml",
            "accesscontrol.tenants-api.yaml", "accesscontrol.tenants-ui.yaml", "accesscontrol.frontcomposer-ui.yaml" })
        {
            string path = FilePath(name);
            RequirePrivate(path);
            string template = File.ReadAllText(Path.Combine(appHostDirectory, "DaprComponents", name));
            string expected = template.Replace("connectionString: \"nr.db\"", "connectionString: " + JsonSerializer.Serialize(FilePath("runtime/nr.db")), StringComparison.Ordinal);
            if (expected == template || File.ReadAllText(path).Replace("\r\n", "\n", StringComparison.Ordinal) != expected.Replace("\r\n", "\n", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Disposable verification refuses shared or altered discovery configuration.");
            }
        }
        foreach (string path in Directory.EnumerateFileSystemEntries(FilePath("runtime"))) RequirePrivate(path);
    }

    /// <summary>Validates canonical issuer/run paths without reading or deleting credentials.</summary>
    /// <param name="runDirectory">The canonical helper's fresh run directory.</param>
    /// <param name="certificateDirectory">The issuer directory under this run's private parent.</param>
    internal void ValidateMtlsDirectories(string runDirectory, string certificateDirectory)
    {
        string run = Path.GetFullPath(runDirectory);
        string issuer = Path.GetFullPath(certificateDirectory);
        string expectedParent = Path.Combine(Path.GetTempPath(), "hexalith-dapr");
        string runName = Path.GetFileName(run);
        string digest = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(run)))[..32];
        if (Path.GetDirectoryName(run) != expectedParent || runName.Length != 32 || runName.Any(character => !char.IsAsciiHexDigit(character))
            || issuer != FilePath("mtls/dapr-credentials-" + digest))
            throw new InvalidOperationException("Canonical Dapr directories do not belong to the private verification run.");
        RequirePrivate(run);
        RequirePrivate(FilePath("mtls"));
        RequirePrivate(issuer);
    }

    /// <summary>Loads only a private real directory and strict generated configuration.</summary>
    internal static LocalReadinessConfiguration Load(string? directory)
    {
        if (string.IsNullOrWhiteSpace(directory) || !Path.IsPathFullyQualified(directory))
        {
            throw new InvalidOperationException("Disposable verification requires its runner-owned private directory.");
        }
        string path = Path.GetFullPath(directory);
        string configurationPath = Path.Combine(path, "configuration.json");
        RequirePrivate(path);
        RequirePrivate(configurationPath);
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(configurationPath));
        JsonElement settings = document.RootElement;
        string owner = settings.GetProperty("owner").GetString() ?? string.Empty;
        string version = settings.GetProperty("daprVersion").GetString() ?? string.Empty;
        int[] ports = settings.GetProperty("ports").EnumerateObject().Select(item => item.Value.GetInt32()).ToArray();
        string topology = settings.TryGetProperty("topology", out JsonElement selected) ? selected.GetString() ?? string.Empty : "gateway";
        if (owner.Length != 32 || owner.Any(character => !char.IsAsciiHexDigit(character))
            || !System.Text.RegularExpressions.Regex.IsMatch(version, @"^1\.18\.[0-9]+$", System.Text.RegularExpressions.RegexOptions.CultureInvariant)
            || topology is not "gateway" and not "full"
            || (topology == "full" && (!settings.TryGetProperty("instance", out JsonElement instance)
                || !System.Text.RegularExpressions.Regex.IsMatch(instance.GetString() ?? string.Empty, "^[0-9a-f]{32}$", System.Text.RegularExpressions.RegexOptions.CultureInvariant)))
            || ports.Length != 4 || ports.Distinct().Count() != 4 || ports.Any(port => port < 10000 || port > 65535))
        {
            throw new InvalidOperationException("Disposable verification configuration is invalid.");
        }
        foreach (string name in new[] { "redis", "placement", "scheduler", "daprHttp" })
        {
            _ = settings.GetProperty("ports").GetProperty(name).GetInt32();
        }
        return new LocalReadinessConfiguration(path, settings.Clone());
    }

    private static void RequirePrivate(string path)
    {
        for (string? current = Path.GetFullPath(path); current is not null; current = Path.GetDirectoryName(current))
        {
            if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidOperationException("Disposable verification refuses linked configuration.");
        }
        if (!OperatingSystem.IsWindows() && (File.GetUnixFileMode(path)
            & (UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute
               | UnixFileMode.OtherRead | UnixFileMode.OtherWrite | UnixFileMode.OtherExecute)) != 0)
        {
            throw new InvalidOperationException("Disposable verification refuses non-private configuration.");
        }
    }
}
