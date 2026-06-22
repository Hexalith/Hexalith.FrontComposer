namespace Hexalith.FrontComposer.Shell.Components.Layout;

/// <summary>
/// Coordinates the accessible name of the shell's single content <c>main</c> landmark
/// (<c>#fc-main-content</c>) between a page's <see cref="FcContentLabel"/> declaration (which lives
/// inside <c>@ChildContent</c>, below the shell) and the shell that renders the landmark element
/// (handoff <c>frontcomposer-2026-06-19-page-header-landmarks-and-contract-hardening</c>, Requested
/// outcome 1/2).
/// </summary>
/// <remarks>
/// <para>
/// Instance-per-shell field cascaded as a fixed value, mirroring <see cref="FcPageLayoutCoordinator"/>
/// (the repo's established child→shell signalling pattern). A page drops
/// <c>&lt;FcContentLabel LabelledBy="@HeadingId" /&gt;</c> into its content to name the shell main
/// landmark by its visible route heading <b>without</b> an orphaned page-level <c>aria-labelledby</c>
/// on a non-landmark wrapper. The shell subscribes to <see cref="Changed"/> to re-render the
/// landmark's <c>aria-label</c> / <c>aria-labelledby</c>.
/// </para>
/// <para>
/// <b>Single-writer, last-writer-wins.</b> One <see cref="FcContentLabel"/> per page is the supported
/// shape. Disposing the declaration resets the coordinator so leaving the page restores the
/// unlabelled default (the implicit "main" name), exactly like <see cref="FcPageLayoutCoordinator"/>.
/// A shell <c>ContentLabel</c> / <c>ContentLabelledBy</c> parameter remains the app-wide static
/// fallback used when no page declares one.
/// </para>
/// </remarks>
internal sealed class FcContentLabelCoordinator {
    /// <summary>
    /// Raised when <see cref="Label"/> or <see cref="LabelledBy"/> changes so the shell can re-render
    /// the content landmark's accessible name. The shell subscribes on init and unsubscribes on dispose.
    /// </summary>
    internal event Action? Changed;

    /// <summary>The page-declared <c>aria-label</c> for the content landmark, or <see langword="null"/>.</summary>
    internal string? Label { get; private set; }

    /// <summary>The page-declared <c>aria-labelledby</c> id reference for the content landmark, or <see langword="null"/>.</summary>
    internal string? LabelledBy { get; private set; }

    /// <summary>Whether a page has declared an accessible name for the content landmark.</summary>
    internal bool HasDeclaration
        => !string.IsNullOrWhiteSpace(Label) || !string.IsNullOrWhiteSpace(LabelledBy);

    /// <summary>
    /// Sets the page-declared accessible name. Called by <see cref="FcContentLabel"/> on render.
    /// No-ops (no event) when both values are unchanged, so a shell re-render cannot re-enter via the
    /// child's render loop.
    /// </summary>
    /// <param name="label">The <c>aria-label</c> to apply, or <see langword="null"/>.</param>
    /// <param name="labelledBy">The <c>aria-labelledby</c> id reference to apply, or <see langword="null"/>.</param>
    internal void Set(string? label, string? labelledBy) {
        string? normalizedLabel = string.IsNullOrWhiteSpace(label) ? null : label;
        string? normalizedLabelledBy = string.IsNullOrWhiteSpace(labelledBy) ? null : labelledBy;
        if (string.Equals(Label, normalizedLabel, StringComparison.Ordinal)
            && string.Equals(LabelledBy, normalizedLabelledBy, StringComparison.Ordinal)) {
            return;
        }

        Label = normalizedLabel;
        LabelledBy = normalizedLabelledBy;
        Changed?.Invoke();
    }

    /// <summary>
    /// Clears the page-declared accessible name. Called on <see cref="FcContentLabel"/> dispose so
    /// leaving the page restores the unlabelled default.
    /// </summary>
    internal void Reset() => Set(null, null);
}
