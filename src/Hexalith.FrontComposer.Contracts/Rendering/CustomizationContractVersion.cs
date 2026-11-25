namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>
/// Packed customization contract version triplet.
/// </summary>
public readonly record struct CustomizationContractVersion(int Major, int Minor, int Build) {
    /// <summary>The multiplier for the minor component and radix for the build component.</summary>
    internal const int MinorFactor = 1_000;

    /// <summary>The multiplier for the major component.</summary>
    internal const int MajorFactor = MinorFactor * MinorFactor;

    /// <summary>Packs the version as <c>Major * 1_000_000 + Minor * 1_000 + Build</c>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The major component is negative, or the minor or build component is outside 0–999.
    /// </exception>
    /// <exception cref="OverflowException">The packed version exceeds <see cref="int.MaxValue"/>.</exception>
    public int Packed => Pack(Major, Minor, Build);

    private static int Pack(int major, int minor, int build)
    {
        if (major < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(major), major, "The major version must be nonnegative.");
        }

        if (minor < 0 || minor >= MinorFactor)
        {
            throw new ArgumentOutOfRangeException(nameof(minor), minor, "The minor version must be between 0 and 999.");
        }

        if (build < 0 || build >= MinorFactor)
        {
            throw new ArgumentOutOfRangeException(nameof(build), build, "The build version must be between 0 and 999.");
        }

        return checked((major * MajorFactor) + (minor * MinorFactor) + build);
    }

    /// <summary>Unpacks a packed customization contract version.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="packed"/> is negative.</exception>
    public static CustomizationContractVersion Unpack(int packed) {
        if (packed < 0) {
            throw new ArgumentOutOfRangeException(
                nameof(packed),
                packed,
                "Packed customization contract versions are non-negative integers of the form Major*1_000_000 + Minor*1_000 + Build.");
        }

        return new(packed / MajorFactor, packed / MinorFactor % MinorFactor, packed % MinorFactor);
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
