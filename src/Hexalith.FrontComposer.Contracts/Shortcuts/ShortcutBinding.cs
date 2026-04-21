#if NET10_0_OR_GREATER
using Microsoft.AspNetCore.Components.Web;
#endif

namespace Hexalith.FrontComposer.Contracts.Shortcuts;

/// <summary>
/// Pure helpers for normalising keyboard-shortcut bindings (Story 3-4 D1 / D4).
/// </summary>
/// <remarks>
/// <b>Grammar:</b> all-lowercase, modifiers in canonical order <c>ctrl+shift+alt+meta+key</c>;
/// bare chord <c>g h</c> with exactly one space.
/// </remarks>
public static class ShortcutBinding {
    private static readonly string[] _modifierOrder = ["ctrl", "shift", "alt", "meta"];

    /// <summary>
    /// Normalises a binding string to the canonical form used by the lookup dictionary.
    /// </summary>
    /// <param name="binding">The raw binding string supplied by an adopter or framework call site.</param>
    /// <returns>The normalised lowercase binding.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="binding"/> is null / empty / whitespace, an unknown modifier, or a single bare letter without a modifier.</exception>
    public static string Normalize(string binding) {
        if (string.IsNullOrWhiteSpace(binding)) {
            throw new ArgumentException("Binding must be a non-empty, non-whitespace string.", nameof(binding));
        }

        string trimmed = binding.Trim();
        foreach (char ch in trimmed) {
            if (char.IsWhiteSpace(ch) && ch != ' ') {
                throw new ArgumentException($"Binding may only use ASCII space as the chord separator; got '{binding}'. Tabs, NBSP, and other whitespace characters are rejected.", nameof(binding));
            }
        }

        if (trimmed.Contains(' ')) {
            // P13 (2026-04-21 pass-4): split WITHOUT RemoveEmptyEntries so multi-space chords like
            // "g  h" surface as 3 parts and fail the "exactly two parts" check — enforcing the
            // XML-doc's "exactly one space" contract that RemoveEmptyEntries previously laundered.
            string[] parts = trimmed.Split(' ');
            if (parts.Length != 2) {
                throw new ArgumentException($"Chord binding must have exactly two parts separated by a single space; got '{binding}'.", nameof(binding));
            }

            // Chord parts MUST be bare letters. The dispatcher fast-paths modifier-bearing combos
            // straight to single-key lookup (D4 sub-decision d), so a modifier-bearing chord part
            // produces an unreachable binding. Reject at registration time.
            if (parts[0].Contains('+') || parts[1].Contains('+')) {
                throw new ArgumentException($"Chord parts may not include modifiers ('+'); got '{binding}'. Use a bare chord like 'g h'.", nameof(binding));
            }

            // P11 (2026-04-21 pass-4): chord tokens must be single characters. Multi-char tokens
            // ("gg hh") can be registered but never dispatched — TryInvokeAsync normalises a single
            // KeyboardEventArgs.Key to a single-char token.
            if (parts[0].Length != 1 || parts[1].Length != 1) {
                throw new ArgumentException($"Chord parts must be single characters; got '{binding}'.", nameof(binding));
            }

            // P12 (2026-04-21 pass-4): identical chord parts ("g g") are ambiguous with rapid
            // double-press / auto-repeat; reject to avoid producing bindings users cannot reliably
            // invoke.
            if (string.Equals(parts[0], parts[1], StringComparison.OrdinalIgnoreCase)) {
                throw new ArgumentException($"Chord parts must differ; got '{binding}'.", nameof(binding));
            }

            string first = NormalizeSingleKey(parts[0], allowBareLetter: true);
            string second = NormalizeSingleKey(parts[1], allowBareLetter: true);
            return $"{first} {second}";
        }

        return NormalizeSingleKey(trimmed, allowBareLetter: false);
    }

    /// <summary>
    /// Renders a normalised binding (e.g., <c>"ctrl+k"</c>) as a human-readable label
    /// (e.g., <c>"Ctrl+K"</c>) for the palette's "shortcuts" reference view.
    /// </summary>
    /// <param name="normalisedBinding">The normalised binding string.</param>
    /// <returns>The human-readable label.</returns>
    public static string FormatLabel(string normalisedBinding) {
        if (string.IsNullOrWhiteSpace(normalisedBinding)) {
            throw new ArgumentException("Binding must be a non-empty, non-whitespace string.", nameof(normalisedBinding));
        }

        if (normalisedBinding.Contains(' ')) {
            string[] parts = normalisedBinding.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
            return $"{Capitalise(parts[0])} {Capitalise(parts[1])}";
        }

        string[] tokens = normalisedBinding.Split(new[] { '+' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < tokens.Length; i++) {
            tokens[i] = Capitalise(tokens[i]);
        }

        return string.Join("+", tokens);
    }

#if NET10_0_OR_GREATER
    /// <summary>
    /// Builds a normalised binding string from a Blazor <see cref="KeyboardEventArgs"/>. Returns
    /// <see langword="false"/> when the event is a modifier-only press (Shift / Ctrl / Alt / Meta
    /// alone) or a key the framework cannot map (e.g., <c>"Unidentified"</c>).
    /// </summary>
    /// <param name="e">The keyboard event to convert.</param>
    /// <param name="binding">The normalised binding string when the conversion succeeds.</param>
    /// <returns><see langword="true"/> when a non-empty binding was produced; otherwise <see langword="false"/>.</returns>
    public static bool TryFromKeyboardEvent(KeyboardEventArgs e, out string binding) {
        ArgumentNullException.ThrowIfNull(e);

        string? key = e.Key;
        if (string.IsNullOrWhiteSpace(key)) {
            binding = string.Empty;
            return false;
        }

        if (string.Equals(key, "Control", StringComparison.OrdinalIgnoreCase)
            || string.Equals(key, "Shift", StringComparison.OrdinalIgnoreCase)
            || string.Equals(key, "Alt", StringComparison.OrdinalIgnoreCase)
            || string.Equals(key, "Meta", StringComparison.OrdinalIgnoreCase)
            || string.Equals(key, "AltGraph", StringComparison.OrdinalIgnoreCase)
            || string.Equals(key, "Dead", StringComparison.OrdinalIgnoreCase)
            || string.Equals(key, "Process", StringComparison.OrdinalIgnoreCase)
            || string.Equals(key, "CapsLock", StringComparison.OrdinalIgnoreCase)
            || string.Equals(key, "NumLock", StringComparison.OrdinalIgnoreCase)
            || string.Equals(key, "ScrollLock", StringComparison.OrdinalIgnoreCase)
            || string.Equals(key, "Unidentified", StringComparison.OrdinalIgnoreCase)) {
            binding = string.Empty;
            return false;
        }

        string keyLower = key.ToLowerInvariant();
        List<string> parts = new(5);
        if (e.CtrlKey) {
            parts.Add("ctrl");
        }

        if (e.ShiftKey) {
            parts.Add("shift");
        }

        if (e.AltKey) {
            parts.Add("alt");
        }

        if (e.MetaKey) {
            parts.Add("meta");
        }

        parts.Add(keyLower);
        binding = string.Join("+", parts);
        return true;
    }
#endif

    private static string NormalizeSingleKey(string raw, bool allowBareLetter) {
        if (string.IsNullOrWhiteSpace(raw)) {
            throw new ArgumentException("Empty key part is not allowed.", nameof(raw));
        }

        string lower = raw.Trim().ToLowerInvariant();
        if (!lower.Contains('+')) {
            if (Array.IndexOf(_modifierOrder, lower) >= 0) {
                throw new ArgumentException($"Modifier '{lower}' cannot stand alone as a binding.", nameof(raw));
            }

            if (!allowBareLetter && lower.Length == 1 && char.IsLetter(lower[0])) {
                throw new ArgumentException($"Single bare letter '{lower}' is not allowed without a modifier (use a chord like 'g h' instead).", nameof(raw));
            }

            return lower;
        }

        // P10 (2026-04-21 pass-4): split WITHOUT RemoveEmptyEntries so empty tokens ("++k", "+k",
        // "ctrl++k", "ctrl+") surface as explicit validation errors rather than being laundered
        // into accidentally-valid bindings.
        string[] parts = lower.Split('+');
        if (parts.Length < 2) {
            throw new ArgumentException($"Modifier+key binding must include at least one modifier and one key; got '{raw}'.", nameof(raw));
        }

        foreach (string part in parts) {
            if (part.Length == 0) {
                throw new ArgumentException($"Modifier+key binding may not contain empty tokens (e.g. '++'); got '{raw}'.", nameof(raw));
            }
        }

        string keyToken = parts[parts.Length - 1];
        if (Array.IndexOf(_modifierOrder, keyToken) >= 0) {
            throw new ArgumentException($"Binding must end with a non-modifier key; got '{raw}'.", nameof(raw));
        }

        bool[] hasModifier = new bool[_modifierOrder.Length];
        for (int i = 0; i < parts.Length - 1; i++) {
            int modIndex = Array.IndexOf(_modifierOrder, parts[i]);
            if (modIndex < 0) {
                throw new ArgumentException($"Unknown modifier '{parts[i]}' in binding '{raw}'.", nameof(raw));
            }

            // P9 (2026-04-21 pass-4): reject duplicate modifiers ("ctrl+ctrl+k") rather than
            // silently collapsing them via the idempotent hasModifier set-bit.
            if (hasModifier[modIndex]) {
                throw new ArgumentException($"Modifier '{parts[i]}' appears more than once in binding '{raw}'.", nameof(raw));
            }

            hasModifier[modIndex] = true;
        }

        List<string> rebuilt = new(parts.Length);
        for (int i = 0; i < _modifierOrder.Length; i++) {
            if (hasModifier[i]) {
                rebuilt.Add(_modifierOrder[i]);
            }
        }

        rebuilt.Add(keyToken);
        return string.Join("+", rebuilt);
    }

    private static string Capitalise(string token)
        => string.IsNullOrEmpty(token)
            ? token
            : char.ToUpperInvariant(token[0]) + token.Substring(1);
}
