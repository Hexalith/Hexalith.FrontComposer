using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Rendering;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Contracts.Tests.Rendering;

/// <summary>
/// Story 4-3 T7.7 — FilterActions enforce non-empty ViewKey per Story 2-2 D30 discipline,
/// and IProjectionSearchProvider's open generic constraint is satisfied by any reference type.
/// </summary>
public sealed class FilterActionsTests {
    [Fact]
    public void ColumnFilterChangedAction_ThrowsOnEmptyViewKey() {
        Should.Throw<ArgumentException>(() => new ColumnFilterChangedAction(string.Empty, "Status", "value"));
        Should.Throw<ArgumentException>(() => new ColumnFilterChangedAction("   ", "Status", "value"));
        Should.Throw<ArgumentException>(() => new ColumnFilterChangedAction("bc:Proj", string.Empty, "value"));
    }

    [Fact]
    public void StatusFilterToggledAction_ThrowsOnEmptyInputs() {
        Should.Throw<ArgumentException>(() => new StatusFilterToggledAction(string.Empty, "Success"));
        Should.Throw<ArgumentException>(() => new StatusFilterToggledAction("bc:Proj", " "));
    }

    [Fact]
    public void GlobalSearchChangedAction_PermitsNullQuery_ButRejectsEmptyViewKey() {
        Should.NotThrow(() => new GlobalSearchChangedAction("bc:Proj", null));
        Should.NotThrow(() => new GlobalSearchChangedAction("bc:Proj", "acme"));
        Should.Throw<ArgumentException>(() => new GlobalSearchChangedAction(string.Empty, "q"));
    }

    [Fact]
    public void SortChangedAction_AcceptsNullSortColumn() {
        SortChangedAction action = new("bc:Proj", sortColumn: null, sortDescending: true);
        action.SortColumn.ShouldBeNull();
        action.SortDescending.ShouldBeTrue();
    }

    [Fact]
    public void FiltersResetAction_ThrowsOnEmptyViewKey() {
        Should.Throw<ArgumentException>(() => new FiltersResetAction(""));
        Should.Throw<ArgumentException>(() => new FiltersResetAction(" "));
    }

    [Fact]
    public void ReservedFilterKeys_ShapeIsPinned() {
        ReservedFilterKeys.StatusKey.ShouldBe("__status");
        ReservedFilterKeys.SearchKey.ShouldBe("__search");
    }

    [Fact]
    public async Task IProjectionSearchProvider_OpenGenericConstraintCompiles() {
        IProjectionSearchProvider<FakeProjection> provider = new FakeSearchProvider();

        IReadOnlyList<FakeProjection> result = await provider.SearchAsync("query", CancellationToken.None);

        result.Count.ShouldBe(0);
    }

    private sealed record FakeProjection(string Name);

    private sealed class FakeSearchProvider : IProjectionSearchProvider<FakeProjection> {
        public Task<IReadOnlyList<FakeProjection>> SearchAsync(string query, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<FakeProjection>>([]);
    }
}
