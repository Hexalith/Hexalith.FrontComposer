using System.Text.Json;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Governance;

// ----------------------------------------------------------------------------
// REL-3 T5 (2026-07-18) — release-model negative coverage (AC16/AC18/AC20).
//
// These pins reverse the G1 "record-and-proceed" posture: the pre-publication
// orchestrator (eng/release_prepublish.py) must fail closed on every evidence
// gap BEFORE any publication side effect, the publisher must be unable to
// repack/substitute/push unsigned paths, post-publication evidence must never
// authorize a release retroactively, and the approval mechanism must be the
// REL-4 freeze variable + publishable readiness evidence everywhere.
//
// Reaches into CiGovernanceTests for the shared internal helpers
// (RepositoryRoot / RunPython / StripYamlComments / ProcessResult), same as
// Story12_4_RedPhaseDefTests.
// ----------------------------------------------------------------------------
[Trait("Category", "Governance")]
public sealed class ReleaseModelGovernanceTests {
    [Fact]
    public void PrepublishOrchestrator_MissingSigningCredentials_FailPreparationClosed() {
        // AC5/AC10 (T5 "missing signing credentials stop preparation"): under G1 an absent
        // certificate wrote a blocking signing-readiness record and the run PROCEEDED to
        // publish unsigned bytes (v3.2.1/v3.2.2). The orchestrator must abort instead.
        // Static pin — reaching the signing phase at runtime requires the full build/pack/
        // test chain, far too heavy for a governance test.
        string root = CiGovernanceTests.RepositoryRoot();
        string orchestrator = File.ReadAllText(Path.Combine(root, "eng/release_prepublish.py"));

        orchestrator.ShouldContain(
            "signing certificate secret is not provisioned; preparation fails closed",
            customMessage: "AC5: absent signing credentials must raise a fail-closed phase failure, not a recorded readiness gap.");
        orchestrator.ShouldContain(
            "signature/timestamp verification failed for signed candidates",
            customMessage: "AC5: a failed `dotnet nuget verify --all` over the signed candidates must abort preparation.");
        // G1 markers that allowed an unsigned release to continue must not reappear.
        orchestrator.ShouldNotContain(
            "require_or_record",
            customMessage: "AC10: the G1 record-and-proceed helper must not exist in the pre-publication orchestrator.");
        orchestrator.ShouldNotContain(
            "recorded, not fatal",
            customMessage: "AC10: no evidence failure may be downgraded to a recorded-not-fatal warning.");
    }

    [Fact]
    public void PrepublishOrchestrator_PublishableClassification_IsRequiredBeforeSideEffects() {
        // AC9/AC10 (T5 "--require-publishable + authorized publish paths"): semantic-release
        // may only reach a publication side effect through the orchestrator, whose
        // classification phase demands classification=ready + publish_authorized=true.
        string root = CiGovernanceTests.RepositoryRoot();
        string orchestrator = File.ReadAllText(Path.Combine(root, "eng/release_prepublish.py"));
        string releaseConfig = File.ReadAllText(Path.Combine(root, ".releaserc.json"));

        releaseConfig.ShouldContain(
            "eng/release_prepublish.py prepare --version ${nextRelease.version}",
            customMessage: "AC10: prepareCmd must run the fail-closed pre-publication gate.");
        releaseConfig.ShouldContain(
            "eng/release_prepublish.py publish --version ${nextRelease.version}",
            customMessage: "AC11: publishCmd must run the manifest-authorized publisher.");
        releaseConfig.ShouldNotContain(
            "dotnet nuget push",
            customMessage: "AC16: no raw push path may exist outside `release_prepublish.py publish`.");

        orchestrator.ShouldContain(
            "--require-publishable",
            customMessage: "AC9: classification must run with --require-publishable so every non-ready result exits non-zero.");
        orchestrator.ShouldContain(
            "refusing to push",
            customMessage: "AC10: the publisher must re-check readiness immediately before pushing and refuse otherwise.");
        // The classification phase is the LAST prepare phase — after the manifest is
        // sealed and verified (call order inside cmd_prepare).
        orchestrator.LastIndexOf("phase_manifest(", StringComparison.Ordinal).ShouldBeLessThan(
            orchestrator.LastIndexOf("phase_classify(", StringComparison.Ordinal),
            "AC8/AC9: classification must run over the sealed manifest, after seal + verify.");
    }

    [Fact]
    public void PrepublishPublisher_PushPlan_ConsumesOnlyManifestAuthorizedSignedPaths() {
        // AC11 (T5 "publisher cannot repack/substitute/consume unsigned paths"): the publish
        // subcommand must audit every manifest row (signed path shape + exact sha256) and
        // never rebuild, repack, or push an unsigned candidate.
        string root = CiGovernanceTests.RepositoryRoot();
        string orchestrator = File.ReadAllText(Path.Combine(root, "eng/release_prepublish.py"))
            .Replace("\r\n", "\n", StringComparison.Ordinal);

        int publishStart = orchestrator.IndexOf("def cmd_publish", StringComparison.Ordinal);
        publishStart.ShouldBeGreaterThanOrEqualTo(0, "the orchestrator must define cmd_publish.");
        int publishEnd = orchestrator.IndexOf("def main", publishStart, StringComparison.Ordinal);
        publishEnd.ShouldBeGreaterThanOrEqualTo(0, "cmd_publish must precede main().");
        string publishSection = orchestrator[publishStart..publishEnd];

        publishSection.ShouldNotContain(
            "pack-release-packages.py",
            customMessage: "AC11: the publisher must not repack.");
        publishSection.ShouldNotContain(
            "\"dotnet\", \"build\"",
            customMessage: "AC11: the publisher must not rebuild.");
        publishSection.ShouldNotContain(
            "\"dotnet\", \"pack\"",
            customMessage: "AC11: the publisher must not produce new artifacts.");
        publishSection.ShouldContain(
            "startswith(\"nupkgs-signed/\")",
            customMessage: "AC11: only signed candidate paths from the sealed manifest may be pushed.");
        publishSection.ShouldContain(
            "artifact path is not a signed candidate path",
            customMessage: "AC11: a manifest row pointing outside nupkgs-signed/ must fail the publish.");
        publishSection.ShouldContain(
            "endswith(\".snupkg\")",
            customMessage: "AC11: symbol pushes must be matched from the sealed manifest rows.");
        publishSection.ShouldContain(
            "checksum mismatch",
            customMessage: "AC11: pushed bytes must hash-match the sealed checksums (post-seal mutation fails).");
        orchestrator.ShouldNotContain(
            "--skip-duplicate",
            customMessage: "Guardrail: --skip-duplicate masks partial publication and is banned.");
    }

    [Fact]
    public void ReleaseConfig_GithubAssets_AttachDurableEvidenceChainAtCreation() {
        // AC12 (T5 "durable initial-release evidence assets"): the evidence chain must ride
        // the immutable initial GitHub Release, not a short-retention Actions artifact.
        string root = CiGovernanceTests.RepositoryRoot();
        string releaseConfig = File.ReadAllText(Path.Combine(root, ".releaserc.json"));

        int githubPlugin = releaseConfig.IndexOf("@semantic-release/github", StringComparison.Ordinal);
        githubPlugin.ShouldBeGreaterThanOrEqualTo(0, ".releaserc.json must configure @semantic-release/github.");
        string githubSection = releaseConfig[githubPlugin..];
        githubSection.ShouldContain("nupkgs-signed/*.nupkg", customMessage: "AC12: signed packages must be release assets.");
        githubSection.ShouldContain("nupkgs/*.snupkg", customMessage: "AC12: symbol packages must be release assets.");
        githubSection.ShouldContain("release-evidence/*.json", customMessage: "AC12: the JSON evidence chain must be release assets.");
        githubSection.ShouldContain("release-evidence/*.txt", customMessage: "AC12: the signing transcript must be a release asset.");
    }

    [Fact]
    public void VerificationWorkflow_DownloadedEvidence_CannotAuthorizeReleaseRetroactively() {
        // AC16 (T5 "post-publication evidence cannot authorize retroactively"): the
        // independent verifier only DOWNLOADS and COMPARES. It never classifies, and it
        // fails when the downloaded readiness was not already publish-authorized before
        // publication. Comment-stripped so prose mentioning the banned command is inert.
        string root = CiGovernanceTests.RepositoryRoot();
        string workflow = File.ReadAllText(Path.Combine(root, ".github/workflows/release-evidence.yml"));
        string executable = CiGovernanceTests.StripYamlComments(workflow);

        executable.ShouldNotContain(
            "classify-release",
            customMessage: "AC16: the post-publication verifier must never run classification.");
        executable.ShouldContain(
            "was not pre-authorized",
            customMessage: "AC16: the verifier must fail when the published release lacked prior authorization.");
        executable.ShouldContain(
            "publish_authorized",
            customMessage: "AC16: the verifier must check the downloaded readiness classification.");
        executable.ShouldContain(
            "partial-publish-incident",
            customMessage: "AC14/AC19: observed divergence must produce a typed incident record.");
    }

    [Fact]
    public void ClassifyRelease_AttestationNeitherAttestedNorSealedFallback_BlocksNonZero() {
        // AC18 (T5 "attestation-absent classification failure"): a manifest whose
        // attestation state is neither `attested` (with a bundle) nor a COMPLETE sealed
        // owner-approved fallback must classify blocked, unauthorized, and exit non-zero
        // under --require-publishable. Runtime proof over the valid-shape manifest fixture
        // (attestation_status=approved-unsupported with no sealed fallback record supplied).
        string root = CiGovernanceTests.RepositoryRoot();
        string manifest = Path.Combine(root, "tests/ci-governance/fixtures/release-manifest-valid.json");
        string output = Path.Combine(Path.GetTempPath(), $"fc-release-attestation-negative-{Guid.NewGuid():N}.json");

        CiGovernanceTests.ProcessResult result = CiGovernanceTests.RunPython(root, [
            "eng/release_evidence.py",
            "classify-release",
            "--root", ".",
            "--manifest", manifest,
            "--require-publishable",
            "--event-name", "push",
            "--ref", "refs/heads/main",
            "--ref-protected", "true",
            "--output", output,
        ]);

        result.ExitCode.ShouldBe(1, "AC9/AC18: a blocked classification under --require-publishable must exit non-zero.");
        using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(output));
        doc.RootElement.GetProperty("classification").GetString().ShouldBe("blocked");
        doc.RootElement.GetProperty("publish_authorized").GetBoolean().ShouldBeFalse();
        string blockingReasons = string.Join("; ", doc.RootElement
            .GetProperty("grouped_reasons").GetProperty("blocking").EnumerateArray()
            .Select(reason => reason.GetString() ?? string.Empty));
        blockingReasons.ShouldContain(
            "fallback missing required field",
            customMessage: "AC18: an incomplete sealed fallback record must be a blocking reason.");

        // The classifier itself (not just fixture authorship) enforces the two attestation
        // negatives modeled in the fixture corpus: round-trip them through classify-fixtures.
        string fixturesOutput = Path.Combine(Path.GetTempPath(), $"fc-release-attestation-fixtures-{Guid.NewGuid():N}.json");
        CiGovernanceTests.ProcessResult fixturesRun = CiGovernanceTests.RunPython(root, [
            "eng/release_evidence.py",
            "classify-fixtures",
            "--root", ".",
            "--fixtures", Path.Combine(root, "tests/ci-governance/fixtures/release-readiness-cases.json"),
            "--output", fixturesOutput,
        ]);
        fixturesRun.ExitCode.ShouldBe(0, $"classify-fixtures must validate: {fixturesRun.Error}");
        using JsonDocument fixtures = JsonDocument.Parse(File.ReadAllText(fixturesOutput));
        foreach (string caseName in (string[])["attested-without-bundle", "discordant-attestation-status"]) {
            JsonElement item = fixtures.RootElement.GetProperty("results").EnumerateArray()
                .Single(entry => entry.GetProperty("name").GetString() == caseName);
            item.GetProperty("classification").GetString().ShouldBe(
                "blocked",
                $"AC18: fixture {caseName} must classify blocked.");
            item.GetProperty("publish_authorized").GetBoolean().ShouldBeFalse(
                $"AC18: fixture {caseName} must not authorize publication.");
        }
    }

    [Fact]
    public void ApprovalMatrix_FreezeGateVariable_IsConsistentAcrossReleaseGovernance() {
        // AC20 (T5 "APPROVAL_MATRIX <-> release.yml <-> REL-4 <-> REL-5 consistency"): the
        // machine-readable matrix must describe the ACTUAL approved mechanisms (the REL-4
        // Release Owner variable + publishable readiness evidence) and must not name the
        // forbidden dispatch/approver inputs release.yml bans. The classifier's legacy CLI
        // env surface is out of scope — the ban targets the matrix constant block only.
        string root = CiGovernanceTests.RepositoryRoot();
        string helper = File.ReadAllText(Path.Combine(root, "eng/release_evidence.py"))
            .Replace("\r\n", "\n", StringComparison.Ordinal);

        int matrixStart = helper.IndexOf("APPROVAL_MATRIX = [", StringComparison.Ordinal);
        matrixStart.ShouldBeGreaterThanOrEqualTo(0, "eng/release_evidence.py must define APPROVAL_MATRIX.");
        int matrixEnd = helper.IndexOf("\n]", matrixStart, StringComparison.Ordinal);
        matrixEnd.ShouldBeGreaterThanOrEqualTo(0, "APPROVAL_MATRIX must be a closed list literal.");
        string matrix = helper[matrixStart..matrixEnd];

        matrix.ShouldContain(
            "vars.HEXALITH_RELEASE_PUBLISH_ENABLED",
            customMessage: "AC20: the matrix must name the REL-4 Release Owner freeze variable as the publish mechanism input.");
        matrix.ShouldContain(
            "publish_authorized=true",
            customMessage: "AC20: the matrix must bind publishable readiness evidence to the publish gates.");
        matrix.ShouldNotContain(
            "workflow_dispatch",
            customMessage: "AC20: release.yml forbids manual dispatch; the matrix must not describe it.");
        matrix.ShouldNotContain(
            "release_owner_approved",
            customMessage: "AC20: the retired dispatch approval input must not reappear in the matrix.");
        matrix.ShouldNotContain(
            "release_approver",
            customMessage: "AC20: the retired approver input must not reappear in the matrix.");

        // The caller-side gate binds exactly that variable (executable content), and the
        // owner-facing stories + the upstream request name the same control.
        string releaseWorkflow = CiGovernanceTests.StripYamlComments(
            File.ReadAllText(Path.Combine(root, ".github/workflows/release.yml")));
        releaseWorkflow.ShouldContain("vars.HEXALITH_RELEASE_PUBLISH_ENABLED");
        foreach (string document in (string[])[
            "_bmad-output/implementation-artifacts/rel-4-enforce-temporary-release-freeze.md",
            "_bmad-output/implementation-artifacts/rel-5-provision-signing-identity-and-first-governed-release.md",
            "_bmad-output/planning-artifacts/g2-hexalith-builds-inline-pre-publish-gate-request.md",
        ]) {
            File.ReadAllText(Path.Combine(root, document)).ShouldContain(
                "HEXALITH_RELEASE_PUBLISH_ENABLED",
                customMessage: $"AC20: {document} must reference the common freeze-gate variable.");
        }
    }

    [Fact]
    public void PartialPublishIncident_TypedRecord_IsProducedAndWiredIntoPublishAndVerification() {
        // AC14/AC19 (T5 "partial-publication incident path"): the helper produces the typed
        // record (runtime proof), and both the publisher and the independent verifier are
        // wired to record observed divergence through it.
        string root = CiGovernanceTests.RepositoryRoot();
        string output = Path.Combine(Path.GetTempPath(), $"fc-partial-publish-incident-{Guid.NewGuid():N}.json");

        CiGovernanceTests.ProcessResult result = CiGovernanceTests.RunPython(root, [
            "eng/release_evidence.py",
            "partial-publish-incident",
            "--manifest", Path.Combine(root, "tests/ci-governance/fixtures/release-manifest-valid.json"),
            "--output", output,
            "--phase", "package-push",
            "--tag", "v9.9.9-governance-probe",
        ]);

        result.ExitCode.ShouldBe(0, $"partial-publish-incident must produce the typed record: {result.Error}");
        using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(output));
        doc.RootElement.GetProperty("decision_contract").GetString().ShouldBe("frontcomposer.partial-publish-incident.v1");
        doc.RootElement.GetProperty("failed_phase").GetString().ShouldBe("package-push");
        (doc.RootElement.GetProperty("next_owner_action").GetString() ?? string.Empty)
            .ShouldContain("reconcile", customMessage: "AC14: the incident must direct owner-led reconciliation.");

        string orchestrator = File.ReadAllText(Path.Combine(root, "eng/release_prepublish.py"));
        orchestrator.ShouldContain("_record_incident", customMessage: "AC14: the publisher must record incidents on push divergence.");
        orchestrator.ShouldContain("\"post-seal-verification\"", customMessage: "AC14: pre-push audit divergence must be a typed incident phase.");
        string verifier = CiGovernanceTests.StripYamlComments(
            File.ReadAllText(Path.Combine(root, ".github/workflows/release-evidence.yml")));
        verifier.ShouldContain("partial-publish-incident", customMessage: "AC19: the verifier must record incidents for observed divergence.");
    }

    [Fact]
    public void ClassifyRelease_HealthyDryRunEvidence_CleanExitGate_ReturnsExit0() {
        // Review VG-1 (2026-07-18): the `--dry-run-clean-exit` CLI exit gate is what
        // `prepare --non-publishing` (the only pre-CI proof of the full chain) depends on,
        // and the healthy carve-out empties the blocking list — the exact case the gate's
        // pre-review `and blocking` guard made unreachable. This traverses the REAL CLI
        // exit gate over a hermetically staged healthy evidence set (self-consistent
        // sealed manifest + on-disk artifacts under a temp work root, built by
        // tests/ci-governance/stage_release_state.py) and pins exit 0. Reverting the gate
        // to `... and blocking:` makes this test fail.
        string root = CiGovernanceTests.RepositoryRoot();
        string workRoot = Path.Combine(Path.GetTempPath(), $"fc-clean-exit-{Guid.NewGuid():N}");
        try {
            CiGovernanceTests.ProcessResult staging = CiGovernanceTests.RunPython(root, [
                "tests/ci-governance/stage_release_state.py", "evidence", root, workRoot,
            ]);
            staging.ExitCode.ShouldBe(0, $"staging must succeed: {staging.Error}");

            string output = Path.Combine(workRoot, "readiness.json");
            CiGovernanceTests.ProcessResult result = CiGovernanceTests.RunPython(root, [
                "eng/release_evidence.py",
                "classify-release",
                "--root", workRoot,
                "--evidence", Path.Combine(workRoot, "evidence.json"),
                "--require-publishable",
                "--dry-run-clean-exit",
                "--output", output,
            ]);

            result.ExitCode.ShouldBe(0, $"a HEALTHY dry-run must take the clean exit: {result.Error}");
            result.Error.ShouldContain(
                "would-be-publishable",
                customMessage: "the clean exit must announce the would-be-publishable dry-run classification.");
            using JsonDocument decision = JsonDocument.Parse(File.ReadAllText(output));
            decision.RootElement.GetProperty("classification").GetString().ShouldBeOneOf("ready", "fallback-approved");
            decision.RootElement.GetProperty("publish_authorized").GetBoolean().ShouldBeFalse(
                "a dry-run can never be publish-authorized, only would-be-publishable.");
        }
        finally {
            if (Directory.Exists(workRoot)) {
                Directory.Delete(workRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void PrepublishPublisher_PostSealArtifactMutation_FailsClosedBeforePush() {
        // Review VG-2 (2026-07-18): the publisher's exact-byte pre-push audit was only
        // source-string pinned; this is the runtime negative. A staged, fully authorized
        // release state (ready + publish_authorized=true) whose artifact bytes were
        // mutated AFTER sealing must fail closed at publish time — typed
        // post-seal-verification incident, no push attempted — even though every static
        // string pin would still pass.
        string root = CiGovernanceTests.RepositoryRoot();
        string workRoot = Path.Combine(Path.GetTempPath(), $"fc-publish-mutation-{Guid.NewGuid():N}");
        try {
            CiGovernanceTests.ProcessResult staging = CiGovernanceTests.RunPython(root, [
                "tests/ci-governance/stage_release_state.py", "publish", root, workRoot, "--corrupt-artifact",
            ]);
            staging.ExitCode.ShouldBe(0, $"staging must succeed: {staging.Error}");
            string stagedVersion = staging.Output.Trim().Split('\n')[^1].Trim();

            CiGovernanceTests.ProcessResult result = CiGovernanceTests.RunPython(
                root,
                ["eng/release_prepublish.py", "publish", "--version", stagedVersion, "--work-root", workRoot],
                new Dictionary<string, string> { ["NUGET_API_KEY"] = "governance-probe-not-a-key" });

            result.ExitCode.ShouldBe(1, "post-seal artifact mutation must fail the publish.");
            result.Output.ShouldContain("FAIL-CLOSED", customMessage: "the refusal must be an explicit fail-closed phase failure.");
            result.Output.ShouldNotContain("pushed ", customMessage: "no artifact may be pushed after a failed pre-push audit.");
            result.Output.ShouldNotContain("push failed", customMessage: "the failure must occur BEFORE the push loop, not inside it.");
            using JsonDocument incident = JsonDocument.Parse(
                File.ReadAllText(Path.Combine(workRoot, "release-evidence", "partial-publish-incident.json")));
            incident.RootElement.GetProperty("decision_contract").GetString().ShouldBe("frontcomposer.partial-publish-incident.v1");
            incident.RootElement.GetProperty("failed_phase").GetString().ShouldBe(
                "post-seal-verification",
                "the divergence is pre-push post-seal mutation, not a push-phase failure.");
        }
        finally {
            if (Directory.Exists(workRoot)) {
                Directory.Delete(workRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void PrepublishPublisher_VersionAuditMismatch_FailsClosedBeforePush() {
        // Review VG-2 (2026-07-18): second runtime layer — an internally consistent,
        // authorized release state whose sealed rows carry a DIFFERENT version than the
        // one semantic-release supplies must trip the per-row audit (not the manifest
        // re-verification), record the typed incident, and never reach the push loop.
        // Inverting the audit's version comparison routes the failure into the push loop
        // (package-push incident), which the phase assertion below catches.
        string root = CiGovernanceTests.RepositoryRoot();
        string workRoot = Path.Combine(Path.GetTempPath(), $"fc-publish-version-audit-{Guid.NewGuid():N}");
        try {
            CiGovernanceTests.ProcessResult staging = CiGovernanceTests.RunPython(root, [
                "tests/ci-governance/stage_release_state.py", "publish", root, workRoot,
            ]);
            staging.ExitCode.ShouldBe(0, $"staging must succeed: {staging.Error}");

            CiGovernanceTests.ProcessResult result = CiGovernanceTests.RunPython(
                root,
                ["eng/release_prepublish.py", "publish", "--version", "9.9.9-governance-audit-mismatch", "--work-root", workRoot],
                new Dictionary<string, string> { ["NUGET_API_KEY"] = "governance-probe-not-a-key" });

            result.ExitCode.ShouldBe(1, "a manifest/semantic-release version divergence must fail the publish.");
            result.Output.ShouldContain(
                "manifest version differs from semantic-release version",
                customMessage: "the per-row version audit must be the tripped guard.");
            result.Output.ShouldNotContain("pushed ", customMessage: "no artifact may be pushed after a failed pre-push audit.");
            using JsonDocument incident = JsonDocument.Parse(
                File.ReadAllText(Path.Combine(workRoot, "release-evidence", "partial-publish-incident.json")));
            incident.RootElement.GetProperty("failed_phase").GetString().ShouldBe(
                "post-seal-verification",
                "the audit failure is pre-push; a package-push phase here means the audit was bypassed.");
        }
        finally {
            if (Directory.Exists(workRoot)) {
                Directory.Delete(workRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void PrepublishPublisher_UnpreparedRepository_RefusesToPush() {
        // AC10/AC11 runtime negative: invoking the publisher outside a prepared, authorized
        // release context must fail closed before any push. Whichever guard trips first
        // (missing NUGET_API_KEY, missing sealed manifest, unauthorized readiness, or the
        // governance-probe version audit), the exit is non-zero and fail-closed — and no
        // `dotnet nuget push` is ever reached (the probe version can never match a sealed
        // manifest row).
        string root = CiGovernanceTests.RepositoryRoot();

        CiGovernanceTests.ProcessResult result = CiGovernanceTests.RunPython(root, [
            "eng/release_prepublish.py",
            "publish",
            "--version", "0.0.0-governance-probe",
        ]);

        result.ExitCode.ShouldBe(1, "the publisher must fail closed outside a prepared, authorized release context.");
        result.Output.ShouldContain("FAIL-CLOSED", customMessage: "AC10: the refusal must be an explicit fail-closed phase failure.");
    }
}
