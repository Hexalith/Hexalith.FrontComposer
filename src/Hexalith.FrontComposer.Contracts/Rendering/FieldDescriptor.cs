namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>
/// Immutable field-level rendering metadata. Used by convention-based ProjectionRenderer
/// to determine how to render each field in data grids, detail views, and forms.
/// </summary>
/// <param name="Name">The property name from the projection model.</param>
/// <param name="TypeName">The CLR type name as a string (e.g., "System.Int32").</param>
/// <param name="IsNullable">Whether the field accepts null values.</param>
/// <param name="DisplayName">The human-readable display name; falls back to <paramref name="Name"/> if null.</param>
/// <param name="Format">A standard .NET format string (e.g., "C2" for currency).</param>
/// <param name="Order">The display order index.</param>
/// <param name="IsReadOnly">Whether the field is read-only in edit forms.</param>
/// <param name="Hints">Additional rendering hints for badges, currency, sorting, etc.</param>
/// <param name="Description">Localization-aware description / help text from <c>[Description]</c>
/// or <c>[Display(Description=)]</c>; null when no description annotation is present.</param>
public record FieldDescriptor(
    string Name,
    string TypeName,
    bool IsNullable,
    string? DisplayName = null,
    string? Format = null,
    int? Order = null,
    bool IsReadOnly = false,
    RenderHints? Hints = null,
    string? Description = null);
