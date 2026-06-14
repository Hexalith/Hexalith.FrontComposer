using System.ComponentModel.DataAnnotations;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Contracts.Tests;

/// <summary>
/// Story 4-4 T7.6 / D10 re-revised — validate that the four virtualization knobs on
/// <see cref="FcShellOptions"/> ship the documented defaults, enforce their range bounds,
/// and keep the server-side threshold strictly below the unfiltered-cap (cross-property
/// invariant shipped by <c>FcShellOptionsThresholdValidator</c>; covered in the Shell test
/// project where the validator lives).
/// </summary>
public sealed class FcShellOptionsVirtualizationTests {
    [Fact]
    public void Defaults_MatchDocumentedValues() {
        FcShellOptions options = new();
        options.MaxUnfilteredItems.ShouldBe(10_000);
        options.SlowQueryThresholdMs.ShouldBe(2_000);
        options.VirtualizationServerSideThreshold.ShouldBe(500);
        options.MaxCachedPages.ShouldBe(200);
        options.PendingCommandPollingIntervalMs.ShouldBe(1_000);
        options.MaxPendingCommandPollingDurationMs.ShouldBe(120_000);
        options.CommandDispatchRetryAttempts.ShouldBe(1);
        options.CommandDispatchRetryDelayMs.ShouldBe(250);
    }

    [Theory]
    [InlineData(nameof(FcShellOptions.MaxUnfilteredItems), 99)]
    [InlineData(nameof(FcShellOptions.MaxUnfilteredItems), 1_000_001)]
    [InlineData(nameof(FcShellOptions.SlowQueryThresholdMs), 499)]
    [InlineData(nameof(FcShellOptions.SlowQueryThresholdMs), 30_001)]
    [InlineData(nameof(FcShellOptions.VirtualizationServerSideThreshold), 49)]
    [InlineData(nameof(FcShellOptions.VirtualizationServerSideThreshold), 10_001)]
    [InlineData(nameof(FcShellOptions.MaxCachedPages), 9)]
    [InlineData(nameof(FcShellOptions.MaxCachedPages), 10_001)]
    [InlineData(nameof(FcShellOptions.PendingCommandPollingIntervalMs), -1)]
    [InlineData(nameof(FcShellOptions.PendingCommandPollingIntervalMs), 300_001)]
    [InlineData(nameof(FcShellOptions.MaxPendingCommandPollingDurationMs), 999)]
    [InlineData(nameof(FcShellOptions.MaxPendingCommandPollingDurationMs), 3_600_001)]
    [InlineData(nameof(FcShellOptions.CommandDispatchRetryAttempts), -1)]
    [InlineData(nameof(FcShellOptions.CommandDispatchRetryAttempts), 4)]
    [InlineData(nameof(FcShellOptions.CommandDispatchRetryDelayMs), 0)]
    [InlineData(nameof(FcShellOptions.CommandDispatchRetryDelayMs), 60_001)]
    public void Range_Annotations_RejectOutOfBoundValues(string propertyName, int outOfRangeValue) {
        FcShellOptions options = new();
        typeof(FcShellOptions).GetProperty(propertyName)!.SetValue(options, outOfRangeValue);

        List<ValidationResult> results = Validate(options);

        results.ShouldContain(r => r.MemberNames.Contains(propertyName));
    }

    [Theory]
    [InlineData(nameof(FcShellOptions.MaxUnfilteredItems), 100)]
    [InlineData(nameof(FcShellOptions.MaxUnfilteredItems), 1_000_000)]
    [InlineData(nameof(FcShellOptions.SlowQueryThresholdMs), 500)]
    [InlineData(nameof(FcShellOptions.SlowQueryThresholdMs), 30_000)]
    [InlineData(nameof(FcShellOptions.VirtualizationServerSideThreshold), 50)]
    // server-side threshold must remain strictly below MaxUnfilteredItems (cross-prop); use a value
    // safely below the 10 000 default cap so the annotation bound is what the test exercises.
    [InlineData(nameof(FcShellOptions.VirtualizationServerSideThreshold), 9_999)]
    [InlineData(nameof(FcShellOptions.MaxCachedPages), 10)]
    [InlineData(nameof(FcShellOptions.MaxCachedPages), 10_000)]
    [InlineData(nameof(FcShellOptions.PendingCommandPollingIntervalMs), 0)]
    [InlineData(nameof(FcShellOptions.PendingCommandPollingIntervalMs), 300_000)]
    [InlineData(nameof(FcShellOptions.MaxPendingCommandPollingDurationMs), 1_000)]
    [InlineData(nameof(FcShellOptions.MaxPendingCommandPollingDurationMs), 3_600_000)]
    [InlineData(nameof(FcShellOptions.CommandDispatchRetryAttempts), 0)]
    [InlineData(nameof(FcShellOptions.CommandDispatchRetryAttempts), 3)]
    [InlineData(nameof(FcShellOptions.CommandDispatchRetryDelayMs), 1)]
    [InlineData(nameof(FcShellOptions.CommandDispatchRetryDelayMs), 60_000)]
    public void Range_Annotations_AcceptBoundaryValues(string propertyName, int inRangeValue) {
        FcShellOptions options = new();
        typeof(FcShellOptions).GetProperty(propertyName)!.SetValue(options, inRangeValue);

        List<ValidationResult> results = Validate(options);

        results.ShouldNotContain(r => r.MemberNames.Contains(propertyName));
    }

    private static List<ValidationResult> Validate(object instance) {
        ValidationContext ctx = new(instance);
        List<ValidationResult> results = [];
        _ = Validator.TryValidateObject(instance, ctx, results, validateAllProperties: true);
        return results;
    }
}
