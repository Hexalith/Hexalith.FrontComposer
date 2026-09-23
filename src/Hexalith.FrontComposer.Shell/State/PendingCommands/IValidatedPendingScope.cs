namespace Hexalith.FrontComposer.Shell.State.PendingCommands;

/// <summary>Supplies an already validated scope to circuit-local command state.</summary>
public interface IValidatedPendingScope {
    /// <summary>Returns the current scope or null when it cannot be validated.</summary>
    (string TenantId, string UserId)? Current();
}
