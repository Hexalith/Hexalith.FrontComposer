using System.Security.Claims;

using Hexalith.FrontComposer.Shell.Infrastructure.Tenancy;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;

namespace Hexalith.FrontComposer.Shell.Services.Authorization;

/// <summary>
/// Default implementation of <see cref="ICommandAuthorizationEvaluator"/>. Resolves the current
/// principal from <see cref="AuthenticationStateProvider"/> (Story 7-1 host-auth seam) and delegates
/// the policy decision to ASP.NET Core <see cref="IAuthorizationService"/>. Constructor-injects all
/// dependencies; service-locator pattern was removed during Story 7-3 Pass 2 review (P30 / DN1).
/// </summary>
public sealed class CommandAuthorizationEvaluator(
    IAuthorizationService authorizationService,
    AuthenticationStateProvider authenticationStateProvider,
    IFrontComposerTenantContextAccessor tenantContextAccessor,
    ILogger<CommandAuthorizationEvaluator> logger) : ICommandAuthorizationEvaluator {
    /// <inheritdoc/>
    public async Task<CommandAuthorizationDecision> EvaluateAsync(
        CommandAuthorizationRequest request,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(request);
        string correlationId = Guid.NewGuid().ToString("N");

        // Pre-trim once so the whitespace short-circuit and downstream resource agree.
        string? trimmedPolicy = request.PolicyName?.Trim();
        if (string.IsNullOrEmpty(trimmedPolicy)) {
            return CommandAuthorizationDecision.Allowed(correlationId, CommandAuthorizationReason.NoPolicy);
        }

        if (cancellationToken.IsCancellationRequested) {
            return CommandAuthorizationDecision.Blocked(CommandAuthorizationReason.Canceled, correlationId);
        }

        AuthenticationState? state;
        try {
            state = await authenticationStateProvider.GetAuthenticationStateAsync().ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            LogBlocked(request, CommandAuthorizationReason.Canceled, trimmedPolicy, correlationId);
            return CommandAuthorizationDecision.Blocked(CommandAuthorizationReason.Canceled, correlationId);
        }
        catch (Exception ex) when (IsRecoverable(ex)) {
            logger.LogWarning(
                ex,
                "Command authorization failed closed resolving authentication state. CommandType={CommandType} PolicyName={PolicyName} CorrelationId={CorrelationId}",
                request.CommandType.FullName ?? request.CommandType.Name,
                trimmedPolicy,
                correlationId);
            return CommandAuthorizationDecision.Blocked(CommandAuthorizationReason.HandlerFailed, correlationId);
        }

        ClaimsPrincipal? user = state?.User;
        if (user is null) {
            // No state resolved yet — typical during prerender / SSR-to-interactive transition.
            return CommandAuthorizationDecision.Pending(correlationId);
        }

        if (user.Identity is null || !user.Identity.IsAuthenticated) {
            LogBlocked(request, CommandAuthorizationReason.Unauthenticated, trimmedPolicy, correlationId);
            return CommandAuthorizationDecision.Blocked(CommandAuthorizationReason.Unauthenticated, correlationId);
        }

        TenantContextResult tenant;
        try {
            tenant = tenantContextAccessor.TryGetContext(operationKind: "command-authorization");
        }
        catch (Exception ex) when (IsRecoverable(ex)) {
            logger.LogWarning(
                ex,
                "Command authorization failed closed resolving tenant context. Reason={Reason} CommandType={CommandType} PolicyName={PolicyName} CorrelationId={CorrelationId}",
                CommandAuthorizationReason.StaleTenantContext,
                request.CommandType.FullName ?? request.CommandType.Name,
                trimmedPolicy,
                correlationId);
            return CommandAuthorizationDecision.Blocked(CommandAuthorizationReason.StaleTenantContext, correlationId);
        }

        if (!tenant.Succeeded) {
            LogBlocked(request, CommandAuthorizationReason.StaleTenantContext, trimmedPolicy, tenant.CorrelationId);
            return CommandAuthorizationDecision.Blocked(CommandAuthorizationReason.StaleTenantContext, tenant.CorrelationId);
        }

        var resource = new CommandAuthorizationResource(
            request.CommandType,
            trimmedPolicy,
            request.BoundedContext,
            request.DisplayLabel,
            request.SourceSurface,
            tenant.Context);

        try {
            AuthorizationResult? result = await authorizationService
                .AuthorizeAsync(user, resource, resource.PolicyName)
                .ConfigureAwait(false);

            // Re-check cancellation: the AuthorizeAsync overload used does not accept a token, so an
            // in-flight handler keeps running even when the caller cancels. Honor cancellation here.
            if (cancellationToken.IsCancellationRequested) {
                LogBlocked(request, CommandAuthorizationReason.Canceled, trimmedPolicy, correlationId);
                return CommandAuthorizationDecision.Blocked(CommandAuthorizationReason.Canceled, correlationId);
            }

            if (result is null) {
                logger.LogWarning(
                    "Command authorization failed closed: AuthorizationService returned null result. CommandType={CommandType} PolicyName={PolicyName} CorrelationId={CorrelationId}",
                    request.CommandType.FullName ?? request.CommandType.Name,
                    trimmedPolicy,
                    correlationId);
                return CommandAuthorizationDecision.Blocked(CommandAuthorizationReason.HandlerFailed, correlationId);
            }

            if (result.Succeeded) {
                return CommandAuthorizationDecision.Allowed(correlationId);
            }

            // ASP.NET's AuthorizationFailure distinguishes "policy unconfigured" (no failed
            // requirements present, no explicit Fail call) from "rule failed normally". A null
            // Failure or empty FailedRequirements without FailCalled means no requirement actually
            // evaluated — operators investigate as MissingPolicy. FailCalled or any failed
            // requirement means a handler explicitly rejected — that's Denied.
            bool hasFailedRequirement = result.Failure?.FailedRequirements?.Any() ?? false;
            bool failCalled = result.Failure?.FailCalled ?? false;
            if (!failCalled && !hasFailedRequirement) {
                LogBlocked(request, CommandAuthorizationReason.MissingPolicy, trimmedPolicy, correlationId);
                return CommandAuthorizationDecision.Blocked(CommandAuthorizationReason.MissingPolicy, correlationId);
            }

            LogBlocked(request, CommandAuthorizationReason.Denied, trimmedPolicy, correlationId);
            return CommandAuthorizationDecision.Denied(correlationId);
        }
        catch (OperationCanceledException) {
            LogBlocked(request, CommandAuthorizationReason.Canceled, trimmedPolicy, correlationId);
            return CommandAuthorizationDecision.Blocked(CommandAuthorizationReason.Canceled, correlationId);
        }
        catch (InvalidOperationException ex) {
            // Most commonly thrown when the requested policy name is not registered with
            // AuthorizationOptions ("No policy found: …"). Logged at Warning so operators can
            // diagnose deployment misconfigurations, but the principal/claims/tokens are never
            // included. Other InvalidOperationException causes (scope-resolution etc.) also fail
            // closed under MissingPolicy — operators investigate via the log message.
            logger.LogWarning(
                ex,
                "Command authorization failed closed. Reason={Reason} CommandType={CommandType} PolicyName={PolicyName} CorrelationId={CorrelationId}",
                CommandAuthorizationReason.MissingPolicy,
                request.CommandType.FullName ?? request.CommandType.Name,
                trimmedPolicy,
                correlationId);
            return CommandAuthorizationDecision.Blocked(CommandAuthorizationReason.MissingPolicy, correlationId);
        }
        catch (Exception ex) when (IsRecoverable(ex)) {
            logger.LogWarning(
                ex,
                "Command authorization failed closed. Reason={Reason} CommandType={CommandType} PolicyName={PolicyName} CorrelationId={CorrelationId}",
                CommandAuthorizationReason.HandlerFailed,
                request.CommandType.FullName ?? request.CommandType.Name,
                trimmedPolicy,
                correlationId);
            return CommandAuthorizationDecision.Blocked(CommandAuthorizationReason.HandlerFailed, correlationId);
        }
    }

    // Excludes corrupted-state and async-thread-abort exceptions in addition to OOM. Catching
    // these would mask process-fatal conditions; operators must see them surface.
    private static bool IsRecoverable(Exception ex)
        => ex is not (OutOfMemoryException
            or StackOverflowException
            or System.Threading.ThreadAbortException
            or AccessViolationException);

    private void LogBlocked(
        CommandAuthorizationRequest request,
        CommandAuthorizationReason reason,
        string trimmedPolicy,
        string correlationId)
        => logger.LogWarning(
            "Command authorization blocked. Reason={Reason} CommandType={CommandType} PolicyName={PolicyName} CorrelationId={CorrelationId}",
            reason,
            request.CommandType.FullName ?? request.CommandType.Name,
            trimmedPolicy,
            correlationId);
}
