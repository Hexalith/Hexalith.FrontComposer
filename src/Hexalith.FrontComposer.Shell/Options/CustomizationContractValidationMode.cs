namespace Hexalith.FrontComposer.Shell.Options;

/// <summary>
/// Runtime customization-contract validation policy selected by adopters via
/// <see cref="FcShellOptions.CustomizationContractValidation"/>.
/// </summary>
public enum CustomizationContractValidationMode {
    /// <summary>Default. Major-mismatched descriptors are skipped with a warning log; startup proceeds.</summary>
    LogAndSkip = 0,

    /// <summary>Strict. Any Major-mismatched descriptor causes startup to fail-closed with a clear diagnostic.</summary>
    FailClosedOnMajorMismatch = 1,
}
