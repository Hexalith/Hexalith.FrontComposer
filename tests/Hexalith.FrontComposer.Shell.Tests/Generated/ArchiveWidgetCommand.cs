using Hexalith.FrontComposer.Contracts.Attributes;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

/// <summary>
/// Story 13.3 VG3-05 — a destructive command that declares no confirmation copy, so its dialog uses
/// the localized framework defaults at runtime.
/// </summary>
[Command]
[BoundedContext("TestCommands")]
[Destructive]
public sealed class ArchiveWidgetCommand {
    public string MessageId { get; set; } = string.Empty;

    public string WidgetId { get; set; } = string.Empty;
}
