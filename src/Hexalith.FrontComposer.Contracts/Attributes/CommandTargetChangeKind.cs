namespace Hexalith.FrontComposer.Contracts.Attributes;

/// <summary>
/// Describes the declared material change a command targets.
/// </summary>
public enum CommandTargetChangeKind
{
    /// <summary>The command creates a new projection entity.</summary>
    Create,

    /// <summary>The command updates an existing projection entity.</summary>
    Update,

    /// <summary>The command moves an entity between status-backed views.</summary>
    StatusMove,

    /// <summary>The command deletes an existing projection entity.</summary>
    Delete,
}
