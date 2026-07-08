using Bunit;

using Hexalith.FrontComposer.Contracts.DevMode;
using Hexalith.FrontComposer.Shell.Components.DevMode;
using Hexalith.FrontComposer.Shell.Services.DevMode;
using Hexalith.FrontComposer.Shell.Tests.Components;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.DevMode;

public sealed class FcDevModeVisualReachabilityTests : BunitContext {
    public FcDevModeVisualReachabilityTests() {
        JSInterop.Mode = JSRuntimeMode.Loose;
        _ = Services.AddLocalization();
        _ = Services.AddFluentUIComponents();
        _ = Services.AddScoped<IDevModeOverlayController, DevModeOverlayController>();
        _ = Services.AddScoped<IRazorEmitter, RazorEmitter>();
    }

    [Fact]
    public void Annotation_RendersStableClassReachedThroughScopedHostDeepSelector() {
        IDevModeOverlayController controller = Services.GetRequiredService<IDevModeOverlayController>();
        controller.Toggle();

        IRenderedComponent<FcDevModeAnnotation> cut = Render<FcDevModeAnnotation>(p => p
            .Add(c => c.Node, CreateNode(isUnsupported: false)));

        AngleSharp.Dom.IElement host = cut.Find(".fc-devmode-annotation-host");
        AngleSharp.Dom.IElement annotation = cut.Find(".fc-devmode-annotation");
        host.QuerySelector(".fc-devmode-annotation").ShouldNotBeNull();
        (annotation.GetAttribute("aria-label") ?? string.Empty).ShouldContain("Generated field");
        cut.Find(".fc-devmode-badge").TextContent.ShouldBe("Generated field");

        string css = VisualReachabilityTestSupport.ReadShellComponentCss(
            "DevMode",
            "FcDevModeAnnotation.razor.css");
        css.ShouldContain(".fc-devmode-annotation-host ::deep .fc-devmode-annotation");
        css.ShouldContain(".fc-devmode-annotation-host ::deep .fc-devmode-unsupported");
        css.ShouldContain("--colorPaletteRedBorder2");
    }

    [Fact]
    public void ToggleButton_RendersStableClassReachedThroughScopedHostDeepSelector() {
        IRenderedComponent<FcDevModeToggleButton> cut = Render<FcDevModeToggleButton>();

        AngleSharp.Dom.IElement host = cut.Find(".fc-devmode-toggle-host");
        AngleSharp.Dom.IElement toggle = cut.Find(".fc-devmode-toggle");
        host.QuerySelector(".fc-devmode-toggle").ShouldNotBeNull();
        (toggle.GetAttribute("aria-label") ?? string.Empty).ShouldContain("Toggle developer overlay");

        string css = VisualReachabilityTestSupport.ReadShellComponentCss(
            "DevMode",
            "FcDevModeToggleButton.razor.css");
        css.ShouldContain(".fc-devmode-toggle-host ::deep .fc-devmode-toggle");
        css.ShouldContain(".fc-devmode-toggle-host ::deep .fc-devmode-toggle:focus-visible");
    }

    [Fact]
    public void OverlayControls_RenderStableClassesReachedThroughDrawerDeepSelectors() {
        IDevModeOverlayController controller = Services.GetRequiredService<IDevModeOverlayController>();
        ComponentTreeNode node = CreateNode(isUnsupported: true);
        controller.Toggle();
        using IDisposable registration = controller.Register(node);
        controller.Open(node.AnnotationKey, node.RenderEpoch).ShouldBeTrue();

        IRenderedComponent<FcDevModeOverlay> cut = Render<FcDevModeOverlay>();

        AngleSharp.Dom.IElement drawer = cut.Find(".fc-devmode-drawer");
        drawer.GetAttribute("role").ShouldBe("complementary");
        drawer.QuerySelector(".fc-devmode-icon-button").ShouldNotBeNull();
        drawer.QuerySelector(".fc-devmode-copy-button").ShouldNotBeNull();
        cut.Find(".fc-devmode-copy-button").Click();
        cut.WaitForAssertion(() => drawer.QuerySelector(".fc-devmode-source").ShouldNotBeNull());

        string css = VisualReachabilityTestSupport.ReadShellComponentCss(
            "DevMode",
            "FcDevModeOverlay.razor.css");
        css.ShouldContain(".fc-devmode-drawer ::deep .fc-devmode-copy-button");
        css.ShouldContain(".fc-devmode-drawer ::deep .fc-devmode-icon-button");
        css.ShouldContain(".fc-devmode-drawer ::deep .fc-devmode-source");
    }

    private static ComponentTreeNode CreateNode(bool isUnsupported)
        => new(
            AnnotationKey: isUnsupported ? "unsupported-field" : "generated-field",
            Convention: new ConventionDescriptor(
                "Generated field",
                "Generated field output.",
                "Use a slot override.",
                CustomizationLevel.Level3),
            ContractTypeName: "Counter.Contracts.CounterProjection",
            CurrentLevel: CustomizationLevel.Level3,
            OriginatingProjectionTypeName: "Counter.Contracts.CounterProjection",
            Role: null,
            FieldAccessor: "Name",
            Children: [],
            RenderEpoch: 42,
            ComponentTreeContractVersion: ComponentTreeContractVersion.Current,
            DescriptorHash: "sha256:abc",
            SourceComponentIdentity: "CounterProjection.Name",
            IsUnsupported: isUnsupported);
}
