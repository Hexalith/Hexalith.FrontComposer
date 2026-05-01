using Hexalith.FrontComposer.Contracts.DevMode;

namespace Hexalith.FrontComposer.Shell.Services.DevMode;

/// <summary>
/// Emits starter Razor source for supported dev-mode component-tree nodes.
/// </summary>
public interface IRazorEmitter {
    /// <summary>Emits starter source for the requested customization level.</summary>
    string EmitStarterTemplate(ComponentTreeNode node, CustomizationLevel level);
}
