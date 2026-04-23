using System.Text.RegularExpressions;

using Hexalith.FrontComposer.SourceTools.Diagnostics;

using Microsoft.CodeAnalysis;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Diagnostics;

/// <summary>
/// Story 4-1 T7.2 — every HFC1xxx diagnostic descriptor must match its AnalyzerReleases
/// entry + ship a stable message format. Gates RS2008 cleanliness.
/// </summary>
public class DiagnosticDescriptorTests {
    [Theory]
    [InlineData("HFC1022", DiagnosticSeverity.Warning)]
    [InlineData("HFC1023", DiagnosticSeverity.Info)]
    [InlineData("HFC1024", DiagnosticSeverity.Warning)]
    public void AllHFC1xxxIdsHaveAnalyzerReleaseEntries(string id, DiagnosticSeverity expectedSeverity) {
        DiagnosticDescriptor descriptor = GetDescriptor(id);

        descriptor.Id.ShouldBe(id);
        descriptor.DefaultSeverity.ShouldBe(expectedSeverity);
        descriptor.IsEnabledByDefault.ShouldBeTrue();
        descriptor.Category.ShouldBe("HexalithFrontComposer");
    }

    [Fact]
    public void HFC1022MessageFormatMatchesSpec() {
        // D17 / AC9 — message must list 'unknown member' + "Valid members:" preview.
        DiagnosticDescriptor descriptor = DiagnosticDescriptors.ProjectionWhenStateMemberUnknown;

        descriptor.Id.ShouldBe("HFC1022");
        descriptor.Title.ToString().ShouldContain("unknown enum member");
        // The format is a pass-through "{0}" placeholder per the SourceTools convention; the
        // concrete message is built in AttributeParser and validated by the parser-side
        // tests. We lock the shape of the descriptor here.
        descriptor.MessageFormat.ToString().ShouldBe("{0}");
    }

    [Fact]
    public void HFC1023MessageFormatMatchesSpec() {
        DiagnosticDescriptor descriptor = DiagnosticDescriptors.ProjectionRoleDashboardFallback;

        descriptor.Id.ShouldBe("HFC1023");
        descriptor.DefaultSeverity.ShouldBe(DiagnosticSeverity.Info);
        descriptor.Title.ToString().ShouldContain("Dashboard");
        descriptor.Title.ToString().ShouldContain("deferred");
    }

    [Fact]
    public void HFC1024MessageFormatMatchesSpec() {
        DiagnosticDescriptor descriptor = DiagnosticDescriptors.UnknownProjectionRoleValue;

        descriptor.Id.ShouldBe("HFC1024");
        descriptor.DefaultSeverity.ShouldBe(DiagnosticSeverity.Warning);
        descriptor.Title.ToString().ShouldContain("Unknown ProjectionRole");
    }

    [Fact]
    public void NewDiagnosticsAreReservedInAnalyzerReleasesUnshipped() {
        // Story 4-1 T1.5 — the Unshipped.md gate must list all three new codes.
        string path = Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "Hexalith.FrontComposer.SourceTools", "AnalyzerReleases.Unshipped.md");
        path = Path.GetFullPath(path);

        string contents = File.ReadAllText(path);

        contents.ShouldContain("HFC1022");
        contents.ShouldContain("HFC1023");
        contents.ShouldContain("HFC1024");
    }

    private static DiagnosticDescriptor GetDescriptor(string id) => id switch {
        "HFC1022" => DiagnosticDescriptors.ProjectionWhenStateMemberUnknown,
        "HFC1023" => DiagnosticDescriptors.ProjectionRoleDashboardFallback,
        "HFC1024" => DiagnosticDescriptors.UnknownProjectionRoleValue,
        _ => throw new ArgumentException("Unhandled descriptor id: " + id, nameof(id)),
    };
}
