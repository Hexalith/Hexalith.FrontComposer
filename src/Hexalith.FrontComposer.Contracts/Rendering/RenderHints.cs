namespace Hexalith.FrontComposer.Contracts.Rendering;

using Hexalith.FrontComposer.Contracts.Attributes;

/// <summary>
/// Additional rendering hints for field-level customization.
/// </summary>
/// <param name="BadgeSlot">Optional badge slot for status-colored badges.</param>
/// <param name="CurrencyCode">ISO 4217 currency code for monetary fields.</param>
/// <param name="DateFormat">.NET date format string (e.g., "yyyy-MM-dd").</param>
/// <param name="Icon">Icon identifier for the field.</param>
/// <param name="IsSortable">Whether the field supports sorting in data grids.</param>
/// <param name="IsFilterable">Whether the field supports filtering in data grids.</param>
public record RenderHints(
    BadgeSlot? BadgeSlot = null,
    string? CurrencyCode = null,
    string? DateFormat = null,
    string? Icon = null,
    bool IsSortable = true,
    bool IsFilterable = true);
