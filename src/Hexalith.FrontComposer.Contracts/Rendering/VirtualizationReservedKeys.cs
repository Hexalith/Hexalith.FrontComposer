namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>
/// Story 4-4 D7 — reserved <see cref="GridViewSnapshot.Filters"/> key used to pack
/// the hidden-columns list (column visibility toggled via <c>FcColumnPrioritizer</c>)
/// into the frozen Story 2-2 / 3-6 blob schema without a contract bump.
/// </summary>
/// <remarks>
/// Extends the Story 4-3 reserved-key convention (<see cref="ReservedFilterKeys"/>) with a third
/// entry: the value stored under <see cref="HiddenColumnsKey"/> is a CSV of column keys
/// (property names) that are currently hidden. C# identifier rules prevent collision with any real
/// projection property because the key begins with two underscores.
/// <para>
/// Changing the string constant is a MAJOR version bump — every persisted blob that includes
/// hidden columns would be invalidated.
/// </para>
/// </remarks>
public static class VirtualizationReservedKeys {
    /// <summary>Reserved key holding a CSV of column keys (property names) hidden by the user.</summary>
    public const string HiddenColumnsKey = "__hidden";
}
