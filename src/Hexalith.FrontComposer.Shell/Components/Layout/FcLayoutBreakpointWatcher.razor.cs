using Fluxor;

using Hexalith.FrontComposer.Shell.State.Navigation;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Hexalith.FrontComposer.Shell.Components.Layout;

/// <summary>
/// Headless component that subscribes to <c>fc-layout-breakpoints.js</c> and dispatches
/// <see cref="ViewportTierChangedAction"/> whenever the browser crosses a breakpoint
/// (Story 3-2 D5 / D6 / ADR-036).
/// </summary>
/// <remarks>
/// Module-import failure (JSException, HubException, generic Exception) is caught and logged as
/// Warning; the framework stays usable at the <see cref="ViewportTier.Desktop"/> feature default
/// (D6 amended 2026-04-19). <see cref="OperationCanceledException"/> on circuit disposal logs at
/// Debug and swallows silently.
/// </remarks>
public partial class FcLayoutBreakpointWatcher : ComponentBase, IAsyncDisposable {
    private const string ModulePath = "./_content/Hexalith.FrontComposer.Shell/js/fc-layout-breakpoints.js";

    private IJSObjectReference? _module;
    private IJSObjectReference? _subscription;
    private DotNetObjectReference<FcLayoutBreakpointWatcher>? _selfRef;
    private bool _disposed;

    /// <summary>Injected JS runtime for module import + subscription.</summary>
    [Inject] private IJSRuntime JS { get; set; } = default!;

    /// <summary>Injected Fluxor dispatcher; fires <see cref="ViewportTierChangedAction"/>.</summary>
    [Inject] private IDispatcher Dispatcher { get; set; } = default!;

    /// <summary>Injected logger for Warning / Debug diagnostics on module-import failures.</summary>
    [Inject] private ILogger<FcLayoutBreakpointWatcher> Logger { get; set; } = default!;

    /// <summary>
    /// [JSInvokable] Callback invoked by <c>fc-layout-breakpoints.js</c> when the viewport tier
    /// crosses a boundary or on initial subscription emission.
    /// </summary>
    /// <param name="tier">The integer tier reported by the JS module (matches <see cref="ViewportTier"/>).</param>
    /// <returns>A completed task.</returns>
    [JSInvokable]
    public Task OnViewportTierChangedAsync(int tier) {
        // .NET 10 Enum.IsDefined requires the runtime value to match the enum's underlying type
        // exactly; ViewportTier is byte-backed, so cast before the check.
        if (tier < byte.MinValue || tier > byte.MaxValue
            || !Enum.IsDefined(typeof(ViewportTier), (byte)tier)) {
            Logger.LogWarning("FcLayoutBreakpointWatcher received unknown viewport tier {Tier}; ignoring.", tier);
            return Task.CompletedTask;
        }

        Dispatcher.Dispatch(new ViewportTierChangedAction((ViewportTier)tier));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender) {
        if (!firstRender) {
            return;
        }

        // Assign _selfRef BEFORE the import/subscribe call so a mid-subscribe throw still leaves
        // the DotNetObjectReference disposable from DisposeAsync (D6 amended 2026-04-19).
        _selfRef = DotNetObjectReference.Create(this);
        IJSObjectReference? module = null;
        IJSObjectReference? subscription = null;
        try {
            module = await JS.InvokeAsync<IJSObjectReference>("import", ModulePath).ConfigureAwait(false);
            if (_disposed) {
                await SafeDisposeAsync(module).ConfigureAwait(false);
                return;
            }

            subscription = await module.InvokeAsync<IJSObjectReference>("subscribe", _selfRef).ConfigureAwait(false);
            if (_disposed) {
                await SafeUnsubscribeAsync(module, subscription).ConfigureAwait(false);
                await SafeDisposeAsync(subscription).ConfigureAwait(false);
                await SafeDisposeAsync(module).ConfigureAwait(false);
                return;
            }

            _module = module;
            _subscription = subscription;
        }
        catch (OperationCanceledException) {
            // Circuit disposing mid-import — silent.
            await SafeDisposeAsync(subscription).ConfigureAwait(false);
            await SafeDisposeAsync(module).ConfigureAwait(false);
        }
        catch (Exception ex) {
            Logger.LogWarning(
                ex,
                "FcLayoutBreakpointWatcher subscribe failed; viewport stays at {Default}.",
                ViewportTier.Desktop);
            await SafeDisposeAsync(subscription).ConfigureAwait(false);
            await SafeDisposeAsync(module).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync() {
        _disposed = true;

        await SafeUnsubscribeAsync(_module, _subscription).ConfigureAwait(false);
        await SafeDisposeAsync(_subscription).ConfigureAwait(false);
        await SafeDisposeAsync(_module).ConfigureAwait(false);

        _selfRef?.Dispose();
        GC.SuppressFinalize(this);
    }

    private static async ValueTask SafeUnsubscribeAsync(IJSObjectReference? module, IJSObjectReference? subscription) {
        if (module is null || subscription is null) {
            return;
        }

        try {
            await module.InvokeVoidAsync("unsubscribe", subscription).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { }
        catch (JSDisconnectedException) { }
        catch (JSException) { }
    }

    private static async ValueTask SafeDisposeAsync(IJSObjectReference? reference) {
        if (reference is null) {
            return;
        }

        try { await reference.DisposeAsync().ConfigureAwait(false); }
        catch (OperationCanceledException) { }
        catch (JSDisconnectedException) { }
    }
}
