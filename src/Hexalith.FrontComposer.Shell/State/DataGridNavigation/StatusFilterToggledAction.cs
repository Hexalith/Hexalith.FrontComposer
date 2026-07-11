using Hexalith.FrontComposer.Contracts.Rendering;

namespace Hexalith.FrontComposer.Shell.State.DataGridNavigation;

/// <summary>
/// Story 4-3 D1 / AC2 — status-chip toggle dispatched by <c>FcStatusFilterChips</c>.
/// Reducer reads the CSV at <see cref="ReservedFilterKeys.StatusKey"/>, toggles the
/// slot name, writes back, and chains <see cref="CaptureGridStateAction"/>.
/// </summary>
public sealed record StatusFilterToggledAction {

    /// <summary>Initializes a new instance of the <see cref="StatusFilterToggledAction"/> record.</summary>
    /// <param name="viewKey">Stable per-view key.</param>
    /// <param name="slotName">Slot name (enum member name, e.g. <c>Success</c>).</param>
    /// <exception cref="System.ArgumentException">Thrown when either argument is null, empty, or whitespace.</exception>
    public StatusFilterToggledAction(string viewKey, string slotName) {
        ViewKey = viewKey;
        SlotName = slotName;
    }

    /// <summary>Gets the stable per-view key.</summary>
    public string ViewKey {
        get;
        init {
            if (string.IsNullOrWhiteSpace(value)) {
                throw new System.ArgumentException("View key cannot be null, empty, or whitespace.", nameof(value));
            }

            field = value;
        }
    } = string.Empty;

    /// <summary>Gets the slot name to toggle.</summary>
    public string SlotName {
        get;
        init {
            if (string.IsNullOrWhiteSpace(value)) {
                throw new System.ArgumentException("Slot name cannot be null, empty, or whitespace.", nameof(value));
            }

            field = value;
        }
    } = string.Empty;
}
