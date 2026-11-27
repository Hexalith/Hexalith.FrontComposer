using System.Runtime.ExceptionServices;
using System.Security.Claims;

using Hexalith.FrontComposer.Shell.Infrastructure.Telemetry;
using Hexalith.FrontComposer.Shell.Infrastructure.Tenancy;
using Hexalith.FrontComposer.Shell.Options;
using Hexalith.FrontComposer.Shell.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Shell.Services.Authorization;

/// <summary>
/// Default implementation of <see cref="ICommandAuthorizationEvaluator"/>. Resolves the current
/// principal from <see cref="AuthenticationStateProvider"/> (Story 7-1 host-auth seam) and delegates
/// the policy decision to ASP.NET Core <see cref="IAuthorizationService"/>. Constructor-injects all
/// dependencies; service-locator pattern was removed during Story 7-3 Pass 2 review (P30 / DN1).
/// </summary>
/// <param name="authorizationService">The host policy evaluator.</param>
/// <param name="authenticationStateProvider">The current scope's principal provider.</param>
/// <param name="tenantContextAccessor">The current scope's tenant context accessor.</param>
/// <param name="logger">The security event logger.</param>
/// <param name="options">The host's command authorization options.</param>
public sealed class CommandAuthorizationEvaluator(
    IAuthorizationService authorizationService,
    AuthenticationStateProvider authenticationStateProvider,
    IFrontComposerTenantContextAccessor tenantContextAccessor,
    ILogger<CommandAuthorizationEvaluator> logger,
    IOptions<FrontComposerAuthorizationOptions> options) : ICommandAuthorizationEvaluator {
    /// <summary>Creates an evaluator with the default command authorization options.</summary>
    /// <param name="authorizationService">The host policy evaluator.</param>
    /// <param name="authenticationStateProvider">The current scope's principal provider.</param>
    /// <param name="tenantContextAccessor">The current scope's tenant context accessor.</param>
    /// <param name="logger">The security event logger.</param>
    public CommandAuthorizationEvaluator(
        IAuthorizationService authorizationService,
        AuthenticationStateProvider authenticationStateProvider,
        IFrontComposerTenantContextAccessor tenantContextAccessor,
        ILogger<CommandAuthorizationEvaluator> logger)
        : this(authorizationService, authenticationStateProvider, tenantContextAccessor, logger,
            Microsoft.Extensions.Options.Options.Create(new FrontComposerAuthorizationOptions()))
    {
    }

    /// <inheritdoc/>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "BL0013", Justification = "This is a one-shot authorization decision: every evaluation resolves a fresh principal and retains no authentication state between calls; dispatch consumers reauthorize immediately before dispatch.")]
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

        int authenticationChanged = 0;
        void OnAuthenticationStateChanged(Task<AuthenticationState> stateTask)
            => Interlocked.Exchange(ref authenticationChanged, 1);
        authenticationStateProvider.AuthenticationStateChanged += OnAuthenticationStateChanged;
        try
        {
            AuthenticationState? state;
            Task<AuthenticationState>? authenticationStateTask = null;
            try
            {
                authenticationStateTask = authenticationStateProvider.GetAuthenticationStateAsync();
                state = await authenticationStateTask.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex)
            {
                if (ExceptionGuard.IsFatal(ex))
                {
                    throw;
                }

                ObserveLateFault(authenticationStateTask);
                LogBlocked(request, CommandAuthorizationReason.Canceled, trimmedPolicy, correlationId);
                return CommandAuthorizationDecision.Blocked(CommandAuthorizationReason.Canceled, correlationId);
            }
            catch (Exception ex) when (!ExceptionGuard.IsFatal(ex))
            {
                FrontComposerSecurityLog.AuthorizationAuthenticationStateFailed(
                    logger,
                    request.CommandType.FullName ?? request.CommandType.Name,
                    trimmedPolicy,
                    correlationId,
                    ex.GetType().FullName ?? "Exception");
                return CommandAuthorizationDecision.Blocked(CommandAuthorizationReason.HandlerFailed, correlationId);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                LogBlocked(request, CommandAuthorizationReason.Canceled, trimmedPolicy, correlationId);
                return CommandAuthorizationDecision.Blocked(CommandAuthorizationReason.Canceled, correlationId);
            }

            if (Volatile.Read(ref authenticationChanged) != 0)
            {
                LogBlocked(request, CommandAuthorizationReason.StaleTenantContext, trimmedPolicy, correlationId);
                return CommandAuthorizationDecision.Blocked(CommandAuthorizationReason.StaleTenantContext, correlationId);
            }

            ClaimsPrincipal? user = state?.User;
            if (user is null)
            {
                // No state resolved yet — typical during prerender / SSR-to-interactive transition.
                return CommandAuthorizationDecision.Pending(correlationId);
            }

            if (user.Identity is null || !user.Identity.IsAuthenticated)
            {
                LogBlocked(request, CommandAuthorizationReason.Unauthenticated, trimmedPolicy, correlationId);
                return CommandAuthorizationDecision.Blocked(CommandAuthorizationReason.Unauthenticated, correlationId);
            }

            if (!TryReadTenantContext(
                () => tenantContextAccessor.TryGetContext(operationKind: "command-authorization"),
                request,
                trimmedPolicy,
                correlationId,
                out TenantContextResult? tenant))
            {
                return CommandAuthorizationDecision.Blocked(CommandAuthorizationReason.StaleTenantContext, correlationId);
            }

            if (tenant is null || !tenant.Succeeded || tenant.Context is null || !tenant.Context.IsAuthenticated)
            {
                string staleCorrelationId = string.IsNullOrWhiteSpace(tenant?.CorrelationId)
                    ? correlationId
                    : tenant.CorrelationId;
                LogBlocked(request, CommandAuthorizationReason.StaleTenantContext, trimmedPolicy, staleCorrelationId);
                return CommandAuthorizationDecision.Blocked(CommandAuthorizationReason.StaleTenantContext, staleCorrelationId);
            }

            var resource = new CommandAuthorizationResource(
                request.CommandType,
                trimmedPolicy,
                request.BoundedContext,
                request.DisplayLabel,
                request.SourceSurface,
                tenant.Context);

            Task<AuthorizationResult>? authorizeTask = null;
            try
            {
                authorizeTask = authorizationService.AuthorizeAsync(user, resource, resource.PolicyName);
                AuthorizationResult? result = await authorizeTask.WaitAsync(cancellationToken).ConfigureAwait(false);

                // Re-check cancellation: the AuthorizeAsync overload used does not accept a token, so an
                // in-flight handler keeps running even when the caller cancels.
                if (cancellationToken.IsCancellationRequested)
                {
                    LogBlocked(request, CommandAuthorizationReason.Canceled, trimmedPolicy, correlationId);
                    return CommandAuthorizationDecision.Blocked(CommandAuthorizationReason.Canceled, correlationId);
                }

                if (result is null)
                {
                    FrontComposerSecurityLog.AuthorizationNullResult(
                        logger,
                        request.CommandType.FullName ?? request.CommandType.Name,
                        trimmedPolicy,
                        correlationId);
                    return CommandAuthorizationDecision.Blocked(CommandAuthorizationReason.HandlerFailed, correlationId);
                }

                if (Volatile.Read(ref authenticationChanged) != 0)
                {
                    LogBlocked(request, CommandAuthorizationReason.StaleTenantContext, trimmedPolicy, correlationId);
                    return CommandAuthorizationDecision.Blocked(CommandAuthorizationReason.StaleTenantContext, correlationId);
                }

                // Revalidate the tenant context before emitting audit logs or returning a policy decision so
                // context-only transitions without an AuthenticationStateChanged event still fail closed.
                if (!TryRevalidateTenantContext(
                    tenant.Context,
                    request,
                    trimmedPolicy,
                    correlationId,
                    out string staleCorrelationId))
                {
                    return CommandAuthorizationDecision.Blocked(CommandAuthorizationReason.StaleTenantContext, staleCorrelationId);
                }

                if (result.Succeeded)
                {
                    LogPolicySuccess(request, trimmedPolicy, correlationId);

                    // The optional audit sink runs host code, so keep the final cancellation, principal,
                    // and tenant-context revalidation checks after it.
                    if (cancellationToken.IsCancellationRequested)
                    {
                        LogBlocked(request, CommandAuthorizationReason.Canceled, trimmedPolicy, correlationId);
                        return CommandAuthorizationDecision.Blocked(CommandAuthorizationReason.Canceled, correlationId);
                    }

                    if (Volatile.Read(ref authenticationChanged) != 0)
                    {
                        LogBlocked(request, CommandAuthorizationReason.StaleTenantContext, trimmedPolicy, correlationId);
                        return CommandAuthorizationDecision.Blocked(CommandAuthorizationReason.StaleTenantContext, correlationId);
                    }

                    if (!TryRevalidateTenantContext(
                        tenant.Context,
                        request,
                        trimmedPolicy,
                        correlationId,
                        out staleCorrelationId))
                    {
                        return CommandAuthorizationDecision.Blocked(CommandAuthorizationReason.StaleTenantContext, staleCorrelationId);
                    }

                    return CommandAuthorizationDecision.Allowed(correlationId);
                }

                // ASP.NET's AuthorizationFailure distinguishes "policy unconfigured" (no failed
                // requirements present, no explicit Fail call) from "rule failed normally". A null
                // Failure or empty FailedRequirements without FailCalled means no requirement actually
                // evaluated — operators investigate as MissingPolicy. FailCalled or any failed
                // requirement means a handler explicitly rejected — that's Denied.
                bool hasFailedRequirement = result.Failure?.FailedRequirements?.Any() ?? false;
                bool failCalled = result.Failure?.FailCalled ?? false;
                if (!failCalled && !hasFailedRequirement)
                {
                    LogBlocked(request, CommandAuthorizationReason.MissingPolicy, trimmedPolicy, correlationId);
                    return CommandAuthorizationDecision.Blocked(CommandAuthorizationReason.MissingPolicy, correlationId);
                }

                LogBlocked(request, CommandAuthorizationReason.Denied, trimmedPolicy, correlationId);
                return CommandAuthorizationDecision.Denied(correlationId);
            }
            catch (OperationCanceledException ex)
            {
                if (ExceptionGuard.IsFatal(ex))
                {
                    throw;
                }

                ObserveLateFault(authorizeTask);
                LogBlocked(request, CommandAuthorizationReason.Canceled, trimmedPolicy, correlationId);
                return CommandAuthorizationDecision.Blocked(CommandAuthorizationReason.Canceled, correlationId);
            }
            catch (InvalidOperationException ex)
            {
                // Most commonly thrown when the requested policy name is not registered with
                // AuthorizationOptions ("No policy found: …"). Logged at Warning so operators can
                // diagnose deployment misconfigurations, but the principal/claims/tokens are never
                // included. Other InvalidOperationException causes (scope-resolution etc.) also fail
                // closed under MissingPolicy — operators investigate via the log message.
                FrontComposerSecurityLog.AuthorizationMissingPolicyFailed(
                    logger,
                    request.CommandType.FullName ?? request.CommandType.Name,
                    trimmedPolicy,
                    correlationId,
                    ex.GetType().FullName ?? "Exception");
                return CommandAuthorizationDecision.Blocked(CommandAuthorizationReason.MissingPolicy, correlationId);
            }
            catch (Exception ex) when (!ExceptionGuard.IsFatal(ex))
            {
                FrontComposerSecurityLog.AuthorizationHandlerFailed(
                    logger,
                    request.CommandType.FullName ?? request.CommandType.Name,
                    trimmedPolicy,
                    correlationId,
                    ex.GetType().FullName ?? "Exception");
                return CommandAuthorizationDecision.Blocked(CommandAuthorizationReason.HandlerFailed, correlationId);
            }
        }
        finally
        {
            authenticationStateProvider.AuthenticationStateChanged -= OnAuthenticationStateChanged;
        }
    }

    private bool TryRevalidateTenantContext(
        TenantContextSnapshot expected,
        CommandAuthorizationRequest request,
        string trimmedPolicy,
        string correlationId,
        out string staleCorrelationId)
    {
        if (!TryReadTenantContext(
            () => tenantContextAccessor.Revalidate(expected, operationKind: "command-authorization"),
            request,
            trimmedPolicy,
            correlationId,
            out TenantContextResult? revalidated))
        {
            staleCorrelationId = correlationId;
            return false;
        }

        if (!IsMatchingAuthenticatedContext(revalidated, expected))
        {
            staleCorrelationId = string.IsNullOrWhiteSpace(revalidated?.CorrelationId)
                ? correlationId
                : revalidated.CorrelationId;
            LogBlocked(request, CommandAuthorizationReason.StaleTenantContext, trimmedPolicy, staleCorrelationId);
            return false;
        }

        staleCorrelationId = correlationId;
        return true;
    }

    private static bool IsMatchingAuthenticatedContext(
        TenantContextResult? result,
        TenantContextSnapshot expected)
        => result is { Succeeded: true, Context: { IsAuthenticated: true } current }
            && string.Equals(current.TenantId, expected.TenantId, StringComparison.Ordinal)
            && string.Equals(current.UserId, expected.UserId, StringComparison.Ordinal);

    private bool TryReadTenantContext(
        Func<TenantContextResult> read,
        CommandAuthorizationRequest request,
        string trimmedPolicy,
        string correlationId,
        out TenantContextResult? result)
    {
        try
        {
            result = read();
            return true;
        }
        catch (Exception ex) when (!ExceptionGuard.IsFatal(ex))
        {
            FrontComposerSecurityLog.AuthorizationTenantContextFailed(
                logger,
                CommandAuthorizationReason.StaleTenantContext,
                request.CommandType.FullName ?? request.CommandType.Name,
                trimmedPolicy,
                correlationId,
                ex.GetType().FullName ?? "Exception");
            result = null;
            return false;
        }
    }

    private static void ObserveLateFault(Task? task)
    {
        if (task is null || task.IsCompletedSuccessfully || task.IsCanceled)
        {
            return;
        }

        if (task.IsFaulted)
        {
            if (task.Exception is AggregateException ex && ExceptionGuard.IsFatal(ex))
            {
                ExceptionDispatchInfo.Capture(ex.InnerExceptions.Count == 1 ? ex.InnerExceptions[0] : ex).Throw();
            }

            return;
        }

        _ = task.ContinueWith(
            static faulted =>
            {
                if (faulted.Exception is AggregateException ex && ExceptionGuard.IsFatal(ex))
                {
                    ExceptionDispatchInfo.Capture(ex.InnerExceptions.Count == 1 ? ex.InnerExceptions[0] : ex).Throw();
                }
            },
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    private void LogPolicySuccess(CommandAuthorizationRequest request, string policy, string correlationId)
    {
        try
        {
            if (options.Value.LogAllowedDecisions)
            {
                FrontComposerSecurityLog.AuthorizationPolicySucceeded(
                    logger, request.SourceSurface, request.CommandType.FullName ?? request.CommandType.Name, policy, correlationId);
            }
        }
        catch (Exception ex) when (!ExceptionGuard.IsFatal(ex))
        {
            // An optional audit sink failure must preserve the already completed policy decision.
        }
    }

    private void LogBlocked(
        CommandAuthorizationRequest request,
        CommandAuthorizationReason reason,
        string trimmedPolicy,
        string correlationId)
        => FrontComposerSecurityLog.AuthorizationBlocked(
            logger,
            reason,
            request.CommandType.FullName ?? request.CommandType.Name,
            trimmedPolicy,
            correlationId);
}
