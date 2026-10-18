namespace Hexalith.FrontComposer.Testing;

/// <summary>Snapshot of a direct origin handle and its semantic identity, measured before activation or after close.</summary>
/// <param name="Handle">Opaque stable direct-element identity; an optional test id does not replace this handle.</param>
/// <param name="Role">Computed accessible role.</param>
/// <param name="Name">Computed accessible name.</param>
/// <param name="TagName">Element tag name.</param>
/// <param name="Connected">Whether the element is in the active document.</param>
/// <param name="Enabled">Whether the element is enabled.</param>
/// <param name="Visible">Whether the element is visible.</param>
/// <param name="TestId">Optional supplementary contract-owned evidence locator.</param>
public sealed record UxFocusTarget(object Handle, string Role, string Name, string TagName,
    bool Connected = true, bool Enabled = true, bool Visible = true, string? TestId = null);
