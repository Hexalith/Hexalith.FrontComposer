using Hexalith.FrontComposer.Contracts;

using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Shell.Options;

/// <summary>
/// Story 2-4 Decision D12 — cross-property ordering validator for the four lifecycle thresholds
/// on <see cref="FcShellOptions"/>. <c>[Range]</c> attributes cover per-property bounds; this
/// validator enforces <c>SyncPulse &lt; StillSyncing &lt; TimeoutAction</c>.
/// </summary>
/// <remarks>
/// Registered <c>AFTER</c> <see cref="OptionsBuilderDataAnnotationsExtensions.ValidateDataAnnotations{TOptions}"/>
/// so <c>[Range]</c> failures surface first (Amelia review 2026-04-16 Medium 2).
/// </remarks>
public sealed class FcShellOptionsThresholdValidator : IValidateOptions<FcShellOptions> {
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, FcShellOptions options) {
        ArgumentNullException.ThrowIfNull(options);

        List<string>? failures = null;
        if (options.SyncPulseThresholdMs >= options.StillSyncingThresholdMs) {
            failures = [$"FcShellOptions.SyncPulseThresholdMs ({options.SyncPulseThresholdMs}) must be strictly less than StillSyncingThresholdMs ({options.StillSyncingThresholdMs})."];
        }

        if (options.StillSyncingThresholdMs >= options.TimeoutActionThresholdMs) {
            (failures ??= []).Add($"FcShellOptions.StillSyncingThresholdMs ({options.StillSyncingThresholdMs}) must be strictly less than TimeoutActionThresholdMs ({options.TimeoutActionThresholdMs}).");
        }

        return failures is null ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
