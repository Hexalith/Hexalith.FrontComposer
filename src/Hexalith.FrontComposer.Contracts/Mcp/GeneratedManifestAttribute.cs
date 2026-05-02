namespace Hexalith.FrontComposer.Contracts.Mcp;

/// <summary>
/// Marks a generated host class that exposes a single static <c>McpManifest Manifest</c> property.
/// The MCP runtime registry only loads manifests from types decorated with this attribute, which
/// closes the stealth-registration channel that a name-only match would expose.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class GeneratedManifestAttribute : Attribute;
