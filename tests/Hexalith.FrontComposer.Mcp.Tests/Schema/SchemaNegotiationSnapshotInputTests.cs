using System.Reflection;

using Hexalith.FrontComposer.Contracts.Schema;
using Hexalith.FrontComposer.Mcp.Schema;

using Shouldly;

namespace Hexalith.FrontComposer.Mcp.Tests.Schema;

/// <summary>
/// AC6 / T2 — replace <c>HasCompatibleAdditiveDrift</c> bool with snapshot inputs and let the
/// negotiator derive Exact / CompatibleAdditive / Incompatible from
/// <see cref="Hexalith.FrontComposer.Schema.Diagnostics.SchemaMigrationDeltaAnalyzer"/>. Memory rule
/// "optional security parameters are an anti-pattern" applies: caller-supplied "additive" bools
/// short-circuit the gate, so the input contract must surface snapshots only.
/// </summary>
public sealed class SchemaNegotiationSnapshotInputTests {
    [Fact]
    public void Input_ExposesBaselineAndServerSnapshotProperties() {
        // RED: when T2 lands, McpSchemaNegotiationInput must expose
        //   BaselineSnapshot? Baseline { get; init; }
        //   ServerSnapshot?   Server   { get; init; }
        // (or equivalently typed SchemaBaselineSnapshot members) so the negotiator can derive
        // additive vs breaking via SchemaMigrationDeltaAnalyzer rather than trust a caller bool.
        Type input = typeof(McpSchemaNegotiationInput);

        PropertyInfo? baseline = input.GetProperty("Baseline", BindingFlags.Public | BindingFlags.Instance);
        PropertyInfo? server = input.GetProperty("Server", BindingFlags.Public | BindingFlags.Instance);

        _ = baseline.ShouldNotBeNull("AC6 requires a typed Baseline snapshot input.");
        _ = server.ShouldNotBeNull("AC6 requires a typed Server snapshot input.");
        baseline!.PropertyType.ShouldBe(typeof(SchemaBaselineSnapshot));
        server!.PropertyType.ShouldBe(typeof(SchemaBaselineSnapshot));
    }

    [Fact]
    public void Input_LegacyBooleanIsObsoleteOrRemoved() {
        // RED: HasCompatibleAdditiveDrift must be removed or marked [Obsolete] for a release so
        // existing callers fail loudly instead of silently bypassing the analyzer.
        PropertyInfo? legacy = typeof(McpSchemaNegotiationInput)
            .GetProperty("HasCompatibleAdditiveDrift", BindingFlags.Public | BindingFlags.Instance);

        if (legacy is null) {
            return; // removed outright — acceptable.
        }

        _ = legacy.GetCustomAttribute<ObsoleteAttribute>().ShouldNotBeNull(
            "AC6 requires HasCompatibleAdditiveDrift be [Obsolete] (or removed). The negotiator must derive additive vs breaking internally.");
    }

    [Fact]
    public void Negotiate_DerivesCompatibleAdditive_FromAnalyzerNotFromCallerBool() {
        // Two snapshots with identical canonicalizer metadata but a single newly-added optional
        // field on `current`. The analyzer should classify this as AdditiveCompatible, and the
        // negotiator's Kind must reflect that — without any caller hint.
        SchemaBaselineSnapshot baselineSnapshot = MakeSnapshot([
            new SchemaFieldContract("Number", "String", "string", true, false),
        ]);
        SchemaBaselineSnapshot currentSnapshot = MakeSnapshot([
            new SchemaFieldContract("Number", "String", "string", true, false),
            new SchemaFieldContract("OptionalNote", "String", "string", false, true),
        ]);

        // RED: requires the new constructor surface.
        McpSchemaNegotiationInput input = NewSnapshotInput(baselineSnapshot, currentSnapshot);

        McpSchemaNegotiationResult result = McpSchemaNegotiator.Negotiate(input);

        result.Kind.ShouldBe(McpSchemaNegotiationResultKind.CompatibleAdditive);
        result.AllowsSideEffects.ShouldBeTrue("AC5 requires CompatibleAdditive to admit dispatch (after revalidation).");
        result.AgentCategory.ShouldBe("schema-compatible-warning");
    }

    [Fact]
    public void Negotiate_TrustingCallerBool_DoesNotOverrideAnalyzerDecision() {
        // Even if a (legacy) caller passes HasCompatibleAdditiveDrift = true, the analyzer's
        // verdict must win. Snapshots disagree on a required field type — Breaking — and the
        // negotiator must classify Incompatible regardless of any leftover bool.
        SchemaBaselineSnapshot baselineSnapshot = MakeSnapshot([
            new SchemaFieldContract("Number", "String", "string", true, false),
        ]);
        SchemaBaselineSnapshot currentSnapshot = MakeSnapshot([
            new SchemaFieldContract("Number", "Int32", "number", true, false),
        ]);

        McpSchemaNegotiationInput input = NewSnapshotInput(baselineSnapshot, currentSnapshot, legacyAdditiveBool: true);

        McpSchemaNegotiationResult result = McpSchemaNegotiator.Negotiate(input);

        result.Kind.ShouldBe(McpSchemaNegotiationResultKind.Incompatible);
        result.AllowsSideEffects.ShouldBeFalse();
    }

    private static SchemaBaselineSnapshot MakeSnapshot(IReadOnlyList<SchemaFieldContract> fields) {
        var document = new SchemaContractDocument(
            "frontcomposer.schema.contract.v1",
            SchemaContractFamily.ProjectionResource,
            "frontcomposer://Sales/projections/Snapshot",
            "frontcomposer.projection-resource.v1",
            "Sales",
            "Sales.SnapshotProjection",
            "frontcomposer://Sales/projections/Snapshot",
            fields,
            [new SchemaCollectionContract("fields", SchemaCollectionOrder.NonStructuralSorted, "name")],
            new Dictionary<string, string>());
        SchemaCanonicalPayload payload = CanonicalSchemaMaterial.CreatePayload(document);
        return new SchemaBaselineSnapshot(
            new SchemaBaselineProvenance(
                SchemaContractFamily.ProjectionResource,
                "frontcomposer.projection-resource.v1",
                SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1,
                "Hexalith.FrontComposer",
                "story-8-6a-snapshot",
                requiresMigrationGuide: false),
            payload.Document,
            payload.Fingerprint);
    }

    /// <summary>
    /// Bridges current and post-T2 input shapes via reflection so this scaffold compiles while
    /// the snapshot constructor is still pending. When unskipped before T2 lands, the test fails
    /// with a precise "snapshot constructor missing" message rather than a compile error.
    /// </summary>
    private static McpSchemaNegotiationInput NewSnapshotInput(
        SchemaBaselineSnapshot baseline,
        SchemaBaselineSnapshot current,
        bool legacyAdditiveBool = false) {
        ConstructorInfo? snapshotCtor = typeof(McpSchemaNegotiationInput)
            .GetConstructors()
            .FirstOrDefault(c => c.GetParameters().Any(p => p.ParameterType == typeof(SchemaBaselineSnapshot))) ?? throw new InvalidOperationException(
                "AC6 requires McpSchemaNegotiationInput to expose a constructor accepting SchemaBaselineSnapshot inputs. "
                + "Implement T2 before unskipping this test.");
        ParameterInfo[] parameters = snapshotCtor.GetParameters();
        object?[] args = new object?[parameters.Length];
        for (int i = 0; i < parameters.Length; i++) {
            ParameterInfo p = parameters[i];
            args[i] = (p.Name?.ToLowerInvariant(), p.ParameterType) switch {
                ("baseline" or "baselinesnapshot", _) => baseline,
                ("server" or "serversnapshot", _) => current,
                ("clientfingerprint", _) => current.Fingerprint,
                ("serverfingerprint", _) => current.Fingerprint,
                ("hastrustedbaseline", _) => true,
                (_, Type t) when t == typeof(bool) && p.Name?.Contains("additive", StringComparison.OrdinalIgnoreCase) == true => legacyAdditiveBool,
                (_, Type t) when t == typeof(bool) => false,
                _ => p.HasDefaultValue ? p.DefaultValue : null,
            };
        }

        return (McpSchemaNegotiationInput)snapshotCtor.Invoke(args)!;
    }
}
