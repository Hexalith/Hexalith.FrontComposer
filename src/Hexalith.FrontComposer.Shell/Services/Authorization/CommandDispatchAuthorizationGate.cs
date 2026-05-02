using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.Shell.Registration;
using Hexalith.FrontComposer.Shell.Resources;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace Hexalith.FrontComposer.Shell.Services.Authorization;

/// <summary>
/// Default dispatch-side authorization guard for policy-protected commands. Acts as the canonical
/// authorization site for direct <see cref="Hexalith.FrontComposer.Contracts.Communication.ICommandService"/>
/// callers (Story 7-3 Pass 4 DN-7-3-4-2 chose to wrap dispatch in <c>AuthorizingCommandServiceDecorator</c>;
/// this gate hosts the actual policy evaluation logic and the decorator delegates to it).
/// </summary>
/// <remarks>
/// The evaluator is resolved lazily through <see cref="IServiceProvider"/> rather than
/// constructor-injected directly: under the decorator pattern the gate is created at
/// <c>ICommandService</c> resolution time, but the evaluator (which transitively requires
/// <see cref="Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider"/> — a
/// Blazor-only service) should not force every test that builds a DI container to also wire the
/// Blazor host stack. Lazy resolution keeps the gate buildable in plain ServiceCollection tests
/// while still failing closed at dispatch time when the evaluator is genuinely missing.
/// </remarks>
public sealed class CommandDispatchAuthorizationGate(
    IFrontComposerRegistry registry,
    IServiceProvider serviceProvider,
    ILogger<CommandDispatchAuthorizationGate> logger,
    IStringLocalizer<FcShellResources>? localizer = null) : ICommandDispatchAuthorizationGate {
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> s_emptyValidationErrors
        = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);

    /// <inheritdoc />
    public async Task EnsureAuthorizedAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : class {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        Type commandType = command.GetType();
        if (commandType.FullName is not { } commandTypeName) {
            // Open generics, dynamic assemblies, and reflection-emitted types can produce a null
            // FullName. The legacy fallback to commandType.Name would let two unrelated commands
            // collide in the manifest lookup → silent fail-OPEN. Fail-closed instead.
            logger.LogWarning(
                "Direct command dispatch failed closed because command type {CommandShortName} has no fully qualified name; refusing to authorize. Wrap such commands in a concrete type before dispatching.",
                commandType.Name);
            throw CreateForbiddenWarning(commandType);
        }

        commandTypeName = commandTypeName.Trim();

        if (!TryResolvePolicy(commandTypeName, out string policyName, out string? boundedContext)) {
            return;
        }

        ICommandAuthorizationEvaluator? evaluator;
        try {
            evaluator = serviceProvider.GetService<ICommandAuthorizationEvaluator>();
        }
        catch (Exception ex) when (ex is InvalidOperationException or ObjectDisposedException) {
            logger.LogWarning(
                ex,
                "Direct command dispatch failed closed because command authorization services could not be resolved. CommandType={CommandType}",
                commandTypeName);
            throw CreateForbiddenWarning(commandType);
        }

        if (evaluator is null) {
            logger.LogWarning(
                "Direct command dispatch failed closed because ICommandAuthorizationEvaluator is not registered. CommandType={CommandType}",
                commandTypeName);
            throw CreateForbiddenWarning(commandType);
        }

        CommandAuthorizationDecision decision;
        try {
            decision = await evaluator.EvaluateAsync(
                new CommandAuthorizationRequest(
                    commandType,
                    policyName,
                    command,
                    boundedContext,
                    DisplayLabel(commandType),
                    CommandAuthorizationSurface.DirectDispatch),
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            // Honour cancellation as cancellation, not as a permission denial. Pass-4 DN-7-3-4-5.
            throw;
        }

        if (decision is null) {
            // Defensive: a broken IAuthorizationService stub returning null Task<AuthorizationResult>
            // could surface here as a null decision; fail-closed.
            logger.LogWarning(
                "Direct command dispatch failed closed because the evaluator returned a null decision. CommandType={CommandType} PolicyName={PolicyName}",
                commandTypeName,
                policyName);
            throw CreateForbiddenWarning(commandType);
        }

        if (decision.IsAllowed) {
            return;
        }

        // Pass-4 DN-7-3-4-5: branch on Kind so Pending and Canceled don't collapse into Forbidden.
        // Pending = transient retry-able state (prerender, in-flight refresh); Canceled =
        // user-initiated cancel surfaced as OperationCanceledException so callers can distinguish
        // it from authorization denial; everything else (Denied, FailedClosed-other) → Forbidden.
        switch (decision.Kind) {
            case CommandAuthorizationDecisionKind.Pending:
                logger.LogInformation(
                    "Direct command dispatch deferred — authorization decision pending. CommandType={CommandType} PolicyName={PolicyName} CorrelationId={CorrelationId}",
                    commandTypeName,
                    policyName,
                    decision.CorrelationId);
                throw CreatePendingWarning(commandType);
            case CommandAuthorizationDecisionKind.FailedClosed when decision.Reason == CommandAuthorizationReason.Canceled:
                logger.LogDebug(
                    "Direct command dispatch cancelled before authorization completed. CommandType={CommandType} PolicyName={PolicyName} CorrelationId={CorrelationId}",
                    commandTypeName,
                    policyName,
                    decision.CorrelationId);
                throw new OperationCanceledException(cancellationToken);
            default:
                logger.LogWarning(
                    "Direct command dispatch blocked by authorization. Reason={Reason} CommandType={CommandType} PolicyName={PolicyName} CorrelationId={CorrelationId}",
                    decision.Reason,
                    commandTypeName,
                    policyName,
                    decision.CorrelationId);
                throw CreateForbiddenWarning(commandType);
        }
    }

    private CommandWarningException CreateForbiddenWarning(Type commandType)
        => new(
            CommandWarningKind.Forbidden,
            new ProblemDetailsPayload(
                Title: ResolveLocalised("AuthorizationActionUnavailableTitle", "Action unavailable"),
                Detail: ResolveLocalised("AuthorizationActionUnavailableMessage", "This action is not available for the current user or session."),
                Status: 403,
                EntityLabel: GenericEntityLabel(commandType),
                ValidationErrors: s_emptyValidationErrors,
                GlobalErrors: Array.Empty<string>()));

    private CommandWarningException CreatePendingWarning(Type commandType)
        => new(
            CommandWarningKind.Pending,
            new ProblemDetailsPayload(
                Title: ResolveLocalised("AuthorizationCheckingPermissionTitle", "Checking permission…"),
                Detail: ResolveLocalised("AuthorizationActionUnavailableMessage", "This action could not be authorized right now. Please retry, and contact support if the problem persists."),
                Status: 503,
                EntityLabel: GenericEntityLabel(commandType),
                ValidationErrors: s_emptyValidationErrors,
                GlobalErrors: Array.Empty<string>()));

    private string ResolveLocalised(string key, string fallback) {
        if (localizer is null) {
            // Tests and minimal hosts may not register localization. Fall through to the static
            // English fallback rather than throwing — the deny payload remains opaque, just not
            // localized.
            return fallback;
        }

        LocalizedString candidate = localizer[key];
        return candidate.ResourceNotFound ? fallback : candidate.Value;
    }

    private bool TryResolvePolicy(string commandTypeName, out string policyName, out string? boundedContext) {
        policyName = string.Empty;
        boundedContext = null;

        // Pass-4 DN-7-3-4-4 (b): the registry's IFrontComposerCommandPolicyRegistry companion is
        // the single canonical lookup. Adopters with a custom IFrontComposerRegistry that does
        // not implement the companion still get the default-implemented interface method (manifest
        // walk with trimming) so the gate has no separate fallback path. This eliminates the
        // canonical-vs-fallback divergence the audit flagged (AA-23 / EH-12 / EH-13 / EH-14 /
        // EH-42) and keeps every consumer routed through one BC-aware lookup.
        if (registry is IFrontComposerCommandPolicyRegistry policyRegistry) {
            return policyRegistry.TryGetCommandPolicy(commandTypeName, out policyName, out boundedContext);
        }

        return false;
    }

    private static string DisplayLabel(Type commandType) {
        // Strip the trailing "Command" suffix using ordinal comparison so types named MyCOMMAND
        // keep their suffix (BH-19). Use FullName-based simple-name extraction so nested types
        // don't lose their enclosing-type context (EH-66). Empty result (a class literally named
        // "Command") falls back to the full simple name to avoid format-string crashes downstream.
        string simpleName = commandType.Name;
        if (simpleName.Length > "Command".Length
            && simpleName.EndsWith("Command", StringComparison.Ordinal)) {
            return simpleName[..^"Command".Length];
        }

        return simpleName;
    }

    private string GenericEntityLabel(Type commandType) {
        // BH-18: the EntityLabel surface is observable to clients via ProblemDetailsPayload. For
        // unauthenticated/denied paths the command's friendly name leaks information about which
        // protected commands exist (helps attackers enumerate the policy graph). Use a localized
        // generic label so the deny payload stays opaque. The forensic correlation Id in logs
        // still lets operators trace the actual command type.
        _ = commandType;
        return ResolveLocalised("AuthorizationActionUnavailableTitle", "Action unavailable");
    }
}
