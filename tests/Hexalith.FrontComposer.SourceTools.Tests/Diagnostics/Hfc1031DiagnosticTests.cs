using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Diagnostics;

/// <summary>
/// Story 4-5 T6.3 / D17 — [ProjectionFieldGroup] is informational-only when the
/// projection role has no detail surface.
/// </summary>
public sealed class Hfc1031DiagnosticTests {
    [Fact]
    public void FiresOnce_WhenTimelineProjectionDeclaresAnyFieldGroup() {
        IReadOnlyList<Diagnostic> diagnostics = RunGenerator(BuildSource("Timeline", groupedColumns: 2));

        Diagnostic[] hfc1031 = [.. diagnostics.Where(d => d.Id == "HFC1031")];
        hfc1031.Length.ShouldBe(1);
        hfc1031[0].Severity.ShouldBe(DiagnosticSeverity.Info);

        string message = hfc1031[0].GetMessage();
        message.ShouldContain("OrderProjection");
        message.ShouldContain("Timeline");
        message.ShouldContain("2 group annotation");
        message.ShouldContain("no detail surface");
    }

    [Theory]
    [InlineData("DetailRecord")]
    [InlineData("ActionQueue")]
    [InlineData("StatusOverview")]
    [InlineData("Dashboard")]
    public void DoesNotFire_ForExplicitRolesWithDetailSurface(string role) {
        IReadOnlyList<Diagnostic> diagnostics = RunGenerator(BuildSource(role, groupedColumns: 1));

        diagnostics.Any(d => d.Id == "HFC1031").ShouldBeFalse();
    }

    [Fact]
    public void DoesNotFire_ForDefaultGridProjectionWithDetailSurface() {
        IReadOnlyList<Diagnostic> diagnostics = RunGenerator(BuildSource(role: null, groupedColumns: 1));

        diagnostics.Any(d => d.Id == "HFC1031").ShouldBeFalse();
    }

    [Fact]
    public void DoesNotFire_WhenTimelineProjectionHasNoFieldGroups() {
        IReadOnlyList<Diagnostic> diagnostics = RunGenerator(BuildSource("Timeline", groupedColumns: 0));

        diagnostics.Any(d => d.Id == "HFC1031").ShouldBeFalse();
    }

    private static string BuildSource(string? role, int groupedColumns)
        => @"
namespace TestDomain
{
    using Hexalith.FrontComposer.Contracts.Attributes;

    [Projection(""Orders"")]
" + (role is null ? string.Empty : "    [ProjectionRole(ProjectionRole." + role + @")]
") + @"    public partial class OrderProjection
    {
        public string Id { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
" + BuildGroupedProperties(groupedColumns) + @"
    }
}
";

    private static string BuildGroupedProperties(int groupedColumns) {
        if (groupedColumns <= 0) {
            return "        public string Notes { get; set; } = string.Empty;";
        }

        return string.Join(
            Environment.NewLine,
            Enumerable.Range(1, groupedColumns).Select(i =>
                "        [ProjectionFieldGroup(\"Group " + i.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\")]" + Environment.NewLine +
                "        public string Notes" + i.ToString(System.Globalization.CultureInfo.InvariantCulture) + " { get; set; } = string.Empty;"));
    }

    private static IReadOnlyList<Diagnostic> RunGenerator(string source) {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source);
        FrontComposerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGenerators(compilation, ct);
        return driver.GetRunResult().Diagnostics;
    }
}
