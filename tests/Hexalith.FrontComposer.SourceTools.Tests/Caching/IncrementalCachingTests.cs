using Hexalith.FrontComposer.SourceTools.Tests.Parsing.TestFixtures;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Caching;

public class IncrementalCachingTests {
    [Fact]
    public void UnrelatedFileEdit_GeneratorStep_ShowsCached() {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(TestSources.BasicProjection);
        FrontComposerGenerator generator = new();

        // Run 1: initial generation
        GeneratorDriver driver1 = CSharpGeneratorDriver.Create(
            [generator.AsSourceGenerator()],
            driverOptions: new GeneratorDriverOptions(
                disabledOutputs: IncrementalGeneratorOutputKind.None,
                trackIncrementalGeneratorSteps: true));
        driver1 = driver1.RunGenerators(compilation, ct);

        // Run 2: add unrelated file -- expect Cached
        SyntaxTree newTree = CSharpSyntaxTree.ParseText(cancellationToken: ct, text: "public class Unrelated { }");
        CSharpCompilation compilation2 = compilation.AddSyntaxTrees(newTree);
        GeneratorDriver driver2 = driver1.RunGenerators(compilation2, ct);

        GeneratorRunResult result2 = driver2.GetRunResult().Results[0];

        result2.TrackedSteps.ContainsKey("Parse").ShouldBeTrue("The parse stage should be tracked explicitly.");

        bool hasCachedOrUnchangedOutput = result2.TrackedSteps["Parse"]
            .SelectMany(s => s.Outputs)
            .Any(o => o.Reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged);

        hasCachedOrUnchangedOutput.ShouldBeTrue(
            "The parse stage should show Cached/Unchanged outputs for unrelated file changes");
    }

    [Fact]
    public void AttributeArgumentChange_GeneratorStep_ShowsModified() {
        CancellationToken ct = TestContext.Current.CancellationToken;
        string source1 = @"
using Hexalith.FrontComposer.Contracts.Attributes;

namespace TestDomain;

[BoundedContext(""Counter"")]
[Projection]
public partial class TestProjection
{
    public string Name { get; set; } = string.Empty;
}";
        string source2 = @"
using Hexalith.FrontComposer.Contracts.Attributes;

namespace TestDomain;

[BoundedContext(""Inventory"")]
[Projection]
public partial class TestProjection
{
    public string Name { get; set; } = string.Empty;
}";

        CSharpCompilation compilation1 = CompilationHelper.CreateCompilation(source1);
        FrontComposerGenerator generator = new();

        GeneratorDriver driver1 = CSharpGeneratorDriver.Create(
            [generator.AsSourceGenerator()],
            driverOptions: new GeneratorDriverOptions(
                disabledOutputs: IncrementalGeneratorOutputKind.None,
                trackIncrementalGeneratorSteps: true));
        driver1 = driver1.RunGenerators(compilation1, ct);

        // Replace the source tree with modified attribute argument
        CSharpCompilation compilation2 = compilation1.ReplaceSyntaxTree(
            compilation1.SyntaxTrees.First(),
            CSharpSyntaxTree.ParseText(cancellationToken: ct, text: source2));
        GeneratorDriver driver2 = driver1.RunGenerators(compilation2, ct);

        GeneratorRunResult result2 = driver2.GetRunResult().Results[0];

        result2.TrackedSteps.ContainsKey("Parse").ShouldBeTrue("The parse stage should be tracked explicitly.");

        result2.TrackedSteps["Parse"]
            .SelectMany(s => s.Outputs)
            .Any(o => o.Reason == IncrementalStepRunReason.Modified)
            .ShouldBeTrue("Changing attribute arguments should re-run the parse stage");
    }

    [Fact]
    public void AddNewProjectionClass_ExistingCachedOutputsNotInvalidated() {
        CancellationToken ct = TestContext.Current.CancellationToken;
        string existingSource = @"
using Hexalith.FrontComposer.Contracts.Attributes;

namespace TestDomain;

[BoundedContext(""Counter"")]
[Projection]
public partial class ExistingProjection
{
    public string Name { get; set; } = string.Empty;
}";
        string newSource = @"
using Hexalith.FrontComposer.Contracts.Attributes;

namespace TestDomain;

[BoundedContext(""Inventory"")]
[Projection]
public partial class NewProjection
{
    public int Count { get; set; }
}";

        CSharpCompilation compilation1 = CompilationHelper.CreateCompilation(existingSource);
        FrontComposerGenerator generator = new();

        GeneratorDriver driver1 = CSharpGeneratorDriver.Create(
            [generator.AsSourceGenerator()],
            driverOptions: new GeneratorDriverOptions(
                disabledOutputs: IncrementalGeneratorOutputKind.None,
                trackIncrementalGeneratorSteps: true));
        driver1 = driver1.RunGenerators(compilation1, ct);

        // Verify initial run produces 5 generated files
        GeneratorDriverRunResult result1 = driver1.GetRunResult();
        result1.GeneratedTrees.Length.ShouldBe(6, "Initial run should produce 5 files for one projection plus the shared Level 2 template manifest");

        // Add new projection class
        CSharpCompilation compilation2 = compilation1.AddSyntaxTrees(
            CSharpSyntaxTree.ParseText(cancellationToken: ct, text: newSource));
        GeneratorDriver driver2 = driver1.RunGenerators(compilation2, ct);

        GeneratorRunResult result2 = driver2.GetRunResult().Results[0];

        // Should now produce 10 total files
        driver2.GetRunResult().GeneratedTrees.Length.ShouldBe(11, "Two projections should produce 10 files plus the shared Level 2 template manifest");

        result2.TrackedSteps.ContainsKey("Parse").ShouldBeTrue("The parse stage should be tracked explicitly.");

        bool hasNewOutput = result2.TrackedSteps["Parse"]
            .SelectMany(s => s.Outputs)
            .Any(o => o.Reason == IncrementalStepRunReason.New);

        bool hasCachedOutput = result2.TrackedSteps["Parse"]
            .SelectMany(s => s.Outputs)
            .Any(o => o.Reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged);

        hasNewOutput.ShouldBeTrue("Adding a new [Projection] class should produce a new output");
        hasCachedOutput.ShouldBeTrue("Existing [Projection] class should remain cached");
    }

    [Fact]
    public void DeterministicOutput_SameInputProducesByteIdenticalOutput() {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(TestSources.BasicProjection);
        FrontComposerGenerator generator = new();

        GeneratorDriver driver1 = CSharpGeneratorDriver.Create(generator);
        driver1 = driver1.RunGenerators(compilation, ct);
        string[] output1 = driver1.GetRunResult().GeneratedTrees
            .Select(t => t.GetText(ct).ToString())
            .OrderBy(s => s)
            .ToArray();

        GeneratorDriver driver2 = CSharpGeneratorDriver.Create(new FrontComposerGenerator());
        driver2 = driver2.RunGenerators(compilation, ct);
        string[] output2 = driver2.GetRunResult().GeneratedTrees
            .Select(t => t.GetText(ct).ToString())
            .OrderBy(s => s)
            .ToArray();

        output1.Length.ShouldBe(output2.Length);
        for (int i = 0; i < output1.Length; i++) {
            output1[i].ShouldBe(output2[i], "Output should be byte-identical across runs");
        }
    }
}
