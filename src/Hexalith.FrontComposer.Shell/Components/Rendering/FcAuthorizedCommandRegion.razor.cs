using Hexalith.FrontComposer.Shell.Services.Authorization;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hexalith.FrontComposer.Shell.Components.Rendering;

/// <summary>
/// Evaluator-backed authorization wrapper for command presentation regions.
/// </summary>
/// <remarks>
/// Story 7-3 Pass 4 DN-7-3-4-7: applies the same hardening matrix the Pass-3 form emitter
/// received — IDisposable + cancellation, sequence-number guard against concurrent
/// <see cref="OnParametersSetAsync"/>, <see cref="AuthenticationStateChanged"/> subscription with
/// re-evaluation, try/catch wrapping the evaluator (fail-closed on throw / null), synchronous
/// Pending stand-in with <see cref="ComponentBase.StateHasChanged"/>, and a single correlation
/// Id reused across the Pending and final decision so security logs can stitch the cycle.
/// </remarks>
public partial class FcAuthorizedCommandRegion : ComponentBase, IDisposable {
    [Inject] private ICommandAuthorizationEvaluator Evaluator { get; set; } = default!;

    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    [Inject] private ILogger<FcAuthorizedCommandRegion>? Logger { get; set; }

    [Parameter, EditorRequired] public Type CommandType { get; set; } = default!;

    [Parameter, EditorRequired] public string PolicyName { get; set; } = string.Empty;

    [Parameter] public string? BoundedContext { get; set; }

    [Parameter] public string DisplayLabel { get; set; } = string.Empty;

    [Parameter] public RenderFragment? Authorized { get; set; }

    [Parameter] public RenderFragment? Pending { get; set; }

    [Parameter] public RenderFragment? NotAuthorized { get; set; }

    private CommandAuthorizationDecision? _decision;
    private Type? _lastCommandType;
    private string? _lastPolicyName;
    private string? _lastBoundedContext;
    private CancellationTokenSource? _cts;
    private bool _disposed;
    private long _refreshSequence;
    private bool _authStateSubscribed;

    /// <inheritdoc />
    protected override void OnInitialized() {
        // Initialize the CTS synchronously so the first OnParametersSetAsync await observes a
        // non-default token and disposal during the await actually cancels in-flight evaluation.
        _cts ??= new CancellationTokenSource();

        if (!_authStateSubscribed) {
            AuthenticationStateProvider.AuthenticationStateChanged += OnAuthenticationStateChanged;
            _authStateSubscribed = true;
        }
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync() {
        if (_disposed) {
            return;
        }

        if (CommandType is null || string.IsNullOrWhiteSpace(PolicyName)) {
            // MissingPolicy is terminal for the cycle; seed the cache fields so subsequent
            // identical-parameter calls don't re-allocate a correlation Id (BH-07 / AA-20).
            _decision = CommandAuthorizationDecision.Blocked(
                CommandAuthorizationReason.MissingPolicy,
                Guid.NewGuid().ToString("N"));
            _lastCommandType = CommandType;
            _lastPolicyName = PolicyName;
            _lastBoundedContext = BoundedContext;
            return;
        }

        string trimmedPolicy = PolicyName.Trim();
        if (_decision is not null
            && _lastCommandType == CommandType
            && string.Equals(_lastPolicyName, trimmedPolicy, StringComparison.Ordinal)
            && string.Equals(_lastBoundedContext, BoundedContext, StringComparison.Ordinal)) {
            return;
        }

        await RefreshAsync(CommandType, trimmedPolicy, BoundedContext).ConfigureAwait(false);
    }

    private async Task RefreshAsync(Type commandType, string trimmedPolicy, string? boundedContext) {
        if (_disposed) {
            return;
        }

        long sequence = Interlocked.Increment(ref _refreshSequence);
        string correlationId = Guid.NewGuid().ToString("N");

        // Sync Pending stand-in + StateHasChanged so the Pending fragment actually renders during
        // the in-flight evaluator round-trip (BH-02 / EH-06). For fast in-process handlers the
        // Pending render is brief but still observable; for remote handlers it provides the
        // user-visible "Checking permission" cue.
        _decision = CommandAuthorizationDecision.Pending(correlationId);
        _lastCommandType = commandType;
        _lastPolicyName = trimmedPolicy;
        _lastBoundedContext = boundedContext;
        StateHasChanged();

        CommandAuthorizationDecision? next;
        try {
            next = await Evaluator.EvaluateAsync(
                new CommandAuthorizationRequest(
                    commandType,
                    trimmedPolicy,
                    null,
                    boundedContext,
                    string.IsNullOrWhiteSpace(DisplayLabel) ? commandType.Name : DisplayLabel,
                    CommandAuthorizationSurface.EmptyStateCta),
                _cts?.Token ?? CancellationToken.None).ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            // Component disposed or cancellation requested mid-call. Honour as transient cancel
            // — surface as Blocked(Canceled) so the NotAuthorized branch renders rather than
            // leaving the previous decision visible.
            next = CommandAuthorizationDecision.Blocked(CommandAuthorizationReason.Canceled, correlationId);
        }
        catch (Exception ex) {
            // Fail-closed on evaluator throw, null result, or any other unexpected failure. Without
            // this catch the exception escapes to the Blazor error boundary and tears the circuit.
            Logger?.LogWarning(
                ex,
                "FcAuthorizedCommandRegion authorization evaluation failed; falling back to Blocked. CommandType={CommandType} PolicyName={PolicyName} CorrelationId={CorrelationId}",
                commandType.FullName ?? commandType.Name,
                trimmedPolicy,
                correlationId);
            next = CommandAuthorizationDecision.Blocked(CommandAuthorizationReason.HandlerFailed, correlationId);
        }

        if (_disposed) {
            return;
        }

        if (Interlocked.Read(ref _refreshSequence) != sequence) {
            // A newer refresh started while this one was awaiting; drop the stale completion so
            // the late callback can't overwrite a fresher decision (BH-05 / EH-06).
            return;
        }

        _decision = next ?? CommandAuthorizationDecision.Blocked(CommandAuthorizationReason.HandlerFailed, correlationId);
        await InvokeAsync(StateHasChanged).ConfigureAwait(false);
    }

    private void OnAuthenticationStateChanged(Task<AuthenticationState> task) {
        if (_disposed) {
            return;
        }

        // Defensive — wrap the whole InvokeAsync invocation, not just the lambda body, so an
        // ObjectDisposedException from a torn-down RendererSynchronizationContext does not
        // escape as an unobserved task exception.
        try {
            _ = InvokeAsync(async () => {
                if (_disposed) {
                    return;
                }

                _ = await task.ConfigureAwait(true);

                if (CommandType is null || string.IsNullOrWhiteSpace(PolicyName)) {
                    return;
                }

                await RefreshAsync(CommandType, PolicyName.Trim(), BoundedContext).ConfigureAwait(true);
            });
        }
        catch (ObjectDisposedException) {
            // Renderer synchronization context torn down between the auth-state event firing and
            // this dispatch — circuit is gone, nothing to refresh.
        }
    }

    /// <inheritdoc />
    public void Dispose() {
        if (_disposed) {
            return;
        }

        _disposed = true;

        if (_authStateSubscribed) {
            AuthenticationStateProvider.AuthenticationStateChanged -= OnAuthenticationStateChanged;
            _authStateSubscribed = false;
        }

        try {
            _cts?.Cancel();
        }
        catch (ObjectDisposedException) {
            // Already disposed.
        }

        _cts?.Dispose();
        _cts = null;
    }
}
