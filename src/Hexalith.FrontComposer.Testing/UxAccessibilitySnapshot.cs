namespace Hexalith.FrontComposer.Testing;

/// <summary>Caller-measured semantic accessibility data; no browser package is required.</summary>
/// <param name="Role">Computed role.</param>
/// <param name="Name">Computed accessible name.</param>
/// <param name="IsFocused">Whether this node owns active focus.</param>
/// <param name="AriaLive">Live attribute, or null when absent.</param>
/// <param name="AriaAtomic">Atomic attribute, or null when absent.</param>
/// <param name="TabIndex">Tab index attribute, or null when absent.</param>
public sealed record UxAccessibilitySnapshot(string Role, string Name, bool IsFocused, string? AriaLive = null, string? AriaAtomic = null, string? TabIndex = null);
