using Bunit;

using Hexalith.FrontComposer.Contracts.Rendering;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

/// <summary>
/// Story 2-4 Task 5.6 — the generated renderer output of every density must include the
/// <c>fc-lifecycle-wrapper</c> marker now that <see cref="CommandFormEmitter"/> wraps its
/// emitted <c>&lt;EditForm&gt;</c> in <c>&lt;FcLifecycleWrapper&gt;</c> (Task 4.1).
/// </summary>
public sealed class CommandRendererWrapperIntegrationTests : CommandRendererTestBase {
    [Fact]
    public async Task Renderer_CompactInline_Markup_Contains_FcLifecycleWrapper_Class() {
        BunitJSModuleInterop module = JSInterop.SetupModule("./_content/Hexalith.FrontComposer.Shell/js/fc-expandinrow.js");
        module.SetupVoid("initializeExpandInRow", _ => true);
        await InitializeStoreAsync();

        IRenderedComponent<TwoFieldCompactCommandRenderer> cut = Render<TwoFieldCompactCommandRenderer>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("fc-lifecycle-wrapper", Case.Insensitive));
    }

    [Fact]
    public async Task Renderer_Inline_Markup_Contains_FcLifecycleWrapper_Class() {
        await InitializeStoreAsync();

        IRenderedComponent<OneFieldInlineCommandRenderer> cut = Render<OneFieldInlineCommandRenderer>();

        cut.WaitForAssertion(() => _ = cut.Find("fluent-button"));
        cut.Find("fluent-button").Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("fc-lifecycle-wrapper", Case.Insensitive));
    }

    [Fact]
    public async Task Renderer_FullPage_Markup_Contains_FcLifecycleWrapper_Class() {
        PageContext.ReturnPath = "/counter";
        await InitializeStoreAsync();

        IRenderedComponent<FiveFieldFullPageCommandRenderer> cut = Render<FiveFieldFullPageCommandRenderer>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("fc-lifecycle-wrapper", Case.Insensitive));
    }
}
