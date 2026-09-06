using Hexalith.FrontComposer.Contracts.Rendering;

namespace Hexalith.FrontComposer.SourceTools.Tests.Integration;

internal sealed class ResolvedDerivedValueProvider(object? value) : IDerivedValueProvider {
    public int Calls { get; private set; }

    public Task<DerivedValueResult> ResolveAsync(
        Type commandType,
        string propertyName,
        ProjectionContext? projectionContext,
        CancellationToken ct) {
        Calls++;
        return Task.FromResult(new DerivedValueResult(true, value));
    }
}
