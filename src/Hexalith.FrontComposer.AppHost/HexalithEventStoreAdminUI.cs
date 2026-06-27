namespace Hexalith.FrontComposer.AppHost;

/// <summary>EventStore Admin Blazor UI (root-declared references submodule copy).</summary>
public class HexalithEventStoreAdminUI : IProjectMetadata {

    public string ProjectPath => ProjectMetadataPaths.GetProjectPath(
        "Hexalith.EventStore",
        "src",
        "Hexalith.EventStore.Admin.UI",
        "Hexalith.EventStore.Admin.UI.csproj");

    public bool SuppressBuild => true;
}
