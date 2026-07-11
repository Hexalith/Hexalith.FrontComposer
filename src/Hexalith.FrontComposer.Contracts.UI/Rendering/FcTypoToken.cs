using Microsoft.FluentUI.AspNetCore.Components;

namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>
/// Immutable typography token binding a Fluent UI v5 <see cref="TextSize"/> +
/// <see cref="TextWeight"/> + <see cref="TextTag"/> (+ optional <see cref="TextFont"/>) set
/// into a single addressable constant. See <see cref="Typography"/> for the 9 framework-owned roles.
/// </summary>
/// <param name="Size">Fluent UI text size (Size100–Size1000).</param>
/// <param name="Weight">Fluent UI text weight (Regular / Medium / Semibold / Bold).</param>
/// <param name="Tag">HTML element tag emitted by <c>FluentText</c>.</param>
/// <param name="Font">Optional font family override (Base / Numeric / Monospace).</param>
public readonly record struct FcTypoToken(
    TextSize Size,
    TextWeight Weight,
    TextTag Tag,
    TextFont? Font = null);
