namespace Hexalith.FrontComposer.AppHost;

/// <summary>Tenants domain service (Hexalith.Tenants submodule).</summary>
public class HexalithTenants : IProjectMetadata {

    public string ProjectPath => ProjectMetadataPaths.GetProjectPath(
        "Hexalith.Tenants",
        "src",
        "Hexalith.Tenants",
        "Hexalith.Tenants.csproj");

    public bool SuppressBuild => true;
}
