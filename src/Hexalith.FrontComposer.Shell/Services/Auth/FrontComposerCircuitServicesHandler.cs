using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;

namespace Hexalith.FrontComposer.Shell.Services.Auth;

/// <summary>
/// Publishes the circuit's scoped <see cref="IServiceProvider"/> into <see cref="CircuitServicesAccessor"/>
/// for the duration of each inbound circuit activity, enabling outbound HTTP handlers to read
/// circuit-scoped services (for example <see cref="AuthenticationStateProvider"/>).
/// </summary>
public sealed class FrontComposerCircuitServicesHandler(
    IServiceProvider circuitServices,
    CircuitServicesAccessor accessor) : CircuitHandler {
    /// <inheritdoc />
    public override Func<CircuitInboundActivityContext, Task> CreateInboundActivityHandler(
        Func<CircuitInboundActivityContext, Task> next)
        => async context => {
            accessor.Services = circuitServices;
            try {
                await next(context).ConfigureAwait(false);
            }
            finally {
                accessor.Services = null;
            }
        };
}
