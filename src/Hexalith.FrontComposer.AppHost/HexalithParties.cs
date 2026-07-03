namespace Hexalith.FrontComposer.AppHost;

/// <summary>Parties domain service (Hexalith.Parties submodule).</summary>
public class HexalithParties : IProjectMetadata
{
    public string ProjectPath => ProjectMetadataPaths.GetProjectPath(
        "Hexalith.Parties",
        "src",
        "Hexalith.Parties",
        "Hexalith.Parties.csproj");

    public bool SuppressBuild => true;
}
