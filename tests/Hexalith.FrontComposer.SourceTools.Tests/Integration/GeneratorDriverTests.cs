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
    public void RunGenerators_BoundedContextXmlCharacters_EscapesRegistrationXmlDoc() {
        CancellationToken ct = TestContext.Current.CancellationToken;
        const string source = """
            using Hexalith.FrontComposer.Contracts.Attributes;

            namespace TestDomain;

            [BoundedContext("Orders <North> & \"Priority\"", DisplayLabel = "Priority Orders")]
            [Projection]
            public partial class OrderProjection
            {
                public string Id { get; set; } = string.Empty;
            }
            """;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source);
        FrontComposerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation, ct);
        GeneratorDriverRunResult result = driver.GetRunResult();

        SyntaxTree registrationTree = result.GeneratedTrees
            .Single(t => System.IO.Path.GetFileName(t.FilePath).EndsWith("ProjectionRegistration.g.cs", StringComparison.Ordinal));
        string registrationSource = registrationTree.GetText(ct).ToString();
        registrationSource.ShouldContain("Orders &lt;North&gt; &amp; &quot;Priority&quot;");
        registrationSource.ShouldNotContain("/// Domain registration for <see cref=\"OrderProjection\"/> in the \"Orders <North>");

        CSharpCompilation outputCompilation = compilation.AddSyntaxTrees(result.GeneratedTrees.ToArray());
        outputCompilation.GetDiagnostics(ct)
            .Where(d => d.Severity == DiagnosticSeverity.Error || d.Id == "CS1570")
            .ShouldBeEmpty("Generated registration XML docs must stay escaped and compile cleanly.");
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

    [Theory]
    [InlineData(1, "Inline", false)]
    [InlineData(2, "CompactInline", false)]
    [InlineData(4, "CompactInline", false)]
    [InlineData(5, "FullPage", true)]
    public void RunGenerators_CommandDensityThresholds_EmitExpectedDefaultModeAndOptionalPage(
        int nonDerivableCount,
        string expectedMode,
        bool shouldEmitPage) {
        string typeName = "Density" + nonDerivableCount + "Command";
        string source = BuildCommandSource(typeName, nonDerivableCount);

        GeneratorDriverRunResult result = RunGenerator(source);

        result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        string[] fileNames = result.GeneratedTrees.Select(t => Path.GetFileName(t.FilePath)).ToArray();
        fileNames.ShouldContain($"TestDomain.{typeName}.CommandRenderer.g.razor.cs");
        if (shouldEmitPage) {
            fileNames.ShouldContain($"TestDomain.{typeName}.CommandPage.g.razor.cs");
        }
        else {
            fileNames.ShouldNotContain($"TestDomain.{typeName}.CommandPage.g.razor.cs");
        }

        SyntaxTree rendererTree = result.GeneratedTrees
            .Single(t => Path.GetFileName(t.FilePath) == $"TestDomain.{typeName}.CommandRenderer.g.razor.cs");
        string rendererSource = rendererTree.GetText(TestContext.Current.CancellationToken).ToString();
        rendererSource.ShouldContain("RenderMode ?? CommandRenderMode." + expectedMode);
        rendererSource.ShouldContain("density=" + expectedMode);
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
    public void RunGenerators_DerivableFields_DoNotInflateCompactDensityOrEmitEditableInputs() {
        GeneratorDriverRunResult result = RunGenerator(BuildCommandSource(
            "CompactWithDerivableFieldsCommand",
            nonDerivableCount: 4,
            derivedFromCount: 6,
            includeWellKnownDerivableFields: true));

        result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        string[] fileNames = result.GeneratedTrees.Select(t => Path.GetFileName(t.FilePath)).ToArray();
        fileNames.ShouldContain("TestDomain.CompactWithDerivableFieldsCommand.CommandRenderer.g.razor.cs");
        fileNames.ShouldNotContain("TestDomain.CompactWithDerivableFieldsCommand.CommandPage.g.razor.cs");

        string rendererSource = result.GeneratedTrees
            .Single(t => Path.GetFileName(t.FilePath) == "TestDomain.CompactWithDerivableFieldsCommand.CommandRenderer.g.razor.cs")
            .GetText(TestContext.Current.CancellationToken)
            .ToString();
        rendererSource.ShouldContain("RenderMode ?? CommandRenderMode.CompactInline");
        rendererSource.ShouldContain("TryPrefillPropertyAsync(\"TenantId\")");
        rendererSource.ShouldContain("TryPrefillPropertyAsync(\"Derived0\")");

        string formSource = result.GeneratedTrees
            .Single(t => Path.GetFileName(t.FilePath) == "TestDomain.CompactWithDerivableFieldsCommand.CommandForm.g.razor.cs")
            .GetText(TestContext.Current.CancellationToken)
            .ToString();
        formSource.ShouldContain("// Field: Field0");
        formSource.ShouldContain("// Field: Field3");
        formSource.ShouldNotContain("// Field: TenantId");
        formSource.ShouldNotContain("// Field: Derived0");
        formSource.ShouldNotContain("ResolveLabel(\"TenantId\"");
        formSource.ShouldNotContain("ResolveLabel(\"Derived0\"");
    }

    [Fact]
    public void RunGenerators_DerivableFields_PreserveFullPageDensityWhenFiveEditableFieldsRemain() {
        GeneratorDriverRunResult result = RunGenerator(BuildCommandSource(
            "FullPageWithDerivableFieldsCommand",
            nonDerivableCount: 5,
            derivedFromCount: 6,
            includeWellKnownDerivableFields: true));

        result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        string[] fileNames = result.GeneratedTrees.Select(t => Path.GetFileName(t.FilePath)).ToArray();
        fileNames.ShouldContain("TestDomain.FullPageWithDerivableFieldsCommand.CommandPage.g.razor.cs");

        string rendererSource = result.GeneratedTrees
            .Single(t => Path.GetFileName(t.FilePath) == "TestDomain.FullPageWithDerivableFieldsCommand.CommandRenderer.g.razor.cs")
            .GetText(TestContext.Current.CancellationToken)
            .ToString();
        rendererSource.ShouldContain("RenderMode ?? CommandRenderMode.FullPage");
        rendererSource.ShouldContain("density=FullPage");
    }

    [Fact]
    public void RunGenerators_Hfc1011_UsesTotalPropertyLimitWithoutChangingDensityCount() {
        GeneratorDriverRunResult result = RunGenerator(BuildCommandSource(
            "HugeMostlyDerivableCommand",
            nonDerivableCount: 4,
            derivedFromCount: 201));

        Diagnostic hfc1011 = result.Diagnostics.Single(d => d.Id == "HFC1011");
        hfc1011.Severity.ShouldBe(DiagnosticSeverity.Error);
        result.Diagnostics.ShouldNotContain(d => d.Id == "HFC1007");
        string[] fileNames = result.GeneratedTrees.Select(t => Path.GetFileName(t.FilePath)).ToArray();
        fileNames.ShouldNotContain("TestDomain.HugeMostlyDerivableCommand.CommandPage.g.razor.cs");

        string rendererSource = result.GeneratedTrees
            .Single(t => Path.GetFileName(t.FilePath) == "TestDomain.HugeMostlyDerivableCommand.CommandRenderer.g.razor.cs")
            .GetText(TestContext.Current.CancellationToken)
            .ToString();
        rendererSource.ShouldContain("RenderMode ?? CommandRenderMode.CompactInline");
        rendererSource.ShouldContain("density=CompactInline");
    }

    [Fact]
    public void RunGenerators_CommandArtifactInventory_UsesCommandHintSegmentAndOptionalPage() {
        AssertCommandArtifactInventory(
            CommandTestSources.SingleStringFieldCommand,
            "SetNameCommand",
            expectedCommandArtifacts:
            [
                "TestDomain.SetNameCommand.CommandForm.g.razor.cs",
                "TestDomain.SetNameCommand.CommandActions.g.cs",
                "TestDomain.SetNameCommand.CommandLifecycleFeature.g.cs",
                "TestDomain.SetNameCommand.CommandRegistration.g.cs",
                "TestDomain.SetNameCommand.CommandRenderer.g.razor.cs",
                "TestDomain.SetNameCommand.CommandLastUsedSubscriber.g.cs",
                "TestDomain.SetNameCommand.CommandLifecycleBridge.g.cs",
            ]);

        AssertCommandArtifactInventory(
            CommandTestSources.MultiFieldCommand,
            "PlaceOrderCommand",
            expectedCommandArtifacts:
            [
                "TestDomain.PlaceOrderCommand.CommandForm.g.razor.cs",
                "TestDomain.PlaceOrderCommand.CommandActions.g.cs",
                "TestDomain.PlaceOrderCommand.CommandLifecycleFeature.g.cs",
                "TestDomain.PlaceOrderCommand.CommandRegistration.g.cs",
                "TestDomain.PlaceOrderCommand.CommandRenderer.g.razor.cs",
                "TestDomain.PlaceOrderCommand.CommandLastUsedSubscriber.g.cs",
                "TestDomain.PlaceOrderCommand.CommandLifecycleBridge.g.cs",
                "TestDomain.PlaceOrderCommand.CommandPage.g.razor.cs",
            ]);
    }

    [Fact]
    public void RunGenerators_CommandRegistration_ExposesCommandToDomainRegistry() {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(CommandTestSources.SingleStringFieldCommand);
        FrontComposerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation, ct);
        GeneratorDriverRunResult result = driver.GetRunResult();

        SyntaxTree registrationTree = result.GeneratedTrees
            .Single(t => Path.GetFileName(t.FilePath) == "TestDomain.SetNameCommand.CommandRegistration.g.cs");
        string registrationSource = registrationTree.GetText(ct).ToString();

        registrationSource.ShouldContain("Commands: new List<string> { typeof(SetNameCommand).FullName! });");
        registrationSource.ShouldContain("public static void RegisterDomain(IFrontComposerRegistry registry)");
        registrationSource.ShouldContain("registry.RegisterDomain(Manifest);");
    }

    [Fact]
    public void RunGenerators_CommandShapeDiagnostics_AreReportedAtGeneratorLevel() {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation([
            CommandTestSources.RecordPositionalCommand_NoDefaults,
            CommandTestSources.MissingMessageIdCommand,
        ]);
        FrontComposerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation, ct);
        GeneratorDriverRunResult result = driver.GetRunResult();

        Diagnostic hfc1009 = result.Diagnostics.Single(d => d.Id == "HFC1009");
        hfc1009.Severity.ShouldBe(DiagnosticSeverity.Error);
        result.Diagnostics.Single(d => d.Id == "HFC1006")
            .GetMessage()
            .ShouldContain("correlated end-to-end");

        string[] fileNames = result.GeneratedTrees.Select(t => Path.GetFileName(t.FilePath)).ToArray();
        fileNames.ShouldNotContain("TestDomain.IncrementCounterCommandNoDefaults.CommandForm.g.razor.cs");
    }

    [Fact]
    public void RunGenerators_UnsupportedCommandField_ReportsHfc1002AndEmitsPlaceholderForm() {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(CommandTestSources.UnsupportedFieldCommand);
        FrontComposerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation, ct);
        GeneratorDriverRunResult result = driver.GetRunResult();

        Diagnostic diagnostic = result.Diagnostics.First(d => d.Id == "HFC1002");
        diagnostic.GetMessage().ShouldContain("command customization path");
        diagnostic.GetMessage().ShouldNotContain("override rendering with [ProjectionFieldSlot]");

        SyntaxTree formTree = result.GeneratedTrees
            .Single(t => Path.GetFileName(t.FilePath) == "TestDomain.UnsupportedCommand.CommandForm.g.razor.cs");
        string formSource = formTree.GetText(ct).ToString();
        formSource.ShouldContain("FcFieldPlaceholder");
        formSource.ShouldContain("\"Raw\"");
        formSource.ShouldContain("\"Object\"");
        formSource.ShouldContain("FluentButton");
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

    private static void AssertCommandArtifactInventory(
        string source,
        string typeName,
        string[] expectedCommandArtifacts) {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source);
        FrontComposerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation, ct);
        GeneratorDriverRunResult result = driver.GetRunResult();

        result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        string[] commandArtifacts = result.GeneratedTrees
            .Select(t => Path.GetFileName(t.FilePath))
            .Where(name => name.StartsWith("TestDomain." + typeName + ".Command", StringComparison.Ordinal))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        commandArtifacts.ShouldBe(expectedCommandArtifacts.OrderBy(name => name, StringComparer.Ordinal).ToArray());
        commandArtifacts.ShouldAllBe(name => name.Contains("." + typeName + ".Command", StringComparison.Ordinal));
        commandArtifacts.ShouldNotContain("TestDomain." + typeName + "Registration.g.cs");
    }

    private static GeneratorDriverRunResult RunGenerator(string source) {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source);
        FrontComposerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation, ct);
        return driver.GetRunResult();
    }

    private static string BuildCommandSource(
        string typeName,
        int nonDerivableCount,
        int derivedFromCount = 0,
        bool includeWellKnownDerivableFields = false) {
        var sb = new System.Text.StringBuilder();
        _ = sb.AppendLine("using System;");
        _ = sb.AppendLine("using Hexalith.FrontComposer.Contracts.Attributes;");
        _ = sb.AppendLine("namespace TestDomain;");
        _ = sb.AppendLine("[Command]");
        _ = sb.Append("public class ").Append(typeName).AppendLine(" {");
        _ = sb.AppendLine("    public string MessageId { get; set; } = string.Empty;");
        if (includeWellKnownDerivableFields) {
            _ = sb.AppendLine("    public string CommandId { get; set; } = string.Empty;");
            _ = sb.AppendLine("    public string CorrelationId { get; set; } = string.Empty;");
            _ = sb.AppendLine("    public string TenantId { get; set; } = string.Empty;");
            _ = sb.AppendLine("    public string UserId { get; set; } = string.Empty;");
            _ = sb.AppendLine("    public DateTime Timestamp { get; set; }");
            _ = sb.AppendLine("    public DateTime CreatedAt { get; set; }");
            _ = sb.AppendLine("    public DateTime ModifiedAt { get; set; }");
        }

        for (int i = 0; i < nonDerivableCount; i++) {
            _ = sb.Append("    public string Field").Append(i).AppendLine(" { get; set; } = string.Empty;");
        }

        for (int i = 0; i < derivedFromCount; i++) {
            _ = sb.AppendLine("    [DerivedFrom(DerivedFromSource.Context)]");
            _ = sb.Append("    public string Derived").Append(i).AppendLine(" { get; set; } = string.Empty;");
        }

        _ = sb.AppendLine("}");
        return sb.ToString();
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
