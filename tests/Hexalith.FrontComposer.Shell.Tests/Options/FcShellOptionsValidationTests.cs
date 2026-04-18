using System.ComponentModel.DataAnnotations;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Shell.Options;

using Microsoft.Extensions.Options;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Options;

/// <summary>
/// Story 2-4 Task 3.1 / 3.2 — options-validation tests. Default values satisfy the ordered-threshold
/// invariant (Pulse &lt; StillSyncing &lt; TimeoutAction); each <see cref="RangeAttribute"/> rejects
/// out-of-bound values.
/// </summary>
public sealed class FcShellOptionsValidationTests {
    [Fact]
    public void Defaults_satisfy_ordered_thresholds_validator() {
        FcShellOptions options = new();

        options.SyncPulseThresholdMs.ShouldBeLessThan(options.StillSyncingThresholdMs);
        options.StillSyncingThresholdMs.ShouldBeLessThan(options.TimeoutActionThresholdMs);

        ValidateDataAnnotations(options).ShouldBeEmpty();
        new FcShellOptionsThresholdValidator().Validate(null, options).Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void FormAbandonment_must_exceed_StillSyncing() {
        // Story 2-5 D6 cross-property invariant #1: abandonment window must not fire before still-syncing visible.
        FcShellOptions options = new() {
            FormAbandonmentThresholdSeconds = 2, // 2000 ms == StillSyncingThresholdMs default
        };

        ValidateOptionsResult result = new FcShellOptionsThresholdValidator().Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("FormAbandonmentThresholdSeconds", Case.Insensitive);
        result.FailureMessage.ShouldContain("StillSyncingThresholdMs", Case.Insensitive);
    }

    [Fact]
    public void Idempotent_toast_must_not_exceed_ConfirmedToast() {
        // Story 2-5 D6 cross-property invariant #2: Info dismisses no later than Success.
        FcShellOptions options = new() {
            ConfirmedToastDurationMs = 3_000,
            IdempotentInfoToastDurationMs = 5_000,
        };

        ValidateOptionsResult result = new FcShellOptionsThresholdValidator().Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("IdempotentInfoToastDurationMs", Case.Insensitive);
        result.FailureMessage.ShouldContain("ConfirmedToastDurationMs", Case.Insensitive);
    }

    [Fact]
    public void SyncPulse_gte_StillSyncing_fails_validation_with_clear_message() {
        FcShellOptions options = new() {
            SyncPulseThresholdMs = 2_000,
            StillSyncingThresholdMs = 2_000,
            TimeoutActionThresholdMs = 10_000,
        };

        ValidateOptionsResult result = new FcShellOptionsThresholdValidator().Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("SyncPulseThresholdMs", Case.Insensitive);
        result.FailureMessage.ShouldContain("StillSyncingThresholdMs", Case.Insensitive);
    }

    [Theory]
    [InlineData(nameof(FcShellOptions.SyncPulseThresholdMs), 49)]
    [InlineData(nameof(FcShellOptions.SyncPulseThresholdMs), 2_001)]
    [InlineData(nameof(FcShellOptions.StillSyncingThresholdMs), 499)]
    [InlineData(nameof(FcShellOptions.StillSyncingThresholdMs), 10_001)]
    [InlineData(nameof(FcShellOptions.TimeoutActionThresholdMs), 4_999)]
    [InlineData(nameof(FcShellOptions.TimeoutActionThresholdMs), 60_001)]
    [InlineData(nameof(FcShellOptions.ConfirmedToastDurationMs), 999)]
    [InlineData(nameof(FcShellOptions.ConfirmedToastDurationMs), 30_001)]
    [InlineData(nameof(FcShellOptions.FormAbandonmentThresholdSeconds), 4)]
    [InlineData(nameof(FcShellOptions.FormAbandonmentThresholdSeconds), 601)]
    [InlineData(nameof(FcShellOptions.IdempotentInfoToastDurationMs), 999)]
    [InlineData(nameof(FcShellOptions.IdempotentInfoToastDurationMs), 30_001)]
    public void Range_annotations_enforce_min_max_bounds_on_each_threshold_property(string propertyName, int outOfRangeValue) {
        FcShellOptions options = new();
        typeof(FcShellOptions).GetProperty(propertyName)!.SetValue(options, outOfRangeValue);

        List<ValidationResult> results = ValidateDataAnnotations(options);

        results.ShouldContain(r => r.MemberNames.Contains(propertyName));
    }

    private static List<ValidationResult> ValidateDataAnnotations(object instance) {
        ValidationContext ctx = new(instance);
        List<ValidationResult> results = [];
        _ = Validator.TryValidateObject(instance, ctx, results, validateAllProperties: true);
        return results;
    }
}
