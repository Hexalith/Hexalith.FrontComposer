using System;
using System.Collections.Generic;

namespace Hexalith.FrontComposer.Shell.Services.Feedback;

/// <summary>
/// Story 5-2 T5 — default <see cref="ICommandFeedbackPublisher"/>. Scoped per circuit so
/// subscribers do not leak across Blazor Server users (mirrors
/// <c>BadgeCountService</c> / <c>InlinePopoverRegistry</c> lifetime conventions).
/// </summary>
public sealed class CommandFeedbackPublisher : ICommandFeedbackPublisher {
    private readonly object _sync = new();
    private readonly List<Action<CommandFeedbackWarning>> _handlers = new();

    /// <inheritdoc />
    public void PublishWarning(CommandFeedbackWarning warning) {
        ArgumentNullException.ThrowIfNull(warning);

        Action<CommandFeedbackWarning>[] snapshot;
        lock (_sync) {
            snapshot = _handlers.ToArray();
        }

        foreach (Action<CommandFeedbackWarning> handler in snapshot) {
            try {
                handler(warning);
            }
            catch (System.Exception) {
                // Subscriber faults must not bring down the publisher or leak into the
                // command pipeline. Subscribers are responsible for their own logging.
            }
        }
    }

    /// <inheritdoc />
    public IDisposable Subscribe(Action<CommandFeedbackWarning> handler) {
        ArgumentNullException.ThrowIfNull(handler);

        lock (_sync) {
            _handlers.Add(handler);
        }

        return new Subscription(this, handler);
    }

    private void Unsubscribe(Action<CommandFeedbackWarning> handler) {
        lock (_sync) {
            _ = _handlers.Remove(handler);
        }
    }

    private sealed class Subscription : IDisposable {
        private readonly CommandFeedbackPublisher _publisher;
        private readonly Action<CommandFeedbackWarning> _handler;
        private int _disposed;

        public Subscription(CommandFeedbackPublisher publisher, Action<CommandFeedbackWarning> handler) {
            _publisher = publisher;
            _handler = handler;
        }

        public void Dispose() {
            if (System.Threading.Interlocked.Exchange(ref _disposed, 1) != 0) {
                return;
            }

            _publisher.Unsubscribe(_handler);
        }
    }
}
