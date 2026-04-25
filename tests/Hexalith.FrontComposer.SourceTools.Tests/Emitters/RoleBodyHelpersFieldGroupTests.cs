using System.Collections.Immutable;

using Hexalith.FrontComposer.SourceTools.Emitters;
using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Emitters;

/// <summary>
/// Story 4-5 T6.5 / D9 — field group partitioning rules for secondary detail fields.
/// </summary>
public sealed class RoleBodyHelpersFieldGroupTests {
    private static readonly EquatableArray<BadgeMappingEntry> _emptyBadges =
        new(ImmutableArray<BadgeMappingEntry>.Empty);

    [Fact]
    public void ResolveFieldGroups_EmptyColumns_ReturnsEmptyBuckets() {
        IReadOnlyList<FieldGroupBucket> buckets = RoleBodyHelpers.ResolveFieldGroups([]);

        buckets.ShouldBeEmpty();
    }

    [Fact]
    public void ResolveFieldGroups_NoAnnotatedColumns_ReturnsSingleCatchAllBucket() {
        ColumnModel[] columns = [Col("Notes"), Col("Reference")];

        IReadOnlyList<FieldGroupBucket> buckets = RoleBodyHelpers.ResolveFieldGroups(columns);

        buckets.Count.ShouldBe(1);
        buckets[0].GroupName.ShouldBeNull();
        buckets[0].Columns.Select(c => c.PropertyName).ShouldBe(["Notes", "Reference"]);
    }

    [Fact]
    public void ResolveFieldGroups_GroupedColumnsKeepFirstSeenOrderAndAppendCatchAllLast() {
        ColumnModel[] columns =
        [
            Col("UngroupedBefore"),
            Col("ShippingStreet", group: "Shipping"),
            Col("BillingReference", group: "Billing"),
            Col("ShippingCity", group: "Shipping"),
            Col("UngroupedAfter"),
        ];

        IReadOnlyList<FieldGroupBucket> buckets = RoleBodyHelpers.ResolveFieldGroups(columns);

        buckets.Count.ShouldBe(3);
        buckets[0].GroupName.ShouldBe("Shipping");
        buckets[0].Columns.Select(c => c.PropertyName).ShouldBe(["ShippingStreet", "ShippingCity"]);
        buckets[1].GroupName.ShouldBe("Billing");
        buckets[1].Columns.Select(c => c.PropertyName).ShouldBe(["BillingReference"]);
        buckets[2].GroupName.ShouldBeNull();
        buckets[2].Columns.Select(c => c.PropertyName).ShouldBe(["UngroupedBefore", "UngroupedAfter"]);
    }

    private static ColumnModel Col(string name, string? group = null)
        => new(name, name, TypeCategory.Text, null, false, _emptyBadges, fieldGroup: group);
}
