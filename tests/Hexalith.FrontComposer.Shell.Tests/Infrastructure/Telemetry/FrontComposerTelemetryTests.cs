using System.Diagnostics;

using Hexalith.FrontComposer.Contracts.Telemetry;
using Hexalith.FrontComposer.Shell.Infrastructure.Telemetry;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Shell.Tests.Infrastructure.Telemetry;

[Trait("Category", "Governance")]
public sealed class FrontComposerTelemetryTests {
    [Fact]
    public void ActivitySource_UsesContractNameAndVersion() {
        FrontComposerTelemetry.Source.Name.ShouldBe(FrontComposerActivitySource.Name);
        FrontComposerTelemetry.Source.Version.ShouldBe(FrontComposerActivitySource.Version);
    }

    [Fact]
    public void StartActivity_NoListenerPath_DoesNotThrow() {
        using Activity? activity = FrontComposerTelemetry.StartProjectionFallbackPoll();

        // The xUnit assembly runs tests in parallel, so another test may have an ActivityListener
        // active. This assertion is intentionally only the fail-open contract: no listener is
        // required by the helper and no exception is thrown.
    }

    [Fact]
    public void CommandDispatchActivity_UsesApprovedNameAndSanitizedTags() {
        using ActivityCapture capture = ActivityCapture.Start();

        using (Activity? activity = FrontComposerTelemetry.StartCommandDispatch(
            "Orders.ShipOrderCommand",
            "01HX-CORR_123",
            FrontComposerTelemetry.TenantMarker("tenant-secret"))) {
            FrontComposerTelemetry.SetCorrelation(activity, "corr-1");
            FrontComposerTelemetry.SetOutcome(activity, "accepted");
        }

        Activity recorded = capture.Single(FrontComposerTelemetry.CommandDispatchOperation);
        recorded.Source.Name.ShouldBe(FrontComposerActivitySource.Name);
        recorded.GetTagItem(FrontComposerTelemetry.CommandTypeTag).ShouldBe("Orders.ShipOrderCommand");
        recorded.GetTagItem(FrontComposerTelemetry.TenantMarkerTag).ShouldBe("present");
        recorded.GetTagItem(FrontComposerTelemetry.CorrelationIdTag).ShouldBe("corr-1");
        recorded.Tags.Select(static tag => tag.Value).ShouldNotContain("tenant-secret");
    }

    [Fact]
    public void NestedActivities_ParentUnderCurrentActivity() {
        using ActivityCapture capture = ActivityCapture.Start();
        using ActivitySource parentSource = new("test-parent");
        using Activity? parent = parentSource.StartActivity("parent");
        const string correlationId = "corr-parent-test-unique";

        using (Activity? child = FrontComposerTelemetry.StartLifecycleTransition(
            "Confirmed",
            correlationId,
            "msg-1",
            idempotencyResolved: true)) {
            child.ShouldNotBeNull();
        }

        Activity recorded = capture.Single(
            FrontComposerTelemetry.LifecycleTransitionOperation,
            activity => string.Equals(
                activity.GetTagItem(FrontComposerTelemetry.CorrelationIdTag) as string,
                correlationId,
                StringComparison.Ordinal));
        recorded.ParentSpanId.ShouldBe(parent!.SpanId);
    }

    [Fact]
    public void FailureCategory_IsBoundedAndSanitized() {
        FrontComposerTelemetry.BoundCategory("bad value\r\nwith spaces and a very very very very very long suffix")
            .ShouldBe("bad_value__with_spaces_and_a_very_very_very_very_very_long_suffi");
    }

    private sealed class ActivityCapture : IDisposable {
        private readonly ActivityListener _listener;
        private readonly List<Activity> _activities = [];

        private ActivityCapture() {
            _listener = new ActivityListener {
                ShouldListenTo = static _ => true,
                Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                ActivityStopped = activity => _activities.Add(activity),
            };
            ActivitySource.AddActivityListener(_listener);
        }

        public static ActivityCapture Start() => new();

        public Activity Single(string operationName)
            => Single(operationName, static _ => true);

        public Activity Single(string operationName, Func<Activity, bool> predicate)
            => _activities.Single(activity => activity.OperationName == operationName && predicate(activity));

        public void Dispose() => _listener.Dispose();
    }
}
