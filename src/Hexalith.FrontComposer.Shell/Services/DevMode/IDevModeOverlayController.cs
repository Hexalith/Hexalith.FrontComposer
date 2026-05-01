using Hexalith.FrontComposer.Contracts.DevMode;

namespace Hexalith.FrontComposer.Shell.Services.DevMode;

/// <summary>
/// Scoped controller for the development-only overlay state.
/// </summary>
public interface IDevModeOverlayController {
    /// <summary>Raised when overlay state or the selected annotation changes.</summary>
    event EventHandler? Changed;

    /// <summary>Gets a value indicating whether annotation inspection is active.</summary>
    bool IsActive { get; }

    /// <summary>Gets the selected annotation key, if any.</summary>
    string? SelectedAnnotationKey { get; }

    /// <summary>Gets the selected node, if any.</summary>
    ComponentTreeNode? SelectedNode { get; }

    /// <summary>Toggles overlay inspection on or off.</summary>
    void Toggle();

    /// <summary>Opens an annotation by key using the currently registered node.</summary>
    bool Open(string annotationKey);

    /// <summary>Opens an annotation by key only when its render epoch is still current.</summary>
    bool Open(string annotationKey, long renderEpoch);

    /// <summary>Closes the selected annotation detail panel.</summary>
    void Close();

    /// <summary>Registers a rendered annotation snapshot for the active render epoch.</summary>
    IDisposable Register(ComponentTreeNode node);
}
