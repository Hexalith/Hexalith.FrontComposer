namespace Hexalith.FrontComposer.Shell.State.ProjectionConnection;

/// <summary>Scoped read/subscribe API for projection connection state.</summary>
public interface IProjectionConnectionState {
    ProjectionConnectionSnapshot Current { get; }

    IDisposable Subscribe(Action<ProjectionConnectionSnapshot> handler, bool replay = true);

    void Apply(ProjectionConnectionTransition transition);
}
