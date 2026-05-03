using System.Buffers;
using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;

using Hexalith.FrontComposer.Contracts.Mcp;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Mcp.Invocation;

public sealed class FrontComposerMcpToolAdmissionService(
    FrontComposerMcpDescriptorRegistry registry,
    IFrontComposerMcpAgentContextAccessor agentContextAccessor,
    IFrontComposerMcpTenantToolGate tenantGate,
    IServiceProvider services,
    IOptions<FrontComposerMcpOptions> options,
    ILogger<FrontComposerMcpToolAdmissionService> logger) {
    private const int SuggestionThreshold = 75;
    private const int ContextMarkerMinLength = 4;
    private const int PrefixBonusFloor = 90;

    public async ValueTask<McpVisibleToolCatalog> BuildVisibleCatalogAsync(CancellationToken cancellationToken = default) {
        FrontComposerMcpAgentContext context = agentContextAccessor.GetContext();
        ValidateContext(context);

        // Policy gate is optional: hosts that do not declare any AuthorizationPolicyName-bearing
        // commands need not register a gate. Resolved lazily so missing-registration ≠ DI failure.
        IFrontComposerMcpCommandPolicyGate? policyGate = (IFrontComposerMcpCommandPolicyGate?)services.GetService(typeof(IFrontComposerMcpCommandPolicyGate));

        List<McpVisibleToolCatalogEntry> visible = [];
        int maxItems = Math.Max(0, options.Value.MaxVisibleToolListItems);
        bool truncated = false;
        foreach (McpCommandDescriptor descriptor in registry.Commands) {
            cancellationToken.ThrowIfCancellationRequested();

            if (!await IsTenantVisibleAsync(tenantGate, descriptor, context, cancellationToken).ConfigureAwait(false)
                || !await IsPolicyVisibleAsync(policyGate, descriptor, context, cancellationToken).ConfigureAwait(false)
                || ContainsContextSensitiveText(descriptor, context)) {
                continue;
            }

            string normalized = registry.GetNormalizedName(descriptor);
            if (normalized.Length == 0) {
                continue;
            }

            if (visible.Count >= maxItems) {
                truncated = true;
                break;
            }

            visible.Add(new McpVisibleToolCatalogEntry(
                descriptor.ProtocolName,
                SanitizeDisplayText(descriptor.Title, options.Value.MaxToolDisplayTextLength),
                string.IsNullOrWhiteSpace(descriptor.Description)
                    ? null
                    : SanitizeDisplayText(descriptor.Description!, options.Value.MaxToolDisplayTextLength),
                descriptor.BoundedContext,
                BuildInputSummary(descriptor),
                descriptor,
                normalized));
        }

        return new McpVisibleToolCatalog(
            McpToolVisibilityContext.FromAgentContext(context),
            visible
                .OrderBy(t => t.Name, StringComparer.Ordinal)
                .ToArray(),
            truncated);
    }

    public async ValueTask<McpToolResolutionResult> ResolveAsync(string? requestedName, CancellationToken cancellationToken = default) {
        McpVisibleToolCatalog catalog = await BuildVisibleCatalogAsync(cancellationToken).ConfigureAwait(false);
        string sanitizedRequested = SanitizeToolName(requestedName, options.Value.MaxToolNameLength);
        if (string.IsNullOrWhiteSpace(sanitizedRequested)) {
            return McpToolResolutionResult.Reject(sanitizedRequested, null, catalog);
        }

        McpVisibleToolCatalogEntry? exact = catalog.Tools
            .FirstOrDefault(t => string.Equals(t.Name, requestedName, StringComparison.Ordinal));
        if (exact is not null) {
            return McpToolResolutionResult.Accept(exact, catalog);
        }

        McpToolSuggestion? suggestion = FindSuggestion(requestedName!, catalog.Tools);
        return McpToolResolutionResult.Reject(sanitizedRequested, suggestion, catalog);
    }

    public static JsonObject BuildUnknownToolStructuredContent(McpToolResolutionResult resolution) {
        ArgumentNullException.ThrowIfNull(resolution);

        JsonArray visibleTools = [];
        foreach (McpVisibleToolCatalogEntry tool in resolution.VisibleTools) {
            visibleTools.Add(new JsonObject {
                ["name"] = tool.Name,
                ["title"] = tool.Title,
                ["description"] = tool.Description,
                ["inputSummary"] = tool.InputSummary,
            });
        }

        JsonObject result = BuildHiddenUnknownStructuredContent();
        result["requestedToolName"] = resolution.RequestedName;
        result["suggestion"] = resolution.Suggestion?.Name;
        result["visibleTools"] = visibleTools;

        if (resolution.IsVisibleListTruncated) {
            result["continuation"] = "visible-list-truncated";
        }

        return result;
    }

    /// <summary>
    /// Shared hidden-unknown shape used by the unknown-tool admission path and the lifecycle
    /// hidden-unknown path. Centralises the AC9 response contract so both surfaces stay aligned
    /// when the shape evolves. Lifecycle callers must NOT add `requestedToolName` (P20: avoids
    /// fingerprinting the lifecycle handle in masked failures).
    /// </summary>
    public static JsonObject BuildHiddenUnknownStructuredContent() => new() {
        ["category"] = "unknown_tool",
        ["suggestion"] = null,
        ["visibleTools"] = new JsonArray(),
        ["docsCode"] = "HFC-MCP-UNKNOWN-TOOL",
    };

    /// <summary>
    /// Normalizes a tool name for matching. Public so the registry can precompute normalized
    /// keys at construction time per spec T7.
    /// </summary>
    public static string NormalizeForMatching(string value, out bool unsupported) {
        unsupported = false;
        if (string.IsNullOrWhiteSpace(value)) {
            return string.Empty;
        }

        string normalized = value.Trim().Normalize(NormalizationForm.FormKC);
        StringBuilder sb = new(normalized.Length);
        foreach (char ch in normalized) {
            if (char.IsControl(ch) || ch > 127) {
                unsupported = true;
                continue;
            }

            if (char.IsLetterOrDigit(ch)) {
                _ = sb.Append(char.ToLower(ch, CultureInfo.InvariantCulture));
            }
        }

        return sb.ToString();
    }

    private static void ValidateContext(FrontComposerMcpAgentContext context) {
        if (string.IsNullOrWhiteSpace(context.TenantId)
            || string.IsNullOrWhiteSpace(context.UserId)
            || context.Principal.Identity is null
            || !context.Principal.Identity.IsAuthenticated) {
            throw new FrontComposerMcpException(FrontComposerMcpFailureCategory.AuthFailed);
        }
    }

    private async ValueTask<bool> IsTenantVisibleAsync(
        IFrontComposerMcpTenantToolGate gate,
        McpCommandDescriptor descriptor,
        FrontComposerMcpAgentContext context,
        CancellationToken cancellationToken) {
        try {
            return await gate.IsVisibleAsync(descriptor, context, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            throw;
        }
        catch (Exception ex) {
            // Sanitized log: descriptor kind + bounded context only; no exception text, no tenant/user IDs,
            // no descriptor name (which could carry generated metadata).
            logger.LogWarning(
                ex,
                "MCP tenant gate threw while evaluating descriptor in bounded context {BoundedContext}; treating as not visible.",
                descriptor.BoundedContext);
            return false;
        }
    }

    private async ValueTask<bool> IsPolicyVisibleAsync(
        IFrontComposerMcpCommandPolicyGate? gate,
        McpCommandDescriptor descriptor,
        FrontComposerMcpAgentContext context,
        CancellationToken cancellationToken) {
        if (string.IsNullOrWhiteSpace(descriptor.AuthorizationPolicyName)) {
            return true;
        }

        if (gate is null) {
            return false;
        }

        try {
            return await gate.EvaluateAsync(descriptor.AuthorizationPolicyName!, context, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            throw;
        }
        catch (Exception ex) {
            logger.LogWarning(
                ex,
                "MCP policy gate threw while evaluating descriptor in bounded context {BoundedContext}; treating as not visible.",
                descriptor.BoundedContext);
            return false;
        }
    }

    private McpToolSuggestion? FindSuggestion(string requestedName, IReadOnlyList<McpVisibleToolCatalogEntry> visibleTools) {
        string normalizedRequested = NormalizeForMatching(requestedName, out bool unsupported);
        if (unsupported || normalizedRequested.Length == 0) {
            return null;
        }

        // Score the full visible list, then take the top N — taking before scoring would let
        // alphabetical truncation hide the actual best match (P12).
        return visibleTools
            .Select(t => new McpToolSuggestion(t.Name, Score(normalizedRequested, t.NormalizedName)))
            .Where(s => s.Score >= SuggestionThreshold)
            .OrderByDescending(s => s.Score)
            .ThenBy(s => s.Name, StringComparer.Ordinal)
            .Take(Math.Max(1, options.Value.MaxSuggestionCandidates))
            .FirstOrDefault();
    }

    private static int Score(string requested, string candidate) {
        if (requested.Length == 0 || candidate.Length == 0) {
            return 0;
        }

        if (string.Equals(requested, candidate, StringComparison.Ordinal)) {
            return 100;
        }

        if (candidate.StartsWith(requested, StringComparison.Ordinal)
            || requested.StartsWith(candidate, StringComparison.Ordinal)) {
            int min = Math.Min(requested.Length, candidate.Length);
            int max = Math.Max(requested.Length, candidate.Length);
            // Prefix bonus floors at 90 so even tiny prefixes outrank Levenshtein-only matches (max 99).
            return PrefixBonusFloor + (10 * min / max);
        }

        int distance = BoundedLevenshteinDistance(requested, candidate, Math.Max(4, Math.Max(requested.Length, candidate.Length) / 4));
        if (distance < 0) {
            return 0;
        }

        int longest = Math.Max(requested.Length, candidate.Length);
        return Math.Max(0, 100 - (distance * 100 / longest));
    }

    private static int BoundedLevenshteinDistance(string left, string right, int maxDistance) {
        if (Math.Abs(left.Length - right.Length) > maxDistance) {
            return -1;
        }

        ArrayPool<int> pool = ArrayPool<int>.Shared;
        int[] previous = pool.Rent(right.Length + 1);
        int[] current = pool.Rent(right.Length + 1);
        try {
            for (int j = 0; j <= right.Length; j++) {
                previous[j] = j;
            }

            for (int i = 1; i <= left.Length; i++) {
                current[0] = i;
                int rowMin = current[0];
                for (int j = 1; j <= right.Length; j++) {
                    int cost = left[i - 1] == right[j - 1] ? 0 : 1;
                    current[j] = Math.Min(
                        Math.Min(current[j - 1] + 1, previous[j] + 1),
                        previous[j - 1] + cost);
                    rowMin = Math.Min(rowMin, current[j]);
                }

                if (rowMin > maxDistance) {
                    return -1;
                }

                (previous, current) = (current, previous);
            }

            return previous[right.Length] <= maxDistance ? previous[right.Length] : -1;
        }
        finally {
            pool.Return(previous);
            pool.Return(current);
        }
    }

    private static string SanitizeToolName(string? value, int maxLength) {
        if (string.IsNullOrEmpty(value)) {
            return string.Empty;
        }

        return SanitizeDisplayText(value, maxLength);
    }

    private static string SanitizeDisplayText(string value, int maxLength) {
        if (string.IsNullOrEmpty(value)) {
            return string.Empty;
        }

        int cap = Math.Max(0, maxLength);
        // NFC normalization preserves precomposed characters so international names ("Étiquette",
        // "支払") survive while combining-mark trickery is collapsed.
        string normalized = value.Normalize(NormalizationForm.FormC);
        StringBuilder sb = new(Math.Min(normalized.Length, cap));
        foreach (char ch in normalized) {
            if (sb.Length >= cap) {
                break;
            }

            // Drop control characters entirely — emitting `\uNNNN` as literal text confused agents
            // and gave the protocol no win over simply removing the bytes.
            if (char.IsControl(ch)) {
                continue;
            }

            _ = sb.Append(ch);
        }

        return sb.ToString();
    }

    private static bool ContainsContextSensitiveText(McpCommandDescriptor descriptor, FrontComposerMcpAgentContext context) {
        // Only treat tenant/user markers as sensitive when they are long enough to be unlikely
        // collisions with English tool prose. Short IDs ("a", "1") are dominated by false positives.
        string[] sensitive = [context.TenantId, context.UserId];
        foreach (string marker in sensitive) {
            if (string.IsNullOrWhiteSpace(marker) || marker.Length < ContextMarkerMinLength) {
                continue;
            }

            if (Contains(descriptor.ProtocolName, marker)
                || Contains(descriptor.Title, marker)
                || Contains(descriptor.Description, marker)) {
                return true;
            }
        }

        return false;

        static bool Contains(string? text, string marker)
            => text?.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static string BuildInputSummary(McpCommandDescriptor descriptor) {
        string[] parts = [.. descriptor.Parameters
            .Where(p => !p.IsUnsupported)
            .OrderBy(p => p.Name, StringComparer.Ordinal)
            .Select(p => $"{p.Name}{(p.IsRequired ? string.Empty : "?")}: {p.JsonType}")];
        return string.Join("; ", parts);
    }
}
