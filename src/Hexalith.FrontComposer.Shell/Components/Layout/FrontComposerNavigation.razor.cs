using System.Text;

using Fluxor;
using Fluxor.Blazor.Web.Components;

using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.Shell.State.Navigation;

using Microsoft.AspNetCore.Components;

namespace Hexalith.FrontComposer.Shell.Components.Layout;

/// <summary>
/// Framework-owned sidebar composing <c>FluentNavCategory</c> per registered <c>DomainManifest</c>
/// (Story 3-2 D1, D2, D9, D11, D16; AC1, AC3, AC6).
/// </summary>
/// <remarks>
/// <para>
/// Subscribes to <see cref="FrontComposerNavigationState"/> via
/// <see cref="FluxorComponent"/> so viewport-tier and collapsed-groups changes trigger
/// a minimal re-render.
/// </para>
/// <para>
/// Routes are derived via the convention in <see cref="BuildRoute"/>
/// (<c>/{boundedContextLowercase}/{projectionTypeKebabCase}</c>). Adopters needing a different
/// route replace the <c>FrontComposerShell.Navigation</c> slot (ADR-035 override path).
/// </para>
/// </remarks>
public partial class FrontComposerNavigation : FluxorComponent {
    [Inject] private IDispatcher Dispatcher { get; set; } = default!;

    [Inject] private IState<FrontComposerNavigationState> NavState { get; set; } = default!;

    [Inject] private IUlidFactory UlidFactory { get; set; } = default!;

    /// <summary>
    /// Builds the nav-item <c>Href</c> from the D2 convention:
    /// <c>/{boundedContextLowercase}/{projectionTypeNameKebabCase}</c>.
    /// Projection type name = segment after the last <c>.</c> (or whole string if no dot).
    /// </summary>
    /// <param name="boundedContext">The bounded-context string (rendered lowercase in the route).</param>
    /// <param name="projectionFqn">The fully-qualified projection type name.</param>
    /// <returns>The conventional route.</returns>
    public static string BuildRoute(string boundedContext, string projectionFqn) {
        ArgumentException.ThrowIfNullOrEmpty(boundedContext);
        ArgumentException.ThrowIfNullOrEmpty(projectionFqn);
        string typeName = LastSegment(projectionFqn);
        return $"/{boundedContext.ToLowerInvariant()}/{ToKebab(typeName)}";
    }

    /// <summary>
    /// Gets the projection type-name segment for display — the substring after the last <c>.</c>
    /// in the fully-qualified name (or the whole string if no dot is present). Namespace prefixes
    /// are stripped; the exact casing of the final type name is preserved. Story 4-1 will replace
    /// this with projection-role-hint-driven friendly names.
    /// </summary>
    /// <param name="projectionFqn">The fully-qualified projection type name.</param>
    /// <returns>The projection type-name segment after the last dot.</returns>
    public static string ProjectionLabel(string projectionFqn) {
        ArgumentException.ThrowIfNullOrEmpty(projectionFqn);
        return LastSegment(projectionFqn);
    }

    internal static List<string> RenderableProjections(DomainManifest manifest) {
        ArgumentNullException.ThrowIfNull(manifest);
        List<string> list = new(manifest.Projections.Count);
        foreach (string projection in manifest.Projections) {
            if (!string.IsNullOrWhiteSpace(projection)) {
                list.Add(projection);
            }
        }

        return list;
    }

    /// <summary>
    /// Test hook exposing the private handler so <c>NavGroupToggledDispatchesOnExpandedChange</c>
    /// can invoke the category callback without driving the FluentNavCategory event pipeline.
    /// </summary>
    /// <param name="boundedContext">The bounded context whose category changed.</param>
    /// <param name="expanded">The new expanded state reported by FluentNavCategory.</param>
    internal void OnGroupExpandedChangedForTest(string boundedContext, bool expanded)
        => OnGroupExpandedChanged(boundedContext, expanded);

    private bool ShouldRenderCollapsedRail() {
        FrontComposerNavigationState snapshot = NavState.Value;
        return snapshot.CurrentViewport == ViewportTier.CompactDesktop
            || (snapshot.CurrentViewport == ViewportTier.Desktop && snapshot.SidebarCollapsed);
    }

    private bool IsGroupCollapsed(string boundedContext)
        => NavState.Value.CollapsedGroups.TryGetValue(boundedContext, out bool collapsed) && collapsed;

    private void OnGroupExpandedChanged(string boundedContext, bool expanded)
        => Dispatcher.Dispatch(new NavGroupToggledAction(UlidFactory.NewUlid(), boundedContext, Collapsed: !expanded));

    private static string LastSegment(string fqn) {
        int lastDot = fqn.LastIndexOf('.');
        return lastDot < 0 ? fqn : fqn[(lastDot + 1)..];
    }

    private static string ToKebab(string pascal) {
        if (string.IsNullOrEmpty(pascal)) {
            return pascal;
        }

        StringBuilder sb = new(pascal.Length + 4);
        for (int i = 0; i < pascal.Length; i++) {
            char c = pascal[i];
            if (i > 0 && char.IsUpper(c)) {
                sb.Append('-');
            }

            sb.Append(char.ToLowerInvariant(c));
        }

        return sb.ToString();
    }
}
