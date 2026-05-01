namespace Hexalith.FrontComposer.Shell.Services.Customization;

/// <summary>
/// Singleton log of Major-mismatched customization-contract rejections recorded by the Level 2,
/// 3, and 4 registries at hydration. Read by <see cref="CustomizationContractValidationGate"/>
/// at startup. Story 6-6 P17 / AC2.
/// </summary>
public interface ICustomizationContractRejectionLog {
    /// <summary>Gets a snapshot of recorded rejections.</summary>
    IReadOnlyList<CustomizationContractRejection> Rejections { get; }

    /// <summary>Records a rejection.</summary>
    void Record(CustomizationContractRejection rejection);
}
