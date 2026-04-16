namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>
/// Chain-of-responsibility provider that resolves a single derivable command property value
/// before form submission (Story 2-2 AC6, ADR-014). Providers are registered as an ordered
/// <see cref="IEnumerable{T}"/> — resolution iterates and takes the first <see cref="DerivedValueResult.HasValue"/>.
/// </summary>
/// <remarks>
/// Built-in providers register in this order (Decision D24):
/// <c>SystemValueProvider</c> → <c>ProjectionContextProvider</c> → <c>ExplicitDefaultValueProvider</c> →
/// <c>LastUsedValueProvider</c> → <c>ConstructorDefaultValueProvider</c>. Adopters add providers at the
/// head of the chain via <c>services.AddDerivedValueProvider&lt;T&gt;()</c>.
/// </remarks>
public interface IDerivedValueProvider {
    /// <summary>
    /// Resolves a single command property's value.
    /// </summary>
    /// <param name="commandType">The command CLR type whose property is being resolved.</param>
    /// <param name="propertyName">The derivable property's name.</param>
    /// <param name="projectionContext">Optional row-level projection context; providers may ignore it.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A <see cref="DerivedValueResult"/> with <see cref="DerivedValueResult.HasValue"/> set when the provider resolved a value.</returns>
    Task<DerivedValueResult> ResolveAsync(
        Type commandType,
        string propertyName,
        ProjectionContext? projectionContext,
        CancellationToken ct);
}

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
