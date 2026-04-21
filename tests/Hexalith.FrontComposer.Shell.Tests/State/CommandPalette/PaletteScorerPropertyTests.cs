// Story 3-4 Task 10.3a (D7 — AC3) — FsCheck property tests.
using FsCheck;
using FsCheck.Xunit;

using Hexalith.FrontComposer.Shell.State.CommandPalette;

namespace Hexalith.FrontComposer.Shell.Tests.State.CommandPalette;

// CA1062 fires on the NonNull<T>.Get property access; the FsCheck `NonNull<string>` constrains
// the generator to non-null strings, and `ArgumentNullException.ThrowIfNull` validates the .Get
// value at method entry. Pragma-disable is the pragmatic fix — the analyzer's callsite-level
// flow analysis doesn't see through the struct wrapper. Scoped to this file only.
#pragma warning disable CA1062
public class PaletteScorerPropertyTests {
    // Pass-5 P19 — previously coalesced nulls on raw `string` parameters inside the body, which
    // neutered FsCheck's null generator and made the property vacuous. Using `NonNull<string>`
    // constrains the generator to produce non-null strings so every shrink actually exercises
    // the scorer. Null handling itself is verified by Score_EmptyInputs_ReturnsZero [Fact].
    [Property]
    public bool ScoreIsDeterministic(NonNull<string> query, NonNull<string> candidate) {
        ArgumentNullException.ThrowIfNull(query.Get);
        ArgumentNullException.ThrowIfNull(candidate.Get);
        int first = PaletteScorer.Score(query.Get, candidate.Get);
        int second = PaletteScorer.Score(query.Get, candidate.Get);
        return first == second;
    }

    // Pass-5 P18 — promoted from a single-example [Fact] to an FsCheck [Property] that exercises
    // arbitrary candidates. Invariant: when both lengths land in the prefix band, the longer
    // prefix scores strictly higher per PaletteScorer's 100 + 2×prefix-length formula.
    // Candidates shorter than 2 characters are vacuously skipped.
    [Property]
    public bool ScoreIsMonotonicOnPrefixLength(NonNull<string> candidate) {
        ArgumentNullException.ThrowIfNull(candidate.Get);
        string c = candidate.Get;
        if (c.Length < 2) {
            return true;
        }

        int shorter = PaletteScorer.Score(c.Substring(0, 1), c);
        int longer = PaletteScorer.Score(c.Substring(0, 2), c);
        return longer > shorter;
    }
}
#pragma warning restore CA1062
