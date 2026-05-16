using System.Reflection;

using Hexalith.FrontComposer.Shell.Extensions;
using Hexalith.FrontComposer.Shell.Infrastructure.EventStore;
using Hexalith.FrontComposer.Shell.State.PendingCommands;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Shell.Tests.Governance;

[Trait("Category", "Governance")]
public sealed class PendingStatusReopenGovernanceTests {
    private const int MaxDiagnostics = 10;
    private const string DeferredWorkRow = "DW-0461";
    private const string ExpectedClassificationField = "Final classification 2026-05-15";
    private const string ExpectedClassificationValue = "accepted-constraint";
    private const string ExpectedConstraint = "PENDING-STATUS-NULL-PROVIDER-V1";
    private const string StoryReferences = "DW-0461; Story 12.3; Story 12.3.1";

    private static readonly string[] TriggerPhrases = [
        "provider-backed pending-command",
        "EventStore pending-command status endpoint",
        "IPendingCommandStatusQuery provider",
    ];

    [Fact]
    public void DeferredWorkRow_ParsesAcceptedNullProviderConstraint() {
        ConstraintGuardState state = LoadCurrentGuardState();

        state.IsValid.ShouldBeTrue(state.Diagnostic);
        state.IsActive.ShouldBeTrue(state.Diagnostic);
    }

    [Fact]
    public void ReleaseNotes_DoNotClaimPendingStatusProviderTriggersWhileConstraintAccepted() {
        ConstraintGuardState state = LoadCurrentGuardState();
        state.IsValid.ShouldBeTrue(state.Diagnostic);
        if (!state.IsActive) {
            state.Diagnostic.ShouldContain("not active");
            return;
        }

        string root = RepositoryRoot();
        List<string> violations = [];
        foreach (string path in EnumerateReleaseNoteSurfaces(root)) {
            violations.AddRange(ScanReleaseNoteFile(root, path));
        }

        violations.ShouldBeEmpty(FormatDiagnostics(violations));
    }

    [Fact]
    public void EventStoreOptions_DoNotExposePendingStatusResourceContractWhileConstraintAccepted() {
        ConstraintGuardState state = LoadCurrentGuardState();
        state.IsValid.ShouldBeTrue(state.Diagnostic);
        if (!state.IsActive) {
            state.Diagnostic.ShouldContain("not active");
            return;
        }

        List<string> suspicious = FindSuspiciousOptionPropertyNames(
            typeof(EventStoreOptions)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Select(static p => p.Name));

        suspicious.ShouldBeEmpty(
            "EventStoreOptions exposes pending-status contract drift while "
            + $"{ExpectedConstraint} is active ({StoryReferences}): "
            + string.Join(", ", suspicious));
    }

    [Fact]
    public void FrontComposerDi_UsesOnlyNullPendingCommandStatusQueryWhileConstraintAccepted() {
        ConstraintGuardState state = LoadCurrentGuardState();
        state.IsValid.ShouldBeTrue(state.Diagnostic);
        if (!state.IsActive) {
            state.Diagnostic.ShouldContain("not active");
            return;
        }

        ServiceCollection frontComposerServices = [];
        _ = frontComposerServices.AddHexalithFrontComposer();
        AssertPendingStatusDescriptorsAreNullProviderOnly(frontComposerServices, "AddHexalithFrontComposer");

        ServiceCollection eventStoreServices = [];
        _ = eventStoreServices.AddHexalithFrontComposer();
        _ = eventStoreServices.AddHexalithEventStore(options => {
            options.BaseAddress = new Uri("https://eventstore.test");
            options.RequireAccessToken = false;
        });
        AssertPendingStatusDescriptorsAreNullProviderOnly(
            eventStoreServices,
            "AddHexalithFrontComposer + AddHexalithEventStore");
    }

    [Fact]
    public void DeferredWorkRow_FailsClosedForMissingDuplicateMalformedOrChangedConstraintRows() {
        ParseGuardState([]).IsValid.ShouldBeFalse();

        ParseGuardState([
            "Reconciliation: Row: DW-0461; Final classification 2026-05-15: accepted-constraint; Constraint: PENDING-STATUS-NULL-PROVIDER-V1",
            "Reconciliation: Row: DW-0461; Final classification 2026-05-15: accepted-constraint; Constraint: PENDING-STATUS-NULL-PROVIDER-V1",
        ]).IsValid.ShouldBeFalse();

        ParseGuardState([
            "Reconciliation: Row: DW-0461; Final classification 2026-05-15 accepted-constraint; Constraint: PENDING-STATUS-NULL-PROVIDER-V1",
        ]).IsValid.ShouldBeFalse();

        ParseGuardState([
            "Reconciliation: Row: DW-0461; Final classification 2026-05-15: accepted-constraint",
        ]).IsValid.ShouldBeFalse();

        ParseGuardState([
            "Reconciliation: Row: DW-0461; Final classification 2026-05-15: accepted-constraint; Constraint: PENDING-STATUS-PROVIDER-V2",
        ]).IsValid.ShouldBeFalse();

        ParseGuardState([
            "Reconciliation: Row: DW-0461; Final classification 2026-05-15: accepted-constraint; Constraint: ",
        ]).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void DeferredWorkRow_AllowsWellFormedNonAcceptedStateWithoutInvertingProviderSupport() {
        ConstraintGuardState state = ParseGuardState([
            "Reconciliation: Row: DW-0461; Final classification 2026-05-15: reopened; Constraint: PENDING-STATUS-NULL-PROVIDER-V1",
        ]);

        state.IsValid.ShouldBeTrue(state.Diagnostic);
        state.IsActive.ShouldBeFalse();
        state.Diagnostic.ShouldContain("not active");
        state.Diagnostic.ShouldContain(StoryReferences);
    }

    [Fact]
    public void ReleaseNoteTriggerScanner_CoversMetadataAllowanceCaseInsensitiveHitsAndNearMisses() {
        string[] lines = [
            "# Release",
            "",
            "Provider-backed pending-command status is now ready.",
            "",
            "## Constraint Metadata",
            "",
            "| Field | Value |",
            "| --- | --- |",
            "| Constraint name | `PENDING-STATUS-NULL-PROVIDER-V1` |",
            "| Agent impact | Agents must not claim PROVIDER-BACKED PENDING-COMMAND status. |",
            "",
            "A provider backed pending command near miss is ordinary prose.",
        ];

        List<string> violations = ScanReleaseNoteLines("docs/release.md", lines);

        violations.Count.ShouldBe(1);
        violations.Single().ShouldContain("docs/release.md:3");
        violations.Single().ShouldContain("provider-backed pending-command");
        violations.Single().ShouldContain(StoryReferences);
    }

    [Fact]
    public void ReleaseNoteTriggerScanner_BoundsMetadataAndDiagnosticOutput() {
        string[] lines = [
            "## Constraint Metadata",
            "| Field | Value |",
            "| --- | --- |",
            "| Constraint name | `PENDING-STATUS-NULL-PROVIDER-V1` |",
            "| One | value |",
            "| Two | value |",
            "| Three | value |",
            "| Four | value |",
            "| Five | value |",
            "| Six | value |",
            "| Seven | value |",
            "| Eight | value |",
            "| Nine | value |",
            "| Ten | value |",
            "| Eleven | provider-backed pending-command status |",
            "| Twelve | value |",
            "| Thirteen | EventStore pending-command status endpoint |",
        ];

        List<string> violations = ScanReleaseNoteLines("docs/release.md", lines);

        violations.Count.ShouldBe(1);
        violations.Single().ShouldContain("docs/release.md:17");
        violations.Single().ShouldNotContain(Path.GetFullPath("."));

        List<string> manyViolations = ScanReleaseNoteLines(
            "docs/many.md",
            Enumerable.Range(1, MaxDiagnostics + 4)
                .Select(static _ => "IPendingCommandStatusQuery provider")
                .ToArray());

        manyViolations.Count.ShouldBe(MaxDiagnostics + 1);
        manyViolations.Last().ShouldContain("additional trigger hits suppressed");
    }

    [Theory]
    [InlineData("BaseAddress", false)]
    [InlineData("CommandEndpointPath", false)]
    [InlineData("QueryEndpointPath", false)]
    [InlineData("ProjectionChangesHubPath", false)]
    [InlineData("PendingStatusEndpoint", true)]
    [InlineData("PendingCommandStatusUrl", true)]
    [InlineData("CommandStatusQueryProvider", true)]
    [InlineData("StatusResourceMetadata", true)]
    [InlineData("StatusQueryProvider", true)]
    public void EventStoreOptionsPropertyClassifier_CoversSuspiciousStatusResourceNames(
        string propertyName,
        bool expectedSuspicious) {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);

        IsSuspiciousStatusContractProperty(propertyName).ShouldBe(expectedSuspicious);
    }

    [Fact]
    public void PendingStatusDiDescriptorClassifier_FailsHiddenOrProviderBackedDescriptors() {
        ServiceCollection valid = [];
        valid.AddScoped<IPendingCommandStatusQuery, NullPendingCommandStatusQuery>();
        FindPendingStatusDescriptorViolations(valid, "valid").ShouldBeEmpty();

        ServiceCollection provider = [];
        provider.AddScoped<IPendingCommandStatusQuery, SyntheticPendingCommandStatusQuery>();
        FindPendingStatusDescriptorViolations(provider, "provider").Single().ShouldContain(nameof(SyntheticPendingCommandStatusQuery));

        ServiceCollection factory = [];
        factory.AddScoped<IPendingCommandStatusQuery>(static _ => new NullPendingCommandStatusQuery());
        FindPendingStatusDescriptorViolations(factory, "factory").Single().ShouldContain("factory");

        ServiceCollection instance = [];
        instance.AddSingleton<IPendingCommandStatusQuery>(new NullPendingCommandStatusQuery());
        List<string> instanceViolations = FindPendingStatusDescriptorViolations(instance, "instance");
        instanceViolations.Count.ShouldBe(2);
        string.Join(Environment.NewLine, instanceViolations).ShouldContain("instance");
        string.Join(Environment.NewLine, instanceViolations).ShouldContain("Singleton");
    }

    private static ConstraintGuardState LoadCurrentGuardState() {
        string root = RepositoryRoot();
        string ledgerPath = Path.Combine(root, "_bmad-output", "implementation-artifacts", "deferred-work.md");
        return ParseGuardState(File.ReadAllLines(ledgerPath));
    }

    private static ConstraintGuardState ParseGuardState(IReadOnlyList<string> lines) {
        List<string> rows = [.. lines.Where(static line => line.Contains("Row: DW-0461", StringComparison.Ordinal))];
        if (rows.Count != 1) {
            return ConstraintGuardState.Invalid(
                $"Expected exactly one Row: {DeferredWorkRow} ledger row but found {rows.Count}; {StoryReferences}.");
        }

        string row = rows[0];
        if (!TryReadField(row, ExpectedClassificationField, out string classification)) {
            return ConstraintGuardState.Invalid(
                $"Malformed {DeferredWorkRow} row: missing or empty '{ExpectedClassificationField}' field; {StoryReferences}.");
        }

        if (!TryReadField(row, "Constraint", out string constraint)) {
            return ConstraintGuardState.Invalid(
                $"Malformed {DeferredWorkRow} row: missing or empty 'Constraint' field; {StoryReferences}.");
        }

        constraint = constraint.Trim('`');
        if (!string.Equals(constraint, ExpectedConstraint, StringComparison.Ordinal)) {
            return ConstraintGuardState.Invalid(
                $"{DeferredWorkRow} constraint token changed from {ExpectedConstraint} to '{constraint}'; fail closed; {StoryReferences}.");
        }

        if (!string.Equals(classification, ExpectedClassificationValue, StringComparison.Ordinal)) {
            return ConstraintGuardState.Inactive(
                $"{DeferredWorkRow} guard is not active because {ExpectedClassificationField} is '{classification}', "
                + $"not '{ExpectedClassificationValue}'; provider-support assertions remain future-story scope; {StoryReferences}.");
        }

        return ConstraintGuardState.Active(
            $"{DeferredWorkRow} active guard: {ExpectedClassificationField}: {ExpectedClassificationValue}; "
            + $"Constraint: {ExpectedConstraint}; {StoryReferences}.");
    }

    private static bool TryReadField(string row, string fieldName, out string value) {
        foreach (string segment in row.Split(';')) {
            string trimmed = segment.Trim();
            if (!trimmed.StartsWith(fieldName, StringComparison.Ordinal)) {
                continue;
            }

            if (trimmed.Length == fieldName.Length || trimmed[fieldName.Length] != ':') {
                value = string.Empty;
                return false;
            }

            value = trimmed[(fieldName.Length + 1)..].Trim();
            return value.Length > 0;
        }

        value = string.Empty;
        return false;
    }

    private static IEnumerable<string> EnumerateReleaseNoteSurfaces(string root) {
        string docsRoot = Path.Combine(root, "docs");
        if (Directory.Exists(docsRoot)) {
            EnumerationOptions options = new() {
                RecurseSubdirectories = true,
                AttributesToSkip = FileAttributes.ReparsePoint | FileAttributes.Hidden,
                IgnoreInaccessible = true,
            };
            foreach (string file in Directory.EnumerateFiles(docsRoot, "*.md", options)
                .Where(path => ShouldScanReleaseSurface(root, path))) {
                yield return file;
            }
        }

        foreach (string file in Directory.EnumerateFiles(root, "CHANGELOG*.md", SearchOption.TopDirectoryOnly)
            .Where(path => ShouldScanReleaseSurface(root, path))) {
            yield return file;
        }

        string implementationArtifacts = Path.Combine(root, "_bmad-output", "implementation-artifacts");
        if (!Directory.Exists(implementationArtifacts)) {
            yield break;
        }

        foreach (string file in Directory.EnumerateFiles(implementationArtifacts, "*release-note*.md", SearchOption.TopDirectoryOnly)
            .Where(path => ShouldScanReleaseSurface(root, path))) {
            yield return file;
        }
    }

    private static bool ShouldScanReleaseSurface(string root, string path) {
        string relative = RelativePath(root, path);
        return !relative.Contains("/bin/", StringComparison.Ordinal)
            && !relative.Contains("/obj/", StringComparison.Ordinal)
            && !relative.StartsWith(".git/", StringComparison.Ordinal)
            && !relative.StartsWith(".agents/", StringComparison.Ordinal)
            && !relative.StartsWith("docs/_site/", StringComparison.Ordinal);
    }

    private static List<string> ScanReleaseNoteFile(string root, string path)
        => ScanReleaseNoteLines(RelativePath(root, path), File.ReadAllLines(path));

    private static List<string> ScanReleaseNoteLines(string relativePath, IReadOnlyList<string> lines) {
        List<string> violations = [];
        for (int i = 0; i < lines.Count; i++) {
            foreach (string trigger in TriggerPhrases) {
                if (!lines[i].Contains(trigger, StringComparison.OrdinalIgnoreCase)) {
                    continue;
                }

                if (IsInsideConstraintMetadataAllowance(lines, i)) {
                    continue;
                }

                if (violations.Count < MaxDiagnostics) {
                    violations.Add(
                        $"{relativePath}:{i + 1}: trigger '{trigger}' violates {ExpectedConstraint}; {StoryReferences}.");
                }
            }
        }

        if (violations.Count == MaxDiagnostics) {
            int suppressed = CountReleaseNoteTriggerHits(lines) - MaxDiagnostics;
            if (suppressed > 0) {
                violations.Add($"{relativePath}: additional trigger hits suppressed: {suppressed}; {StoryReferences}.");
            }
        }

        return violations;
    }

    private static int CountReleaseNoteTriggerHits(IReadOnlyList<string> lines)
        => lines.Sum(line => TriggerPhrases.Count(trigger => line.Contains(trigger, StringComparison.OrdinalIgnoreCase)));

    private static bool IsInsideConstraintMetadataAllowance(IReadOnlyList<string> lines, int matchIndex) {
        for (int start = matchIndex; start >= 0; start--) {
            string trimmed = lines[start].Trim();
            if (ContainsConstraintMetadataCaption(trimmed)) {
                MetadataWindow window = BuildConstraintMetadataWindow(lines, start);
                return matchIndex >= window.StartIndex
                    && matchIndex <= window.EndIndex
                    && window.ContainsExpectedConstraint;
            }

            if (IsMarkdownHeading(trimmed) && start != matchIndex) {
                break;
            }
        }

        return false;
    }

    private static MetadataWindow BuildConstraintMetadataWindow(IReadOnlyList<string> lines, int headingIndex) {
        int startIndex = headingIndex;
        int endIndex = headingIndex;
        int dataLines = 0;
        bool containsConstraint = lines[headingIndex].Contains(ExpectedConstraint, StringComparison.Ordinal);
        bool tableMode = false;
        bool started = false;

        for (int i = headingIndex + 1; i < lines.Count; i++) {
            string trimmed = lines[i].Trim();
            if (trimmed.Length == 0) {
                if (started) {
                    break;
                }

                continue;
            }

            if (IsMarkdownHeading(trimmed)) {
                break;
            }

            started = true;
            startIndex = Math.Min(startIndex, i);
            if (IsMarkdownTableLine(trimmed)) {
                tableMode = true;
            }

            bool countsTowardLimit = !tableMode || IsMarkdownTableDataRow(lines, i);
            if (countsTowardLimit) {
                dataLines++;
                if (dataLines > 12) {
                    break;
                }
            }

            containsConstraint |= trimmed.Contains(ExpectedConstraint, StringComparison.Ordinal);
            endIndex = i;
        }

        return new MetadataWindow(startIndex, endIndex, containsConstraint);
    }

    private static bool ContainsConstraintMetadataCaption(string trimmed)
        => trimmed.Contains("Constraint Metadata", StringComparison.Ordinal)
            && (IsMarkdownHeading(trimmed)
                || trimmed.StartsWith("Table", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("|", StringComparison.Ordinal));

    private static bool IsMarkdownHeading(string trimmed)
        => trimmed.StartsWith("#", StringComparison.Ordinal);

    private static bool IsMarkdownTableLine(string trimmed)
        => trimmed.StartsWith("|", StringComparison.Ordinal) && trimmed.EndsWith("|", StringComparison.Ordinal);

    private static bool IsMarkdownTableDataRow(IReadOnlyList<string> lines, int index) {
        string trimmed = lines[index].Trim();
        if (!IsMarkdownTableLine(trimmed)) {
            return false;
        }

        if (index + 1 < lines.Count && IsMarkdownTableSeparator(lines[index + 1].Trim())) {
            return false;
        }

        if (IsMarkdownTableSeparator(trimmed)) {
            return false;
        }

        return true;
    }

    private static bool IsMarkdownTableSeparator(string trimmed) {
        if (!IsMarkdownTableLine(trimmed)) {
            return false;
        }

        string compact = trimmed.Replace("|", string.Empty, StringComparison.Ordinal).Trim();
        return compact.Length > 0 && compact.All(static ch => ch is '-' or ':' or ' ');
    }

    private static List<string> FindSuspiciousOptionPropertyNames(IEnumerable<string> propertyNames)
        => [.. propertyNames.Where(IsSuspiciousStatusContractProperty).Order(StringComparer.Ordinal)];

    private static bool IsSuspiciousStatusContractProperty(string propertyName) {
        string[] exactSuspiciousFragments = [
            "PendingStatus",
            "PendingCommandStatus",
            "CommandStatusQuery",
            "StatusResource",
            "StatusQueryProvider",
        ];
        if (exactSuspiciousFragments.Any(fragment => propertyName.Contains(fragment, StringComparison.OrdinalIgnoreCase))) {
            return true;
        }

        string[] resourceFragments = ["Endpoint", "Uri", "Url", "Path", "Resource", "Metadata"];
        return propertyName.Contains("Status", StringComparison.OrdinalIgnoreCase)
            && resourceFragments.Any(fragment => propertyName.Contains(fragment, StringComparison.OrdinalIgnoreCase));
    }

    private static void AssertPendingStatusDescriptorsAreNullProviderOnly(
        IServiceCollection services,
        string scenario) {
        List<string> violations = FindPendingStatusDescriptorViolations(services, scenario);
        violations.ShouldBeEmpty(FormatDiagnostics(violations));
    }

    private static List<string> FindPendingStatusDescriptorViolations(
        IServiceCollection services,
        string scenario) {
        ServiceDescriptor[] descriptors = [.. services.Where(static d => d.ServiceType == typeof(IPendingCommandStatusQuery))];
        if (descriptors.Length == 0) {
            return [$"{scenario}: no IPendingCommandStatusQuery descriptor registered; {StoryReferences}."];
        }

        List<string> violations = [];
        foreach (ServiceDescriptor descriptor in descriptors) {
            if (descriptor.Lifetime != ServiceLifetime.Scoped) {
                violations.Add($"{scenario}: {FormatDescriptor(descriptor)} must be Scoped; {StoryReferences}.");
            }

            if (descriptor.ImplementationType != typeof(NullPendingCommandStatusQuery)) {
                violations.Add(
                    $"{scenario}: {FormatDescriptor(descriptor)} must use ImplementationType "
                    + $"{nameof(NullPendingCommandStatusQuery)}; {StoryReferences}.");
            }
        }

        return [.. violations.Take(MaxDiagnostics)];
    }

    private static string FormatDescriptor(ServiceDescriptor descriptor) {
        string implementation = descriptor.ImplementationType is not null
            ? descriptor.ImplementationType.Name
            : descriptor.ImplementationFactory is not null
                ? "factory"
                : descriptor.ImplementationInstance is not null
                    ? "instance"
                    : "unknown";

        return $"IPendingCommandStatusQuery lifetime={descriptor.Lifetime} implementation={implementation}";
    }

    private static string FormatDiagnostics(IReadOnlyList<string> diagnostics) {
        if (diagnostics.Count <= MaxDiagnostics) {
            return string.Join(Environment.NewLine, diagnostics);
        }

        return string.Join(Environment.NewLine, diagnostics.Take(MaxDiagnostics))
            + Environment.NewLine
            + $"additional diagnostics suppressed: {diagnostics.Count - MaxDiagnostics}; {StoryReferences}.";
    }

    private static string RepositoryRoot() {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null) {
            if (File.Exists(Path.Combine(dir.FullName, "Hexalith.FrontComposer.sln"))) {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }

    private static string RelativePath(string root, string path) {
        string fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string fullPath = Path.GetFullPath(path);
        if (!fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase)) {
            throw new InvalidOperationException($"Path escaped repository root: {Path.GetFileName(path)}");
        }

        return Path.GetRelativePath(fullRoot, fullPath).Replace('\\', '/');
    }

    private sealed record ConstraintGuardState(bool IsValid, bool IsActive, string Diagnostic) {
        public static ConstraintGuardState Active(string diagnostic) => new(true, true, diagnostic);

        public static ConstraintGuardState Inactive(string diagnostic) => new(true, false, diagnostic);

        public static ConstraintGuardState Invalid(string diagnostic) => new(false, false, diagnostic);
    }

    private sealed record MetadataWindow(int StartIndex, int EndIndex, bool ContainsExpectedConstraint);

    private sealed class SyntheticPendingCommandStatusQuery : IPendingCommandStatusQuery {
        public ValueTask<PendingCommandOutcomeObservation?> QueryAsync(
            PendingCommandEntry pendingCommand,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult<PendingCommandOutcomeObservation?>(null);
    }
}
