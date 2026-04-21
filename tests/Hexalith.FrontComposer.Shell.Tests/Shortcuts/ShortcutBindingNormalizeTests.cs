// Story 3-4 Task 10.2 (D1, D4 — AC1).
using Hexalith.FrontComposer.Contracts.Shortcuts;

using Microsoft.AspNetCore.Components.Web;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Shortcuts;

public class ShortcutBindingNormalizeTests {
    [Theory]
    [InlineData("ctrl+k", "ctrl+k")]
    [InlineData("CTRL+K", "ctrl+k")]
    [InlineData("Ctrl+K", "ctrl+k")]
    [InlineData("SHIFT+CTRL+K", "ctrl+shift+k")]
    [InlineData("alt+ctrl+shift+meta+k", "ctrl+shift+alt+meta+k")]
    public void Normalize_LowercasesAndReordersModifiers(string input, string expected) {
        ShortcutBinding.Normalize(input).ShouldBe(expected);
    }

    [Theory]
    [InlineData("g h", "g h")]
    [InlineData("G H", "g h")]
    [InlineData("  g h  ", "g h")] // leading/trailing whitespace trimmed; internal remains single-space
    public void Normalize_AcceptsBareChordWithExactSingleSpace(string input, string expected) {
        // P13 (2026-04-21 pass-4): the XML-doc contract is "exactly one space"; leading/trailing
        // whitespace is trimmed, but multi-space between the two chord keys must be rejected (see
        // Normalize_RejectsMultiSpaceChord).
        ShortcutBinding.Normalize(input).ShouldBe(expected);
    }

    [Theory]
    [InlineData("g")]
    [InlineData("a")]
    [InlineData("Z")]
    public void Normalize_RejectsBareSingleLetterWithoutModifier(string input) {
        Should.Throw<ArgumentException>(() => ShortcutBinding.Normalize(input));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Normalize_RejectsEmptyOrWhitespace(string input) {
        Should.Throw<ArgumentException>(() => ShortcutBinding.Normalize(input));
    }

    [Theory]
    [InlineData("hyper+k")]
    [InlineData("ctrl+shift+ctrl")] // last token is a modifier — invalid.
    public void Normalize_RejectsUnknownModifiersOrInvalidTrailingTokens(string input) {
        Should.Throw<ArgumentException>(() => ShortcutBinding.Normalize(input));
    }

    [Fact]
    public void TryFromKeyboardEvent_BuildsCanonicalLowercaseBinding() {
        ShortcutBinding.TryFromKeyboardEvent(new KeyboardEventArgs { Key = "K", CtrlKey = true }, out string b)
            .ShouldBeTrue();
        b.ShouldBe("ctrl+k");
    }

    [Fact]
    public void TryFromKeyboardEvent_ReturnsFalse_ForModifierOnlyPresses() {
        ShortcutBinding.TryFromKeyboardEvent(new KeyboardEventArgs { Key = "Control", CtrlKey = true }, out _)
            .ShouldBeFalse();
        ShortcutBinding.TryFromKeyboardEvent(new KeyboardEventArgs { Key = "Shift", ShiftKey = true }, out _)
            .ShouldBeFalse();
    }

    [Fact]
    public void FormatLabel_ProducesCapitalisedHumanLabel() {
        ShortcutBinding.FormatLabel("ctrl+k").ShouldBe("Ctrl+K");
        ShortcutBinding.FormatLabel("ctrl+shift+p").ShouldBe("Ctrl+Shift+P");
        ShortcutBinding.FormatLabel("g h").ShouldBe("G H");
    }

    [Theory]
    [InlineData("ctrl+g ctrl+h")]
    [InlineData("g ctrl+h")]
    [InlineData("ctrl+g h")]
    public void Normalize_RejectsChordPartsWithModifiers(string binding) {
        // Blind-Hunter / Edge-Hunter finding — the dispatcher fast-paths modifier-bearing combos to
        // single-key lookup (D4 sub-decision d), so a chord part containing `+` creates a dead
        // binding. Reject at registration time rather than silently accept.
        Should.Throw<ArgumentException>(() => ShortcutBinding.Normalize(binding));
    }

    [Theory]
    [InlineData("ctrl+ctrl+k")]
    [InlineData("shift+shift+p")]
    [InlineData("alt+ctrl+alt+k")]
    public void Normalize_RejectsDuplicateModifiers(string binding) {
        // P9 (2026-04-21 pass-4): duplicate modifiers used to collapse idempotently via the
        // hasModifier set-bit; reject to surface author typos rather than launder them.
        Should.Throw<ArgumentException>(() => ShortcutBinding.Normalize(binding));
    }

    [Theory]
    [InlineData("++k")]
    [InlineData("+k")]
    [InlineData("ctrl++k")]
    [InlineData("ctrl+")]
    public void Normalize_RejectsEmptyTokens(string binding) {
        // P10 (2026-04-21 pass-4): empty tokens from `+` splits used to be silently collapsed via
        // RemoveEmptyEntries; reject with a clear error to surface typos.
        Should.Throw<ArgumentException>(() => ShortcutBinding.Normalize(binding));
    }

    [Theory]
    [InlineData("gg hh")]
    [InlineData("go home")]
    public void Normalize_RejectsMultiCharChordTokens(string binding) {
        // P11 (2026-04-21 pass-4): chord parts must be single characters because the dispatcher
        // matches a single KeyboardEventArgs.Key against each token; multi-char chord tokens are
        // unreachable dead bindings.
        Should.Throw<ArgumentException>(() => ShortcutBinding.Normalize(binding));
    }

    [Theory]
    [InlineData("g g")]
    [InlineData("a a")]
    public void Normalize_RejectsIdenticalChordParts(string binding) {
        // P12 (2026-04-21 pass-4): self-chord is ambiguous with rapid double-press / auto-repeat
        // and produces bindings users cannot reliably invoke.
        Should.Throw<ArgumentException>(() => ShortcutBinding.Normalize(binding));
    }

    [Theory]
    [InlineData("g  h")]   // two spaces
    [InlineData("g   h")]  // three spaces
    public void Normalize_RejectsMultiSpaceChord(string binding) {
        // P13 (2026-04-21 pass-4): the XML-doc and test-name both promise "exactly one space";
        // multi-space chords used to be silently collapsed via RemoveEmptyEntries.
        Should.Throw<ArgumentException>(() => ShortcutBinding.Normalize(binding));
    }
}
