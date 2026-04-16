namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>
/// Story 2-2 Decision D37 — implemented by generated Inline command renderers so
/// <c>InlinePopoverRegistry</c> can dismiss a currently-open popover before opening another.
/// </summary>
public interface IInlinePopover {
    /// <summary>Closes the popover and returns focus to its trigger button.</summary>
    Task ClosePopoverAsync();
}
