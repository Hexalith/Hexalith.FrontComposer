using Hexalith.FrontComposer.Contracts.Schema;

namespace Hexalith.FrontComposer.Mcp.Schema;

public sealed class InMemorySchemaBaselineProvider : ISchemaBaselineProvider {
    // C5 (Group D / chunk-2 re-review): the previous implementation shipped placeholder snapshots
    // (a single `Number` field) keyed under `baseline-known-v1`. Every fingerprint-bearing request
    // from a real adopter resolved a baseline that structurally differed from the adopter's actual
    // descriptor — producing `SchemaMismatch` for every request. Until D3 ships build-time
    // baseline materialization, the default provider returns `false` from `TryResolve` for all
    // callers, letting the gate fall back to descriptor.Fingerprint byte-match behavior. Hosts
    // that need fixture-driven baselines must register their own `ISchemaBaselineProvider`
    // implementation, or the test suite registers an in-memory provider with real snapshots.
    private static readonly IReadOnlyDictionary<BaselineKey, SchemaBaselineSnapshot> EmptySnapshots
        = new Dictionary<BaselineKey, SchemaBaselineSnapshot>();

    public bool TryResolve(
        SchemaContractFamily family,
        string packageOwner,
        string fixtureId,
        out SchemaBaselineSnapshot? snapshot) {
        snapshot = null;
        if (!IsSafeIdentifier(packageOwner) || !IsSafeIdentifier(fixtureId)) {
            return false;
        }

        return EmptySnapshots.TryGetValue(new BaselineKey(family, packageOwner, fixtureId), out snapshot);
    }

    // C-2-new (chunk 2 re-review): align with the contract-side regex
    // `^[a-zA-Z0-9][a-zA-Z0-9._-]{0,127}$` (`SchemaBaselineProvenance.SafeIdentifier`). The previous
    // implementation used `char.IsLetterOrDigit` which accepts Unicode letters/digits — a non-ASCII
    // identifier would pass the provider but be rejected by the contract constructor. Matching the
    // contract regex closes the divergence and keeps fail-closed posture consistent across layers.
    private static bool IsSafeIdentifier(string value) {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 128) {
            return false;
        }

        if (!IsAsciiLetterOrDigit(value[0])) {
            return false;
        }

        foreach (char ch in value) {
            if (!IsAsciiLetterOrDigit(ch) && ch is not '.' and not '_' and not '-') {
                return false;
            }
        }

        return true;
    }

    private static bool IsAsciiLetterOrDigit(char c)
        => (c >= '0' && c <= '9')
            || (c >= 'a' && c <= 'z')
            || (c >= 'A' && c <= 'Z');

    private sealed record BaselineKey(SchemaContractFamily Family, string PackageOwner, string FixtureId);
}
