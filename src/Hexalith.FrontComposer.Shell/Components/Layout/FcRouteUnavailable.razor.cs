using Hexalith.FrontComposer.Shell.Services;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

// Browser focus belongs to the component synchronization context.
#pragma warning disable CA2007

namespace Hexalith.FrontComposer.Shell.Components.Layout;

/// <summary>Shows a support-safe heading for a route with no registered destination.</summary>
public sealed partial class FcRouteUnavailable : ComponentBase
{
    private string? _handledUri;

    [Inject] private NavigationFailureNotifier NavigationFailure { get; set; } = default!;

    [Inject] private IJSRuntime JS { get; set; } = default!;

    [Inject] private NavigationManager Navigation { get; set; } = default!;

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        string routeUri = Navigation.Uri;
        if (string.Equals(_handledUri, routeUri, StringComparison.Ordinal))
        {
            return;
        }

        _handledUri = routeUri;

        string? returnUri = NavigationFailure.ReturnUriForActivatedUnmatched(routeUri);
        if (!string.IsNullOrWhiteSpace(returnUri)
            && !string.Equals(returnUri, routeUri, StringComparison.OrdinalIgnoreCase))
        {
            NavigationFailure.ReportFailureAndPreserveReturn(returnUri);
            try
            {
                Navigation.NavigateTo(returnUri, replace: true);
                return;
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
            {
                // If the prior route can no longer be reached, keep the safe unavailable page.
                NavigationFailure.CancelFailedReturn();
            }
        }

        NavigationFailure.BeginAttempt(null);
        NavigationFailure.ReportFailure();
        try
        {
            IJSObjectReference module = await JS.InvokeAsync<IJSObjectReference>("import", "./_content/Hexalith.FrontComposer.Shell/js/fc-focus.js");
            await module.InvokeVoidAsync("focusOverlayEntry", "fc-route-unavailable-heading");
            await module.DisposeAsync();
        }
        catch (Exception ex) when (ex is JSDisconnectedException or JSException or OperationCanceledException or InvalidOperationException)
        {
            // The unavailable heading remains visible if browser focus interop is unavailable.
        }
    }
}
