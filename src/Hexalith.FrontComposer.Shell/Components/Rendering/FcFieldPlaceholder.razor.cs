using Hexalith.FrontComposer.Shell.Resources;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Hexalith.FrontComposer.Shell.Components.Rendering;

/// <summary>
/// Placeholder rendered for unsupported auto-generated fields.
/// </summary>
public partial class FcFieldPlaceholder {
    public const string DocsLink = "https://hexalith.github.io/FrontComposer/diagnostics/HFC1002";

    [Inject]
    private IStringLocalizer<FcShellResources> Localizer { get; set; } = default!;

    /// <summary>Gets or sets the name of the property this placeholder represents.</summary>
    [Parameter, EditorRequired]
    public string FieldName { get; set; } = default!;

    /// <summary>Gets or sets the fully qualified name of the unsupported type.</summary>
    [Parameter, EditorRequired]
    public string TypeName { get; set; } = default!;

    /// <summary>Gets or sets a value indicating whether developer mode highlighting is active.</summary>
    [Parameter]
    public bool IsDevMode { get; set; }

    private string AriaLabel => Localizer["FieldPlaceholderAriaLabelTemplate", FieldName, TypeName].Value;

    private string CssClass => IsDevMode
        ? "fc-field-placeholder fc-field-placeholder-dev"
        : "fc-field-placeholder";
}
