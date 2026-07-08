using System.Security.Cryptography;
using System.Text;

using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Mcp;

/// <summary>
/// Matches configured MCP API keys by hashing candidates and configured keys before comparison.
/// </summary>
internal sealed class FrontComposerMcpApiKeyCredentialStore(IOptions<FrontComposerMcpOptions> options) {
    /// <summary>
    /// Attempts to resolve an API-key candidate to its configured MCP identity.
    /// </summary>
    /// <param name="candidate">API key supplied by the current request.</param>
    /// <returns>The configured identity when the candidate matches; otherwise <see langword="null"/>.</returns>
    public FrontComposerMcpApiKeyIdentity? Match(string candidate) {
        if (string.IsNullOrWhiteSpace(candidate)) {
            return null;
        }

        byte[] candidateBytes = Encoding.UTF8.GetBytes(candidate);
        byte[] candidateHash;
        try {
            candidateHash = SHA256.HashData(candidateBytes);
        }
        finally {
            CryptographicOperations.ZeroMemory(candidateBytes);
        }

        try {
            FrontComposerMcpApiKeyIdentity? matched = null;
            foreach ((byte[] hash, FrontComposerMcpApiKeyIdentity identity) in CreateCredentials(options.Value.ApiKeys)) {
                if (CryptographicOperations.FixedTimeEquals(candidateHash, hash)) {
                    matched = identity;
                }

                CryptographicOperations.ZeroMemory(hash);
            }

            return matched;
        }
        finally {
            CryptographicOperations.ZeroMemory(candidateHash);
        }
    }

    private static (byte[] Hash, FrontComposerMcpApiKeyIdentity Identity)[] CreateCredentials(
        IDictionary<string, FrontComposerMcpApiKeyIdentity> apiKeys) {
        List<(byte[] Hash, FrontComposerMcpApiKeyIdentity Identity)> result = [];
        foreach (KeyValuePair<string, FrontComposerMcpApiKeyIdentity> entry in apiKeys.ToArray()) {
            if (string.IsNullOrWhiteSpace(entry.Key) || entry.Value is null) {
                continue;
            }

            byte[] keyBytes = Encoding.UTF8.GetBytes(entry.Key);
            try {
                result.Add((SHA256.HashData(keyBytes), entry.Value));
            }
            finally {
                CryptographicOperations.ZeroMemory(keyBytes);
            }
        }

        return [.. result];
    }
}
