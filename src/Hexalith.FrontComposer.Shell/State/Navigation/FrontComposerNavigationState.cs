using System.Collections.Immutable;

namespace Hexalith.FrontComposer.Shell.State.Navigation;

/// <summary>
/// Fluxor state record for the framework-owned sidebar navigation (Story 3-2 D3 / ADR-037).
/// </summary>
/// <param name="SidebarCollapsed">Whether the sidebar is collapsed (user preference; persisted).</param>
/// <param name="CollapsedGroups">Per-bounded-context collapsed flag — sparse map (D11); persisted.</param>
/// <param name="CurrentViewport">Observed viewport tier — derived at runtime, NEVER persisted (ADR-037).</param>
/// <param name="CurrentBoundedContext">
/// The bounded-context segment of the current route — derived from <c>NavigationManager.Uri</c>
/// at route-change time, NEVER persisted (Story 3-4 D7 / D8 — drives the palette's contextual
/// scoring bonus). <see langword="null"/> when on the home route or a non-domain route.
/// </param>
public record FrontComposerNavigationState(
    bool SidebarCollapsed,
    ImmutableDictionary<string, bool> CollapsedGroups,
    ViewportTier CurrentViewport,
    string? CurrentBoundedContext = null);
