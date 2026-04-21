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
    [InlineData("  g   h  ", "g h")]
    public void Normalize_AcceptsBareChordWithExactSingleSpace(string input, string expected) {
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
}
