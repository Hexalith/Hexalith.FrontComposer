namespace Hexalith.FrontComposer.Shell.State.CommandPalette;

/// <summary>
/// Single row in the palette result list (Story 3-4 D6 / D14).
/// </summary>
/// <param name="Category">The result category.</param>
/// <param name="DisplayLabel">The human-readable label (already localised).</param>
/// <param name="BoundedContext">The bounded-context name when applicable; <see cref="string.Empty"/> for synthetic rows.</param>
/// <param name="RouteUrl">The navigation target URL; <see langword="null"/> for shortcut-reference rows or synthetic entries that refill the palette instead.</param>
/// <param name="CommandTypeName">The fully qualified command type name (or the <c>"@"</c>-prefixed sentinel for framework-synthetic entries per D23); <see langword="null"/> for non-command rows.</param>
/// <param name="Score">Pre-computed score from <see cref="PaletteScorer"/> + contextual bonus.</param>
/// <param name="IsInCurrentContext">Whether the result lives in the current bounded context (drives the inline hint).</param>
/// <param name="ProjectionType">The projection runtime type when <see cref="Category"/> is <see cref="PaletteResultCategory.Projection"/>; <see langword="null"/> otherwise.</param>
/// <param name="DescriptionKey">Localisation resource key for the description column (shortcut category); <see langword="null"/> when not applicable.</param>
public sealed record PaletteResult(
    PaletteResultCategory Category,
    string DisplayLabel,
    string BoundedContext,
    string? RouteUrl,
    string? CommandTypeName,
    int Score,
    bool IsInCurrentContext,
    Type? ProjectionType = null,
    string? DescriptionKey = null);
