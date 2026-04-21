#if NET10_0_OR_GREATER
using System.Runtime.CompilerServices;

using Microsoft.AspNetCore.Components.Web;

namespace Hexalith.FrontComposer.Contracts.Shortcuts;

/// <summary>
/// Single framework surface for global keyboard-shortcut registration and dispatch
/// (Story 3-4 D1 / ADR-042). Adopters register additional shortcuts via the same surface so
/// framework + adopter bindings compete in one arbitration path with deterministic conflict
/// detection (Story 3-4 D3 — last-writer-wins, <c>HFC2108_ShortcutConflict</c> Information log).
/// </summary>
/// <remarks>
/// <para>
/// <b>Binding grammar (D1, normalised by <see cref="ShortcutBinding.Normalize(string)"/>):</b>
/// all-lowercase, modifiers in canonical order <c>ctrl+shift+alt+meta+&lt;key&gt;</c>; bare-chord
/// <c>g h</c> with exactly one space separating the two keys. Examples: <c>"ctrl+k"</c>,
/// <c>"ctrl+shift+p"</c>, <c>"g h"</c>. Single-letter bindings without a modifier (<c>"g"</c>)
/// throw <see cref="ArgumentException"/> at registration — they would fight every text input.
/// </para>
/// <para>
/// <b>Lifetime:</b> implementations are Scoped (per-circuit in Blazor Server, per-user in WASM —
/// mirrors Story 3-1 <c>IStorageService</c> ADR-030).
/// </para>
/// </remarks>
public interface IShortcutService {
    /// <summary>
    /// Registers a keyboard-shortcut handler. Returns an <see cref="IDisposable"/> whose disposal
    /// removes the registration. Duplicate normalised bindings replace the prior registration
    /// (last-writer-wins per D3) and emit an <c>HFC2108_ShortcutConflict</c> Information log.
    /// </summary>
    /// <param name="binding">The keyboard binding string (will be normalised via <see cref="ShortcutBinding.Normalize(string)"/>).</param>
    /// <param name="descriptionKey">The resource key resolved by <see cref="Microsoft.Extensions.Localization.IStringLocalizer{T}"/> when the palette renders the "shortcuts" reference view.</param>
    /// <param name="handler">The async callback invoked when the binding fires.</param>
    /// <param name="callSiteFile">Compiler-injected caller source file (do NOT pass explicitly).</param>
    /// <param name="callSiteLine">Compiler-injected caller line number (do NOT pass explicitly).</param>
    /// <returns>An <see cref="IDisposable"/> that removes the registration on disposal.</returns>
    IDisposable Register(
        string binding,
        string descriptionKey,
        Func<Task> handler,
        [CallerFilePath] string callSiteFile = "",
        [CallerLineNumber] int callSiteLine = 0);

    /// <summary>
    /// Returns a snapshot of all current registrations.
    /// </summary>
    /// <returns>A read-only list of currently registered shortcuts.</returns>
    IReadOnlyList<ShortcutRegistration> GetRegistrations();

    /// <summary>
    /// Normalises the incoming keyboard event to a binding string and invokes the matching handler.
    /// Two-key chords (<c>g h</c>) are tracked with a 1500 ms pending-first-key window
    /// (<see cref="TimeProvider"/>-driven so tests use <c>FakeTimeProvider</c>).
    /// </summary>
    /// <param name="e">The Blazor <see cref="KeyboardEventArgs"/> to normalise + dispatch.</param>
    /// <returns><see langword="true"/> when a registered handler was invoked; <see langword="false"/> otherwise.</returns>
    Task<bool> TryInvokeAsync(KeyboardEventArgs e);
}
#endif
