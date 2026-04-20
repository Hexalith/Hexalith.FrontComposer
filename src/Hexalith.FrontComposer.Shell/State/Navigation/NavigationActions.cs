using System.Collections.Immutable;

namespace Hexalith.FrontComposer.Shell.State.Navigation;

/// <summary>
/// Dispatched when the user manually toggles the sidebar collapsed state (Story 3-2 D9).
/// Flips <see cref="FrontComposerNavigationState.SidebarCollapsed"/>.
/// </summary>
/// <param name="CorrelationId">ULID correlation identifier for tracing.</param>
public sealed record SidebarToggledAction(string CorrelationId);

/// <summary>
/// Dispatched when the user expands or collapses a single nav category (Story 3-2 D11).
/// Sparse-by-default: collapsed entries are set, expanded entries are removed from the map.
/// </summary>
/// <param name="CorrelationId">ULID correlation identifier for tracing.</param>
/// <param name="BoundedContext">The bounded context key whose category was toggled.</param>
/// <param name="Collapsed"><see langword="true"/> to collapse; <see langword="false"/> to expand.</param>
public sealed record NavGroupToggledAction(string CorrelationId, string BoundedContext, bool Collapsed);

/// <summary>
/// Dispatched when <c>FcLayoutBreakpointWatcher</c> reports a new viewport tier (Story 3-2 D4 / ADR-036).
/// NEVER triggers persistence (ADR-037).
/// </summary>
/// <param name="NewTier">The new observed viewport tier.</param>
public sealed record ViewportTierChangedAction(ViewportTier NewTier);

/// <summary>
/// Dispatched when the user clicks a <c>FcCollapsedNavRail</c> button to force the sidebar open
/// (Story 3-2 D13). Idempotent — always sets <see cref="FrontComposerNavigationState.SidebarCollapsed"/>
/// to <see langword="false"/>.
/// </summary>
/// <param name="CorrelationId">ULID correlation identifier for tracing.</param>
public sealed record SidebarExpandedAction(string CorrelationId);

/// <summary>
/// Dispatched by <c>NavigationEffects.HandleAppInitialized</c> after loading the persisted blob
/// (Story 3-2 D15 / ADR-038). Reducer REPLACES <see cref="FrontComposerNavigationState.SidebarCollapsed"/>
/// and <see cref="FrontComposerNavigationState.CollapsedGroups"/> wholesale.
/// Does NOT trigger re-persistence (ADR-038).
/// </summary>
/// <param name="SidebarCollapsed">The hydrated sidebar collapsed flag.</param>
/// <param name="CollapsedGroups">The hydrated per-bounded-context collapsed flags.</param>
public sealed record NavigationHydratedAction(
    bool SidebarCollapsed,
    ImmutableDictionary<string, bool> CollapsedGroups);
