using Hexalith.FrontComposer.Shell.Services;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

// Route focus must resume on the component synchronization context.
#pragma warning disable CA2007

namespace Hexalith.FrontComposer.Shell.Components.Layout;

/// <summary>Coordinates route-heading focus after the routed view has rendered.</summary>
public sealed partial class FcRouteFocus : ComponentBase, IAsyncDisposable
{
    private const string FocusModulePath = "./_content/Hexalith.FrontComposer.Shell/js/fc-focus.js";
    private IJSObjectReference? _focusModule;
    private DotNetObjectReference<FcRouteFocus>? _selfReference;
    private string? _failedRouteKey;
    private long _failedAttemptVersion = -1;
    private string? _renderedRouteKey;
    private bool _showUnavailable;
    private bool _unavailableNeedsFocus;

    [Inject] private IJSRuntime JS { get; set; } = default!;

    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [Inject] private NavigationFailureNotifier NavigationFailure { get; set; } = default!;

    /// <summary>The current route URI, used to rerun focus after route changes.</summary>
    [Parameter] public string RouteKey { get; set; } = string.Empty;

    /// <summary>Whether this owner focuses a route heading. Disable when an unavailable-route component owns focus.</summary>
    [Parameter] public bool FocusHeading { get; set; } = true;

    /// <inheritdoc />
    protected override void OnInitialized() => NavigationFailure.RegisterFocusOwner();

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (!string.Equals(_renderedRouteKey, RouteKey, StringComparison.Ordinal))
        {
            _renderedRouteKey = RouteKey;
            _showUnavailable = false;
            _unavailableNeedsFocus = false;
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!FocusHeading) return;

        try
        {
            _focusModule ??= await JS.InvokeAsync<IJSObjectReference>("import", FocusModulePath);
            if (_showUnavailable)
            {
                if (_unavailableNeedsFocus)
                {
                    _unavailableNeedsFocus = false;
                    await _focusModule.InvokeVoidAsync("focusOverlayEntry", "fc-route-unavailable-heading");
                }

                return;
            }

            _selfReference ??= DotNetObjectReference.Create(this);
            await _focusModule.InvokeVoidAsync("focusRouteHeading", RouteKey, _selfReference, NavigationFailure.IsFailedReturn(RouteKey), NavigationFailure.OwnsAttempt(RouteKey), NavigationFailure.IsCanonicalAlias(RouteKey));
        }
        catch (Exception ex) when (ex is JSDisconnectedException or JSException or OperationCanceledException or InvalidOperationException)
        {
            // A pending activation must settle even when the browser cannot run focus interop.
            if (NavigationFailure.OwnsAttempt(RouteKey))
            {
                HandleRouteFailure(RouteKey, showUnavailable: false);
            }
        }
    }

    /// <summary>Records success only when the current route's heading has appeared in the DOM.</summary>
    [JSInvokable]
    public void ConfirmRouteHeading(string routeKey)
    {
        if (!_showUnavailable && string.Equals(routeKey, Navigation.Uri, StringComparison.Ordinal))
        {
            _failedRouteKey = null;
            NavigationFailure.ConfirmRoute(routeKey);
        }
    }

    /// <summary>Reports an activated route that did not render a heading.</summary>
    [JSInvokable]
    public void ReportRouteHeadingMissing(string routeKey)
    {
        if (!string.Equals(routeKey, Navigation.Uri, StringComparison.Ordinal)
            || (string.Equals(_failedRouteKey, routeKey, StringComparison.Ordinal)
                && _failedAttemptVersion == NavigationFailure.AttemptVersion))
        {
            return;
        }

        _failedRouteKey = routeKey;
        _failedAttemptVersion = NavigationFailure.AttemptVersion;
        HandleRouteFailure(routeKey, showUnavailable: true);
    }

    /// <summary>Reports a protected route that rendered its denied state.</summary>
    [JSInvokable]
    public void ReportRouteHeadingDenied(string routeKey)
    {
        if (string.Equals(routeKey, Navigation.Uri, StringComparison.Ordinal)
            && NavigationFailure.OwnsAttempt(routeKey))
        {
            HandleRouteFailure(routeKey, showUnavailable: false);
        }
    }

    private void HandleRouteFailure(string routeKey, bool showUnavailable)
    {
        if (!string.Equals(routeKey, Navigation.Uri, StringComparison.Ordinal))
        {
            return;
        }

        string? returnUri = NavigationFailure.ReturnUriForActivatedUnmatched(routeKey);
        if (!string.IsNullOrWhiteSpace(returnUri)
            && !string.Equals(returnUri, routeKey, StringComparison.OrdinalIgnoreCase))
        {
            NavigationFailure.ReportFailureAndPreserveReturn(returnUri);
            try
            {
                Navigation.NavigateTo(returnUri, replace: true);
                return;
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
            {
                NavigationFailure.CancelFailedReturn();
            }
        }
        else if (NavigationFailure.OwnsAttempt(routeKey))
        {
            NavigationFailure.ReportFailure();
        }

        if (showUnavailable)
        {
            _showUnavailable = true;
            _unavailableNeedsFocus = true;
            _ = InvokeAsync(StateHasChanged);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        NavigationFailure.UnregisterFocusOwner();
        _selfReference?.Dispose();
        if (_focusModule is not null)
        {
            try
            {
                await _focusModule.DisposeAsync();
            }
            catch (Exception ex) when (ex is JSDisconnectedException or JSException or OperationCanceledException)
            {
                // Browser circuit already ended.
            }
        }

        GC.SuppressFinalize(this);
    }
}
