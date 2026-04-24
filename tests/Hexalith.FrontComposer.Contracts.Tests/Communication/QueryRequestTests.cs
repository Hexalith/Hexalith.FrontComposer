using System;
using System.Collections.Generic;

using Hexalith.FrontComposer.Contracts.Communication;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Contracts.Tests.Communication;

/// <summary>
/// Story 4-3 T7.6 / D2 — QueryRequest additive fields default to null/false; legacy Filter
/// gains [Obsolete] but still compiles. Guards the backward-compat contract frozen for v0.x.
/// </summary>
public sealed class QueryRequestTests {
    [Fact]
    public void AdditiveFieldsDefaultToNullOrFalse() {
        QueryRequest request = new(ProjectionType: "OrdersProjection", TenantId: "acme");

        request.ColumnFilters.ShouldBeNull();
        request.StatusFilters.ShouldBeNull();
        request.SearchQuery.ShouldBeNull();
        request.SortColumn.ShouldBeNull();
        request.SortDescending.ShouldBeFalse();
#pragma warning disable CS0618 // Legacy property
        request.Filter.ShouldBeNull();
#pragma warning restore CS0618
    }

    [Fact]
    public void SupportsInitializedFilterFields() {
        Dictionary<string, string> columnFilters = new(StringComparer.Ordinal) { ["Name"] = "acme" };
        List<string> statusFilters = ["Pending", "Approved"];

        QueryRequest request = new(
            ProjectionType: "OrdersProjection",
            TenantId: "acme",
            ColumnFilters: columnFilters,
            StatusFilters: statusFilters,
            SearchQuery: "foo",
            SortColumn: "Name",
            SortDescending: true);

        request.ColumnFilters.ShouldNotBeNull();
        request.ColumnFilters!["Name"].ShouldBe("acme");
        request.StatusFilters.ShouldBe(["Pending", "Approved"]);
        request.SearchQuery.ShouldBe("foo");
        request.SortColumn.ShouldBe("Name");
        request.SortDescending.ShouldBeTrue();
    }
}
