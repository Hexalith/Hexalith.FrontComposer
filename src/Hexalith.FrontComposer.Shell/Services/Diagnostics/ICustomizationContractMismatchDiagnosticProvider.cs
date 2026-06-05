using Hexalith.FrontComposer.Contracts.Diagnostics;

namespace Hexalith.FrontComposer.Shell.Services.Diagnostics;

internal interface ICustomizationContractMismatchDiagnosticProvider {
    IReadOnlyList<CustomizationDiagnostic> GetDiagnostics();
}
