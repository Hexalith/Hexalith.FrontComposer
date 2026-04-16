namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>
/// Result of a single <see cref="IDerivedValueProvider"/> invocation. When <see cref="HasValue"/>
/// is <see langword="false"/>, the chain continues to the next provider.
/// </summary>
/// <param name="HasValue">Whether this provider resolved a value.</param>
/// <param name="Value">The resolved value (ignored when <see cref="HasValue"/> is false).</param>
public readonly record struct DerivedValueResult(bool HasValue, object? Value) {
    /// <summary>Shared empty sentinel representing "this provider declined".</summary>
    public static readonly DerivedValueResult None = new(false, null);
}
