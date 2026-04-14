namespace Hexalith.FrontComposer.SourceTools.Tests.Integration;

using System.Linq;
using System.Threading;

using Hexalith.FrontComposer.SourceTools.Tests.Parsing.TestFixtures;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Shouldly;

using Xunit;

public class GeneratorDriverTests
{
    [Fact]
    public void RunGenerators_BasicProjection_Produces5Files()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(TestSources.BasicProjection);
        FrontComposerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation, ct);
        GeneratorDriverRunResult result = driver.GetRunResult();

        result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        result.GeneratedTrees.Length.ShouldBe(5, "Generator should produce 5 files per projection");

        // Verify file names
        string[] fileNames = result.GeneratedTrees.Select(t => System.IO.Path.GetFileName(t.FilePath)).ToArray();
        fileNames.ShouldContain("CounterProjection.g.razor.cs");
        fileNames.ShouldContain("CounterProjectionFeature.g.cs");
        fileNames.ShouldContain("CounterProjectionActions.g.cs");
        fileNames.ShouldContain("CounterProjectionReducers.g.cs");
        fileNames.ShouldContain("CounterProjectionRegistration.g.cs");

        // Verify generated code compiles
        CSharpCompilation outputCompilation = compilation.AddSyntaxTrees(
            result.GeneratedTrees.ToArray());
        outputCompilation.GetDiagnostics(ct)
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ShouldBeEmpty("Generated code should compile without errors");
    }

    [Fact]
    public void RunGenerators_AllFieldTypesProjection_GeneratedCodeCompiles()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(TestSources.AllFieldTypesProjection);
        FrontComposerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation, ct);
        GeneratorDriverRunResult result = driver.GetRunResult();

        result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        result.GeneratedTrees.Length.ShouldBe(5);

        CSharpCompilation outputCompilation = compilation.AddSyntaxTrees(
            result.GeneratedTrees.ToArray());
        outputCompilation.GetDiagnostics(ct)
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ShouldBeEmpty("Generated code should compile without errors");
    }

    [Fact]
    public void RunGenerators_UnsupportedFieldProjection_GeneratedCodeCompiles()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(TestSources.UnsupportedFieldProjection);
        FrontComposerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation, ct);
        GeneratorDriverRunResult result = driver.GetRunResult();

        result.Diagnostics.Where(d => d.Id == "HFC1002").ShouldNotBeEmpty();

        CSharpCompilation outputCompilation = compilation.AddSyntaxTrees(
            result.GeneratedTrees.ToArray());
        outputCompilation.GetDiagnostics(ct)
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ShouldBeEmpty("Generated code should compile even with unsupported field warnings");
    }

    [Fact]
    public void RunGenerators_MultipleProjectionTypes_AllProcessed()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        string[] sources =
        [
            TestSources.BasicProjection,
            TestSources.MultiAttributeProjection,
        ];
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(sources);
        FrontComposerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation, ct);
        GeneratorDriverRunResult result = driver.GetRunResult();

        result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        result.GeneratedTrees.Length.ShouldBe(10, "Two projections should produce 10 files total");
    }

    [Fact]
    public void RunGenerators_NoAnnotatedTypes_ReportsHfc1001()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        string source = @"
namespace TestDomain;

public class NotAProjection
{
    public string Name { get; set; } = string.Empty;
}";
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source);
        FrontComposerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation, ct);
        GeneratorDriverRunResult result = driver.GetRunResult();

        result.GeneratedTrees.ShouldBeEmpty();
        result.Diagnostics.Count(d => d.Id == "HFC1001").ShouldBe(1);
    }

    [Fact]
    public void RunGenerators_CommandOnlyCompilation_DoesNotReportHfc1001()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        string source = @"
using Hexalith.FrontComposer.Contracts.Attributes;

namespace TestDomain;

[Command]
public partial class SubmitOrderCommand
{
    public string Name { get; set; } = string.Empty;
}";
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source);
        FrontComposerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation, ct);
        GeneratorDriverRunResult result = driver.GetRunResult();

        result.GeneratedTrees.ShouldBeEmpty();
        result.Diagnostics.Where(d => d.Id == "HFC1001").ShouldBeEmpty();
    }

    [Fact]
    public void RunGenerators_BadgeMappingProjection_GeneratedCodeCompiles()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(TestSources.BadgeMappingProjection);
        FrontComposerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation, ct);
        GeneratorDriverRunResult result = driver.GetRunResult();

        result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();

        CSharpCompilation outputCompilation = compilation.AddSyntaxTrees(
            result.GeneratedTrees.ToArray());
        outputCompilation.GetDiagnostics(ct)
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ShouldBeEmpty("Generated code should compile without errors");
    }

    [Fact]
    public void RunGenerators_BoundedContextGrouping_NoHintNameCrash()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        string[] sources =
        [
            @"
using Hexalith.FrontComposer.Contracts.Attributes;

namespace TestDomain;

[BoundedContext(""Orders"")]
[Projection]
public partial class OrderProjection
{
    public string Name { get; set; } = string.Empty;
}",
            @"
using Hexalith.FrontComposer.Contracts.Attributes;

namespace TestDomain;

[BoundedContext(""Orders"")]
[Projection]
public partial class OrderItemProjection
{
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
}",
        ];
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(sources);
        FrontComposerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation, ct);
        GeneratorDriverRunResult result = driver.GetRunResult();

        result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        result.GeneratedTrees.Length.ShouldBe(10, "Two projections should produce 10 files");

        // Verify both registration files contribute to same partial class
        string[] fileNames = result.GeneratedTrees.Select(t => System.IO.Path.GetFileName(t.FilePath)).ToArray();
        fileNames.ShouldContain("OrderProjectionRegistration.g.cs");
        fileNames.ShouldContain("OrderItemProjectionRegistration.g.cs");

        // Verify all generated code compiles together (partial classes merge)
        CSharpCompilation outputCompilation = compilation.AddSyntaxTrees(
            result.GeneratedTrees.ToArray());
        outputCompilation.GetDiagnostics(ct)
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ShouldBeEmpty("Two projections sharing BoundedContext should compile together");
    }

    [Fact]
    public void RunGenerators_OnlyUnsupportedFields_StillGeneratesCode()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        string source = @"
using System;
using System.Collections.Generic;
using Hexalith.FrontComposer.Contracts.Attributes;

namespace TestDomain;

[BoundedContext(""Test"")]
[Projection]
public partial class AllUnsupportedProjection
{
    public byte[] ByteArray { get; set; } = Array.Empty<byte>();
    public Dictionary<string, int> Dict { get; set; } = new();
}";
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source);
        FrontComposerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation, ct);
        GeneratorDriverRunResult result = driver.GetRunResult();

        // Should still produce 5 files (component with zero columns, feature, actions, reducers, registration)
        result.GeneratedTrees.Length.ShouldBe(5, "Even with all unsupported fields, should generate 5 files");

        CSharpCompilation outputCompilation = compilation.AddSyntaxTrees(
            result.GeneratedTrees.ToArray());
        outputCompilation.GetDiagnostics(ct)
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ShouldBeEmpty("Generated code with zero columns should still compile");
    }
}
