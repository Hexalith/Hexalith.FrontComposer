using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Rendering;

namespace Counter.Web;

/// <summary>
/// Decorates the authorized sample command service so confirmed create/update commands can drive
/// the demo projection catch-up without changing the dispatched command instance.
/// </summary>
internal sealed class CounterSampleCommandService :
    ICommandServiceWithLifecycle,
    ICommandServiceWithLifecycleObservations
{
    private readonly ICommandServiceWithLifecycleObservations _inner;
    private readonly CounterCommandProjectionCatchUpChannel _catchUp;
    private readonly IUserContextAccessor _userContext;
    private readonly ILogger<CounterSampleCommandService> _logger;

    /// <summary>Initializes a new instance of the <see cref="CounterSampleCommandService"/> class.</summary>
    public CounterSampleCommandService(
        ICommandServiceWithLifecycleObservations inner,
        CounterCommandProjectionCatchUpChannel catchUp,
        IUserContextAccessor userContext,
        ILogger<CounterSampleCommandService> logger)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _catchUp = catchUp ?? throw new ArgumentNullException(nameof(catchUp));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Task<CommandResult> DispatchAsync<TCommand>(
        TCommand command,
        CancellationToken cancellationToken = default)
        where TCommand : class
        => DispatchAsync(command, (Action<CommandLifecycleObservation>?)null, cancellationToken);

    /// <inheritdoc />
    public Task<CommandResult> DispatchAsync<TCommand>(
        TCommand command,
        Action<CommandLifecycleObservation>? onLifecycleObservation,
        CancellationToken cancellationToken = default)
        where TCommand : class
    {
        ArgumentNullException.ThrowIfNull(command);
        string? tenantId = _userContext.TenantId;
        string? userId = _userContext.UserId;
        Action<string?>? publishConfirmed = string.IsNullOrWhiteSpace(tenantId)
            || string.IsNullOrWhiteSpace(userId)
                ? null
                : _catchUp.Capture(command, tenantId, userId);
        return _inner.DispatchAsync(
            command,
            observation =>
            {
                try
                {
                    onLifecycleObservation?.Invoke(observation);
                }
                finally
                {
                    if (observation.State == CommandLifecycleState.Confirmed)
                    {
                        CounterSampleCommandLog.ExactTargetCommandConfirmed(
                            _logger,
                            typeof(TCommand).Name,
                            nameof(CommandLifecycleState.Confirmed));
                        publishConfirmed?.Invoke(observation.MessageId);
                    }
                }
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<CommandResult> DispatchAsync<TCommand>(
        TCommand command,
        Action<CommandLifecycleState, string?>? onLifecycleChange,
        CancellationToken cancellationToken = default)
        where TCommand : class
        => DispatchAsync(
            command,
            observation => onLifecycleChange?.Invoke(observation.State, observation.MessageId),
            cancellationToken);
}
