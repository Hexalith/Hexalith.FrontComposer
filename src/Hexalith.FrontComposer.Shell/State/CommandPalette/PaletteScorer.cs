namespace Hexalith.FrontComposer.Shell.State.CommandPalette;

/// <summary>
/// Pure-static three-band fuzzy matcher (Story 3-4 D7 / ADR-043).
/// </summary>
/// <remarks>
/// <para>
/// <b>Algorithm:</b>
/// </para>
/// <list type="bullet">
///   <item><description>Exact prefix → <c>100 + matchLen × 2</c>.</description></item>
///   <item><description>Contains substring → <c>50 + matchLen</c>.</description></item>
///   <item><description>Fuzzy subsequence (query chars appear in order in candidate, not necessarily contiguous) → <c>max(0, 10 + matched − gaps)</c>. Gaps are counted in runes, not UTF-16 code units (Pass-5 P7). Weak matches (gap count dominates) collapse to 0, preserving the effect-layer <c>score &lt;= 0</c> filter semantics per Pass-5 C1-D1(b) ratification.</description></item>
///   <item><description>No match → <c>0</c>.</description></item>
/// </list>
/// <para>
/// The <c>+15</c> contextual bonus (current bounded context) is applied by the EFFECT, NOT inside
/// this pure scorer — keeps the function dependency-free and unit-testable without DI mocks.
/// </para>
/// </remarks>
public static class PaletteScorer {
    /// <summary>
    /// Scores a candidate against a query. Higher is better; zero means no match.
    /// </summary>
    /// <param name="query">The user's query (case-insensitive, ordinal compare).</param>
    /// <param name="candidate">The candidate name (projection or command).</param>
    /// <returns>The score; <c>0</c> when no match.</returns>
    public static int Score(string query, string candidate) {
        if (string.IsNullOrEmpty(query) || string.IsNullOrEmpty(candidate)) {
            return 0;
        }

        string q = query.ToLowerInvariant();
        string c = candidate.ToLowerInvariant();

        // Exact prefix.
        if (c.StartsWith(q, StringComparison.Ordinal)) {
            return 100 + (q.Length * 2);
        }

        // Contains substring.
        if (c.Contains(q, StringComparison.Ordinal)) {
            return 50 + q.Length;
        }

        // Fuzzy subsequence — iterate by Rune so emoji / supplementary-plane queries don't
        // split into independent lo/hi-surrogate matches and produce nonsense scores.
        // Pass-5 P7 — count gaps in RUNES, not UTF-16 code units, so a supplementary-plane rune
        // between matches contributes a gap of 1, matching the rune-aware iteration intent.
        int matched = 0;
        int gaps = 0;
        int ci = 0;
        int ri = 0;
        foreach (System.Text.Rune qr in q.EnumerateRunes()) {
            if (!TryNextRuneMatch(c, qr, ci, ri, out int found, out int foundRuneIndex, out int afterMatchRuneIndex)) {
                return 0;
            }

            gaps += foundRuneIndex - ri;
            ci = found + qr.Utf16SequenceLength;
            ri = afterMatchRuneIndex;
            matched++;
        }

        // Pass-5 C1-D1 (b) — ratified 2026-04-21: weak fuzzy matches collapse to 0 ("Non-match → 0"
        // per spec §scope line 26) so the effect-layer `score <= 0` filter can drop noise. A
        // full subsequence match whose gap count dwarfs its matched length (e.g., "cv" vs a
        // 50-char string with one 'c' and one 'v' at opposite ends) is semantically a match but
        // not a useful one; treating it as no-match preserves palette signal-to-noise.
        return Math.Max(0, 10 + matched - gaps);
    }

    private static bool TryNextRuneMatch(
        string source,
        System.Text.Rune rune,
        int startIndex,
        int startRuneIndex,
        out int foundUtf16Index,
        out int foundRuneIndex,
        out int afterMatchRuneIndex) {
        int runeIndex = startRuneIndex;
        int i = startIndex;
        while (i < source.Length) {
            if (System.Text.Rune.TryGetRuneAt(source, i, out System.Text.Rune here)) {
                if (here == rune) {
                    foundUtf16Index = i;
                    foundRuneIndex = runeIndex;
                    afterMatchRuneIndex = runeIndex + 1;
                    return true;
                }

                i += here.Utf16SequenceLength;
                runeIndex++;
                continue;
            }

            // Malformed surrogate: skip one UTF-16 unit but count as a rune so gap accounting stays sane.
            i++;
            runeIndex++;
        }

        foundUtf16Index = -1;
        foundRuneIndex = -1;
        afterMatchRuneIndex = -1;
        return false;
    }
}
