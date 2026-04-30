namespace Hexalith.FrontComposer.Contracts.Shortcuts;

/// <summary>
/// Public projection of a single keyboard-shortcut registration (Story 3-4 D1 / D14).
/// </summary>
/// <remarks>
/// <para>
/// <see cref="NormalisedLabel"/> is pre-computed at registration time (e.g., <c>"Ctrl+K"</c> from
/// the lowercase binding <c>"ctrl+k"</c>) so the palette's "shortcuts" reference view does not
/// re-derive the human label per render.
/// </para>
/// <para>
/// <see cref="DescriptionKey"/> is a resource key (NOT a literal string) — the palette resolves it
/// through <see cref="Microsoft.Extensions.Localization.IStringLocalizer{T}"/> per user culture.
/// </para>
/// </remarks>
/// <param name="Binding">The normalised binding string (e.g., <c>"ctrl+k"</c>).</param>
/// <param name="DescriptionKey">The localisation resource key for the description column.</param>
/// <param name="NormalisedLabel">The human-readable rendering of the binding (e.g., <c>"Ctrl+K"</c>).</param>
/// <param name="RouteUrl">Optional shell route activated by the shortcut reference row.</param>
public sealed record ShortcutRegistration(string Binding, string DescriptionKey, string NormalisedLabel, string? RouteUrl = null);
