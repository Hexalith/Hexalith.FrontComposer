namespace Hexalith.FrontComposer.Shell.Components.Forms;

/// <summary>
/// Describes one generated command field for linked validation-summary rendering.
/// </summary>
/// <param name="FieldName">The command-model property name.</param>
/// <param name="Label">The localized visible field label.</param>
/// <param name="InputId">The stable DOM identifier of the editable control.</param>
public sealed record FcValidationFieldDescriptor(
    string FieldName,
    string Label,
    string InputId);
