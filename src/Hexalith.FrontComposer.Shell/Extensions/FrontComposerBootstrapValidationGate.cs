using Hexalith.FrontComposer.Shell.Infrastructure.Telemetry;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Hexalith.FrontComposer.Shell.Extensions;

/// <summary>
/// Story 1.1 AC2 — hosted service that runs the <see cref="FrontComposerBootstrapValidator"/> at
/// host start, before first render. Mirrors the two existing fail-fast precedents
/// (<c>CustomizationContractValidationGate</c>, <c>FrontComposerAuthorizationPolicyCatalogValidator</c>):
/// an <see cref="IHostedService.StartAsync"/> that logs then throws an
/// <see cref="InvalidOperationException"/> when the bootstrap is misconfigured.
/// </summary>
/// <remarks>
/// Registered (idempotently, via <c>AddHostedService</c> → <c>TryAddEnumerable</c>) by every
/// FrontComposer bootstrap entry point so the missing-foundational-call case is detectable even when
/// the adopter wires <em>only</em> a downstream call (e.g. <c>AddHexalithEventStore(...)</c> without
/// <c>AddHexalithFrontComposerQuickstart()</c>). Depends only on the singleton markers and a logger,
/// so it is scope-safe under <c>ValidateScopes = true</c> (ADR-030).
/// </remarks>
internal sealed class FrontComposerBootstrapValidationGate : IHostedService {
    private readonly IEnumerable<IFrontComposerBootstrapMarker> _markers;
    private readonly ILogger<FrontComposerBootstrapValidationGate> _logger;

    /// <summary>
    /// Initialises a new instance of the <see cref="FrontComposerBootstrapValidationGate"/> class.
    /// </summary>
    /// <param name="markers">The registered bootstrap ordering markers (DI preserves insertion order).</param>
    /// <param name="logger">Logger used to surface the validation failure before it is thrown.</param>
    public FrontComposerBootstrapValidationGate(
        IEnumerable<IFrontComposerBootstrapMarker> markers,
        ILogger<FrontComposerBootstrapValidationGate> logger) {
        ArgumentNullException.ThrowIfNull(markers);
        ArgumentNullException.ThrowIfNull(logger);
        _markers = markers;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();

        try {
            FrontComposerBootstrapValidator.Validate(_markers);
        }
        catch (InvalidOperationException ex) {
            // Mirror CustomizationContractValidationGate: log the message first, then throw so the
            // host start fails fast with the same named diagnostic surfaced in both channels.
            FrontComposerWarningLog.BootstrapValidationFailed(_logger, ex);
            throw;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
