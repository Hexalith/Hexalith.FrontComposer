using System.Text.RegularExpressions;

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
        quarantineLane.ShouldContain("test-results-quarantine.trx");

        ci.ShouldContain("ci_governance.py summarize-quarantine");
        ci.ShouldContain("artifacts/quarantine/quarantine-summary.md");
        ci.ShouldContain("artifacts/quarantine/quarantine-summary.json");
        ci.ShouldContain("Upload quarantine artifacts");
    }

    [Fact]
    public void Workflows_UseRootLevelSubmodulesOnly() {
        string root = RepositoryRoot();
        foreach (string workflow in Directory.EnumerateFiles(Path.Combine(root, ".github/workflows"), "*.yml")) {
            string text = File.ReadAllText(workflow);
            text.ShouldNotContain("submodules: recursive", Case.Sensitive, Path.GetFileName(workflow));
            text.ShouldNotContain("git submodule update --init --recursive", Case.Sensitive, Path.GetFileName(workflow));
        }
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
            "reintroduction-valid-pass.json",
            "reintroduction-invalid-reset.json",
            "duration-breach-three-days.json",
            "hostile-output-redaction.json",
            "ambiguous-source-mapping.json",
            "malformed-evidence.json",
            "permission-untrusted-context.json",
            "concurrent-update-marker.json",
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
}
