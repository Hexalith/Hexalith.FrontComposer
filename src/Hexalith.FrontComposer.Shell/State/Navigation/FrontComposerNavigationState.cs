using System.Collections.Immutable;

namespace Hexalith.FrontComposer.Shell.State.Navigation;

/// <summary>
/// Fluxor state record for the framework-owned sidebar navigation (Story 3-2 D3 / ADR-037).
/// </summary>
/// <param name="SidebarCollapsed">Whether the sidebar is collapsed (user preference; persisted).</param>
/// <param name="CollapsedGroups">Per-bounded-context collapsed flag — sparse map (D11); persisted.</param>
/// <param name="CurrentViewport">Observed viewport tier — derived at runtime, NEVER persisted (ADR-037).</param>
public record FrontComposerNavigationState(
    bool SidebarCollapsed,
    ImmutableDictionary<string, bool> CollapsedGroups,
    ViewportTier CurrentViewport);
