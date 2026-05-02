using Hexalith.FrontComposer.Contracts.Communication;

namespace Hexalith.FrontComposer.Shell.Services.Authorization;

/// <summary>
/// Authorizes direct framework command dispatch before command-service side effects begin.
/// </summary>
/// <remarks>
/// <para>
/// Implementations MUST fail closed for policy-protected commands. Story 7-3 Pass 4 contract:
/// </para>
/// <list type="bullet">
///   <item>Unprotected commands (no <c>[RequiresPolicy]</c>) return synchronously without side effects.</item>
///   <item>
///     Allowed decisions return synchronously; the caller proceeds with normal dispatch.
///   </item>
///   <item>
///     Denied or fail-closed decisions throw <see cref="CommandWarningException"/> with
///     <see cref="CommandWarningKind.Forbidden"/>.
///   </item>
///   <item>
///     Pending decisions (transient prerender / in-flight refresh) throw
///     <see cref="CommandWarningException"/> with <see cref="CommandWarningKind.Pending"/>; callers
///     may surface a "Checking permission…" affordance and retry.
///   </item>
///   <item>
///     Cancellation surfaces as <see cref="OperationCanceledException"/>, never as a
///     permission-denied warning.
///   </item>
///   <item>
///     Missing authorization services or null evaluator results MUST fail closed for protected
///     commands. Callers are expected to register the gate and the evaluator together via
///     <c>AddHexalithFrontComposer*</c>.
///   </item>
/// </list>
/// </remarks>
public interface ICommandDispatchAuthorizationGate {
    /// <summary>
    /// Ensures the supplied command can be dispatched by the current principal.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <param name="command">The command instance.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task EnsureAuthorizedAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : class;
}
