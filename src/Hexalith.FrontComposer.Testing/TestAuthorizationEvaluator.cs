using System.Collections.Concurrent;

using Hexalith.FrontComposer.Shell.Services.Authorization;

namespace Hexalith.FrontComposer.Testing;

/// <summary>Deterministic, fail-closed authorization evaluator for generated component tests.</summary>
public sealed class TestAuthorizationEvaluator : ICommandAuthorizationEvaluator {
    private readonly ConcurrentDictionary<string, Func<CommandAuthorizationRequest, CommandAuthorizationDecision>> _policies = new(StringComparer.Ordinal);
    private long _sequence;

    /// <summary>Configures an allowed policy.</summary>
    public void Allow(string policyName) => Configure(policyName, CommandAuthorizationReason.None);

    /// <summary>Configures a denied policy.</summary>
    public void Deny(string policyName) => Configure(policyName, CommandAuthorizationReason.Denied);

    /// <summary>Configures a pending policy.</summary>
    public void Pending(string policyName) => Configure(policyName, CommandAuthorizationReason.Pending);

    /// <summary>Configures a failed-closed policy reason, including unauthenticated and handler-failed states.</summary>
    public void Block(string policyName, CommandAuthorizationReason reason) {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyName);
        if (reason is CommandAuthorizationReason.None or CommandAuthorizationReason.Denied or CommandAuthorizationReason.Pending) {
            throw new ArgumentOutOfRangeException(nameof(reason), reason, "Block requires a failed-closed authorization reason.");
        }

        _policies[policyName] = request => CommandAuthorizationDecision.Blocked(reason, NextCorrelationId());
    }

    /// <summary>Configures a per-request policy decision.</summary>
    public void DecideWith(string policyName, Func<CommandAuthorizationRequest, CommandAuthorizationDecision> callback) {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyName);
        ArgumentNullException.ThrowIfNull(callback);
        _policies[policyName] = callback;
    }

    /// <inheritdoc />
    public Task<CommandAuthorizationDecision> EvaluateAsync(CommandAuthorizationRequest request, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(request.PolicyName)) {
            return Task.FromResult(CommandAuthorizationDecision.Blocked(CommandAuthorizationReason.MissingPolicy, NextCorrelationId()));
        }

        CommandAuthorizationDecision decision;
        if (!_policies.TryGetValue(request.PolicyName, out Func<CommandAuthorizationRequest, CommandAuthorizationDecision>? callback)) {
            decision = CommandAuthorizationDecision.Blocked(CommandAuthorizationReason.MissingPolicy, NextCorrelationId());
        }
        else {
            try {
                decision = callback(request)
                    ?? CommandAuthorizationDecision.Blocked(CommandAuthorizationReason.HandlerFailed, NextCorrelationId());
            }
            catch (OperationCanceledException canceled) {
                CancellationToken canceledToken = canceled.CancellationToken.IsCancellationRequested
                    ? canceled.CancellationToken
                    : new CancellationToken(canceled: true);
                return Task.FromCanceled<CommandAuthorizationDecision>(canceledToken);
            }
            catch (Exception) {
                decision = CommandAuthorizationDecision.Blocked(CommandAuthorizationReason.HandlerFailed, NextCorrelationId());
            }
        }

        return Task.FromResult(decision);
    }

    /// <summary>Clears configured policies.</summary>
    public void Reset() => _policies.Clear();

    private void Configure(string policyName, CommandAuthorizationReason reason) {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyName);
        _policies[policyName] = request => reason switch {
            CommandAuthorizationReason.None => CommandAuthorizationDecision.Allowed(NextCorrelationId()),
            CommandAuthorizationReason.Denied => CommandAuthorizationDecision.Denied(NextCorrelationId()),
            CommandAuthorizationReason.Pending => CommandAuthorizationDecision.Pending(NextCorrelationId()),
            _ => CommandAuthorizationDecision.Blocked(reason, NextCorrelationId()),
        };
    }

    private string NextCorrelationId() => $"test-authorization-{Interlocked.Increment(ref _sequence):0000}";
}
