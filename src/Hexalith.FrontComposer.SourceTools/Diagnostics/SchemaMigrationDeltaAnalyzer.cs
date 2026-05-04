using Hexalith.FrontComposer.Contracts.Schema;

namespace Hexalith.FrontComposer.SourceTools.Diagnostics;

public static class SchemaMigrationDeltaAnalyzer {
    private const int MaxDeltaCount = 25;

    public static SchemaMigrationDeltaResult Compare(
        SchemaBaselineSnapshot baseline,
        SchemaBaselineSnapshot current,
        int maxDeltaCount = MaxDeltaCount) {
        if (baseline is null) {
            throw new ArgumentNullException(nameof(baseline));
        }

        if (current is null) {
            throw new ArgumentNullException(nameof(current));
        }

        if (!string.Equals(baseline.Provenance.FingerprintAlgorithm, SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1, StringComparison.Ordinal)
            || !string.Equals(current.Provenance.FingerprintAlgorithm, SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1, StringComparison.Ordinal)) {
            return Result(SchemaCompatibilityDecision.UnsupportedAlgorithm, [
                Delta(SchemaDeltaKind.Truncated, SchemaCompatibilityDecision.UnsupportedAlgorithm, "$.fingerprintAlgorithm", "schema.delta.unsupported-algorithm"),
            ], false);
        }

        if (!string.Equals(baseline.Provenance.CanonicalizerVersion, current.Provenance.CanonicalizerVersion, StringComparison.Ordinal)
            || !string.Equals(baseline.Provenance.TestVectorId, current.Provenance.TestVectorId, StringComparison.Ordinal)) {
            return Result(SchemaCompatibilityDecision.Unknown, [
                Delta(SchemaDeltaKind.Truncated, SchemaCompatibilityDecision.Unknown, "$.canonicalizer", "schema.delta.unsupported-canonicalizer"),
            ], false);
        }

        if (string.Equals(baseline.Fingerprint.Value, current.Fingerprint.Value, StringComparison.Ordinal)) {
            return Result(SchemaCompatibilityDecision.Exact, [], false);
        }

        List<SchemaDelta> deltas = [];
        if (!string.Equals(baseline.Document.ProtocolIdentifier, current.Document.ProtocolIdentifier, StringComparison.Ordinal)) {
            deltas.Add(Delta(SchemaDeltaKind.ProtocolIdentifierChanged, SchemaCompatibilityDecision.Breaking, "$.ProtocolIdentifier", "schema.delta.protocol-id-changed"));
        }

        Dictionary<string, SchemaFieldContract> oldFields = baseline.Document.Fields.ToDictionary(f => f.Name, StringComparer.Ordinal);
        Dictionary<string, SchemaFieldContract> newFields = current.Document.Fields.ToDictionary(f => f.Name, StringComparer.Ordinal);

        foreach (string name in oldFields.Keys.OrderBy(k => k, StringComparer.Ordinal)) {
            if (!newFields.ContainsKey(name)) {
                deltas.Add(Delta(SchemaDeltaKind.RemovedField, SchemaCompatibilityDecision.Breaking, "$.Fields." + name, "schema.delta.field-removed"));
            }
        }

        foreach (string name in newFields.Keys.OrderBy(k => k, StringComparer.Ordinal)) {
            if (!oldFields.TryGetValue(name, out SchemaFieldContract? oldField)) {
                SchemaCompatibilityDecision decision = newFields[name].IsRequired
                    ? SchemaCompatibilityDecision.Breaking
                    : SchemaCompatibilityDecision.CompatibleWarning;
                deltas.Add(Delta(
                    newFields[name].IsRequired ? SchemaDeltaKind.AddedRequiredField : SchemaDeltaKind.AddedOptionalField,
                    decision,
                    "$.Fields." + name,
                    newFields[name].IsRequired ? "schema.delta.required-field-added" : "schema.delta.optional-field-added"));
                continue;
            }

            SchemaFieldContract newField = newFields[name];
            if (!string.Equals(oldField.JsonType, newField.JsonType, StringComparison.Ordinal)
                || !string.Equals(oldField.TypeName, newField.TypeName, StringComparison.Ordinal)) {
                deltas.Add(Delta(SchemaDeltaKind.TypeChanged, SchemaCompatibilityDecision.Breaking, "$.Fields." + name + ".Type", "schema.delta.type-changed"));
            }

            if (!SequenceEqual(oldField.EnumValues, newField.EnumValues)) {
                deltas.Add(Delta(SchemaDeltaKind.EnumChanged, SchemaCompatibilityDecision.CompatibleWarning, "$.Fields." + name + ".EnumValues", "schema.delta.enum-changed"));
            }

            if (!DictionaryEqual(oldField.ValidationConstraints, newField.ValidationConstraints)) {
                deltas.Add(Delta(SchemaDeltaKind.ValidationConstraintChanged, SchemaCompatibilityDecision.CompatibleWarning, "$.Fields." + name + ".ValidationConstraints", "schema.delta.validation-changed"));
            }
        }

        foreach (KeyValuePair<string, string> oldMeta in baseline.Document.Metadata.OrderBy(p => p.Key, StringComparer.Ordinal)) {
            if (!current.Document.Metadata.TryGetValue(oldMeta.Key, out string? newValue)
                || !string.Equals(oldMeta.Value, newValue, StringComparison.Ordinal)) {
                AddMetadataDelta(oldMeta.Key, deltas);
            }
        }

        foreach (KeyValuePair<string, string> newMeta in current.Document.Metadata.OrderBy(p => p.Key, StringComparer.Ordinal)) {
            if (!baseline.Document.Metadata.ContainsKey(newMeta.Key)) {
                AddMetadataDelta(newMeta.Key, deltas);
            }
        }

        bool truncated = deltas.Count > maxDeltaCount;
        if (truncated) {
            deltas = [.. deltas.Take(maxDeltaCount)];
            deltas.Add(Delta(SchemaDeltaKind.Truncated, SchemaCompatibilityDecision.CompatibleWarning, "$.Deltas", "schema.delta.truncated"));
        }

        SchemaCompatibilityDecision aggregate = deltas.Any(d => d.Decision == SchemaCompatibilityDecision.Breaking)
            ? SchemaCompatibilityDecision.Breaking
            : SchemaCompatibilityDecision.CompatibleWarning;
        return Result(aggregate, deltas, truncated);
    }

    private static void AddMetadataDelta(string key, List<SchemaDelta> deltas) {
        if (key.Contains("capability", StringComparison.OrdinalIgnoreCase)
            || key.Contains("outputContentType", StringComparison.OrdinalIgnoreCase)) {
            deltas.Add(Delta(SchemaDeltaKind.RendererCapabilityChanged, SchemaCompatibilityDecision.Breaking, "$.Metadata." + key, "schema.delta.renderer-contract-changed"));
            return;
        }

        if (key.Contains("bounds", StringComparison.OrdinalIgnoreCase)) {
            deltas.Add(Delta(SchemaDeltaKind.BoundsChanged, SchemaCompatibilityDecision.CompatibleWarning, "$.Metadata." + key, "schema.delta.bounds-changed"));
            return;
        }

        if (key.Contains("resource", StringComparison.OrdinalIgnoreCase) || key.Contains("sample", StringComparison.OrdinalIgnoreCase)) {
            deltas.Add(Delta(SchemaDeltaKind.SkillCorpusResourceChanged, SchemaCompatibilityDecision.CompatibleWarning, "$.Metadata." + key, "schema.delta.skill-corpus-changed"));
        }
    }

    private static bool SequenceEqual(IReadOnlyList<string>? left, IReadOnlyList<string>? right)
        => (left ?? []).OrderBy(v => v, StringComparer.Ordinal).SequenceEqual((right ?? []).OrderBy(v => v, StringComparer.Ordinal), StringComparer.Ordinal);

    private static bool DictionaryEqual(IReadOnlyDictionary<string, string>? left, IReadOnlyDictionary<string, string>? right)
        => (left ?? new Dictionary<string, string>(StringComparer.Ordinal))
            .OrderBy(p => p.Key, StringComparer.Ordinal)
            .SequenceEqual(
                (right ?? new Dictionary<string, string>(StringComparer.Ordinal)).OrderBy(p => p.Key, StringComparer.Ordinal),
                EqualityComparer<KeyValuePair<string, string>>.Default);

    private static SchemaDelta Delta(SchemaDeltaKind kind, SchemaCompatibilityDecision decision, string path, string messageKey)
        => new(kind, decision, path, messageKey);

    private static SchemaMigrationDeltaResult Result(SchemaCompatibilityDecision decision, IReadOnlyList<SchemaDelta> deltas, bool truncated)
        => new(
            decision,
            deltas,
            truncated,
            "HFC-SCHEMA-DELTA",
            "https://docs.hexalith.dev/frontcomposer/schema-migration");
}
