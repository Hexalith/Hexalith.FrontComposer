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
    public void RunGenerators_BasicProjection_GeneratedCodeCompiles()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(TestSources.BasicProjection);
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
    public void RunGenerators_AllFieldTypesProjection_GeneratedCodeCompiles()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(TestSources.AllFieldTypesProjection);
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
    }

    [Fact]
    public void RunGenerators_NoProjectionTypes_ProducesNoOutput()
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
        result.Diagnostics.ShouldBeEmpty();
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
}
