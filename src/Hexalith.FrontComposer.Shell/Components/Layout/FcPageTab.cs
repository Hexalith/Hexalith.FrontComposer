using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Hexalith.FrontComposer.Shell.Components.Layout;

/// <summary>
/// Defines one page-level Fluent tab and owns the content rendered in its generated tab panel.
/// </summary>
/// <remarks>
/// Fluent UI derives the panel id as <c>{Id}-panel</c>. Callers must not create a sibling panel or
/// override <c>aria-controls</c> or panel ids through additional attributes.
/// </remarks>
public sealed class FcPageTab : ComponentBase
{
    private bool _hasRenderedContent;

    [CascadingParameter]
    private FcPageTabs Owner { get; set; } = default!;

    /// <summary>Stable tab id. Fluent UI derives the associated panel id as <c>{Id}-panel</c>.</summary>
    [Parameter]
    public string Id { get; set; } = string.Empty;

    /// <summary>Visible tab label and accessible name.</summary>
    [Parameter]
    public string Header { get; set; } = string.Empty;

    /// <summary>Whether the tab is disabled.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Optional caller-supplied Fluent icon rendered before the label.</summary>
    [Parameter]
    public Icon? IconStart { get; set; }

    /// <summary>Whether FrontComposer defers rendering the panel content until the tab is first active.</summary>
    [Parameter]
    public bool DeferredLoading { get; set; }

    /// <summary>Optional Fluent tooltip for the tab header.</summary>
    [Parameter]
    public string? Tooltip { get; set; }

    /// <summary>Caller-owned content rendered in the Fluent-generated tab panel.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (Owner is null)
        {
            throw new InvalidOperationException($"{nameof(FcPageTab)} must be a child of {nameof(FcPageTabs)}.");
        }

        Owner.Register(this);
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Id);
        ArgumentException.ThrowIfNullOrWhiteSpace(Header);
        ArgumentNullException.ThrowIfNull(ChildContent);
    }

    /// <summary>Returns whether the panel content has reached its allowed first render.</summary>
    /// <param name="activeTabId">The active id supplied to the owning tab set.</param>
    /// <returns><see langword="true"/> when the content may render.</returns>
    internal bool ShouldRenderContent(string? activeTabId)
    {
        if (!DeferredLoading || string.Equals(Id, activeTabId, StringComparison.Ordinal))
        {
            _hasRenderedContent = true;
        }

        return _hasRenderedContent;
    }
}
