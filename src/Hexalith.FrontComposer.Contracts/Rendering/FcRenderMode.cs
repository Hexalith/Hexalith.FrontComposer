namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>
/// Blazor render mode for FrontComposer components.
/// Named FcRenderMode to avoid collision with
/// <c>Microsoft.AspNetCore.Components.Web.RenderMode</c>.
/// </summary>
public enum FcRenderMode {
    /// <summary>Server-side interactive rendering via SignalR.</summary>
    Server,

    /// <summary>Client-side WebAssembly rendering.</summary>
    WebAssembly,

    /// <summary>Automatic render mode selection.</summary>
    Auto,
}
