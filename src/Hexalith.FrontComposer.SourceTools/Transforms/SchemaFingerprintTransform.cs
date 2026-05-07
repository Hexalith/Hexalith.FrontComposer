using System.Security.Cryptography;
using System.Text;

using Hexalith.FrontComposer.Contracts.Schema;

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
    // P-15 / DN-2: SourceTools-emitted fingerprints declare a distinct algorithm id from the
    // Contracts canonical-JSON form because the build-time canonicalizer here produces a
    // newline-delimited key=value text blob, not JSON. Both algorithms are supported by the
    // runtime negotiator since the runtime trusts emitter-supplied fingerprints (it never
    // recomputes them). Unifying the canonicalizers is deferred to Story 8-6a (D23).
    public const string AlgorithmId = SchemaFingerprintAlgorithm.Sha256SourceToolsBlobV1;
    public const string CanonicalizerVersion = SchemaFingerprintAlgorithm.CanonicalizerVersionV1;
    public const string TestVectorId = SchemaFingerprintAlgorithm.TestVectorIdV1;
    public const string CommandSchemaVersion = "frontcomposer.command-tool.v1";
    public const string ProjectionResourceSchemaVersion = "frontcomposer.projection-resource.v1";
    public const string LifecycleResultSchemaVersion = "frontcomposer.lifecycle-result.v1";
    public const string MarkdownRendererSchemaVersion = "frontcomposer.renderer.markdown.v1";
    public const string SkillCorpusSchemaVersion = "frontcomposer.skill-corpus.v1";
    public const string AggregateManifestSchemaVersion = "frontcomposer.mcp-manifest.aggregate.v1";

    /// <summary>
    /// Sentinel emitted into canonical fingerprint material in place of a null/absent optional
    /// scalar so a logical "value not provided" hashes differently from an explicit empty string.
    /// Mirrors <c>CanonicalSchemaMaterial.AbsentValueSentinel</c> on the runtime side.
    /// </summary>
    public const string AbsentValueSentinel = "<absent>";

    /// <summary>
    /// Defense-in-depth: parameter names that must never appear in a structural fingerprint
    /// because they are runtime correlation/identity values (per AC4 / D4). The upstream
    /// <c>McpManifestTransform</c> already excludes these by parameter selection, but this
    /// filter makes the invariant structurally enforced at the canonicalization edge.
    /// </summary>
    private static readonly HashSet<string> RuntimeCorrelationFieldNames = new(StringComparer.OrdinalIgnoreCase) {
        "MessageId",
        "TenantId",
        "CorrelationId",
        "UserId",
        "Principal",
        "Claims",
        "Token",
    };

    public static GeneratedSchemaPayload CreateCommandPayload(McpCommandDescriptorModel command)
        => Payload(
            "CommandTool",
            command.ProtocolName,
            CommandSchemaVersion,
            command.BoundedContext,
            command.CommandTypeName,
            command.ProtocolName,
            command.Parameters
                .Where(p => !RuntimeCorrelationFieldNames.Contains(p.Name))
                .Select(FieldLine),
            new Dictionary<string, string>(StringComparer.Ordinal) {
                ["authorizationPolicy"] = OptionalScalar(command.AuthorizationPolicyName),
                ["description"] = OptionalScalar(command.Description),
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
            resource.Fields
                .Where(p => !RuntimeCorrelationFieldNames.Contains(p.Name))
                .Select(FieldLine),
            new Dictionary<string, string>(StringComparer.Ordinal) {
                ["description"] = OptionalScalar(resource.Description),
                ["emptyStateCtaCommandName"] = OptionalScalar(resource.EmptyStateCtaCommandName),
                ["entityPluralLabel"] = OptionalScalar(resource.EntityPluralLabel),
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
            CreateLifecycleFieldLines(),
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

    /// <summary>
    /// 8-6a review: replaces the prior <c>AppDomain.CurrentDomain.GetAssemblies()</c> scan with a
    /// deterministic catalog. The scan was non-deterministic across build environments — Roslyn
    /// analyzer host vs IDE vs CI process loads different assembly sets, and at source-generator
    /// time (the actual fingerprint emission path) the consumer's <c>Hexalith.FrontComposer.Mcp</c>
    /// assembly is being COMPILED, not loaded, so the scan always failed and silently fell back
    /// to a separate camelCase constant. That broke AC11 fingerprint determinism between build
    /// time and test time when property casing differed from the fallback. The catalog mirrors
    /// <c>Hexalith.FrontComposer.Mcp.Invocation.McpLifecycleResult</c>'s public surface; field
    /// names are pinned by <c>SchemaFingerprintCrossPackageTests.LifecycleCatalog_FieldNames_…</c>
    /// against the runtime record, and the State enum-values cell is pinned by
    /// <c>LifecycleCatalog_StateEnumValues_MatchMcpLifecycleStateNames</c> against
    /// <c>Hexalith.FrontComposer.Contracts.Lifecycle.McpLifecycleStateNames.Canonical</c>
    /// (8-6a chunk-3 decision: Contracts owns the canonical wire-state set; SourceTools repeats
    /// the literal here because Contracts non-const references are not loadable by the Roslyn
    /// analyzer host that runs the source generator on adopter projects). Adding/renaming/
    /// removing a lifecycle property or wire-state name requires updating BOTH this literal AND
    /// the Contracts constant; the cross-check test surfaces drift as a build failure.
    /// </summary>
    private static IReadOnlyList<string> CreateLifecycleFieldLines()
        => [
            "Category|string|string|required|not-null",
            "CorrelationId|string|string|required|not-null",
            "MessageId|string|string|required|not-null",
            "State|string|string|required|not-null|Accepted,Confirmed,Failed,Rejected,Running",
        ];

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
        // P-1: every user-controlled scalar that crosses the canonical-blob delimiters
        // (`=`, `|`, `\n`) is escaped so an attacker- or user-controlled string (XML doc,
        // attribute Description, etc.) cannot synthesize a fake `metadata.X=...\n` line and
        // collide with a different schema. Backslash escapes are reversible: `\\` → `\`,
        // `\n` → newline, `\=` → equals, `\|` → pipe, `\:` → colon. Field lines pre-escape
        // their cells inside FieldLine; static identifiers (family/protocol/family-keys) are
        // author-controlled and do not require escaping.
        var sb = new StringBuilder(1024);
        sb.Append("root=frontcomposer.schema.contract.v1\n");
        sb.Append("family=").Append(EscapeDelimited(Normalize(family))).Append('\n');
        sb.Append("contractId=").Append(EscapeDelimited(Normalize(contractId))).Append('\n');
        sb.Append("schemaVersion=").Append(EscapeDelimited(Normalize(schemaVersion))).Append('\n');
        sb.Append("boundedContext=").Append(EscapeDelimited(Normalize(boundedContext))).Append('\n');
        sb.Append("fqn=").Append(EscapeDelimited(Normalize(fullyQualifiedName))).Append('\n');
        sb.Append("protocol=").Append(EscapeDelimited(Normalize(protocolIdentifier))).Append('\n');
        sb.Append("collections=fields:non-structural-sorted:name\n");
        foreach (string field in fields.OrderBy(f => f, StringComparer.Ordinal)) {
            // Field lines are pre-escaped per cell inside FieldLine (commands/resources); for
            // hand-authored static field strings (lifecycle/markdown/skill-corpus), the
            // surface here is author-controlled, so newline normalization is sufficient.
            sb.Append("field=").Append(Normalize(field)).Append('\n');
        }

        foreach (KeyValuePair<string, string> pair in metadata.OrderBy(p => p.Key, StringComparer.Ordinal)) {
            sb.Append("metadata.")
                .Append(EscapeDelimited(Normalize(pair.Key)))
                .Append('=')
                .Append(EscapeDelimited(Normalize(pair.Value)))
                .Append('\n');
        }

        string json = sb.ToString();
        return new GeneratedSchemaPayload(
            json,
            new GeneratedSchemaFingerprint(AlgorithmId, Sha256Hex(json), CanonicalizerVersion, TestVectorId));
    }

    private static string FieldLine(McpParameterDescriptorModel parameter)
        // Pipe-delimited record. EscapeDelimited per cell prevents a `|` inside a parameter
        // name/description from synthesizing additional positional fields and colliding with
        // an unrelated parameter.
        => string.Join("|", [
            EscapeDelimited(parameter.Name),
            EscapeDelimited(parameter.TypeName),
            EscapeDelimited(parameter.JsonType),
            parameter.IsRequired ? "required" : "optional",
            parameter.IsNullable ? "nullable" : "not-null",
            EscapeDelimited(parameter.Title),
            EscapeDelimited(OptionalScalar(parameter.Description)),
            string.Join(",", parameter.EnumValues.OrderBy(v => v, StringComparer.Ordinal).Select(EscapeDelimited)),
            parameter.IsUnsupported ? "unsupported" : "supported",
            string.Join(",", parameter.BadgeMappings.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => EscapeDelimited(p.Key) + ":" + EscapeDelimited(p.Value))),
            EscapeDelimited(parameter.DisplayFormat),
        ]);

    private static string Normalize(string value)
        // 8-6a chunk-3 review: AC11 EOL-invariance applies to LF, CRLF, CR, U+2028 LINE SEPARATOR,
        // and U+2029 PARAGRAPH SEPARATOR. Other Unicode line-break code points (U+0085 NEL, U+000B
        // VT, U+000C FF) are not normalized \u2014 sources do not produce them in practice (Roslyn
        // syntax trees + attribute string literals + XML doc comments), and AC11 determinism tests
        // do not parameterize over them. EscapeDelimited (`'\n'`/`'\u2028'`/`'\u2029'` -> `\\n`)
        // collapses any surviving in-cell line breaks to a literal escape so they cannot synthesize
        // a row split inside the canonical blob even when Normalize runs after cell escaping.
        => (value ?? string.Empty)
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Replace('\u2028', '\n')
            .Replace('\u2029', '\n')
            .Trim();

    /// <summary>
    /// Returns <see cref="AbsentValueSentinel"/> for null/empty/whitespace input so the
    /// fingerprint distinguishes "value not provided" from explicit empty string. P-4.
    /// </summary>
    private static string OptionalScalar(string? value)
        => string.IsNullOrWhiteSpace(value) ? AbsentValueSentinel : value!;

    /// <summary>
    /// Escapes the canonical-blob delimiters so the canonical text cannot be ambiguously re-parsed
    /// and so attacker-controlled metadata values cannot be crafted to collide. P-1.
    /// 8-6a chunk-3 review: U+2028 / U+2029 are escaped to the same `\\n` escape as `\n` because
    /// `field=` rows go through `Normalize(field)` AFTER per-cell escaping (see Payload `:241`).
    /// Without this, U+2028 in a parameter description survived FieldLine then was collapsed to a
    /// bare `\n` by Normalize, terminating the row mid-cell and synthesizing a fake `field=…` line
    /// inside the canonical blob. Mapping all line-break flavors to the same escape also enforces
    /// AC11 EOL-invariance: the same description authored with any line terminator hashes alike.
    /// </summary>
    private static string EscapeDelimited(string value) {
        if (string.IsNullOrEmpty(value)) {
            return string.Empty;
        }

        // Collapse CRLF to LF before per-char escape so the switch handles all line-break flavors
        // uniformly: a `\r\n` pair would otherwise emit `\\n\\n` (two escapes) where `\n` alone
        // emits `\\n` (one), breaking AC11 EOL-invariance for FieldLine cells.
        if (value.IndexOf('\r') >= 0) {
            value = value.Replace("\r\n", "\n");
        }

        var sb = new StringBuilder(value.Length);
        foreach (char c in value) {
            switch (c) {
                case '\\': sb.Append("\\\\"); break;
                case '=': sb.Append("\\="); break;
                case '|': sb.Append("\\|"); break;
                case ':': sb.Append("\\:"); break;
                case '\n':
                case '\r':
                case '\u2028':
                case '\u2029': sb.Append("\\n"); break;
                default: sb.Append(c); break;
            }
        }

        return sb.ToString();
    }

    private static string JoinFingerprints(IEnumerable<GeneratedSchemaFingerprint> fingerprints)
        => string.Join("|", fingerprints.Select(f => EscapeDelimited(f.AlgorithmId) + ":" + EscapeDelimited(f.Value)).OrderBy(v => v, StringComparer.Ordinal));

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
