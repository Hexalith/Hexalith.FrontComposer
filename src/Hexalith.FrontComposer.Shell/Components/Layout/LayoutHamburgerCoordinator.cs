using Microsoft.FluentUI.AspNetCore.Components;

namespace Hexalith.FrontComposer.Shell.Components.Layout;

/// <summary>
/// Coordinates access to the shared <see cref="FluentLayoutHamburger"/> instance rendered by
/// <see cref="FrontComposerShell"/> so sibling layout components can request the navigation drawer
/// on responsive tiers without reaching through the DOM.
/// </summary>
/// <remarks>
/// The coordinator is an instance-per-shell field cascaded as a fixed value. <see cref="FcHamburgerToggle"/>
/// registers its <c>@ref</c> on first render and unregisters on dispose, so <see cref="ShowAsync"/>
/// is a no-op when no toggle is currently rendered (Desktop tier with no manual collapse).
/// </remarks>
internal class LayoutHamburgerCoordinator {
    private FluentLayoutHamburger? _hamburger;

    /// <summary>
    /// Registers the current hamburger instance. Called once by <see cref="FcHamburgerToggle"/> on
    /// first render when the toggle is visible; cleared with <see langword="null"/> on toggle dispose.
    /// </summary>
    /// <param name="hamburger">The layout hamburger currently rendered by the shell, or <see langword="null"/> to clear.</param>
    internal void Register(FluentLayoutHamburger? hamburger)
        => _hamburger = hamburger;

    /// <summary>
    /// Opens the navigation drawer when the hamburger is available.
    /// </summary>
    /// <returns>A completed task if no hamburger is registered; otherwise the drawer open task.</returns>
    internal virtual Task ShowAsync() {
        FluentLayoutHamburger? hamburger = _hamburger;
        return hamburger is null ? Task.CompletedTask : hamburger.ShowAsync();
    }
}
