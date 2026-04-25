using System.Collections.Immutable;

using Hexalith.FrontComposer.SourceTools.Emitters;
using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Emitters;

/// <summary>
/// Story 4-4 T5.4 — verifies the generator unconditionally emits <c>FcSlowQueryNotice</c>
/// and <c>FcMaxItemsCapNotice</c> above the grid (visibility is component-side gated)
/// plus the unfiltered row-count display and the phone-viewport HTML deferral comment.
/// </summary>
public sealed class RazorEmitterBannersTests {
    private static readonly EquatableArray<BadgeMappingEntry> _emptyBadges =
        new(ImmutableArray<BadgeMappingEntry>.Empty);

    private static RazorModel Model()
        => new("OrderProjection", "TestDomain", "Orders",
            new EquatableArray<ColumnModel>(ImmutableArray.Create(
                new ColumnModel("Id", "Id", TypeCategory.Text, null, false, _emptyBadges),
                new ColumnModel("Name", "Name", TypeCategory.Text, null, false, _emptyBadges))));

    [Fact]
    public void EmitsBothBannerComponentsWithViewKeyParameter() {
        string src = RazorEmitter.Emit(Model());
        src.ShouldContain("FcSlowQueryNotice");
        src.ShouldContain("FcMaxItemsCapNotice");
        src.ShouldContain("\"ViewKey\", _viewKey");
        src.ShouldContain("\"ItemsCount\", state.Items.Count");
        src.ShouldContain("\"AnyRealFilterActive\", anyRealFilterActive");
    }

    [Fact]
    public void EmitsOuterScrollHostAndPhoneViewportComment() {
        string src = RazorEmitter.Emit(Model());
        src.ShouldContain("<!-- Phone-viewport layout (UX-DR7 phone variant) deferred to Story 10-2 -->");
        src.ShouldContain("\"data-fc-datagrid\", _viewKey");
        src.ShouldContain("\"onscroll\"");
        src.ShouldContain("\"fc-datagrid-host\"");
    }

    [Fact]
    public void EmitsUnfilteredRowCountDisplay() {
        string src = RazorEmitter.Emit(Model());
        src.ShouldContain("\"fc-row-count\"");
        src.ShouldContain("state.Items.Count");
    }
}
