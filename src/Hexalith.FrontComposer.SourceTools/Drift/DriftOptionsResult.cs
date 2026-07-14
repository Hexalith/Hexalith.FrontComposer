using System.Collections.Immutable;

namespace Hexalith.FrontComposer.SourceTools.Drift;

internal sealed class DriftOptionsResult(
    DriftOptions options,
    ImmutableArray<DriftDiagnosticFact> diagnostics) {
    internal DriftOptions Options { get; } = options;
    internal ImmutableArray<DriftDiagnosticFact> Diagnostics { get; } = diagnostics;
}
