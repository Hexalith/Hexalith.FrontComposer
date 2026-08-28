using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Hexalith.FrontComposer.Shell.Components.Layout;

/// <summary>
/// Renders page-level tabs whose <see cref="FcPageTab"/> children own their associated panel content.
/// Place the component in the page body, after the page header, so Fluent UI can keep every tab and
/// generated tab panel in one semantic contract.
/// </summary>
public sealed partial class FcPageTabs : ComponentBase
{
    private readonly List<FcPageTab> _tabs = [];

    private IReadOnlyList<FcPageTab> Tabs => _tabs;

    /// <summary>Currently active tab id.</summary>
    [Parameter]
    public string? ActiveTabId { get; set; }

    /// <summary>Raised when Fluent UI changes the active tab id.</summary>
    [Parameter]
    public EventCallback<string?> ActiveTabIdChanged { get; set; }

    /// <summary>Accessible name for the page tab list.</summary>
    [Parameter]
    public string AriaLabel { get; set; } = "Page sections";

    /// <summary>Stable selector applied to the Fluent tabs root.</summary>
    [Parameter]
    public string TestId { get; set; } = "fc-page-tabs";

    /// <summary>Fluent tab appearance. Defaults to the subtle page-level treatment.</summary>
    [Parameter]
    public TabsAppearance? Appearance { get; set; } = TabsAppearance.Subtle;

    /// <summary>Whether the entire tab set is disabled.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Fluent tab orientation. Defaults to horizontal.</summary>
    [Parameter]
    public Orientation? Orientation { get; set; } = Microsoft.FluentUI.AspNetCore.Components.Orientation.Horizontal;

    /// <summary>Width applied to the Fluent tabs root.</summary>
    [Parameter]
    public string? Width { get; set; } = "100%";

    /// <summary>Caller-owned <see cref="FcPageTab"/> children.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Registers one declarative page-tab child in render order.</summary>
    /// <param name="tab">The child descriptor to register.</param>
    internal void Register(FcPageTab tab)
    {
        ArgumentNullException.ThrowIfNull(tab);
        if (_tabs.Contains(tab))
        {
            return;
        }

        if (_tabs.Any(existing => string.Equals(existing.Id, tab.Id, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException($"A page tab with id '{tab.Id}' is already registered.");
        }

        _tabs.Add(tab);
        _ = InvokeAsync(StateHasChanged);
    }

    private Task OnActiveTabIdChangedAsync(string? activeTabId)
        => ActiveTabIdChanged.InvokeAsync(activeTabId);
}
