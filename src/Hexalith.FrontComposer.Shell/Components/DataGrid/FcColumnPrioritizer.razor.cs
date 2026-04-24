using System.Collections.Generic;
using System.Globalization;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Resources;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Hexalith.FrontComposer.Shell.Components.DataGrid;

/// <summary>
/// Story 4-4 T1.1 / D6 / AC5 / AC6 — wraps a projection's <c>FluentDataGrid</c> with a gear-icon
/// affordance pinned top-right OUTSIDE the scroll viewport. Activated by the source generator when
/// <c>model.Columns.Count &gt; 15</c>; generator always passes <see cref="MaxVisibleColumns"/>=10
/// per UX-DR7, but the parameter is exposed for adopters who wrap the component manually.
/// </summary>
/// <remarks>
/// <b>DOM structure:</b> the component emits a root <c>&lt;div class="fc-column-prioritizer"&gt;</c>
/// (CSS: <c>position: relative</c>) that is the positioning context for the absolutely-positioned
/// gear button. The CSS custom property <c>--fc-datagrid-affordance-z</c> controls stacking so
/// adopters can raise / lower without patching the component.
/// <para>
/// <b>ARIA:</b> the gear button carries an <c>aria-label</c> that switches between
/// <c>PrioritizerMoreColumnsAriaLabelTemplate</c> (plural-neutral colon form when hidden count ≥ 1)
/// and <c>PrioritizerColumnsAllVisibleAriaLabel</c> (when zero columns are hidden). The popover
/// carries <c>role="dialog"</c> and <c>aria-labelledby</c> on its header title.
/// </para>
/// </remarks>
public partial class FcColumnPrioritizer : ComponentBase {
    private readonly string _gearButtonId = $"fc-col-prio-gear-{Guid.NewGuid():N}";
    private readonly string _popoverTitleId = $"fc-col-prio-title-{Guid.NewGuid():N}";
    private readonly Microsoft.FluentUI.AspNetCore.Components.Icons.Regular.Size20.Settings _gearIcon = new();
    private FluentButton? _gearButton;
    private bool _popoverOpen;
    private string _gearAriaLabel = string.Empty;
    private string _checkboxAriaLabelTemplate = "{0}";
    private HashSet<string> _hiddenSet = new(StringComparer.Ordinal);
    private ColumnVisibilityContext _context = new(new HashSet<string>(StringComparer.Ordinal));

    /// <summary>Stable per-view key.</summary>
    [Parameter]
    [EditorRequired]
    public string ViewKey { get; set; } = string.Empty;

    /// <summary>
    /// Full list of columns in priority-then-declaration order (post-Transform sort); the source
    /// generator materialises this from the <see cref="Transforms.ColumnModel"/> IR.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public IReadOnlyList<ColumnDescriptor> AllColumns { get; set; } = Array.Empty<ColumnDescriptor>();

    /// <summary>
    /// Currently hidden column keys (property names). Typically bound to the CSV under
    /// <c>GridViewSnapshot.Filters["__hidden"]</c>.
    /// </summary>
    [Parameter]
    public IReadOnlyList<string> HiddenColumns { get; set; } = Array.Empty<string>();

    /// <summary>Max columns visible by default. Generator always emits 10 per UX-DR7.</summary>
    [Parameter]
    public int MaxVisibleColumns { get; set; } = 10;

    /// <summary>
    /// Render fragment for the wrapped <c>FluentDataGrid</c>. Receives a
    /// <see cref="ColumnVisibilityContext"/> so the data grid can skip rendering hidden columns.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public RenderFragment<ColumnVisibilityContext> ChildContent { get; set; } = default!;

    [Inject]
    private IDispatcher Dispatcher { get; set; } = default!;

    [Inject]
    private IStringLocalizer<FcShellResources> Localizer { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnParametersSet() {
        _hiddenSet = new HashSet<string>(HiddenColumns, StringComparer.Ordinal);
        _context = new ColumnVisibilityContext(_hiddenSet);
        _checkboxAriaLabelTemplate = Localizer["PrioritizerColumnCheckboxAriaLabelTemplate"].Value;
        UpdateGearAriaLabel();
    }

    private void UpdateGearAriaLabel() {
        int hidden = _hiddenSet.Count;
        _gearAriaLabel = hidden == 0
            ? Localizer["PrioritizerColumnsAllVisibleAriaLabel"].Value
            : Localizer[
                "PrioritizerMoreColumnsAriaLabelTemplate",
                hidden.ToString(CultureInfo.CurrentUICulture)].Value;
    }

    private Task OnGearClickedAsync() {
        _popoverOpen = !_popoverOpen;
        return Task.CompletedTask;
    }

    private Task OnVisibilityChangedAsync(string columnKey, bool isVisible) {
        Dispatcher.Dispatch(new ColumnVisibilityChangedAction(ViewKey, columnKey, isVisible));
        return Task.CompletedTask;
    }

    private Task OnResetToDefaultsAsync() {
        Dispatcher.Dispatch(new ResetColumnVisibilityAction(ViewKey));
        _popoverOpen = false;
        return Task.CompletedTask;
    }
}

/// <summary>
/// Descriptor for one column exposed by the prioritizer's popover. Carried by the generator-emitted
/// wrap.
/// </summary>
/// <param name="Key">Declared property name (the column's stable key).</param>
/// <param name="Header">Human-readable header text (post-<c>CamelCaseHumanizer</c> / <c>[Display]</c>).</param>
/// <param name="Priority">Declared priority or <see langword="null"/> when unannotated.</param>
public sealed record ColumnDescriptor(string Key, string Header, int? Priority);

/// <summary>
/// Render context passed to the prioritizer's child fragment so the wrapped <c>FluentDataGrid</c>
/// can omit rendering hidden columns via <c>@if (!Context.IsHidden(key))</c>.
/// </summary>
/// <param name="HiddenKeys">Set of hidden column keys (case-sensitive).</param>
public sealed class ColumnVisibilityContext {
    private readonly ISet<string> _hiddenKeys;

    /// <summary>Initializes the visibility context.</summary>
    public ColumnVisibilityContext(ISet<string> hiddenKeys) {
        ArgumentNullException.ThrowIfNull(hiddenKeys);
        _hiddenKeys = hiddenKeys;
    }

    /// <summary>Returns <see langword="true"/> when <paramref name="columnKey"/> is hidden.</summary>
    public bool IsHidden(string columnKey) => _hiddenKeys.Contains(columnKey);
}
