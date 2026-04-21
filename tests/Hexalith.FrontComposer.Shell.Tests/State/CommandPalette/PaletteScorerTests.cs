// Story 3-4 Task 10.3 (D7 — AC3).
using Hexalith.FrontComposer.Shell.State.CommandPalette;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.State.CommandPalette;

public class PaletteScorerTests {
    [Theory]
    [InlineData("counter", "CounterProjection")]
    [InlineData("Counter", "counterprojection")]
    [InlineData("cou", "CounterProjection")]
    public void Score_ExactPrefixCaseInsensitive_Returns100Plus(string query, string candidate) {
        PaletteScorer.Score(query, candidate).ShouldBeGreaterThanOrEqualTo(100);
    }

    [Theory]
    [InlineData("ord", "SubmitOrderCommand")]
    [InlineData("ent", "IncrementCommand")]
    public void Score_Contains_ReturnsBetween50And100(string query, string candidate) {
        int score = PaletteScorer.Score(query, candidate);
        score.ShouldBeGreaterThanOrEqualTo(50);
        score.ShouldBeLessThan(100);
    }

    [Theory]
    [InlineData("smtod", "SubmitOrder")]
    [InlineData("inccmd", "IncrementCommand")]
    public void Score_FuzzySubsequence_ReturnsAtLeast10(string query, string candidate) {
        PaletteScorer.Score(query, candidate).ShouldBeGreaterThanOrEqualTo(1);
    }

    [Theory]
    [InlineData("xyz", "CounterProjection")]
    [InlineData("zzz", "Increment")]
    public void Score_NoMatch_ReturnsZero(string query, string candidate) {
        PaletteScorer.Score(query, candidate).ShouldBe(0);
    }

    [Fact]
    public void Score_EmptyInputs_ReturnsZero() {
        PaletteScorer.Score("", "x").ShouldBe(0);
        PaletteScorer.Score("x", "").ShouldBe(0);
    }
}
