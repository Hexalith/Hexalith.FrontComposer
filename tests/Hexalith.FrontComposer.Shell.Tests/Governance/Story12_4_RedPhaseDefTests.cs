using System.Text.Json;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Governance;

// ----------------------------------------------------------------------------
// Story 12.4 — Release-evidence regression pins (formerly red-phase ATDD scaffolds).
//
// These tests were authored as quarantined red-phase scaffolds for the open
// CR-12-4-DefN items. As of this change those Def items are IMPLEMENTED, so the
// tests are GREEN and the per-method Quarantined trait + frontcomposer-quarantine
// metadata comments have been removed — they now run as ordinary regression pins:
//
//   Def14  -> release.yml wires `actions/attest-build-provenance@v2` before the
//             live publish + restores `attestations: write` / `id-token: write`
//             (minimal structural wiring; the build-step reordering needed for the
//             attestation to FUNCTION at runtime remains tracked in deferred-work.md).
//   Def22  -> compound dry-run-with-side-effect-attempt fixture.
//   Def23  -> verify-manifest --no-root fails closed on empty fingerprints.
//   Def25  -> packages-empty-array fixture + "package rows are required" diagnostic.
//   Def102 -> approved_at == expires_at boundary fixture + ordering check.
//   Def103 -> approved_at 365-day boundary fixture.
//   Def104 -> partial_publish_state recovered/full fixtures.
//   Def105 -> string/JSON-boolean strictness for approval + concurrent guard.
//   Def106 -> credentialed-URL + signing-material dangerous-evidence patterns.
//   Def107 -> fallback affected_artifact mismatch cross-check.
//
// They live in a SEPARATE class (no Governance Category class-level trait) and reach
// into `CiGovernanceTests` for shared `internal static` helpers. They run in the
// default blocking test lane (Category!=Quarantined etc.), not the Gate 2b governance
// filter — which is fine now that none carry the Quarantined trait.
// ----------------------------------------------------------------------------
public sealed class Story12_4_RedPhaseDefTests {
    [Fact]
    public void Story12_4_Def14_AttestBuildProvenanceStep_IsWiredInReleaseWorkflow() {
        // Regression pin (Def14, implemented): AC9 ("Attestations are generated and verified before
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

    [Fact]
    public void Story12_4_Def14_AttestationsWritePermission_IsRestored() {
        // Regression pin (Def14, implemented): round-8 CR-12-4-P189 deliberately narrowed the job
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

    [Fact]
    public void Story12_4_Def22_DryRunWithSideEffectAttemptFixture_IsPresent() {
        // Regression pin (Def22, implemented): the `dry-run-from-dispatch` fixture exercises only
        // the single axis `context.dry_run=true`. Def22 calls for a compound fixture
        // that ALSO sets `checks.dry_run_side_effect_attempt=true` so the AC24
        // invariant — dry-run runs cannot reach side-effect-capable steps — is proven
        // when the workflow tries to mutate state during a dry-run.
        string root = CiGovernanceTests.RepositoryRoot();
        string fixturesPath = Path.Combine(root, "tests/ci-governance/fixtures/release-readiness-cases.json");
        string fixtureJson = File.ReadAllText(fixturesPath);

        using var fixtureDoc = JsonDocument.Parse(fixtureJson);
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

            using var resultDoc = JsonDocument.Parse(File.ReadAllText(output));
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
        }
        finally {
            if (File.Exists(output)) {
                File.Delete(output);
            }
        }
    }

    [Fact]
    public void Story12_4_Def23_ManifestMissingFingerprints_NoRoot_IsBlocked() {
        // Regression pin (Def23, implemented): AC30 binds release-definition fingerprints into the
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
        }
        finally {
            if (File.Exists(unsealedManifest)) {
                File.Delete(unsealedManifest);
            }
            if (File.Exists(sealedManifest)) {
                File.Delete(sealedManifest);
            }
        }
    }

    [Fact]
    public void Story12_4_Def25_PackagesEmptyArrayFixture_EmitsPackageRowsRequiredDiagnostic() {
        // Regression pin (Def25, implemented): AC12 ("Manifest rows bind package id, version, …")
        // implies a non-empty packages array. The `manifest_diagnostics` helper
        // already queues a "package rows are required" diagnostic when packages
        // is null, but the analogous `packages: []` case is structurally untested.
        // A regression that silently treats an empty array as valid would pass
        // every existing fixture. This test pins the missing coverage.
        string root = CiGovernanceTests.RepositoryRoot();
        string fixturesPath = Path.Combine(root, "tests/ci-governance/fixtures/release-readiness-cases.json");
        string fixtureJson = File.ReadAllText(fixturesPath);

        using var fixtureDoc = JsonDocument.Parse(fixtureJson);
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

            using var resultDoc = JsonDocument.Parse(File.ReadAllText(output));
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
        }
        finally {
            if (File.Exists(output)) {
                File.Delete(output);
            }
        }
    }

    [Fact]
    public void Story12_4_Def102_FallbackApprovedAtEqualsExpiresAtFixture_IsPresentAndBlocked() {
        // Regression pin (Def102, implemented): P243 requires fallback approvals to be strictly
        // before their expiry. A future refactor that permits approved_at == expires_at
        // would silently re-open the operator-footgun window unless the exact boundary
        // is pinned in the release-readiness fixture corpus.
        string root = CiGovernanceTests.RepositoryRoot();
        string fixturesPath = ReleaseReadinessFixturesPath(root);

        using var fixtureDoc = JsonDocument.Parse(File.ReadAllText(fixturesPath));
        JsonElement matchingCase = RequireFixtureCase(
            fixtureDoc.RootElement,
            "fallback-approved-at-equals-expires-at",
            "Def102: fixture `fallback-approved-at-equals-expires-at` must exist as a cases[].name entry in release-readiness-cases.json.");

        JsonElement fallback = matchingCase.GetProperty("override").GetProperty("attestation").GetProperty("fallback");
        fallback.GetProperty("approved_at").GetString().ShouldBe(
            fallback.GetProperty("expires_at").GetString(),
            "Def102: the fixture must pin the exact approved_at == expires_at boundary.");
        matchingCase.GetProperty("expected_classification").GetString().ShouldBe(
            "blocked",
            "Def102: a fallback approved at its expiry instant must classify as blocked.");
        matchingCase.GetProperty("expected_publish_authorized").GetBoolean().ShouldBeFalse(
            "Def102: a fallback approved at its expiry instant must not authorize publishing.");

        string output = Path.Combine(Path.GetTempPath(), $"fc-release-readiness-def102-{Guid.NewGuid():N}.json");
        try {
            using JsonDocument resultDoc = ClassifyReleaseReadinessFixtures(root, fixturesPath, output);
            JsonElement result = RequireClassifierResult(resultDoc.RootElement, "fallback-approved-at-equals-expires-at");
            result.GetProperty("classification").GetString().ShouldBe("blocked");
            BlockingReasonsContain(result, "approved_at", "expires_at").ShouldBeTrue(
                "Def102: the classifier diagnostic must name the approved_at/expires_at boundary.");
        }
        finally {
            DeleteIfExists(output);
        }
    }

    [Fact]
    public void Story12_4_Def103_FallbackApprovedAtExactly365DaysOldFixture_IsPresentAndBlocked() {
        // Regression pin (Def103, implemented): P259 changed fallback approval age from `>` to
        // `>=` 365 days. The exact boundary needs a fixture so the regression cannot
        // slip back through as an apparently harmless off-by-one.
        string root = CiGovernanceTests.RepositoryRoot();
        string fixturesPath = ReleaseReadinessFixturesPath(root);

        using var fixtureDoc = JsonDocument.Parse(File.ReadAllText(fixturesPath));
        JsonElement matchingCase = RequireFixtureCase(
            fixtureDoc.RootElement,
            "fallback-approved-at-365-day-boundary",
            "Def103: fixture `fallback-approved-at-365-day-boundary` must exist as a cases[].name entry in release-readiness-cases.json.");

        matchingCase.GetProperty("expected_classification").GetString().ShouldBe(
            "blocked",
            "Def103: a fallback approval that is exactly 365 days old must classify as blocked.");
        matchingCase.GetProperty("expected_publish_authorized").GetBoolean().ShouldBeFalse(
            "Def103: a 365-day-old fallback approval must not authorize publishing.");

        string output = Path.Combine(Path.GetTempPath(), $"fc-release-readiness-def103-{Guid.NewGuid():N}.json");
        try {
            using JsonDocument resultDoc = ClassifyReleaseReadinessFixtures(root, fixturesPath, output);
            JsonElement result = RequireClassifierResult(resultDoc.RootElement, "fallback-approved-at-365-day-boundary");
            result.GetProperty("classification").GetString().ShouldBe("blocked");
            BlockingReasonsContain(result, "365").ShouldBeTrue(
                "Def103: the classifier diagnostic must name the 365-day fallback approval boundary.");
        }
        finally {
            DeleteIfExists(output);
        }
    }

    [Fact]
    public void Story12_4_Def104_PartialPublishRecoveredAndFullFixtures_ArePresentAndRequireRerunReview() {
        // Regression pin (Def104, implemented): the classifier treats every non-none partial publish
        // state as rerun-review, but the fixture corpus only pins `partial`. Add
        // `recovered` and `full` so future state-machine edits cannot accidentally
        // route those publish-side-effect states through the trusted happy path.
        string root = CiGovernanceTests.RepositoryRoot();
        string fixturesPath = ReleaseReadinessFixturesPath(root);

        using var fixtureDoc = JsonDocument.Parse(File.ReadAllText(fixturesPath));
        string[] caseNames = ["partial-publish-state-recovered", "partial-publish-state-full"];
        string[] states = ["recovered", "full"];
        for (int i = 0; i < caseNames.Length; i++) {
            JsonElement matchingCase = RequireFixtureCase(
                fixtureDoc.RootElement,
                caseNames[i],
                $"Def104: fixture `{caseNames[i]}` must exist as a cases[].name entry in release-readiness-cases.json.");

            matchingCase.GetProperty("override").GetProperty("context").GetProperty("partial_publish_state").GetString().ShouldBe(
                states[i],
                $"Def104: `{caseNames[i]}` must set context.partial_publish_state={states[i]}.");
            matchingCase.GetProperty("expected_context_class").GetString().ShouldBe(
                "rerun-review",
                $"Def104: `{caseNames[i]}` must require rerun-review.");
            matchingCase.GetProperty("expected_classification").GetString().ShouldBe(
                "blocked",
                $"Def104: `{caseNames[i]}` must classify as blocked.");
            matchingCase.GetProperty("expected_publish_authorized").GetBoolean().ShouldBeFalse(
                $"Def104: `{caseNames[i]}` must not authorize publishing.");
        }

        string output = Path.Combine(Path.GetTempPath(), $"fc-release-readiness-def104-{Guid.NewGuid():N}.json");
        try {
            using JsonDocument resultDoc = ClassifyReleaseReadinessFixtures(root, fixturesPath, output);
            foreach (string caseName in caseNames) {
                JsonElement result = RequireClassifierResult(resultDoc.RootElement, caseName);
                result.GetProperty("context_class").GetString().ShouldBe("rerun-review");
                result.GetProperty("classification").GetString().ShouldBe("blocked");
                result.GetProperty("publish_authorized").GetBoolean().ShouldBeFalse();
            }
        }
        finally {
            DeleteIfExists(output);
        }
    }

    [Fact]
    public void Story12_4_Def105_StringBooleanSymmetryFixtures_ArePresentAndBlocked() {
        // Regression pin (Def105, implemented): existing fixtures catch string false for approval
        // and string true for concurrent publish. The inverse stringly-typed cases
        // need fixtures too, otherwise truthy-string coercion regressions can survive.
        string root = CiGovernanceTests.RepositoryRoot();
        string fixturesPath = ReleaseReadinessFixturesPath(root);

        using var fixtureDoc = JsonDocument.Parse(File.ReadAllText(fixturesPath));
        JsonElement stringTrueApproval = RequireFixtureCase(
            fixtureDoc.RootElement,
            "string-true-approval",
            "Def105: fixture `string-true-approval` must exist as a cases[].name entry in release-readiness-cases.json.");
        stringTrueApproval.GetProperty("override").GetProperty("approval").GetProperty("approved").GetString().ShouldBe(
            "true",
            "Def105: string-true-approval must pin approval.approved as a string literal, not a JSON boolean.");
        stringTrueApproval.GetProperty("expected_classification").GetString().ShouldBe("blocked");
        stringTrueApproval.GetProperty("expected_publish_authorized").GetBoolean().ShouldBeFalse();

        JsonElement stringFalseConcurrent = RequireFixtureCase(
            fixtureDoc.RootElement,
            "concurrent-same-version-string-false",
            "Def105: fixture `concurrent-same-version-string-false` must exist as a cases[].name entry in release-readiness-cases.json.");
        stringFalseConcurrent.GetProperty("override").GetProperty("checks").GetProperty("concurrent_same_version").GetString().ShouldBe(
            "false",
            "Def105: concurrent-same-version-string-false must pin checks.concurrent_same_version as a string literal, not a JSON boolean.");
        stringFalseConcurrent.GetProperty("expected_classification").GetString().ShouldBe("blocked");
        stringFalseConcurrent.GetProperty("expected_publish_authorized").GetBoolean().ShouldBeFalse();

        string output = Path.Combine(Path.GetTempPath(), $"fc-release-readiness-def105-{Guid.NewGuid():N}.json");
        try {
            using JsonDocument resultDoc = ClassifyReleaseReadinessFixtures(root, fixturesPath, output);
            RequireClassifierResult(resultDoc.RootElement, "string-true-approval").GetProperty("classification").GetString().ShouldBe("blocked");
            RequireClassifierResult(resultDoc.RootElement, "concurrent-same-version-string-false").GetProperty("classification").GetString().ShouldBe("blocked");
        }
        finally {
            DeleteIfExists(output);
        }
    }

    [Fact]
    public void Story12_4_Def106_DangerousEvidenceFixtures_CoverCredentialedUrlsAndSigningMaterial() {
        // Regression pin (Def106, implemented): AC29 calls out credentialed URLs and signing
        // material markers, but the helper-side dangerous evidence patterns do not
        // currently pin those categories. These fixtures should fail closed with
        // unsafe raw evidence diagnostics.
        string root = CiGovernanceTests.RepositoryRoot();
        string fixturesPath = ReleaseReadinessFixturesPath(root);

        using var fixtureDoc = JsonDocument.Parse(File.ReadAllText(fixturesPath));
        JsonElement credentialedUrl = RequireFixtureCase(
            fixtureDoc.RootElement,
            "credentialed-url-leakage",
            "Def106: fixture `credentialed-url-leakage` must exist as a cases[].name entry in release-readiness-cases.json.");
        // The credentialed shape deliberately avoids `user:`/`tenant:` tokens so this case
        // pins the NEW credentialed-URL pattern specifically — a `user:pass@` form would
        // also trip the pre-existing tenant/user-identifier pattern, masking a regression
        // that deleted the credentialed-URL regex.
        (credentialedUrl.GetProperty("override").GetProperty("checks").GetProperty("raw_evidence").GetString() ?? string.Empty)
            .Contains("https://abc123:def456@", StringComparison.Ordinal).ShouldBeTrue(
            "Def106: credentialed-url-leakage must include a credentialed URL shape.");
        credentialedUrl.GetProperty("expected_classification").GetString().ShouldBe("blocked");
        credentialedUrl.GetProperty("expected_publish_authorized").GetBoolean().ShouldBeFalse();

        JsonElement signingMaterial = RequireFixtureCase(
            fixtureDoc.RootElement,
            "signing-material-leakage",
            "Def106: fixture `signing-material-leakage` must exist as a cases[].name entry in release-readiness-cases.json.");
        string signingEvidence = signingMaterial.GetProperty("override").GetProperty("checks").GetProperty("raw_evidence").GetString() ?? string.Empty;
        (
            signingEvidence.Contains("-----BEGIN PRIVATE KEY-----", StringComparison.Ordinal)
            || signingEvidence.Contains("-----BEGIN RSA PRIVATE KEY-----", StringComparison.Ordinal)
            || signingEvidence.Contains("-----BEGIN CERTIFICATE-----", StringComparison.Ordinal)
        ).ShouldBeTrue("Def106: signing-material-leakage must include a PEM private key or certificate marker.");
        signingMaterial.GetProperty("expected_classification").GetString().ShouldBe("blocked");
        signingMaterial.GetProperty("expected_publish_authorized").GetBoolean().ShouldBeFalse();

        string output = Path.Combine(Path.GetTempPath(), $"fc-release-readiness-def106-{Guid.NewGuid():N}.json");
        try {
            using JsonDocument resultDoc = ClassifyReleaseReadinessFixtures(root, fixturesPath, output);
            foreach (string caseName in new[] { "credentialed-url-leakage", "signing-material-leakage" }) {
                JsonElement result = RequireClassifierResult(resultDoc.RootElement, caseName);
                result.GetProperty("classification").GetString().ShouldBe("blocked");
                BlockingReasonsContain(result, "unsafe raw evidence").ShouldBeTrue(
                    $"Def106: `{caseName}` must surface the unsafe raw evidence diagnostic.");
            }
        }
        finally {
            DeleteIfExists(output);
        }
    }

    [Fact]
    public void Story12_4_Def107_FallbackAffectedArtifactMismatchFixture_IsPresentAndBlocked() {
        // Regression pin (Def107, implemented): AC34 says changed affected artifacts invalidate an
        // approved fallback, but fallback_complete currently validates only presence.
        // This fixture pins the mismatch so a fallback approved for artifact X cannot
        // authorize a manifest shipping artifact Y.
        string root = CiGovernanceTests.RepositoryRoot();
        string fixturesPath = ReleaseReadinessFixturesPath(root);

        using var fixtureDoc = JsonDocument.Parse(File.ReadAllText(fixturesPath));
        JsonElement matchingCase = RequireFixtureCase(
            fixtureDoc.RootElement,
            "fallback-affected-artifact-mismatch",
            "Def107: fixture `fallback-affected-artifact-mismatch` must exist as a cases[].name entry in release-readiness-cases.json.");

        JsonElement override_ = matchingCase.GetProperty("override");
        string affectedArtifact = override_.GetProperty("attestation").GetProperty("fallback").GetProperty("affected_artifact").GetString() ?? string.Empty;
        string shippedArtifactPath = override_.GetProperty("manifest").GetProperty("packages").EnumerateArray()
            .First()
            .GetProperty("artifact_path")
            .GetString() ?? string.Empty;
        shippedArtifactPath.EndsWith(affectedArtifact, StringComparison.Ordinal).ShouldBeFalse(
            "Def107: the fixture must approve one affected_artifact while the manifest ships a different artifact.");
        matchingCase.GetProperty("expected_classification").GetString().ShouldBe("blocked");
        matchingCase.GetProperty("expected_publish_authorized").GetBoolean().ShouldBeFalse();

        string output = Path.Combine(Path.GetTempPath(), $"fc-release-readiness-def107-{Guid.NewGuid():N}.json");
        try {
            using JsonDocument resultDoc = ClassifyReleaseReadinessFixtures(root, fixturesPath, output);
            JsonElement result = RequireClassifierResult(resultDoc.RootElement, "fallback-affected-artifact-mismatch");
            result.GetProperty("classification").GetString().ShouldBe("blocked");
            BlockingReasonsContain(result, "affected_artifact").ShouldBeTrue(
                "Def107: the classifier diagnostic must name the fallback affected_artifact mismatch.");
        }
        finally {
            DeleteIfExists(output);
        }
    }

    private static string ReleaseReadinessFixturesPath(string root)
        => Path.Combine(root, "tests/ci-governance/fixtures/release-readiness-cases.json");

    private static JsonElement RequireFixtureCase(JsonElement root, string caseName, string because) {
        JsonElement? matchingCase = null;
        foreach (JsonElement caseElement in root.GetProperty("cases").EnumerateArray()) {
            if (caseElement.TryGetProperty("name", out JsonElement nameElement)
                && nameElement.ValueKind == JsonValueKind.String
                && nameElement.GetString() == caseName) {
                matchingCase = caseElement;
                break;
            }
        }

        matchingCase.ShouldNotBeNull(because);
        return matchingCase!.Value;
    }

    private static JsonDocument ClassifyReleaseReadinessFixtures(string root, string fixturesPath, string output) {
        CiGovernanceTests.ProcessResult result = CiGovernanceTests.RunPython(root, [
            "eng/release_evidence.py",
            "classify-fixtures",
            "--fixtures", fixturesPath,
            "--output", output,
        ]);
        result.ExitCode.ShouldBe(0, result.Error);
        return JsonDocument.Parse(File.ReadAllText(output));
    }

    private static JsonElement RequireClassifierResult(JsonElement root, string caseName) {
        JsonElement? matchingResult = null;
        foreach (JsonElement resultElement in root.GetProperty("results").EnumerateArray()) {
            if (resultElement.TryGetProperty("name", out JsonElement nameElement)
                && nameElement.ValueKind == JsonValueKind.String
                && nameElement.GetString() == caseName) {
                matchingResult = resultElement;
                break;
            }
        }

        matchingResult.ShouldNotBeNull(
            $"release_evidence.py classify-fixtures must include a result entry for `{caseName}`.");
        return matchingResult!.Value;
    }

    private static bool BlockingReasonsContain(JsonElement result, params string[] fragments) {
        if (!result.TryGetProperty("grouped_reasons", out JsonElement grouped)
            || !grouped.TryGetProperty("blocking", out JsonElement blocking)
            || blocking.ValueKind != JsonValueKind.Array) {
            return false;
        }

        foreach (JsonElement reason in blocking.EnumerateArray()) {
            string reasonText = reason.GetString() ?? string.Empty;
            bool matched = true;
            foreach (string fragment in fragments) {
                if (!reasonText.Contains(fragment, StringComparison.OrdinalIgnoreCase)) {
                    matched = false;
                    break;
                }
            }

            if (matched) {
                return true;
            }
        }

        return false;
    }

    private static void DeleteIfExists(string path) {
        if (File.Exists(path)) {
            File.Delete(path);
        }
    }
}
