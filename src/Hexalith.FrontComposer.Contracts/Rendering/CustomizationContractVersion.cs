namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>
/// Packed customization contract version triplet.
/// </summary>
public readonly record struct CustomizationContractVersion(int Major, int Minor, int Build) {
    /// <summary>Packs the version as <c>Major * 1_000_000 + Minor * 1_000 + Build</c>.</summary>
    public int Packed => (Major * 1_000_000) + (Minor * 1_000) + Build;

    /// <summary>Unpacks a packed customization contract version.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="packed"/> is negative.</exception>
    public static CustomizationContractVersion Unpack(int packed) {
        if (packed < 0) {
            throw new ArgumentOutOfRangeException(
                nameof(packed),
                packed,
                "Packed customization contract versions are non-negative integers of the form Major*1_000_000 + Minor*1_000 + Build.");
        }

        return new(packed / 1_000_000, (packed / 1_000) % 1_000, packed % 1_000);
    }

    /// <summary>
    /// Compares an override's expected version against the installed contract version.
    /// </summary>
    public static CustomizationContractVersionComparison Compare(int expectedPacked, int actualPacked) {
        CustomizationContractVersion expected = Unpack(expectedPacked);
        CustomizationContractVersion actual = Unpack(actualPacked);

        if (expected.Major != actual.Major) {
            return new(expected, actual, CustomizationContractVersionDecision.MajorMismatch, CanSelect: false, ShouldReportDiagnostic: true);
        }

        if (expected.Minor != actual.Minor) {
            return new(expected, actual, CustomizationContractVersionDecision.MinorDrift, CanSelect: true, ShouldReportDiagnostic: true);
        }

        if (expected.Build != actual.Build) {
            return new(expected, actual, CustomizationContractVersionDecision.BuildDrift, CanSelect: true, ShouldReportDiagnostic: false);
        }

        return new(expected, actual, CustomizationContractVersionDecision.Exact, CanSelect: true, ShouldReportDiagnostic: false);
    }
}

/// <summary>
/// Outcome for a customization contract-version comparison.
/// </summary>
public enum CustomizationContractVersionDecision {
    /// <summary>Expected and installed versions are identical.</summary>
    Exact = 0,

    /// <summary>Major versions differ; unsafe selection must be suppressed.</summary>
    MajorMismatch = 1,

    /// <summary>Major versions match but minor versions differ; selection can proceed with a warning.</summary>
    MinorDrift = 2,

    /// <summary>Only build metadata differs; selection proceeds without a diagnostic.</summary>
    BuildDrift = 3,
}

/// <summary>
/// Contract-version comparison result.
/// </summary>
public readonly record struct CustomizationContractVersionComparison(
    CustomizationContractVersion Expected,
    CustomizationContractVersion Actual,
    CustomizationContractVersionDecision Decision,
    bool CanSelect,
    bool ShouldReportDiagnostic);
