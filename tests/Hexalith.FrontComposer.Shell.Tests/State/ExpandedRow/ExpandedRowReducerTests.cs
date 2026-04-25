using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.State.ExpandedRow;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.State.ExpandedRow;

/// <summary>
/// Story 4-5 T3.4 / T4.1 / D2 / D3 / D4 / D18 — pure-reducer coverage for the single-expand
/// invariant (REPLACE on existing entry), idempotent collapse, and per-viewKey isolation.
/// </summary>
public sealed class ExpandedRowReducerTests {
    private static ExpandedRowState Empty() => new();

    [Fact]
    public void ExpandRowAction_AddsEntry_WhenNoneExistsForViewKey() {
        ExpandedRowState state = Empty();
        ExpandedRowState result = ExpandedRowReducers.ReduceExpandRow(
            state,
            new ExpandRowAction("orders:Orders:abc", 42));

        result.ExpandedByViewKey.Count.ShouldBe(1);
        result.GetEntry("orders:Orders:abc").HasValue.ShouldBeTrue();
        result.GetEntry("orders:Orders:abc")!.Value.ItemKey.ShouldBe(42);
    }

    [Fact]
    public void ExpandRowAction_ReplacesEntry_OnSameViewKey() {
        ExpandedRowState state = ExpandedRowReducers.ReduceExpandRow(
            Empty(),
            new ExpandRowAction("orders:Orders:abc", 42));

        ExpandedRowState result = ExpandedRowReducers.ReduceExpandRow(
            state,
            new ExpandRowAction("orders:Orders:abc", 99));

        result.ExpandedByViewKey.Count.ShouldBe(1);
        result.GetEntry("orders:Orders:abc")!.Value.ItemKey.ShouldBe(99);
    }

    [Fact]
    public void ExpandRowAction_PreservesOtherViewKeys() {
        ExpandedRowState state = ExpandedRowReducers.ReduceExpandRow(
            Empty(),
            new ExpandRowAction("orders:Orders:abc", 42));

        ExpandedRowState result = ExpandedRowReducers.ReduceExpandRow(
            state,
            new ExpandRowAction("invoices:Invoices:xyz", "I-001"));

        result.ExpandedByViewKey.Count.ShouldBe(2);
        result.GetEntry("orders:Orders:abc")!.Value.ItemKey.ShouldBe(42);
        result.GetEntry("invoices:Invoices:xyz")!.Value.ItemKey.ShouldBe("I-001");
    }

    [Fact]
    public void CollapseRowAction_RemovesExistingEntry() {
        ExpandedRowState state = ExpandedRowReducers.ReduceExpandRow(
            Empty(),
            new ExpandRowAction("orders:Orders:abc", 42));

        ExpandedRowState result = ExpandedRowReducers.ReduceCollapseRow(
            state,
            new CollapseRowAction("orders:Orders:abc"));

        result.ExpandedByViewKey.ShouldBeEmpty();
    }

    [Fact]
    public void CollapseRowAction_IsIdempotent_WhenNoEntryExists() {
        ExpandedRowState state = Empty();
        ExpandedRowState result = ExpandedRowReducers.ReduceCollapseRow(
            state,
            new CollapseRowAction("orders:Orders:abc"));

        result.ShouldBeSameAs(state);
    }

    [Fact]
    public void GetEntry_ReturnsNull_WhenViewKeyAbsent() {
        ExpandedRowState state = Empty();
        state.GetEntry("nonexistent").ShouldBeNull();
    }
}
