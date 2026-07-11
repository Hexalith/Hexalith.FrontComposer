using System.Collections.Immutable;

using Hexalith.FrontComposer.Contracts.Rendering;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Shell.Tests.State.DataGridNavigation;

/// <summary>
/// Story 4-4 T7.5 / D3/D6/D7 — contract-surface validation for the 8 virtualization action records.
/// Every record rejects null/empty/whitespace <c>ViewKey</c>. Range-sensitive records
/// (<see cref="LoadPageAction"/>, <see cref="ScrollCapturedAction"/>) reject bad numeric inputs.
/// </summary>
public sealed class VirtualizationActionsTests {
    private static TaskCompletionSource<object> Tcs() => new();

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void AllActions_RejectEmptyOrWhitespaceViewKey(string bad) {
        _ = Should.Throw<ArgumentException>(() => new LoadPageAction(bad, 0, 20, ImmutableDictionary<string, string>.Empty, null, false, null, Tcs(), CancellationToken.None));
        _ = Should.Throw<ArgumentException>(() => new LoadPageSucceededAction(bad, 0, Array.Empty<object>(), 0, 0));
        _ = Should.Throw<ArgumentException>(() => new LoadPageFailedAction(bad, 0, "err"));
        _ = Should.Throw<ArgumentException>(() => new LoadPageCancelledAction(bad, 0));
        _ = Should.Throw<ArgumentException>(() => new ClearPendingPagesAction(bad));
        _ = Should.Throw<ArgumentException>(() => new ColumnVisibilityChangedAction(bad, "Status", true));
        _ = Should.Throw<ArgumentException>(() => new ResetColumnVisibilityAction(bad));
        _ = Should.Throw<ArgumentException>(() => new ScrollCapturedAction(bad, 0d));
    }

    [Fact]
    public void LoadPageAction_RejectsNegativeSkipAndNonPositiveTake() {
        _ = Should.Throw<ArgumentOutOfRangeException>(() => new LoadPageAction(
            "bc:Proj", skip: -1, take: 20, ImmutableDictionary<string, string>.Empty, null, false, null, Tcs(), CancellationToken.None));
        _ = Should.Throw<ArgumentOutOfRangeException>(() => new LoadPageAction(
            "bc:Proj", skip: 0, take: 0, ImmutableDictionary<string, string>.Empty, null, false, null, Tcs(), CancellationToken.None));
        _ = Should.Throw<ArgumentOutOfRangeException>(() => new LoadPageAction(
            "bc:Proj", skip: 0, take: -5, ImmutableDictionary<string, string>.Empty, null, false, null, Tcs(), CancellationToken.None));
    }

    [Fact]
    public void LoadPageAction_RejectsNullFiltersOrCompletion() {
        _ = Should.Throw<ArgumentNullException>(() => new LoadPageAction(
            "bc:Proj", 0, 20, filters: null!, null, false, null, Tcs(), CancellationToken.None));
        _ = Should.Throw<ArgumentNullException>(() => new LoadPageAction(
            "bc:Proj", 0, 20, ImmutableDictionary<string, string>.Empty, null, false, null, completion: null!, CancellationToken.None));
    }

    [Fact]
    public void ScrollCapturedAction_RejectsNaNInfinityAndNegatives() {
        _ = Should.Throw<ArgumentOutOfRangeException>(() => new ScrollCapturedAction("bc:Proj", double.NaN));
        _ = Should.Throw<ArgumentOutOfRangeException>(() => new ScrollCapturedAction("bc:Proj", double.PositiveInfinity));
        _ = Should.Throw<ArgumentOutOfRangeException>(() => new ScrollCapturedAction("bc:Proj", double.NegativeInfinity));
        _ = Should.Throw<ArgumentOutOfRangeException>(() => new ScrollCapturedAction("bc:Proj", -0.001));

        ScrollCapturedAction zero = new("bc:Proj", 0d);
        zero.ScrollTop.ShouldBe(0d);
        ScrollCapturedAction positive = new("bc:Proj", 123.45);
        positive.ScrollTop.ShouldBe(123.45);
    }

    [Fact]
    public void ColumnVisibilityChangedAction_RejectsReservedPrefixAndEmptyColumnKey() {
        _ = Should.Throw<ArgumentException>(() => new ColumnVisibilityChangedAction("bc:Proj", string.Empty, true));
        _ = Should.Throw<ArgumentException>(() => new ColumnVisibilityChangedAction("bc:Proj", "   ", false));
        _ = Should.Throw<ArgumentException>(() => new ColumnVisibilityChangedAction("bc:Proj", "__hidden", true));
        _ = Should.Throw<ArgumentException>(() => new ColumnVisibilityChangedAction("bc:Proj", "__status", false));

        ColumnVisibilityChangedAction ok = new("bc:Proj", "Status", isVisible: false);
        ok.IsVisible.ShouldBeFalse();
        ok.ColumnKey.ShouldBe("Status");
    }

    [Fact]
    public void LoadPageFailedAction_RejectsEmptyErrorMessage() {
        _ = Should.Throw<ArgumentException>(() => new LoadPageFailedAction("bc:Proj", 0, string.Empty));
        _ = Should.Throw<ArgumentException>(() => new LoadPageFailedAction("bc:Proj", 0, "   "));
        _ = Should.Throw<ArgumentOutOfRangeException>(() => new LoadPageFailedAction("bc:Proj", -1, "err"));
    }

    [Fact]
    public void LoadPageSucceededAction_RejectsNegativeTotalCountAndElapsed() {
        _ = Should.Throw<ArgumentOutOfRangeException>(() => new LoadPageSucceededAction("bc:Proj", 0, Array.Empty<object>(), totalCount: -1, elapsedMs: 0));
        _ = Should.Throw<ArgumentOutOfRangeException>(() => new LoadPageSucceededAction("bc:Proj", 0, Array.Empty<object>(), totalCount: 0, elapsedMs: -1));

        LoadPageSucceededAction ok = new("bc:Proj", 0, Array.Empty<object>(), totalCount: 0, elapsedMs: 0);
        _ = ok.Items.ShouldNotBeNull();
        ok.Items!.Count.ShouldBe(0);
    }

    [Fact]
    public void ReservedKey_IsPinned() => VirtualizationReservedKeys.HiddenColumnsKey.ShouldBe("__hidden");
}
