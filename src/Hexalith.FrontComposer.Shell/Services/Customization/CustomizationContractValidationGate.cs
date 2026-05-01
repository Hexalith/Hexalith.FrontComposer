using System.Text;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.Contracts.Rendering;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Shell.Services.Customization;

/// <summary>
/// Story 6-6 P17 / AC2. Hosted service that runs after the three customization registries
/// have hydrated their descriptors. When
/// <see cref="FcShellOptions.CustomizationContractValidation"/> is
/// <see cref="CustomizationContractValidationMode.FailClosedOnMajorMismatch"/>, throws if any
/// Major-mismatched descriptor was rejected by Level 2 / 3 / 4 registries during their
/// constructor pass. In <see cref="CustomizationContractValidationMode.LogAndSkip"/> (default),
/// this service is a no-op — the per-registry warning logs already surfaced the rejections.
/// </summary>
/// <remarks>
/// This service depends on the registry interfaces so registrations are forced to
/// instantiate (and therefore hydrate) before this gate runs. The dependency on
/// <see cref="ICustomizationContractRejectionLog"/> aggregates the rejection records emitted
/// by all three registries.
/// </remarks>
internal sealed class CustomizationContractValidationGate : IHostedService {
    private readonly IOptions<FcShellOptions> _options;
    private readonly ICustomizationContractRejectionLog _rejectionLog;
    private readonly ILogger<CustomizationContractValidationGate> _logger;

    public CustomizationContractValidationGate(
        IOptions<FcShellOptions> options,
        ICustomizationContractRejectionLog rejectionLog,
        ILogger<CustomizationContractValidationGate> logger,
        // Force-resolve registries so their constructors hydrate before validation.
        IProjectionTemplateRegistry templateRegistry,
        IProjectionSlotRegistry slotRegistry,
        IProjectionViewOverrideRegistry viewOverrideRegistry) {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(rejectionLog);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(templateRegistry);
        ArgumentNullException.ThrowIfNull(slotRegistry);
        ArgumentNullException.ThrowIfNull(viewOverrideRegistry);
        _options = options;
        _rejectionLog = rejectionLog;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken) {
        FcShellOptions options = _options.Value;
        IReadOnlyList<CustomizationContractRejection> rejections = _rejectionLog.Rejections;

        if (options.CustomizationContractValidation == CustomizationContractValidationMode.LogAndSkip) {
            return Task.CompletedTask;
        }

        if (rejections.Count == 0) {
            return Task.CompletedTask;
        }

        StringBuilder message = new();
        _ = message.Append(
            "FrontComposer customization-contract validation is in FailClosedOnMajorMismatch mode and detected ");
        _ = message.Append(rejections.Count.ToString(System.Globalization.CultureInfo.InvariantCulture));
        _ = message.Append(" Major-mismatched override registration");
        _ = message.Append(rejections.Count == 1 ? "" : "s");
        _ = message.Append(". Adopters opting into strict validation must rebuild the affected ");
        _ = message.Append("templates / slot components / view replacements against the installed framework, ");
        _ = message.Append("or set FcShellOptions.CustomizationContractValidation = LogAndSkip.\n\n");

        for (int i = 0; i < rejections.Count; i++) {
            CustomizationContractRejection r = rejections[i];
            _ = message.Append("[").Append(i + 1).Append("] ");
            _ = message.Append(r.DiagnosticId).Append(' ');
            _ = message.Append(r.Level).Append(' ');
            _ = message.Append("projection=").Append(r.ProjectionTypeName).Append(' ');
            if (!string.IsNullOrWhiteSpace(r.FieldName)) {
                _ = message.Append("field=").Append(r.FieldName).Append(' ');
            }

            _ = message.Append("role=").Append(r.Role).Append(' ');
            _ = message.Append("component=").Append(r.ComponentTypeName).Append(' ');
            _ = message.Append("decision=").Append(r.Comparison.Decision).Append(' ');
            _ = message.Append("expected=")
                .Append(r.Comparison.Expected.Major).Append('.')
                .Append(r.Comparison.Expected.Minor).Append('.')
                .Append(r.Comparison.Expected.Build).Append(' ');
            _ = message.Append("actual=")
                .Append(r.Comparison.Actual.Major).Append('.')
                .Append(r.Comparison.Actual.Minor).Append('.')
                .Append(r.Comparison.Actual.Build).Append(' ');
            _ = message.Append("docs=https://hexalith.github.io/FrontComposer/diagnostics/")
                .Append(r.DiagnosticId).Append('\n');
        }

        string final = message.ToString();
        _logger.LogError("{DiagnosticId}: {Message}", FcDiagnosticIds.HFC1601_ManifestInvalid, final);
        throw new InvalidOperationException(final);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
