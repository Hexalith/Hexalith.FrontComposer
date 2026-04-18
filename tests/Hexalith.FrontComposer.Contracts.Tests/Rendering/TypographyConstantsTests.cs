using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Rendering;

using Microsoft.FluentUI.AspNetCore.Components;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Contracts.Tests.Rendering;

/// <summary>
/// Story 3-1 Task 10.10 (D2 / D11 / AC5) — lock the <see cref="Typography"/> constants against
/// the 3.1.0 mapping baseline. Any drift (mapping change without a version bump) fails CI before
/// it reaches adopter code. The mapping version is pinned via
/// <see cref="ContractsMetadata.TypographyMappingVersion"/>.
/// </summary>
public sealed class TypographyConstantsTests {
    [Fact]
    public void TypographyMappingVersionIsPinned() {
        ContractsMetadata.TypographyMappingVersion.ShouldBe("3.1.0");
    }

    [Fact]
    public void TypographyConstantsMatch3_1_0Baseline() {
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
