using System.Text.RegularExpressions;

namespace Hexalith.FrontComposer.Contracts.Schema;

public sealed record SchemaBaselineProvenance {
    /// <summary>
    /// Pattern accepted for <see cref="PackageOwner"/> and <see cref="FixtureId"/>. Restricts to
    /// dotted/dashed/underscored alphanumerics so that if a future loader uses these values to
    /// look up baselines on disk, attacker-supplied values cannot smuggle path-traversal segments
    /// (`..`, `/`, `\`, NUL, etc.) into the resolution. Bounded to 128 chars to prevent abuse.
    /// </summary>
    private static readonly Regex SafeIdentifier = new("^[a-zA-Z0-9][a-zA-Z0-9._-]{0,127}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public SchemaBaselineProvenance(
        SchemaContractFamily contractFamily,
        string contractSchemaVersion,
        string fingerprintAlgorithm,
        string packageOwner,
        string fixtureId,
        bool requiresMigrationGuide,
        string canonicalizerVersion = SchemaFingerprintAlgorithm.CanonicalizerVersionV1,
        string testVectorId = SchemaFingerprintAlgorithm.TestVectorIdV1) {
        // Inline null/empty validation rather than `ArgumentException.ThrowIfNullOrEmpty` so the
        // record stays compatible with the netstandard2.0 target framework.
        ThrowIfNullOrEmpty(contractSchemaVersion, nameof(contractSchemaVersion));
        ThrowIfNullOrEmpty(fingerprintAlgorithm, nameof(fingerprintAlgorithm));
        ThrowIfNullOrEmpty(packageOwner, nameof(packageOwner));
        ThrowIfNullOrEmpty(fixtureId, nameof(fixtureId));
        ThrowIfNullOrEmpty(canonicalizerVersion, nameof(canonicalizerVersion));
        ThrowIfNullOrEmpty(testVectorId, nameof(testVectorId));

        if (!SafeIdentifier.IsMatch(packageOwner)) {
            throw new ArgumentException(
                "Package owner must match the safe-identifier pattern (alphanumerics with dot/dash/underscore, 1-128 chars).",
                nameof(packageOwner));
        }

        if (!SafeIdentifier.IsMatch(fixtureId)) {
            throw new ArgumentException(
                "Fixture id must match the safe-identifier pattern (alphanumerics with dot/dash/underscore, 1-128 chars). Path-traversal segments and untrusted lookup keys are rejected before resolution.",
                nameof(fixtureId));
        }

        ContractFamily = contractFamily;
        ContractSchemaVersion = contractSchemaVersion;
        FingerprintAlgorithm = fingerprintAlgorithm;
        PackageOwner = packageOwner;
        FixtureId = fixtureId;
        RequiresMigrationGuide = requiresMigrationGuide;
        CanonicalizerVersion = canonicalizerVersion;
        TestVectorId = testVectorId;
    }

    public SchemaContractFamily ContractFamily { get; init; }

    public string ContractSchemaVersion { get; init; }

    public string FingerprintAlgorithm { get; init; }

    public string PackageOwner { get; init; }

    public string FixtureId { get; init; }

    public bool RequiresMigrationGuide { get; init; }

    public string CanonicalizerVersion { get; init; }

    public string TestVectorId { get; init; }

    private static void ThrowIfNullOrEmpty(string value, string paramName) {
        if (string.IsNullOrEmpty(value)) {
            throw new ArgumentException("Value cannot be null or empty.", paramName);
        }
    }
}

public sealed record SchemaBaselineSnapshot(
    SchemaBaselineProvenance Provenance,
    SchemaContractDocument Document,
    SchemaFingerprint Fingerprint);

public enum SchemaCompatibilityDecision {
    Exact,
    AdditiveCompatible,
    CompatibleWarning,
    Breaking,
    Unknown,
    UnsupportedAlgorithm,
}

public enum SchemaDeltaKind {
    AddedOptionalField,
    AddedRequiredField,
    RemovedField,
    TypeChanged,
    EnumChanged,
    ValidationConstraintChanged,
    ProtocolIdentifierChanged,
    RendererCapabilityChanged,
    BoundsChanged,
    SkillCorpusResourceChanged,
    StructuralOrderChanged,
    Truncated,
    /// <summary>P-6: catch-all for metadata key/value changes that do not match a more specific category.</summary>
    MetadataChanged,
    /// <summary>P-27: distinct from <see cref="Truncated"/>; used when baselines were canonicalized by an unsupported canonicalizer/test-vector pair.</summary>
    CanonicalizerUnsupported,
    /// <summary>P-18: emitted when a Breaking delta would require a migration guide that the baseline provenance does not declare.</summary>
    MissingMigrationGuide,
}

public sealed record SchemaDelta(
    SchemaDeltaKind Kind,
    SchemaCompatibilityDecision Decision,
    string Path,
    string MessageKey,
    IReadOnlyDictionary<string, string>? Parameters = null);

public sealed record SchemaMigrationDeltaResult(
    SchemaCompatibilityDecision Decision,
    IReadOnlyList<SchemaDelta> Deltas,
    bool IsTruncated,
    string DiagnosticId,
    string DocsLink);
