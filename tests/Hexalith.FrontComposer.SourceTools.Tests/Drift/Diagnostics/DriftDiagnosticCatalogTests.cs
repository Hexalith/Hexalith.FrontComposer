using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.SourceTools.Diagnostics;

using Microsoft.CodeAnalysis;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Drift.Diagnostics;

/// <summary>
/// AC12 / T4 — Story 9-1 catalog discipline.
/// </summary>
public sealed class DriftDiagnosticCatalogTests {
    private const string DocsLinkPrefix = "https://hexalith.github.io/FrontComposer/diagnostics/";
    private const int FirstAllocatedDriftId = 1058;
    private const int LastReservedDriftId = 1099;

    [Fact]
    public void DriftDescriptors_AreContiguousFromHfc1058() {
        DiagnosticDescriptor[] driftDescriptors = [.. DriftDescriptors()];

        driftDescriptors.Length.ShouldBeGreaterThan(0,
            "AC12 — at least one drift descriptor must be allocated in T4.");
        int[] numericIds = [.. driftDescriptors.Select(d => int.Parse(d.Id["HFC".Length..], CultureInfo.InvariantCulture))];
        numericIds.Min().ShouldBeGreaterThanOrEqualTo(FirstAllocatedDriftId);
        numericIds.Max().ShouldBeLessThanOrEqualTo(LastReservedDriftId);

        // CH-15 — IDs must be unique.
        numericIds.Distinct().Count().ShouldBe(numericIds.Length,
            "AC12 / T4 — drift descriptor IDs must be unique.");

        // CM-10 — no gaps inside the allocated range.
        int[] sorted = [.. numericIds.OrderBy(n => n)];
        Enumerable.Range(sorted[0], sorted[^1] - sorted[0] + 1)
            .ShouldBe(sorted, ignoreOrder: false,
                "AC12 / T4 — drift IDs must be contiguous (no gaps in HFC1058+).");
    }

    [Fact]
    public void EveryDriftDescriptor_PopulatesHelpLinkUri_AgainstCanonicalDocsPrefix() {
        static Regex pathSegment(string id) => new($"/{Regex.Escape(id)}(/|$|\\.html|\\.md)", RegexOptions.CultureInvariant);

        foreach (DiagnosticDescriptor descriptor in DriftDescriptors()) {
            descriptor.HelpLinkUri.ShouldNotBeNullOrWhiteSpace($"AC12 — {descriptor.Id} missing HelpLinkUri.");
            descriptor.HelpLinkUri.ShouldStartWith(DocsLinkPrefix);
            // CM-4 — assert the ID appears as a path segment, not just any substring.
            pathSegment(descriptor.Id).IsMatch(descriptor.HelpLinkUri).ShouldBeTrue(
                $"AC12 — {descriptor.Id} HelpLinkUri must contain the ID as a path segment, got: {descriptor.HelpLinkUri}");
        }
    }

    [Fact]
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

    [Fact]
    public void EveryDriftDescriptor_IsDocumentedInUnshippedAnalyzerReleases() {
        string releasesPath = LocateAnalyzerReleasesUnshipped();
        string releaseNotes = File.ReadAllText(releasesPath);

        // CH-7 — assert each ID appears as a real release-notes row of shape:
        // `HFC#### | <Severity> | <Category> | <Notes>` rather than a comment-only mention.
        foreach (DiagnosticDescriptor descriptor in DriftDescriptors()) {
            Regex row = new($@"^\s*{Regex.Escape(descriptor.Id)}\s*\|", RegexOptions.Multiline);
            row.IsMatch(releaseNotes).ShouldBeTrue(
                $"AC12 / T4 — {descriptor.Id} must be listed in AnalyzerReleases.Unshipped.md as a `ID | Severity | Category | Notes` row.");
        }
    }

    [Fact]
    public void DriftDescriptors_HaveUniqueTitles() {
        // CH-14 — copy-paste regression where two descriptors share Title would ship silently.
        // MessageFormat is intentionally "{0}" across drift descriptors (Story 9-1 DEF-9-1A-3:
        // structured data flows through the diagnostic property bag), so duplicates there are
        // by-design and not asserted.
        DiagnosticDescriptor[] descriptors = [.. DriftDescriptors()];

        IEnumerable<IGrouping<string, DiagnosticDescriptor>> dupTitles = descriptors
            .GroupBy(d => d.Title.ToString(CultureInfo.InvariantCulture), StringComparer.Ordinal)
            .Where(g => g.Count() > 1);
        dupTitles.ShouldBeEmpty(
            "AC12 — drift descriptor Title values must be unique across descriptors.");
    }

    [Fact]
    public void Hfc1010_IsNotReusedByAnyDriftDescriptor() {
        // CC-3 — scan ALL DiagnosticDescriptors fields, not just the drift range, for any
        // descriptor sharing HFC1010's ID. The previous filter excluded HFC1010 by construction,
        // making the assertion tautological.
        IEnumerable<DiagnosticDescriptor> allDescriptors = AllSourceToolsDescriptors();
        int hfc1010Count = allDescriptors.Count(d => d.Id == FcDiagnosticIds.HFC1010_FullRebuildRequired);
        hfc1010Count.ShouldBe(1,
            "AC12 — HFC1010 is owned by Story 1-8 and must be allocated to exactly one descriptor (no drift reuse).");
    }

    [Fact]
    public void EveryHfcDescriptor_HasFourDigitNumericTail() {
        // CM-14 — mis-shaped IDs (e.g. HFC10A0) would slip through DriftDescriptors() filter.
        IEnumerable<DiagnosticDescriptor> hfcDescriptors = AllSourceToolsDescriptors()
            .Where(d => d.Id.StartsWith("HFC", StringComparison.Ordinal));

        foreach (DiagnosticDescriptor d in hfcDescriptors) {
            string tail = d.Id["HFC".Length..];
            (tail.Length == 4 && tail.All(char.IsDigit)).ShouldBeTrue(
                $"AC12 — descriptor {d.Id} must follow HFCxxxx shape with a 4-digit numeric tail.");
        }
    }

    private static IEnumerable<DiagnosticDescriptor> DriftDescriptors() {
        foreach (DiagnosticDescriptor descriptor in AllSourceToolsDescriptors()) {
            if (descriptor.Id.StartsWith("HFC", StringComparison.Ordinal)
                && int.TryParse(descriptor.Id.AsSpan("HFC".Length), NumberStyles.None, CultureInfo.InvariantCulture, out int n)
                && n >= FirstAllocatedDriftId
                && n <= LastReservedDriftId) {
                yield return descriptor;
            }
        }
    }

    private static IEnumerable<DiagnosticDescriptor> AllSourceToolsDescriptors() {
        foreach (FieldInfo field in typeof(DiagnosticDescriptors).GetFields(BindingFlags.Public | BindingFlags.Static)) {
            if (field.FieldType == typeof(DiagnosticDescriptor)
                && field.GetValue(null) is DiagnosticDescriptor descriptor) {
                yield return descriptor;
            }
        }
    }

    private static string LocateAnalyzerReleasesUnshipped([CallerFilePath] string callerPath = "") {
        // CM-1 — anchor at the test source location via [CallerFilePath] and walk up to repo root.
        string? dir = Path.GetDirectoryName(callerPath);
        while (dir is not null) {
            string candidate = Path.Combine(dir, "src", "Hexalith.FrontComposer.SourceTools", "AnalyzerReleases.Unshipped.md");
            if (File.Exists(candidate)) {
                return candidate;
            }

            dir = Path.GetDirectoryName(dir);
        }

        throw new FileNotFoundException("AnalyzerReleases.Unshipped.md not found from caller path: " + callerPath);
    }
}
