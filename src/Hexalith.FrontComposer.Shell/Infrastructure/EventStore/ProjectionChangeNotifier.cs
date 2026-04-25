using Hexalith.FrontComposer.Contracts.Communication;

namespace Hexalith.FrontComposer.Shell.Infrastructure.EventStore;

internal sealed class ProjectionChangeNotifier : IProjectionChangeNotifierWithTenant {
    public event Action<string>? ProjectionChanged;
    public event Action<string, string>? ProjectionChangedForTenant;

    public void NotifyChanged(string projectionType) => ProjectionChanged?.Invoke(projectionType);

    public void NotifyChanged(string projectionType, string tenantId) {
        ProjectionChanged?.Invoke(projectionType);
        ProjectionChangedForTenant?.Invoke(projectionType, tenantId);
    }
}
