using System.Collections.Immutable;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.Shell.Badges;
using Hexalith.FrontComposer.Shell.Components.Layout;
using Hexalith.FrontComposer.Shell.Resources;
using Hexalith.FrontComposer.Shell.State.CapabilityDiscovery;
using Hexalith.FrontComposer.Shell.State.CommandPalette;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Localization;

namespace Hexalith.FrontComposer.Shell.Components.Home;

/// <summary>
/// Home directory component (Story 3-5 Task 3 / D8 / D15 / D16 / D17). Renders the welcome
/// header, urgency-sorted bounded-context cards, and the collapsed "Other areas" tail.
/// Subscribes to <see cref="FrontComposerCapabilityDiscoveryState"/> via
/// <see cref="Fluxor.Blazor.Web.Components.FluxorComponent"/> so live count + seen-set updates
/// re-render with no manual subscription wiring.
/// </summary>
public partial class FcHomeDirectory {
    internal const string GettingStartedGuideUrl = "https://github.com/Hexalith/Hexalith.FrontComposer/blob/main/README.md";

    [Inject] private IFrontComposerRegistry Registry { get; set; } = default!;

    [Inject] private IDispatcher Dispatcher { get; set; } = default!;

    [Inject] private IState<FrontComposerCapabilityDiscoveryState> DiscoveryState { get; set; } = default!;

    [Inject] private IStringLocalizer<FcShellResources> Localizer { get; set; } = default!;

    [Inject] private NavigationManager Nav { get; set; } = default!;

    /// <summary>
    /// Optional cascading authentication state — null for adopters that have not wired
    /// <c>AuthorizeRouteView</c>. Story 3-5 D19 — when the identity name is null/empty/whitespace
    /// the welcome string MUST resolve to <c>HomeWelcomeAnonymous</c>; "Welcome back, ." is forbidden.
    /// </summary>
    [CascadingParameter] public Task<AuthenticationState>? AuthenticationStateTask { get; set; }

    private string? _resolvedUserName;

    /// <inheritdoc />
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Reliability",
        "CA2007:Consider calling ConfigureAwait on the awaited task",
        Justification =
            "Fluxor's FLXW01 analyzer requires the bare `await base.OnInitializedAsync()` syntax "
            + "(no ConfigureAwait); CA2007 then flags every other awaited Task in this method. "
            + "Blazor lifecycle methods always run on the dispatcher thread, so suppressing here "
            + "preserves both invariants.")]
    protected override async Task OnInitializedAsync() {
        await base.OnInitializedAsync();

        if (AuthenticationStateTask is not null) {
            try {
                AuthenticationState authState = await AuthenticationStateTask;
                _resolvedUserName = authState.User?.Identity?.Name;
            }
            catch (Exception) {
                // Cascading auth-state task can fail (rare); fall back to anonymous welcome.
                _resolvedUserName = null;
            }
        }
    }

    private string WelcomeText() {
        if (string.IsNullOrWhiteSpace(_resolvedUserName)) {
            return Localizer["HomeWelcomeAnonymous"].Value;
        }

        return string.Format(
            System.Globalization.CultureInfo.CurrentUICulture,
            Localizer["HomeWelcomeTemplate"].Value,
            _resolvedUserName);
    }

    internal Task HandleBoundedContextClickAsync(HomeCardModel card) {
        DomainManifest manifest = card.Manifest;
        // D13 — synchronous dispatch BEFORE navigation so the reducer update lands first.
        Dispatcher.Dispatch(new CapabilityVisitedAction(CapabilityIds.ForBoundedContext(manifest.BoundedContext)));

        // Navigate to the first renderable projection (Story 3-2 nav convention). Bounded contexts
        // with no projections only get the badge dismissal — no navigation occurs.
        foreach (string projection in manifest.Projections) {
            if (!string.IsNullOrWhiteSpace(projection)) {
                string url = FrontComposerNavigation.BuildRoute(manifest.BoundedContext, projection);
                Nav.NavigateTo(url);
                break;
            }
        }

        return Task.CompletedTask;
    }

    private static IReadOnlyList<HomeCardModel> AggregateManifests(
        IReadOnlyList<DomainManifest> manifests,
        ImmutableDictionary<Type, int> counts) {
        List<HomeCardModel> result = new(manifests.Count);
        foreach (DomainManifest manifest in manifests) {
            List<HomeProjectionRow> rows = new(manifest.Projections.Count);
            int aggregate = 0;
            bool isReady = false;
            foreach (string projection in manifest.Projections) {
                if (string.IsNullOrWhiteSpace(projection)) {
                    continue;
                }

                int? count = LookupCount(projection, counts);
                if (count is int knownCount) {
                    aggregate += knownCount;
                    isReady = true;
                }

                rows.Add(new HomeProjectionRow(projection, count));
            }

            result.Add(new HomeCardModel(manifest, aggregate, isReady, rows));
        }

        return result;
    }

    private static int? LookupCount(string projectionFqn, ImmutableDictionary<Type, int> counts) {
        Type? resolved = ProjectionTypeResolver.Resolve(projectionFqn);
        return resolved is not null && counts.TryGetValue(resolved, out int count) ? count : null;
    }

}

/// <summary>
/// Per-card view-model: manifest + aggregated count + readiness marker + per-projection rows.
/// Public so the framework-owned <see cref="FcHomeCard"/> can accept it as a parameter
/// across assembly boundaries (Razor components' generated partial classes are public by default).
/// </summary>
/// <param name="Manifest">The source <see cref="DomainManifest"/>.</param>
/// <param name="AggregateCount">Sum of all projection counts inside this manifest.</param>
/// <param name="IsReady">Whether at least one projection count has arrived for this card.</param>
/// <param name="ProjectionRows">Per-projection (FQN, count) rows for the card body.</param>
public sealed record HomeCardModel(
    DomainManifest Manifest,
    int AggregateCount,
    bool IsReady,
    IReadOnlyList<HomeProjectionRow> ProjectionRows);

/// <summary>
/// Per-projection row for the card body.
/// </summary>
/// <param name="ProjectionFqn">The fully-qualified projection type name.</param>
/// <param name="Count">The current actionable-item count for the projection, or <see langword="null"/> while still pending.</param>
public sealed record HomeProjectionRow(string ProjectionFqn, int? Count);
