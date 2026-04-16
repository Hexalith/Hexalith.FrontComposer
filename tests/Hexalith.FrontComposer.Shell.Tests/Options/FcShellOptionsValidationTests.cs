using System.ComponentModel.DataAnnotations;

using Hexalith.FrontComposer.Contracts;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Options;

/// <summary>
/// TDD RED-phase options-validation tests for Story 2-4 Task 3.1 / 3.2. The default values
/// must satisfy the ordered-threshold invariant (Pulse &lt; StillSyncing &lt; TimeoutAction)
/// and each <see cref="RangeAttribute"/> must reject out-of-bound values.
/// </summary>
public sealed class FcShellOptionsValidationTests {
    [Fact(Skip = "TDD RED — Story 2-4 Task 3.1: default threshold values must satisfy the ordered validator (300 < 2000 < 10000).")]
    public void Defaults_satisfy_ordered_thresholds_validator() {
        FcShellOptions options = new();

        options.SyncPulseThresholdMs.ShouldBeLessThan(options.StillSyncingThresholdMs);
        options.StillSyncingThresholdMs.ShouldBeLessThan(options.TimeoutActionThresholdMs);

        List<ValidationResult> results = ValidateDataAnnotations(options);
        results.ShouldBeEmpty();
    }

    [Fact(Skip = "TDD RED — Story 2-4 Task 3.2: FcShellOptionsThresholdValidator rejects SyncPulse >= StillSyncing with a clear error message.")]
    public void SyncPulse_gte_StillSyncing_fails_validation_with_clear_message() {
        FcShellOptions options = new() {
            SyncPulseThresholdMs = 2_000,
            StillSyncingThresholdMs = 2_000, // violation: must be strictly less than.
            TimeoutActionThresholdMs = 10_000,
        };

        Microsoft.Extensions.Options.IValidateOptions<FcShellOptions> validator = ResolveThresholdValidator();
        Microsoft.Extensions.Options.ValidateOptionsResult result = validator.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("SyncPulseThresholdMs", Case.Insensitive);
        result.FailureMessage.ShouldContain("StillSyncingThresholdMs", Case.Insensitive);
    }

    [Theory(Skip = "TDD RED — Story 2-4 Task 3.1: [Range] annotations enforce min/max bounds on each new threshold property.")]
    [InlineData(nameof(FcShellOptions.SyncPulseThresholdMs), 49)]
    [InlineData(nameof(FcShellOptions.SyncPulseThresholdMs), 2_001)]
    [InlineData(nameof(FcShellOptions.StillSyncingThresholdMs), 499)]
    [InlineData(nameof(FcShellOptions.StillSyncingThresholdMs), 10_001)]
    [InlineData(nameof(FcShellOptions.TimeoutActionThresholdMs), 4_999)]
    [InlineData(nameof(FcShellOptions.TimeoutActionThresholdMs), 60_001)]
    [InlineData(nameof(FcShellOptions.ConfirmedToastDurationMs), 999)]
    [InlineData(nameof(FcShellOptions.ConfirmedToastDurationMs), 30_001)]
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

    private static Microsoft.Extensions.Options.IValidateOptions<FcShellOptions> ResolveThresholdValidator() {
        // Task 3.2 creates FcShellOptionsThresholdValidator in Hexalith.FrontComposer.Shell.Options.
        // Test locates it via reflection so this test file does not hard-code the concrete
        // namespace path; if the type moves during implementation the test still finds it as long
        // as it is the registered IValidateOptions<FcShellOptions>.
        Type? validatorType = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => SafeGetTypes(a))
            .FirstOrDefault(t =>
                typeof(Microsoft.Extensions.Options.IValidateOptions<FcShellOptions>).IsAssignableFrom(t)
                && !t.IsAbstract);

        validatorType.ShouldNotBeNull("Task 3.2 must ship an IValidateOptions<FcShellOptions> implementation.");
        return (Microsoft.Extensions.Options.IValidateOptions<FcShellOptions>)Activator.CreateInstance(validatorType!)!;
    }

    private static IEnumerable<Type> SafeGetTypes(System.Reflection.Assembly a) {
        try {
            return a.GetTypes();
        } catch (System.Reflection.ReflectionTypeLoadException ex) {
            return ex.Types.Where(t => t is not null)!;
        }
    }
}
