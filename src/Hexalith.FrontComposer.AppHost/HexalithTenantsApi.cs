namespace Hexalith.FrontComposer.AppHost;

/// <summary>Generated Tenants REST API host from the selected source module.</summary>
public class HexalithTenantsApi : IProjectMetadata {

    public string ProjectPath => ProjectMetadataPaths.GetProjectPath(
        "Hexalith.Tenants",
        "src",
        "Hexalith.Tenants.Api",
        "Hexalith.Tenants.Api.csproj");

    public bool SuppressBuild => true;
}
