namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>
/// Resolves explicit target intent for one typed command before dispatch.
/// </summary>
/// <typeparam name="TCommand">The command type.</typeparam>
public interface ICommandTargetIdentityProvider<in TCommand>
{
    /// <summary>
    /// Resolves the command target, or returns <see langword="null"/> when the target cannot be proven.
    /// </summary>
    /// <param name="command">The command being dispatched.</param>
    /// <param name="cancellationToken">A token that cancels target resolution.</param>
    /// <returns>The resolved target intent, or <see langword="null"/>.</returns>
    ValueTask<CommandTargetIdentity?> ResolveAsync(TCommand command, CancellationToken cancellationToken);
}
