using Counter.Domain;
using Counter.Specimens;
using Counter.Specimens.Domain;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Components.Specimens;

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
    private readonly bool _mappedRejectionEnabled;
    private readonly AccessibilitySpecimenState? _accessibilityScenarios;
    private readonly bool _dispatchForbiddenEnabled;

    /// <summary>Initializes a new instance of the <see cref="CounterSampleCommandService"/> class.</summary>
    /// <param name="inner">The authorized sample command service.</param>
    /// <param name="catchUp">The sample projection catch-up channel.</param>
    /// <param name="userContext">The resolved sample tenant and user context.</param>
    /// <param name="logger">The sample logger.</param>
    /// <param name="configuration">The optional specimen configuration.</param>
    /// <param name="accessibilityScenarios">Optional fixed Test-only outcomes.</param>
    /// <param name="environment">The host environment that restricts specimen command outcomes to Test.</param>
    public CounterSampleCommandService(
        ICommandServiceWithLifecycleObservations inner,
        CounterCommandProjectionCatchUpChannel catchUp,
        IUserContextAccessor userContext,
        ILogger<CounterSampleCommandService> logger,
        IConfiguration? configuration = null,
        IHostEnvironment? environment = null,
        AccessibilitySpecimenState? accessibilityScenarios = null)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _catchUp = catchUp ?? throw new ArgumentNullException(nameof(catchUp));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        // This deterministic rejection is available only to the opt-in local Test specimen host.
        bool specimenOutcomesEnabled = environment?.IsEnvironment("Test") == true
            && configuration is not null
            && FrontComposerSpecimenRoutes.IsEnabled(configuration, environment);
        _accessibilityScenarios = specimenOutcomesEnabled ? accessibilityScenarios : null;
        _mappedRejectionEnabled = specimenOutcomesEnabled
            && string.Equals(configuration?["Hexalith:FrontComposer:Specimens:MappedRejectionEnabled"], "true", StringComparison.OrdinalIgnoreCase);
        _dispatchForbiddenEnabled = specimenOutcomesEnabled
            && string.Equals(configuration?["Hexalith:FrontComposer:Specimens:DispatchForbiddenEnabled"], "true", StringComparison.OrdinalIgnoreCase);
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
        if (command is AccessibilityZeroFieldCommand specimen && _accessibilityScenarios is { } scenarios)
        {
            scenarios.RecordDispatch();
            switch (scenarios.Outcome)
            {
                case "validation":
                    throw new CommandValidationException(ProblemDetailsPayload.Empty with {
                        ValidationErrors = new Dictionary<string, IReadOnlyList<string>> { ["TenantId"] = ["Review the specimen workspace and try again."] },
                    });
                case "returned-rejection":
                    return Task.FromResult(new CommandResult(specimen.MessageId, CommandResultStatus.Rejected));
                case "exception-rejection":
                    throw new CommandRejectedException("Specimen rejected", "Review the specimen and try again.");
                case "forbidden":
                    throw new CommandWarningException(CommandWarningKind.Forbidden, ProblemDetailsPayload.Empty);
                case "unavailable":
                    throw new CommandWarningException(CommandWarningKind.NotFound, ProblemDetailsPayload.Empty with { Detail = "This specimen item is unavailable." });
                case "blank-unavailable":
                    throw new CommandWarningException(CommandWarningKind.NotFound, ProblemDetailsPayload.Empty with { Detail = "  " });
                case "rate-limited":
                    throw new CommandWarningException(CommandWarningKind.RateLimited, ProblemDetailsPayload.Empty with { Detail = "The specimen is temporarily rate limited." }, TimeSpan.FromSeconds(17));
                case "blank-rate-limited":
                    throw new CommandWarningException(CommandWarningKind.RateLimited, ProblemDetailsPayload.Empty, TimeSpan.FromSeconds(17));
                case "transport":
                    throw new CommandWarningException(CommandWarningKind.RetryableDispatchFailed, ProblemDetailsPayload.Empty);
            }
        }
        if (_dispatchForbiddenEnabled && command is BatchIncrementCommand)
        {
            throw new CommandWarningException(
                CommandWarningKind.Forbidden,
                new ProblemDetailsPayload(
                    "Specimen backend denial",
                    "This specimen command was denied at dispatch.",
                    403,
                    null,
                    new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal),
                    []));
        }

        if (_mappedRejectionEnabled && command is BatchIncrementCommand { Amount: 3 })
        {
            throw CommandRejectedException.FromProblem(
                "Amount rejected",
                "Correct the linked amount and retry.",
                new ProblemDetailsPayload(
                    "Amount rejected",
                    "Correct the linked amount and retry.",
                    409,
                    null,
                    new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
                    {
                        [nameof(BatchIncrementCommand.Amount)] = ["Amount conflicts with the specimen limit."],
                    },
                    []));
        }

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
