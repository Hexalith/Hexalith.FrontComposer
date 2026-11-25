using System.Diagnostics;

using Hexalith.FrontComposer.Contracts.Telemetry;

namespace Hexalith.FrontComposer.Shell.Tests.Infrastructure.Telemetry;

internal sealed class ActivityCapture : IDisposable {
    private readonly ActivityListener _listener;
    private readonly List<Activity> _activities = [];
    private readonly object _sync = new();

    private ActivityCapture() {
        _listener = new ActivityListener {
            ShouldListenTo = source => source.Name == FrontComposerActivitySource.Name,
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

    public Activity Single(string operationName, Func<Activity, bool> predicate) {
        Activity[] snapshot;
        lock (_sync) {
            snapshot = [.. _activities];
        }

        return snapshot.Single(activity => activity.OperationName == operationName && predicate(activity));
    }

    public IEnumerable<Activity> AllOf(string operationName) {
        Activity[] snapshot;
        lock (_sync) {
            snapshot = [.. _activities];
        }

        return snapshot.Where(activity => activity.OperationName == operationName);
    }

    public void Dispose() => _listener.Dispose();
}
