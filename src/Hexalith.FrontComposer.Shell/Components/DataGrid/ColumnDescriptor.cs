namespace Hexalith.FrontComposer.Shell.Components.DataGrid;

/// <summary>
/// Descriptor for one column exposed by the prioritizer's popover. Carried by the generator-emitted
/// wrap.
/// </summary>
/// <param name="Key">Declared property name (the column's stable key).</param>
/// <param name="Header">Human-readable header text (post-<c>CamelCaseHumanizer</c> / <c>[Display]</c>).</param>
/// <param name="Priority">Declared priority or <see langword="null"/> when unannotated.</param>
public sealed record ColumnDescriptor(string Key, string Header, int? Priority);
