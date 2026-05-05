using Hexalith.FrontComposer.Contracts.Schema;

namespace Hexalith.FrontComposer.Mcp.Schema;

public interface ISkillCorpusFingerprintProvider {
    IReadOnlyList<SchemaFingerprint> GetFingerprints();
}
