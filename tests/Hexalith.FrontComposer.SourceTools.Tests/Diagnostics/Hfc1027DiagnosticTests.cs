using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Diagnostics;

/// <summary>
/// Story 4-3 T9.5 / D14 / D20 — generator-level assertions for the HFC1027
/// collection-column-not-filterable diagnostic. Per-projection dedupe + Information severity +
/// silent-when-no-collection-columns contract.
/// </summary>
public class Hfc1027DiagnosticTests {
    private const string ProjectionWithCollectionSource = @"
namespace TestDomain
{
    using System.Collections.Generic;
    using Hexalith.FrontComposer.Contracts.Attributes;

    [Projection(""Orders"")]
    public partial class OrderProjection
    {
        public string Name { get; set; } = string.Empty;
        public IReadOnlyList<string> Tags { get; set; } = new List<string>();
    }
}
";

    private const string ProjectionWithMultipleCollectionsSource = @"
namespace TestDomain
{
    using System.Collections.Generic;
    using Hexalith.FrontComposer.Contracts.Attributes;

    [Projection(""Orders"")]
    public partial class OrderProjection
    {
        public string Name { get; set; } = string.Empty;
        public IReadOnlyList<string> Tags { get; set; } = new List<string>();
        public List<int> Ids { get; set; } = new List<int>();
    }
}
";

    private const string ProjectionWithoutCollectionSource = @"
namespace TestDomain
{
    using Hexalith.FrontComposer.Contracts.Attributes;

    [Projection(""Orders"")]
    public partial class OrderProjection
    {
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
";

    [Fact]
    public void FiresOnce_WhenProjectionHasCollectionColumn() {
        IReadOnlyList<Diagnostic> diagnostics = RunGenerator(ProjectionWithCollectionSource);

        Diagnostic[] hfc1027 = [.. diagnostics.Where(d => d.Id == "HFC1027")];
        hfc1027.Length.ShouldBe(1);
        hfc1027[0].Severity.ShouldBe(DiagnosticSeverity.Info);
    }

    [Fact]
    public void PayloadNamesEveryCollectionColumn() {
        IReadOnlyList<Diagnostic> diagnostics = RunGenerator(ProjectionWithMultipleCollectionsSource);

        Diagnostic[] hfc1027 = [.. diagnostics.Where(d => d.Id == "HFC1027")];
        hfc1027.Length.ShouldBe(1);
        string message = hfc1027[0].GetMessage();
        message.ShouldContain("OrderProjection");
        message.ShouldContain("Tags");
        message.ShouldContain("Ids");
    }

    [Fact]
    public void DedupesPerProjection_EvenWithManyCollectionColumns() {
        // D20 — one diagnostic per projection regardless of how many Collection columns.
        IReadOnlyList<Diagnostic> diagnostics = RunGenerator(ProjectionWithMultipleCollectionsSource);

        diagnostics.Count(d => d.Id == "HFC1027").ShouldBe(1);
    }

    [Fact]
    public void DoesNotFire_WhenProjectionHasNoCollectionColumn() {
        IReadOnlyList<Diagnostic> diagnostics = RunGenerator(ProjectionWithoutCollectionSource);

        diagnostics.Any(d => d.Id == "HFC1027").ShouldBeFalse();
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
