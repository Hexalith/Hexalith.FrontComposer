using Hexalith.FrontComposer.Contracts.Rendering;

namespace Hexalith.FrontComposer.Shell.Services;

/// <summary>Allows the Shell to dismiss an inline popover while its circuit is reconnecting.</summary>
public interface IReconnectableInlinePopover : IInlinePopover {
    /// <summary>Dismisses the popover without waiting for JavaScript or moving focus.</summary>
    /// <returns>Whether an open popover was dismissed.</returns>
    Task<bool> DismissOnReconnectAsync();
}
