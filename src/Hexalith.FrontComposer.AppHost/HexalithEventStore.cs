namespace Hexalith.FrontComposer.AppHost;

/// <summary>EventStore command gateway service (root-declared references submodule copy).</summary>
public class HexalithEventStore : IProjectMetadata {

    public string ProjectPath => ProjectMetadataPaths.GetProjectPath(
        "Hexalith.EventStore",
        "src",
        "Hexalith.EventStore",
        "Hexalith.EventStore.csproj");

    public bool SuppressBuild => true;
}
