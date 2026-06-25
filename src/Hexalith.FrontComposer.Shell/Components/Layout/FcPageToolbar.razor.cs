using Microsoft.AspNetCore.Components;

namespace Hexalith.FrontComposer.Shell.Components.Layout;

/// <summary>
/// Reusable page toolbar for search, filters, view menu actions, right-aligned page actions, and
/// optional page-local tabs.
/// </summary>
public sealed partial class FcPageToolbar : ComponentBase
{
    private bool _filterOpen;

    /// <summary>Stable test selector for the toolbar row.</summary>
    [Parameter] public string TestId { get; set; } = "fc-page-toolbar";

    /// <summary>Accessible name for the toolbar row.</summary>
    [Parameter] public string AriaLabel { get; set; } = "Page tools";

    /// <summary>Optional CSS class applied to the root toolbar container.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Current search value.</summary>
    [Parameter] public string? SearchValue { get; set; }

    /// <summary>Raised when the search value changes.</summary>
    [Parameter] public EventCallback<string?> SearchValueChanged { get; set; }

    /// <summary>Placeholder text for the search input.</summary>
    [Parameter] public string? SearchPlaceholder { get; set; }

    /// <summary>Accessible name for the search input.</summary>
    [Parameter] public string SearchAriaLabel { get; set; } = "Search";

    /// <summary>Visible label and accessible name for the filter trigger.</summary>
    [Parameter] public string FilterLabel { get; set; } = "Filters";

    /// <summary>Stable id for the filter trigger used by the popover anchor.</summary>
    [Parameter] public string FilterTriggerId { get; set; } = "fc-page-toolbar-filter-trigger";

    /// <summary>Stable id for the filter popover title.</summary>
    [Parameter] public string FilterTitleId { get; set; } = "fc-page-toolbar-filter-title";

    /// <summary>Optional caller-owned filter panel content.</summary>
    [Parameter] public RenderFragment? FilterContent { get; set; }

    /// <summary>Visible label and accessible name for the view menu trigger.</summary>
    [Parameter] public string ViewMenuLabel { get; set; } = "View";

    /// <summary>Optional caller-owned menu items for view or overflow actions.</summary>
    [Parameter] public RenderFragment? ViewMenuContent { get; set; }

    /// <summary>Optional caller-owned page actions rendered at the end of the toolbar row.</summary>
    [Parameter] public RenderFragment? Actions { get; set; }

    /// <summary>Optional tabs rendered under the toolbar row.</summary>
    [Parameter] public IReadOnlyList<FcPageToolbarTab> Tabs { get; set; } = Array.Empty<FcPageToolbarTab>();

    /// <summary>Currently active tab id.</summary>
    [Parameter] public string? ActiveTabId { get; set; }

    /// <summary>Raised when the active tab id changes.</summary>
    [Parameter] public EventCallback<string?> ActiveTabIdChanged { get; set; }

    private bool HasTabs => Tabs.Count > 0;

    private string RootClass
        => string.IsNullOrWhiteSpace(Class) ? "fc-page-toolbar-root" : $"fc-page-toolbar-root {Class}";

    private Task OnSearchValueChangedAsync(string? value)
        => SearchValueChanged.InvokeAsync(value);

    private Task ToggleFilterAsync()
    {
        _filterOpen = !_filterOpen;
        return Task.CompletedTask;
    }

    private Task OnFilterOpenChangedAsync(bool opened)
    {
        _filterOpen = opened;
        return Task.CompletedTask;
    }

    private Task OnActiveTabIdChangedAsync(string? activeTabId)
        => ActiveTabIdChanged.InvokeAsync(activeTabId);
}
