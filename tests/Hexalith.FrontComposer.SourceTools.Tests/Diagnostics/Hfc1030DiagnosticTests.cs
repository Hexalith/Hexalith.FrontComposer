using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Diagnostics;

/// <summary>
/// Story 4-5 T6.4 / D16 — generator diagnostics for [ProjectionFieldGroup] names that
/// collide with the reserved catch-all "Additional details" label.
/// </summary>
public sealed class Hfc1030DiagnosticTests {
    private const string CollisionSource = @"
namespace TestDomain
{
    using Hexalith.FrontComposer.Contracts.Attributes;

    [Projection(""Orders"")]
    public partial class OrderProjection
    {
        public string Id { get; set; } = string.Empty;

        [ProjectionFieldGroup(""Additional details"")]
        public string Notes { get; set; } = string.Empty;
    }
}
";

    private const string CaseInsensitiveCollisionSource = @"
namespace TestDomain
{
    using Hexalith.FrontComposer.Contracts.Attributes;

    [Projection(""Orders"")]
    public partial class OrderProjection
    {
        [ProjectionFieldGroup(""additional DETAILS"")]
        public string Notes { get; set; } = string.Empty;

        [ProjectionFieldGroup(""Additional details"")]
        public string Reference { get; set; } = string.Empty;
    }
}
";

    private const string NonCollisionSource = @"
namespace TestDomain
{
    using Hexalith.FrontComposer.Contracts.Attributes;

    [Projection(""Orders"")]
    public partial class OrderProjection
    {
        [ProjectionFieldGroup(""Shipping"")]
        public string Notes { get; set; } = string.Empty;
    }
}
";

    [Fact]
    public void FiresOnce_WhenGroupNameCollidesWithReservedCatchAllLabel() {
        IReadOnlyList<Diagnostic> diagnostics = RunGenerator(CollisionSource);

        Diagnostic[] hfc1030 = [.. diagnostics.Where(d => d.Id == "HFC1030")];
        hfc1030.Length.ShouldBe(1);
        hfc1030[0].Severity.ShouldBe(DiagnosticSeverity.Info);

        string message = hfc1030[0].GetMessage();
        message.ShouldContain("OrderProjection");
        message.ShouldContain("Additional details");
        message.ShouldContain("reserved catch-all");
    }

    [Fact]
    public void DedupesPerProjection_WhenMultiplePropertiesCollideCaseInsensitively() {
        IReadOnlyList<Diagnostic> diagnostics = RunGenerator(CaseInsensitiveCollisionSource);

        Diagnostic[] hfc1030 = [.. diagnostics.Where(d => d.Id == "HFC1030")];
        hfc1030.Length.ShouldBe(1);
        hfc1030[0].GetMessage().ShouldContain("additional DETAILS");
        hfc1030[0].GetMessage().ShouldContain("Additional details");
    }

    [Fact]
    public void DoesNotFire_WhenGroupNameDoesNotCollide() {
        IReadOnlyList<Diagnostic> diagnostics = RunGenerator(NonCollisionSource);

        diagnostics.Any(d => d.Id == "HFC1030").ShouldBeFalse();
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
