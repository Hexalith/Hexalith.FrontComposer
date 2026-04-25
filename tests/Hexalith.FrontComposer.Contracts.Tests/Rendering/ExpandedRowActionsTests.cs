using Hexalith.FrontComposer.Contracts.Rendering;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Contracts.Tests.Rendering;

/// <summary>
/// Story 4-5 T4.6 — record validation for ExpandRowAction + CollapseRowAction.
/// </summary>
public sealed class ExpandedRowActionsTests {
    [Fact]
    public void ExpandRowAction_RejectsEmptyViewKey() {
        Should.Throw<ArgumentException>(() => new ExpandRowAction("", 42));
        Should.Throw<ArgumentException>(() => new ExpandRowAction("   ", 42));
    }

    [Fact]
    public void ExpandRowAction_RejectsNullItemKey() {
        Should.Throw<ArgumentNullException>(() => new ExpandRowAction("orders:View:abc", null!));
    }

    [Fact]
    public void ExpandRowAction_AcceptsValueTypeKey() {
        ExpandRowAction action = new("orders:View:abc", 42);
        action.ItemKey.ShouldBe(42);
        action.ViewKey.ShouldBe("orders:View:abc");
    }

    [Fact]
    public void CollapseRowAction_RejectsEmptyViewKey() {
        Should.Throw<ArgumentException>(() => new CollapseRowAction(""));
        Should.Throw<ArgumentException>(() => new CollapseRowAction("   "));
    }

    [Fact]
    public void Records_AreValueEquatable() {
        ExpandRowAction a1 = new("v:k", "id");
        ExpandRowAction a2 = new("v:k", "id");
        a1.ShouldBe(a2);

        CollapseRowAction c1 = new("v:k");
        CollapseRowAction c2 = new("v:k");
        c1.ShouldBe(c2);
    }
}
