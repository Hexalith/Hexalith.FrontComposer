using System.Text.Json;

namespace Hexalith.FrontComposer.AppHost;

/// <summary>Reads the private, runner-owned configuration for disposable readiness verification.</summary>
internal sealed class LocalReadinessConfiguration
{
    /// <summary>Gets the configuration prefix; ordinary development never enables this mode.</summary>
    internal const string Prefix = "FrontComposerVerification";
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

    /// <summary>Gets a runner-allocated loopback port.</summary>
    internal int Port(string name) => _settings.GetProperty("ports").GetProperty(name).GetInt32();

    /// <summary>Gets the installed Dapr runtime version used by the owned infrastructure.</summary>
    internal string DaprVersion => _settings.GetProperty("daprVersion").GetString()!;

    /// <summary>Resolves one fixed file inside the private verification directory.</summary>
    internal string FilePath(string name) => Path.Combine(DirectoryPath, name);

    /// <summary>Loads only a private real directory and strict generated configuration.</summary>
    internal static LocalReadinessConfiguration Load(string? directory)
    {
        if (string.IsNullOrWhiteSpace(directory) || !Path.IsPathFullyQualified(directory))
        {
            throw new InvalidOperationException("Disposable verification requires its runner-owned private directory.");
        }
        string path = Path.GetFullPath(directory);
        string configurationPath = Path.Combine(path, "configuration.json");
        foreach (string entry in new[] { path, configurationPath })
        {
            if ((File.GetAttributes(entry) & FileAttributes.ReparsePoint) != 0
                || (!OperatingSystem.IsWindows() && (File.GetUnixFileMode(entry)
                    & (UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute
                       | UnixFileMode.OtherRead | UnixFileMode.OtherWrite | UnixFileMode.OtherExecute)) != 0))
            {
                throw new InvalidOperationException("Disposable verification refuses linked or non-private configuration.");
            }
        }
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(configurationPath));
        JsonElement settings = document.RootElement;
        string owner = settings.GetProperty("owner").GetString() ?? string.Empty;
        string version = settings.GetProperty("daprVersion").GetString() ?? string.Empty;
        int[] ports = settings.GetProperty("ports").EnumerateObject().Select(item => item.Value.GetInt32()).ToArray();
        if (owner.Length != 32 || owner.Any(character => !char.IsAsciiHexDigit(character))
            || !System.Text.RegularExpressions.Regex.IsMatch(version, @"^1\.18\.[0-9]+$", System.Text.RegularExpressions.RegexOptions.CultureInvariant)
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
}
