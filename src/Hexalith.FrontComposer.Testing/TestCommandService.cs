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
    private long _dispatchSequence;
    private TestCommandConfiguration _configuration = new(
        TestCommandOutcome.Success,
        "Command rejected by the configured test outcome.",
        "Change the test command or configure a successful outcome.");

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

    /// <summary>Configures subsequent dispatches to complete successfully.</summary>
    public void Succeed() => Volatile.Write(ref _configuration, Volatile.Read(ref _configuration) with { Outcome = TestCommandOutcome.Success });

    /// <summary>Configures subsequent dispatches to be rejected through the production rejection contract.</summary>
    public void Reject(string reason, string resolution) {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        ArgumentException.ThrowIfNullOrWhiteSpace(resolution);
        Volatile.Write(ref _configuration, new(TestCommandOutcome.Rejected, reason, resolution));
    }

    /// <summary>Configures subsequent dispatches to throw a deterministic timeout without waiting.</summary>
    public void Timeout() => Volatile.Write(ref _configuration, Volatile.Read(ref _configuration) with { Outcome = TestCommandOutcome.Timeout });

    /// <summary>Configures subsequent dispatches to stop at the Syncing lifecycle state.</summary>
    public void StallAtSyncing() => Volatile.Write(ref _configuration, Volatile.Read(ref _configuration) with { Outcome = TestCommandOutcome.StallAtSyncing });

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

        long sequence = Interlocked.Increment(ref _dispatchSequence);
        TestCommandConfiguration configuration = Volatile.Read(ref _configuration);
        TestCommandOutcome outcome = configuration.Outcome;
        string messageId = $"test-message-{sequence:0000}";
        string correlationId = $"test-correlation-{sequence:0000}";
        CommandLifecycleState[] states = outcome switch {
            TestCommandOutcome.Success => [CommandLifecycleState.Acknowledged, CommandLifecycleState.Syncing, CommandLifecycleState.Confirmed],
            TestCommandOutcome.Rejected => [CommandLifecycleState.Rejected],
            TestCommandOutcome.Timeout => [CommandLifecycleState.Acknowledged, CommandLifecycleState.Syncing],
            TestCommandOutcome.StallAtSyncing => [CommandLifecycleState.Acknowledged, CommandLifecycleState.Syncing],
            _ => throw new InvalidOperationException($"Unsupported command outcome '{outcome}'."),
        };

        List<CommandLifecycleState> emittedStates = [];
        try {
            foreach (CommandLifecycleState state in states) {
                emittedStates.Add(state);
                onLifecycleChange?.Invoke(state, messageId);
            }
        }
        catch (Exception ex) {
            EnqueueBounded(CreateEvidence(command, messageId, correlationId, "LifecycleCallbackFailed", emittedStates));
            if (ex is OperationCanceledException canceled) {
                CancellationToken canceledToken = canceled.CancellationToken.IsCancellationRequested
                    ? canceled.CancellationToken
                    : new CancellationToken(canceled: true);
                return Task.FromCanceled<CommandResult>(canceledToken);
            }

            return Task.FromException<CommandResult>(ex);
        }

        EnqueueBounded(CreateEvidence(
            command,
            messageId,
            correlationId,
            outcome switch {
                TestCommandOutcome.Success => CommandResultStatus.Accepted,
                TestCommandOutcome.Rejected => CommandResultStatus.Rejected,
                _ => outcome.ToString(),
            },
            states));

        return outcome switch {
            TestCommandOutcome.Rejected => Task.FromException<CommandResult>(new CommandRejectedException(configuration.RejectionReason, configuration.RejectionResolution)),
            TestCommandOutcome.Timeout => Task.FromException<CommandResult>(new TimeoutException("The configured test command timed out.")),
            _ => Task.FromResult(new CommandResult(messageId, CommandResultStatus.Accepted, correlationId)),
        };
    }

    private CommandDispatchEvidence CreateEvidence<TCommand>(
        TCommand command,
        string messageId,
        string correlationId,
        string status,
        IReadOnlyList<CommandLifecycleState> states)
        where TCommand : class
        => new(
            typeof(TCommand).FullName ?? typeof(TCommand).Name,
            _userContext.TenantId,
            _userContext.UserId,
            _pageContext.BoundedContext,
            _pageContext.CommandName,
            messageId,
            correlationId,
            status,
            states,
            _options.TimeProvider.GetUtcNow(),
            RedactedEvidenceFormatter.Format(command, _options));

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
