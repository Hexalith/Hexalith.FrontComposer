using Hexalith.FrontComposer.Contracts.Rendering;

using Microsoft.AspNetCore.Components;

namespace Hexalith.FrontComposer.Shell.Components.Layout;

/// <summary>
/// Reusable aggregate list page wrapper (FC-LST contract, cc-2026-06-21). Composes
/// <see cref="FcPageLayout"/>, a vertical <c>FluentStack</c> root, and <see cref="FcPageHeader"/> — with
/// the header Actions slot as the page toolbar and the Metadata slot for return context — and exposes
/// slots for a consuming domain's filters, command flows, surface states, data grid body, and pager.
/// The wrapper stays generic over <typeparamref name="TItem"/> and owns no domain copy, query gateway,
/// or <c>IQueryService</c> dependency; domains keep all server-side search/freshness/safety semantics.
/// </summary>
/// <typeparam name="TItem">The row item type the list renders. Types the optional row navigation callback.</typeparam>
public sealed partial class FcAggregateListPage<TItem> : ComponentBase {
    private FcPageHeader? _pageHeader;

    /// <summary>The FC-LYT layout measure the page declares. Defaults to <see cref="FcPageLayoutMode.FullWidth"/> for dense list surfaces.</summary>
    [Parameter] public FcPageLayoutMode LayoutMode { get; set; } = FcPageLayoutMode.FullWidth;

    /// <summary>The browser document title forwarded to the composed <see cref="FcPageHeader"/>.</summary>
    [Parameter] public string? PageTitle { get; set; }

    /// <summary>The visible route-level heading. Must be non-blank (enforced by <see cref="FcPageHeader"/>).</summary>
    [Parameter] public string Heading { get; set; } = string.Empty;

    /// <summary>Optional short context text rendered above the heading.</summary>
    [Parameter] public string? Eyebrow { get; set; }

    /// <summary>Optional descriptive text rendered below the heading.</summary>
    [Parameter] public string? Description { get; set; }

    /// <summary>Optional id assigned to the route-level heading (focus target for return-context restoration).</summary>
    [Parameter] public string? HeadingId { get; set; }

    /// <summary>Optional tabindex assigned to the route-level heading when it is a focus target.</summary>
    [Parameter] public int? HeadingTabIndex { get; set; }

    /// <summary>Stable test selector for the composed header root.</summary>
    [Parameter] public string HeaderTestId { get; set; } = "fc-aggregate-list-header";

    /// <summary>Optional additional CSS classes applied to the composed header root.</summary>
    [Parameter] public string? HeaderClass { get; set; }

    /// <summary>Stable test selector for the page root stack.</summary>
    [Parameter] public string RootTestId { get; set; } = "fc-aggregate-list";

    /// <summary>Optional CSS class applied to the page root stack.</summary>
    [Parameter] public string? RootClass { get; set; }

    /// <summary>Vertical gap between root regions (CSS length). Defaults to <c>24px</c>.</summary>
    [Parameter] public string RootGap { get; set; } = "24px";

    /// <summary>Toolbar content rendered in the header Actions slot (refresh/reset/navigation commands).</summary>
    [Parameter] public RenderFragment? Toolbar { get; set; }

    /// <summary>Return-context or page metadata rendered in the header Metadata slot.</summary>
    [Parameter] public RenderFragment? HeaderMetadata { get; set; }

    /// <summary>Search and filter controls rendered below the header.</summary>
    [Parameter] public RenderFragment? Filters { get; set; }

    /// <summary>Command flow surfaces (for example a create flow) rendered above the list body.</summary>
    [Parameter] public RenderFragment? Commands { get; set; }

    /// <summary>Surface-state content (loading/empty/filtered-empty/error/stale/degraded) rendered above the body.</summary>
    [Parameter] public RenderFragment? States { get; set; }

    /// <summary>The data grid / list body, supplied by the domain (for example a <c>FluentDataGrid</c>).</summary>
    [Parameter] public RenderFragment? Body { get; set; }

    /// <summary>Pagination controls rendered below the body.</summary>
    [Parameter] public RenderFragment? Pager { get; set; }

    /// <summary>Optional additional content rendered after the pager.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Optional typed row-navigation callback a domain row template can raise via <see cref="NotifyItemSelectedAsync"/>.</summary>
    [Parameter] public EventCallback<TItem> OnItemSelected { get; set; }

    /// <summary>Moves focus to the route-level heading (forwarded to the composed <see cref="FcPageHeader"/>).</summary>
    public ValueTask FocusHeadingAsync()
        => _pageHeader?.FocusHeadingAsync() ?? ValueTask.CompletedTask;

    /// <summary>Raises <see cref="OnItemSelected"/> for the supplied row.</summary>
    /// <param name="item">The selected row item.</param>
    public Task NotifyItemSelectedAsync(TItem item)
        => OnItemSelected.InvokeAsync(item);
}
