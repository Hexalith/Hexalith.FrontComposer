namespace Hexalith.FrontComposer.Testing;

/// <summary>The single permitted speech path for an announcement matrix event.</summary>
public enum UxAnnouncementChannel
{
    /// <summary>Shared polite status.</summary>
    PoliteStatus,
    /// <summary>Palette-owned polite combobox status.</summary>
    PoliteComboboxStatus,
    /// <summary>Complete non-live validation summary receiving focus.</summary>
    FocusedSummary,
    /// <summary>Non-live replacement heading receiving focus.</summary>
    FocusedHeading,
}
