using System.Collections.Immutable;

using Hexalith.FrontComposer.SourceTools.Emitters;
using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Emitters;

/// <summary>
/// Story 4-4 T5.3 — verifies the generator wraps the FluentDataGrid in
/// <c>FcColumnPrioritizer</c> when <c>model.Columns.Count &gt; 15</c> and emits the
/// matching <c>_allColumnsDescriptor</c> field. Below the threshold, the wrap is
/// omitted (CounterProjectionApprovalTests regression rail).
/// </summary>
public sealed class RazorEmitterColumnPrioritizerTests {
    private static readonly EquatableArray<BadgeMappingEntry> _emptyBadges =
        new(ImmutableArray<BadgeMappingEntry>.Empty);

    private static RazorModel BuildModel(int columnCount) {
        ImmutableArray<ColumnModel>.Builder builder = ImmutableArray.CreateBuilder<ColumnModel>(columnCount);
        for (int i = 1; i <= columnCount; i++) {
            builder.Add(new ColumnModel("Col" + i, "Header " + i, TypeCategory.Text, null, false, _emptyBadges));
        }

        return new RazorModel(
            typeName: "WideProjection",
            @namespace: "TestDomain",
            boundedContext: "Wide",
            columns: new EquatableArray<ColumnModel>(builder.ToImmutable()));
    }

    [Fact]
    public void NoWrap_AtFifteenColumns_PreservesBaseline() {
        string src = RazorEmitter.Emit(BuildModel(15));
        src.ShouldNotContain("FcColumnPrioritizer");
        src.ShouldNotContain("_allColumnsDescriptor");
    }

    [Fact]
    public void Wraps_WhenSixteenColumns_EmitsPrioritizerAndDescriptor() {
        string src = RazorEmitter.Emit(BuildModel(16));
        src.ShouldContain("FcColumnPrioritizer");
        src.ShouldContain("_allColumnsDescriptor");
        src.ShouldContain("_defaultHiddenColumns");
        src.ShouldContain("\"Col11\"");
        src.ShouldContain("MaxVisibleColumns");
    }

    [Fact]
    public void DescriptorListContainsAllColumnKeys() {
        string src = RazorEmitter.Emit(BuildModel(20));
        for (int i = 1; i <= 20; i++) {
            src.ShouldContain("\"Col" + i + "\"");
        }

        src.ShouldContain("if (!hiddenColumnSet.Contains(\"Col20\"))");
    }
}
