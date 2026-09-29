using System.ComponentModel.DataAnnotations;

using Hexalith.FrontComposer.Contracts.Attributes;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

[Command]
[BoundedContext("TestCommands")]
public sealed class GroupedFieldsCommand {
    public string MessageId { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Record ID", Description = "Record to change.")]
    [ProjectionFieldGroup("Change details")]
    public string RecordId { get; set; } = string.Empty;

    [Display(Description = "Why the change is required.")]
    [ProjectionFieldGroup("Change details")]
    public string Reason { get; set; } = string.Empty;
}
