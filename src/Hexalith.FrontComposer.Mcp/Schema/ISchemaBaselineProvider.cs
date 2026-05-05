using Hexalith.FrontComposer.Contracts.Schema;

namespace Hexalith.FrontComposer.Mcp.Schema;

public interface ISchemaBaselineProvider {
    bool TryResolve(
        SchemaContractFamily family,
        string packageOwner,
        string fixtureId,
        out SchemaBaselineSnapshot? snapshot);
}
