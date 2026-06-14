using Hexalith.FrontComposer.Contracts.Schema;

namespace Hexalith.FrontComposer.Schema.Diagnostics;

public static class SchemaMigrationDeltaAnalyzer {
    private const int _maxDeltaCount = 25;
    private const int _maxPathLength = 256;

    private static readonly HashSet<string> _boundsKeyPrefixes = new(StringComparer.Ordinal) {
        "bounds.",
    };

    /// <summary>
    /// Exact metadata-key allowlist. Keys whose CHANGE has a specific compatibility meaning
    /// (renderer capability, bounds, skill-corpus resource) are mapped to their dedicated delta
    /// kinds; every other change emits a generic <see cref="SchemaDeltaKind.MetadataChanged"/>
    /// delta so changes are never silently dropped (P-6, P-7).
    /// </summary>
    private static readonly HashSet<string> _rendererCapabilityKeys = new(StringComparer.Ordinal) {
        "capability",
        "outputContentType",
    };

    private static readonly HashSet<string> _skillCorpusKeys = new(StringComparer.Ordinal) {
        "publicApiReferences",
        "samplePaths",
        "resourceCount",
    };

    /// <summary>
    /// Algorithm identifiers the analyzer accepts. Mirrors the negotiator's supported set; the
    /// SourceTools build-time canonicalizer also flows through here when comparing baselines
    /// captured at build time. Per D23.
    /// </summary>
    private static readonly HashSet<string> _supportedAlgorithms = new(StringComparer.Ordinal) {
        SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1,
        SchemaFingerprintAlgorithm.Sha256SourceToolsBlobV1,
    };

    public static SchemaMigrationDeltaResult Compare(
        SchemaBaselineSnapshot baseline,
        SchemaBaselineSnapshot current,
        int maxDeltaCount = _maxDeltaCount) {
        if (baseline is null) {
            throw new ArgumentNullException(nameof(baseline));
        }

        if (current is null) {
            throw new ArgumentNullException(nameof(current));
        }

        // P-12: caller-supplied 0 or negative maxDeltaCount would suppress every delta
        // (including Breaking ones), so reject the misuse rather than silently truncating.
        if (maxDeltaCount <= 0) {
            throw new ArgumentOutOfRangeException(nameof(maxDeltaCount), maxDeltaCount, "maxDeltaCount must be positive.");
        }

        if (!_supportedAlgorithms.Contains(baseline.Provenance.FingerprintAlgorithm ?? string.Empty)
            || !_supportedAlgorithms.Contains(current.Provenance.FingerprintAlgorithm ?? string.Empty)) {
            return Result(SchemaCompatibilityDecision.UnsupportedAlgorithm, [
                Delta(SchemaDeltaKind.CanonicalizerUnsupported, SchemaCompatibilityDecision.UnsupportedAlgorithm, "$.fingerprintAlgorithm", "schema.delta.unsupported-algorithm"),
            ], false);
        }

        // P-27: distinct CanonicalizerUnsupported kind so consumers branching on delta kind get
        // actionable canonicalizer-version remediation instead of "your input was truncated".
        if (!string.Equals(baseline.Provenance.CanonicalizerVersion, current.Provenance.CanonicalizerVersion, StringComparison.Ordinal)
            || !string.Equals(baseline.Provenance.TestVectorId, current.Provenance.TestVectorId, StringComparison.Ordinal)) {
            return Result(SchemaCompatibilityDecision.Unknown, [
                Delta(SchemaDeltaKind.CanonicalizerUnsupported, SchemaCompatibilityDecision.Unknown, "$.canonicalizer", "schema.delta.unsupported-canonicalizer"),
            ], false);
        }

        if (string.Equals(baseline.Fingerprint.Value, current.Fingerprint.Value, StringComparison.Ordinal)) {
            return Result(SchemaCompatibilityDecision.Exact, [], false);
        }

        List<SchemaDelta> deltas = [];
        if (!string.Equals(baseline.Document.ProtocolIdentifier, current.Document.ProtocolIdentifier, StringComparison.Ordinal)) {
            deltas.Add(Delta(SchemaDeltaKind.ProtocolIdentifierChanged, SchemaCompatibilityDecision.Breaking, "$.ProtocolIdentifier", "schema.delta.protocol-id-changed"));
        }

        var oldFields = baseline.Document.Fields.ToDictionary(f => f.Name, StringComparer.Ordinal);
        var newFields = current.Document.Fields.ToDictionary(f => f.Name, StringComparer.Ordinal);

        foreach (string name in oldFields.Keys.OrderBy(k => k, StringComparer.Ordinal)) {
            if (!newFields.ContainsKey(name)) {
                deltas.Add(Delta(SchemaDeltaKind.RemovedField, SchemaCompatibilityDecision.Breaking, "$.Fields." + name, "schema.delta.field-removed"));
            }
        }

        foreach (string name in newFields.Keys.OrderBy(k => k, StringComparer.Ordinal)) {
            if (!oldFields.TryGetValue(name, out SchemaFieldContract? oldField)) {
                SchemaCompatibilityDecision decision = newFields[name].IsRequired
                    ? SchemaCompatibilityDecision.Breaking
                    : SchemaCompatibilityDecision.AdditiveCompatible;
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

        // P-10: compute aggregate decision from the FULL pre-truncation set so a Breaking delta
        //       past index N is not silently downgraded to CompatibleWarning.
        // P-11: keep the truncation marker WITHIN the maxDeltaCount budget so callers can size
        //       their telemetry exactly. Holds for the MissingMigrationGuide marker too — see P-43.
        // P-43 (8-6a Group B): emit the MissingMigrationGuide delta BEFORE truncation so it
        //   participates in worst-decision ordering and respects the maxDeltaCount budget rather
        //   than silently overflowing it by one entry.
        // P-44 (8-6a Group B): when fingerprints differ but the delta list is empty (the
        //   canonicalizer-bug case — same canonicalizer version, same algorithm, structurally
        //   identical documents, divergent hashes), classify as Unknown rather than Exact so the
        //   negotiator fails closed instead of granting AllowsSideEffects.
        SchemaCompatibilityDecision preliminaryAggregate = ComputeAggregate(deltas);

        // P-18: a Breaking delta against shipped public material requires a migration guide
        // declared on the baseline provenance; if absent, append a MissingMigrationGuide delta
        // so the build fails closed on un-documented breakage.
        if (preliminaryAggregate == SchemaCompatibilityDecision.Breaking && !baseline.Provenance.RequiresMigrationGuide) {
            deltas.Add(Delta(
                SchemaDeltaKind.MissingMigrationGuide,
                SchemaCompatibilityDecision.Breaking,
                "$.Provenance.RequiresMigrationGuide",
                "schema.delta.missing-migration-guide"));
        }

        bool truncated = deltas.Count > maxDeltaCount;
        SchemaCompatibilityDecision aggregate = ComputeAggregate(deltas);

        if (truncated) {
            // Group C: reserve a slot for the MissingMigrationGuide marker (when emitted) so it
            // survives truncation. The marker's `$.Provenance.*` path sorts after `$.Fields.*` and
            // `$.Metadata.*` in the Path tiebreaker, so when ≥ maxDeltaCount-1 Breaking deltas
            // exist with earlier-sorting paths, the marker would otherwise be dropped — losing
            // P-18's "no migration guide for shipped breakage" signal.
            SchemaDelta? migrationGuideMarker = deltas.FirstOrDefault(d => d.Kind == SchemaDeltaKind.MissingMigrationGuide);
            // P-48 (8-6a Chunk 1): when maxDeltaCount < 2, only the Truncated marker fits — the
            // migration-guide marker must be sacrificed so callers can still detect that
            // truncation occurred at all. Without this, maxDeltaCount=1 would emit 2 entries
            // (marker + Truncated), violating P-11's "marker stays WITHIN the budget" invariant.
            bool keepMigrationMarker = migrationGuideMarker is not null && maxDeltaCount >= 2;
            int markerSlot = keepMigrationMarker ? 1 : 0;

            // Keep the worst-decision deltas to maximise signal in the bounded window:
            // Breaking first (so the operator sees the actual blockers), then CompatibleWarning.
            List<SchemaDelta> ordered = [.. deltas
                .Where(d => d.Kind != SchemaDeltaKind.MissingMigrationGuide)
                .OrderBy(d => DecisionRank(d.Decision))
                .ThenBy(d => d.Path, StringComparer.Ordinal)];
            deltas = [.. ordered.Take(Math.Max(0, maxDeltaCount - 1 - markerSlot))];
            if (keepMigrationMarker) {
                deltas.Add(migrationGuideMarker!);
            }

            deltas.Add(Delta(
                SchemaDeltaKind.Truncated,
                aggregate, // marker reflects FULL aggregate, not the bounded subset
                "$.Deltas",
                "schema.delta.truncated"));
        }
        else {
            deltas = [.. deltas
                .OrderBy(d => DecisionRank(d.Decision))
                .ThenBy(d => d.Path, StringComparer.Ordinal)];
        }

        return Result(aggregate, deltas, truncated);
    }

    private static void AddMetadataDelta(string key, List<SchemaDelta> deltas) {
        string path = "$.Metadata." + key;

        if (_rendererCapabilityKeys.Contains(key)) {
            deltas.Add(Delta(SchemaDeltaKind.RendererCapabilityChanged, SchemaCompatibilityDecision.Breaking, path, "schema.delta.renderer-contract-changed"));
            return;
        }

        foreach (string prefix in _boundsKeyPrefixes) {
            if (key.StartsWith(prefix, StringComparison.Ordinal)) {
                deltas.Add(Delta(SchemaDeltaKind.BoundsChanged, SchemaCompatibilityDecision.CompatibleWarning, path, "schema.delta.bounds-changed"));
                return;
            }
        }

        if (_skillCorpusKeys.Contains(key)) {
            deltas.Add(Delta(SchemaDeltaKind.SkillCorpusResourceChanged, SchemaCompatibilityDecision.CompatibleWarning, path, "schema.delta.skill-corpus-changed"));
            return;
        }

        // P-6: catch-all so authorization-policy / description / title / etc. changes are
        // surfaced rather than silently dropped. CompatibleWarning by default; if the key
        // semantically demands Breaking, map it explicitly above.
        deltas.Add(Delta(SchemaDeltaKind.MetadataChanged, SchemaCompatibilityDecision.CompatibleWarning, path, "schema.delta.metadata-changed"));
    }

    private static SchemaCompatibilityDecision ComputeAggregate(List<SchemaDelta> deltas)
            => deltas.Count == 0
            ? SchemaCompatibilityDecision.Unknown
            : deltas.Any(d => d.Decision == SchemaCompatibilityDecision.Breaking)
                ? SchemaCompatibilityDecision.Breaking
                : deltas.All(d => d.Decision == SchemaCompatibilityDecision.AdditiveCompatible)
                    ? SchemaCompatibilityDecision.AdditiveCompatible
                    : SchemaCompatibilityDecision.CompatibleWarning;

    private static int DecisionRank(SchemaCompatibilityDecision decision)
        => decision switch {
            SchemaCompatibilityDecision.Breaking => 0,
            SchemaCompatibilityDecision.UnsupportedAlgorithm => 1,
            SchemaCompatibilityDecision.Unknown => 2,
            SchemaCompatibilityDecision.CompatibleWarning => 3,
            SchemaCompatibilityDecision.AdditiveCompatible => 4,
            SchemaCompatibilityDecision.Exact => 5,
            _ => 6,
        };

    /// <summary>
    /// P-9: bound the structural path length so a hostile or extremely deep field name cannot
    /// balloon downstream telemetry/log payloads. Truncation is deterministic and marked.
    /// </summary>
    private static SchemaDelta Delta(SchemaDeltaKind kind, SchemaCompatibilityDecision decision, string path, string messageKey)
        => new(kind, decision, TruncatePath(path), messageKey);

    private static bool DictionaryEqual(IReadOnlyDictionary<string, string>? left, IReadOnlyDictionary<string, string>? right)
        => (left ?? new Dictionary<string, string>(StringComparer.Ordinal))
            .OrderBy(p => p.Key, StringComparer.Ordinal)
            .SequenceEqual(
                (right ?? new Dictionary<string, string>(StringComparer.Ordinal)).OrderBy(p => p.Key, StringComparer.Ordinal),
                EqualityComparer<KeyValuePair<string, string>>.Default);

    private static SchemaMigrationDeltaResult Result(SchemaCompatibilityDecision decision, IReadOnlyList<SchemaDelta> deltas, bool truncated)
        => new(
            decision,
            deltas,
            truncated,
            "HFC-SCHEMA-DELTA",
            "https://docs.hexalith.dev/frontcomposer/schema-migration");

    private static bool SequenceEqual(IReadOnlyList<string>? left, IReadOnlyList<string>? right)
                    => (left ?? []).OrderBy(v => v, StringComparer.Ordinal).SequenceEqual((right ?? []).OrderBy(v => v, StringComparer.Ordinal), StringComparer.Ordinal);

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0057:Use range operator", Justification = "Not supported in this context")]
    private static string TruncatePath(string path) {
        if (path.Length <= _maxPathLength) {
            return path;
        }

        // P-45 (8-6a Group B): UTF-16 Substring at code-unit MaxPathLength can split a surrogate
        // pair and leave an unpaired high surrogate at the truncation boundary. Downstream JSON
        // and structured-log encoders would then emit U+FFFD or non-deterministic escape
        // sequences. Step back one code unit when the cut would land between paired surrogates.
        int cut = _maxPathLength;
        if (char.IsHighSurrogate(path[cut - 1])) {
            cut--;
        }

        return path.Substring(0, cut) + "...";
    }
}
