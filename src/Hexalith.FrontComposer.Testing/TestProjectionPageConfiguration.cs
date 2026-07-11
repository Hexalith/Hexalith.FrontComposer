using Hexalith.FrontComposer.Shell.State.DataGridNavigation;

namespace Hexalith.FrontComposer.Testing;

internal sealed record TestProjectionPageConfiguration(
    ProjectionPageResult? Result,
    Func<ProjectionPageRequest, ProjectionPageResult>? Callback) {
    public static TestProjectionPageConfiguration FromResult(ProjectionPageResult result) => new(result, null);

    public static TestProjectionPageConfiguration FromCallback(Func<ProjectionPageRequest, ProjectionPageResult> callback) => new(null, callback);
}
