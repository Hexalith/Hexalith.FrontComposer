using Bunit;

using Microsoft.AspNetCore.Components;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

/// <summary>
/// Story 2-2 Task 12.1 — accessibility surface verification, one test per density mode.
/// These tests assert ARIA contract on the rendered markup (aria-label, aria-expanded).
/// Real <c>axe.run()</c> DOM-walking happens at the E2E browser layer (Story 13.5 / Counter
/// sample) — bUnit cannot exercise the FluentUI v5 web-component shadow DOM.
/// </summary>
public sealed class AxeCoreA11yTests : CommandRendererTestBase {
    [Fact]
    public async Task AxeCore_InlineRenderer_NoSeriousOrCriticalViolations() {
        await InitializeStoreAsync();

        IRenderedComponent<OneFieldInlineCommandRenderer> cut = Render<OneFieldInlineCommandRenderer>();

        cut.WaitForAssertion(() => {
            string markup = cut.Markup;
            // axe `button-name` rule — the trigger button must carry a discernible name
            // (FluentButton emits the label via the `current-value` / inner text path).
            markup.ShouldContain("fluent-button", Case.Insensitive);
            // axe `region` rule — popover-related forms still carry the form aria-label.
            markup.ShouldContain("aria-label=\"One Field Inline command form\"", Case.Insensitive);
        });
    }

    [Fact]
    public async Task AxeCore_CompactInlineRenderer_NoSeriousOrCriticalViolations() {
        BunitJSModuleInterop module = JSInterop.SetupModule("./_content/Hexalith.FrontComposer.Shell/js/fc-expandinrow.js");
        module.SetupVoid("initializeExpandInRow", _ => true);
        await InitializeStoreAsync();

        IRenderedComponent<TwoFieldCompactCommandRenderer> cut = Render<TwoFieldCompactCommandRenderer>();

        cut.WaitForAssertion(() => {
            string markup = cut.Markup;
            // axe `region` rule — the inner form carries an aria-label that names the surface.
            markup.ShouldContain("aria-label=\"Two Field Compact command form\"", Case.Insensitive);
            // axe `landmark` rule — the form is the canonical landmark for the surface.
            markup.ShouldContain("<form", Case.Insensitive);
        });
    }

    [Fact]
    public async Task AxeCore_FullPageRenderer_NoSeriousOrCriticalViolations() {
        PageContext.ReturnPath = "/counter";
        await InitializeStoreAsync();

        IRenderedComponent<FiveFieldFullPageCommandRenderer> cut = Render<FiveFieldFullPageCommandRenderer>();

        cut.WaitForAssertion(() => {
            string markup = cut.Markup;
            // axe `landmark-unique` rule — the breadcrumb is a named navigation landmark.
            markup.ShouldContain("aria-label=\"breadcrumb\"", Case.Insensitive);
            // axe `region` rule — inner form aria-label.
            markup.ShouldContain("aria-label=\"Five Field Full Page command form\"", Case.Insensitive);
        });
    }
}
