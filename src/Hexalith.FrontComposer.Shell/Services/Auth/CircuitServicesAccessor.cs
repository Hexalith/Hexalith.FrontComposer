namespace Hexalith.FrontComposer.Shell.Services.Auth;

/// <summary>
/// Holds the current Blazor circuit's service provider in an <see cref="AsyncLocal{T}"/> so pooled
/// infrastructure (such as <see cref="DelegatingHandler"/> instances created by
/// <see cref="IHttpClientFactory"/>) can resolve circuit-scoped services while an inbound circuit
/// activity is executing. Registered as a singleton; the value is published per inbound activity by
/// <see cref="FrontComposerCircuitServicesHandler"/>.
/// </summary>
public sealed class CircuitServicesAccessor {
    // Instance field (the accessor is a DI singleton): AsyncLocal still flows per async/circuit
    // activity, and an instance member keeps the singleton's Services accessor non-static.
    private readonly AsyncLocal<IServiceProvider?> _current = new();

    /// <summary>The service provider scoped to the currently executing circuit activity, if any.</summary>
    public IServiceProvider? Services {
        get => _current.Value;
        set => _current.Value = value;
    }
}
