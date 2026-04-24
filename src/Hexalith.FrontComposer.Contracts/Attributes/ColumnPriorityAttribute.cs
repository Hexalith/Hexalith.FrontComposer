namespace Hexalith.FrontComposer.Contracts.Attributes;

/// <summary>
/// Declares the column priority for a projection property. Lower values render first;
/// unannotated properties implicitly sort to <see cref="int.MaxValue"/> so explicitly prioritised
/// columns precede them while declaration order is preserved as a deterministic tiebreaker
/// (Story 4-4 Decision D14 / D17).
/// </summary>
/// <remarks>
/// Drives the <c>FcColumnPrioritizer</c> gate when a projection declares more than 15 columns
/// (UX-DR63 / UX-DR7). Priority collisions within a single projection surface via HFC1028
/// Information at parse time; the deterministic fallback is declaration order.
/// <para>
/// <b>Author obligation</b> — the chosen <c>ItemKey</c> property (<c>AggregateId</c> / <c>Id</c> /
/// <c>Key</c>) must be non-null for every row visible in a list view (Story 4-4 D13 null-handling
/// contract). First-match resolution does not fall through on runtime null values; Blazor's
/// <c>@key</c> collapses null identifiers into a single slot, masking row identity.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class ColumnPriorityAttribute : Attribute {
    /// <summary>
    /// Initializes a new instance of the <see cref="ColumnPriorityAttribute"/> class.
    /// </summary>
    /// <param name="priority">
    /// Priority value. Lower = higher display priority. Any 32-bit signed integer is accepted —
    /// <see cref="int.MinValue"/> is a legitimate "pin to front" value and <see cref="int.MaxValue"/>
    /// is reserved for the "unannotated sentinel" used at sort time.
    /// </param>
    public ColumnPriorityAttribute(int priority) => Priority = priority;

    /// <summary>Gets the declared column priority. Lower values render first.</summary>
    public int Priority { get; }
}
