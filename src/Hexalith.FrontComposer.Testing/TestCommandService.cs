using System.Collections.Concurrent;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Rendering;

namespace Hexalith.FrontComposer.Testing;

/// <summary>
/// Deterministic fake command service that records immutable evidence and never reaches EventStore.
/// </summary>
public sealed class TestCommandService : ICommandServiceWithLifecycle {
    private readonly ConcurrentQueue<CommandDispatchEvidence> _evidence = new();
    private readonly IUserContextAccessor _userContext;
    private readonly ICommandPageContext _pageContext;
    private readonly FrontComposerTestOptions _options;

    internal TestCommandService(
        IUserContextAccessor userContext,
        ICommandPageContext pageContext,
        FrontComposerTestOptions options) {
        _userContext = userContext;
        _pageContext = pageContext;
        _options = options;
    }

    /// <summary>Gets captured command dispatch evidence for this test context.</summary>
    public IReadOnlyList<CommandDispatchEvidence> Evidence => [.. _evidence];

    /// <inheritdoc />
    public Task<CommandResult> DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : class
        => DispatchAsync(command, null, cancellationToken);

    /// <inheritdoc />
    public Task<CommandResult> DispatchAsync<TCommand>(
        TCommand command,
        Action<CommandLifecycleState, string?>? onLifecycleChange,
        CancellationToken cancellationToken = default)
        where TCommand : class {
        cancellationToken.ThrowIfCancellationRequested();

        string messageId = $"test-message-{Evidence.Count + 1:0000}";
        string correlationId = $"test-correlation-{Evidence.Count + 1:0000}";
        CommandLifecycleState[] states =
        [
            CommandLifecycleState.Acknowledged,
            CommandLifecycleState.Syncing,
            CommandLifecycleState.Confirmed,
        ];

        foreach (CommandLifecycleState state in states) {
            onLifecycleChange?.Invoke(state, messageId);
        }

        CommandDispatchEvidence evidence = new(
            typeof(TCommand).FullName ?? typeof(TCommand).Name,
            _userContext.TenantId,
            _userContext.UserId,
            _pageContext.BoundedContext,
            _pageContext.CommandName,
            messageId,
            correlationId,
            "Accepted",
            states,
            _options.TimeProvider.GetUtcNow(),
            RedactedEvidenceFormatter.Format(command, _options));
        EnqueueBounded(evidence);

        return Task.FromResult(new CommandResult(messageId, "Accepted", correlationId));
    }

    /// <summary>Clears evidence retained by this fake service.</summary>
    public void Reset() {
        while (_evidence.TryDequeue(out _)) {
        }
    }

    private void EnqueueBounded(CommandDispatchEvidence evidence) {
        _evidence.Enqueue(evidence);
        while (_evidence.Count > _options.MaxEvidenceRecords && _evidence.TryDequeue(out _)) {
        }
    }
}
