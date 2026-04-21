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
///   <item><description>Fuzzy subsequence (query chars appear in order in candidate, not necessarily contiguous) → <c>10 + matched − gaps</c>.</description></item>
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
        int matched = 0;
        int gaps = 0;
        int ci = 0;
        foreach (System.Text.Rune qr in q.EnumerateRunes()) {
            int found = IndexOfRune(c, qr, ci);
            if (found < 0) {
                return 0;
            }

            gaps += found - ci;
            ci = found + qr.Utf16SequenceLength;
            matched++;
        }

        // Clamp to non-negative: a long-gap subsequence can otherwise return a negative score,
        // which callers treat as "no match" but is a latent footgun for future consumers.
        return Math.Max(0, 10 + matched - gaps);
    }

    private static int IndexOfRune(string source, System.Text.Rune rune, int startIndex) {
        int i = startIndex;
        while (i < source.Length) {
            if (System.Text.Rune.TryGetRuneAt(source, i, out System.Text.Rune here) && here == rune) {
                return i;
            }

            i += char.IsHighSurrogate(source[i]) && i + 1 < source.Length ? 2 : 1;
        }

        return -1;
    }
}
