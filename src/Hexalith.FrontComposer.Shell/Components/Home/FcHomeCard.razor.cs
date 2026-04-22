using System.Collections.Immutable;

using Hexalith.FrontComposer.Shell.State.CapabilityDiscovery;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Hexalith.FrontComposer.Shell.Components.Home;

/// <summary>
/// Per-card render surface for <see cref="FcHomeDirectory"/>. Owns the button element with
/// keyboard (<see cref="HandleKeydownAsync"/>) and click (<see cref="HandleClickAsync"/>)
/// handlers; the parent supplies the activation callback via <see cref="OnActivated"/>.
/// </summary>
/// <remarks>
/// Story 3-5 Task 3 / AC7 — extracted so the Razor compiler treats the card markup as a real
/// component body and emits Blazor event bindings for <c>@onclick</c> / <c>@onkeydown</c>. The
/// previous inline <c>RenderFragment = __builder =>{ ... }</c> shape parsed the markup as C#
/// template output and silently dropped the event wiring (bUnit
/// <c>MissingEventHandlerException</c> observed during bmad-code-review 2026-04-22).
/// </remarks>
public partial class FcHomeCard {
    /// <summary>
    /// Gets or sets the per-card view-model rendered by this component.
    /// </summary>
    [Parameter, EditorRequired] public HomeCardModel Card { get; set; } = default!;

    /// <summary>
    /// Gets or sets the current per-user "seen capabilities" set used to gate the "New" badges.
    /// </summary>
    [Parameter, EditorRequired] public ImmutableHashSet<string> SeenCapabilities { get; set; } = default!;

    /// <summary>
    /// Gets or sets the hydration state; gates progressive disclosure of projection rows while
    /// the seed is still landing.
    /// </summary>
    [Parameter, EditorRequired] public CapabilityDiscoveryHydrationState HydrationState { get; set; }

    /// <summary>
    /// Gets or sets the activation callback invoked when the card is clicked OR activated via
    /// Enter / Space. The parent is responsible for dispatching the
    /// <see cref="CapabilityVisitedAction"/> and navigating.
    /// </summary>
    [Parameter, EditorRequired] public EventCallback<HomeCardModel> OnActivated { get; set; }

    private bool ShouldRenderProjectionRow(HomeProjectionRow row)
        => HydrationState == CapabilityDiscoveryHydrationState.Seeded || row.Count is not null;

    private Task HandleClickAsync() => OnActivated.InvokeAsync(Card);

    private async Task HandleKeydownAsync(KeyboardEventArgs args) {
        if (args is null) {
            return;
        }

        if (string.Equals(args.Key, "Enter", StringComparison.Ordinal)
            || string.Equals(args.Key, " ", StringComparison.Ordinal)) {
            await OnActivated.InvokeAsync(Card).ConfigureAwait(false);
        }
    }
}
