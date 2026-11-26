using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.Logging;

namespace Hexalith.FrontComposer.Shell.Services;

/// <summary>Dismisses the current generated popover when an existing Blazor circuit reconnects.</summary>
internal sealed partial class InlinePopoverCircuitHandler(
    InlinePopoverRegistry registry,
    ILogger<InlinePopoverCircuitHandler> logger) : CircuitHandler {
    private bool _hasConnected;

    /// <inheritdoc />
    public override async Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken) {
        if (!_hasConnected) {
            _hasConnected = true;
            return;
        }

        try {
            // Blazor waits for this handler before the new client can answer JS interop.
            if (await registry.DismissOnReconnectAsync().ConfigureAwait(false)) {
                LogPopoverDismissed(logger);
            }
        }
        catch (Exception ex) when (!ExceptionGuard.IsFatal(ex)) {
            LogDismissalFailed(logger, ex.GetType().Name);
        }
    }

    [LoggerMessage(EventId = 5856, EventName = "InlinePopoverDismissedOnReconnect", Level = LogLevel.Warning, Message = "Dismissed an open inline popover after circuit reconnection.")]
    private static partial void LogPopoverDismissed(ILogger logger);

    [LoggerMessage(EventId = 5857, EventName = "InlinePopoverReconnectDismissalFailed", Level = LogLevel.Warning, Message = "Could not dismiss an inline popover after circuit reconnection. ExceptionType={ExceptionType}")]
    private static partial void LogDismissalFailed(ILogger logger, string exceptionType);
}
