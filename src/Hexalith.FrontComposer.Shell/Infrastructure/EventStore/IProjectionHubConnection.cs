using Hexalith.FrontComposer.Contracts.Communication;

namespace Hexalith.FrontComposer.Shell.Infrastructure.EventStore;

internal interface IProjectionHubConnection : IAsyncDisposable {
    bool IsConnected { get; }

    IDisposable OnProjectionChanged(Func<string, string, Task> handler);

    IDisposable OnProjectionChangedDetail(Func<ProjectionChangedDetail, Task> handler);

    IDisposable OnConnectionStateChanged(Func<ProjectionHubConnectionStateChanged, Task> handler);

    Task StartAsync(CancellationToken cancellationToken);

    Task JoinGroupAsync(string projectionType, string tenantId, CancellationToken cancellationToken);

    /// <summary>
    /// Joins a group, optionally scoped below tenant level. A null/empty <paramref name="scope"/>
    /// joins the tenant-wide group (wire method <c>JoinGroup</c>); a non-empty scope joins the
    /// scoped group (wire method <c>JoinGroupScoped</c>).
    /// </summary>
    Task JoinGroupAsync(string projectionType, string tenantId, string? scope, CancellationToken cancellationToken);

    Task LeaveGroupAsync(string projectionType, string tenantId, CancellationToken cancellationToken);

    /// <summary>
    /// Leaves a group, optionally scoped below tenant level. A null/empty <paramref name="scope"/>
    /// leaves the tenant-wide group (wire method <c>LeaveGroup</c>); a non-empty scope leaves the
    /// scoped group (wire method <c>LeaveGroupScoped</c>).
    /// </summary>
    Task LeaveGroupAsync(string projectionType, string tenantId, string? scope, CancellationToken cancellationToken);

    Task StopAsync(CancellationToken cancellationToken);
}
