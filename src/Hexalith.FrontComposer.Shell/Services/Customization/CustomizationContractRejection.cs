using Hexalith.FrontComposer.Contracts.DevMode;
using Hexalith.FrontComposer.Contracts.Rendering;

namespace Hexalith.FrontComposer.Shell.Services.Customization;

/// <summary>
/// Records a Major-mismatched customization-contract rejection captured by a registry during
/// startup hydration. Story 6-6 P17 / AC2. Used by
/// <see cref="ICustomizationContractRejectionLog"/> to feed
/// <see cref="CustomizationContractValidationGate"/>.
/// </summary>
/// <param name="Level">Customization level the rejection applies to.</param>
/// <param name="ProjectionTypeName">Sanitized projection type name (FullName).</param>
/// <param name="ComponentTypeName">Sanitized component / template / replacement type name (FullName).</param>
/// <param name="Role">Projection role string when applicable; "&lt;any&gt;" for role-agnostic registrations.</param>
/// <param name="FieldName">Field name when applicable (Level 3 only).</param>
/// <param name="Comparison">Contract-version comparison outcome.</param>
/// <param name="DiagnosticId">Stable HFC ID associated with the rejection.</param>
public sealed record CustomizationContractRejection(
    CustomizationLevel Level,
    string ProjectionTypeName,
    string ComponentTypeName,
    string Role,
    string? FieldName,
    CustomizationContractVersionComparison Comparison,
    string DiagnosticId);
