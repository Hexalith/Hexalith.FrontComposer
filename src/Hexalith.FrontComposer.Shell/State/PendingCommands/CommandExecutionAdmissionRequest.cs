namespace Hexalith.FrontComposer.Shell.State.PendingCommands;

/// <summary>
/// Framework metadata for FC-CNC admission. It intentionally excludes command payloads and form values.
/// </summary>
public sealed record CommandExecutionAdmissionRequest {
    public CommandExecutionAdmissionRequest(string commandTypeName, string? displayLabel = null) {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandTypeName);
        CommandTypeName = commandTypeName;
        DisplayLabel = displayLabel;
    }

    public string CommandTypeName { get; }

    public string? DisplayLabel { get; }
}
