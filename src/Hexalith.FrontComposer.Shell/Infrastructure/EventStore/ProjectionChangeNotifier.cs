using Hexalith.FrontComposer.Contracts.Communication;

namespace Hexalith.FrontComposer.Shell.Infrastructure.EventStore;

internal sealed class ProjectionChangeNotifier : IProjectionChangeNotifierWithTenant, IProjectionChangeDetailNotifier {
    public event Action<string>? ProjectionChanged;
    public event Action<string, string>? ProjectionChangedForTenant;
    public event Func<ProjectionChangedDetail, Task>? ProjectionChangedDetail;

    public void NotifyChanged(string projectionType) => ProjectionChanged?.Invoke(projectionType);

    public void NotifyChanged(string projectionType, string tenantId) {
        ProjectionChanged?.Invoke(projectionType);
        ProjectionChangedForTenant?.Invoke(projectionType, tenantId);
    }

    public async Task NotifyDetailAsync(ProjectionChangedDetail detail, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(detail);

        Func<ProjectionChangedDetail, Task>? handlers = ProjectionChangedDetail;
        if (handlers is null) {
            return;
        }

        // Await every subscriber (a multicast Func only surfaces the last task otherwise) so a
        // detail subscriber can run its ordering/staleness gate before the caller continues.
        foreach (Delegate handler in handlers.GetInvocationList()) {
            await ((Func<ProjectionChangedDetail, Task>)handler)(detail).ConfigureAwait(false);
        }
    }
}
