using Hexalith.FrontComposer.Shell.Resources;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Hexalith.FrontComposer.Shell.Components.Rendering;

/// <summary>Contains a generated fallback fault without re-entering customization resolution.</summary>
/// <remarks>Fallback failures display localized unavailable copy and never publish raw faults.</remarks>
internal sealed class FcCustomizationFallbackBoundary : ErrorBoundaryBase
{
    /// <summary>Gets or sets the shell string localizer for support-safe unavailable copy.</summary>
    [Inject]
    public IStringLocalizer<FcShellResources> Localizer { get; set; } = default!;

    /// <inheritdoc />
    protected override Task OnErrorAsync(Exception exception)
        => Task.CompletedTask;

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        if (CurrentException is null)
        {
            builder.AddContent(0, ChildContent);
            return;
        }

        builder.OpenComponent<FluentMessageBar>(1);
        builder.AddAttribute(2, "Intent", MessageBarIntent.Error);
        builder.AddAttribute(3, "AllowDismiss", false);
        builder.AddAttribute(4, "role", "alert");
        builder.AddAttribute(5, "ChildContent", (RenderFragment)(content =>
            content.AddContent(0, Localizer["Am30QueryFailed"].Value)));
        builder.CloseComponent();
    }
}
