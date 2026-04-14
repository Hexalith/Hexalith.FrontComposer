namespace Hexalith.FrontComposer.SourceTools.Tests.Integration;

using System.Linq;
using System.Threading;

using Hexalith.FrontComposer.SourceTools.Tests.Parsing.TestFixtures;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Shouldly;

using Xunit;

public class CounterDomainIntegrationTests
{
    [Fact]
    public void RunGenerators_CounterProjection_Produces5Files()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(TestSources.CounterProjectionSource);
        FrontComposerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation, ct);
        GeneratorDriverRunResult result = driver.GetRunResult();

        result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        result.GeneratedTrees.Length.ShouldBe(5, "Counter projection should produce 5 generated files");

        string[] fileNames = result.GeneratedTrees.Select(t => System.IO.Path.GetFileName(t.FilePath)).ToArray();
        fileNames.ShouldContain("Counter.Domain.CounterProjection.g.razor.cs");
        fileNames.ShouldContain("Counter.Domain.CounterProjectionFeature.g.cs");
        fileNames.ShouldContain("Counter.Domain.CounterProjectionActions.g.cs");
        fileNames.ShouldContain("Counter.Domain.CounterProjectionReducers.g.cs");
        fileNames.ShouldContain("Counter.Domain.CounterProjectionRegistration.g.cs");

        CSharpCompilation outputCompilation = compilation.AddSyntaxTrees(
            result.GeneratedTrees.ToArray());
        outputCompilation.GetDiagnostics(ct)
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ShouldBeEmpty("Counter generated code should compile without errors");
    }

    [Fact]
    public void RunGenerators_CounterRegistration_ContainsManifestAndRegisterDomain()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(TestSources.CounterProjectionSource);
        FrontComposerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation, ct);
        GeneratorDriverRunResult result = driver.GetRunResult();

        SyntaxTree registrationTree = result.GeneratedTrees
            .Single(t => System.IO.Path.GetFileName(t.FilePath).Contains("Registration"));
        string registrationSource = registrationTree.GetText(ct).ToString();

        registrationSource.ShouldContain("DomainManifest Manifest");
        registrationSource.ShouldContain("RegisterDomain(IFrontComposerRegistry registry)");
        registrationSource.ShouldContain("\"Counter\"");
        registrationSource.ShouldContain("CounterProjectionRegistration");
    }

    [Fact]
    public void RunGenerators_CounterProjection_GeneratesCorrectColumns()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(TestSources.CounterProjectionSource);
        FrontComposerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation, ct);
        GeneratorDriverRunResult result = driver.GetRunResult();

        SyntaxTree razorTree = result.GeneratedTrees
            .Single(t => System.IO.Path.GetFileName(t.FilePath).EndsWith(".g.razor.cs"));
        string razorSource = razorTree.GetText(ct).ToString();

        // Verify 3 columns: Id (text), Count (numeric N0), LastUpdated (datetime "d")
        razorSource.ShouldContain("Text column: Id");
        razorSource.ShouldContain("Numeric column: Count");
        razorSource.ShouldContain("DateTime column: LastUpdated");
    }
}
