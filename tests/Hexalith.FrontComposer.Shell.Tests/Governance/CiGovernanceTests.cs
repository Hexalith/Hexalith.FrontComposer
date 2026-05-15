using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Shell.Tests.Governance;

[Trait("Category", "Governance")]
public sealed class CiGovernanceTests {
    [Fact]
    public void BuildAndTestJob_IsBlockingAndHasGovernanceTelemetryGate() {
        string root = RepositoryRoot();
        string workflow = File.ReadAllText(Path.Combine(root, ".github/workflows/ci.yml"));

        string buildJob = workflow[(workflow.IndexOf("  build-and-test:", StringComparison.Ordinal))..];
        string buildJobHeader = buildJob[..buildJob.IndexOf("    steps:", StringComparison.Ordinal)];

        buildJobHeader.ShouldNotContain("continue-on-error: true");
        workflow.ShouldContain("Gate 2b: Infrastructure governance and telemetry contracts");
        workflow.ShouldContain("Category=Governance");
        workflow.ShouldContain("test-results-governance.trx");
    }

    [Fact]
    public void Gate2bGovernanceStep_IsNotMarkedAdvisory() {
        // F31 — verify the Gate 2b step itself never carries `continue-on-error: true`. The
        // job-header check above only proves the job is not advisory at job scope; a future
        // edit that marked the governance STEP advisory would slip through. Find the named
        // step body and assert its block does not contain a step-level continue-on-error flag.
        string root = RepositoryRoot();
        string workflow = File.ReadAllText(Path.Combine(root, ".github/workflows/ci.yml"));
        string gateName = "Gate 2b: Infrastructure governance and telemetry contracts";
        int idx = workflow.IndexOf(gateName, StringComparison.Ordinal);
        idx.ShouldBeGreaterThanOrEqualTo(0, $"workflow is missing the named step '{gateName}'.");

        // A step block ends at the next `      - name:` (six-space indented dash) or end of file.
        int nextStep = workflow.IndexOf("      - name:", idx + gateName.Length, StringComparison.Ordinal);
        string stepBody = nextStep < 0 ? workflow[idx..] : workflow[idx..nextStep];

        stepBody.ShouldNotContain("continue-on-error: true");
    }

    [Fact]
    public void BlockingTestLanes_ExcludeQuarantinedTestsWithoutSkippingGovernance() {
        string root = RepositoryRoot();
        string ci = File.ReadAllText(Path.Combine(root, ".github/workflows/ci.yml"));
        string release = File.ReadAllText(Path.Combine(root, ".github/workflows/release.yml"));

        string defaultLane = ExtractNamedStep(ci, "Gate 3a: Unit + bUnit (default lane)");
        defaultLane.ShouldContain("Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined");
        defaultLane.ShouldNotContain("continue-on-error: true");

        string governanceLane = ExtractNamedStep(ci, "Gate 2b: Infrastructure governance and telemetry contracts");
        governanceLane.ShouldContain("Category=Governance");
        governanceLane.ShouldNotContain("Category!=Quarantined");
        governanceLane.ShouldNotContain("continue-on-error: true");

        release.ShouldContain("--filter \"Category!=Quarantined\"");
        release.ShouldNotContain("continue-on-error: true");
    }

    [Fact]
    public void QuarantineLane_IsWarningOnlyAndPublishesBoundedEvidence() {
        string root = RepositoryRoot();
        string ci = File.ReadAllText(Path.Combine(root, ".github/workflows/ci.yml"));

        string quarantineLane = ExtractNamedStep(ci, "Gate 3d: Quarantined tests (warning-only)");
        quarantineLane.ShouldContain("continue-on-error: true");
        quarantineLane.ShouldContain("--filter \"Category=Quarantined\"");
        quarantineLane.ShouldContain("LogFilePrefix=test-results-quarantine");

        ci.ShouldContain("ci_governance.py summarize-quarantine");
        ci.ShouldContain("artifacts/quarantine/quarantine-summary.md");
        ci.ShouldContain("artifacts/quarantine/quarantine-summary.json");
        ci.ShouldContain("Upload quarantine artifacts");
    }

    [Fact]
    public void Workflows_UseRootLevelSubmodulesOnly() {
        // Story 11.7 code review P-7 — match enabling forms of recursive submodule commands
        // and flag values with whitespace tolerance. Explicitly allow disable-forms
        // (e.g. `--recurse-submodules=no`) and YAML comments that mention the flag.
        Regex recursiveCommand = new(
            @"\bgit\s+submodule\s+update\b(?:(?!\r?\n)[^\r\n])*?\s--recursive\b",
            RegexOptions.CultureInvariant);
        Regex recurseFlagEnabling = new(
            @"--recurse-submodules(?:\s|=(?:true|yes|on-demand))",
            RegexOptions.CultureInvariant);
        Regex submodulesRecursive = new(
            @"\bsubmodules\s*:\s*recursive\b",
            RegexOptions.CultureInvariant);

        string root = RepositoryRoot();
        foreach (string workflow in Directory.EnumerateFiles(Path.Combine(root, ".github/workflows"), "*.yml")) {
            string name = Path.GetFileName(workflow);
            string text = StripYamlComments(File.ReadAllText(workflow));
            recursiveCommand.IsMatch(text).ShouldBeFalse($"{name} must not enable recursive submodule updates.");
            recurseFlagEnabling.IsMatch(text).ShouldBeFalse($"{name} must not enable --recurse-submodules.");
            submodulesRecursive.IsMatch(text).ShouldBeFalse($"{name} must not use submodules: recursive.");
        }
    }

    private static string StripYamlComments(string yaml) {
        // Remove YAML comments (anything from `#` to end-of-line) so a comment that mentions
        // a forbidden command cannot trigger the assertion. Preserves line numbering.
        StringBuilder sb = new(yaml.Length);
        foreach (string line in yaml.Split('\n')) {
            int hashIndex = -1;
            bool inSingleQuote = false;
            bool inDoubleQuote = false;
            for (int i = 0; i < line.Length; i++) {
                char ch = line[i];
                if (ch == '\'' && !inDoubleQuote) {
                    inSingleQuote = !inSingleQuote;
                }
                else if (ch == '"' && !inSingleQuote) {
                    inDoubleQuote = !inDoubleQuote;
                }
                else if (ch == '#' && !inSingleQuote && !inDoubleQuote) {
                    hashIndex = i;
                    break;
                }
            }

            sb.Append(hashIndex < 0 ? line : line[..hashIndex]);
            sb.Append('\n');
        }

        return sb.ToString();
    }

    [Fact]
    public void NightlyBenchmarkWorkflow_UsesEmbeddedPromptContractAndReadOnlyEvidence() {
        string root = RepositoryRoot();
        string workflow = File.ReadAllText(Path.Combine(root, ".github/workflows/nightly.yml"));

        workflow.ShouldContain("schedule:");
        workflow.ShouldContain("workflow_dispatch:");
        workflow.ShouldContain("contents: read");
        workflow.ShouldContain("submodules: true");
        workflow.ShouldContain("eng/llm_benchmark.py validate-prompt-set");
        workflow.ShouldContain("eng/llm_benchmark.py run-benchmark");
        workflow.ShouldContain("SkillBenchmarkPromptSet.LoadEmbeddedV1");
        workflow.ShouldContain("budget-status");
        workflow.ShouldContain("BenchmarkHarnessTests");
        workflow.ShouldContain("candidate evidence only");
        workflow.ShouldContain("28-day ratchet");

        string budget = Path.Combine(Path.GetTempPath(), $"fc-budget-{Guid.NewGuid():N}.json");
        string output = Path.Combine(Path.GetTempPath(), $"fc-benchmark-run-{Guid.NewGuid():N}.json");
        File.WriteAllText(budget, """{"status":"budget-unknown","api_spend_allowed":false}""");
        ProcessResult run = RunPython(root, [
            "eng/llm_benchmark.py",
            "run-benchmark",
            "--root", ".",
            "--budget-artifact", budget,
            "--output", output,
        ]);
        run.ExitCode.ShouldNotBe(0);
        using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(output));
        doc.RootElement.GetProperty("prompt_count").GetInt32().ShouldBe(20);
        doc.RootElement.GetProperty("classification").GetString().ShouldBe("budget-blocked");
    }

    [Fact]
    public void ReleaseWorkflow_AddsSbomSigningAttestationAndManifestGatesAfterBlockingTests() {
        string root = RepositoryRoot();
        string workflow = File.ReadAllText(Path.Combine(root, ".github/workflows/release.yml"));
        string releaseConfig = File.ReadAllText(Path.Combine(root, ".releaserc.json"));

        workflow.ShouldContain("contents: read");
        workflow.ShouldContain("contents: write");
        workflow.ShouldContain("id-token: write");
        workflow.ShouldContain("attestations: write");
        workflow.ShouldContain("submodules: true");
        workflow.ShouldNotContain("submodules: recursive");

        workflow.IndexOf("Run All Tests", StringComparison.Ordinal).ShouldBeLessThan(workflow.IndexOf("Run semantic-release", StringComparison.Ordinal));
        workflow.IndexOf("Record approved attestation fallback before publish", StringComparison.Ordinal).ShouldBeLessThan(workflow.IndexOf("Run semantic-release", StringComparison.Ordinal));
        workflow.ShouldContain("Verify release test evidence");
        workflow.ShouldContain("release-evidence/test-results.json");
        workflow.ShouldContain("Preflight package inventory");
        workflow.ShouldContain("Require governed attestation fallback before publish");
        workflow.ShouldContain("attestation-unavailable.md");
        workflow.ShouldContain("release-budget");
        workflow.ShouldContain("--append-current");
        workflow.ShouldNotContain("|| true");

        releaseConfig.ShouldContain("--include-symbols");
        releaseConfig.ShouldContain("CycloneDX");
        releaseConfig.ShouldContain("dotnet nuget sign");
        releaseConfig.ShouldContain("--timestamper");
        releaseConfig.ShouldContain("dotnet nuget verify");
        releaseConfig.ShouldContain("prepare-manifest");
        releaseConfig.ShouldContain("seal-manifest");
        releaseConfig.ShouldContain("verify-manifest");
        releaseConfig.ShouldContain("nupkgs-signed/*.nupkg");
        releaseConfig.ShouldContain("nupkgs/*.snupkg");
        releaseConfig.ShouldContain("release-evidence/checksums.json");
        releaseConfig.ShouldContain("release-evidence/release-budget-summary.json");
        releaseConfig.ShouldContain("partial-publish-incident.json");
    }

    [Fact]
    public void PackageInventory_IsExplicitLockstepAndReviewable() {
        string root = RepositoryRoot();
        string inventory = File.ReadAllText(Path.Combine(root, "eng/release-package-inventory.json"));
        string directoryTargets = File.ReadAllText(Path.Combine(root, "Directory.Build.targets"));
        string testingProject = File.ReadAllText(Path.Combine(root, "src/Hexalith.FrontComposer.Testing/Hexalith.FrontComposer.Testing.csproj"));

        inventory.ShouldContain("Hexalith.FrontComposer.Cli");
        inventory.ShouldContain("Hexalith.FrontComposer.Contracts");
        inventory.ShouldContain("Hexalith.FrontComposer.Mcp");
        inventory.ShouldContain("Hexalith.FrontComposer.Schema");
        inventory.ShouldContain("Hexalith.FrontComposer.Shell");
        inventory.ShouldContain("Hexalith.FrontComposer.Testing");
        inventory.ShouldContain("Hexalith.FrontComposer.SourceTools");
        inventory.ShouldContain("\"packable\": false");
        inventory.ShouldContain("exception");
        directoryTargets.ShouldContain("Condition=\"'$(IsPackable)' == 'true' AND '$(EnableFrontComposerPackageValidation)' == 'true'\"");
        directoryTargets.ShouldContain("<IncludeSymbols>true</IncludeSymbols>");
        directoryTargets.ShouldContain("<SymbolPackageFormat>snupkg</SymbolPackageFormat>");
        testingProject.ShouldNotContain("<Version>");

        string output = Path.Combine(Path.GetTempPath(), $"fc-release-inventory-{Guid.NewGuid():N}.json");
        ProcessResult result = RunPython(root, [
            "eng/release_evidence.py",
            "inventory",
            "--root", ".",
            "--expected", "eng/release-package-inventory.json",
            "--output", output,
        ]);

        result.ExitCode.ShouldBe(0, result.Error);
        using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(output));
        doc.RootElement.GetProperty("status").GetString().ShouldBe("valid");
        doc.RootElement.GetProperty("expected_version_source").GetString().ShouldBe("semantic-release");

        string unexpectedRoot = Path.Combine(Path.GetTempPath(), $"fc-release-inventory-root-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(unexpectedRoot, "src", "Unexpected"));
        File.Copy(Path.Combine(root, "eng", "release-package-inventory.json"), Path.Combine(unexpectedRoot, "expected.json"));
        File.WriteAllText(Path.Combine(unexpectedRoot, "src", "Unexpected", "Unexpected.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <IsPackable>true</IsPackable>
                <PackageId>Unexpected.Package</PackageId>
              </PropertyGroup>
            </Project>
            """);
        ProcessResult unexpectedInventory = RunPython(root, [
            "eng/release_evidence.py",
            "inventory",
            "--root", unexpectedRoot,
            "--expected", Path.Combine(unexpectedRoot, "expected.json"),
        ]);
        unexpectedInventory.ExitCode.ShouldNotBe(0);
        unexpectedInventory.Error.ShouldBeEmpty();
    }

    [Fact]
    public void ReleaseEvidenceScript_VerifiesSealedManifestBudgetAndPathContainment() {
        string root = RepositoryRoot();
        string output = Path.Combine(Path.GetTempPath(), $"fc-release-budget-{Guid.NewGuid():N}.json");

        ProcessResult budget = RunPython(root, [
            "eng/release_evidence.py",
            "release-budget",
            "--evidence", "tests/ci-governance/fixtures/release-budget-three-breaches.json",
            "--output", output,
        ]);
        budget.ExitCode.ShouldBe(0, budget.Error);
        using (JsonDocument doc = JsonDocument.Parse(File.ReadAllText(output))) {
            (doc.RootElement.GetProperty("marker").GetString() ?? string.Empty).ShouldContain("frontcomposer:package-count-collapse");
            doc.RootElement.GetProperty("action").GetString().ShouldBe("open-or-update-package-count-collapse-issue");
            (doc.RootElement.GetProperty("recommendation").GetString() ?? string.Empty).ShouldContain("8 packages to 5");
        }

        ProcessResult untrustedApply = RunPython(root, [
            "eng/release_evidence.py",
            "release-budget",
            "--evidence", "tests/ci-governance/fixtures/release-budget-three-breaches.json",
            "--apply",
            "--event-name", "pull_request",
            "--ref", "refs/pull/1/merge",
            "--from-fork", "true",
        ]);
        untrustedApply.ExitCode.ShouldNotBe(0);
        untrustedApply.Error.ShouldContain("trusted release/main context required");

        ProcessResult validManifest = RunPython(root, [
            "eng/release_evidence.py",
            "verify-manifest",
            "--manifest", "tests/ci-governance/fixtures/release-manifest-valid.json",
        ]);
        validManifest.ExitCode.ShouldBe(0, validManifest.Error);

        ProcessResult invalidManifest = RunPython(root, [
            "eng/release_evidence.py",
            "verify-manifest",
            "--manifest", "tests/ci-governance/fixtures/release-manifest-invalid.json",
        ]);
        invalidManifest.ExitCode.ShouldNotBe(0);
        invalidManifest.Error.ShouldBeEmpty();

        string placeholderManifestPath = Path.Combine(Path.GetTempPath(), $"fc-release-placeholder-{Guid.NewGuid():N}.json");
        File.WriteAllText(placeholderManifestPath, """
            {
              "commit_sha": "abc123",
              "tag": "v1.2.3",
              "run_id": "42",
              "workflow_ref": "Hexalith/Hexalith.FrontComposer/.github/workflows/release.yml@refs/tags/v1.2.3",
              "sbom_hash": "pending-sbom-hash",
              "benchmark_summary_hash": "benchmark",
              "packages": [
                {
                  "package_id": "Hexalith.FrontComposer.Contracts",
                  "version": "1.2.3",
                  "commit_sha": "abc123",
                  "artifact_path": "nupkgs-signed/Hexalith.FrontComposer.Contracts.1.2.3.nupkg",
                  "checksum": "pending-checksum",
                  "symbol_artifact": "nupkgs/Hexalith.FrontComposer.Contracts.1.2.3.snupkg",
                  "sbom_component": "Hexalith.FrontComposer.Contracts",
                  "signing_status": "verified",
                  "attestation_status": "approved-unsupported",
                  "publish_status": "pending"
                }
              ]
            }
            """);
        ProcessResult placeholderManifest = RunPython(root, [
            "eng/release_evidence.py",
            "verify-manifest",
            "--manifest", placeholderManifestPath,
        ]);
        placeholderManifest.ExitCode.ShouldNotBe(0);

        string evidenceRoot = Path.Combine(Path.GetTempPath(), $"fc-evidence-{Guid.NewGuid():N}");
        ProcessResult pathEscape = RunPython(root, [
            "eng/release_evidence.py",
            "path-check",
            "--root", evidenceRoot,
            "--name", "../outside.json",
        ]);
        pathEscape.ExitCode.ShouldNotBe(0);
        pathEscape.Error.ShouldContain("escapes approved root");
    }

    [Fact]
    public void ReleaseEvidenceScript_ClassifiesReleaseReadinessFixtures() {
        string root = RepositoryRoot();
        string fixtures = Path.Combine(root, "tests/ci-governance/fixtures/release-readiness-cases.json");
        string output = Path.Combine(Path.GetTempPath(), $"fc-release-readiness-{Guid.NewGuid():N}.json");

        string fixtureJson = File.ReadAllText(fixtures);
        string[] requiredCases = [
            "trusted-ready",
            "approved-fallback",
            "missing-inventory-package",
            "skipped-tests",
            "zero-tests",
            "unsigned-package",
            "stale-missing-timestamp",
            "missing-sbom",
            "checksum-mismatch",
            "unsealed-manifest",
            "pr-same-repo",
            "fork-pr",
            "local-candidate",
            "recursive-submodule-command",
            "path-leakage",
            "token-like-leakage",
            "hostile-workflow-command",
            "dry-run-side-effect-attempt",
            "stale-release-definition-fingerprint",
            "post-seal-package-mutation",
            "concurrent-same-version-run",
            "stale-fallback-approval",
            "partial-helper-output",
            "rerun-review",
        ];

        foreach (string requiredCase in requiredCases) {
            fixtureJson.ShouldContain(requiredCase);
        }

        ProcessResult result = RunPython(root, [
            "eng/release_evidence.py",
            "classify-fixtures",
            "--fixtures", fixtures,
            "--output", output,
        ]);

        result.ExitCode.ShouldBe(0, result.Error);
        using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(output));
        doc.RootElement.GetProperty("status").GetString().ShouldBe("valid");

        JsonElement trustedReady = doc.RootElement.GetProperty("results").EnumerateArray()
            .Single(r => r.GetProperty("name").GetString() == "trusted-ready");
        trustedReady.GetProperty("classification").GetString().ShouldBe("ready");
        trustedReady.GetProperty("publish_authorized").GetBoolean().ShouldBeTrue();

        JsonElement localCandidate = doc.RootElement.GetProperty("results").EnumerateArray()
            .Single(r => r.GetProperty("name").GetString() == "local-candidate");
        localCandidate.GetProperty("context_class").GetString().ShouldBe("local-candidate");
        localCandidate.GetProperty("publish_authorized").GetBoolean().ShouldBeFalse();

        JsonElement fallback = doc.RootElement.GetProperty("results").EnumerateArray()
            .Single(r => r.GetProperty("name").GetString() == "approved-fallback");
        fallback.GetProperty("classification").GetString().ShouldBe("fallback-approved");
        fallback.GetProperty("publish_authorized").GetBoolean().ShouldBeTrue();
    }

    [Fact]
    public void ReleaseWorkflow_GatesPublishSideEffectsOnTypedReadinessAndOwnerApproval() {
        string root = RepositoryRoot();
        string workflow = File.ReadAllText(Path.Combine(root, ".github/workflows/release.yml"));
        string releaseConfig = File.ReadAllText(Path.Combine(root, ".releaserc.json"));

        workflow.ShouldContain("release_owner_approved");
        workflow.ShouldContain("RELEASE_OWNER_APPROVED");
        workflow.ShouldContain("RELEASE_APPROVER");
        workflow.ShouldContain("Release owner approval gate");

        releaseConfig.ShouldContain("classify-release");
        releaseConfig.ShouldContain("--require-publishable");
        releaseConfig.IndexOf("verify-manifest", StringComparison.Ordinal).ShouldBeLessThan(
            releaseConfig.IndexOf("classify-release", StringComparison.Ordinal));
        releaseConfig.IndexOf("classify-release", StringComparison.Ordinal).ShouldBeLessThan(
            releaseConfig.IndexOf("dotnet nuget push", StringComparison.Ordinal));
    }

    [Fact]
    public void GovernanceAutomation_UsesTrustedWriteContextsAndStableMarkers() {
        string root = RepositoryRoot();
        string flaky = File.ReadAllText(Path.Combine(root, ".github/workflows/flaky-test-governance.yml"));
        string nightly = File.ReadAllText(Path.Combine(root, ".github/workflows/quarantine-governance-nightly.yml"));
        string script = File.ReadAllText(Path.Combine(root, ".github/scripts/ci_governance.py"));

        flaky.ShouldContain("workflow_run:");
        flaky.ShouldContain("workflow_dispatch:");
        nightly.ShouldContain("schedule:");
        nightly.ShouldContain("workflow_dispatch:");
        flaky.ShouldContain("contents: write");
        flaky.ShouldContain("issues: write");
        flaky.ShouldContain("pull-requests: write");
        nightly.ShouldContain("contents: write");
        nightly.ShouldContain("issues: write");
        nightly.ShouldContain("pull-requests: write");

        script.ShouldContain("frontcomposer:flaky-test-quarantine");
        script.ShouldContain("frontcomposer:quarantine-reintroduction");
        script.ShouldContain("frontcomposer:ci-diet");
        script.ShouldContain("trusted protected-branch, schedule, or manual context required");
        script.ShouldContain("missing labels");
    }

    [Fact]
    public void GovernanceScript_ProvidesFailClosedEvidenceDecisionsAndSanitization() {
        string root = RepositoryRoot();
        string script = File.ReadAllText(Path.Combine(root, ".github/scripts/ci_governance.py"));

        script.ShouldContain("summarize-quarantine");
        script.ShouldContain("classify-flake");
        script.ShouldContain("reintroduction");
        script.ShouldContain("duration-monitor");
        script.ShouldContain("validate-quarantine-metadata");
        script.ShouldContain("mixed pass/fail evidence");
        script.ShouldContain("Category=Quarantined");
        script.ShouldContain("Category!=Quarantined");
        script.ShouldContain("Bearer [REDACTED]");
        script.ShouldContain("html.escape");
        script.ShouldContain("MAX_SUMMARY_BYTES");
    }

    [Fact]
    public void GovernanceFixtures_CoverRequiredDryRunScenarios() {
        string root = RepositoryRoot();
        string fixtureRoot = Path.Combine(root, "tests/ci-governance/fixtures");
        string[] requiredFixtures = [
            "flake-pass-fail-same-sha.json",
            "flake-pass-fail-outside-window.json",
            "flake-approved-window.json",
            "flake-approved-window-outside.json",
            "reintroduction-valid-pass.json",
            "reintroduction-invalid-reset.json",
            "duration-breach-three-days.json",
            "duration-breach-nonconsecutive.json",
            "hostile-output-redaction.json",
            "ambiguous-source-mapping.json",
            "malformed-evidence.json",
            "permission-untrusted-context.json",
            "concurrent-update-marker.json",
            "reintroduction-batch-mixed.json",
            "contradictory-evidence.json",
            "missing-labels.json",
            "repeat-flake.json",
            "zero-quarantined-summary.json",
        ];

        foreach (string fixture in requiredFixtures) {
            File.Exists(Path.Combine(fixtureRoot, fixture)).ShouldBeTrue($"Missing CI governance fixture: {fixture}");
        }
    }

    [Fact]
    public void QuarantinedTests_RequireIssueOwnerReasonAndReintroductionMetadata() {
        string root = RepositoryRoot();
        foreach (string file in Directory.EnumerateFiles(Path.Combine(root, "tests"), "*.cs", SearchOption.AllDirectories)) {
            string normalized = file.Replace(Path.DirectorySeparatorChar, '/');
            if (normalized.Contains("/bin/", StringComparison.Ordinal) || normalized.Contains("/obj/", StringComparison.Ordinal)) {
                continue;
            }

            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++) {
                if (!lines[i].Contains("[Trait(\"Category\", \"Quarantined\")]", StringComparison.Ordinal)) {
                    continue;
                }

                string context = string.Join('\n', lines.Skip(Math.Max(0, i - 3)).Take(4));
                context.ShouldContain("frontcomposer-quarantine:");
                context.ShouldContain("issue=");
                context.ShouldContain("owner=");
                context.ShouldContain("reason=");
                context.ShouldContain("reintroduction=");
            }
        }
    }

    [Fact]
    public void E2EGovernanceAndStoryTenFourBoundariesRemainExplicit() {
        string root = RepositoryRoot();
        string readme = File.ReadAllText(Path.Combine(root, "tests/README.md"));
        string ci = File.ReadAllText(Path.Combine(root, ".github/workflows/ci.yml"));
        string mutationNightly = File.ReadAllText(Path.Combine(root, ".github/workflows/mutation-property-nightly.yml"));

        readme.ShouldContain("happy path");
        readme.ShouldContain("disconnect/reconnect");
        readme.ShouldContain("rejection rollback");
        mutationNightly.ShouldContain("Validate mutation reports");
        mutationNightly.ShouldContain("Validate property artifacts");
        ci.ShouldNotContain("Category!=Mutation");
        ci.ShouldNotContain("Category!=Property");
    }

    [Fact]
    public void GovernanceScript_ClassifiesFlakeEvidenceFromFixtures() {
        string root = RepositoryRoot();
        string output = Path.Combine(Path.GetTempPath(), $"fc-flake-{Guid.NewGuid():N}.json");

        ProcessResult result = RunGovernance(root, [
            "classify-flake",
            "--evidence", "tests/ci-governance/fixtures/flake-pass-fail-same-sha.json",
            "--output", output,
            "--source-root", ".",
        ]);

        result.ExitCode.ShouldBe(0, result.Error);
        using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(output));
        doc.RootElement.GetProperty("classification").GetString().ShouldBe("flaky");
        doc.RootElement.GetProperty("decision").GetString().ShouldBe("open-or-update-issue-and-pr");
        doc.RootElement.GetProperty("manual_patch_required").GetBoolean().ShouldBeTrue();

        ProcessResult approvedWindow = RunGovernance(root, [
            "classify-flake",
            "--evidence", "tests/ci-governance/fixtures/flake-approved-window.json",
            "--output", output,
        ]);
        approvedWindow.ExitCode.ShouldBe(0, approvedWindow.Error);
        using JsonDocument approvedDoc = JsonDocument.Parse(File.ReadAllText(output));
        approvedDoc.RootElement.GetProperty("classification").GetString().ShouldBe("flaky");

        ProcessResult outsideApprovedWindow = RunGovernance(root, [
            "classify-flake",
            "--evidence", "tests/ci-governance/fixtures/flake-approved-window-outside.json",
            "--output", output,
        ]);
        outsideApprovedWindow.ExitCode.ShouldBe(0, outsideApprovedWindow.Error);
        using JsonDocument outsideApprovedDoc = JsonDocument.Parse(File.ReadAllText(output));
        outsideApprovedDoc.RootElement.GetProperty("classification").GetString().ShouldBe("not-flaky");
    }

    [Fact]
    public void GovernanceScript_RejectsOutsideWindowContradictoryMalformedAndUntrustedEvidence() {
        string root = RepositoryRoot();
        string output = Path.Combine(Path.GetTempPath(), $"fc-flake-{Guid.NewGuid():N}.json");

        ProcessResult outsideWindow = RunGovernance(root, [
            "classify-flake",
            "--evidence", "tests/ci-governance/fixtures/flake-pass-fail-outside-window.json",
            "--output", output,
        ]);
        outsideWindow.ExitCode.ShouldBe(0, outsideWindow.Error);
        using (JsonDocument doc = JsonDocument.Parse(File.ReadAllText(output))) {
            doc.RootElement.GetProperty("classification").GetString().ShouldBe("not-flaky");
        }

        ProcessResult contradictory = RunGovernance(root, [
            "classify-flake",
            "--evidence", "tests/ci-governance/fixtures/contradictory-evidence.json",
        ]);
        contradictory.ExitCode.ShouldNotBe(0);
        contradictory.Error.ShouldContain("one stable test identity");

        ProcessResult malformed = RunGovernance(root, [
            "classify-flake",
            "--evidence", "tests/ci-governance/fixtures/malformed-evidence.json",
        ]);
        malformed.ExitCode.ShouldNotBe(0);
        malformed.Error.ShouldContain("requires identity, passed/failed outcome, and sha");

        ProcessResult untrusted = RunGovernance(root, [
            "classify-flake",
            "--evidence", "tests/ci-governance/fixtures/flake-pass-fail-same-sha.json",
            "--apply",
            "--event-name", "pull_request",
            "--ref", "refs/pull/5/merge",
            "--from-fork", "true",
        ]);
        untrusted.ExitCode.ShouldNotBe(0);
        untrusted.Error.ShouldContain("trusted protected-branch, schedule, or manual context required");
    }

    [Fact]
    public void GovernanceScript_HandlesReintroductionDurationAndRepeatFlakeFixtures() {
        string root = RepositoryRoot();
        string reintroOutput = Path.Combine(Path.GetTempPath(), $"fc-reintro-{Guid.NewGuid():N}.json");
        string stateOutput = Path.Combine(Path.GetTempPath(), $"fc-reintro-state-{Guid.NewGuid():N}.json");

        ProcessResult reintro = RunGovernance(root, [
            "reintroduction",
            "--evidence", "tests/ci-governance/fixtures/reintroduction-valid-pass.json",
            "--state", "tests/ci-governance/quarantine-reintroduction-state.json",
            "--output-state", stateOutput,
            "--output", reintroOutput,
        ]);
        reintro.ExitCode.ShouldBe(0, reintro.Error);
        using (JsonDocument doc = JsonDocument.Parse(File.ReadAllText(reintroOutput))) {
            doc.RootElement.GetProperty("action").GetString().ShouldBe("track");
        }

        string durationOutput = Path.Combine(Path.GetTempPath(), $"fc-duration-{Guid.NewGuid():N}.json");
        ProcessResult duration = RunGovernance(root, [
            "duration-monitor",
            "--evidence", "tests/ci-governance/fixtures/duration-breach-three-days.json",
            "--output", durationOutput,
        ]);
        duration.ExitCode.ShouldBe(0, duration.Error);
        using (JsonDocument doc = JsonDocument.Parse(File.ReadAllText(durationOutput))) {
            doc.RootElement.GetProperty("action").GetString().ShouldBe("open-or-update-ci-diet-issue");
        }

        ProcessResult nonconsecutiveDuration = RunGovernance(root, [
            "duration-monitor",
            "--evidence", "tests/ci-governance/fixtures/duration-breach-nonconsecutive.json",
            "--output", durationOutput,
        ]);
        nonconsecutiveDuration.ExitCode.ShouldBe(0, nonconsecutiveDuration.Error);
        using (JsonDocument doc = JsonDocument.Parse(File.ReadAllText(durationOutput))) {
            doc.RootElement.GetProperty("action").GetString().ShouldBe("record-only");
        }

        string batchOutput = Path.Combine(Path.GetTempPath(), $"fc-reintro-batch-{Guid.NewGuid():N}.json");
        string batchState = Path.Combine(Path.GetTempPath(), $"fc-reintro-batch-state-{Guid.NewGuid():N}.json");
        ProcessResult batchReintro = RunGovernance(root, [
            "reintroduction",
            "--evidence", "tests/ci-governance/fixtures/reintroduction-batch-mixed.json",
            "--output-state", batchState,
            "--output", batchOutput,
        ]);
        batchReintro.ExitCode.ShouldBe(0, batchReintro.Error);
        using (JsonDocument doc = JsonDocument.Parse(File.ReadAllText(batchOutput))) {
            JsonElement items = doc.RootElement.GetProperty("items");
            items.GetArrayLength().ShouldBe(2);
        }
        string stateJson = File.ReadAllText(batchState);
        stateJson.ShouldContain("FirstQuarantined");
        stateJson.ShouldContain("SecondQuarantined");

        string repeatOutput = Path.Combine(Path.GetTempPath(), $"fc-repeat-{Guid.NewGuid():N}.json");
        ProcessResult repeat = RunGovernance(root, [
            "classify-flake",
            "--evidence", "tests/ci-governance/fixtures/repeat-flake.json",
            "--output", repeatOutput,
        ]);
        repeat.ExitCode.ShouldBe(0, repeat.Error);
        string issueBody = JsonDocument.Parse(File.ReadAllText(repeatOutput)).RootElement.GetProperty("issue_body").GetString() ?? string.Empty;
        issueBody.ShouldContain("Repeat flake");
        issueBody.ShouldContain("recurrence count: 2");
    }

    [Fact]
    public void Workflow_DoesNotUsePathFiltersThatCanSkipFrameworkGovernance() {
        // F24 — only the workflow `on:` trigger block can skip governance via path filters.
        // Forbidding any "paths:" substring across the entire workflow false-fires on legitimate
        // step inputs (e.g., `actions/upload-artifact paths:`). Restrict the assertion to the
        // top-level on: block.
        string root = RepositoryRoot();
        string workflow = File.ReadAllText(Path.Combine(root, ".github/workflows/ci.yml"));
        string onBlock = ExtractOnBlock(workflow);

        onBlock.ShouldNotContain("paths-ignore:");
        Regex onPathsRegex = new(@"^\s*paths\s*:", RegexOptions.Multiline);
        onPathsRegex.IsMatch(onBlock).ShouldBeFalse(
            $"the on: trigger block must not use a paths: filter (D16). Found:{Environment.NewLine}{onBlock}");
    }

    private static string ExtractOnBlock(string workflow) {
        // Pull from `^on:` (or `^on:\n`) up to the next top-level YAML key (any line starting
        // with a non-whitespace character followed by ':'). YAML alias `'on':` also tolerated.
        Match onMatch = Regex.Match(workflow, @"^on\s*:[ \t]*\r?\n", RegexOptions.Multiline);
        if (!onMatch.Success) {
            // Top-level on: either is missing or written on one line.
            int singleLineOn = workflow.IndexOf("on:", StringComparison.Ordinal);
            return singleLineOn < 0 ? string.Empty : workflow[singleLineOn..];
        }

        int start = onMatch.Index + onMatch.Length;
        Match nextTopLevel = Regex.Match(workflow[start..], @"^[A-Za-z][\w-]*\s*:", RegexOptions.Multiline);
        return nextTopLevel.Success ? workflow.Substring(start, nextTopLevel.Index) : workflow[start..];
    }

    private static string ExtractNamedStep(string workflow, string name) {
        string quotedNeedle = $"- name: '{name}'";
        string plainNeedle = $"- name: {name}";
        int idx = workflow.IndexOf(quotedNeedle, StringComparison.Ordinal);
        if (idx < 0) {
            idx = workflow.IndexOf(plainNeedle, StringComparison.Ordinal);
        }

        idx.ShouldBeGreaterThanOrEqualTo(0, $"workflow is missing the named step '{name}'.");
        int nextStep = workflow.IndexOf("      - name:", idx + name.Length, StringComparison.Ordinal);
        return nextStep < 0 ? workflow[idx..] : workflow[idx..nextStep];
    }

    private static string RepositoryRoot() {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null) {
            if (File.Exists(Path.Combine(dir.FullName, "Hexalith.FrontComposer.sln"))) {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }

    private static ProcessResult RunGovernance(string root, IReadOnlyList<string> arguments) {
        List<string> fullArguments = [".github/scripts/ci_governance.py", .. arguments];
        return RunPython(root, fullArguments);
    }

    private static ProcessResult RunPython(string root, IReadOnlyList<string> arguments) {
        string executable = OperatingSystem.IsWindows() ? "python" : "python3";
        ProcessStartInfo startInfo = new(executable) {
            WorkingDirectory = root,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };

        foreach (string argument in arguments) {
            startInfo.ArgumentList.Add(argument);
        }

        using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start governance script.");
        Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
        Task<string> errorTask = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(30_000)) {
            process.Kill(entireProcessTree: true);
            process.WaitForExit();
            return new ProcessResult(-1, outputTask.GetAwaiter().GetResult(), "governance script timed out");
        }

        string output = outputTask.GetAwaiter().GetResult();
        string error = errorTask.GetAwaiter().GetResult();
        return new ProcessResult(process.ExitCode, output, error);
    }

    private sealed record ProcessResult(int ExitCode, string Output, string Error);
}
