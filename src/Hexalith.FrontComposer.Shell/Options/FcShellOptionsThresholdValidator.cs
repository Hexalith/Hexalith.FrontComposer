using Hexalith.FrontComposer.Contracts;

using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Shell.Options;

/// <summary>
/// Story 2-4 Decision D12 + Story 2-5 Decision D6 — cross-property validator for lifecycle + form
/// thresholds on <see cref="FcShellOptions"/>. <c>[Range]</c> attributes cover per-property bounds;
/// this validator enforces:
/// <list type="bullet">
///   <item><description><c>SyncPulse &lt; StillSyncing &lt; TimeoutAction</c> (Story 2-4).</description></item>
///   <item><description><c>FormAbandonmentThresholdSeconds * 1000 &gt; StillSyncingThresholdMs</c> (Story 2-5) — abandonment must not fire before "Still syncing…" has had a chance to show.</description></item>
///   <item><description><c>IdempotentInfoToastDurationMs &lt;= ConfirmedToastDurationMs</c> (Story 2-5) — Info dismisses no later than Success for UX consistency.</description></item>
/// </list>
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

        if (options.FormAbandonmentThresholdSeconds * 1000L <= options.StillSyncingThresholdMs) {
            (failures ??= []).Add($"FcShellOptions.FormAbandonmentThresholdSeconds ({options.FormAbandonmentThresholdSeconds}s = {options.FormAbandonmentThresholdSeconds * 1000}ms) must exceed StillSyncingThresholdMs ({options.StillSyncingThresholdMs}ms) so abandonment cannot fire before the still-syncing badge has had a chance to show.");
        }

        if (options.IdempotentInfoToastDurationMs > options.ConfirmedToastDurationMs) {
            (failures ??= []).Add($"FcShellOptions.IdempotentInfoToastDurationMs ({options.IdempotentInfoToastDurationMs}) must not exceed ConfirmedToastDurationMs ({options.ConfirmedToastDurationMs}); the idempotent Info bar must dismiss no later than the Success bar.");
        }

        // Story 3-1 D14 / AC7 — SupportedCultures must include DefaultCulture, otherwise request
        // localization silently falls back to the invariant culture and our EN/FR resource files
        // never resolve. Non-OrdinalIgnoreCase (plain Ordinal) so "en" and "EN" are distinct — BCP-47
        // convention uses lowercase language tags which the RegularExpression on DefaultCulture enforces.
        if (options.SupportedCultures is not null
            && !options.SupportedCultures.Contains(options.DefaultCulture, StringComparer.Ordinal)) {
            (failures ??= []).Add($"FcShellOptions.SupportedCultures must include DefaultCulture ('{options.DefaultCulture}'); currently contains [{string.Join(", ", options.SupportedCultures)}].");
        }

        // Story 4-4 D10 — the server-side virtualization lane must trigger strictly BEFORE the
        // unfiltered cap, otherwise a projection hitting the cap would full-render (client-side lane)
        // before the ItemsProvider path had a chance to page.
        if (options.VirtualizationServerSideThreshold >= options.MaxUnfilteredItems) {
            (failures ??= []).Add($"FcShellOptions.VirtualizationServerSideThreshold ({options.VirtualizationServerSideThreshold}) must be strictly less than MaxUnfilteredItems ({options.MaxUnfilteredItems}).");
        }

        return failures is null ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
