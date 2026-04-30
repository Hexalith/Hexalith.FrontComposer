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
        // F37 — exercise the no-listener path with a deterministic ActivitySource that has no
        // listener attached. We instrument a short-lived helper source whose StartActivity will
        // return null, and assert downstream tag setters tolerate the null without throwing.
        using ActivitySource isolatedSource = new("Hexalith.FrontComposer.Tests.NoListener");
        Activity? noListener = isolatedSource.StartActivity("noop");
        noListener.ShouldBeNull();

        // All public helpers must accept a null activity without throwing.
        Should.NotThrow(() => FrontComposerTelemetry.SetOutcome(noListener, "ok"));
        Should.NotThrow(() => FrontComposerTelemetry.SetFailure(noListener, "category"));
        Should.NotThrow(() => FrontComposerTelemetry.SetCorrelation(noListener, "corr-1"));
        Should.NotThrow(() => FrontComposerTelemetry.SetHttpStatus(noListener, 200));
        Should.NotThrow(() => FrontComposerTelemetry.SetElapsed(noListener, TimeSpan.FromMilliseconds(5)));
    }

    [Fact]
    public void TagDerivation_FailureModes_AreFailOpenAndSideEffectFree() {
        // F36 / D14 — Telemetry helpers must not throw when inputs are degenerate. Derivation
        // failures (null, whitespace, unrepresentable) must produce a no-op or bounded marker
        // without affecting caller behaviour.
        using ActivityCapture capture = ActivityCapture.Start();

        using (Activity? activity = FrontComposerTelemetry.StartCommandDispatch(
            string.Empty,
            string.Empty,
            FrontComposerTelemetry.TenantMarker(null))) {
            FrontComposerTelemetry.SetCorrelation(activity, null);
            FrontComposerTelemetry.SetCorrelation(activity, "   ");
            FrontComposerTelemetry.SetFailure(activity, null);
            FrontComposerTelemetry.SetOutcome(activity, null);
        }

        // The activity exists (listener attached) but tag setters with degenerate inputs must
        // not crash. We can't assert tag absence reliably (other tests may set them) but the
        // absence of an exception is the contract.
        FrontComposerTelemetry.SafeIdentifierOrAbsent(null).ShouldBe("absent");
        FrontComposerTelemetry.SafeIdentifierOrAbsent(string.Empty).ShouldBe("absent");
        FrontComposerTelemetry.SafeIdentifierOrAbsent("   ").ShouldBe("absent");
        FrontComposerTelemetry.SafeIdentifierOrAbsent("01HXAB-CORR_1.").ShouldBe("01HXAB-CORR_1.");
    }

    [Fact]
    public void CommandDispatchActivity_UsesApprovedNameAndSanitizedTags() {
        using ActivityCapture capture = ActivityCapture.Start();
        const string messageId = "01HX-CORR_123-telemetry-test";

        using (Activity? activity = FrontComposerTelemetry.StartCommandDispatch(
            "Orders.ShipOrderCommand",
            messageId,
            FrontComposerTelemetry.TenantMarker("tenant-secret"))) {
            FrontComposerTelemetry.SetCorrelation(activity, "corr-1");
            FrontComposerTelemetry.SetOutcome(activity, "accepted");
        }

        Activity recorded = capture.Single(
            FrontComposerTelemetry.CommandDispatchOperation,
            activity => string.Equals(
                activity.GetTagItem(FrontComposerTelemetry.MessageIdTag) as string,
                messageId,
                StringComparison.Ordinal));
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
        private readonly object _sync = new();

        private ActivityCapture() {
            _listener = new ActivityListener {
                ShouldListenTo = static _ => true,
                Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                ActivityStopped = activity => {
                    lock (_sync) {
                        _activities.Add(activity);
                    }
                },
            };
            ActivitySource.AddActivityListener(_listener);
        }

        public static ActivityCapture Start() => new();

        public Activity Single(string operationName)
            => Single(operationName, static _ => true);

        public Activity Single(string operationName, Func<Activity, bool> predicate) {
            Activity[] snapshot;
            lock (_sync) {
                snapshot = [.. _activities];
            }

            return snapshot.Single(activity => activity.OperationName == operationName && predicate(activity));
        }

        public void Dispose() => _listener.Dispose();
    }
}
