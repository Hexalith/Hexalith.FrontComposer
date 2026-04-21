namespace Hexalith.FrontComposer.Shell.State.Navigation;

/// <summary>
/// Dispatched when the navigation manager observes a route change that updates the bounded
/// context (Story 3-4 D7 / Task 2.1a). Drives the palette's contextual scoring bonus.
/// </summary>
/// <param name="NewBoundedContext">The new bounded-context segment, or <see langword="null"/> on home / non-domain routes.</param>
public sealed record BoundedContextChangedAction(string? NewBoundedContext);
