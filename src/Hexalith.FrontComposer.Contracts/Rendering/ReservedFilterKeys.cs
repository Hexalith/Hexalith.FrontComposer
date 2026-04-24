namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>
/// Story 4-3 D3 — reserved <see cref="GridViewSnapshot.Filters"/> keys used to pack
/// non-column filter state (status chip membership, global search query) into the
/// frozen Story 2-2 / 3-6 blob schema without a contract bump.
/// </summary>
/// <remarks>
/// Both keys start with <c>__</c> to partition against any C# property-named column key
/// (C# identifier convention prevents real projection properties from starting with
/// two underscores). Changing either constant is a MAJOR version bump — it invalidates
/// every persisted filter blob in the wild.
/// </remarks>
public static class ReservedFilterKeys {
    /// <summary>Reserved key holding a CSV of active <c>BadgeSlot</c> names.</summary>
    public const string StatusKey = "__status";

    /// <summary>Reserved key holding the current global search query.</summary>
    public const string SearchKey = "__search";
}
