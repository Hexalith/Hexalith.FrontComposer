namespace Hexalith.FrontComposer.Contracts.DevMode;

/// <summary>
/// Effective customization level for a generated element, ordered by override precedence.
/// </summary>
public enum CustomizationLevel {
    /// <summary>Generated convention output with no adopter override.</summary>
    Default = 0,

    /// <summary>Level 1 attribute or descriptor metadata override.</summary>
    Level1 = 1,

    /// <summary>Level 2 typed Razor template override.</summary>
    Level2 = 2,

    /// <summary>Level 3 field-slot replacement override.</summary>
    Level3 = 3,

    /// <summary>Level 4 full projection-view replacement override.</summary>
    Level4 = 4,
}
