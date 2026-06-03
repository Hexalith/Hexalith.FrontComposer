using AngleSharp.Dom;

using Bunit;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Components.Layout;

using Microsoft.AspNetCore.Components;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Layout;

/// <summary>
/// Story 1.2 (FC-LYT) — pins both page-layout modes on the shell's <c>#fc-main-content</c> container
/// so the measure is assertable and cannot silently regress (AC3). Full-width is the zero-config
/// default (preserving today's edge-to-edge behaviour); constrained is opt-in via <see cref="FcPageLayout"/>.
/// </summary>
public sealed class Story12PageLayoutTests : LayoutComponentTestBase {
    [Fact]
    public void FrontComposerShell_WithNoPageLayout_RendersFullWidthContentContainer() {
        IRenderedComponent<FrontComposerShell> cut = Render<FrontComposerShell>(p => p
            .AddChildContent("<p>Body</p>"));

        cut.WaitForAssertion(() => {
            IElement content = cut.Find("#fc-main-content");
            content.GetAttribute("data-fc-page-layout").ShouldBe("full-width");

            string? cssClass = content.GetAttribute("class");
            cssClass.ShouldNotBeNull();
            cssClass.ShouldContain("fc-page-layout");
            cssClass.ShouldNotContain("fc-page-layout--constrained");
        });
    }

    [Fact]
    public void FrontComposerShell_WithConstrainedPageLayout_RendersConstrainedContentContainer() {
        RenderFragment constrainedBody = builder => {
            builder.OpenComponent<FcPageLayout>(0);
            builder.AddComponentParameter(1, nameof(FcPageLayout.Mode), FcPageLayoutMode.Constrained);
            builder.AddComponentParameter(2, nameof(FcPageLayout.ChildContent),
                (RenderFragment)(b => b.AddMarkupContent(0, "<p>Constrained body</p>")));
            builder.CloseComponent();
        };

        IRenderedComponent<FrontComposerShell> cut = Render<FrontComposerShell>(p => p
            .Add(c => c.ChildContent, constrainedBody));

        cut.WaitForAssertion(() => {
            IElement content = cut.Find("#fc-main-content");
            content.GetAttribute("data-fc-page-layout").ShouldBe("constrained");

            string? cssClass = content.GetAttribute("class");
            cssClass.ShouldNotBeNull();
            cssClass.ShouldContain("fc-page-layout--constrained");
        });
    }

    [Fact]
    public void FcPageLayout_WhenDeclaredAndDisposed_FlipsThenResetsCoordinatorMode() {
        FcPageLayoutCoordinator coordinator = new();
        coordinator.Mode.ShouldBe(FcPageLayoutMode.FullWidth);

        IRenderedComponent<FcPageLayout> cut = Render<FcPageLayout>(p => p
            .AddCascadingValue(coordinator)
            .Add(c => c.Mode, FcPageLayoutMode.Constrained)
            .AddChildContent("<p>Constrained body</p>"));

        // Register-on-first-render flips the cascaded coordinator (mirrors FcHamburgerToggle).
        cut.WaitForAssertion(() => coordinator.Mode.ShouldBe(FcPageLayoutMode.Constrained));

        // Dispose resets to the default so leaving a constrained page restores edge-to-edge.
        cut.Instance.Dispose();
        coordinator.Mode.ShouldBe(FcPageLayoutMode.FullWidth);
    }

    // ── QA gap coverage (qa-generate-e2e-tests, 2026-06-03) ─────────────────────────────────────
    // The three tests above pin the happy-path render of each mode + the FcPageLayout lifecycle.
    // The tests below close the gaps the QA sweep surfaced: the zero-regression enum keystone, the
    // coordinator's no-op/event contract (the render re-entry guard), the component's verbatim /
    // shell-less behaviour, the default-FcPageLayout no-op, the leave-constrained round-trip, and
    // the #fc-main-content skip-link/focus-target regression surface.

    [Fact]
    public void FcPageLayoutMode_Default_IsFullWidthZeroValue() {
        // The entire "no regression for existing pages" argument depends on FullWidth being the
        // zero/default value — a bare default(FcPageLayoutMode) must mean today's edge-to-edge mode.
        default(FcPageLayoutMode).ShouldBe(FcPageLayoutMode.FullWidth);
        ((int)FcPageLayoutMode.FullWidth).ShouldBe(0);
    }

    [Fact]
    public void FcPageLayoutCoordinator_SetSameMode_DoesNotRaiseChanged() {
        FcPageLayoutCoordinator coordinator = new();
        int changes = 0;
        coordinator.Changed += () => changes++;

        coordinator.SetMode(FcPageLayoutMode.FullWidth); // already FullWidth

        // No-op on an unchanged mode is what stops a shell re-render from re-entering the render loop.
        changes.ShouldBe(0);
        coordinator.Mode.ShouldBe(FcPageLayoutMode.FullWidth);
    }

    [Fact]
    public void FcPageLayoutCoordinator_SetDifferentMode_RaisesChangedOnce() {
        FcPageLayoutCoordinator coordinator = new();
        int changes = 0;
        coordinator.Changed += () => changes++;

        coordinator.SetMode(FcPageLayoutMode.Constrained);

        changes.ShouldBe(1);
        coordinator.Mode.ShouldBe(FcPageLayoutMode.Constrained);
    }

    [Fact]
    public void FcPageLayoutCoordinator_ResetFromConstrained_RaisesChangedAndRestoresFullWidth() {
        FcPageLayoutCoordinator coordinator = new();
        coordinator.SetMode(FcPageLayoutMode.Constrained);
        int changes = 0;
        coordinator.Changed += () => changes++;

        coordinator.Reset();

        changes.ShouldBe(1);
        coordinator.Mode.ShouldBe(FcPageLayoutMode.FullWidth);
    }

    [Fact]
    public void FcPageLayoutCoordinator_ResetWhenAlreadyFullWidth_IsNoOp() {
        FcPageLayoutCoordinator coordinator = new();
        int changes = 0;
        coordinator.Changed += () => changes++;

        coordinator.Reset();

        changes.ShouldBe(0);
        coordinator.Mode.ShouldBe(FcPageLayoutMode.FullWidth);
    }

    [Fact]
    public void FcPageLayout_RendersChildContentVerbatim_WithNoWrapperMarkup() {
        FcPageLayoutCoordinator coordinator = new();

        IRenderedComponent<FcPageLayout> cut = Render<FcPageLayout>(p => p
            .AddCascadingValue(coordinator)
            .Add(c => c.Mode, FcPageLayoutMode.Constrained)
            .AddChildContent("<p>Body</p>"));

        // FcPageLayout.razor is just @ChildContent — it must add no element of its own (the measure
        // is applied on the shell's #fc-main-content, never by wrapping the page content).
        cut.Markup.Trim().ShouldBe("<p>Body</p>");
    }

    [Fact]
    public void FcPageLayout_WhenModeRebindAfterFirstRender_UpdatesCoordinator() {
        // Mode is a value [Parameter] a page may rebind (e.g. <FcPageLayout Mode="@_mode">). A change
        // after the first render must flow through to the coordinator — first-render-only registration
        // would silently drop it (M1 review fix).
        FcPageLayoutCoordinator coordinator = new();

        IRenderedComponent<FcPageLayout> cut = Render<FcPageLayout>(p => p
            .AddCascadingValue(coordinator)
            .Add(c => c.Mode, FcPageLayoutMode.FullWidth)
            .AddChildContent("<p>Body</p>"));

        cut.WaitForAssertion(() => coordinator.Mode.ShouldBe(FcPageLayoutMode.FullWidth));

        cut.Render(p => p
            .Add(c => c.Mode, FcPageLayoutMode.Constrained)
            .AddChildContent("<p>Body</p>"));

        cut.WaitForAssertion(() => coordinator.Mode.ShouldBe(FcPageLayoutMode.Constrained));
    }

    [Fact]
    public void FcPageLayout_WithNoCascadedCoordinator_IsInertAndDisposeDoesNotThrow() {
        // Used outside a FrontComposerShell there is no coordinator to register with; the declaration
        // is silently inert and disposing the never-registered component must not throw.
        IRenderedComponent<FcPageLayout> cut = Render<FcPageLayout>(p => p
            .Add(c => c.Mode, FcPageLayoutMode.Constrained)
            .AddChildContent("<p>Body</p>"));

        cut.Markup.Trim().ShouldBe("<p>Body</p>");
        Should.NotThrow(() => cut.Instance.Dispose());
    }

    [Fact]
    public void FrontComposerShell_WithDefaultPageLayout_KeepsFullWidthContentContainer() {
        // A bare <FcPageLayout> (Mode defaults to FullWidth) is a no-op — content stays edge-to-edge.
        RenderFragment defaultBody = builder => {
            builder.OpenComponent<FcPageLayout>(0);
            builder.AddComponentParameter(1, nameof(FcPageLayout.ChildContent),
                (RenderFragment)(b => b.AddMarkupContent(0, "<p>Default body</p>")));
            builder.CloseComponent();
        };

        IRenderedComponent<FrontComposerShell> cut = Render<FrontComposerShell>(p => p
            .Add(c => c.ChildContent, defaultBody));

        cut.WaitForAssertion(() => {
            IElement content = cut.Find("#fc-main-content");
            content.GetAttribute("data-fc-page-layout").ShouldBe("full-width");
            string? cssClass = content.GetAttribute("class");
            cssClass.ShouldNotBeNull();
            cssClass.ShouldNotContain("fc-page-layout--constrained");
        });
    }

    [Fact]
    public void FrontComposerShell_WhenConstrainedPageRemoved_RestoresFullWidth() {
        RenderFragment constrainedBody = builder => {
            builder.OpenComponent<FcPageLayout>(0);
            builder.AddComponentParameter(1, nameof(FcPageLayout.Mode), FcPageLayoutMode.Constrained);
            builder.AddComponentParameter(2, nameof(FcPageLayout.ChildContent),
                (RenderFragment)(b => b.AddMarkupContent(0, "<p>Constrained body</p>")));
            builder.CloseComponent();
        };

        IRenderedComponent<FrontComposerShell> cut = Render<FrontComposerShell>(p => p
            .Add(c => c.ChildContent, constrainedBody));

        cut.WaitForAssertion(() =>
            cut.Find("#fc-main-content").GetAttribute("data-fc-page-layout").ShouldBe("constrained"));

        // Navigating away from the constrained page disposes its FcPageLayout → coordinator resets →
        // the shell re-renders #fc-main-content back to the full-width default.
        cut.Render(p => p
            .Add(c => c.ChildContent, (RenderFragment)(b => b.AddMarkupContent(0, "<p>Plain body</p>"))));

        cut.WaitForAssertion(() => {
            IElement content = cut.Find("#fc-main-content");
            content.GetAttribute("data-fc-page-layout").ShouldBe("full-width");
            string? cssClass = content.GetAttribute("class");
            cssClass.ShouldNotBeNull();
            cssClass.ShouldNotContain("fc-page-layout--constrained");
        });
    }

    [Fact]
    public void FrontComposerShell_WhenLayoutAnnotated_PreservesMainContentSkipLinkTarget() {
        // The layout attr/class is ADDED to #fc-main-content; its id (skip-link href="#fc-main-content"
        // target) and tabindex="-1" focus target must survive untouched (must-not-break regression).
        IRenderedComponent<FrontComposerShell> cut = Render<FrontComposerShell>(p => p
            .AddChildContent("<p>Body</p>"));

        cut.WaitForAssertion(() => {
            IElement content = cut.Find("#fc-main-content");
            content.Id.ShouldBe("fc-main-content");
            content.GetAttribute("tabindex").ShouldBe("-1");
        });
    }
}
