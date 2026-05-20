using System.Text.Json;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Shell.Tests.Governance;

// ----------------------------------------------------------------------------
// Story 12.4 — Red-phase ATDD scaffolds for genuinely-deferred work.
//
// These tests intentionally FAIL today against the current release-evidence
// implementation. They are the executable specification for the open
// CR-12-4-DefN items called out in
// _bmad-output/implementation-artifacts/12-4-trusted-release-evidence-dry-run.md.
//
// They are quarantined so they do not block the main lane. When the targeted
// Def item is implemented, the corresponding test should go green and the
// Quarantined trait + frontcomposer-quarantine metadata comment must be
// removed in the same change.
//
// IMPORTANT - class placement: These tests live in a SEPARATE class without
// the Governance Category class-level trait carried by `CiGovernanceTests`.
// The Gate 2b CI lane runs the Category=Governance filter and ignores the
// per-method Quarantined trait (xUnit traits are additive), so a
// quarantined-but-Governance-class test would run in Gate 2b and break the
// blocking lane. The existing
// `CiGovernanceTests.BlockingTestLanes_ExcludeQuarantinedTestsWithoutSkippingGovernance`
// contract forbids excluding quarantined tests from the governance lane by
// design - so the right place for quarantined release-evidence red-phase
// tests is outside the Governance class. These tests therefore live here,
// with only the Quarantined Category trait on each method, and reach into
// `CiGovernanceTests` for shared `internal static` helpers.
// ----------------------------------------------------------------------------
public sealed class Story12_4_RedPhaseDefTests {
    // frontcomposer-quarantine: issue=12-4-trusted-release-evidence-dry-run.md#CR-12-4-Def14 owner=release-owner reason=attest-build-provenance-step-not-wired reintroduction=5-nightly-passes
    [Fact]
    [Trait("Category", "Quarantined")]
    public void Story12_4_Def14_AttestBuildProvenanceStep_IsWiredInReleaseWorkflow() {
        // RED until Def14 lands: AC9 ("Attestations are generated and verified before
        // release readiness is claimed") requires an actual generation step. The story
        // currently reaches AC9 only via AC10's `fallback-approved` path, leaving AC9
        // structurally unreachable for production. This test pins the contract that
        // closing Def14 requires wiring `actions/attest-build-provenance@v2` before
        // `Run semantic-release (live publish)` and binding the attestation bundle into
        // the sealed manifest.
        //
        // F4 (Story 12.4 test review): structural step-body checks via FindStepBlockContaining
        // replace raw `workflow.Contains(...)` + `IndexOf(...)`. The previous substring approach
        // matched on comments and doc anchors, ignored `if: false`/`continue-on-error: true`
        // skip flags, and used positional indices that could be confused by unrelated mentions
        // of the same text earlier in the file.
        string root = CiGovernanceTests.RepositoryRoot();
        string workflow = File.ReadAllText(Path.Combine(root, ".github/workflows/release.yml"));

        string attestStep = CiGovernanceTests.FindStepBlockContaining(workflow, "actions/attest-build-provenance@v2");
        attestStep.ShouldNotBeNullOrEmpty(
            "AC9/Def14: release.yml must include a workflow STEP whose `uses:` (or `run:` body) invokes actions/attest-build-provenance@v2. A reference inside a comment does not count.");

        attestStep.Contains("if: false", StringComparison.Ordinal).ShouldBeFalse(
            "AC9/Def14: the attest-build-provenance step must not be conditionally disabled (`if: false`).");
        attestStep.Contains("continue-on-error: true", StringComparison.Ordinal).ShouldBeFalse(
            "AC9/Def14: the attest-build-provenance step must not be marked advisory (`continue-on-error: true`).");

        // Ordering: attestation must run before the live-publish semantic-release step so the
        // bundle binds into the sealed manifest before any irreversible side effect.
        string livePublishStep = CiGovernanceTests.ExtractNamedStep(workflow, "Run semantic-release (live publish)");
        int attestStepIdx = workflow.IndexOf(attestStep, StringComparison.Ordinal);
        int livePublishIdx = workflow.IndexOf(livePublishStep, StringComparison.Ordinal);
        attestStepIdx.ShouldBeLessThan(
            livePublishIdx,
            "AC9/Def14: attest-build-provenance must run before semantic-release (live publish) so the attestation bundle is bound into the sealed manifest.");
    }

    // frontcomposer-quarantine: issue=12-4-trusted-release-evidence-dry-run.md#CR-12-4-Def14 owner=release-owner reason=attestations-write-permission-narrowed reintroduction=5-nightly-passes
    [Fact]
    [Trait("Category", "Quarantined")]
    public void Story12_4_Def14_AttestationsWritePermission_IsRestored() {
        // RED until Def14 lands: round-8 CR-12-4-P189 deliberately narrowed the job
        // permission scope to `attestations: read` because no attestation creation
        // step exists. The attested path becomes reachable only when Def14 restores
        // `attestations: write` AND wires the build-provenance step. Pin both halves
        // so a partial Def14 fix (permission flipped without the step, or vice versa)
        // does not silently slip past review.
        //
        // F4 (Story 12.4 test review): scope the permission assertions to the release job's
        // `permissions:` block via ExtractJobPermissionsBlock. The previous substring approach
        // on the full workflow text could not distinguish workflow-level vs job-level
        // permission scope, allowing a partial green if `attestations: write` appeared at
        // workflow scope while the release job still carried `attestations: read`.
        string root = CiGovernanceTests.RepositoryRoot();
        string workflow = File.ReadAllText(Path.Combine(root, ".github/workflows/release.yml"));

        string jobPermissions = CiGovernanceTests.ExtractJobPermissionsBlock(workflow, "release");
        jobPermissions.ShouldNotBeNullOrEmpty(
            "AC9/Def14: the release job must declare a `permissions:` block — none found.");

        jobPermissions.Contains("attestations: write", StringComparison.Ordinal).ShouldBeTrue(
            "AC9/Def14: the release job permissions must include `attestations: write` so attest-build-provenance can sign and upload (round-8 CR-12-4-P189 narrowed to `read`; Def14 must restore `write`).");
        jobPermissions.Contains("id-token: write", StringComparison.Ordinal).ShouldBeTrue(
            "AC9/Def14: the release job permissions must include `id-token: write` so the attestation step can mint an OIDC token for the signing provider.");

        // Guard against the dual-grant footgun: the narrowed `attestations: read` from round-8
        // must be REPLACED, not duplicated, when Def14 wires the attestation step. YAML
        // duplicate-key semantics let a later `attestations: write` shadow an earlier
        // `attestations: read`, but the duplication is a red flag for an incomplete edit.
        jobPermissions.Contains("attestations: read", StringComparison.Ordinal).ShouldBeFalse(
            "AC9/Def14: the narrowed `attestations: read` from round-8 CR-12-4-P189 must be replaced, not duplicated, when Def14 wires the attestation step.");
    }

    // frontcomposer-quarantine: issue=12-4-trusted-release-evidence-dry-run.md#CR-12-4-Def22 owner=release-owner reason=compound-dry-run-side-effect-fixture-missing reintroduction=5-nightly-passes
    [Fact]
    [Trait("Category", "Quarantined")]
    public void Story12_4_Def22_DryRunWithSideEffectAttemptFixture_IsPresent() {
        // RED until Def22 lands: the `dry-run-from-dispatch` fixture exercises only
        // the single axis `context.dry_run=true`. Def22 calls for a compound fixture
        // that ALSO sets `checks.dry_run_side_effect_attempt=true` so the AC24
        // invariant — dry-run runs cannot reach side-effect-capable steps — is proven
        // when the workflow tries to mutate state during a dry-run.
        string root = CiGovernanceTests.RepositoryRoot();
        string fixturesPath = Path.Combine(root, "tests/ci-governance/fixtures/release-readiness-cases.json");
        string fixtureJson = File.ReadAllText(fixturesPath);

        using JsonDocument fixtureDoc = JsonDocument.Parse(fixtureJson);
        JsonElement? matchingCase = null;
        foreach (JsonElement caseElement in fixtureDoc.RootElement.GetProperty("cases").EnumerateArray()) {
            if (caseElement.TryGetProperty("name", out JsonElement nameElement)
                && nameElement.ValueKind == JsonValueKind.String
                && nameElement.GetString() == "dry-run-with-side-effect-attempt") {
                matchingCase = caseElement;
                break;
            }
        }

        matchingCase.ShouldNotBeNull(
            "AC24/Def22: fixture `dry-run-with-side-effect-attempt` must exist as a cases[].name entry in release-readiness-cases.json.");

        // The fixture must set BOTH axes: context.dry_run=true AND checks.dry_run_side_effect_attempt=true.
        JsonElement override_ = matchingCase!.Value.GetProperty("override");
        override_.GetProperty("context").GetProperty("dry_run").GetBoolean().ShouldBeTrue(
            "AC24/Def22: the compound fixture must set context.dry_run=true.");
        override_.GetProperty("checks").GetProperty("dry_run_side_effect_attempt").GetBoolean().ShouldBeTrue(
            "AC24/Def22: the compound fixture must set checks.dry_run_side_effect_attempt=true.");

        // A dry-run that attempts a side effect must classify `blocked` with publish_authorized=false.
        matchingCase.Value.GetProperty("expected_classification").GetString().ShouldBe(
            "blocked",
            "AC24/Def22: a dry-run that attempts a side effect must classify as blocked.");
        matchingCase.Value.GetProperty("expected_publish_authorized").GetBoolean().ShouldBeFalse(
            "AC24/Def22: a dry-run that attempts a side effect cannot authorize publishing.");

        // F3 (Story 12.4 test review): round-trip the fixture through classify-fixtures to assert
        // the classifier actually enforces AC24. Fixture-shape assertions above prove the input
        // is correctly authored; this block proves the contract is alive end-to-end. Mirrors
        // the gold-standard pattern in Story12_4_Def25_PackagesEmptyArrayFixture_*.
        string output = Path.Combine(Path.GetTempPath(), $"fc-release-readiness-def22-{Guid.NewGuid():N}.json");
        try {
            CiGovernanceTests.ProcessResult result = CiGovernanceTests.RunPython(root, [
                "eng/release_evidence.py",
                "classify-fixtures",
                "--fixtures", fixturesPath,
                "--output", output,
            ]);
            result.ExitCode.ShouldBe(0, result.Error);

            using JsonDocument resultDoc = JsonDocument.Parse(File.ReadAllText(output));
            JsonElement? compoundResult = null;
            foreach (JsonElement r in resultDoc.RootElement.GetProperty("results").EnumerateArray()) {
                if (r.TryGetProperty("name", out JsonElement n)
                    && n.ValueKind == JsonValueKind.String
                    && n.GetString() == "dry-run-with-side-effect-attempt") {
                    compoundResult = r;
                    break;
                }
            }

            compoundResult.ShouldNotBeNull(
                "AC24/Def22: classify-fixtures must include a result entry for the dry-run-with-side-effect-attempt case.");
            compoundResult!.Value.GetProperty("classification").GetString().ShouldBe(
                "blocked",
                "AC24/Def22: classify-fixtures must report blocked for a dry-run that attempts a side effect (proves the classifier enforces the contract, not just that the fixture is correctly authored).");
            compoundResult.Value.GetProperty("publish_authorized").GetBoolean().ShouldBeFalse(
                "AC24/Def22: classify-fixtures must not authorize publishing for a dry-run that attempts a side effect.");
        } finally {
            if (File.Exists(output)) {
                File.Delete(output);
            }
        }
    }

    // frontcomposer-quarantine: issue=12-4-trusted-release-evidence-dry-run.md#CR-12-4-Def23 owner=release-owner reason=manifest-missing-fingerprints-no-root-fixture-missing reintroduction=5-nightly-passes
    [Fact]
    [Trait("Category", "Quarantined")]
    public void Story12_4_Def23_ManifestMissingFingerprints_NoRoot_IsBlocked() {
        // RED until Def23 lands: AC30 binds release-definition fingerprints into the
        // sealed manifest so replayed evidence cannot authorize publishing. The
        // current `--no-root` verify-manifest path only enforces the fingerprint
        // contract when `root is not None`. Def23 calls for a fixture that proves
        // verify-manifest --no-root still fails closed when release_definition_fingerprints
        // is missing or empty from the sealed manifest.
        //
        // F5 (Story 12.4 test review): the unsealed manifest below passes EVERY OTHER
        // manifest-diagnostics axis under --no-root — all top-level fields are concrete,
        // the single package row has all REQUIRED_ROW_FIELDS with signing/timestamp/
        // attestation statuses set to `verified`/`attested`, and the seal is computed by
        // the helper itself via `seal-manifest`. The ONLY failing axis is
        // `release_definition_fingerprints: {}`. Today the fingerprints check is gated by
        // `root is not None`, so `verify-manifest --no-root` exits 0 → the first assertion
        // fails RED on the precise axis we want. When Def23 lifts the gate, the test
        // transitions cleanly RED → GREEN without conflating with seal-hash or
        // package-row failure modes.
        string root = CiGovernanceTests.RepositoryRoot();
        string unsealedManifest = Path.Combine(Path.GetTempPath(), $"fc-manifest-no-fingerprints-unsealed-{Guid.NewGuid():N}.json");
        string sealedManifest = Path.Combine(Path.GetTempPath(), $"fc-manifest-no-fingerprints-sealed-{Guid.NewGuid():N}.json");
        try {
            File.WriteAllText(unsealedManifest, """
                {
                  "commit_sha": "abc123",
                  "tag": "v1.2.3",
                  "run_id": "42",
                  "workflow_ref": "Hexalith/Hexalith.FrontComposer/.github/workflows/release.yml@refs/tags/v1.2.3",
                  "sbom_hash": "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                  "benchmark_summary_hash": "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc",
                  "helper_version": { "version": "1.0.0", "content_sha256": "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" },
                  "release_definition_fingerprints": {},
                  "package_set_fingerprint": "eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee",
                  "packages": [
                    {
                      "package_id": "Hexalith.FrontComposer.Cli",
                      "version": "1.2.3",
                      "commit_sha": "abc123",
                      "artifact_path": "release-evidence/packages/Hexalith.FrontComposer.Cli.1.2.3.nupkg",
                      "checksum": "dddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddd",
                      "symbol_artifact": "release-evidence/packages/Hexalith.FrontComposer.Cli.1.2.3.snupkg",
                      "sbom_component": "Hexalith.FrontComposer.Cli@1.2.3",
                      "signing_status": "verified",
                      "timestamp_status": "verified",
                      "attestation_status": "attested",
                      "publish_status": "pending"
                    }
                  ]
                }
                """);

            // Use the helper's own seal-manifest path so the seal hash matches the
            // canonical-JSON algorithm verify-manifest expects. Avoids re-implementing
            // sort_keys=True + separators=(',',':') canonicalization in C#.
            CiGovernanceTests.ProcessResult sealResult = CiGovernanceTests.RunPython(root, [
                "eng/release_evidence.py",
                "seal-manifest",
                "--manifest", unsealedManifest,
                "--output", sealedManifest,
            ]);
            sealResult.ExitCode.ShouldBe(
                0,
                $"setup precondition: seal-manifest must succeed before verify-manifest can be exercised. stderr={sealResult.Error}");

            CiGovernanceTests.ProcessResult result = CiGovernanceTests.RunPython(root, [
                "eng/release_evidence.py",
                "verify-manifest",
                "--manifest", sealedManifest,
                "--no-root",
            ]);

            result.ExitCode.ShouldNotBe(
                0,
                "AC30/Def23: verify-manifest --no-root must fail closed when release_definition_fingerprints is empty. The supplied manifest is otherwise valid; today this exits 0 because the fingerprints check is gated by `root is not None`.");
            (result.Output + result.Error).Contains("release_definition_fingerprints", StringComparison.Ordinal).ShouldBeTrue(
                "AC30/Def23: the diagnostic must name the missing release_definition_fingerprints contract so operators can act.");
        } finally {
            if (File.Exists(unsealedManifest)) {
                File.Delete(unsealedManifest);
            }
            if (File.Exists(sealedManifest)) {
                File.Delete(sealedManifest);
            }
        }
    }

    // frontcomposer-quarantine: issue=12-4-trusted-release-evidence-dry-run.md#CR-12-4-Def25 owner=release-owner reason=packages-empty-fixture-missing reintroduction=5-nightly-passes
    [Fact]
    [Trait("Category", "Quarantined")]
    public void Story12_4_Def25_PackagesEmptyArrayFixture_EmitsPackageRowsRequiredDiagnostic() {
        // RED until Def25 lands: AC12 ("Manifest rows bind package id, version, …")
        // implies a non-empty packages array. The `manifest_diagnostics` helper
        // already queues a "package rows are required" diagnostic when packages
        // is null, but the analogous `packages: []` case is structurally untested.
        // A regression that silently treats an empty array as valid would pass
        // every existing fixture. This test pins the missing coverage.
        string root = CiGovernanceTests.RepositoryRoot();
        string fixturesPath = Path.Combine(root, "tests/ci-governance/fixtures/release-readiness-cases.json");
        string fixtureJson = File.ReadAllText(fixturesPath);

        using JsonDocument fixtureDoc = JsonDocument.Parse(fixtureJson);
        JsonElement? matchingCase = null;
        foreach (JsonElement caseElement in fixtureDoc.RootElement.GetProperty("cases").EnumerateArray()) {
            if (caseElement.TryGetProperty("name", out JsonElement nameElement)
                && nameElement.ValueKind == JsonValueKind.String
                && nameElement.GetString() == "packages-empty-array") {
                matchingCase = caseElement;
                break;
            }
        }

        matchingCase.ShouldNotBeNull(
            "AC12/Def25: fixture `packages-empty-array` must exist as a cases[].name entry in release-readiness-cases.json.");

        JsonElement override_ = matchingCase!.Value.GetProperty("override");
        JsonElement packages = override_.GetProperty("manifest").GetProperty("packages");
        packages.ValueKind.ShouldBe(
            JsonValueKind.Array,
            "AC12/Def25: the fixture override must set manifest.packages to an empty array literal.");
        packages.GetArrayLength().ShouldBe(
            0,
            "AC12/Def25: the fixture must hold an empty packages array (not omitted, not null) to prove the empty-array path is rejected.");

        matchingCase.Value.GetProperty("expected_classification").GetString().ShouldBe(
            "blocked",
            "AC12/Def25: an empty packages array must classify as blocked.");

        // Round-trip the fixture through classify-fixtures to assert the typed diagnostic surfaces.
        string output = Path.Combine(Path.GetTempPath(), $"fc-release-readiness-{Guid.NewGuid():N}.json");
        try {
            CiGovernanceTests.ProcessResult result = CiGovernanceTests.RunPython(root, [
                "eng/release_evidence.py",
                "classify-fixtures",
                "--fixtures", fixturesPath,
                "--output", output,
            ]);
            result.ExitCode.ShouldBe(0, result.Error);

            using JsonDocument resultDoc = JsonDocument.Parse(File.ReadAllText(output));
            JsonElement emptyPackagesResult = resultDoc.RootElement.GetProperty("results").EnumerateArray()
                .Single(r => r.GetProperty("name").GetString() == "packages-empty-array");
            emptyPackagesResult.GetProperty("classification").GetString().ShouldBe(
                "blocked",
                "AC12/Def25: classify-fixtures must report blocked for the empty-packages case.");

            // The grouped_reasons.blocking array must surface the typed "package rows are required" diagnostic.
            bool diagnosticPresent = false;
            if (emptyPackagesResult.TryGetProperty("grouped_reasons", out JsonElement grouped)
                && grouped.TryGetProperty("blocking", out JsonElement blocking)
                && blocking.ValueKind == JsonValueKind.Array) {
                foreach (JsonElement reason in blocking.EnumerateArray()) {
                    if ((reason.GetString() ?? string.Empty).Contains("package rows are required", StringComparison.Ordinal)) {
                        diagnosticPresent = true;
                        break;
                    }
                }
            }
            diagnosticPresent.ShouldBeTrue(
                "AC12/Def25: the typed `package rows are required` diagnostic must appear in grouped_reasons.blocking for the empty-packages case.");
        } finally {
            if (File.Exists(output)) {
                File.Delete(output);
            }
        }
    }
}
