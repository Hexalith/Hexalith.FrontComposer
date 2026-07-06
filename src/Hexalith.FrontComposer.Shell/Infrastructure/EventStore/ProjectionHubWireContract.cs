namespace Hexalith.FrontComposer.Shell.Infrastructure.EventStore;

internal static class ProjectionHubWireContract
{
    public const string ProjectionChanged = nameof(ProjectionChanged);

    public const string ProjectionChangedDetail = nameof(ProjectionChangedDetail);

    public const string JoinGroup = nameof(JoinGroup);

    public const string JoinGroupScoped = nameof(JoinGroupScoped);

    public const string LeaveGroup = nameof(LeaveGroup);

    public const string LeaveGroupScoped = nameof(LeaveGroupScoped);
}
