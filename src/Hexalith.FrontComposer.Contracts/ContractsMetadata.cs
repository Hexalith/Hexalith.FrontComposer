namespace Hexalith.FrontComposer.Contracts;

/// <summary>
/// Framework-owned metadata constants for Contracts consumers (version pins, policy markers).
/// </summary>
public static class ContractsMetadata
{
    /// <summary>
    /// Version pin for the <c>Typography</c> role → Fluent UI primitive mapping (Story 3-1 D11 /
    /// AC5). Patch bumps MUST NOT change the mapping table; minor bumps require a changelog entry
    /// and a before/after screenshot committed to <c>docs/typography-baseline/</c>; major bumps
    /// may restructure with migration notes. Asserted in
    /// <c>TypographyConstantsTests.cs</c>.
    /// </summary>
    public const string TypographyMappingVersion = "3.1.0";
}
