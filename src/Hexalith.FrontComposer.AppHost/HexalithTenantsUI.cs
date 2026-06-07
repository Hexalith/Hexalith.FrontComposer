using Projects;

namespace Hexalith.FrontComposer.AppHost;

/// <summary>
/// Tenants Blazor UI (Hexalith.Tenants submodule). This is the runnable front end that consumes
/// the FrontComposer Shell + Contracts components, so it is the primary user-facing service of
/// this AppHost.
/// </summary>
public class HexalithTenantsUI : IProjectMetadata {

    public string ProjectPath => ProjectMetadataPaths.GetProjectPath(
        "Hexalith.Tenants",
        "src",
        "Hexalith.Tenants.UI",
        "Hexalith.Tenants.UI.csproj");

    public bool SuppressBuild => true;
}
