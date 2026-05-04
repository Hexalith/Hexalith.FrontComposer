namespace Hexalith.FrontComposer.Contracts.Schema;

public sealed record SchemaBaselineProvenance(
    SchemaContractFamily ContractFamily,
    string ContractSchemaVersion,
    string FingerprintAlgorithm,
    string PackageOwner,
    string FixtureId,
    bool RequiresMigrationGuide,
    string CanonicalizerVersion = SchemaFingerprintAlgorithm.CanonicalizerVersionV1,
    string TestVectorId = SchemaFingerprintAlgorithm.TestVectorIdV1);

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
