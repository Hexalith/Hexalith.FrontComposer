using Microsoft.AspNetCore.Components;

namespace Hexalith.FrontComposer.Shell.Components.Layout;

/// <summary>
/// Bundles the browser document title and the visible route-level page header for FrontComposer
/// adopters. Consuming domains provide localized strings and optional page-specific fragments.
/// </summary>
public sealed partial class FcPageHeader : ComponentBase {
    private ElementReference _headingElement;

    /// <summary>The browser document title rendered through Blazor <c>PageTitle</c>.</summary>
    [Parameter] public string? PageTitle { get; set; }

    /// <summary>The visible route-level heading text.</summary>
    [Parameter] public string Heading { get; set; } = string.Empty;

    /// <summary>Optional short context text rendered above the heading.</summary>
    [Parameter] public string? Eyebrow { get; set; }

    /// <summary>Optional descriptive text rendered below the heading.</summary>
    [Parameter] public string? Description { get; set; }

    /// <summary>Optional id assigned to the route-level heading.</summary>
    [Parameter] public string? HeadingId { get; set; }

    /// <summary>Optional tabindex assigned to the route-level heading when it is a focus target.</summary>
    [Parameter] public int? HeadingTabIndex { get; set; }

    /// <summary>Optional stable test selector for the header root.</summary>
    [Parameter] public string TestId { get; set; } = "fc-page-header";

    /// <summary>Optional additional CSS classes for the header root.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Optional page actions rendered in the header title row.</summary>
    [Parameter] public RenderFragment? Actions { get; set; }

    /// <summary>Optional page metadata or return-context content rendered below the description.</summary>
    [Parameter] public RenderFragment? Metadata { get; set; }

    /// <summary>Additional attributes applied to the header root.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>
    /// Moves focus to the route-level heading. Pages use this when restoring context after navigation.
    /// </summary>
    public ValueTask FocusHeadingAsync() => _headingElement.FocusAsync();

    private string ResolvedPageTitle
        => string.IsNullOrWhiteSpace(PageTitle) ? Heading : PageTitle;

    private string CssClass
        => string.IsNullOrWhiteSpace(Class) ? "fc-page-header" : $"fc-page-header {Class}";

    /// <inheritdoc />
    protected override void OnParametersSet() {
        base.OnParametersSet();
        ArgumentException.ThrowIfNullOrWhiteSpace(Heading);
    }
}
