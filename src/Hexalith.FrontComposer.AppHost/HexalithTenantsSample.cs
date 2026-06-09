using Projects;

namespace Hexalith.FrontComposer.AppHost;

/// <summary>
/// Tenants sample domain service (Hexalith.Tenants submodule). Registered as the EventStore
/// domain app id <c>sample</c> so the EventStore's configured sample/counter/greeting domain
/// registrations resolve at startup (the admin operational-index poll would otherwise fail).
/// </summary>
public class HexalithTenantsSample : IProjectMetadata {

    public string ProjectPath => ProjectMetadataPaths.GetProjectPath(
        "Hexalith.Tenants",
        "samples",
        "Hexalith.Tenants.Sample",
        "Hexalith.Tenants.Sample.csproj");

    public bool SuppressBuild => true;
}
