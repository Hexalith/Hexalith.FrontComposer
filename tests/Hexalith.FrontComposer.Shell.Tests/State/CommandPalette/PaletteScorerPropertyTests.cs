// Story 3-4 Task 10.3a (D7 — AC3) — FsCheck property tests.
using FsCheck.Xunit;

using Hexalith.FrontComposer.Shell.State.CommandPalette;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.State.CommandPalette;

public class PaletteScorerPropertyTests {
    [Property]
    public bool ScoreIsDeterministic(string query, string candidate) {
        int first = PaletteScorer.Score(query ?? string.Empty, candidate ?? string.Empty);
        int second = PaletteScorer.Score(query ?? string.Empty, candidate ?? string.Empty);
        return first == second;
    }

    [Fact]
    public void ScoreIsMonotonicOnPrefixLength() {
        // Within the prefix band, longer prefixes score strictly higher.
        int two = PaletteScorer.Score("co", "Counter");
        int four = PaletteScorer.Score("coun", "Counter");
        four.ShouldBeGreaterThan(two);
    }
}
