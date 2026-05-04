using System.Security.Cryptography;
using System.Text;

namespace Hexalith.FrontComposer.SourceTools.Transforms;

public sealed class GeneratedSchemaFingerprint {
    public GeneratedSchemaFingerprint(string algorithmId, string value, string canonicalizerVersion, string testVectorId) {
        AlgorithmId = algorithmId;
        Value = value;
        CanonicalizerVersion = canonicalizerVersion;
        TestVectorId = testVectorId;
    }

    public string AlgorithmId { get; }

    public string Value { get; }

    public string CanonicalizerVersion { get; }

    public string TestVectorId { get; }
}

public sealed class GeneratedSchemaPayload {
    public GeneratedSchemaPayload(string json, GeneratedSchemaFingerprint fingerprint) {
        Json = json;
        Fingerprint = fingerprint;
    }

    public string Json { get; }

    public GeneratedSchemaFingerprint Fingerprint { get; }
}

public static class SchemaFingerprintTransform {
    public const string AlgorithmId = "frontcomposer.schema.sha256.canonical-json.v1";
    public const string CanonicalizerVersion = "frontcomposer.canonical-json.v1";
    public const string TestVectorId = "hfc-schema-v1";
    public const string CommandSchemaVersion = "frontcomposer.command-tool.v1";
    public const string ProjectionResourceSchemaVersion = "frontcomposer.projection-resource.v1";
    public const string LifecycleResultSchemaVersion = "frontcomposer.lifecycle-result.v1";
    public const string MarkdownRendererSchemaVersion = "frontcomposer.renderer.markdown.v1";
    public const string SkillCorpusSchemaVersion = "frontcomposer.skill-corpus.v1";
    public const string AggregateManifestSchemaVersion = "frontcomposer.mcp-manifest.aggregate.v1";

    public static GeneratedSchemaPayload CreateCommandPayload(McpCommandDescriptorModel command)
        => Payload(
            "CommandTool",
            command.ProtocolName,
            CommandSchemaVersion,
            command.BoundedContext,
            command.CommandTypeName,
            command.ProtocolName,
            command.Parameters.Select(FieldLine),
            new Dictionary<string, string>(StringComparer.Ordinal) {
                ["authorizationPolicy"] = command.AuthorizationPolicyName ?? "",
                ["description"] = command.Description ?? "",
                ["derivablePropertyCount"] = command.DerivablePropertyNames.Count.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["title"] = command.Title,
            });

    public static GeneratedSchemaPayload CreateResourcePayload(McpResourceDescriptorModel resource)
        => Payload(
            "ProjectionResource",
            resource.ProtocolUri,
            ProjectionResourceSchemaVersion,
            resource.BoundedContext,
            resource.ProjectionTypeName,
            resource.ProtocolUri,
            resource.Fields.Select(FieldLine),
            new Dictionary<string, string>(StringComparer.Ordinal) {
                ["description"] = resource.Description ?? "",
                ["emptyStateCtaCommandName"] = resource.EmptyStateCtaCommandName ?? "",
                ["entityPluralLabel"] = resource.EntityPluralLabel ?? "",
                ["name"] = resource.Name,
                ["renderStrategy"] = resource.RenderStrategy,
                ["title"] = resource.Title,
            });

    public static GeneratedSchemaPayload CreateLifecycleResultPayload()
        => Payload(
            "LifecycleResult",
            "frontcomposer.lifecycle.result",
            LifecycleResultSchemaVersion,
            "",
            "Hexalith.FrontComposer.Mcp.Invocation.McpLifecycleResult",
            "frontcomposer://lifecycle/result",
            [
                "category|string|string|required|not-null",
                "correlationId|string|string|required|not-null",
                "messageId|string|string|required|not-null",
                "state|string|string|required|not-null|Accepted,Confirmed,Failed,Rejected,Running",
            ],
            new Dictionary<string, string>(StringComparer.Ordinal) {
                ["compatibilityPolicy"] = "no-client-timestamps",
            });

    public static GeneratedSchemaPayload CreateMarkdownRendererPayload(string rendererId, string renderStrategy, int maxCharacters, int maxFieldCharacters)
        => Payload(
            "MarkdownRendererContract",
            rendererId,
            MarkdownRendererSchemaVersion,
            "",
            "",
            rendererId,
            [],
            new Dictionary<string, string>(StringComparer.Ordinal) {
                ["bounds.maxCharacters"] = maxCharacters.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["bounds.maxFieldCharacters"] = maxFieldCharacters.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["capability"] = "bounded-markdown|sanitized-inert-text",
                ["outputContentType"] = "text/markdown",
                ["renderStrategy"] = renderStrategy,
            });

    public static GeneratedSchemaPayload CreateSkillCorpusResourcePayload(
        string id,
        string version,
        string resourceUri,
        string title,
        string contentType,
        int order,
        IReadOnlyList<string> publicApiReferences,
        IReadOnlyList<string> samplePaths)
        => Payload(
            "SkillCorpusResource",
            id,
            SkillCorpusSchemaVersion,
            "",
            "",
            resourceUri,
            [],
            new Dictionary<string, string>(StringComparer.Ordinal) {
                ["contentType"] = contentType,
                ["order"] = order.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["publicApiReferences"] = string.Join("|", publicApiReferences.OrderBy(v => v, StringComparer.Ordinal)),
                ["samplePaths"] = string.Join("|", samplePaths.OrderBy(v => v, StringComparer.Ordinal)),
                ["title"] = title,
                ["version"] = version,
            });

    public static GeneratedSchemaPayload CreateAggregateManifestPayload(
        IReadOnlyList<GeneratedSchemaFingerprint> commandFingerprints,
        IReadOnlyList<GeneratedSchemaFingerprint> resourceFingerprints,
        GeneratedSchemaFingerprint? lifecycleFingerprint,
        IReadOnlyList<GeneratedSchemaFingerprint> rendererFingerprints,
        IReadOnlyList<GeneratedSchemaFingerprint> skillCorpusFingerprints)
        => Payload(
            "AggregateMcpManifest",
            "frontcomposer.mcp.manifest",
            AggregateManifestSchemaVersion,
            "",
            "",
            "frontcomposer://mcp/manifest",
            [],
            new Dictionary<string, string>(StringComparer.Ordinal) {
                ["commandFingerprints"] = JoinFingerprints(commandFingerprints),
                ["lifecycleFingerprint"] = lifecycleFingerprint?.Value ?? "",
                ["resourceFingerprints"] = JoinFingerprints(resourceFingerprints),
                ["rendererFingerprints"] = JoinFingerprints(rendererFingerprints),
                ["skillCorpusFingerprints"] = JoinFingerprints(skillCorpusFingerprints),
            });

    private static GeneratedSchemaPayload Payload(
        string family,
        string contractId,
        string schemaVersion,
        string boundedContext,
        string fullyQualifiedName,
        string protocolIdentifier,
        IEnumerable<string> fields,
        IReadOnlyDictionary<string, string> metadata) {
        var sb = new StringBuilder(1024);
        sb.Append("root=frontcomposer.schema.contract.v1\n");
        sb.Append("family=").Append(Normalize(family)).Append('\n');
        sb.Append("contractId=").Append(Normalize(contractId)).Append('\n');
        sb.Append("schemaVersion=").Append(Normalize(schemaVersion)).Append('\n');
        sb.Append("boundedContext=").Append(Normalize(boundedContext)).Append('\n');
        sb.Append("fqn=").Append(Normalize(fullyQualifiedName)).Append('\n');
        sb.Append("protocol=").Append(Normalize(protocolIdentifier)).Append('\n');
        sb.Append("collections=fields:non-structural-sorted:name\n");
        foreach (string field in fields.OrderBy(f => f, StringComparer.Ordinal)) {
            sb.Append("field=").Append(Normalize(field)).Append('\n');
        }

        foreach (KeyValuePair<string, string> pair in metadata.OrderBy(p => p.Key, StringComparer.Ordinal)) {
            sb.Append("metadata.").Append(Normalize(pair.Key)).Append('=').Append(Normalize(pair.Value)).Append('\n');
        }

        string json = sb.ToString();
        return new GeneratedSchemaPayload(
            json,
            new GeneratedSchemaFingerprint(AlgorithmId, Sha256Hex(json), CanonicalizerVersion, TestVectorId));
    }

    private static string FieldLine(McpParameterDescriptorModel parameter)
        => string.Join("|", [
            parameter.Name,
            parameter.TypeName,
            parameter.JsonType,
            parameter.IsRequired ? "required" : "optional",
            parameter.IsNullable ? "nullable" : "not-null",
            parameter.Title,
            parameter.Description ?? "",
            string.Join(",", parameter.EnumValues.OrderBy(v => v, StringComparer.Ordinal)),
            parameter.IsUnsupported ? "unsupported" : "supported",
            string.Join(",", parameter.BadgeMappings.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => p.Key + ":" + p.Value)),
            parameter.DisplayFormat,
        ]);

    private static string Normalize(string value)
        => (value ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n').Trim();

    private static string JoinFingerprints(IEnumerable<GeneratedSchemaFingerprint> fingerprints)
        => string.Join("|", fingerprints.Select(f => f.AlgorithmId + ":" + f.Value).OrderBy(v => v, StringComparer.Ordinal));

    private static string Sha256Hex(string value) {
        using SHA256 sha = SHA256.Create();
        byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value));
        char[] chars = new char[bytes.Length * 2];
        const string Hex = "0123456789abcdef";
        for (int i = 0; i < bytes.Length; i++) {
            chars[i * 2] = Hex[bytes[i] >> 4];
            chars[(i * 2) + 1] = Hex[bytes[i] & 0xF];
        }

        return new string(chars);
    }
}
