using Bunit;

using Hexalith.FrontComposer.Contracts.Rendering;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

/// <summary>
/// TDD RED-phase assertions for Story 2-4 Task 5.6 — the generated renderer output of every
/// density must include the <c>fc-lifecycle-wrapper</c> marker once <see cref="CommandFormEmitter"/>
/// wraps its emitted <c>&lt;EditForm&gt;</c> in <c>&lt;FcLifecycleWrapper&gt;</c> (Task 4.1).
/// </summary>
/// <remarks>
/// Prior renderer snapshots (<c>CommandRendererCompactInlineTests</c>, <c>CommandRendererInlineTests</c>,
/// <c>CommandRendererFullPageTests</c>) remain untouched in the red phase; these 3 additional tests
/// supply the Task 5.6 coverage requirement without mutating passing fixtures.
/// </remarks>
public sealed class CommandRendererWrapperIntegrationTests : CommandRendererTestBase {
    [Fact(Skip = "TDD RED — Story 2-4 Task 4.1: emitter wrap landing in CompactInline density output.")]
    public async Task Renderer_CompactInline_Markup_Contains_FcLifecycleWrapper_Class() {
        await InitializeStoreAsync();

        IRenderedComponent<TwoFieldCompactCommandRenderer> cut = Render<TwoFieldCompactCommandRenderer>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("fc-lifecycle-wrapper", Case.Insensitive));
    }

    [Fact(Skip = "TDD RED — Story 2-4 Task 4.1: emitter wrap landing in Inline density output.")]
    public async Task Renderer_Inline_Markup_Contains_FcLifecycleWrapper_Class() {
        await InitializeStoreAsync();

        IRenderedComponent<OneFieldInlineCommandRenderer> cut = Render<OneFieldInlineCommandRenderer>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("fc-lifecycle-wrapper", Case.Insensitive));
    }

    [Fact(Skip = "TDD RED — Story 2-4 Task 4.1: emitter wrap landing in FullPage density output.")]
    public async Task Renderer_FullPage_Markup_Contains_FcLifecycleWrapper_Class() {
        await InitializeStoreAsync();

        IRenderedComponent<FiveFieldFullPageCommandRenderer> cut = Render<FiveFieldFullPageCommandRenderer>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("fc-lifecycle-wrapper", Case.Insensitive));
    }
}
