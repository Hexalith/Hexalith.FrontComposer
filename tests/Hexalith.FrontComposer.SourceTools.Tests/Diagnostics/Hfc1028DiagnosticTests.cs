using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Diagnostics;

/// <summary>
/// Story 4-4 T9.5 / D15 — generator-level assertions for the HFC1028 [ColumnPriority]
/// collision diagnostic. Per-projection-per-colliding-priority dedupe + Information
/// severity + silent-when-no-collision contract.
/// </summary>
public class Hfc1028DiagnosticTests {
    private const string SinglePriorityCollisionSource = @"
namespace TestDomain
{
    using Hexalith.FrontComposer.Contracts.Attributes;

    [Projection(""Orders"")]
    public partial class OrderProjection
    {
        [ColumnPriority(1)]
        public string Name { get; set; } = string.Empty;

        [ColumnPriority(1)]
        public int Count { get; set; }

        public string Notes { get; set; } = string.Empty;
    }
}
";

    private const string MultipleDistinctCollisionsSource = @"
namespace TestDomain
{
    using Hexalith.FrontComposer.Contracts.Attributes;

    [Projection(""Orders"")]
    public partial class OrderProjection
    {
        [ColumnPriority(1)]
        public string Name { get; set; } = string.Empty;

        [ColumnPriority(1)]
        public int Count { get; set; }

        [ColumnPriority(5)]
        public string Alpha { get; set; } = string.Empty;

        [ColumnPriority(5)]
        public string Beta { get; set; } = string.Empty;
    }
}
";

    private const string NoCollisionSource = @"
namespace TestDomain
{
    using Hexalith.FrontComposer.Contracts.Attributes;

    [Projection(""Orders"")]
    public partial class OrderProjection
    {
        [ColumnPriority(1)]
        public string Name { get; set; } = string.Empty;

        [ColumnPriority(2)]
        public int Count { get; set; }

        public string Notes { get; set; } = string.Empty;
    }
}
";

    private const string UnannotatedCollisionsSource = @"
namespace TestDomain
{
    using Hexalith.FrontComposer.Contracts.Attributes;

    [Projection(""Orders"")]
    public partial class OrderProjection
    {
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
        public string Notes { get; set; } = string.Empty;
    }
}
";

    [Fact]
    public void FiresOnce_WhenTwoColumnsSharePriority_PayloadIncludesPropertyNamesAndPriority() {
        IReadOnlyList<Diagnostic> diagnostics = RunGenerator(SinglePriorityCollisionSource);

        Diagnostic[] hfc1028 = [.. diagnostics.Where(d => d.Id == "HFC1028")];
        hfc1028.Length.ShouldBe(1);
        hfc1028[0].Severity.ShouldBe(DiagnosticSeverity.Info);

        string message = hfc1028[0].GetMessage();
        message.ShouldContain("OrderProjection");
        message.ShouldContain("Name");
        message.ShouldContain("Count");
        message.ShouldContain("1", Case.Sensitive);
        message.ShouldContain("declaration order");
    }

    [Fact]
    public void FiresOncePerCollidingPriorityValue_WhenMultipleDistinctPrioritiesCollide() {
        // D15 / D22 — one diagnostic per colliding priority value per projection.
        IReadOnlyList<Diagnostic> diagnostics = RunGenerator(MultipleDistinctCollisionsSource);

        Diagnostic[] hfc1028 = [.. diagnostics.Where(d => d.Id == "HFC1028")];
        hfc1028.Length.ShouldBe(2);

        string allMessages = string.Join("\n", hfc1028.Select(d => d.GetMessage()));
        allMessages.ShouldContain("Name");
        allMessages.ShouldContain("Count");
        allMessages.ShouldContain("Alpha");
        allMessages.ShouldContain("Beta");
    }

    [Fact]
    public void DoesNotFire_WhenPrioritiesAreDistinctOrUnannotated() {
        IReadOnlyList<Diagnostic> noCollision = RunGenerator(NoCollisionSource);
        noCollision.Any(d => d.Id == "HFC1028").ShouldBeFalse();

        IReadOnlyList<Diagnostic> unannotated = RunGenerator(UnannotatedCollisionsSource);
        unannotated.Any(d => d.Id == "HFC1028").ShouldBeFalse();
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
