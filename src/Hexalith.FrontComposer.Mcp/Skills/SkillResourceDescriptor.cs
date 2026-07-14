using Hexalith.FrontComposer.Contracts.Schema;

namespace Hexalith.FrontComposer.Mcp.Skills;

public sealed record SkillResourceDescriptor(
    string Id,
    string Title,
    string Description,
    string ResourceUri,
    string ContentType,
    int Order,
    SchemaFingerprint? Fingerprint = null);
