using System.Threading;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Shell.Resources;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Hexalith.FrontComposer.Shell.Components.Badges;

/// <summary>
/// Colored status icon for generated <c>[ProjectionBadge]</c> enum members.
/// </summary>
public partial class FcStatusIcon : ComponentBase {
    private static long _nextAnchorId;

    private readonly string _anchorId = "fc-status-icon-" + Interlocked.Increment(ref _nextAnchorId).ToString(System.Globalization.CultureInfo.InvariantCulture);
    private string _ariaLabel = string.Empty;
    private Color _color;
    private Icon _icon = default!;

    /// <summary>Gets or sets the semantic status slot.</summary>
    [Parameter]
    [EditorRequired]
    public BadgeSlot Slot { get; set; }

    /// <summary>Gets or sets the status label shown in the tooltip and accessible name.</summary>
    [Parameter]
    [EditorRequired]
    public string Label { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional column header used to contextualize the accessible name.</summary>
    [Parameter]
    public string? ColumnHeader { get; set; }

    /// <summary>Injected string localizer for the EN / FR aria-label template.</summary>
    [Inject]
    private IStringLocalizer<FcShellResources> Localizer { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnParametersSet() {
        (_icon, _color) = StatusIconTable.Resolve(Slot);

        string label = Label ?? string.Empty;
        _ariaLabel = string.IsNullOrEmpty(ColumnHeader)
            ? label
            : Localizer["StatusBadgeAriaLabelTemplate", ColumnHeader, label].Value;
    }
}
