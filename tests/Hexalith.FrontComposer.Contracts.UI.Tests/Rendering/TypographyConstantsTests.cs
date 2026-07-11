using Hexalith.FrontComposer.Contracts.Rendering;

using Microsoft.FluentUI.AspNetCore.Components;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Contracts.UI.Tests.Rendering;

public sealed class TypographyConstantsTests {
    [Fact]
    public void TypographyConstants_MappingVersion3_1_0_MatchesBaseline() {
        Typography.AppTitle.ShouldBe(new FcTypoToken(TextSize.Size700, TextWeight.Bold, TextTag.H1));
        Typography.BoundedContextHeading.ShouldBe(new FcTypoToken(TextSize.Size500, TextWeight.Semibold, TextTag.H2));
        Typography.ViewTitle.ShouldBe(new FcTypoToken(TextSize.Size600, TextWeight.Semibold, TextTag.H2));
        Typography.SectionHeading.ShouldBe(new FcTypoToken(TextSize.Size400, TextWeight.Semibold, TextTag.H3));
        Typography.FieldLabel.ShouldBe(new FcTypoToken(TextSize.Size300, TextWeight.Semibold, TextTag.Span));
        Typography.Body.ShouldBe(new FcTypoToken(TextSize.Size300, TextWeight.Regular, TextTag.Span));
        Typography.Secondary.ShouldBe(new FcTypoToken(TextSize.Size200, TextWeight.Regular, TextTag.Span));
        Typography.Caption.ShouldBe(new FcTypoToken(TextSize.Size200, TextWeight.Regular, TextTag.Span));
        Typography.Code.ShouldBe(new FcTypoToken(TextSize.Size300, TextWeight.Regular, TextTag.Span, TextFont.Monospace));
    }
}
