namespace Hexalith.FrontComposer.Contracts.Communication;

/// <summary>
/// Stable command dispatch status values used by <see cref="CommandResult"/>.
/// </summary>
public static class CommandResultStatus {
    /// <summary>Command dispatch was accepted for processing.</summary>
    public const string Accepted = "Accepted";

    /// <summary>Command dispatch was rejected before processing could continue.</summary>
    public const string Rejected = "Rejected";
}
