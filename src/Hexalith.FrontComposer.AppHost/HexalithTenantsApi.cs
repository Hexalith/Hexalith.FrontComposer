namespace Hexalith.FrontComposer.AppHost;

/// <summary>Generated Tenants REST API host from the selected source module.</summary>
public class HexalithTenantsApi : IProjectMetadata
{

    /// <summary>Gets the selected source module's REST API project path.</summary>
    public string ProjectPath => ProjectMetadataPaths.GetProjectPath(
        "Hexalith.Tenants",
        "src",
        "Hexalith.Tenants.Api",
        "Hexalith.Tenants.Api.csproj");

    /// <summary>Gets whether Aspire skips its runtime build after the owning source prebuild.</summary>
    public bool SuppressBuild => true;
}
