using Bunit;

using Microsoft.AspNetCore.Components.Web;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

public sealed class KeyboardTabOrderTests : CommandRendererTestBase {
    [Fact]
    public async Task Inline_OneField_EscapeKey_IsHandledWithoutException() {
        await InitializeStoreAsync();

        IRenderedComponent<OneFieldInlineCommandRenderer> cut = Render<OneFieldInlineCommandRenderer>();

        cut.WaitForAssertion(() => _ = cut.Find("fluent-button"));
        cut.Find("fluent-button").Click();
        cut.WaitForAssertion(() => {
            cut.FindAll(".fc-popover").Count.ShouldBeGreaterThan(0);
        });

        Should.NotThrow(() => cut.Find(".fc-popover").KeyDown(new KeyboardEventArgs { Key = "Escape" }));
    }

    [Fact]
    public async Task CompactInline_FieldMarkupOrder_MatchesFormFieldOrder() {
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
    public async Task FullPage_BreadcrumbMarkup_PrecedesFormMarkup() {
        PageContext.ReturnPath = "/counter";
        await InitializeStoreAsync();

        IRenderedComponent<FiveFieldFullPageCommandRenderer> cut = Render<FiveFieldFullPageCommandRenderer>();

        cut.WaitForAssertion(() => {
            string markup = cut.Markup;
            markup.IndexOf("breadcrumb", StringComparison.OrdinalIgnoreCase)
                .ShouldBeLessThan(markup.IndexOf("Name", StringComparison.Ordinal));
        });
    }
}
