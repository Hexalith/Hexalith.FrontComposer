using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.Contracts.Registration;

using Microsoft.Extensions.Logging;

namespace Hexalith.FrontComposer.Shell.Registration;
/// <summary>
/// Default implementation of <see cref="IFrontComposerRegistry"/>.
/// Stores registered domain manifests and navigation groups for runtime composition.
/// Domain registrations from <see cref="DomainRegistrationAction"/> are applied on construction.
/// </summary>
internal sealed class FrontComposerRegistry : IFrontComposerRegistry, IFrontComposerFullPageRouteRegistry, IFrontComposerCommandWriteAccessRegistry, IFrontComposerCommandPolicyRegistry {
    private readonly List<DomainManifest> _manifests = [];
    private readonly List<(string Name, string BoundedContext)> _navGroups = [];
    private readonly ILogger<FrontComposerRegistry> _logger;

    public FrontComposerRegistry(
        IEnumerable<DomainRegistrationAction> registrationActions,
        IEnumerable<DomainRegistrationWarning> warnings,
        ILogger<FrontComposerRegistry> logger) {
        _logger = logger;
        foreach (DomainRegistrationWarning warning in warnings) {
            logger.LogWarning(
                "Skipping registration type {RegistrationType}: expected a static Manifest member and RegisterDomain(IFrontComposerRegistry) method. Found Manifest={HasManifest}, RegisterDomain={HasRegisterMethod}.",
                warning.RegistrationType,
                warning.HasManifest,
                warning.HasRegisterMethod);
        }

        foreach (DomainRegistrationAction action in registrationActions) {
            action.Apply(this);
        }

        ValidateManifests();
    }

    /// <summary>
    /// Walks every registered manifest and asserts each <see cref="DomainManifest.Commands"/> entry
    /// has a FullPage route (Story 3-4 D21). Story 9-4 supersedes this with a build-time analyzer
    /// and an adopter-provided render-mode metadata source that makes the check meaningful.
    /// </summary>
    /// <remarks>
    /// <b>Inert placeholder today (ratified 2026-04-21, DN6):</b> <see cref="HasFullPageRoute"/> returns
    /// <c>true</c> for every command that appears in any manifest, so this validator cannot throw
    /// under the current single-source-of-truth. The branch is retained to (a) reserve the diagnostic
    /// ID, (b) keep the future-compatible code path warm, and (c) pin the validation contract so
    /// Story 9-4 can harden it without a surface-area change. Revisit when adopter-supplied
    /// render-mode metadata (e.g., from the Story 2-2 source generator) becomes available.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown with <see cref="FcDiagnosticIds.HFC1601_ManifestInvalid"/> when a command has no FullPage route — unreachable today.</exception>
    private void ValidateManifests() {
        foreach (DomainManifest manifest in _manifests) {
            // Defensive: a custom IFrontComposerRegistry consumer could theoretically register a
            // DomainManifest with a null Commands collection. The DomainManifest record declares
            // Commands non-nullable but nothing enforces it at runtime construction paths outside
            // the source-generator. Skip the manifest rather than NRE the startup validator.
            if (manifest.Commands is null) {
                continue;
            }

            foreach (string command in manifest.Commands) {
                if (!HasFullPageRoute(command)) {
                    throw new InvalidOperationException(
                        $"{FcDiagnosticIds.HFC1601_ManifestInvalid}: command '{command}' in bounded context '{manifest.BoundedContext}' has no FullPage route. Every registered command must emit a FullPage page so the command palette can route to it.");
                }
            }
        }
    }

    /// <inheritdoc />
    public void AddNavGroup(string name, string boundedContext) => _navGroups.Add((name, boundedContext));

    /// <inheritdoc />
    public IReadOnlyList<DomainManifest> GetManifests() => _manifests;

    /// <summary>
    /// Story 4-6 / Pass-3 review DN1-c — default heuristic: a command is treated as writable
    /// (eligible for empty-state CTA) unless its simple type name ends with "Query" /
    /// "Queries" / "Reader" (read-only suffix conventions). Adopters who need finer control
    /// override this by registering a custom <see cref="IFrontComposerRegistry"/> implementation
    /// that also implements <see cref="IFrontComposerCommandWriteAccessRegistry"/>.
    /// </summary>
    public bool IsCommandWritable(string commandTypeName) {
        if (string.IsNullOrWhiteSpace(commandTypeName)) {
            return false;
        }

        int lastDot = commandTypeName.LastIndexOf('.');
        string simple = lastDot >= 0 ? commandTypeName[(lastDot + 1)..] : commandTypeName;
        return !simple.EndsWith("Query", StringComparison.Ordinal)
            && !simple.EndsWith("Queries", StringComparison.Ordinal)
            && !simple.EndsWith("Reader", StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public bool HasFullPageRoute(string commandTypeName) {
        // Story 3-4 D21 — until the source generator surfaces real per-command render-mode metadata
        // (Story 9-4 build-time analyzer), every registered command is assumed to have a FullPage
        // route. Surface a true response when the command is in any manifest; false otherwise.
        if (string.IsNullOrWhiteSpace(commandTypeName)) {
            return false;
        }

        foreach (DomainManifest manifest in _manifests) {
            if (manifest.Commands.Contains(commandTypeName, StringComparer.Ordinal)) {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc />
    public bool TryGetCommandPolicy(string commandTypeName, out string policyName, out string? boundedContext) {
        policyName = string.Empty;
        boundedContext = null;
        if (string.IsNullOrWhiteSpace(commandTypeName)) {
            return false;
        }

        string trimmedKey = commandTypeName.Trim();

        // Snapshot the manifest list to a local array so a concurrent RegisterDomain call cannot
        // throw InvalidOperationException("Collection was modified") mid-enumeration. The
        // dispatch gate, palette filter, and CTA resolver all call this on the hot path, and
        // RegisterDomain runs during host startup hosted-service execution.
        DomainManifest[] snapshot = [.. _manifests];

        foreach (DomainManifest manifest in snapshot) {
            if (!manifest.CommandPolicies.TryGetValue(trimmedKey, out string? candidate)
                || string.IsNullOrWhiteSpace(candidate)) {
                continue;
            }

            string trimmedCandidate = candidate.Trim();
            if (policyName.Length > 0
                && !string.Equals(policyName, trimmedCandidate, StringComparison.Ordinal)) {
                _logger.LogWarning(
                    "FrontComposer registry policy lookup: command {CommandTypeName} resolved to multiple policies across manifests ({PriorPolicy} vs {IncomingPolicy}). Last-write-wins is the legacy default — duplicate cross-bounded-context policy declarations should be reconciled by the adopter.",
                    trimmedKey,
                    policyName,
                    trimmedCandidate);
            }

            policyName = trimmedCandidate;
            boundedContext = manifest.BoundedContext;
        }

        return policyName.Length > 0;
    }

    /// <inheritdoc />
    public void RegisterDomain(DomainManifest manifest) {
        int existingIndex = _manifests.FindIndex(m => string.Equals(m.BoundedContext, manifest.BoundedContext, StringComparison.Ordinal));
        if (existingIndex < 0) {
            _manifests.Add(Clone(manifest));
            return;
        }

        DomainManifest existing = _manifests[existingIndex];
        string name = ChooseName(existing.Name, manifest.Name, existing.BoundedContext);
        _manifests[existingIndex] = new DomainManifest(
            name,
            existing.BoundedContext,
            [.. existing.Projections.Concat(manifest.Projections).Distinct(StringComparer.Ordinal)],
            [.. existing.Commands.Concat(manifest.Commands).Distinct(StringComparer.Ordinal)],
            MergeCommandPolicies(existing.CommandPolicies, manifest.CommandPolicies));
    }

    private static DomainManifest Clone(DomainManifest manifest)
        => new(
            manifest.Name,
            manifest.BoundedContext,
            [.. manifest.Projections],
            [.. manifest.Commands],
            new Dictionary<string, string>(manifest.CommandPolicies, StringComparer.Ordinal));

    private IReadOnlyDictionary<string, string> MergeCommandPolicies(
        IReadOnlyDictionary<string, string> existing,
        IReadOnlyDictionary<string, string> incoming) {
        Dictionary<string, string> merged = new(existing, StringComparer.Ordinal);
        foreach (KeyValuePair<string, string> pair in incoming) {
            if (string.IsNullOrWhiteSpace(pair.Key) || string.IsNullOrWhiteSpace(pair.Value)) {
                _logger.LogInformation(
                    "FrontComposer registry merge: skipping command policy entry with empty key or value during manifest merge.");
                continue;
            }

            // Trim both key (command FQN) and value (policy name). The source generator emits
            // un-padded FullName keys but hand-rolled manifests may carry stray whitespace; the
            // catalog validator and palette filter compare ordinally so a single trailing space
            // would otherwise produce a phantom missing-policy entry.
            string keyTrimmed = pair.Key.Trim();
            string incomingTrimmed = pair.Value.Trim();
            if (merged.TryGetValue(keyTrimmed, out string? prior)
                && !string.Equals(prior, incomingTrimmed, StringComparison.Ordinal)) {
                _logger.LogWarning(
                    "FrontComposer registry merge: command {CommandTypeName} policy was overwritten from {PriorPolicy} to {IncomingPolicy} during manifest merge. Last-write-wins is the legacy default — duplicate policy declarations across manifests should be reconciled by the adopter.",
                    keyTrimmed,
                    prior,
                    incomingTrimmed);
            }

            merged[keyTrimmed] = incomingTrimmed;
        }

        return merged;
    }

    private static string ChooseName(string currentName, string candidateName, string boundedContext) {
        if (string.IsNullOrWhiteSpace(currentName)) {
            return candidateName;
        }

        if (string.Equals(currentName, boundedContext, StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(candidateName)
            && !string.Equals(candidateName, boundedContext, StringComparison.Ordinal)) {
            return candidateName;
        }

        return currentName;
    }
}
