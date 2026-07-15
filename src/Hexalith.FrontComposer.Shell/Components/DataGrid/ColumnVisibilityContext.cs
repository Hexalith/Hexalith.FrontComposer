namespace Hexalith.FrontComposer.Shell.Components.DataGrid;

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
