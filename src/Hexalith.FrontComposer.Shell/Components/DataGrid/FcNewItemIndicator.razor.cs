using Hexalith.FrontComposer.Shell.Resources;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Hexalith.FrontComposer.Shell.Components.DataGrid;

/// <summary>Transient DataGrid lane marker for newly confirmed rows outside active filter criteria.</summary>
public partial class FcNewItemIndicator : ComponentBase {
    [Inject]
    private IStringLocalizer<FcShellResources> Localizer { get; set; } = default!;

    [Parameter]
    public string? Text { get; set; }

    [Parameter]
    public string? AriaLabelOverride { get; set; }

    /// <summary>Resolves the visible copy. Falls back to the localized whole-string default.</summary>
    protected string ResolvedText => string.IsNullOrWhiteSpace(Text)
        ? Localizer["NewItemIndicatorText"].Value
        : Text!;

    /// <summary>P12 — distinct accessible label so the role=status announcement carries context without duplication.</summary>
    protected string AriaLabel => string.IsNullOrWhiteSpace(AriaLabelOverride)
        ? Localizer["NewItemIndicatorAriaLabel"].Value
        : AriaLabelOverride!;
}
