using Microsoft.CodeAnalysis.CSharp;

namespace Hexalith.FrontComposer.SourceTools.Emitters;

/// <summary>
/// Single escaping helper for embedding adopter-supplied text inside generated C# string
/// literals. Emitters must use this instead of hand-rolled <c>Replace</c> chains.
/// </summary>
internal static class GeneratedLiteral {
    /// <summary>
    /// Escapes <paramref name="value"/> for embedding between double quotes in generated C#.
    /// </summary>
    /// <remarks>
    /// Delegates to Roslyn's <see cref="SymbolDisplay.FormatLiteral(string, bool)"/> with
    /// <c>quote: true</c> and strips the surrounding quotes, because the unquoted mode does
    /// <b>not</b> escape embedded double quotes (verified empirically, 2026-07-04), while the
    /// hand-rolled Replace chains this replaces missed Unicode line terminators
    /// (U+0085/U+2028/U+2029) and other control characters. The quoted mode handles both. A <see langword="null"/> or empty value is returned unchanged.
    /// </remarks>
    /// <param name="value">The raw adopter-supplied text.</param>
    /// <returns>The escaped literal body, without surrounding quotes.</returns>
    public static string Escape(string value) {
        if (string.IsNullOrEmpty(value)) {
            return value;
        }

        string quoted = SymbolDisplay.FormatLiteral(value, quote: true);
        return quoted.Substring(1, quoted.Length - 2);
    }
}
