using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Hexalith.FrontComposer.AppHost;

/// <summary>
/// Owns a private local realm import with one generated EventStore workload identity.
/// The checked-in user realm is read-only and never replaced or reset.
/// </summary>
internal sealed class LocalWorkloadRealm : IDisposable
{
    /// <summary>Gets the workload identity expected by EventStore's internal assertion contract.</summary>
    internal const string ClientId = "eventstore";

    /// <summary>Gets the secret parameter name shared with the AppHost's workload wiring.</summary>
    internal const string SecretParameterName = "eventstore-local-workload-secret";

    private const string RealmFileName = "hexalith-realm.json";
    private readonly string _realmPath;
    private bool _disposed;

    private LocalWorkloadRealm(string importDirectory, string clientSecret)
    {
        ImportDirectory = importDirectory;
        ClientSecret = clientSecret;
        _realmPath = Path.Combine(importDirectory, RealmFileName);
    }

    /// <summary>Gets the temporary directory containing only this run's rendered realm.</summary>
    internal string ImportDirectory { get; }

    /// <summary>Gets the generated secret to bind through a secret Aspire parameter.</summary>
    internal string ClientSecret { get; }

    /// <summary>
    /// Adds a workload service account to a private copy of the existing local realm.
    /// Persistent realms and caller-supplied secret overrides cannot share per-run credentials.
    /// </summary>
    /// <param name="sourceDirectory">The checked-in local user-realm directory.</param>
    /// <param name="persistent">Whether persistent Keycloak reuse was requested.</param>
    /// <param name="configuredSecret">Any configured override of the generated secret parameter.</param>
    /// <returns>The owned realm import and its matching generated workload secret.</returns>
    internal static LocalWorkloadRealm Create(string sourceDirectory, bool persistent, string? configuredSecret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceDirectory);
        if (persistent)
        {
            throw new InvalidOperationException("Local workload credentials require a fresh non-persistent Keycloak realm; an existing user realm will not be reset.");
        }
        if (configuredSecret is not null)
        {
            throw new InvalidOperationException("The local workload secret parameter is generated per run and cannot be overridden.");
        }

        JsonObject realm = JsonNode.Parse(File.ReadAllText(Path.Combine(sourceDirectory, RealmFileName)))?.AsObject()
            ?? throw new InvalidOperationException("The local Keycloak realm must be a JSON object.");
        JsonArray clients = realm["clients"]?.AsArray()
            ?? throw new InvalidOperationException("The local Keycloak realm must declare its existing clients.");
        if (clients.Any(client => client?["clientId"]?.GetValue<string>() == ClientId))
        {
            throw new InvalidOperationException("The user realm already declares the workload client; refusing to replace it.");
        }
        JsonArray scopes = realm["clientScopes"]?.AsArray() ?? new JsonArray();
        realm["clientScopes"] = scopes;
        JsonArray optionalScopes = [];
        foreach (string audience in new[] { "sample", "tenants", "parties", "counter-scope-fixture" })
        {
            string name = "eventstore-audience." + audience;
            AddScope(scopes, optionalScopes, name, "oidc-audience-mapper", new JsonObject
            {
                ["included.custom.audience"] = audience,
                ["id.token.claim"] = "false",
                ["access.token.claim"] = "true",
                ["introspection.token.claim"] = "true",
            });
        }
        foreach (string operation in new[] { "process", "replay-state", "query", "project", "metadata" })
        {
            string name = "eventstore-operation.domain-service." + operation;
            AddScope(scopes, optionalScopes, name, "oidc-hardcoded-claim-mapper", new JsonObject
            {
                ["claim.name"] = "eventstore:operation",
                ["claim.value"] = "domain-service:" + operation,
                ["jsonType.label"] = "String",
                ["id.token.claim"] = "false",
                ["access.token.claim"] = "true",
                ["userinfo.token.claim"] = "false",
                ["introspection.token.claim"] = "true",
            });
        }
        string secret = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        clients.Add(new JsonObject
        {
            ["clientId"] = ClientId,
            ["name"] = "FrontComposer local EventStore workload",
            ["enabled"] = true,
            ["publicClient"] = false,
            ["clientAuthenticatorType"] = "client-secret",
            ["secret"] = secret,
            ["directAccessGrantsEnabled"] = false,
            ["standardFlowEnabled"] = false,
            ["implicitFlowEnabled"] = false,
            ["serviceAccountsEnabled"] = true,
            ["fullScopeAllowed"] = false,
            ["attributes"] = new JsonObject { ["access.token.lifespan"] = "300" },
            ["defaultClientScopes"] = new JsonArray(),
            ["optionalClientScopes"] = optionalScopes,
        });

        // CreateTempSubdirectory creates a new unpredictable directory with owner-only Unix access.
        // Windows uses the current user's temporary-directory ACL. No shared directory is chmodded.
        string directory = Directory.CreateTempSubdirectory("hexalith-frontcomposer-keycloak-").FullName;
        LocalWorkloadRealm rendered = new(directory, secret);
        try
        {
            FileStreamOptions options = new()
            {
                Mode = FileMode.CreateNew,
                Access = FileAccess.Write,
                Share = FileShare.None,
            };
            if (!OperatingSystem.IsWindows())
            {
                options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
            }
            using FileStream stream = new(rendered._realmPath, options);
            JsonSerializer.Serialize(stream, realm);
            return rendered;
        }
        catch
        {
            rendered.Dispose();
            throw;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        if (Directory.Exists(ImportDirectory))
        {
            if ((File.GetAttributes(ImportDirectory) & FileAttributes.ReparsePoint) != 0
                || Directory.EnumerateFileSystemEntries(ImportDirectory).Any(path => path != _realmPath)
                || (File.Exists(_realmPath) && (File.GetAttributes(_realmPath) & FileAttributes.ReparsePoint) != 0))
            {
                throw new IOException("Generated realm cleanup refused an unowned path or linked file.");
            }
            File.Delete(_realmPath);
            Directory.Delete(ImportDirectory, recursive: false);
        }
        _disposed = true;
    }

    private static void AddScope(JsonArray scopes, JsonArray optionalScopes, string name, string mapper, JsonObject configuration)
    {
        if (scopes.Any(scope => scope?["name"]?.GetValue<string>() == name))
        {
            throw new InvalidOperationException("The user realm already declares a workload scope; refusing to replace it.");
        }
        scopes.Add(new JsonObject
        {
            ["name"] = name,
            ["protocol"] = "openid-connect",
            ["attributes"] = new JsonObject
            {
                ["include.in.token.scope"] = "true",
                ["display.on.consent.screen"] = "false",
            },
            ["protocolMappers"] = new JsonArray(new JsonObject
            {
                ["name"] = "workload-grant",
                ["protocol"] = "openid-connect",
                ["protocolMapper"] = mapper,
                ["consentRequired"] = false,
                ["config"] = configuration,
            }),
        });
        optionalScopes.Add(name);
    }
}
