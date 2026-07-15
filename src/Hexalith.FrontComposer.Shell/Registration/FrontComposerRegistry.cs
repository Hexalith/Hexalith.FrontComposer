using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.Shell.Infrastructure.Telemetry;

using Microsoft.Extensions.Logging;

namespace Hexalith.FrontComposer.Shell.Registration;
/// <summary>
/// Default implementation of <see cref="IFrontComposerRegistry"/>.
/// Stores registered domain manifests and navigation groups for runtime composition.
/// Domain registrations from <see cref="DomainRegistrationAction"/> are applied on construction.
/// </summary>
internal sealed class FrontComposerRegistry : IFrontComposerRegistry, IFrontComposerNavEntryRegistry, IFrontComposerFullPageRouteRegistry, IFrontComposerCommandWriteAccessRegistry, IFrontComposerCommandPolicyRegistry {
    private readonly object _sync = new();
    private readonly List<DomainManifest> _manifests = [];
    private readonly List<(string Name, string BoundedContext)> _navGroups = [];
    private readonly List<FrontComposerNavEntry> _navEntries = [];
    private readonly ILogger<FrontComposerRegistry> _logger;

    public FrontComposerRegistry(
        IEnumerable<DomainRegistrationAction> registrationActions,
        IEnumerable<DomainRegistrationWarning> warnings,
        ILogger<FrontComposerRegistry> logger) {
        _logger = logger;
        foreach (DomainRegistrationWarning warning in warnings) {
            FrontComposerWarningLog.RegistryRegistrationSkipped(
                logger,
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
    /// Walks every metadata-aware manifest and asserts each declared full-page command is also
    /// present in <see cref="DomainManifest.Commands"/>.
    /// </summary>
    /// <remarks>
    /// Legacy manifests have absent metadata and retain their compatibility behavior. Generated
    /// manifests always provide metadata, including an explicitly empty collection for Inline and
    /// CompactInline commands.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown with <see cref="FcDiagnosticIds.HFC1601_ManifestInvalid"/> when full-page metadata names an unregistered command.</exception>
    private void ValidateManifests() {
        foreach (DomainManifest manifest in SnapshotManifestReferences()) {
            ValidateManifest(manifest);
        }
    }

    /// <inheritdoc />
    public void AddNavGroup(string name, string boundedContext) {
        lock (_sync) {
            _navGroups.Add((name, boundedContext));
        }
    }

    /// <inheritdoc />
    public void AddNavEntry(FrontComposerNavEntry entry) {
        ArgumentNullException.ThrowIfNull(entry);
        lock (_sync) {
            _navEntries.Add(entry);
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<FrontComposerNavEntry> GetNavEntries() {
        lock (_sync) {
            return [.. _navEntries.OrderBy(static e => e.Order).ThenBy(static e => e.Title, StringComparer.Ordinal)];
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<DomainManifest> GetManifests() => SnapshotManifests();

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
        if (string.IsNullOrWhiteSpace(commandTypeName)) {
            return false;
        }

        foreach (DomainManifest manifest in SnapshotManifestReferences()) {
            if (!manifest.Commands.Contains(commandTypeName, StringComparer.Ordinal)) {
                continue;
            }

            if (manifest.FullPageCommands is null
                || manifest.FullPageCommands.Contains(commandTypeName, StringComparer.Ordinal)) {
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
        DomainManifest[] snapshot = SnapshotManifestReferences();

        foreach (DomainManifest manifest in snapshot) {
            if (!manifest.CommandPolicies.TryGetValue(trimmedKey, out string? candidate)
                || string.IsNullOrWhiteSpace(candidate)) {
                continue;
            }

            string trimmedCandidate = candidate.Trim();
            if (policyName.Length > 0
                && !string.Equals(policyName, trimmedCandidate, StringComparison.Ordinal)) {
                FrontComposerWarningLog.RegistryPolicyConflict(
                    _logger,
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
        ArgumentNullException.ThrowIfNull(manifest);
        DomainManifest incoming = Clone(manifest);
        ValidateManifest(incoming);

        lock (_sync) {
            int existingIndex = _manifests.FindIndex(m => string.Equals(m.BoundedContext, incoming.BoundedContext, StringComparison.Ordinal));
            if (existingIndex < 0) {
                _manifests.Add(incoming);
                return;
            }

            DomainManifest existing = _manifests[existingIndex];
            string name = ChooseName(existing.Name, incoming.Name, existing.BoundedContext);

            // Preserve the existing manifest's display metadata (Icon / NameKey / Resource) and fall back
            // to the incoming manifest's when the existing one left them unset, so a later registration can
            // supply the rail icon or localization pointer without a prior registration clobbering it. A
            // `with` expression carries every other field (including any future addition) untouched.
            // Null-coalesce the incoming collections to match the hardening in Clone / ValidateManifests:
            // a custom registry consumer or hand-rolled manifest can leave Projections / Commands null,
            // and Concat would otherwise throw under _sync during host startup on the second registration.
            DomainManifest merged = existing with {
                Name = name,
                Projections = [.. existing.Projections.Concat(incoming.Projections).Distinct(StringComparer.Ordinal)],
                Commands = [.. existing.Commands.Concat(incoming.Commands).Distinct(StringComparer.Ordinal)],
                CommandPolicies = MergeCommandPolicies(existing.CommandPolicies, incoming.CommandPolicies),
                FullPageCommands = MergeFullPageCommands(existing, incoming),
                Icon = existing.Icon ?? incoming.Icon,
                NameKey = existing.NameKey ?? incoming.NameKey,
                Resource = existing.Resource ?? incoming.Resource,
            };
            ValidateManifest(merged);
            _manifests[existingIndex] = merged;
        }
    }

    private DomainManifest[] SnapshotManifests() {
        lock (_sync) {
            return [.. _manifests.Select(Clone)];
        }
    }

    private DomainManifest[] SnapshotManifestReferences() {
        lock (_sync) {
            return [.. _manifests];
        }
    }

    private static DomainManifest Clone(DomainManifest manifest)
        => manifest with {
            Projections = manifest.Projections is null ? [] : [.. manifest.Projections],
            Commands = manifest.Commands is null ? [] : [.. manifest.Commands],
            CommandPolicies = manifest.CommandPolicies is null
                ? new Dictionary<string, string>(StringComparer.Ordinal)
                : new Dictionary<string, string>(manifest.CommandPolicies, StringComparer.Ordinal),
            FullPageCommands = manifest.FullPageCommands is null ? null : [.. manifest.FullPageCommands],
        };

    private static IReadOnlyList<string>? MergeFullPageCommands(DomainManifest existing, DomainManifest incoming) {
        if (existing.FullPageCommands is null && incoming.FullPageCommands is null) {
            return null;
        }

        HashSet<string> explicitlyDescribedCommands = new(StringComparer.Ordinal);
        HashSet<string> explicitlyReachable = new(StringComparer.Ordinal);
        HashSet<string> legacyCommands = new(StringComparer.Ordinal);

        AddExplicitMetadata(existing, explicitlyDescribedCommands, explicitlyReachable);
        AddExplicitMetadata(incoming, explicitlyDescribedCommands, explicitlyReachable);
        AddLegacyCommands(existing, legacyCommands);
        AddLegacyCommands(incoming, legacyCommands);

        return [.. existing.Commands
            .Concat(incoming.Commands)
            .Distinct(StringComparer.Ordinal)
            .Where(command => explicitlyReachable.Contains(command)
                || (!explicitlyDescribedCommands.Contains(command) && legacyCommands.Contains(command)))];
    }

    private static void AddExplicitMetadata(
        DomainManifest manifest,
        HashSet<string> explicitlyDescribedCommands,
        HashSet<string> reachable) {
        if (manifest.FullPageCommands is null) {
            return;
        }

        explicitlyDescribedCommands.UnionWith(manifest.Commands);
        reachable.UnionWith(manifest.FullPageCommands);
    }

    private static void AddLegacyCommands(DomainManifest manifest, HashSet<string> legacyCommands) {
        if (manifest.FullPageCommands is not null) {
            return;
        }

        legacyCommands.UnionWith(manifest.Commands);
    }

    private static void ValidateManifest(DomainManifest manifest) {
        if (manifest.FullPageCommands is null) {
            return;
        }

        foreach (string command in manifest.FullPageCommands) {
            if (!manifest.Commands.Contains(command, StringComparer.Ordinal)) {
                throw new InvalidOperationException(
                    $"{FcDiagnosticIds.HFC1601_ManifestInvalid}: full-page command '{command}' in bounded context '{manifest.BoundedContext}' is not present in the manifest command membership.");
            }
        }
    }

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
                FrontComposerWarningLog.RegistryPolicyOverwritten(
                    _logger,
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
