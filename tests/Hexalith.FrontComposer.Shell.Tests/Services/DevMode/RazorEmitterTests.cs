using System.Collections.Immutable;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.DevMode;
using Hexalith.FrontComposer.Shell.Services.DevMode;

using Microsoft.Extensions.Options;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Services.DevMode;

public sealed class RazorEmitterTests {
    [Theory]
    [InlineData(CustomizationLevel.Level2, "ProjectionViewContext<Counter.Contracts.CounterProjection>", "AddProjectionTemplate")]
    [InlineData(CustomizationLevel.Level3, "FieldSlotContext<Counter.Contracts.CounterProjection, string>", "AddSlotOverride<Counter.Contracts.CounterProjection>")]
    [InlineData(CustomizationLevel.Level4, "ProjectionViewContext<Counter.Contracts.CounterProjection>", "AddViewOverride<Counter.Contracts.CounterProjection")]
    public void EmitStarterTemplate_ProducesLevelSpecificRazorAndRegistrationSnippet(CustomizationLevel level, string contextText, string registrationText) {
        RazorEmitter emitter = new();
        ComponentTreeNode node = CreateNode(level);

        string source = emitter.EmitStarterTemplate(node, level);

        source.ShouldStartWith("// Generated for FrontComposer contract v");
        source.ShouldContain("HFC1049");
        source.ShouldContain("Projection: Counter.Contracts.CounterProjection");
        source.ShouldContain("DescriptorHash: sha256:abc");
        source.ShouldContain(contextText);
        source.ShouldContain(registrationText);
        source.ShouldContain("Generated DataGrid");
    }

    [Fact]
    public void EmitStarterTemplate_InvalidLevel_ReturnsSafeStub() {
        RazorEmitter emitter = new();

        string source = emitter.EmitStarterTemplate(CreateNode(CustomizationLevel.Level2), CustomizationLevel.Level4);

        source.ShouldContain("Starter template unavailable");
        source.ShouldContain("requested level does not match");
    }

    [Fact]
    public void EmitStarterTemplate_StaleNode_ReturnsSafeStub() {
        RazorEmitter emitter = new();
        ComponentTreeNode node = CreateNode(
            CustomizationLevel.Level3,
            staleReasons: [ComponentTreeStaleReason.DescriptorHashMismatch]);

        string source = emitter.EmitStarterTemplate(node, CustomizationLevel.Level3);

        source.ShouldContain("Starter template unavailable");
        source.ShouldContain("stale metadata");
        source.ShouldContain("DescriptorHashMismatch");
    }

    [Fact]
    public void EmitStarterTemplate_TruncatesDeepAndWideTrees() {
        // P15 — `depth >= MaxNodeDepth` truncates AT the bound. With MaxNodeDepth=2, root (depth 0)
        // and first-level children (depth 1) render; depth 2 (grandchild) hits the truncation
        // sentinel. MaxFanOut=1 caps the first level to one child, so child-b is replaced by the
        // fan-out truncation marker.
        FcShellOptions options = new() {
            DevMode = new FcShellDevModeOptions {
                MaxNodeDepth = 8,
                MaxFanOut = 8,
            },
        };
        FcShellOptions narrow = new() {
            DevMode = new FcShellDevModeOptions {
                MaxNodeDepth = 2,
                MaxFanOut = 1,
            },
        };
        _ = options;
        RazorEmitter emitter = new(Microsoft.Extensions.Options.Options.Create(narrow));
        ComponentTreeNode node = CreateNode(
            CustomizationLevel.Level2,
            children: [
                CreateNode(CustomizationLevel.Level1, annotationKey: "child-a", children: [CreateNode(CustomizationLevel.Level1, annotationKey: "grandchild")]),
                CreateNode(CustomizationLevel.Level1, annotationKey: "child-b"),
            ]);

        string source = emitter.EmitStarterTemplate(node, CustomizationLevel.Level2);

        source.ShouldContain("child-a");
        source.ShouldContain("Component tree truncated at MaxNodeDepth=2");
        source.ShouldContain("Component tree fan-out truncated after 1 children");
        source.ShouldNotContain("child-b");
        source.ShouldNotContain("grandchild");
    }

    private static ComponentTreeNode CreateNode(
        CustomizationLevel level,
        string annotationKey = "counter-grid",
        ImmutableArray<ComponentTreeNode> children = default,
        ImmutableArray<ComponentTreeStaleReason> staleReasons = default)
        => new(
            AnnotationKey: annotationKey,
            Convention: new ConventionDescriptor("Generated DataGrid", "Generated grid body.", "Use the lowest viable override.", level),
            ContractTypeName: level == CustomizationLevel.Level3 ? "string" : "Counter.Contracts.CounterProjection",
            CurrentLevel: level,
            OriginatingProjectionTypeName: "Counter.Contracts.CounterProjection",
            Role: null,
            FieldAccessor: "Name",
            Children: children,
            RenderEpoch: 42,
            ComponentTreeContractVersion: ComponentTreeContractVersion.Current,
            DescriptorHash: "sha256:abc",
            SourceComponentIdentity: "CounterProjection.Default",
            StaleReasons: staleReasons);
}
