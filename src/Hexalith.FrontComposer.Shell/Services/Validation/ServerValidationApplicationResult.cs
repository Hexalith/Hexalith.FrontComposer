namespace Hexalith.FrontComposer.Shell.Services.Validation;

/// <summary>Describes the safe result of applying a server field map to an editable command.</summary>
/// <param name="UnmappedMessages">Support-safe messages that could not be linked to an editable field.</param>
/// <param name="MappedFieldCount">The number of allowlisted fields that received at least one message.</param>
public sealed record ServerValidationApplicationResult(
    IReadOnlyList<string> UnmappedMessages,
    int MappedFieldCount) {
    /// <summary>Gets a value indicating whether the outcome contains a safe mapped field error.</summary>
    public bool HasMappedFieldErrors => MappedFieldCount > 0;
}
