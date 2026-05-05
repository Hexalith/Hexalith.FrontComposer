using System.Reflection;

using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.SourceTools.Diagnostics;

using Microsoft.CodeAnalysis;

using Shouldly;
using Xunit;

namespace Hexalith.FrontComposer.SourceTools.Tests.Drift.Diagnostics;

/// <summary>
/// AC12 / T4 — Story 9-1 catalog discipline. Drift diagnostics live in a contiguous HFC range
/// allocated immediately after HFC1057 (current Unshipped allocation tail). Sibling to
/// <see cref="DiagnosticCatalogTests"/>; the existing catalog test asserts cross-cutting shape,
/// while this test asserts:
/// (a) every newly-introduced drift descriptor falls in the planned HFC1058+ range,
/// (b) every drift descriptor populates <see cref="DiagnosticDescriptor.HelpLinkUri"/>
///     against the canonical docs prefix,
/// (c) every drift descriptor has a matching <see cref="FcDiagnosticIds"/> constant,
/// (d) every drift descriptor is documented in <c>AnalyzerReleases.Unshipped.md</c>.
/// </summary>
public sealed class DriftDiagnosticCatalogTests {
    private const string SkipReason = "RED-PHASE: T4 — drift diagnostic descriptors not yet introduced.";

    private const string DocsLinkPrefix = "https://hexalith.github.io/FrontComposer/diagnostics/";
    private const int FirstAllocatedDriftId = 1058;
    private const int LastReservedDriftId = 1099;

    [Fact(Skip = SkipReason)]
    public void DriftDescriptors_AreContiguousFromHfc1058() {
        DiagnosticDescriptor[] driftDescriptors = [.. DriftDescriptors()];

        driftDescriptors.Length.ShouldBeGreaterThan(0,
            "AC12 — at least one drift descriptor must be allocated in T4.");
        int[] numericIds = [.. driftDescriptors.Select(d => int.Parse(d.Id["HFC".Length..], System.Globalization.CultureInfo.InvariantCulture))];
        numericIds.Min().ShouldBeGreaterThanOrEqualTo(FirstAllocatedDriftId);
        numericIds.Max().ShouldBeLessThanOrEqualTo(LastReservedDriftId);
    }

    [Fact(Skip = SkipReason)]
    public void EveryDriftDescriptor_PopulatesHelpLinkUri_AgainstCanonicalDocsPrefix() {
        foreach (DiagnosticDescriptor descriptor in DriftDescriptors()) {
            descriptor.HelpLinkUri.ShouldNotBeNullOrWhiteSpace($"AC12 — {descriptor.Id} missing HelpLinkUri.");
            descriptor.HelpLinkUri.ShouldStartWith(DocsLinkPrefix);
            descriptor.HelpLinkUri.ShouldContain(descriptor.Id, Case.Sensitive);
        }
    }

    [Fact(Skip = SkipReason)]
    public void EveryDriftDescriptorId_HasMatchingFcDiagnosticIdsConstant() {
        HashSet<string> driftIds = [.. DriftDescriptors().Select(d => d.Id)];
        HashSet<string> constantIds = [.. typeof(FcDiagnosticIds)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .Where(v => v.StartsWith("HFC", StringComparison.Ordinal))];

        foreach (string driftId in driftIds) {
            constantIds.ShouldContain(driftId,
                $"AC12 / T4 — every drift descriptor must have a matching FcDiagnosticIds constant ({driftId}).");
        }
    }

    [Fact(Skip = SkipReason)]
    public void EveryDriftDescriptor_IsDocumentedInUnshippedAnalyzerReleases() {
        string releasesPath = LocateAnalyzerReleasesUnshipped();
        string releaseNotes = File.ReadAllText(releasesPath);

        foreach (DiagnosticDescriptor descriptor in DriftDescriptors()) {
            releaseNotes.ShouldContain(descriptor.Id,
                customMessage: $"AC12 / T4 — {descriptor.Id} must be listed in AnalyzerReleases.Unshipped.md.");
        }
    }

    [Fact(Skip = SkipReason)]
    public void Hfc1010_IsNotReusedForDriftCategory() {
        // Story 1-8 / HFC1010 owns hot-reload/full-rebuild guidance. Drift diagnostics MUST NOT
        // borrow that ID — they must allocate fresh IDs in HFC1058+.
        IEnumerable<DiagnosticDescriptor> driftDescriptors = DriftDescriptors();
        driftDescriptors.Any(d => d.Id == FcDiagnosticIds.HFC1010_FullRebuildRequired).ShouldBeFalse(
            "AC12 — HFC1010 is owned by Story 1-8 hot-reload guidance and must not be reused for drift.");
    }

    private static IEnumerable<DiagnosticDescriptor> DriftDescriptors() {
        foreach (FieldInfo field in typeof(DiagnosticDescriptors).GetFields(BindingFlags.Public | BindingFlags.Static)) {
            if (field.FieldType == typeof(DiagnosticDescriptor)
                && field.GetValue(null) is DiagnosticDescriptor descriptor
                && descriptor.Id.StartsWith("HFC", StringComparison.Ordinal)
                && int.TryParse(descriptor.Id.AsSpan("HFC".Length), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out int n)
                && n >= FirstAllocatedDriftId
                && n <= LastReservedDriftId) {
                yield return descriptor;
            }
        }
    }

    private static string LocateAnalyzerReleasesUnshipped() {
        string here = Path.GetDirectoryName(typeof(DriftDiagnosticCatalogTests).Assembly.Location)!;
        for (int i = 0; i < 8; i++) {
            string candidate = Path.Combine(here, "src", "Hexalith.FrontComposer.SourceTools", "AnalyzerReleases.Unshipped.md");
            if (File.Exists(candidate)) {
                return candidate;
            }
            here = Path.GetDirectoryName(here)
                ?? throw new InvalidOperationException("Could not locate repo root from test assembly location.");
        }
        throw new FileNotFoundException("AnalyzerReleases.Unshipped.md not found.");
    }
}
