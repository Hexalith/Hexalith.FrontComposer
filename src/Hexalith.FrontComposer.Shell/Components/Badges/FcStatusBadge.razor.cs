using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Shell.Resources;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Hexalith.FrontComposer.Shell.Components.Badges;

/// <summary>
/// Story 4-2 D1 / AC1 / AC5 — semantic status badge. Wraps Fluent UI v5
/// <see cref="FluentBadge"/> with a frozen slot → (Color, Appearance) mapping and produces
/// a contextual <c>aria-label</c> so screen readers announce
/// <c>"{ColumnHeader}: {Label}"</c> instead of a bare state name. Color is never the sole
/// signal — the text label is always rendered (UX-DR30 commitment #1).
/// </summary>
public partial class FcStatusBadge : ComponentBase {
    private BadgeColor _color;
    private BadgeAppearance _appearance;
    private string _ariaLabel = string.Empty;

    /// <summary>
    /// Gets or sets the semantic slot. Resolves to a Fluent <see cref="BadgeColor"/> +
    /// <see cref="BadgeAppearance"/> pair via <see cref="SlotAppearanceTable"/>. Changing
    /// this slot at runtime re-resolves the pair on the next render.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public BadgeSlot Slot { get; set; }

    /// <summary>
    /// Gets or sets the visible badge label. Always rendered — UX-DR30 commitment #1 makes
    /// "color is never the sole signal" structurally true. Consumers supply the humanized
    /// enum value (typically from <c>HumanizeEnumLabel</c>).
    /// </summary>
    [Parameter]
    [EditorRequired]
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional column-header text used to prefix the <c>aria-label</c>
    /// (for example <c>"Status"</c>). When <see langword="null"/> the aria-label falls back
    /// to just <see cref="Label"/>.
    /// </summary>
    [Parameter]
    public string? ColumnHeader { get; set; }

    /// <summary>Injected string localizer for the EN / FR aria-label template.</summary>
    [Inject]
    private IStringLocalizer<FcShellResources> Localizer { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnParametersSet() {
        (_color, _appearance) = SlotAppearanceTable.Resolve(Slot);

        string label = Label ?? string.Empty;
        _ariaLabel = string.IsNullOrEmpty(ColumnHeader)
            ? label
            : Localizer["StatusBadgeAriaLabelTemplate", ColumnHeader, label].Value;
    }
}
