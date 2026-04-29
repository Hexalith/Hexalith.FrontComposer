namespace Hexalith.FrontComposer.Shell.Tests.Infrastructure.EventStore.FaultInjection;

/// <summary>
/// Thrown by <see cref="FaultInjectingProjectionHubConnection"/> on disposal when scenario state
/// is not fully consumed (outstanding scripted actions, queued nudges, blocked operations, or
/// active subscriptions). The message lists checkpoint identifiers and bounded counts only —
/// no tenant values, group strings, exception messages, payloads, or connection identifiers.
/// </summary>
internal sealed class HarnessDisposalException : InvalidOperationException {
    public HarnessDisposalException(string message)
        : base(message) {
    }
}
