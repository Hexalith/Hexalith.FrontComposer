using System.Collections.Immutable;

using Hexalith.FrontComposer.SourceTools.Emitters;
using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Microsoft.CodeAnalysis.CSharp;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Emitters;

/// <summary>
/// Story 2-2 Task 11.1 — golden-file snapshots for the renderer emitter, covering all density
/// boundaries (0/1/2/4/5 non-derivable fields), `[Icon]` overrides, and the FullPage page.
/// Task 11.2 (parseability) and 11.3 (determinism) live alongside.
/// </summary>
public class CommandRendererEmitterTests {
    private static CommandRendererModel BuildModel(
        int nonDerivableCount,
        string? iconName = null,
        CommandDensity? densityOverride = null,
        string typeName = "DemoCommand",
        string @namespace = "Demo.Domain",
        string boundedContext = "Demo") {
        ImmutableArray<string> nonDerivable = Enumerable
            .Range(0, nonDerivableCount)
            .Select(i => "Field" + i)
            .ToImmutableArray();
        ImmutableArray<string> derivable = ["MessageId", "TenantId"];

        CommandDensity density = densityOverride ?? nonDerivableCount switch {
            <= 1 => CommandDensity.Inline,
            <= 4 => CommandDensity.CompactInline,
            _ => CommandDensity.FullPage,
        };

        return new CommandRendererModel(
            typeName: typeName,
            @namespace: @namespace,
            boundedContext: boundedContext,
            density: density,
            iconName: iconName,
            displayLabel: "Demo",
            fullPageRoute: "/commands/" + boundedContext + "/" + typeName,
            commandFullyQualifiedName: @namespace + "." + typeName,
            nonDerivablePropertyNames: new EquatableArray<string>(nonDerivable),
            derivablePropertyNames: new EquatableArray<string>(derivable),
            formComponentName: typeName + "Form",
            actionsWrapperName: typeName + "Actions",
            stateName: typeName + "LifecycleState",
            subscriberTypeName: typeName + "LastUsedSubscriber");
    }

    [Fact]
    public Task Renderer_ZeroFields_InlineSnapshot()
        => Verify(CommandRendererEmitter.Emit(BuildModel(0)));

    [Fact]
    public Task Renderer_OneField_InlinePopoverSnapshot()
        => Verify(CommandRendererEmitter.Emit(BuildModel(1)));

    [Fact]
    public Task Renderer_TwoFields_CompactInlineSnapshot()
        => Verify(CommandRendererEmitter.Emit(BuildModel(2)));

    [Fact]
    public Task Renderer_FourFields_CompactInlineBoundarySnapshot()
        => Verify(CommandRendererEmitter.Emit(BuildModel(4)));

    [Fact]
    public Task Renderer_FiveFields_FullPageBoundarySnapshot()
        => Verify(CommandRendererEmitter.Emit(BuildModel(5)));

    [Fact]
    public Task Page_FiveFields_FullPageBoundarySnapshot()
        => Verify(CommandPageEmitter.Emit(BuildModel(5)));

    [Fact]
    public Task Renderer_OneField_WithIconAttributeSnapshot()
        => Verify(CommandRendererEmitter.Emit(BuildModel(1, iconName: "Regular.Size20.Settings")));

    [Fact]
    public Task Renderer_OneField_WithoutIconUsesDefaultSnapshot()
        => Verify(CommandRendererEmitter.Emit(BuildModel(1, iconName: null)));

    // === Task 11.2 — parseability ===

    [Fact]
    public void Renderer_AllDensities_ProduceValidCSharp() {
        CancellationToken ct = TestContext.Current.CancellationToken;
        foreach (int count in (int[])[0, 1, 2, 4, 5]) {
            string source = CommandRendererEmitter.Emit(BuildModel(count));
            Microsoft.CodeAnalysis.SyntaxTree tree = CSharpSyntaxTree.ParseText(source, cancellationToken: ct);
            tree.GetDiagnostics(ct).ShouldBeEmpty($"renderer for {count} non-derivable fields should parse cleanly");
        }

        string pageSource = CommandPageEmitter.Emit(BuildModel(5));
        Microsoft.CodeAnalysis.SyntaxTree pageTree = CSharpSyntaxTree.ParseText(pageSource, cancellationToken: ct);
        pageTree.GetDiagnostics(ct).ShouldBeEmpty("FullPage page should parse cleanly");
    }

    // === Task 11.3 — determinism ===

    [Fact]
    public void Renderer_RepeatedEmit_IsByteIdentical() {
        CommandRendererModel model = BuildModel(3);
        string first = CommandRendererEmitter.Emit(model);
        string second = CommandRendererEmitter.Emit(model);
        first.ShouldBe(second);

        CommandRendererModel pageModel = BuildModel(5);
        string firstPage = CommandPageEmitter.Emit(pageModel);
        string secondPage = CommandPageEmitter.Emit(pageModel);
        firstPage.ShouldBe(secondPage);
    }
}
