using Bunit;

using Hexalith.FrontComposer.Contracts.Rendering;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

public sealed class CommandRendererCompactInlineTests : CommandRendererTestBase {
    [Fact]
    public async Task Renderer_CompactInline_RendersFluentCardWithExpandInRowClass() {
        BunitJSModuleInterop module = JSInterop.SetupModule("./_content/Hexalith.FrontComposer.Shell/js/fc-expandinrow.js");
        module.SetupVoid("initializeExpandInRow", _ => true);
        await InitializeStoreAsync();

        IRenderedComponent<TwoFieldCompactCommandRenderer> cut = Render<TwoFieldCompactCommandRenderer>();

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("fc-expand-in-row", Case.Insensitive);
            cut.Markup.ShouldContain("fluent-card", Case.Insensitive);
        });
    }

    [Fact]
    public async Task Renderer_CompactInline_InitializesJsModuleOnFirstRender() {
        BunitJSModuleInterop module = JSInterop.SetupModule("./_content/Hexalith.FrontComposer.Shell/js/fc-expandinrow.js");
        module.SetupVoid("initializeExpandInRow", _ => true);
        await InitializeStoreAsync();

        _ = Render<TwoFieldCompactCommandRenderer>();

        module.Invocations.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Renderer_CompactInline_FieldOrder_MatchesStory21FieldOrder() {
        BunitJSModuleInterop module = JSInterop.SetupModule("./_content/Hexalith.FrontComposer.Shell/js/fc-expandinrow.js");
        module.SetupVoid("initializeExpandInRow", _ => true);
        await InitializeStoreAsync();

        IRenderedComponent<TwoFieldCompactCommandRenderer> cut = Render<TwoFieldCompactCommandRenderer>();

        cut.WaitForAssertion(() => {
            string markup = cut.Markup;
            markup.IndexOf("Name", StringComparison.Ordinal).ShouldBeLessThan(markup.IndexOf("Amount", StringComparison.Ordinal));
        });
    }

    [Fact]
    public async Task Renderer_CompactInline_DerivableFieldIsNotRendered() {
        BunitJSModuleInterop module = JSInterop.SetupModule("./_content/Hexalith.FrontComposer.Shell/js/fc-expandinrow.js");
        module.SetupVoid("initializeExpandInRow", _ => true);
        await InitializeStoreAsync();

        IRenderedComponent<CompactCommandWithDerivableFieldRenderer> cut = Render<CompactCommandWithDerivableFieldRenderer>();

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("Name", Case.Insensitive);
            cut.Markup.ShouldContain("Amount", Case.Insensitive);
            cut.Markup.ShouldNotContain("Tenant", Case.Insensitive);
        });
    }

    [Fact]
    public async Task Renderer_CompactInline_PassesElementReferenceToJSModule() {
        // Decision D11 — `initializeExpandInRow` receives the FluentCard ElementReference
        // for scroll stabilization. The bUnit module setup records the call and arguments;
        // this test verifies the contract is wired (PrefersReducedMotion is honored inside the
        // JS module itself; covered by the JS module's own test path / E2E).
        BunitJSModuleInterop module = JSInterop.SetupModule("./_content/Hexalith.FrontComposer.Shell/js/fc-expandinrow.js");
        module.SetupVoid("initializeExpandInRow", _ => true);
        await InitializeStoreAsync();

        _ = Render<TwoFieldCompactCommandRenderer>();

        // The single argument is the captured ElementReference (cannot be null when the card is in DOM).
        List<JSRuntimeInvocation> initInvocations = [];
        foreach (JSRuntimeInvocation invocation in module.Invocations) {
            if (invocation.Identifier == "initializeExpandInRow") {
                initInvocations.Add(invocation);
            }
        }

        initInvocations.ShouldNotBeEmpty();
        initInvocations[0].Arguments.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Renderer_CompactInline_PrerenderJSDisconnect_DoesNotCrashRenderer() {
        // Decision D25 — the renderer's OnAfterRenderAsync wraps the JS import in a
        // try/catch (InvalidOperationException, JSDisconnectedException) so prerender or
        // circuit teardown does not surface as a Blazor render exception. This test
        // configures the module's `initializeExpandInRow` to throw and verifies the renderer
        // still constructs without surfacing the exception.
        BunitJSModuleInterop module = JSInterop.SetupModule("./_content/Hexalith.FrontComposer.Shell/js/fc-expandinrow.js");
        _ = module.SetupVoid("initializeExpandInRow", _ => true)
            .SetException(new Microsoft.JSInterop.JSDisconnectedException("simulated circuit teardown"));
        await InitializeStoreAsync();

        Should.NotThrow(() => Render<TwoFieldCompactCommandRenderer>());
    }

    [Fact]
    public async Task Renderer_CompactInline_DoesNotEmitEditFormDirectly() {
        // ADR-016 — the renderer is CHROME ONLY. The renderer must NEVER emit `<EditForm>`;
        // only the inner Form component owns validation. We assert that the renderer's outer
        // FluentCard wrapper does NOT contain a duplicate <form> — only the single one
        // emitted by the inner {Cmd}Form.
        BunitJSModuleInterop module = JSInterop.SetupModule("./_content/Hexalith.FrontComposer.Shell/js/fc-expandinrow.js");
        module.SetupVoid("initializeExpandInRow", _ => true);
        await InitializeStoreAsync();

        IRenderedComponent<TwoFieldCompactCommandRenderer> cut = Render<TwoFieldCompactCommandRenderer>();

        cut.WaitForAssertion(() => {
            // Exactly one <form> in markup — emitted by the inner Form's <EditForm>.
            int formCount = System.Text.RegularExpressions.Regex.Matches(cut.Markup, "<form\\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Count;
            formCount.ShouldBe(1);
        });
    }
}
