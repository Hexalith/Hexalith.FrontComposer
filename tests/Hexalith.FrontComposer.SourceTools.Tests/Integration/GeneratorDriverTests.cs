using Hexalith.FrontComposer.SourceTools.Tests.Parsing.TestFixtures;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Integration;

public class GeneratorDriverTests {
    [Fact]
    public void RunGenerators_BasicProjection_Produces6Files() {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(TestSources.BasicProjection);
        FrontComposerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation, ct);
        GeneratorDriverRunResult result = driver.GetRunResult();

        result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        // Story 6-2 emits one Level 2 template manifest; Story 8-1 adds one MCP manifest.
        result.GeneratedTrees.Length.ShouldBe(7, "Generator should produce 5 files per projection plus shared Level 2 and MCP manifests");

        // Verify file names (namespace-qualified hint names for collision safety)
        string[] fileNames = result.GeneratedTrees.Select(t => System.IO.Path.GetFileName(t.FilePath)).ToArray();
        fileNames.ShouldContain("TestDomain.CounterProjection.g.razor.cs");
        fileNames.ShouldContain("TestDomain.CounterProjectionFeature.g.cs");
        fileNames.ShouldContain("TestDomain.CounterProjectionActions.g.cs");
        fileNames.ShouldContain("TestDomain.CounterProjectionReducers.g.cs");
        fileNames.ShouldContain("TestDomain.CounterProjectionRegistration.g.cs");
        fileNames.ShouldContain("__FrontComposerProjectionTemplatesRegistration.g.cs");
        fileNames.ShouldContain("FrontComposerMcpManifest.g.cs");

        // Verify generated code compiles
        CSharpCompilation outputCompilation = compilation.AddSyntaxTrees(
            result.GeneratedTrees.ToArray());
        outputCompilation.GetDiagnostics(ct)
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ShouldBeEmpty("Generated code should compile without errors");
    }

    [Fact]
    public void RunGenerators_AllFieldTypesProjection_GeneratedCodeCompiles() {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(TestSources.AllFieldTypesProjection);
        FrontComposerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation, ct);
        GeneratorDriverRunResult result = driver.GetRunResult();

        result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        result.GeneratedTrees.Length.ShouldBe(7);

        CSharpCompilation outputCompilation = compilation.AddSyntaxTrees(
            result.GeneratedTrees.ToArray());
        outputCompilation.GetDiagnostics(ct)
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ShouldBeEmpty("Generated code should compile without errors");
    }

    [Fact]
    public void RunGenerators_UnsupportedFieldProjection_GeneratedCodeCompiles() {
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
    public void RunGenerators_MultipleProjectionTypes_AllProcessed() {
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
        result.GeneratedTrees.Length.ShouldBe(12, "Two projections should produce 10 files plus shared Level 2 and MCP manifests");
    }

    [Fact]
    public void RunGenerators_NoAnnotatedTypes_ReportsHfc1001() {
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

        // Story 6-2 T3 — even when no projections/commands exist, the empty Level 2 manifest is
        // still emitted so registry registration code remains stable for adopter assemblies.
        result.GeneratedTrees.Length.ShouldBe(1);
        result.GeneratedTrees[0].FilePath.ShouldEndWith("__FrontComposerProjectionTemplatesRegistration.g.cs");
        result.Diagnostics.Count(d => d.Id == "HFC1001").ShouldBe(1);
    }

    [Fact]
    public void RunGenerators_CommandOnlyCompilation_DoesNotReportHfc1001() {
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

        result.Diagnostics.Where(d => d.Id == "HFC1001").ShouldBeEmpty();
        // Story 2-3 expanded command-pipeline emission: form, actions, lifecycle feature, registration,
        // density-driven renderer, LastUsed subscriber, and per-command lifecycle bridge — 7 trees for
        // non-FullPage densities. Story 6-2 adds the Level 2 template manifest; Story 8-1 adds MCP.
        result.GeneratedTrees.Length.ShouldBe(9, "Command-only compilations should emit command artifacts plus shared Level 2 and MCP manifests");

        CSharpCompilation outputCompilation = compilation.AddSyntaxTrees(
            result.GeneratedTrees.ToArray());
        outputCompilation.GetDiagnostics(ct)
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ShouldBeEmpty("Generated command-only code should compile without errors");
    }

    [Fact]
    public void RunGenerators_BadgeMappingProjection_GeneratedCodeCompiles() {
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
    public void RunGenerators_BoundedContextGrouping_NoHintNameCrash() {
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
        result.GeneratedTrees.Length.ShouldBe(12, "Two projections should produce 10 files plus shared Level 2 and MCP manifests");

        // Verify both registration files contribute to same partial class (namespace-qualified hint names)
        string[] fileNames = result.GeneratedTrees.Select(t => System.IO.Path.GetFileName(t.FilePath)).ToArray();
        fileNames.ShouldContain("TestDomain.OrderProjectionRegistration.g.cs");
        fileNames.ShouldContain("TestDomain.OrderItemProjectionRegistration.g.cs");

        // Verify all generated code compiles together (partial classes merge)
        CSharpCompilation outputCompilation = compilation.AddSyntaxTrees(
            result.GeneratedTrees.ToArray());
        outputCompilation.GetDiagnostics(ct)
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ShouldBeEmpty("Two projections sharing BoundedContext should compile together");
    }

    [Fact]
    public void RunGenerators_DisplayLabel_PropagatedToRegistration() {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(TestSources.DisplayLabelProjection);
        FrontComposerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation, ct);
        GeneratorDriverRunResult result = driver.GetRunResult();

        result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();

        // Find the projection-registration file (excluding the Story 6-2 Level 2 template manifest)
        // and verify it contains the display label.
        SyntaxTree registrationTree = result.GeneratedTrees
            .Single(t => System.IO.Path.GetFileName(t.FilePath).EndsWith("ProjectionRegistration.g.cs", StringComparison.Ordinal));
        string registrationSource = registrationTree.GetText(ct).ToString();
        registrationSource.ShouldContain("Commandes");

        // Verify all generated code compiles
        CSharpCompilation outputCompilation = compilation.AddSyntaxTrees(
            result.GeneratedTrees.ToArray());
        outputCompilation.GetDiagnostics(ct)
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ShouldBeEmpty("Generated code with DisplayLabel should compile without errors");
    }

    [Fact]
    public void RunGenerators_SameNameDifferentNamespace_NoHintNameCollision() {
        CancellationToken ct = TestContext.Current.CancellationToken;
        string[] sources =
        [
            @"
using Hexalith.FrontComposer.Contracts.Attributes;

namespace NamespaceA;

[BoundedContext(""DomainA"")]
[Projection]
public partial class SharedProjection
{
    public string Name { get; set; } = string.Empty;
}",
            @"
using Hexalith.FrontComposer.Contracts.Attributes;

namespace NamespaceB;

[BoundedContext(""DomainB"")]
[Projection]
public partial class SharedProjection
{
    public int Count { get; set; }
}",
        ];
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(sources);
        FrontComposerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation, ct);
        GeneratorDriverRunResult result = driver.GetRunResult();

        result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        result.GeneratedTrees.Length.ShouldBe(12, "Two same-named projections in different namespaces should produce 10 files plus shared Level 2 and MCP manifests");

        // Verify namespace-qualified hint names prevent collision
        string[] fileNames = result.GeneratedTrees.Select(t => System.IO.Path.GetFileName(t.FilePath)).ToArray();
        fileNames.ShouldContain("NamespaceA.SharedProjection.g.razor.cs");
        fileNames.ShouldContain("NamespaceB.SharedProjection.g.razor.cs");

        // Verify all generated code compiles
        CSharpCompilation outputCompilation = compilation.AddSyntaxTrees(
            result.GeneratedTrees.ToArray());
        outputCompilation.GetDiagnostics(ct)
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ShouldBeEmpty("Same-named projections in different namespaces should compile together");
    }

    [Fact]
    public void RunGenerators_GlobalNamespaceProjection_HintNameHasNoNamespacePrefix() {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(TestSources.GlobalNamespaceProjection);
        FrontComposerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation, ct);
        GeneratorDriverRunResult result = driver.GetRunResult();

        result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        result.GeneratedTrees.Length.ShouldBe(7, "Global namespace projection should produce 5 files plus shared Level 2 and MCP manifests");

        // Verify hint names have no namespace prefix (just TypeName)
        string[] fileNames = result.GeneratedTrees.Select(t => System.IO.Path.GetFileName(t.FilePath)).ToArray();
        fileNames.ShouldContain("GlobalProjection.g.razor.cs");
        fileNames.ShouldContain("GlobalProjectionFeature.g.cs");
        fileNames.ShouldContain("GlobalProjectionActions.g.cs");
        fileNames.ShouldContain("GlobalProjectionReducers.g.cs");
        fileNames.ShouldContain("GlobalProjectionRegistration.g.cs");

        // Verify generated code compiles
        CSharpCompilation outputCompilation = compilation.AddSyntaxTrees(
            result.GeneratedTrees.ToArray());
        outputCompilation.GetDiagnostics(ct)
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ShouldBeEmpty("Global namespace projection should compile without errors");
    }

    [Fact]
    public void RunGenerators_OnlyUnsupportedFields_StillGeneratesCode() {
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
        // plus the shared Level 2 and MCP manifest trees.
        result.GeneratedTrees.Length.ShouldBe(7, "Even with all unsupported fields, should generate 5 files plus shared Level 2 and MCP manifests");

        CSharpCompilation outputCompilation = compilation.AddSyntaxTrees(
            result.GeneratedTrees.ToArray());
        outputCompilation.GetDiagnostics(ct)
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ShouldBeEmpty("Generated code with zero columns should still compile");
    }

    [Fact]
    public void RunGenerators_ZeroFieldCommand_EmitsInlineRendererWithoutPage() {
        VerifyCommandArtifacts(
            CommandTestSources.EmptyCommand,
            "TestDomain.EmptyCommand",
            expectedTreeCount: 9,
            shouldEmitPage: false);
    }

    [Fact]
    public void RunGenerators_OneFieldCommand_EmitsInlineRendererWithoutPage() {
        VerifyCommandArtifacts(
            CommandTestSources.SingleStringFieldCommand,
            "TestDomain.SetNameCommand",
            expectedTreeCount: 9,
            shouldEmitPage: false);
    }

    [Fact]
    public void RunGenerators_TwoFieldCommand_EmitsCompactRendererWithoutPage() {
        VerifyCommandArtifacts(
            TwoFieldCommandSource,
            "TestDomain.TwoFieldCommand",
            expectedTreeCount: 9,
            shouldEmitPage: false);
    }

    [Fact]
    public void RunGenerators_FiveFieldCommand_EmitsFullPageRendererAndPage() {
        VerifyCommandArtifacts(
            CommandTestSources.MultiFieldCommand,
            "TestDomain.PlaceOrderCommand",
            expectedTreeCount: 10,
            shouldEmitPage: true);
    }

    [Fact]
    public void RunGenerators_FiveFieldCommand_PageInfersRestoreViewKeyFromQuery() {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(CommandTestSources.MultiFieldCommand);
        FrontComposerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation, ct);
        GeneratorDriverRunResult result = driver.GetRunResult();

        SyntaxTree pageTree = result.GeneratedTrees
            .Single(t => System.IO.Path.GetFileName(t.FilePath).Contains("CommandPage.g.razor.cs", StringComparison.Ordinal));
        string pageSource = pageTree.GetText(ct).ToString();

        pageSource.ShouldContain("InferReturnViewKeyFromReferrer");
        pageSource.ShouldContain("projectionTypeFqn");
        pageSource.ShouldContain("NavigationManager.ToAbsoluteUri(NavigationManager.Uri)");
        pageSource.ShouldContain("OpenComponent<PageTitle>");
    }

    [Fact]
    public void RunGenerators_RenderModeOverride_HostComponentCompilesAgainstGeneratedRenderer() {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(
            [TwoFieldCommandSource, RenderModeOverrideHostSource]);
        FrontComposerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation, ct);
        GeneratorDriverRunResult result = driver.GetRunResult();

        result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();

        CSharpCompilation outputCompilation = compilation.AddSyntaxTrees(result.GeneratedTrees.ToArray());
        outputCompilation.GetDiagnostics(ct)
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ShouldBeEmpty("A consumer should be able to compile against a generated renderer with RenderMode override.");
    }

    private static void VerifyCommandArtifacts(
        string source,
        string metadataName,
        int expectedTreeCount,
        bool shouldEmitPage) {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source);
        FrontComposerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation, ct);
        GeneratorDriverRunResult result = driver.GetRunResult();

        result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        result.GeneratedTrees.Length.ShouldBe(expectedTreeCount);

        string typeName = metadataName.Split('.').Last();
        string[] fileNames = result.GeneratedTrees.Select(t => System.IO.Path.GetFileName(t.FilePath)).ToArray();
        fileNames.ShouldContain($"TestDomain.{typeName}.CommandRenderer.g.razor.cs");
        fileNames.ShouldContain($"TestDomain.{typeName}.CommandLastUsedSubscriber.g.cs");
        fileNames.ShouldContain($"TestDomain.{typeName}.CommandLifecycleBridge.g.cs");

        if (shouldEmitPage) {
            fileNames.ShouldContain($"TestDomain.{typeName}.CommandPage.g.razor.cs");
        }
        else {
            fileNames.ShouldNotContain($"TestDomain.{typeName}.CommandPage.g.razor.cs");
        }

        CSharpCompilation outputCompilation = compilation.AddSyntaxTrees(result.GeneratedTrees.ToArray());
        outputCompilation.GetDiagnostics(ct)
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ShouldBeEmpty($"Generated command artifacts for {metadataName} should compile without errors.");
    }

    private const string TwoFieldCommandSource = @"
using Hexalith.FrontComposer.Contracts.Attributes;

namespace TestDomain;

[Command]
public class TwoFieldCommand
{
    public string MessageId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Amount { get; set; }
}";

    private const string RenderModeOverrideHostSource = @"
using Hexalith.FrontComposer.Contracts.Rendering;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace TestDomain;

public sealed class RenderModeOverrideHost : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<TwoFieldCommandRenderer>(0);
        builder.AddAttribute(1, ""RenderMode"", CommandRenderMode.FullPage);
        builder.CloseComponent();
    }
}";
}
