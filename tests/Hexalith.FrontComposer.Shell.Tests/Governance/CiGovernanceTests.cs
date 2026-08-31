using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Governance;

[Trait("Category", "Governance")]
public sealed class CiGovernanceTests {
    private static readonly string[] ApprovedStrykerTargetRoots = [
        "src/Hexalith.FrontComposer.SourceTools/Parsing",
        "src/Hexalith.FrontComposer.SourceTools/Transforms",
    ];
    private static readonly string[] StrykerTriageActions = [
        "kill-test-added",
        "equivalent-accepted",
        "deferred-with-owner",
        "blocking",
    ];

    /// <summary>
    /// The isolated heavy lane is selected by trait but authenticated by a hard-coded identity
    /// allowlist in `quality.yml`. Pin both directions here so a renamed, added, or removed
    /// `GovernanceBuild` fact fails at test time instead of at the CI evidence step.
    /// </summary>
    [Fact]
    public void GovernanceBuildTraitedFacts_MatchTheWorkflowIdentityAllowlist() {
        string root = RepositoryRoot();
        string quality = File.ReadAllText(Path.Combine(root, ".github/workflows/quality.yml"));

        string[] allowlisted = [.. Regex.Matches(quality, @"--expected-test\s+(?<identity>\S+)")
            .Select(match => match.Groups["identity"].Value)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)];

        string[] traited = [.. typeof(AnalyzerPolicyGovernanceTests).Assembly.GetTypes()
            .SelectMany(static type => type.GetMethods())
            .Where(static method => method.GetCustomAttributesData().Any(static attribute =>
                attribute.AttributeType.FullName == "Xunit.TraitAttribute"
                && attribute.ConstructorArguments.Count == 2
                && (string?)attribute.ConstructorArguments[0].Value == "Category"
                && (string?)attribute.ConstructorArguments[1].Value == "GovernanceBuild"))
            .Select(static method => $"{method.DeclaringType!.FullName}.{method.Name}")
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)];

        traited.ShouldNotBeEmpty("the isolated heavy lane must select at least one fact");
        traited.ShouldBe(
            allowlisted,
            "every GovernanceBuild fact must be named by a quality.yml --expected-test allowlist entry, and vice versa");
    }

    [Fact]
    public void CommitlintJob_BlocksPrTitlesAndCommitMessagesUsedBySemanticRelease() {
        // REL-2 (2026-07-13): commitlint moved out of the inline ci.yml job into the dedicated
        // commitlint.yml reusable caller (Tenants parity). semantic-release derives versions from
        // commit messages and this repository pushes to main directly, so the gate MUST run on
        // both pull requests and pushes to main; the shared reusable owns the actual PR-title /
        // PR-commit-range / last-main-commit validation.
        string root = RepositoryRoot();
        string workflow = File.ReadAllText(Path.Combine(root, ".github/workflows/commitlint.yml"));

        workflow.ShouldContain("uses: Hexalith/Hexalith.Builds/.github/workflows/commitlint.yml@main");
        workflow.ShouldNotContain("continue-on-error: true");

        string onBlock = ExtractOnBlock(workflow);
        onBlock.ShouldContain("pull_request:");
        onBlock.ShouldContain("push:");
        onBlock.ShouldContain("branches: [main]");
    }

    [Fact]
    public void AgentEntryPoints_CommitMessageGuidance_IsSynchronizedAndFailClosed() {
        string root = RepositoryRoot();
        string agents = File.ReadAllText(Path.Combine(root, "AGENTS.md")).ReplaceLineEndings("\n");
        string claude = File.ReadAllText(Path.Combine(root, "CLAUDE.md")).ReplaceLineEndings("\n");
        string copilot = File.ReadAllText(Path.Combine(root, ".github/copilot-instructions.md")).ReplaceLineEndings("\n");

        claude.ShouldBe(agents);
        copilot.ShouldBe(agents);

        string expectedGitAndSubmodulesGuidance = """
            ## Git and Submodules

            - Before Git work, inspect the current repository's branch, working tree,
              remotes, and recent history.
            - Any commit message an assistant creates, suggests, or uses, including Claude,
              Codex, Cursor, GitHub Copilot, and supported Visual Studio Copilot commit-message
              generation, must follow Conventional Commits and satisfy both the owning
              repository's effective commitlint policy and its tracked Git guidance.
            - Before presenting or using a message, an assistant capable of running repository tooling must validate the exact full candidate with the owning repository's pinned commitlint CLI and preserve successful validation evidence.
              If validation rejects the candidate, report the rule violations, revise the message, and revalidate it; if the validator cannot run, report the exact command and blocker and do not present or use the candidate until validation succeeds. Never bypass commit validation.
            - When repository instructions are enabled, Visual Studio 2026 version 18.6 and later reads `.github/copilot-instructions.md`; older, disabled, or unsupported cases are not controlled by this file.
              These instructions guide generation but cannot execute commitlint or guarantee compliance; the installed commit-message hook and blocking CI commitlint gate remain enforcement layers.
            - In an umbrella workspace, initialize or update only dependencies declared by
              the top-level workspace `.gitmodules` file.
            - Never initialize or update a submodule's nested submodules unless the user
              explicitly requests that nested work. Never use recursive or remote submodule
              updates by default.
            - If nested submodules were initialized accidentally, deinitialize them before
              continuing.
            """.ReplaceLineEndings("\n");
        int guidanceStart = agents.IndexOf("## Git and Submodules\n", StringComparison.Ordinal);
        int guidanceEnd = agents.IndexOf("## Shared Entry Points\n", StringComparison.Ordinal);

        guidanceStart.ShouldBeGreaterThanOrEqualTo(0);
        guidanceEnd.ShouldBeGreaterThan(guidanceStart);
        guidanceStart.ShouldBe(agents.LastIndexOf("## Git and Submodules\n", StringComparison.Ordinal));
        guidanceEnd.ShouldBe(agents.LastIndexOf("## Shared Entry Points\n", StringComparison.Ordinal));
        agents[guidanceStart..guidanceEnd].TrimEnd().ShouldBe(expectedGitAndSubmodulesGuidance);
    }

    [Fact]
    public void BuildAndTestJob_IsBlockingAndHasGovernanceTelemetryGate() {
        // REL-2 (2026-07-13): the FrontComposer-only Gate 2b governance lane moved from the
        // inline ci.yml build-and-test job into the supplemental quality.yml (ci.yml now delegates
        // to the shared reusable domain-ci.yml). quality.yml is CI-authoritative for this gate.
        string root = RepositoryRoot();
        string workflow = File.ReadAllText(Path.Combine(root, ".github/workflows/quality.yml"));

        string buildJob = workflow[workflow.IndexOf("  build-and-test:", StringComparison.Ordinal)..];
        string buildJobHeader = buildJob[..buildJob.IndexOf("    steps:", StringComparison.Ordinal)];

        buildJobHeader.ShouldNotContain("continue-on-error: true");
        workflow.ShouldContain("Gate 2b: Infrastructure governance and telemetry contracts");
        workflow.ShouldContain("Category=Governance");
        workflow.ShouldContain("--results-directory ./TestResults/governance --report-xunit-trx");
        workflow.ShouldContain("Gate 2b: Analyzer governance build proofs");
        workflow.ShouldContain("Category=GovernanceBuild");
        workflow.ShouldContain("--results-directory ./TestResults/governance-build --report-xunit-trx");
    }

    [Fact]
    public void Gate2bGovernanceStep_IsNotMarkedAdvisory() {
        // F31 — verify the Gate 2b step itself never carries `continue-on-error: true`. The
        // job-header check above only proves the job is not advisory at job scope; a future
        // edit that marked the governance STEP advisory would slip through. Find the named
        // step body and assert its block does not contain a step-level continue-on-error flag.
        // REL-2 (2026-07-13): Gate 2b lives in the supplemental quality.yml after the CI migration.
        string root = RepositoryRoot();
        string workflow = File.ReadAllText(Path.Combine(root, ".github/workflows/quality.yml"));
        string gateName = "Gate 2b: Infrastructure governance and telemetry contracts";
        int idx = workflow.IndexOf(gateName, StringComparison.Ordinal);
        idx.ShouldBeGreaterThanOrEqualTo(0, $"workflow is missing the named step '{gateName}'.");

        // A step block ends at the next `      - name:` (six-space indented dash) or end of file.
        int nextStep = workflow.IndexOf("      - name:", idx + gateName.Length, StringComparison.Ordinal);
        string stepBody = nextStep < 0 ? workflow[idx..] : workflow[idx..nextStep];

        stepBody.ShouldNotContain("continue-on-error: true");

        // Story 11.17d code review (2026-08-01 / 2026-08-02): the two catalog-compatibility
        // facts in this gate shell out to eng/dependency_graph.py, whose own semantic rules
        // -- the required-property and required-package checks -- are covered only by
        // tests/eng/test_dependency_graph.py. That suite ran in no workflow, so the entire
        // required-property loop could be deleted with every lane still green. Pin the named
        // Gate 2b step itself (comment-stripped, step-scoped) and refuse an advisory posture
        // on that block — a raw full-file ShouldContain would stay green on a YAML comment
        // or a sibling job while the blocking step was gone or marked continue-on-error.
        string stripped = StripYamlComments(workflow);
        string dependencyGraphStep = ExtractNamedStep(
            stripped,
            "Gate 2b: Dependency graph semantic policy tests");
        dependencyGraphStep.ShouldContain("python3 -m unittest tests/eng/test_dependency_graph.py");
        dependencyGraphStep.ShouldContain("python3 -m unittest tests/eng/test_dependency_handoff.py");
        dependencyGraphStep.ShouldContain("python3 -m unittest tests/eng/test_release_contract.py");
        dependencyGraphStep.ShouldContain("python3 -m unittest tests/eng/test_release_evidence_v2.py");
        dependencyGraphStep.ShouldNotContain("continue-on-error: true");
    }

    [Fact]
    public void StoryArtifactValidatorGate_IsBlockingAndExact() {
        string root = RepositoryRoot();
        string quality = StripYamlComments(File.ReadAllText(Path.Combine(root, ".github/workflows/quality.yml")));
        int jobStart = quality.IndexOf("  build-and-test:", StringComparison.Ordinal);
        jobStart.ShouldBeGreaterThanOrEqualTo(0);
        int jobEnd = quality.IndexOf("\n  accessibility-visual:", jobStart, StringComparison.Ordinal);
        jobEnd.ShouldBeGreaterThan(jobStart);
        string blockingJob = quality[jobStart..jobEnd];
        int stepsStart = blockingJob.IndexOf("\n    steps:", StringComparison.Ordinal);
        stepsStart.ShouldBeGreaterThanOrEqualTo(0);
        string jobContract = blockingJob[..stepsStart];
        jobContract.ShouldNotContain("continue-on-error: true");
        jobContract.ShouldNotContain("if:");
        string blockingStep = ExtractNamedStep(blockingJob, "Gate 2b: Story artifact validator tests");
        Regex.Count(
                blockingStep,
                @"^[ \t]*run:[ \t]*python3 -m unittest eng\.tests\.test_validate_story_artifacts[ \t]*\r?$",
                RegexOptions.Multiline | RegexOptions.CultureInvariant)
            .ShouldBe(1, "the blocking validator command must remain exact and unwrapped.");
        blockingStep.ShouldNotContain("continue-on-error: true");
        blockingStep.ShouldNotContain("if:");
        blockingStep.ShouldNotContain("|| true");
        blockingStep.ShouldNotContain("set +e");
        Regex.Count(quality, "Gate 2b: Story artifact validator tests", RegexOptions.CultureInvariant)
            .ShouldBe(1, "the authoritative validator test step must not be shadowed by an advisory duplicate.");

        // "Blocking" was never actually pinned: every assertion above constrains the job
        // and the step, but none required the workflow to run on a pull request at all.
        // Dropping the trigger would leave this fact green while the gate stopped gating.
        int triggerStart = quality.IndexOf("\non:", StringComparison.Ordinal);
        triggerStart.ShouldBeGreaterThanOrEqualTo(0, "quality.yml must declare workflow triggers.");
        int triggerEnd = quality.IndexOf("\nconcurrency:", triggerStart, StringComparison.Ordinal);
        triggerEnd.ShouldBeGreaterThan(triggerStart, "quality.yml must keep its concurrency block after the triggers.");
        string triggers = quality[triggerStart..triggerEnd];
        // Without `pull_request` Gate 2b never blocks a merge; without `push` it never
        // covers the merged head.
        triggers.ShouldContain("pull_request:");
        triggers.ShouldContain("push:");
    }

    [Fact]
    public void BlockingTestLanes_ExcludeQuarantinedTestsWithoutSkippingGovernance() {
        // REL-2 (2026-07-13): the trait-filtered test lanes moved from the inline ci.yml into the
        // supplemental quality.yml; the release path no longer re-runs tests (the reusable
        // domain-release.yml publishes and CI already gated the head). REL-3 (2026-07-18): the
        // release-lane test run moved from the supplemental evidence workflow into the
        // pre-publication orchestrator, which runs the release tests (Gate 3a filter)
        // against the exact candidates before any publication side effect.
        string root = RepositoryRoot();
        string quality = File.ReadAllText(Path.Combine(root, ".github/workflows/quality.yml"));
        string orchestrator = File.ReadAllText(Path.Combine(root, "eng/release_prepublish.py"));

        string defaultLane = ExtractNamedStep(quality, "Gate 3a: Unit + bUnit (default lane)");
        foreach (string trait in new[] { "GovernanceBuild", "Performance", "e2e-palette", "NightlyProperty", "Quarantined" }) {
            defaultLane.ShouldContain($"--filter-not-trait \"Category={trait}\"");
        }
        defaultLane.ShouldContain("--report-xunit-trx");
        defaultLane.ShouldContain("--coverage --coverage-output-format cobertura");
        defaultLane.ShouldContain("--results-directory ./TestResults/default");
        defaultLane.ShouldNotContain("--report-xunit-trx-filename");
        defaultLane.ShouldNotContain("--coverage-output ");
        defaultLane.ShouldNotContain("continue-on-error: true");

        string governanceLane = ExtractNamedStep(quality, "Gate 2b: Infrastructure governance and telemetry contracts");
        governanceLane.ShouldContain("--filter-trait \"Category=Governance\"");
        governanceLane.ShouldContain("--filter-not-trait \"Category=GovernanceBuild\"");
        governanceLane.ShouldContain("--ignore-exit-code 8");
        governanceLane.ShouldContain("--results-directory ./TestResults/governance --report-xunit-trx");
        governanceLane.ShouldNotContain("--report-xunit-trx-filename");
        governanceLane.ShouldNotContain("--filter-not-trait \"Category=Quarantined\"");
        governanceLane.ShouldNotContain("continue-on-error: true");

        string analyzerBuildLane = ExtractNamedStep(quality, "Gate 2b: Analyzer governance build proofs");
        Regex.Count(
            quality,
            Regex.Escape("- name: 'Gate 2b: Analyzer governance build proofs'"))
            .ShouldBe(1);
        analyzerBuildLane.ShouldContain("dotnet test --project tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj");
        analyzerBuildLane.ShouldContain("--configuration Release --no-build");
        analyzerBuildLane.ShouldContain("--filter-trait \"Category=GovernanceBuild\"");
        analyzerBuildLane.ShouldContain("--results-directory ./TestResults/governance-build --report-xunit-trx");
        analyzerBuildLane.ShouldNotContain("--ignore-exit-code");
        analyzerBuildLane.ShouldNotContain("continue-on-error: true");
        analyzerBuildLane.ShouldNotContain("\n        if:");

        string analyzerBuildEvidence = ExtractNamedStep(
            quality,
            "Gate 2b: Verify analyzer governance build MTP evidence");
        Regex.Count(
            quality,
            Regex.Escape("- name: 'Gate 2b: Verify analyzer governance build MTP evidence'"))
            .ShouldBe(1);
        analyzerBuildEvidence.ShouldContain("ci_governance.py validate-mtp-evidence");
        analyzerBuildEvidence.ShouldContain("--results-dir ./TestResults/governance-build");
        analyzerBuildEvidence.ShouldContain("--expected-trx-files 1");
        analyzerBuildEvidence.ShouldContain("--require-tests");
        analyzerBuildEvidence.ShouldContain(
            "--expected-test Hexalith.FrontComposer.Shell.Tests.Governance.AnalyzerPolicyGovernanceTests.AnalyzerPolicy_ActivatedReleaseBuild_MatchesForcedRecommendedCandidate");
        analyzerBuildEvidence.ShouldContain(
            "--expected-test Hexalith.FrontComposer.Shell.Tests.Governance.AnalyzerPolicyGovernanceTests.AnalyzerPolicy_Story1122RecordedProjects_RemainRecommendedClean");
        analyzerBuildEvidence.ShouldNotContain("continue-on-error: true");
        analyzerBuildEvidence.ShouldNotContain("\n        if:");

        // Pin the executable MTP argument pairs in the orchestrator's dotnet-test
        // invocation, not bare trait strings that comments could satisfy.
        foreach (string trait in new[] { "GovernanceBuild", "Performance", "e2e-palette", "NightlyProperty", "Quarantined" }) {
            orchestrator.ShouldContain($"\"--filter-not-trait\", \"Category={trait}\",");
        }
        orchestrator.ShouldContain("\"--report-xunit-trx\",");
        orchestrator.ShouldNotContain("\"--filter\",");
        orchestrator.ShouldNotContain("\"--logger\",");

        string performanceLane = ExtractNamedStep(quality, "Gate 3c: Performance bench (Performance lane)");
        performanceLane.ShouldContain("continue-on-error: true");
        performanceLane.ShouldContain("--filter-trait \"Category=Performance\"");
        performanceLane.ShouldContain("--ignore-exit-code 8");
        performanceLane.ShouldContain("--results-directory ./TestResults/performance --report-xunit-trx");
        performanceLane.ShouldNotContain("--report-xunit-trx-filename");

        string paletteLane = ExtractNamedStep(quality, "Gate 3b: Palette E2E (e2e-palette lane)");
        paletteLane.ShouldContain("--filter-trait \"Category=e2e-palette\"");
        paletteLane.ShouldContain("--ignore-exit-code 8");
        paletteLane.ShouldContain("--results-directory ./TestResults/e2e-palette --report-xunit-trx");
        paletteLane.ShouldNotContain("--report-xunit-trx-filename");
    }

    [Fact]
    public void QuarantineLane_IsWarningOnlyAndPublishesBoundedEvidence() {
        // REL-2 (2026-07-13): the advisory quarantine lane + telemetry moved to quality.yml.
        string root = RepositoryRoot();
        string ci = File.ReadAllText(Path.Combine(root, ".github/workflows/quality.yml"));

        string quarantineLane = ExtractNamedStep(ci, "Gate 3d: Quarantined tests (warning-only)");
        quarantineLane.ShouldContain("continue-on-error: true");
        quarantineLane.ShouldContain("--filter-trait \"Category=Quarantined\"");
        quarantineLane.ShouldContain("--ignore-exit-code 8");
        quarantineLane.ShouldContain("--results-directory ./TestResults/quarantine --report-xunit-trx");
        quarantineLane.ShouldNotContain("--report-xunit-trx-filename");

        ci.ShouldContain("ci_governance.py summarize-quarantine");
        ci.ShouldContain("artifacts/quarantine/quarantine-summary.md");
        ci.ShouldContain("artifacts/quarantine/quarantine-summary.json");
        ci.ShouldContain("Upload quarantine artifacts");
    }

    [Fact]
    public void QualityWorkflow_PinsContractPactStaleAndArtifactGates() {
        // REL-2 code-review P3 (2026-07-13): Gate 2c (Contract pacts + contract-artifact validation +
        // the stale-pact-diff guard) relocated from the inline ci.yml into the supplemental
        // quality.yml. Pin it at its new home so a future edit cannot silently drop the sole
        // enforcement that drifted EventStore consumer pacts are caught (AC8 / PRD NFR-11).
        string root = RepositoryRoot();
        string quality = File.ReadAllText(Path.Combine(root, ".github/workflows/quality.yml"));

        string pactLane = ExtractNamedStep(quality, "Gate 2c: Contract pacts");
        pactLane.ShouldContain("--filter-trait \"Category=Contract\"");
        pactLane.ShouldContain("--report-xunit-trx-filename test-results-contract.trx");
        pactLane.ShouldNotContain("continue-on-error: true");

        quality.ShouldContain("Gate 2c: Validate contract artifacts");
        quality.ShouldContain("./eng/validate-contract-artifacts.ps1");

        string stalePactGuard = ExtractNamedStep(quality, "Gate 2c: Fail on stale pact diff");
        stalePactGuard.ShouldContain("git diff --exit-code -- tests/Hexalith.FrontComposer.Shell.Tests/Pact");
        stalePactGuard.ShouldContain("exit 1");
        stalePactGuard.ShouldNotContain("continue-on-error: true");
    }

    [Fact]
    public void QualityWorkflow_PinsAccessibilityVisualGate() {
        // REL-2 code-review P3 (2026-07-13): the Playwright a11y/visual job (the sole automated
        // accessibility + visual-regression gate) relocated from ci.yml into quality.yml. Pin the job
        // and its non-advisory test step so it cannot be silently dropped or made advisory (AC8 / PRD
        // NFR-11 requires e2e a11y/visual for the changed surface).
        // Windows MAX_PATH (2026-08-05): accessibility-visual must initialize only
        // references/Hexalith.Builds — never EventStore evidence trees or a bare/full submodule init.
        string root = RepositoryRoot();
        string quality = File.ReadAllText(Path.Combine(root, ".github/workflows/quality.yml"));
        int a11yJobStart = quality.LastIndexOf("  accessibility-visual:", StringComparison.Ordinal);
        a11yJobStart.ShouldBeGreaterThanOrEqualTo(0);
        string a11yJob = quality[a11yJobStart..];

        quality.ShouldContain("accessibility-visual:");
        quality.ShouldContain("npm run validate:visual-governance");
        quality.ShouldContain("npm run validate:a11y-artifacts");

        string a11yStep = ExtractNamedStep(quality, "Run accessibility, keyboard, media, zoom, and visual specimen gate");
        a11yStep.ShouldContain("npm run test:a11y");
        a11yStep.ShouldNotContain("continue-on-error: true");

        int checkoutStart = a11yJob.IndexOf("      - uses: actions/checkout@", StringComparison.Ordinal);
        checkoutStart.ShouldBeGreaterThanOrEqualTo(0);
        int checkoutEnd = a11yJob.IndexOf("\n      - ", checkoutStart + 1, StringComparison.Ordinal);
        checkoutEnd.ShouldBeGreaterThan(checkoutStart);
        string checkoutStep = a11yJob[checkoutStart..checkoutEnd];
        checkoutStep.ShouldContain("GIT_CONFIG_COUNT: 1");
        checkoutStep.ShouldContain("GIT_CONFIG_KEY_0: core.longpaths");
        checkoutStep.ShouldContain("GIT_CONFIG_VALUE_0: 'true'");
        checkoutStep.ShouldNotContain("git config --global");
        a11yJob.Replace(checkoutStep, string.Empty, StringComparison.Ordinal).ShouldNotContain("core.longpaths");

        string initializeBuildSubmodules = ExtractNamedStep(a11yJob, "Initialize build submodules");
        a11yJob.ShouldContain("fetch-depth: 0");
        initializeBuildSubmodules.ShouldContain("shell: bash");
        initializeBuildSubmodules.ShouldContain(
            "git -c submodule.recurse=false submodule update --init references/Hexalith.Builds");
        initializeBuildSubmodules.ShouldNotContain("initialize-build");
        initializeBuildSubmodules.ShouldNotContain("references/Hexalith.EventStore");
        initializeBuildSubmodules
            .Replace("submodule update --init references/Hexalith.Builds", string.Empty, StringComparison.Ordinal)
            .ShouldNotContain("submodule update --init");
        initializeBuildSubmodules.ShouldContain("GIT_CONFIG_COUNT: 1");
        initializeBuildSubmodules.ShouldContain("GIT_CONFIG_KEY_0: core.symlinks");
        initializeBuildSubmodules.ShouldContain("GIT_CONFIG_VALUE_0: 'false'");
        initializeBuildSubmodules.ShouldNotContain("git config --global");

        foreach (string stepName in new[] {
            "Typecheck Playwright accessibility lane",
            "Run FC-NIP contract guards (browserless)",
            "Validate visual baseline governance",
            "Validate accessibility artifacts",
        }) {
            ExtractNamedStep(a11yJob, stepName).ShouldNotContain("continue-on-error: true");
        }
    }

    [Fact]
    public void PlaywrightBrowserlessScripts_UseCrossPlatformEnvironmentAssignment() {
        string root = RepositoryRoot();
        using JsonDocument package = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "tests/e2e/package.json")));
        using JsonDocument packageLock = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "tests/e2e/package-lock.json")));
        JsonElement scripts = package.RootElement.GetProperty("scripts");
        string[] expectedBrowserlessScriptNames = [
            "test:fc-level3",
            "test:fc-level4",
            "test:fc-a11y-diagnostics",
            "test:fc-diagnostics",
            "test:fc-nip",
            "test:epic-9",
            "test:story-10-2",
            "test:story-10-3",
            "test:story-10-4",
        ];
        string[] actualBrowserlessScriptNames = scripts.EnumerateObject()
            .Where(property => property.Value.GetString()?.Contains("PLAYWRIGHT_SKIP_WEBSERVER=1", StringComparison.Ordinal) is true)
            .Select(property => property.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();

        actualBrowserlessScriptNames.ShouldBe(expectedBrowserlessScriptNames.Order(StringComparer.Ordinal));
        foreach (string scriptName in expectedBrowserlessScriptNames) {
            scripts.GetProperty(scriptName).GetString().ShouldStartWith(
                "cross-env PLAYWRIGHT_SKIP_WEBSERVER=1 ");
        }
        package.RootElement.GetProperty("devDependencies").GetProperty("cross-env").GetString()
            .ShouldBe("^10.1.0");
        packageLock.RootElement.GetProperty("packages").GetProperty(string.Empty)
            .GetProperty("devDependencies").GetProperty("cross-env").GetString()
            .ShouldBe("^10.1.0");
        packageLock.RootElement.GetProperty("packages").TryGetProperty("node_modules/cross-env", out _)
            .ShouldBeTrue("the regenerated lockfile must pin the cross-platform helper.");
    }

    [Fact]
    public void MicrosoftTestingPlatformEntrypoints_UseNativeFiltersReportsAndCoverage() {
        string root = RepositoryRoot();
        string quality = File.ReadAllText(Path.Combine(root, ".github/workflows/quality.yml"));
        string quarantineNightly = File.ReadAllText(Path.Combine(
            root,
            ".github/workflows/quarantine-governance-nightly.yml"));
        string lifecycle = File.ReadAllText(Path.Combine(root, "eng/run-lifecycle-property-suite.ps1"));
        string prepublish = File.ReadAllText(Path.Combine(root, "eng/release_prepublish.py"));

        quality.ShouldContain("--filter-trait");
        quality.ShouldContain("--filter-not-trait");
        quality.ShouldContain("--report-xunit-trx");
        quality.ShouldContain("--coverage --coverage-output-format cobertura");
        quality.ShouldNotContain("--collect:\"XPlat Code Coverage\"");
        quality.ShouldNotContain("--logger \"trx;");

        quarantineNightly.ShouldContain("--filter-trait \"Category=Quarantined\"");
        quarantineNightly.ShouldContain("--ignore-exit-code 8");
        quarantineNightly.ShouldContain("--results-directory ./TestResults/quarantine --report-xunit-trx");
        quarantineNightly.ShouldNotContain("--report-xunit-trx-filename");
        quarantineNightly.ShouldNotContain("--logger \"trx;");

        lifecycle.ShouldContain("--filter-trait");
        lifecycle.ShouldContain("--filter-not-trait");
        lifecycle.ShouldContain("--report-xunit-trx-filename lifecycle-property.trx");
        lifecycle.ShouldNotContain("--logger \"trx;");

        prepublish.ShouldContain("\"--filter-not-trait\"");
        prepublish.ShouldContain("\"--report-xunit-trx\"");
        prepublish.ShouldNotContain("\"--logger\"");

        string governanceEvidence = ExtractNamedStep(quality, "Gate 2b: Verify Governance MTP evidence");
        governanceEvidence.ShouldContain("ci_governance.py validate-mtp-evidence");
        governanceEvidence.ShouldContain("--results-dir ./TestResults/governance");
        governanceEvidence.ShouldContain("--minimum-trx-files 1");
        governanceEvidence.ShouldContain("--require-tests");
        governanceEvidence.ShouldNotContain("continue-on-error: true");

        string analyzerBuildEvidence = ExtractNamedStep(
            quality,
            "Gate 2b: Verify analyzer governance build MTP evidence");
        analyzerBuildEvidence.ShouldContain("ci_governance.py validate-mtp-evidence");
        analyzerBuildEvidence.ShouldContain("--results-dir ./TestResults/governance-build");
        analyzerBuildEvidence.ShouldContain("--expected-trx-files 1");
        analyzerBuildEvidence.ShouldContain("--require-tests");
        analyzerBuildEvidence.ShouldContain(
            "--expected-test Hexalith.FrontComposer.Shell.Tests.Governance.AnalyzerPolicyGovernanceTests.AnalyzerPolicy_ActivatedReleaseBuild_MatchesForcedRecommendedCandidate");
        analyzerBuildEvidence.ShouldContain(
            "--expected-test Hexalith.FrontComposer.Shell.Tests.Governance.AnalyzerPolicyGovernanceTests.AnalyzerPolicy_Story1122RecordedProjects_RemainRecommendedClean");
        analyzerBuildEvidence.ShouldNotContain("continue-on-error: true");

        string defaultEvidence = ExtractNamedStep(quality, "Gate 3a: Verify default MTP evidence");
        defaultEvidence.ShouldContain("--results-dir ./TestResults/default");
        defaultEvidence.ShouldContain("--expected-trx-files 8");
        defaultEvidence.ShouldContain("--require-tests");
        defaultEvidence.ShouldContain("--require-distinct-modules");
        defaultEvidence.ShouldContain("--coverage-dir ./TestResults/default");
        defaultEvidence.ShouldContain("--expected-coverage-files 8");
        defaultEvidence.ShouldNotContain("continue-on-error: true");

        string trxUpload = ExtractNamedStep(quality, "Upload test results");
        trxUpload.ShouldContain("if: always()");
        trxUpload.ShouldContain("TestResults/**/*.trx");
        trxUpload.ShouldContain("retention-days: 14");

        string coverageSummary = ExtractNamedStep(quality, "Coverage Summary");
        coverageSummary.ShouldContain("raise SystemExit('No coverage files found.')");
        coverageSummary.ShouldContain("report contains no measured lines");
        coverageSummary.ShouldContain("Could not parse coverage report");
        coverageSummary.ShouldContain("f'Report {report_number}'");
        coverageSummary.ShouldNotContain("Project_{project_guid}");
    }

    [Fact]
    public void QualityWorkflow_PinsCliSmokeAndDocsGates() {
        // REL-2 code-review P3 (2026-07-13): Gate 2a (CLI tool package smoke) and Gate 2d (DocFX docs
        // validation) relocated from ci.yml into quality.yml. Pin both so neither is silently dropped
        // (AC8 / PRD NFR-11).
        string root = RepositoryRoot();
        string quality = File.ReadAllText(Path.Combine(root, ".github/workflows/quality.yml"));

        quality.ShouldContain("Gate 2a: CLI Tool Package Smoke");

        string docsGate = ExtractNamedStep(quality, "Gate 2d: Docs Validation");
        docsGate.ShouldContain("./eng/validate-docs.ps1");
        docsGate.ShouldNotContain("continue-on-error: true");
    }

    [Fact]
    public void CiWorkflow_DelegatesToReusableDomainCiWithConsumerValidation() {
        // REL-2 code-review P3 (2026-07-13): AC2/AC6 — the primary CI job must delegate to the shared
        // reusable domain-ci.yml with FrontComposer's trait-clean unit-test-projects and
        // run-consumer-validation: true (the ONLY trigger for the FR24 scripts/ pack+validate+consumer
        // trio). Pin the delegation so a silent removal of the flag or a dropped project fails a test.
        string root = RepositoryRoot();
        string ci = File.ReadAllText(Path.Combine(root, ".github/workflows/ci.yml"));

        Match domainCiPin = Regex.Match(
            ci,
            @"uses: Hexalith/Hexalith\.Builds/\.github/workflows/domain-ci\.yml@(?<sha>[0-9a-f]{40})\b");
        domainCiPin.Success.ShouldBeTrue(
            "ci.yml must pin domain-ci.yml to an exact 40-hex lowercase Builds commit SHA (never @main).");
        ci.ShouldNotContain("domain-ci.yml@main");
        ci.ShouldContain("solution: Hexalith.FrontComposer.slnx");
        ci.ShouldContain("test-platform: microsoft-testing-platform");
        ci.ShouldContain("run-consumer-validation: true");
        ci.ShouldContain("unit-test-projects:");
        ci.ShouldContain("tests/Hexalith.FrontComposer.Cli.Tests");
        ci.ShouldContain("tests/Hexalith.FrontComposer.Testing.Tests");

        using JsonDocument globalJson = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "global.json")));
        globalJson.RootElement.GetProperty("test").GetProperty("runner").GetString()
            .ShouldBe("Microsoft.Testing.Platform");
        string testProps = File.ReadAllText(Path.Combine(root, "tests/Directory.Build.props"));
        testProps.ShouldContain("Microsoft.Testing.Extensions.CodeCoverage");
    }

    [Fact]
    public void DependencyGovernance_UsesExactRevisionsAndPolicyOwnedStaticModuleCommands() {
        string root = RepositoryRoot();
        string ci = StripYamlComments(File.ReadAllText(Path.Combine(root, ".github/workflows/ci.yml")));
        string quality = StripYamlComments(File.ReadAllText(Path.Combine(root, ".github/workflows/quality.yml")));
        string architecture = File.ReadAllText(Path.Combine(
            root,
            "_bmad-output",
            "planning-artifacts",
            "architecture",
            "architecture-gov-1-2026-07-19",
            "ARCHITECTURE-SPINE.md"));

        ci.ShouldContain("dependency-governance:");
        ci.ShouldContain("needs: ci");
        ci.ShouldContain("PULL_REQUEST_BASE: ${{ github.event.pull_request.base.sha }}");
        ci.ShouldContain("PUSH_BEFORE: ${{ github.event.before }}");
        ci.ShouldContain("CANDIDATE: ${{ github.sha }}");
        ci.ShouldContain("eng/dependency_graph.py acquire");
        ci.ShouldContain("--destination \"$object_root\"");
        ci.ShouldContain("eng/dependency_graph.py --root \"$object_root\" diff");
        ci.ShouldContain("payload.get(\"error\")");
        ci.ShouldContain("eng/dependency_graph.py --root \"${{ steps.dependency-diff.outputs.object-root }}\" run-affected");
        ci.ShouldContain("dependency-graph-evidence-${{ github.run_id }}-${{ github.run_attempt }}");
        ci.ShouldContain("dependency_handoff.py --root \"$OBJECT_ROOT\" draft-evaluator");
        ci.ShouldContain("dependency_handoff.py --root \"$OBJECT_ROOT\" create-ci");
        ci.ShouldContain("dependency-release-handoff-${{ github.run_id }}-${{ github.run_attempt }}");
        ci.ShouldNotContain("submodule update --init --recursive");
        ci.ShouldNotContain("eval ");

        string helperStep = ExtractNamedStep(quality, "Gate 2b: Dependency graph semantic policy tests");
        helperStep.ShouldContain("tests/eng/test_dependency_graph.py");
        helperStep.ShouldContain("tests/eng/test_workflow_source_closure.py");
        helperStep.ShouldContain("tests/eng/test_dependency_handoff.py");
        quality.ShouldContain("fetch-depth: 0");

        using JsonDocument policyDocument = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(root, "eng/dependency-graph-policy.json")));
        JsonElement policy = policyDocument.RootElement;
        policy.GetProperty("schema").GetString().ShouldBe("hexalith.dependency-graph-policy.v1");
        JsonElement registry = policy.GetProperty("module_build_registry");
        registry.EnumerateObject().Count().ShouldBe(9);
        foreach (JsonProperty module in registry.EnumerateObject()) {
            JsonElement row = module.Value;
            string disposition = row.GetProperty("disposition").GetString().ShouldNotBeNull();
            if (disposition == "evidence-only") {
                row.GetProperty("restore_argv").ValueKind.ShouldBe(JsonValueKind.Null);
                row.GetProperty("build_argv").ValueKind.ShouldBe(JsonValueKind.Null);
            }
            else {
                row.GetProperty("restore_argv")[0].GetString().ShouldBe("dotnet");
                row.GetProperty("restore_argv")[1].GetString().ShouldBe("restore");
                row.GetProperty("build_argv")[0].GetString().ShouldBe("dotnet");
                row.GetProperty("build_argv")[1].GetString().ShouldBe("build");
                row.GetProperty("build_argv").EnumerateArray().Select(item => item.GetString()).ShouldContain("--no-restore");
            }
        }

        architecture.ShouldContain("eng/dependency-graph-policy.json");
        architecture.ShouldNotContain("\"restore_argv\"");
        architecture.ShouldNotContain("\"build_argv\"");
    }

    [Fact]
    public void DependencyGovernance_CollectAndEnforceStepsPrintGraphErrorOnFailure() {
        string root = RepositoryRoot();
        string ci = File.ReadAllText(Path.Combine(root, ".github/workflows/ci.yml"));
        string collect = ExtractNamedStep(ci, "Collect exact graph diff and affected-module proof");
        string enforce = ExtractNamedStep(ci, "Enforce dependency-governance result");

        collect.ShouldContain("payload.get(\"error\")");
        collect.ShouldContain("::error::{error}");
        enforce.ShouldContain("payload.get(\"error\")");
        enforce.ShouldContain("::error::{error}");
        enforce.ShouldContain("exit 1");

        const string errorText = "EventStore System.CommandLine expected 2.0.10 found 2.0.11";
        string workDir = Path.Combine(Path.GetTempPath(), $"fc-gov-graph-error-{Guid.NewGuid():N}");
        string artifactDir = Path.Combine(workDir, "artifacts", "dependency-governance");
        Directory.CreateDirectory(artifactDir);
        try {
            File.WriteAllText(
                Path.Combine(artifactDir, "dependency-graph-diff.json"),
                """{"ok":false,"error":"EventStore System.CommandLine expected 2.0.10 found 2.0.11"}""");
            File.WriteAllText(Path.Combine(workDir, "collect-printer.py"), ExtractPythonHeredoc(collect));
            File.WriteAllText(
                Path.Combine(workDir, "enforce.sh"),
                ExtractRunScript(ci, "Enforce dependency-governance result"));

            Dictionary<string, string> collectEnvironment = new() {
                ["WORK_DIR"] = workDir,
            };
            ProcessResult collectResult = RunProcess(root, "bash", [
                "-c",
                "cd \"$WORK_DIR\" && python3 collect-printer.py",
            ], collectEnvironment);
            collectResult.ExitCode.ShouldBe(0, $"stdout={collectResult.Output} stderr={collectResult.Error}");
            collectResult.Output.ShouldContain("::error::");
            collectResult.Output.ShouldContain(errorText);

            Dictionary<string, string> enforceEnvironment = new() {
                ["WORK_DIR"] = workDir,
                ["DIFF_EXIT_CODE"] = "1",
            };
            ProcessResult enforceResult = RunProcess(root, "bash", [
                "-c",
                "cd \"$WORK_DIR\" && bash enforce.sh",
            ], enforceEnvironment);
            enforceResult.ExitCode.ShouldBe(1, $"stdout={enforceResult.Output} stderr={enforceResult.Error}");
            enforceResult.Output.ShouldContain("::error::");
            enforceResult.Output.ShouldContain(errorText);
        }
        finally {
            if (Directory.Exists(workDir)) {
                Directory.Delete(workDir, recursive: true);
            }
        }
    }

    [Fact]
    public void ValidateNugetPackagesScript_FailsClosedOnEmptyPackageDirectory() {
        // REL-2 code-review P4 (2026-07-13): the scripts/ consumer-validation trio is the sole
        // enforcement of the 8-package inventory + kernel-split invariant (AC6), but had no automated
        // test — a validator logic error (e.g. a wrong forbidden-fragment, or the license/count check)
        // would let a broken package set pass CI silently. Negative pin: a package directory that is
        // NOT the 8 expected packages MUST exit non-zero so the reusable domain-ci lane fails closed
        // rather than green-lighting an incomplete pack.
        string root = RepositoryRoot();
        string emptyDir = Path.Combine(Path.GetTempPath(), $"fc-empty-nupkgs-{Guid.NewGuid():N}");
        Directory.CreateDirectory(emptyDir);
        try {
            ProcessResult result = RunPython(root, ["scripts/validate-nuget-packages.py", emptyDir]);
            result.ExitCode.ShouldNotBe(
                0,
                $"validate-nuget-packages.py must fail closed on a package set that is not the 8 expected packages. stdout={result.Output} stderr={result.Error}");
            (result.Output + result.Error).ShouldContain(
                "Expected 8 packages",
                customMessage: "the failure must name the package-count mismatch so operators can act.");
        }
        finally {
            if (Directory.Exists(emptyDir)) {
                Directory.Delete(emptyDir, recursive: true);
            }
        }
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

    [Fact]
    public void HexalithDependencyMode_DefaultsToProjectReferencesForDebugAndPackagesForRelease() {
        string root = RepositoryRoot();
        string directoryBuildProps = File.ReadAllText(Path.Combine(root, "Directory.Build.props"));
        string appHostProject = File.ReadAllText(Path.Combine(root, "src", "Hexalith.FrontComposer.AppHost", "Hexalith.FrontComposer.AppHost.csproj"));
        string appHostProgram = File.ReadAllText(Path.Combine(root, "src", "Hexalith.FrontComposer.AppHost", "Program.cs"));

        directoryBuildProps.ShouldContain("UseHexalithProjectReferences");
        directoryBuildProps.ShouldContain("$(Configuration)' == 'Debug'\">true</UseHexalithProjectReferences>");
        directoryBuildProps.ShouldContain("<Import Project=\"deps.local.props\" Condition=\"'$(UseHexalithProjectReferences)' == 'true'");
        directoryBuildProps.ShouldContain("<Import Project=\"deps.nuget.props\" Condition=\"'$(UseHexalithProjectReferences)' != 'true'");
        directoryBuildProps.ShouldContain("UseNuGetDeps", customMessage: "the legacy inverse switch remains supported for existing scripts");

        File.Exists(Path.Combine(root, "deps.local.props")).ShouldBeTrue();
        File.Exists(Path.Combine(root, "deps.nuget.props")).ShouldBeTrue();
        appHostProject.ShouldContain("ProjectReference Include=\"$(EventStorePath)/src/Hexalith.EventStore.Aspire/Hexalith.EventStore.Aspire.csproj\"");
        appHostProject.ShouldContain("Condition=\"'$(HexalithEventStoreFromSource)' == 'true'\"");
        appHostProject.ShouldContain("PackageReference Include=\"Hexalith.EventStore.Aspire\"");
        appHostProject.ShouldContain("Condition=\"'$(HexalithEventStoreFromSource)' != 'true'\"");
        appHostProgram.ShouldNotContain(
            "Hexalith.Commons.Aspire",
            customMessage: "Hexalith.Commons.Aspire is not published as a NuGet package, so AppHost Release builds must not depend on it.");

        XDocument packages = XDocument.Load(Path.Combine(root, "references", "Hexalith.Builds", "Props", "Directory.Packages.props"));
        XElement eventStoreAspire = packages
            .Descendants("PackageVersion")
            .Single(e => string.Equals((string?)e.Attribute("Include"), "Hexalith.EventStore.Aspire", StringComparison.Ordinal));
        XAttribute versionAttribute = eventStoreAspire.Attribute("Version").ShouldNotBeNull(
            "Release builds consume the centrally imported Hexalith.Builds package pin, so the governance guard must still find a Version attribute on Hexalith.EventStore.Aspire.");
        versionAttribute.Value.ShouldNotBeNullOrWhiteSpace(
            "Release builds consume the centrally imported Hexalith.Builds package pin; this guard must not hard-code a sibling package patch version.");
    }

    [Fact]
    public void CentralPackageManagement_EnablesTransitivePinningForImportedPackageVersions() {
        string root = RepositoryRoot();
        XDocument packages = XDocument.Load(Path.Combine(root, "Directory.Packages.props"));

        XElement? transitivePinning = packages
            .Descendants("CentralPackageTransitivePinningEnabled")
            .SingleOrDefault();

        transitivePinning.ShouldNotBeNull(
            "OpenIdConnect restores IdentityModel packages transitively; imported PackageVersion pins must apply to prevent split Microsoft.IdentityModel assemblies.");
        transitivePinning.Value.ShouldBe("true");
    }

    [Fact]
    public void ToolchainPins_MatchApprovedDotnetAndAspireVersions() {
        const string expectedDotnetSdk = "10.0.400";
        const string sourceResourceCompatibilitySdk = "10.0.302";
        const string expectedAspire = "13.5.3";
        string root = RepositoryRoot();

        using (JsonDocument globalJson = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "global.json")))) {
            globalJson.RootElement.GetProperty("sdk").GetProperty("version").GetString().ShouldBe(expectedDotnetSdk);
        }

        Regex dotnetVersionPin = new(
            @"(?m)^[ \t]*dotnet-version[ \t]*:[ \t]*(?<quote>['""]?)(?<version>[^'""#\s]+)\k<quote>[ \t]*(?:#.*)?\r?$",
            RegexOptions.CultureInvariant);
        foreach (string quotingVariant in new[] {
            "dotnet-version: '10.0.400'",
            "dotnet-version: \"10.0.400\"",
            "dotnet-version: 10.0.400",
        }) {
            Match sample = dotnetVersionPin.Match(quotingVariant);
            sample.Success.ShouldBeTrue($"active dotnet-version parser must support valid YAML quoting: {quotingVariant}");
            sample.Groups["version"].Value.ShouldBe(expectedDotnetSdk);
        }

        string workflowsRoot = Path.Combine(root, ".github", "workflows");
        string[] workflowPaths = Directory.EnumerateFiles(workflowsRoot, "*.yml", SearchOption.TopDirectoryOnly)
            .Concat(Directory.EnumerateFiles(workflowsRoot, "*.yaml", SearchOption.TopDirectoryOnly))
            .Order(StringComparer.Ordinal)
            .ToArray();
        var dotnetPins = workflowPaths
            .SelectMany(path => dotnetVersionPin.Matches(File.ReadAllText(path)).Cast<Match>()
                .Select(match => (
                    Workflow: Path.GetFileName(path),
                    Version: match.Groups["version"].Value)))
            .ToArray();
        dotnetPins.ShouldNotBeEmpty("active dotnet-version pins must be discovered across every workflow file");
        dotnetPins.Count(pin => string.Equals(pin.Version, sourceResourceCompatibilitySdk, StringComparison.Ordinal))
            .ShouldBe(1, "only the full source-resource topology may install the 10.0.302 compatibility SDK");
        dotnetPins
            .Where(pin => !string.Equals(pin.Version, sourceResourceCompatibilitySdk, StringComparison.Ordinal))
            .ShouldAllBe(pin => string.Equals(pin.Version, expectedDotnetSdk, StringComparison.Ordinal));

        string quality = File.ReadAllText(Path.Combine(workflowsRoot, "quality.yml"));
        quality.ShouldMatch(
            @"(?ms)^[ \t]*- name:[ \t]*Install source-resource compatibility SDK[ \t]*\r?\n[ \t]*uses:[ \t]*actions/setup-dotnet@[^\r\n]+\r?\n[ \t]*with:[ \t]*\r?\n[ \t]*dotnet-version[ \t]*:[ \t]*(?:'10\.0\.302'|""10\.0\.302""|10\.0\.302)[ \t]*\r?$",
            customMessage: "the sole 10.0.302 pin must stay attached to the explicitly named source-resource compatibility step");

        Regex aspireCliInstall = new(
            @"(?m)^[^#\r\n]*\bdotnet\s+tool\s+install\s+--global\s+Aspire\.Cli\s+--version\s+(?<quote>['""]?)(?<version>[^'""#\s]+)\k<quote>(?:\s|$)",
            RegexOptions.CultureInvariant);
        Match[] aspireInstalls = workflowPaths
            .SelectMany(path => aspireCliInstall.Matches(File.ReadAllText(path)).Cast<Match>())
            .ToArray();
        aspireInstalls.Length.ShouldBe(1, "active workflows must contain exactly one Aspire CLI installation command");
        aspireInstalls[0].Groups["version"].Value.ShouldBe(expectedAspire);

        string ideWorkflow = File.ReadAllText(
            Path.Combine(root, ".github", "workflows", "ide-parity-revalidation.yml"));
        ideWorkflow.ShouldContain($"Detected .NET SDK version (e.g. {expectedDotnetSdk})");
        string ideJob = File.ReadAllText(Path.Combine(root, "jobs", "ide-parity-version-revalidation.ps1"));
        ideJob.ShouldContain($"Minimum = \"{expectedDotnetSdk}\"");
        ideJob.ShouldContain("Maximum = \"10.0.500\"");

        XDocument appHost = XDocument.Load(
            Path.Combine(root, "src", "Hexalith.FrontComposer.AppHost", "Hexalith.FrontComposer.AppHost.csproj"));
        appHost.Root.ShouldNotBeNull().Attribute("Sdk").ShouldNotBeNull().Value
            .ShouldBe($"Aspire.AppHost.Sdk/{expectedAspire}");
        appHost.Descendants("AspireUseCliBundle").Single().Value.ShouldBe("true");

        XDocument catalog = XDocument.Load(
            Path.Combine(root, "references", "Hexalith.Builds", "Props", "Directory.Packages.props"));
        string selectedAspire = catalog
            .Descendants("PackageVersion")
            .Single(element => string.Equals(
                (string?)element.Attribute("Include"),
                "Aspire.Hosting",
                StringComparison.Ordinal))
            .Attribute("Version")
            .ShouldNotBeNull()
            .Value;
        selectedAspire.ShouldBe(expectedAspire);
    }

    [Theory]
    [InlineData("10.0.302", 1)]
    [InlineData("10.0.400", 0)]
    [InlineData("10.0.499", 0)]
    [InlineData("10.0.500", 1)]
    [InlineData("10.0.400-preview.1", 1)]
    [InlineData("10.0", 1)]
    [InlineData("10.0.400.1", 1)]
    public void IdeParityVersionRevalidation_DotnetSdkFeatureBand_FailsClosed(
        string detectedSdk,
        int expectedExitCode) {
        string root = RepositoryRoot();
        string artifactRelative = $"artifacts/ide-parity/.sdk-threshold-{Guid.NewGuid():N}.md";
        string artifactPath = Path.Combine(root, artifactRelative.Replace('/', Path.DirectorySeparatorChar));
        try {
            ProcessResult result = RunPwsh(
                root,
                [
                    "jobs/ide-parity-version-revalidation.ps1",
                    "-NoGithub",
                    "-OutPath",
                    artifactRelative,
                ],
                new Dictionary<string, string> {
                    ["FRONTCOMPOSER_DOTNET_SDK_VERSION"] = detectedSdk,
                    ["FRONTCOMPOSER_IDE_VERSION_VISUALSTUDIO"] = "17.13",
                    ["FRONTCOMPOSER_IDE_VERSION_RIDER"] = "2026.1",
                });

            result.ExitCode.ShouldBe(
                expectedExitCode,
                $"SDK {detectedSdk} threshold result was unexpected. stdout={result.Output} stderr={result.Error}");
            File.Exists(artifactPath).ShouldBe(
                expectedExitCode != 0,
                "fail-closed SDK values must produce a deterministic dry-run revalidation artifact");
        }
        finally {
            File.Delete(artifactPath);
        }
    }

    [Fact]
    public void SharedPackageCatalog_WhenChanged_InvalidatesCacheAndReleaseEvidenceOnly() {
        string root = RepositoryRoot();
        string qualityWorkflow = File.ReadAllText(Path.Combine(root, ".github", "workflows", "quality.yml"));
        qualityWorkflow.ShouldContain(
            "hashFiles('Directory.Packages.props', 'references/Hexalith.Builds/Props/Directory.Packages.props')");

        string releaseEvidence = File.ReadAllText(Path.Combine(root, "eng", "release_evidence.py"));
        int releaseDefinitionsStart = releaseEvidence.IndexOf("RELEASE_DEFINITION_FILES = [", StringComparison.Ordinal);
        int fallbackInvalidationStart = releaseEvidence.IndexOf("FALLBACK_INVALIDATION_FILES = [", StringComparison.Ordinal);
        int approvalMatrixStart = releaseEvidence.IndexOf("APPROVAL_MATRIX = [", StringComparison.Ordinal);
        releaseDefinitionsStart.ShouldBeGreaterThanOrEqualTo(0);
        fallbackInvalidationStart.ShouldBeGreaterThan(releaseDefinitionsStart);
        approvalMatrixStart.ShouldBeGreaterThan(fallbackInvalidationStart);

        const string sharedCatalog = "references/Hexalith.Builds/Props/Directory.Packages.props";
        string releaseDefinitions = releaseEvidence[releaseDefinitionsStart..fallbackInvalidationStart];
        string fallbackInvalidation = releaseEvidence[fallbackInvalidationStart..approvalMatrixStart];
        releaseDefinitions.ShouldContain(sharedCatalog);
        fallbackInvalidation.ShouldNotContain(
            sharedCatalog,
            customMessage: "routine shared package-version changes must not invalidate active fallback approvals");
    }

    [Fact]
    public void ReleaseSolutionBuild_ExcludesExternalHexalithReferenceProjects() {
        string root = RepositoryRoot();
        XDocument solution = XDocument.Load(Path.Combine(root, "Hexalith.FrontComposer.slnx"));

        List<string> offenders = [];
        int scanned = 0;
        foreach (XElement project in solution.Descendants("Project")) {
            string? path = project.Attribute("Path")?.Value;
            if (path is null || !path.StartsWith("references/Hexalith.", StringComparison.Ordinal)) {
                continue;
            }

            scanned++;
            bool disablesRelease = project
                .Elements("Build")
                .Any(static build =>
                    string.Equals((string?)build.Attribute("Solution"), "Release|*", StringComparison.Ordinal)
                    && string.Equals((string?)build.Attribute("Project"), "false", StringComparison.Ordinal));
            if (!disablesRelease) {
                offenders.Add(path);
            }
        }

        scanned.ShouldBeGreaterThan(0, "the solution should continue to expose external Hexalith projects for Debug/source navigation");
        offenders.ShouldBeEmpty(
            "references/Hexalith.* projects are source-debug conveniences only. Release solution builds must consume "
            + "published NuGet packages instead. Missing Release|* Project=false on: " + string.Join("; ", offenders));
    }

    internal static string StripYamlComments(string yaml) {
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
        workflow.ShouldContain("submodules: false");
        workflow.ShouldContain("Initialize build submodules");
        workflow.ShouldContain("eng/llm_benchmark.py validate-prompt-set");
        workflow.ShouldContain("eng/llm_benchmark.py run-benchmark");
        workflow.ShouldContain("SkillBenchmarkPromptSet.LoadEmbeddedV1");
        workflow.ShouldContain("budget-status");
        workflow.ShouldContain("BenchmarkHarnessTests");
        workflow.ShouldContain("tests/Hexalith.FrontComposer.Shell.Tests.Bench/Hexalith.FrontComposer.Shell.Tests.Bench.csproj");
        workflow.ShouldContain("--filter-trait \"Category=Performance\"");
        workflow.ShouldContain("--filter-method \"*BenchmarkHarnessTests*\"");
        workflow.ShouldContain("--report-xunit-trx-filename benchmark-results.trx");
        workflow.ShouldNotContain("tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj --configuration Release --filter FullyQualifiedName~BenchmarkHarnessTests");
        workflow.ShouldContain("candidate evidence only");
        workflow.ShouldContain("28-day ratchet");
        workflow.ShouldContain("--budget .github/benchmark-budget.json");
        workflow.ShouldNotContain("--provider-results");

        string budgetStep = ExtractNamedStep(workflow, "Check monthly LLM budget before provider spend");
        budgetStep.ShouldContain("continue-on-error: true");
        budgetStep.ShouldContain("--budget .github/benchmark-budget.json");

        string runBenchmarkStep = ExtractNamedStep(workflow, "Run 20-prompt LLM benchmark gate");
        runBenchmarkStep.ShouldContain("continue-on-error: true");
        runBenchmarkStep.ShouldContain("eng/llm_benchmark.py run-benchmark");
        runBenchmarkStep.ShouldNotContain("--provider-results");

        string requireSummaryStep = ExtractNamedStep(workflow, "Require benchmark run-summary artifact");
        requireSummaryStep.ShouldContain("test -f artifacts/benchmark/run-summary.json");
        requireSummaryStep.ShouldNotContain("continue-on-error: true");

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
        using var doc = JsonDocument.Parse(File.ReadAllText(output));
        doc.RootElement.GetProperty("prompt_count").GetInt32().ShouldBe(20);
        doc.RootElement.GetProperty("classification").GetString().ShouldBe("budget-blocked");
    }

    [Fact]
    public void NightlyBenchmarkWorkflow_MissingBudgetFile_WritesUnknownNoSpendArtifact() {
        string root = RepositoryRoot();
        string missingBudget = Path.Combine(Path.GetTempPath(), $"fc-missing-budget-{Guid.NewGuid():N}.json");
        string output = Path.Combine(Path.GetTempPath(), $"fc-budget-unknown-{Guid.NewGuid():N}.json");

        File.Exists(missingBudget).ShouldBeFalse();
        ProcessResult result = RunPython(root, [
            "eng/llm_benchmark.py",
            "budget-status",
            "--budget", missingBudget,
            "--output", output,
        ]);

        result.ExitCode.ShouldBe(2, result.Error);
        using var doc = JsonDocument.Parse(File.ReadAllText(output));
        doc.RootElement.GetProperty("status").GetString().ShouldBe("budget-unknown");
        doc.RootElement.GetProperty("api_spend_allowed").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    public void NightlyBenchmarkWorkflow_FailClosedPlaceholder_DeniesSpendAndWritesArtifact() {
        string root = RepositoryRoot();
        string budgetPath = Path.Combine(root, ".github/benchmark-budget.json");
        string output = Path.Combine(Path.GetTempPath(), $"fc-committed-budget-{Guid.NewGuid():N}.json");

        File.Exists(budgetPath).ShouldBeTrue();
        using (var budgetDoc = JsonDocument.Parse(File.ReadAllText(budgetPath))) {
            budgetDoc.RootElement.GetProperty("monthly_cap").GetInt32().ShouldBe(0);
            budgetDoc.RootElement.GetProperty("provider_cost_metadata_available").GetBoolean().ShouldBeFalse();
        }

        ProcessResult result = RunPython(root, [
            "eng/llm_benchmark.py",
            "budget-status",
            "--budget", ".github/benchmark-budget.json",
            "--output", output,
        ]);

        result.ExitCode.ShouldBe(2, result.Error);
        using var doc = JsonDocument.Parse(File.ReadAllText(output));
        doc.RootElement.GetProperty("status").GetString().ShouldBe("budget-unknown");
        doc.RootElement.GetProperty("api_spend_allowed").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    public void NightlyBenchmarkWorkflow_MissingBudgetArtifact_WritesBudgetBlockedSummary() {
        string root = RepositoryRoot();
        string missingArtifact = Path.Combine(Path.GetTempPath(), $"fc-missing-artifact-{Guid.NewGuid():N}.json");
        string output = Path.Combine(Path.GetTempPath(), $"fc-run-summary-{Guid.NewGuid():N}.json");

        File.Exists(missingArtifact).ShouldBeFalse();
        ProcessResult run = RunPython(root, [
            "eng/llm_benchmark.py",
            "run-benchmark",
            "--root", ".",
            "--budget-artifact", missingArtifact,
            "--output", output,
        ]);

        run.ExitCode.ShouldBe(2, run.Error);
        using var doc = JsonDocument.Parse(File.ReadAllText(output));
        doc.RootElement.GetProperty("prompt_count").GetInt32().ShouldBe(20);
        doc.RootElement.GetProperty("classification").GetString().ShouldBe("budget-blocked");
        doc.RootElement.GetProperty("provider_results_supplied").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    public void QualityWorkflow_BuildsContractsNetStandard20InIsolation() {
        // REL-2 (2026-07-13): the Contracts netstandard2.0 isolation build (Gate 1) is a
        // FrontComposer-specific gate the shared reusable domain-ci.yml does not run, so it moved
        // from the bespoke release.yml into the supplemental quality.yml and runs before the full
        // solution build (Gate 2). The release path no longer builds/tests inline.
        string root = RepositoryRoot();
        string workflow = File.ReadAllText(Path.Combine(root, ".github/workflows/quality.yml"));
        string gate1 = ExtractNamedStep(workflow, "Gate 1: Contracts Build (netstandard2.0)");

        const string restoreCommand = "dotnet restore Hexalith.FrontComposer.slnx -p:Configuration=Release";
        const string buildCommand = "dotnet build src/Hexalith.FrontComposer.Contracts/Hexalith.FrontComposer.Contracts.csproj -f netstandard2.0 --configuration Release --no-restore";
        gate1.ShouldContain(restoreCommand);
        gate1.ShouldContain(buildCommand);
        gate1.ShouldNotContain("if:");
        gate1.ShouldNotContain("continue-on-error");

        workflow.IndexOf("Gate 1: Contracts Build (netstandard2.0)", StringComparison.Ordinal)
            .ShouldBeLessThan(workflow.IndexOf("Gate 2: Solution Build", StringComparison.Ordinal));
    }

    [Fact]
    public void ReleaseWorkflow_DelegatesToReusableDomainReleaseAfterCiGate() {
        string root = RepositoryRoot();
        string workflow = File.ReadAllText(Path.Combine(root, ".github/workflows/release.yml"));
        string releaseConfig = File.ReadAllText(Path.Combine(root, ".releaserc.json"));
        MatchCollection domainReleasePins = Regex.Matches(
            workflow,
            @"uses: Hexalith/Hexalith\.Builds/\.github/workflows/domain-release\.yml@(?<sha>[0-9a-f]{40})\b");
        domainReleasePins.Count.ShouldBeGreaterThanOrEqualTo(
            1,
            "release.yml must pin domain-release.yml to an exact 40-hex lowercase Builds commit SHA (not @main or a tag).");
        MatchCollection buildsExecutionEnvs = Regex.Matches(
            workflow,
            @"(?m)^  BUILDS_EXECUTION_SHA: (?<sha>[0-9a-f]{40})\s*$");
        buildsExecutionEnvs.Count.ShouldBeGreaterThanOrEqualTo(
            1,
            "release.yml must declare env.BUILDS_EXECUTION_SHA as an exact 40-hex lowercase Builds commit.");
        MatchCollection hexalithBuildsExecutionEnvs = Regex.Matches(
            workflow,
            @"(?m)^          HEXALITH_BUILDS_EXECUTION_SHA: (?<sha>[0-9a-f]{40})\s*$");
        hexalithBuildsExecutionEnvs.Count.ShouldBeGreaterThanOrEqualTo(
            1,
            "release.yml must declare HEXALITH_BUILDS_EXECUTION_SHA as an exact 40-hex lowercase Builds commit.");
        MatchCollection prepareBuildsRefs = Regex.Matches(
            workflow,
            @"(?ms)repository: Hexalith/Hexalith\.Builds\r?\n\s+ref: (?<sha>[0-9a-f]{40})\r?\n\s+path: \.hexalith/builds-execution");
        prepareBuildsRefs.Count.ShouldBeGreaterThanOrEqualTo(
            1,
            "release.yml prepare-candidate must check out Hexalith.Builds at an exact 40-hex ref into .hexalith/builds-execution.");
        MatchCollection buildsExecutionShas = Regex.Matches(
            workflow,
            @"builds-execution-sha: (?<sha>[0-9a-f]{40})\b");
        buildsExecutionShas.Count.ShouldBeGreaterThanOrEqualTo(1);

        // The selected package catalog and the immutable reusable-workflow execution
        // coordinate are separate contracts. A catalog-only gitlink move must not silently
        // retarget CI or Release execution; BUILDS_EXECUTION_SHA owns that coordinate.
        string approvedBuildsSha = buildsExecutionEnvs
            .Cast<Match>()
            .Select(match => match.Groups["sha"].Value)
            .Distinct(StringComparer.Ordinal)
            .Single();

        string[] releaseBuildsCoordinates =
        [
            .. domainReleasePins.Cast<Match>().Select(match => match.Groups["sha"].Value),
            .. buildsExecutionEnvs.Cast<Match>().Select(match => match.Groups["sha"].Value),
            .. hexalithBuildsExecutionEnvs.Cast<Match>().Select(match => match.Groups["sha"].Value),
            .. prepareBuildsRefs.Cast<Match>().Select(match => match.Groups["sha"].Value),
            .. buildsExecutionShas.Cast<Match>().Select(match => match.Groups["sha"].Value),
        ];
        releaseBuildsCoordinates.ShouldAllBe(sha => sha == approvedBuildsSha);

        string ciWorkflow = File.ReadAllText(Path.Combine(root, ".github/workflows/ci.yml"));
        MatchCollection domainCiPins = Regex.Matches(
            ciWorkflow,
            @"uses: Hexalith/Hexalith\.Builds/\.github/workflows/domain-ci\.yml@(?<sha>[0-9a-f]{40})\b");
        domainCiPins.Count.ShouldBeGreaterThanOrEqualTo(
            1,
            "ci.yml must pin domain-ci.yml to an exact 40-hex lowercase Builds commit SHA (never @main).");
        domainCiPins.Cast<Match>().ShouldAllBe(match =>
            match.Groups["sha"].Value == approvedBuildsSha);

        string releaseEvidence = File.ReadAllText(Path.Combine(root, ".github/workflows/release-evidence.yml"));
        MatchCollection evidenceBuildsRefs = Regex.Matches(
            releaseEvidence,
            @"(?ms)repository: Hexalith/Hexalith\.Builds\r?\n\s+ref: (?<sha>[0-9a-f]{40})\r?\n\s+path: \.hexalith/builds-execution");
        evidenceBuildsRefs.Count.ShouldBeGreaterThanOrEqualTo(
            1,
            "release-evidence.yml must check out Hexalith.Builds at an exact 40-hex ref into .hexalith/builds-execution.");
        evidenceBuildsRefs.Cast<Match>().ShouldAllBe(match =>
            match.Groups["sha"].Value == approvedBuildsSha);
        // The reusable workflow requires actions: read to validate the successful exact-source CI
        // run. Assert it on the release job itself: a workflow-level or sibling-job occurrence
        // cannot satisfy reusable-workflow permission validation. BUILD-REL-1 also declares a
        // governed-release job with id-token/attestations; GitHub checks those scopes statically
        // against the caller even when FrontComposer leaves governed-release unset.
        string releasePermissions = ExtractJobPermissionsBlock(workflow, "release");
        releasePermissions.ShouldMatch(@"(?m)^      actions: read\r?$");
        releasePermissions.ShouldMatch(@"(?m)^      attestations: write\r?$");
        releasePermissions.ShouldMatch(@"(?m)^      id-token: write\r?$");
        workflow.ShouldContain("solution: Hexalith.FrontComposer.slnx");
        workflow.ShouldContain("test-projects: ''");
        workflow.ShouldContain("environment-name: production");
        workflow.ShouldContain("package-manifest: tools/release-packages.json");
        workflow.ShouldContain("expected-package-count: 8");
        workflow.ShouldContain("publish-containers: false");
        workflow.ShouldContain("container-projects: ''");
        workflow.ShouldContain("NUGET_API_KEY: ${{ secrets.NUGET_API_KEY }}");
        releasePermissions.ShouldMatch(@"(?m)^      contents: write\r?$");
        releasePermissions.ShouldMatch(@"(?m)^      issues: write\r?$");
        releasePermissions.ShouldMatch(@"(?m)^      pull-requests: write\r?$");
        workflow.ShouldNotContain("submodules: recursive");

        workflow.ShouldNotContain("attest-build-provenance");
        workflow.ShouldContain("workflow_dispatch:");

        // REL-3 (2026-07-18): semantic-release delegates prepare/publish to the repository-owned
        // exact-artifact orchestrator (eng/release_prepublish.py) — pack-once, fail-closed FR24
        // gate before any side effect, and a publisher that pushes only the manifest-authorized
        // sealed bytes. Raw pack/push commands and inlined evidence commands stay out of the JSON.
        releaseConfig.ShouldContain("@semantic-release/commit-analyzer");
        releaseConfig.ShouldContain("@semantic-release/release-notes-generator");
        releaseConfig.ShouldContain("python3 eng/release_prepublish.py restore --version ${nextRelease.version}");
        releaseConfig.ShouldContain("python3 eng/release_prepublish.py verify-prepared --version ${nextRelease.version}");
        releaseConfig.ShouldContain("python3 eng/release_prepublish.py publish --version ${nextRelease.version}");
        releaseConfig.ShouldContain("nupkgs/*.nupkg");
        releaseConfig.ShouldContain("nupkgs/*.snupkg");
        releaseConfig.ShouldContain("release-evidence/*.json");
        releaseConfig.ShouldContain("release-evidence/*.txt");
        releaseConfig.ShouldContain("@semantic-release/github");
        releaseConfig.ShouldNotContain("\"@semantic-release/git\"");
        releaseConfig.ShouldNotContain("\"@semantic-release/changelog\"");
        releaseConfig.ShouldNotContain("pack_release_packages.py");
        releaseConfig.ShouldNotContain("dotnet nuget push");
        releaseConfig.ShouldNotContain("--skip-duplicate");
        releaseConfig.ShouldNotContain("CycloneDX");
        releaseConfig.ShouldNotContain("dotnet nuget sign");
        releaseConfig.ShouldNotContain("gh attestation");
    }

    [Fact]
    public void SemanticReleaseAnalyzer_ConventionalCommitsMatrix_SelectsExpectedReleaseTypes() {
        string root = RepositoryRoot();

        using (JsonDocument releaseConfig = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, ".releaserc.json")))) {
            JsonElement plugins = releaseConfig.RootElement.GetProperty("plugins");
            JsonElement analyzer = plugins.EnumerateArray().Single(static plugin =>
                plugin.ValueKind == JsonValueKind.Array
                && plugin.GetArrayLength() > 0
                && string.Equals(plugin[0].GetString(), "@semantic-release/commit-analyzer", StringComparison.Ordinal));
            JsonElement notes = plugins.EnumerateArray().Single(static plugin =>
                plugin.ValueKind == JsonValueKind.Array
                && plugin.GetArrayLength() > 0
                && string.Equals(plugin[0].GetString(), "@semantic-release/release-notes-generator", StringComparison.Ordinal));

            analyzer[1].GetProperty("preset").GetString().ShouldBe("conventionalcommits");
            notes[1].GetProperty("preset").GetString().ShouldBe("conventionalcommits");
        }

        using (JsonDocument package = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "package.json")))) {
            package.RootElement
                .GetProperty("devDependencies")
                .GetProperty("conventional-changelog-conventionalcommits")
                .GetString()
                .ShouldBe("^10.4.0");
        }

        using (JsonDocument packageLock = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "package-lock.json")))) {
            packageLock.RootElement
                .GetProperty("packages")
                .GetProperty(string.Empty)
                .GetProperty("devDependencies")
                .GetProperty("conventional-changelog-conventionalcommits")
                .GetString()
                .ShouldBe("^10.4.0");
        }

        const string analyzerHarness = """
            import { spawnSync } from 'node:child_process';
            import fs from 'node:fs/promises';
            import { analyzeCommits } from '@semantic-release/commit-analyzer';
            import { generateNotes } from '@semantic-release/release-notes-generator';

            const config = JSON.parse(await fs.readFile('.releaserc.json', 'utf8'));
            const analyzerEntry = config.plugins.find(entry =>
              Array.isArray(entry) && entry[0] === '@semantic-release/commit-analyzer');
            if (!analyzerEntry) {
              throw new Error('Configured semantic-release commit analyzer was not found.');
            }
            const notesEntry = config.plugins.find(entry =>
              Array.isArray(entry) && entry[0] === '@semantic-release/release-notes-generator');
            if (!notesEntry) {
              throw new Error('Configured semantic-release notes generator was not found.');
            }

            const cases = [
              { name: 'fixBreakingHeader', message: 'fix!: break the public API' },
              { name: 'featBreakingHeader', message: 'feat!: break the public API' },
              { name: 'scopedBreakingHeader', message: 'fix(release)!: break the scoped public API' },
              { name: 'breakingFooter', message: 'fix: adjust the public API\n\nBREAKING CHANGE: replace the public contract' },
              { name: 'ordinaryFix', message: 'fix: adjust the public API' },
              { name: 'ordinaryFeat', message: 'feat: add a public API' },
              { name: 'malformedBreakingSubject', message: 'BREAKING CHANGE: break the public API' },
              { name: 'headerMaxValid', message: 'fix: ' + 'a'.repeat(195) },
              { name: 'headerMaxExceeded', message: 'fix: ' + 'a'.repeat(196) },
              { name: 'bodyMaxValid', message: 'fix: subject\n\n' + 'b'.repeat(200) },
              { name: 'bodyMaxExceeded', message: 'fix: subject\n\n' + 'b'.repeat(201) },
              { name: 'footerMaxValid', message: 'fix: subject\n\nbody\n\n' + 'c'.repeat(200) },
              { name: 'footerMaxExceeded', message: 'fix: subject\n\nbody\n\n' + 'c'.repeat(201) },
            ];
            const releaseTypes = {};
            const commitlintValid = {};
            const logger = { log() {} };
            for (const testCase of cases) {
              const releaseType = await analyzeCommits(
                analyzerEntry[1],
                { commits: [{ message: testCase.message }], logger });
              releaseTypes[testCase.name] = releaseType ?? null;

              const lintResult = spawnSync(
                process.execPath,
                ['node_modules/@commitlint/cli/cli.js'],
                { cwd: process.cwd(), input: `${testCase.message}\n`, encoding: 'utf8' });
              if (lintResult.error) {
                throw lintResult.error;
              }
              commitlintValid[testCase.name] = lintResult.status === 0;
            }

            const releaseNotes = await generateNotes(
              notesEntry[1],
              {
                commits: [
                  { message: cases.find(testCase => testCase.name === 'fixBreakingHeader').message, hash: '1111111111111111' },
                  { message: cases.find(testCase => testCase.name === 'breakingFooter').message, hash: '2222222222222222' },
                  { message: cases.find(testCase => testCase.name === 'ordinaryFix').message, hash: '3333333333333333' },
                  { message: cases.find(testCase => testCase.name === 'ordinaryFeat').message, hash: '4444444444444444' },
                ],
                lastRelease: { gitTag: 'v2.0.4', gitHead: 'old' },
                nextRelease: { version: '3.0.0', gitTag: 'v3.0.0', gitHead: 'new' },
                options: { repositoryUrl: 'https://github.com/Hexalith/Hexalith.FrontComposer.git' },
                cwd: process.cwd(),
              });

            process.stdout.write(JSON.stringify({ releaseTypes, commitlintValid, releaseNotes }));
            """;
        ProcessResult result = RunProcess(root, "node", ["--input-type=module", "--eval", analyzerHarness]);
        result.ExitCode.ShouldBe(0, $"stdout={result.Output} stderr={result.Error}");

        using JsonDocument behavior = JsonDocument.Parse(result.Output);
        JsonElement releaseTypes = behavior.RootElement.GetProperty("releaseTypes");
        releaseTypes.GetProperty("fixBreakingHeader").GetString().ShouldBe("major");
        releaseTypes.GetProperty("featBreakingHeader").GetString().ShouldBe("major");
        releaseTypes.GetProperty("scopedBreakingHeader").GetString().ShouldBe("major");
        releaseTypes.GetProperty("breakingFooter").GetString().ShouldBe("major");
        releaseTypes.GetProperty("ordinaryFix").GetString().ShouldBe("patch");
        releaseTypes.GetProperty("ordinaryFeat").GetString().ShouldBe("minor");
        releaseTypes.GetProperty("malformedBreakingSubject").ValueKind.ShouldBe(JsonValueKind.Null);

        JsonElement commitlintValid = behavior.RootElement.GetProperty("commitlintValid");
        commitlintValid.GetProperty("fixBreakingHeader").GetBoolean().ShouldBeTrue();
        commitlintValid.GetProperty("featBreakingHeader").GetBoolean().ShouldBeTrue();
        commitlintValid.GetProperty("scopedBreakingHeader").GetBoolean().ShouldBeTrue();
        commitlintValid.GetProperty("breakingFooter").GetBoolean().ShouldBeTrue();
        commitlintValid.GetProperty("ordinaryFix").GetBoolean().ShouldBeTrue();
        commitlintValid.GetProperty("ordinaryFeat").GetBoolean().ShouldBeTrue();
        commitlintValid.GetProperty("malformedBreakingSubject").GetBoolean().ShouldBeFalse();
        commitlintValid.GetProperty("headerMaxValid").GetBoolean().ShouldBeTrue();
        commitlintValid.GetProperty("headerMaxExceeded").GetBoolean().ShouldBeFalse();
        commitlintValid.GetProperty("bodyMaxValid").GetBoolean().ShouldBeTrue();
        commitlintValid.GetProperty("bodyMaxExceeded").GetBoolean().ShouldBeFalse();
        commitlintValid.GetProperty("footerMaxValid").GetBoolean().ShouldBeTrue();
        commitlintValid.GetProperty("footerMaxExceeded").GetBoolean().ShouldBeFalse();

        string releaseNotes = behavior.RootElement.GetProperty("releaseNotes").GetString() ?? string.Empty;
        releaseNotes.ShouldContain("BREAKING CHANGES");
        releaseNotes.ShouldContain("break the public API");
        releaseNotes.ShouldContain("replace the public contract");
        releaseNotes.ShouldContain("### Bug Fixes");
        releaseNotes.ShouldContain("adjust the public API");
        releaseNotes.ShouldContain("### Features");
        releaseNotes.ShouldContain("add a public API");
    }

    [Fact]
    public void PackageInventory_IsExplicitLockstepAndReviewable() {
        string root = RepositoryRoot();
        string inventory = File.ReadAllText(Path.Combine(root, "eng/release-package-inventory.json"));
        string packScript = File.ReadAllText(Path.Combine(root, "scripts/pack-release-packages.py"));
        string compatibilityPolicy = File.ReadAllText(Path.Combine(root, "eng/release_compatibility.py"));
        string releasePrepublish = File.ReadAllText(Path.Combine(root, "eng/release_prepublish.py"));
        string directoryTargets = File.ReadAllText(Path.Combine(root, "Directory.Build.targets"));
        string qualityWorkflow = File.ReadAllText(Path.Combine(root, ".github/workflows/quality.yml"));
        string testingProject = File.ReadAllText(Path.Combine(root, "src/Hexalith.FrontComposer.Testing/Hexalith.FrontComposer.Testing.csproj"));

        inventory.ShouldContain("Hexalith.FrontComposer.Cli");
        inventory.ShouldContain("Hexalith.FrontComposer.Contracts");
        inventory.ShouldContain("Hexalith.FrontComposer.Contracts.UI");
        inventory.ShouldContain("Hexalith.FrontComposer.Mcp");
        inventory.ShouldContain("Hexalith.FrontComposer.Schema");
        inventory.ShouldContain("Hexalith.FrontComposer.Shell");
        inventory.ShouldContain("Hexalith.FrontComposer.Testing");
        inventory.ShouldContain("Hexalith.FrontComposer.SourceTools");
        inventory.ShouldContain("\"packable\": false");
        inventory.ShouldContain("exception");
        packScript.ShouldContain("release_properties(version)");
        compatibilityPolicy.ShouldContain("\"-p:EnableFrontComposerPackageValidation=true\"");
        compatibilityPolicy.ShouldContain("f\"-p:PackageVersion={version}\"");
        compatibilityPolicy.ShouldContain("\"-p:ContinuousIntegrationBuild=true\"");
        compatibilityPolicy.ShouldContain("-p:FrontComposerPackageValidationBaselineVersion={PUBLISHED_BASELINE_VERSION}");
        compatibilityPolicy.ShouldContain("\"-p:FrontComposerPackageValidationSkipBaseline=false\"");
        releasePrepublish.ShouldContain("scripts/pack-release-packages.py");
        releasePrepublish.ShouldContain("\"--release-policy\"");
        releasePrepublish.ShouldContain("eng/verify-candidate-packages.cs");
        compatibilityPolicy.ShouldContain("PUBLISHED_BASELINE_VERSION = \"4.1.1\"");
        File.Exists(Path.Combine(root, "eng/pack_release_packages.py")).ShouldBeFalse(
            "the retired build-plus-pack lifecycle entrypoint must not coexist with the live packer.");
        qualityWorkflow.ShouldContain("python3 -m unittest tests/eng/test_pack_release_packages.py tests/eng/test_release_prepublish.py");
        qualityWorkflow.ShouldContain("dotnet restore Hexalith.FrontComposer.slnx -p:Configuration=Release -p:EnableFrontComposerPackageValidation=true");
        qualityWorkflow.ShouldMatch(@"dotnet pack[^\r\n]+-p:EnableFrontComposerPackageValidation=true");
        qualityWorkflow.ShouldMatch(@"dotnet pack[^\r\n]+-p:FrontComposerPackageValidationBaselineVersion=4.1.1");
        qualityWorkflow.ShouldMatch(@"dotnet pack[^\r\n]+-p:FrontComposerPackageValidationSkipBaseline=false");
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
        using var doc = JsonDocument.Parse(File.ReadAllText(output));
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
    public void SemanticReleasePack_EvaluatesPublished411PackageValidationBaseline() {
        // The shared package-validation policy and the Contracts.UI explicit pin must both resolve
        // to the latest published 4.1.1 surface before semantic-release packs the 4.2 line.
        string root = RepositoryRoot();

        static (string enable, string baseline) EvaluatePackageValidation(string root, string project) {
            ProcessResult result = RunProcess(root, "dotnet", [
                "msbuild",
                project,
                "-getProperty:EnablePackageValidation,PackageValidationBaselineVersion",
                "-p:EnableFrontComposerPackageValidation=true",
                "-nologo",
            ]);
            result.ExitCode.ShouldBe(0, result.Error);
            using JsonDocument evaluated = JsonDocument.Parse(result.Output);
            JsonElement properties = evaluated.RootElement.GetProperty("Properties");
            return (
                properties.GetProperty("EnablePackageValidation").GetString() ?? string.Empty,
                properties.GetProperty("PackageValidationBaselineVersion").GetString() ?? string.Empty);
        }

        (string baseEnable, string baseBaseline) = EvaluatePackageValidation(
            root,
            Path.Combine(root, "src", "Hexalith.FrontComposer.Contracts", "Hexalith.FrontComposer.Contracts.csproj"));
        baseEnable.ShouldBe("true");
        baseBaseline.ShouldBe("4.1.1");

        (string uiEnable, string uiBaseline) = EvaluatePackageValidation(
            root,
            Path.Combine(root, "src", "Hexalith.FrontComposer.Contracts.UI", "Hexalith.FrontComposer.Contracts.UI.csproj"));
        uiEnable.ShouldBe("true");
        uiBaseline.ShouldBe("4.1.1");
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
        using (var doc = JsonDocument.Parse(File.ReadAllText(output))) {
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
            "--no-root",
        ]);
        validManifest.ExitCode.ShouldBe(0, validManifest.Error);

        ProcessResult invalidManifest = RunPython(root, [
            "eng/release_evidence.py",
            "verify-manifest",
            "--manifest", "tests/ci-governance/fixtures/release-manifest-invalid.json",
            "--no-root",
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
                  "artifact_path": "nupkgs/Hexalith.FrontComposer.Contracts.1.2.3.nupkg",
                  "checksum": "pending-checksum",
                  "symbol_artifact": "nupkgs/Hexalith.FrontComposer.Contracts.1.2.3.snupkg",
                  "sbom_component": "Hexalith.FrontComposer.Contracts",
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
            "--no-root",
        ]);
        placeholderManifest.ExitCode.ShouldNotBe(0);

        string nullPackageManifestPath = Path.Combine(Path.GetTempPath(), $"fc-release-null-package-{Guid.NewGuid():N}.json");
        File.WriteAllText(nullPackageManifestPath, """
            {
              "commit_sha": "abc123",
              "tag": "v1.2.3",
              "run_id": "42",
              "workflow_ref": "Hexalith/Hexalith.FrontComposer/.github/workflows/release.yml@refs/tags/v1.2.3",
              "sbom_hash": "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
              "benchmark_summary_hash": "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc",
              "packages": [null],
              "seal": {
                "algorithm": "sha256",
                "hash": "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"
              }
            }
            """);
        ProcessResult nullPackageManifest = RunPython(root, [
            "eng/release_evidence.py",
            "verify-manifest",
            "--manifest", nullPackageManifestPath,
            "--no-root",
        ]);
        nullPackageManifest.ExitCode.ShouldBe(1);
        nullPackageManifest.Error.ShouldBeEmpty();

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
    public void ReleaseEvidenceScript_DetectsPostSealArtifactMutationFromRealFiles() {
        string root = RepositoryRoot();
        string tempRoot = Path.Combine(Path.GetTempPath(), $"fc-release-post-seal-{Guid.NewGuid():N}");
        try {
            RunPython(root, [
                "tests/ci-governance/stage_release_state.py",
                "publish",
                root,
                tempRoot,
            ]).ExitCode.ShouldBe(0);

            string sealedManifest = Path.Combine(tempRoot, "release-evidence", "sealed-manifest.json");
            using (JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(sealedManifest))) {
                string artifactPath = manifest.RootElement.GetProperty("packages")[0]
                    .GetProperty("artifact_path").GetString()!;
                File.WriteAllText(Path.Combine(tempRoot, artifactPath), "mutated package bytes");
            }
            string output = Path.Combine(tempRoot, "verification.json");

            ProcessResult result = RunPython(root, [
                "eng/release_evidence.py",
                "verify-manifest",
                "--root", tempRoot,
                "--graph-root", tempRoot,
                "--manifest", sealedManifest,
                "--output", output,
            ]);

            result.ExitCode.ShouldBe(1);
            File.ReadAllText(output).ShouldContain("sealed artifact checksum does not match");
        }
        finally {
            if (Directory.Exists(tempRoot)) {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void ReleaseEvidenceScript_DetectsReleaseDefinitionDriftFromRealFiles() {
        string root = RepositoryRoot();
        string tempRoot = Path.Combine(Path.GetTempPath(), $"fc-release-definition-drift-{Guid.NewGuid():N}");
        try {
            RunPython(root, [
                "tests/ci-governance/stage_release_state.py",
                "publish",
                root,
                tempRoot,
            ]).ExitCode.ShouldBe(0);

            File.WriteAllText(
                Path.Combine(tempRoot, "references", "Hexalith.Builds", "Props", "Directory.Packages.props"),
                "drifted shared package catalog");

            string sealedManifest = Path.Combine(tempRoot, "release-evidence", "sealed-manifest.json");
            string output = Path.Combine(tempRoot, "verification.json");

            ProcessResult result = RunPython(root, [
                "eng/release_evidence.py",
                "verify-manifest",
                "--root", tempRoot,
                "--graph-root", tempRoot,
                "--manifest", sealedManifest,
                "--output", output,
            ]);

            result.ExitCode.ShouldBe(1);
            File.ReadAllText(output).ShouldContain("release-definition drift");
        }
        finally {
            if (Directory.Exists(tempRoot)) {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void ReleaseEvidenceScript_ClassifiesReleaseReadinessFixtures() {
        string root = RepositoryRoot();
        string fixtures = Path.Combine(root, "tests/ci-governance/fixtures/release-readiness-cases.json");
        string output = Path.Combine(Path.GetTempPath(), $"fc-release-readiness-{Guid.NewGuid():N}.json");

        // CR-12-4-P156 (round-7): parse the fixture JSON and assert each required case
        // name appears as a `cases[].name` key. The prior `fixtureJson.ShouldContain(...)`
        // would pass when the substring happened to appear anywhere in the document
        // (e.g., a `context_class: "local-candidate"` enum value elsewhere) even if the
        // CASE itself was missing.
        string fixtureJson = File.ReadAllText(fixtures);
        string[] requiredCases = [
            "trusted-ready",
            "approved-fallback",
            "string-false-approval",
            "dry-run-from-dispatch",
            "missing-inventory-package",
            "skipped-tests",
            "zero-tests",
            "legacy-v2-unsigned-package",
            "legacy-v2-missing-timestamp",
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
            "fallback-approved-against-drifted-definition",
            "partial-helper-output",
            "rerun-review",
        ];

        using (var fixtureDoc = JsonDocument.Parse(fixtureJson)) {
            HashSet<string> caseNames = [];
            foreach (JsonElement caseElement in fixtureDoc.RootElement.GetProperty("cases").EnumerateArray()) {
                if (caseElement.TryGetProperty("name", out JsonElement nameElement) && nameElement.ValueKind == JsonValueKind.String) {
                    string? caseName = nameElement.GetString();
                    if (!string.IsNullOrWhiteSpace(caseName)) {
                        caseNames.Add(caseName);
                    }
                }
            }
            foreach (string requiredCase in requiredCases) {
                caseNames.ShouldContain(requiredCase, $"fixture '{requiredCase}' must be present as a cases[].name entry");
            }
        }

        ProcessResult result = RunPython(root, [
            "eng/release_evidence.py",
            "classify-fixtures",
            "--fixtures", fixtures,
            "--output", output,
        ]);

        result.ExitCode.ShouldBe(0, result.Error);
        using var doc = JsonDocument.Parse(File.ReadAllText(output));
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

        JsonElement rerun = doc.RootElement.GetProperty("results").EnumerateArray()
            .Single(r => r.GetProperty("name").GetString() == "rerun-review");
        (rerun.GetProperty("next_owner_action").GetString() ?? string.Empty)
            .ShouldContain("create a fresh dispatch or new tag");

        JsonElement stringFalseApproval = doc.RootElement.GetProperty("results").EnumerateArray()
            .Single(r => r.GetProperty("name").GetString() == "string-false-approval");
        stringFalseApproval.GetProperty("publish_authorized").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    public void ReleaseEvidenceScript_WritesTypedOutputForMalformedCliBooleans() {
        string root = RepositoryRoot();
        string manifest = Path.Combine(root, "tests/ci-governance/fixtures/release-manifest-valid.json");
        string output = Path.Combine(Path.GetTempPath(), $"fc-release-malformed-cli-{Guid.NewGuid():N}.json");

        ProcessResult result = RunPython(root, [
            "eng/release_evidence.py",
            "classify-release",
            "--root", ".",
            "--manifest", manifest,
            "--output", output,
            "--from-fork", "approved",
        ]);

        result.ExitCode.ShouldBe(2);
        result.Error.ShouldContain("invalid --from-fork");
        File.Exists(output).ShouldBeTrue();
        File.ReadAllText(output).ShouldContain("helper_state must be success");
    }

    [Fact]
    public void ReleaseWorkflow_RequiresManualExactSourceCiBeforeProduction() {
        string root = RepositoryRoot();
        string workflow = File.ReadAllText(Path.Combine(root, ".github/workflows/release.yml"));
        string releaseConfig = File.ReadAllText(Path.Combine(root, ".releaserc.json"));

        workflow.ShouldContain("workflow_dispatch:");
        workflow.ShouldContain("DISPATCH_REF");
        workflow.ShouldContain("refs/heads/main");
        workflow.ShouldContain("release_contract.py select-ci");
        workflow.ShouldContain("dependency_handoff.py verify-ci");
        workflow.ShouldContain("dependency-release-handoff-");
        workflow.ShouldContain("emit-verification-handoff");
        workflow.ShouldContain("create-release");
        workflow.ShouldContain("release-verification-handoff-");
        workflow.ShouldContain("status=completed");
        workflow.ShouldContain(".immutable == true");
        workflow.ShouldContain("(.assets | length > 0)");
        workflow.ShouldContain("--require-immutable");
        workflow.ShouldContain("materialize-release-assets");
        workflow.ShouldContain("AD-15 create-release cannot soft-defer after publication");
        workflow.ShouldContain("AD-15 cannot soft-succeed after publication when AD-13 CI handoff is unavailable");
        File.ReadAllText(Path.Combine(root, "eng/release_contract.py")).ShouldContain("conclusion");
        workflow.ShouldContain("environment: production");
        workflow.ShouldContain("group: release-production");
        workflow.ShouldContain("cancel-in-progress: false");
        ExtractOnBlock(workflow).ShouldNotContain("push:");
        ExtractOnBlock(workflow).ShouldNotContain("workflow_run:");
        workflow.ShouldNotContain("HEXALITH_RELEASE_PUBLISH_ENABLED");

        releaseConfig.ShouldContain("\"branches\": [\"main\"]");
        releaseConfig.ShouldContain("\"tagFormat\": \"v${version}\"");
        releaseConfig.ShouldContain("python3 eng/release_prepublish.py restore --version ${nextRelease.version}");
        releaseConfig.ShouldContain("python3 eng/release_prepublish.py verify-prepared --version ${nextRelease.version}");
        releaseConfig.ShouldContain("python3 eng/release_prepublish.py publish --version ${nextRelease.version}");
        releaseConfig.ShouldContain("@semantic-release/github");
        releaseConfig.ShouldNotContain("\"@semantic-release/git\"");
        releaseConfig.ShouldNotContain("\"@semantic-release/changelog\"");
        releaseConfig.ShouldNotContain("pack_release_packages.py");
        releaseConfig.ShouldNotContain("dotnet nuget push");
        releaseConfig.ShouldNotContain("--skip-duplicate");
        releaseConfig.ShouldNotContain("RELEASE_DRY_RUN");
        releaseConfig.ShouldNotContain("gh attestation");
        releaseConfig.IndexOf("release_prepublish.py restore", StringComparison.Ordinal).ShouldBeLessThan(
            releaseConfig.IndexOf("release_prepublish.py publish", StringComparison.Ordinal));
    }

    [Fact]
    public void ReleaseWorkflow_PinsBuildsHostedPublicationFreezeContract() {
        // REL-4 supersession: standing freeze lives in the pinned Builds publisher, not a caller
        // freeze-guard. Load domain-release.yml bytes via `git show {uses-sha}:...` so the tested
        // artifact is the exact SHA release.yml pins — not a divergent submodule working tree.
        string root = RepositoryRoot();
        string workflow = File.ReadAllText(Path.Combine(root, ".github/workflows/release.yml"));
        MatchCollection domainReleasePins = Regex.Matches(
            workflow,
            @"uses: Hexalith/Hexalith\.Builds/\.github/workflows/domain-release\.yml@(?<sha>[0-9a-f]{40})\b");
        domainReleasePins.Count.ShouldBe(
            1,
            "release.yml must pin exactly one domain-release.yml uses SHA for the Builds freeze contract.");
        string buildsSha = domainReleasePins[0].Groups["sha"].Value;

        string buildsRoot = Path.Combine(root, "references", "Hexalith.Builds");
        Directory.Exists(buildsRoot).ShouldBeTrue(
            "references/Hexalith.Builds must be present so governance can resolve the pinned publisher bytes.");
        ProcessResult shown = RunProcess(buildsRoot, "git", [
            "show",
            $"{buildsSha}:.github/workflows/domain-release.yml",
        ]);
        shown.ExitCode.ShouldBe(
            0,
            $"git show {buildsSha}:.github/workflows/domain-release.yml failed: {shown.Error}");
        string domainRelease = shown.Output.Replace("\r\n", "\n", StringComparison.Ordinal);
        string executableDomainRelease = StripYamlComments(domainRelease);

        MatchCollection freezeSteps = Regex.Matches(
            domainRelease,
            @"(?ms)^      - name: Resolve release publication freeze\n        id: publish-gate\n        shell: bash\n        env:\n          HEXALITH_RELEASE_PUBLISH_ENABLED: \$\{\{ vars\.HEXALITH_RELEASE_PUBLISH_ENABLED \}\}\n        run: \|.*?(?=^      - name: |\z)");
        freezeSteps.Count.ShouldBe(
            2,
            "pinned domain-release.yml must host Resolve release publication freeze / publish-gate with vars.HEXALITH_RELEASE_PUBLISH_ENABLED on both release and governed-release paths.");
        foreach (Match freezeStep in freezeSteps) {
            freezeStep.Value.ShouldContain(
                """if [ "${HEXALITH_RELEASE_PUBLISH_ENABLED-}" = "true" ]; then""",
                customMessage: "each publish-gate run body must exact-match HEXALITH_RELEASE_PUBLISH_ENABLED to true.");
            freezeStep.Value.ShouldContain(
                "echo \"publish-enabled=false\" >> \"$GITHUB_OUTPUT\"",
                customMessage: "each publish-gate run body must emit publish-enabled=false on the non-true path.");
            freezeStep.Value.ShouldContain(
                "::notice title=Release publication frozen::",
                customMessage: "each publish-gate run body must emit the frozen-path notice.");
        }

        MatchCollection semanticReleaseSteps = Regex.Matches(
            domainRelease,
            @"(?m)^      - name: Semantic Release\n");
        MatchCollection semanticReleaseGates = Regex.Matches(
            domainRelease,
            @"(?m)^      - name: Semantic Release\n(?:        #.*\n)*        if: \$\{\{ steps\.publish-gate\.outputs\.publish-enabled == 'true'(?: && steps\.governed-candidate\.outputs\.release-required == 'true')? \}\}\n");
        semanticReleaseSteps.Count.ShouldBe(
            semanticReleaseGates.Count,
            "every Semantic Release step in the pinned publisher must be gated on publish-enabled == 'true'.");
        semanticReleaseGates.Count.ShouldBe(
            2,
            "pinned domain-release.yml must gate both Semantic Release steps on publish-enabled == 'true'.");
        semanticReleaseGates.Cast<Match>().Count(static match =>
                match.Value.Contains("steps.governed-candidate.outputs.release-required == 'true'", StringComparison.Ordinal))
            .ShouldBe(1, "governed-release Semantic Release must also require release-required == 'true'.");
        semanticReleaseGates.Cast<Match>().Count(static match =>
                !match.Value.Contains("steps.governed-candidate.outputs.release-required == 'true'", StringComparison.Ordinal))
            .ShouldBe(1, "non-governed release Semantic Release must require only publish-enabled == 'true'.");

        MatchCollection semanticReleaseRuns = Regex.Matches(
            executableDomainRelease,
            @"npx semantic-release");
        semanticReleaseRuns.Count.ShouldBeGreaterThanOrEqualTo(
            2,
            "pinned domain-release.yml must invoke npx semantic-release in executable content on both publisher paths.");
        foreach (Match run in semanticReleaseRuns) {
            int lookbackStart = Math.Max(0, run.Index - 2500);
            string preceding = executableDomainRelease[lookbackStart..run.Index];
            preceding.ShouldContain(
                "steps.publish-gate.outputs.publish-enabled == 'true'",
                customMessage: "every npx semantic-release invocation must sit under a publish-enabled == 'true' step if.");
        }
    }

    [Fact]
    public void ReleaseWorkflow_RetiresFreezeOnlyThroughOperatorProductionMigration() {
        string root = RepositoryRoot();
        string workflow = File.ReadAllText(Path.Combine(root, ".github/workflows/release.yml"));
        string normalized = workflow.Replace("\r\n", "\n", StringComparison.Ordinal);

        normalized.ShouldNotContain("freeze-guard:");
        normalized.ShouldNotContain("HEXALITH_RELEASE_PUBLISH_ENABLED");
        normalized.ShouldContain("workflow_dispatch:");
        normalized.ShouldContain("environment: production");
        normalized.ShouldContain("environment-name: production");
        normalized.ShouldContain("Revalidate current main before using protected credentials");
        normalized.ShouldContain("No releasable commits were found");
        normalized.ShouldContain("Require a non-draft release tag resolving to the dispatched SHA");
    }

    [Fact]
    public void Workflows_HaveNoPublishPathOutsideGatedReleaseWorkflow() {
        // REL-4 (2026-07-15): the freeze gate is only meaningful if release.yml is the ONLY
        // publish path. Scan every repository-owned workflow: only release.yml may reference the
        // reusable domain-release.yml, and no workflow may execute `npx semantic-release` or
        // `dotnet nuget push` itself. Assertions target executable content (comments stripped):
        // those strings legitimately appear in workflow comments today. Review VG (2026-07-18):
        // GitHub Actions also loads `.yaml` workflow files — enumerate both extensions so a
        // future `.yaml` workflow cannot evade the only-publish-path pin.
        string root = RepositoryRoot();
        string workflowsDir = Path.Combine(root, ".github/workflows");
        foreach (string workflowPath in Directory.EnumerateFiles(workflowsDir, "*.yml")
                     .Concat(Directory.EnumerateFiles(workflowsDir, "*.yaml"))) {
            string name = Path.GetFileName(workflowPath);
            string executable = StripYamlComments(File.ReadAllText(workflowPath));

            if (name == "release.yml") {
                executable.ShouldContain(
                    "domain-release.yml",
                    customMessage: "release.yml must delegate publication to the reusable domain-release.yml.");
            }
            else {
                executable.ShouldNotContain(
                    "domain-release.yml",
                    customMessage: $"{name} must not reference the reusable publish workflow; release.yml is the only gated publish path.");
            }

            executable.ShouldNotContain(
                "npx semantic-release",
                customMessage: $"{name} must not run semantic-release directly; publication happens only through the gated release.yml delegation.");
            executable.ShouldNotContain(
                "dotnet nuget push",
                customMessage: $"{name} must not push packages directly; publication happens only through the gated release.yml delegation.");
        }
    }

    private static string ExtractReleaseJobCondition(string normalizedWorkflow) {
        int releaseJob = normalizedWorkflow.IndexOf("\n  release:\n", StringComparison.Ordinal);
        releaseJob.ShouldBeGreaterThanOrEqualTo(0, "release.yml must define a release job.");
        int conditionStart = normalizedWorkflow.IndexOf("if: >-", releaseJob, StringComparison.Ordinal);
        conditionStart.ShouldBeGreaterThanOrEqualTo(0, "the release job must carry a multi-line if: condition.");
        int conditionEnd = normalizedWorkflow.IndexOf("permissions:", conditionStart, StringComparison.Ordinal);
        conditionEnd.ShouldBeGreaterThanOrEqualTo(0, "the release job condition must precede its permissions block.");
        return normalizedWorkflow[conditionStart..conditionEnd];
    }

    [Fact]
    public void ReleaseEvidenceWorkflow_IndependentlyVerifiesPublishedArtifacts() {
        // REL-3 (2026-07-18): the FR24 evidence chain moved into the pre-publication
        // orchestrator (eng/release_prepublish.py, via .releaserc.json), which packs once,
        // seals, and classifies with --require-publishable BEFORE any publication
        // side effect and attaches the durable evidence at initial GitHub Release creation
        // (AC12). The supplemental workflow is now the INDEPENDENT verifier: it downloads
        // the published GitHub Release assets and the published NuGet bytes, verifies
        // NuGet.org repository signatures, and compares the package payload with the
        // sealed candidate (AC13). It runs on Release completion regardless of
        // conclusion (AC19) and records partial-publication incidents (AC14). It must not
        // rebuild, repack, sign, classify, or attest — reconstructed evidence can never
        // establish the identity of published bytes (the v3.2.1/v3.2.2 lesson).
        string root = RepositoryRoot();
        string workflow = File.ReadAllText(Path.Combine(root, ".github/workflows/release-evidence.yml"));
        string releaseConfig = File.ReadAllText(Path.Combine(root, ".releaserc.json"));
        string executable = StripYamlComments(workflow);

        workflow.ShouldContain("workflow_run:");
        workflow.ShouldContain("workflows: [Release]");
        workflow.ShouldContain("types: [completed]");
        executable.ShouldNotContain("github.event.workflow_run.conclusion == 'success'");
        workflow.ShouldContain("release_disposition.py classify");
        workflow.ShouldContain("ref: ${{ steps.disposition.outputs.candidate }}");
        workflow.ShouldNotContain("ref: ${{ github.event.workflow_run.head_sha }}");

        string dispositionHelper = File.ReadAllText(Path.Combine(root, "eng/release_disposition.py"));
        dispositionHelper.ShouldContain("no-releasable-commits");
        dispositionHelper.ShouldContain("rejected-before-publication");
        dispositionHelper.ShouldContain("governed-publication-attempt");
        dispositionHelper.ShouldContain("release / release");

        // AC13: independent download + verification of the published bytes.
        executable.ShouldContain("gh release download");
        executable.ShouldContain("api.nuget.org/v3-flatcontainer");
        executable.ShouldContain("dotnet nuget verify");
        executable.ShouldContain("release_evidence.py verify-manifest");
        executable.ShouldContain("release_evidence.py partial-publish-incident");
        workflow.ShouldContain("published-byte-comparison.json");
        workflow.ShouldContain("ledger-record.json");

        // AC12 negative: the durable evidence chain must be present on the release itself;
        // a short-retention Actions artifact alone fails the criterion.
        workflow.ShouldContain("sealed-manifest.json");
        workflow.ShouldContain("release-readiness.json");
        workflow.ShouldContain("published-repository-signatures.txt");
        workflow.ShouldContain("published-repository-signatures.json");

        // No reconstruction: the verifier must not re-run any part of the evidence
        // production chain the pre-publication orchestrator owns.
        executable.ShouldNotContain("pack-release-packages.py");
        executable.ShouldNotContain("pack_release_packages.py");
        executable.ShouldNotContain("dotnet build");
        executable.ShouldNotContain("dotnet nuget sign");
        executable.ShouldNotContain("attest-build-provenance");
        executable.ShouldNotContain("CycloneDX");
        executable.ShouldNotContain("llm_benchmark.py");
        executable.ShouldNotContain("release_evidence.py prepare-manifest");
        executable.ShouldNotContain("release_evidence.py seal-manifest");
        executable.ShouldNotContain("release_evidence.py classify-release");
        executable.ShouldNotContain("--require-publishable");

        // Read-only lane: no release-mutation permissions or paths, no dispatch/dry-run,
        // no best-effort suppression.
        workflow.ShouldContain("permissions:");
        workflow.ShouldContain("contents: read");
        workflow.ShouldNotContain("contents: write");
        workflow.ShouldNotContain("attestations: write");
        workflow.ShouldNotContain("id-token: write");
        executable.ShouldNotContain("gh release upload");
        workflow.ShouldNotContain("workflow_dispatch:");
        workflow.ShouldNotContain("RELEASE_DRY_RUN");
        workflow.ShouldNotContain("|| true");
        workflow.ShouldNotContain("continue-on-error: true");

        // Fail-closed forensic artifact upload + root-only submodule init (also enforced
        // by Workflows_UseRootLevelSubmodulesOnly).
        workflow.ShouldContain("Upload verification evidence artifact");
        workflow.ShouldContain("verification-evidence/**");
        workflow.ShouldContain("if-no-files-found: error");
        workflow.ShouldContain("submodules: false");
        workflow.ShouldContain("Initialize exact root-declared dependencies");

        workflow.ShouldContain("frontcomposer.release-run-disposition.v2");
        workflow.ShouldContain("release-verification-handoff-");
        workflow.ShouldContain("dependency_handoff.py verify-release");
        dispositionHelper.ShouldContain("emit-verification-handoff");
        workflow.ShouldContain("prepared-candidate.json");
        workflow.ShouldContain("GitHub Release tag does not resolve to the dispatched source SHA");
        releaseConfig.ShouldNotContain("classify-release");
        releaseConfig.ShouldNotContain("CycloneDX");

        // Runtime proof: completed release / release topology must classify as governed.
        string sha = new string('a', 40);
        string workRoot = Path.Combine(Path.GetTempPath(), $"fc-disposition-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workRoot);
        try {
            string runPath = Path.Combine(workRoot, "upstream-run.json");
            string jobsPath = Path.Combine(workRoot, "upstream-jobs.json");
            string outputPath = Path.Combine(workRoot, "release-disposition.json");
            File.WriteAllText(runPath, $$"""
            {
              "id": 42,
              "run_attempt": 1,
              "event": "workflow_dispatch",
              "status": "completed",
              "conclusion": "success",
              "head_branch": "main",
              "head_sha": "{{sha}}",
              "path": ".github/workflows/release.yml"
            }
            """);
            File.WriteAllText(jobsPath, $$"""
            {
              "total_count": 7,
              "jobs": [
                {"name":"verify-source","status":"completed","conclusion":"success"},
                {"name":"plan-release","status":"completed","conclusion":"success"},
                {"name":"prepare-candidate","status":"completed","conclusion":"success"},
                {"name":"release","status":"completed","conclusion":"success"},
                {"name":"release / release","status":"completed","conclusion":"success"},
                {"name":"verify-publication","status":"completed","conclusion":"success"},
                {"name":"emit-verification-handoff","status":"completed","conclusion":"success"}
              ]
            }
            """);
            ProcessResult disposition = RunPython(root, [
                "eng/release_disposition.py",
                "classify",
                "--run", runPath,
                "--jobs", jobsPath,
                "--expected-run-id", "42",
                "--expected-run-attempt", "1",
                "--expected-conclusion", "success",
                "--expected-head-sha", sha,
                "--output", outputPath,
            ]);
            disposition.ExitCode.ShouldBe(0, disposition.Error);
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(outputPath));
            doc.RootElement.GetProperty("governed_attempt").GetBoolean().ShouldBeTrue();
            doc.RootElement.GetProperty("status").GetString().ShouldBe("governed-publication-attempt");
        }
        finally {
            Directory.Delete(workRoot, recursive: true);
        }
    }

    [Fact]
    public void ReleaseEvidenceWorkflow_MissingPublicationFailsGovernedAttemptClosed() {
        string root = RepositoryRoot();
        string workflow = StripYamlComments(
            File.ReadAllText(Path.Combine(root, ".github/workflows/release-evidence.yml")));

        workflow.ShouldContain("if ! gh api \"repos/${GITHUB_REPOSITORY}/releases/tags/${RELEASE_TAG}\"");
        workflow.ShouldContain("A governed publication attempt produced no GitHub Release");
        workflow.ShouldContain("partial-publish-incident.json");
        workflow.ShouldContain("exit 1");
    }

    [Fact]
    public void ReleaseEvidenceWorkflow_TagResolverRequiresExactDispatchedSha() {
        string root = RepositoryRoot();
        string workflow = StripYamlComments(File.ReadAllText(Path.Combine(root, ".github/workflows/release-evidence.yml")));
        workflow.ShouldContain("git/ref/tags/${RELEASE_TAG}");
        workflow.ShouldContain("object_type");
        workflow.ShouldContain("object_sha");
        workflow.ShouldContain("[ \"$object_sha\" != \"$EXPECTED_SHA\" ]");
        workflow.ShouldContain("failed_phase:$phase");
    }

    [Fact]
    public void ReleaseEvidenceScript_EmitsApprovalMatrixAndPackageSetFingerprint() {
        // CR-12-4-D7 (round-5): the AC26 approval matrix must be a machine-readable
        // top-level field of the classify-release output. CR-12-4-D8 (round-5): the
        // separate `package_set_fingerprint` field lets consumers tell "package set
        // changed" apart from generic release-definition drift.
        string root = RepositoryRoot();
        string fixtures = Path.Combine(root, "tests/ci-governance/fixtures/release-readiness-cases.json");
        string output = Path.Combine(Path.GetTempPath(), $"fc-classify-fixtures-{Guid.NewGuid():N}.json");
        ProcessResult result = RunPython(root, [
            "eng/release_evidence.py",
            "classify-fixtures",
            "--fixtures", fixtures,
            "--root", root,
            "--output", output,
        ]);
        result.ExitCode.ShouldBe(0);
        // Classify a single trusted-ready case via classify-release to inspect the
        // top-level payload that the workflow gates publish on.
        string evidence = Path.Combine(Path.GetTempPath(), $"fc-classify-trusted-{Guid.NewGuid():N}.json");
        string trusted = """
        {
          "approval": {"approved": true, "approver": "release-owner", "mechanism": "workflow_dispatch"},
          "attestation": {"status": "attested"},
          "checks": {
            "checksums_status": "valid", "concurrent_same_version": false,
            "dry_run_side_effect_attempt": false, "helper_state": "success",
            "inventory_status": "valid", "paths_status": "normalized",
            "post_seal_artifact_mutation": false, "recursive_submodule_command": false,
            "redaction_status": "passed", "release_definition_drift": false,
            "sbom_status": "present", "semantic_release_state": "matches",
            "test_count": 42, "test_status": "passed", "trx_present": true
          },
          "context": {
            "dry_run": false, "event_name": "workflow_dispatch", "from_fork": false,
            "partial_publish_state": "none", "ref": "refs/heads/main",
            "ref_protected": true, "run_attempt": 1
          },
          "manifest": {}
        }
        """;
        File.WriteAllText(evidence, trusted);
        string decisionPath = Path.Combine(Path.GetTempPath(), $"fc-decision-{Guid.NewGuid():N}.json");
        ProcessResult classify = RunPython(root, [
            "eng/release_evidence.py",
            "classify-release",
            "--root", root,
            "--evidence", evidence,
            "--output", decisionPath,
        ]);
        // CR-12-4-P138 (round-6): expect either 0 (rejected unconditionally because
        // manifest is empty → `blocked`) or 1 (require-publishable rejection). Exit-2
        // would indicate a helper crash before the readiness JSON is written and the
        // test must NOT silently accept that. The prior assertion `File.Exists` alone
        // could pass even when classifier crashed before writing.
        classify.ExitCode.ShouldBeOneOf(0, 1);
        File.Exists(decisionPath).ShouldBeTrue();
        using var decision = JsonDocument.Parse(File.ReadAllText(decisionPath));
        JsonElement root_el = decision.RootElement;
        root_el.TryGetProperty("approval_matrix", out JsonElement matrix).ShouldBeTrue();
        matrix.ValueKind.ShouldBe(JsonValueKind.Array);
        matrix.GetArrayLength().ShouldBe(7);
        // Each row must carry the AC26-required fields plus the round-6 gate_id and
        // fallback_action additions (CR-12-4-P136/P140).
        foreach (JsonElement row in matrix.EnumerateArray()) {
            row.TryGetProperty("action", out _).ShouldBeTrue();
            row.TryGetProperty("gate_id", out _).ShouldBeTrue();
            row.TryGetProperty("owner", out _).ShouldBeTrue();
            row.TryGetProperty("mechanism", out _).ShouldBeTrue();
            row.TryGetProperty("evidence", out _).ShouldBeTrue();
            row.TryGetProperty("effect", out JsonElement effect).ShouldBeTrue();
            // Effect vocabulary must be one of the normalized set; old
            // `blocking-with-approved-unsupported-fallback` is gone (P140).
            effect.GetString().ShouldBeOneOf("blocking", "blocking-with-fallback", "fallback");
            row.TryGetProperty("fallback_action", out _).ShouldBeTrue();
            // CR-12-4-P179 (round-7): structured mechanism_inputs per row.
            row.TryGetProperty("mechanism_inputs", out JsonElement mechInputs).ShouldBeTrue();
            mechInputs.ValueKind.ShouldBe(JsonValueKind.Array);
            mechInputs.GetArrayLength().ShouldBeGreaterThan(0);
        }
        root_el.TryGetProperty("package_set_fingerprint", out JsonElement packageSet).ShouldBeTrue();
        // CR-12-4-P182 (round-7): the field may serialize to JSON null when the inventory
        // file is absent on disk; `.GetString()` on a JSON null raises
        // InvalidOperationException with a confusing error. Assert the value kind
        // explicitly first so the test produces a clean failure.
        packageSet.ValueKind.ShouldBeOneOf(JsonValueKind.String, JsonValueKind.Null);
        if (packageSet.ValueKind == JsonValueKind.String) {
            packageSet.GetString().ShouldNotBeNullOrEmpty();
        }
    }

    [Fact]
    public void ReleaseEvidenceScript_TestResults_FailsClosedOnTrxFailedCounter() {
        // CR-12-4-P92 (round-5): TRX `failed`/`error`/`aborted`/`timeout` counters now
        // fail closed. Previously only `executed <= 0` blocked, so a run with
        // `executed=100, failed=100` classified as `valid` and bypassed AC3.
        string root = RepositoryRoot();
        string trxDir = Path.Combine(Path.GetTempPath(), $"fc-trx-failed-{Guid.NewGuid():N}");
        Directory.CreateDirectory(trxDir);
        string trxBody = """
        <?xml version="1.0" encoding="UTF-8"?>
        <TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
          <ResultSummary>
            <Counters total="3" executed="3" passed="2" failed="1" error="0" aborted="0" timeout="0" />
          </ResultSummary>
        </TestRun>
        """;
        File.WriteAllText(Path.Combine(trxDir, "release-results.trx"), trxBody);
        string output = Path.Combine(Path.GetTempPath(), $"fc-test-results-{Guid.NewGuid():N}.json");
        ProcessResult result = RunPython(root, [
            "eng/release_evidence.py",
            "test-results",
            "--results-dir", trxDir,
            "--output", output,
        ]);
        result.ExitCode.ShouldNotBe(0);
        using var doc = JsonDocument.Parse(File.ReadAllText(output));
        doc.RootElement.GetProperty("status").GetString().ShouldBe("invalid");
        string diagnostics = doc.RootElement.GetProperty("diagnostics").ToString();
        diagnostics.ShouldContain("failed test");
    }

    [Theory]
    [InlineData("error", 1, 0, 0, "error test")]
    [InlineData("aborted", 0, 1, 0, "aborted test")]
    [InlineData("timeout", 0, 0, 1, "timed-out test")]
    public void ReleaseEvidenceScript_TestResults_FailsClosedOnTrxNonFailedCounters(
        string label, int errorCount, int abortedCount, int timeoutCount, string expectedDiag) {
        // CR-12-4-P176 (round-7): the prior `..._FailsClosedOnTrxFailedCounter` covered
        // only the `failed=1` axis. Lock the round-5 P92 contract for every other
        // counter that AC3 expects to fail closed (`error`/`aborted`/`timeout`).
        string root = RepositoryRoot();
        string trxDir = Path.Combine(Path.GetTempPath(), $"fc-trx-{label}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(trxDir);
        string trxBody = $$"""
        <?xml version="1.0" encoding="UTF-8"?>
        <TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
          <ResultSummary>
            <Counters total="3" executed="3" passed="2" failed="0" error="{{errorCount}}" aborted="{{abortedCount}}" timeout="{{timeoutCount}}" />
          </ResultSummary>
        </TestRun>
        """;
        File.WriteAllText(Path.Combine(trxDir, "release-results.trx"), trxBody);
        string output = Path.Combine(Path.GetTempPath(), $"fc-test-results-{label}-{Guid.NewGuid():N}.json");
        ProcessResult result = RunPython(root, [
            "eng/release_evidence.py",
            "test-results",
            "--results-dir", trxDir,
            "--output", output,
        ]);
        result.ExitCode.ShouldNotBe(0);
        using var doc = JsonDocument.Parse(File.ReadAllText(output));
        doc.RootElement.GetProperty("status").GetString().ShouldBe("invalid");
        string diagnostics = doc.RootElement.GetProperty("diagnostics").ToString();
        diagnostics.ShouldContain(expectedDiag);
        // CR-12-4-P167 (round-7): the typed per-category counter fields are now part
        // of the contract.
        doc.RootElement.GetProperty("error_count").GetInt32().ShouldBe(errorCount);
        doc.RootElement.GetProperty("aborted_count").GetInt32().ShouldBe(abortedCount);
        doc.RootElement.GetProperty("timeout_count").GetInt32().ShouldBe(timeoutCount);
    }

    [Fact]
    public void ReleaseEvidenceScript_TestResults_FailsClosedOnSkippedTests() {
        // CR-12-4-P226 (round-9, BH-037): lock the round-8 P196 skipped-test contract.
        // A TRX with executed < total (e.g., executed=50, total=100) used to classify
        // `test_status: passed` and `test_count: 50` because the per-counter gates only
        // checked failed/error/aborted/timeout. P196 added a typed diagnostic that
        // surfaces the skip count and folds it into blocking. A regression that drops
        // the executed-vs-total comparison would otherwise slip past CI.
        string root = RepositoryRoot();
        string trxDir = Path.Combine(Path.GetTempPath(), $"fc-trx-skipped-{Guid.NewGuid():N}");
        Directory.CreateDirectory(trxDir);
        const string trxBody = """
        <?xml version="1.0" encoding="UTF-8"?>
        <TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
          <ResultSummary>
            <Counters total="100" executed="50" passed="50" failed="0" error="0" aborted="0" timeout="0" />
          </ResultSummary>
        </TestRun>
        """;
        File.WriteAllText(Path.Combine(trxDir, "release-results.trx"), trxBody);
        string output = Path.Combine(Path.GetTempPath(), $"fc-test-results-skipped-{Guid.NewGuid():N}.json");
        ProcessResult result = RunPython(root, [
            "eng/release_evidence.py",
            "test-results",
            "--results-dir", trxDir,
            "--output", output,
        ]);
        result.ExitCode.ShouldNotBe(0);
        using var doc = JsonDocument.Parse(File.ReadAllText(output));
        doc.RootElement.GetProperty("status").GetString().ShouldBe("invalid");
        string diagnostics = doc.RootElement.GetProperty("diagnostics").ToString();
        diagnostics.ShouldContain("skipped");
    }

    [Fact]
    public void ReleaseEvidenceScript_SealAndVerifyManifest_RoundTripsCleanly() {
        string root = RepositoryRoot();
        string tempRoot = Path.Combine(Path.GetTempPath(), $"fc-seal-roundtrip-{Guid.NewGuid():N}");
        try {
            RunPython(root, [
                "tests/ci-governance/stage_release_state.py",
                "publish",
                root,
                tempRoot,
            ]).ExitCode.ShouldBe(0);

            string preManifest = Path.Combine(tempRoot, "pre-manifest.json");
            string sealedManifest = Path.Combine(tempRoot, "release-evidence", "sealed-manifest.json");
            string output = Path.Combine(tempRoot, "verification.json");
            Dictionary<string, JsonElement> unsealed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
                File.ReadAllText(sealedManifest))!;
            unsealed.Remove("seal");
            File.WriteAllText(preManifest, JsonSerializer.Serialize(unsealed));

            RunPython(root, [
                "eng/release_evidence.py",
                "seal-manifest",
                "--manifest", preManifest,
                "--output", sealedManifest,
            ]).ExitCode.ShouldBe(0);

            ProcessResult result = RunPython(root, [
                "eng/release_evidence.py",
                "verify-manifest",
                "--root", tempRoot,
                "--graph-root", tempRoot,
                "--manifest", sealedManifest,
                "--output", output,
            ]);

            result.ExitCode.ShouldBe(0, $"verify-manifest must succeed for a clean round-trip; got: {File.ReadAllText(output)}");
            using var verification = JsonDocument.Parse(File.ReadAllText(output));
            verification.RootElement.GetProperty("status").GetString().ShouldBe("valid");
        }
        finally {
            if (Directory.Exists(tempRoot)) {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void ReleaseEvidenceScript_FallbackComplete_RejectsMalformedDigest() {
        // CR-12-4-P108 (round-5): fallback `approved_against_fingerprints_sha256` must be
        // a well-formed 64-char hex sha256 string. A malformed value now produces a
        // typed `malformed-fallback-digest` reason instead of the generic "drifted
        // release definition" message.
        string root = RepositoryRoot();
        string fixtures = Path.Combine(root, "tests/ci-governance/fixtures/release-readiness-cases.json");
        string output = Path.Combine(Path.GetTempPath(), $"fc-classify-malformed-{Guid.NewGuid():N}.json");
        ProcessResult result = RunPython(root, [
            "eng/release_evidence.py",
            "classify-fixtures",
            "--fixtures", fixtures,
            "--root", root,
            "--output", output,
        ]);
        result.ExitCode.ShouldBe(0);
        using var doc = JsonDocument.Parse(File.ReadAllText(output));
        JsonElement results = doc.RootElement.GetProperty("results");
        bool foundCase = false;
        foreach (JsonElement c in results.EnumerateArray()) {
            if (c.GetProperty("name").GetString() == "fallback-malformed-digest") {
                foundCase = true;
                c.GetProperty("classification").GetString().ShouldBe("blocked");
                c.GetProperty("publish_authorized").GetBoolean().ShouldBeFalse();
            }
        }
        foundCase.ShouldBeTrue("fixture `fallback-malformed-digest` is required for CR-12-4-P108 coverage");
    }

    [Fact]
    public void ReleaseEvidenceScript_ClassifyRelease_FailsClosedOnConcurrencyGuardDiagnostics() {
        // CR-12-4-P225 (round-9, BH-006/BH-034/EC-30): the prior assertion
        // `ExitCode.ShouldBe(0)` did not exercise `--require-publishable`, so the exit
        // code was 0 unconditionally regardless of authorization. Add the flag and
        // assert exit 1 so a regression that drops the concurrency-probe-diagnostic
        // injection into `checks` would fail this test at the exit-code level (the
        // prior JSON-level `publish_authorized=false` assertion alone could pass even
        // if the helper returned `blocked` for a different reason).
        string root = RepositoryRoot();
        string fixtures = Path.Combine(root, "tests/ci-governance/fixtures/release-readiness-cases.json");
        using var fixtureDoc = JsonDocument.Parse(File.ReadAllText(fixtures));
        string evidencePath = Path.Combine(Path.GetTempPath(), $"fc-classify-concurrency-{Guid.NewGuid():N}.json");
        string output = Path.Combine(Path.GetTempPath(), $"fc-classify-concurrency-out-{Guid.NewGuid():N}.json");
        File.WriteAllText(evidencePath, fixtureDoc.RootElement.GetProperty("base_evidence").GetRawText());

        ProcessResult result = RunPython(root, [
            "eng/release_evidence.py",
            "classify-release",
            "--root", root,
            "--evidence", evidencePath,
            "--concurrency-guard", Path.Combine(Path.GetTempPath(), $"missing-guard-{Guid.NewGuid():N}.json"),
            "--require-publishable",
            "--output", output,
        ]);

        result.ExitCode.ShouldBe(1, result.Error);
        using var decision = JsonDocument.Parse(File.ReadAllText(output));
        decision.RootElement.GetProperty("publish_authorized").GetBoolean().ShouldBeFalse();
        string blocking = string.Join('\n', decision.RootElement.GetProperty("grouped_reasons").GetProperty("blocking").EnumerateArray().Select(r => r.GetString()));
        blocking.ShouldContain("concurrency-probe");
    }

    [Fact]
    public void ReleaseEvidenceScript_DirectEvidenceMalformedSectionsFailClosed() {
        string root = RepositoryRoot();
        string evidence = Path.Combine(Path.GetTempPath(), $"fc-classify-malformed-sections-{Guid.NewGuid():N}.json");
        string output = Path.Combine(Path.GetTempPath(), $"fc-classify-malformed-sections-out-{Guid.NewGuid():N}.json");
        File.WriteAllText(evidence, """
        {
          "approval": true,
          "attestation": [],
          "checks": {
            "checksums_status": "valid",
            "concurrent_same_version": false,
            "dry_run_side_effect_attempt": false,
            "helper_state": "success",
            "inventory_status": "valid",
            "paths_status": "normalized",
            "post_seal_artifact_mutation": false,
            "recursive_submodule_command": false,
            "redaction_status": "passed",
            "release_definition_drift": false,
            "sbom_status": "present",
            "semantic_release_state": "matches",
            "test_count": "unknown",
            "test_status": "passed",
            "trx_present": true
          },
          "context": {
            "dry_run": false,
            "event_name": "workflow_dispatch",
            "from_fork": false,
            "partial_publish_state": "none",
            "ref": "refs/heads/main",
            "ref_protected": true,
            "run_attempt": 1
          },
          "manifest": {}
        }
        """);

        ProcessResult result = RunPython(root, [
            "eng/release_evidence.py",
            "classify-release",
            "--root", root,
            "--evidence", evidence,
            "--output", output,
        ]);

        result.ExitCode.ShouldBe(0, result.Error);
        string decisionJson = File.ReadAllText(output);
        using var decision = JsonDocument.Parse(decisionJson);
        decision.RootElement.GetProperty("classification").GetString().ShouldBe("blocked");
        decisionJson.ShouldContain("approval section must be an object");
        decisionJson.ShouldContain("attestation section must be an object");
        decisionJson.ShouldContain("test_count must be numeric");
    }

    [Fact]
    public void ReleaseEvidenceScript_ClassifyRelease_DryRunCleanExit_LocalCandidate_HealthyCarveOut_ReturnsExit0() {
        // CR-12-4-P252 (round-11): fixture-level coverage of the healthy carve-out
        // CLASSIFICATION (ready / publish_authorized=false) via `classify-fixtures`,
        // which calls `classify_release_payload` directly with verify_drift=False —
        // it does NOT traverse the CLI `--dry-run-clean-exit` exit gate itself
        // (review VG-1, 2026-07-18). The exit-code contract of that gate is pinned
        // by `ReleaseModelGovernanceTests.ClassifyRelease_HealthyDryRunEvidence_
        // CleanExitGate_ReturnsExit0`, which runs the real CLI over a hermetically
        // staged healthy evidence set.
        string root = RepositoryRoot();
        string fixturesPath = Path.Combine(root, "tests/ci-governance/fixtures/release-readiness-cases.json");

        ProcessResult result = RunPython(root, [
            "eng/release_evidence.py",
            "classify-fixtures",
            "--root", root,
            "--fixtures", fixturesPath,
        ]);

        // classify-fixtures returns exit 0 only when every case's expected_*
        // matches actual. The `dry-run-from-dispatch` and `local-candidate` cases
        // both expect `classification=ready` with `publish_authorized=false` —
        // exercising the carve-out arm. The `local-candidate-not-dry-run` case
        // (added by CR-12-4-P253) expects `classification=blocked`, exercising the
        // negative path. The `dry-run-from-dispatch-fallback-approved` case (added
        // by CR-12-4-P264) expects `classification=fallback-approved`, exercising
        // the second carve-out arm.
        result.ExitCode.ShouldBe(0, result.Error);
    }

    [Fact]
    public void ReleaseEvidenceScript_ClassifyRelease_DryRunCleanExit_RealBlocker_ReturnsExit1() {
        // CR-12-4-P252 (round-11): non-carve-out case must fail-loud at exit-code level
        // so a regression that broadens the allowlist is caught. Dry-run flag is true
        // but an additional blocker (zero tests) means the carve-out's `len(blocking)==1`
        // guard does not fire — classification stays blocked, gate exits 1.
        string root = RepositoryRoot();
        string evidence = Path.Combine(Path.GetTempPath(), $"fc-classify-carveout-block-{Guid.NewGuid():N}.json");
        string output = Path.Combine(Path.GetTempPath(), $"fc-classify-carveout-block-out-{Guid.NewGuid():N}.json");
        string fixtureContent = File.ReadAllText(Path.Combine(root, "tests/ci-governance/fixtures/release-readiness-cases.json"));
        using var fixtureDoc = JsonDocument.Parse(fixtureContent);
        string baseText = fixtureDoc.RootElement.GetProperty("base_evidence").GetRawText();
        // Set dry_run=true AND test_count=0 to introduce a real blocker beyond the candidate blocker.
        string mutated = baseText
            .Replace("\"dry_run\": false", "\"dry_run\": true")
            .Replace("\"test_count\": 42", "\"test_count\": 0");
        File.WriteAllText(evidence, mutated);

        ProcessResult result = RunPython(root, [
            "eng/release_evidence.py",
            "classify-release",
            "--root", root,
            "--evidence", evidence,
            "--require-publishable",
            "--dry-run-clean-exit",
            "--output", output,
        ]);

        result.ExitCode.ShouldBe(1, result.Error);
        using var decision = JsonDocument.Parse(File.ReadAllText(output));
        decision.RootElement.GetProperty("classification").GetString().ShouldBe("blocked");
        decision.RootElement.GetProperty("publish_authorized").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    public void ReleaseBudgetSkippedMarker_HasRequiredShape() {
        // CR-12-4-P252 (round-11): assert the typed `release-budget-skipped.json`
        // marker contract shape. The marker is emitted by `.github/workflows/release.yml`
        // when `RELEASE_STARTED_AT` is empty (CR-12-4-P239 round-10 / P250 round-11);
        // it satisfies AC19's "explicitly marked unavailable" requirement. The
        // workflow uses a `python3 -c` one-liner — verify the same logic produces
        // a marker with the four required keys plus `decision_contract`.
        string markerPath = Path.Combine(Path.GetTempPath(), $"fc-budget-skipped-{Guid.NewGuid():N}.json");
        try {
            var psi = new System.Diagnostics.ProcessStartInfo {
                FileName = "python3",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            psi.ArgumentList.Add("-c");
            psi.ArgumentList.Add(
                "import datetime as dt, json, sys; " +
                "data = {'decision_contract': 'frontcomposer.release-budget-skipped.v1', " +
                "'classification': 'budget-unavailable', " +
                "'reason': 'RELEASE_STARTED_AT empty; release-budget monitor cannot compute elapsed minutes', " +
                "'skipped_at': dt.datetime.now(dt.timezone.utc).isoformat()}; " +
                $"open(r'{markerPath}', 'w', encoding='utf-8').write(json.dumps(data, sort_keys=True, separators=(',', ':')) + '\\n')"
            );
            using System.Diagnostics.Process proc = System.Diagnostics.Process.Start(psi)!;
            proc.WaitForExit();
            proc.ExitCode.ShouldBe(0, proc.StandardError.ReadToEnd());
            string content = File.ReadAllText(markerPath);
            using var marker = JsonDocument.Parse(content);
            marker.RootElement.GetProperty("decision_contract").GetString().ShouldBe("frontcomposer.release-budget-skipped.v1");
            marker.RootElement.GetProperty("classification").GetString().ShouldBe("budget-unavailable");
            string reason = marker.RootElement.GetProperty("reason").GetString() ?? string.Empty;
            reason.ShouldContain("RELEASE_STARTED_AT empty");
            marker.RootElement.TryGetProperty("skipped_at", out JsonElement skippedAt).ShouldBeTrue();
            skippedAt.GetString().ShouldNotBeNullOrEmpty();
        }
        finally {
            if (File.Exists(markerPath)) {
                File.Delete(markerPath);
            }
        }
    }

    [Fact]
    public void ReleaseEvidenceScript_ReleaseBudgetUsesManifestTagWhenAppending() {
        string root = RepositoryRoot();
        string manifest = Path.Combine(Path.GetTempPath(), $"fc-budget-manifest-{Guid.NewGuid():N}.json");
        string output = Path.Combine(Path.GetTempPath(), $"fc-budget-output-{Guid.NewGuid():N}.json");
        File.WriteAllText(manifest, """
        {
          "tag": "v9.9.9",
          "packages": [
            { "package_id": "Hexalith.FrontComposer.Contracts" }
          ]
        }
        """);

        ProcessResult result = RunPython(root, [
            "eng/release_evidence.py",
            "release-budget",
            "--evidence", Path.Combine(Path.GetTempPath(), $"missing-budget-{Guid.NewGuid():N}.json"),
            "--append-current",
            "--started-at", "2026-05-19T00:00:00Z",
            "--ended-at", "2026-05-19T00:02:00Z",
            "--manifest", manifest,
            "--tag", "main",
            "--run-id", "42",
            "--output", output,
        ]);

        result.ExitCode.ShouldBe(0, result.Error);
        using var budget = JsonDocument.Parse(File.ReadAllText(output));
        JsonElement release = budget.RootElement.GetProperty("releases").EnumerateArray().Last();
        release.GetProperty("tag").GetString().ShouldBe("v9.9.9");
        release.GetProperty("package_count").GetInt32().ShouldBe(1);
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
        script.ShouldContain("validate-mtp-evidence");
        script.ShouldContain("aggregate TRX total must be greater than zero");
        script.ShouldContain("No quarantine TRX files were found.");
        script.ShouldContain("malformed or empty Cobertura");
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
            "mtp-quarantine/nested-a/module-a.trx",
            "mtp-quarantine/nested-b/deeper/module-b.trx",
            "mtp-quarantine/zero/zero.trx",
            "mtp-quarantine/malformed/malformed.trx",
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
    public void SourceToolsMutationConfigs_AreProjectScopedAndReleaseBuilt() {
        // spec-actions-30978026706-fix-source-tools-mutation: Stryker's initial build must
        // target the SourceTools project graph only (Release), never the umbrella .slnx, so
        // it cannot hit submodule UI project file locks (e.g. references/Hexalith.Tenants).
        string root = RepositoryRoot();
        foreach (string configPath in new[] {
            "tests/Hexalith.FrontComposer.SourceTools.Tests/Mutation/stryker-happy-path.json",
            "tests/Hexalith.FrontComposer.SourceTools.Tests/Mutation/stryker-error-handling.json",
        }) {
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, configPath)));
            JsonElement strykerConfig = doc.RootElement.GetProperty("stryker-config");
            strykerConfig.TryGetProperty("solution", out _).ShouldBeFalse(
                $"{configPath} must stay project-scoped (no umbrella '.slnx' Stryker build).");
            strykerConfig.GetProperty("configuration").GetString().ShouldBe("Release");
            strykerConfig.GetProperty("project").GetString().ShouldBe(
                "src/Hexalith.FrontComposer.SourceTools/Hexalith.FrontComposer.SourceTools.csproj");
            JsonElement testProjects = strykerConfig.GetProperty("test-projects");
            testProjects.EnumerateArray().Select(element => element.GetString()).ShouldContain(
                "tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj");
        }
    }

    [Fact]
    public void ValidateStrykerReportsScript_RejectsReintroducedSolutionAndMissingConfiguration() {
        // Behavioral replacement for a brittle `ShouldNotContain("\"solution\"")` script-text
        // check: the script legitimately contains the word "solution" now (in the forbid-solution
        // Add-Failure message), so assert the actual validation outcome instead of raw script text.
        string root = RepositoryRoot();
        string workDir = Path.Combine(root, $"artifacts/mutation-governance-config-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workDir);

        try {
            string mutatedConfigPath = Path.Combine(workDir, "stryker-happy-path-regressed.json");
            using (JsonDocument original = JsonDocument.Parse(File.ReadAllText(
                Path.Combine(root, "tests/Hexalith.FrontComposer.SourceTools.Tests/Mutation/stryker-happy-path.json")))) {
                Dictionary<string, object?> strykerConfig = original.RootElement.GetProperty("stryker-config")
                    .EnumerateObject()
                    .Where(property => property.Name != "configuration")
                    .ToDictionary(property => property.Name, property => (object?)JsonSerializer.Deserialize<JsonElement>(property.Value.GetRawText()));
                strykerConfig["solution"] = "Hexalith.FrontComposer.slnx";
                File.WriteAllText(mutatedConfigPath, JsonSerializer.Serialize(new Dictionary<string, object?> {
                    ["stryker-config"] = strykerConfig,
                }));
            }

            string manifestPath = Path.Combine(workDir, "manifest.json");
            File.WriteAllText(manifestPath, JsonSerializer.Serialize(new Dictionary<string, object?> {
                ["schemaVersion"] = "1.0",
                ["ownerStory"] = "governance-fixture",
                ["approvedTargetRoots"] = ApprovedStrykerTargetRoots,
                ["segments"] = new[] {
                    new Dictionary<string, object?> {
                        ["name"] = "happy-path",
                        ["config"] = Path.GetRelativePath(root, mutatedConfigPath).Replace('\\', '/'),
                        ["threshold"] = 80,
                        ["artifactPrefix"] = "source-tools-happy-path",
                        ["minimumMutantCount"] = 1,
                    },
                },
                ["explicitExclusions"] = Array.Empty<object>(),
                ["triageActions"] = StrykerTriageActions,
                ["problemMutantTriage"] = Array.Empty<object>(),
            }));

            ProcessResult result = RunPwsh(root, [
                "-NoProfile",
                "-NonInteractive",
                "-File", "eng/validate-stryker-reports.ps1",
                "-ManifestPath", Path.GetRelativePath(root, manifestPath).Replace('\\', '/'),
                "-ReportRoot", Path.GetRelativePath(root, workDir).Replace('\\', '/') + "/reports",
                "-OutputPath", Path.GetRelativePath(root, workDir).Replace('\\', '/') + "/job-summary.md",
                "-AllowMissingReports",
            ]);

            result.ExitCode.ShouldNotBe(0);
            string combined = Regex.Replace(result.Output + result.Error, @"\s+", " ");
            combined.ShouldContain("is missing 'configuration'");
            combined.ShouldContain("must not set 'solution'");
        }
        finally {
            if (Directory.Exists(workDir)) {
                Directory.Delete(workDir, recursive: true);
            }
        }
    }

    [Fact]
    public void ValidateStrykerReportsScript_SkipsTargetDriftWhenAReportIsMissing() {
        string root = RepositoryRoot();
        string reportRoot = $"artifacts/mutation-governance-{Guid.NewGuid():N}";
        string reportRootFullPath = Path.Combine(root, reportRoot);
        Directory.CreateDirectory(Path.Combine(reportRootFullPath, "happy-path"));

        // Only the happy-path segment report is present; error-handling is deliberately missing
        // so validation must fail on the missing report without fabricating target drift for
        // every approved Parsing/Transforms file.
        File.WriteAllText(
            Path.Combine(reportRootFullPath, "happy-path", "source-tools-happy-path.json"),
            """
            {
              "schemaVersion": "1.0",
              "thresholds": { "high": 80, "low": 80 },
              "files": {
                "src/Hexalith.FrontComposer.SourceTools/Parsing/GovernanceFixture.cs": {
                  "language": "cs",
                  "mutants": [
                    { "id": "1", "mutatorName": "EqualityMutator", "status": "Killed", "location": {} }
                  ]
                }
              }
            }
            """);

        try {
            ProcessResult result = RunPwsh(root, [
                "-NoProfile",
                "-NonInteractive",
                "-File", "eng/validate-stryker-reports.ps1",
                "-ReportRoot", reportRoot,
                "-OutputPath", $"{reportRoot}/job-summary.md",
            ]);

            result.ExitCode.ShouldNotBe(0);
            // PowerShell wraps long error lines to the host width, so match the message in
            // pieces rather than as one contiguous substring.
            string combined = Regex.Replace(result.Output + result.Error, @"\s+", " ");
            combined.ShouldContain("Missing JSON mutation report for segment");
            combined.ShouldContain("'error-handling'");
            combined.ShouldNotContain("Target drift:");
        }
        finally {
            if (Directory.Exists(reportRootFullPath)) {
                Directory.Delete(reportRootFullPath, recursive: true);
            }
        }
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
        using var doc = JsonDocument.Parse(File.ReadAllText(output));
        doc.RootElement.GetProperty("classification").GetString().ShouldBe("flaky");
        doc.RootElement.GetProperty("decision").GetString().ShouldBe("open-or-update-issue-and-pr");
        doc.RootElement.GetProperty("manual_patch_required").GetBoolean().ShouldBeTrue();

        ProcessResult approvedWindow = RunGovernance(root, [
            "classify-flake",
            "--evidence", "tests/ci-governance/fixtures/flake-approved-window.json",
            "--output", output,
        ]);
        approvedWindow.ExitCode.ShouldBe(0, approvedWindow.Error);
        using var approvedDoc = JsonDocument.Parse(File.ReadAllText(output));
        approvedDoc.RootElement.GetProperty("classification").GetString().ShouldBe("flaky");

        ProcessResult outsideApprovedWindow = RunGovernance(root, [
            "classify-flake",
            "--evidence", "tests/ci-governance/fixtures/flake-approved-window-outside.json",
            "--output", output,
        ]);
        outsideApprovedWindow.ExitCode.ShouldBe(0, outsideApprovedWindow.Error);
        using var outsideApprovedDoc = JsonDocument.Parse(File.ReadAllText(output));
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
        using (var doc = JsonDocument.Parse(File.ReadAllText(output))) {
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
        using (var doc = JsonDocument.Parse(File.ReadAllText(reintroOutput))) {
            doc.RootElement.GetProperty("action").GetString().ShouldBe("track");
        }

        string durationOutput = Path.Combine(Path.GetTempPath(), $"fc-duration-{Guid.NewGuid():N}.json");
        ProcessResult duration = RunGovernance(root, [
            "duration-monitor",
            "--evidence", "tests/ci-governance/fixtures/duration-breach-three-days.json",
            "--output", durationOutput,
        ]);
        duration.ExitCode.ShouldBe(0, duration.Error);
        using (var doc = JsonDocument.Parse(File.ReadAllText(durationOutput))) {
            doc.RootElement.GetProperty("action").GetString().ShouldBe("open-or-update-ci-diet-issue");
        }

        ProcessResult nonconsecutiveDuration = RunGovernance(root, [
            "duration-monitor",
            "--evidence", "tests/ci-governance/fixtures/duration-breach-nonconsecutive.json",
            "--output", durationOutput,
        ]);
        nonconsecutiveDuration.ExitCode.ShouldBe(0, nonconsecutiveDuration.Error);
        using (var doc = JsonDocument.Parse(File.ReadAllText(durationOutput))) {
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
        using (var doc = JsonDocument.Parse(File.ReadAllText(batchOutput))) {
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

    // ----------------------------------------------------------------------------
    // Story 12.4 red-phase ATDD scaffolds for genuinely-deferred Def items live in
    // the sibling `Story12_4_RedPhaseDefTests` class below. They were moved out of
    // `CiGovernanceTests` because this class carries `[Trait("Category", "Governance")]`
    // at class level, which makes Gate 2b (`--filter "Category=Governance"`) match
    // every test in the class regardless of the per-method Quarantined trait. The
    // existing `BlockingTestLanes_ExcludeQuarantinedTestsWithoutSkippingGovernance`
    // contract forbids the governance lane from excluding quarantined tests by
    // design, so the only safe placement for quarantined-but-governance-adjacent
    // tests is outside the Governance class.
    // ----------------------------------------------------------------------------

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

    internal static string ExtractNamedStep(string workflow, string name) {
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

    internal static string FindStepBlockContaining(string workflow, string needle) {
        // F4 (Story 12.4 test review): return the step block that actually contains `needle`
        // as part of its `uses:`/`run:`/body text — not just any substring anywhere in the
        // file. Step blocks start at `      - name:` or `      - uses:` (six-space indent +
        // dash). The block ends at the next six-space `- name:`/`- uses:` boundary or EOF.
        Regex stepBoundary = new(@"^      - (name|uses):", RegexOptions.Multiline);
        MatchCollection matches = stepBoundary.Matches(workflow);
        for (int i = 0; i < matches.Count; i++) {
            int start = matches[i].Index;
            int end = i + 1 < matches.Count ? matches[i + 1].Index : workflow.Length;
            string block = workflow[start..end];
            if (block.Contains(needle, StringComparison.Ordinal)) {
                return block;
            }
        }

        return string.Empty;
    }

    internal static string ExtractJobPermissionsBlock(string workflow, string jobId) {
        // F4 (Story 12.4 test review): return the contents of the named job's `permissions:`
        // block — distinct from the workflow-level permissions at column 0. Job headers are
        // two-space indented (`  release:`); the job's `permissions:` key is four-space
        // indented; permission entries are six-space indented. The block ends at the first
        // line whose indent drops below six spaces (typically `    steps:` or another
        // four-space-indented job-level key). Returns empty string if the job is absent or
        // declares no permissions.
        string jobNeedle = $"  {jobId}:";
        int jobIdx = workflow.IndexOf(jobNeedle, StringComparison.Ordinal);
        if (jobIdx < 0) {
            return string.Empty;
        }

        // Constrain the search to the job's own body — stop at the next two-space-indented
        // top-level job header so we never read into a sibling job.
        int jobBodyEnd = workflow.Length;
        Match nextJob = Regex.Match(workflow[(jobIdx + jobNeedle.Length)..], @"^  [A-Za-z][\w-]*:", RegexOptions.Multiline);
        if (nextJob.Success) {
            jobBodyEnd = jobIdx + jobNeedle.Length + nextJob.Index;
        }

        int permIdx = workflow.IndexOf("\n    permissions:", jobIdx, StringComparison.Ordinal);
        if (permIdx < 0 || permIdx >= jobBodyEnd) {
            return string.Empty;
        }

        int lineEnd = workflow.IndexOf('\n', permIdx + 1);
        if (lineEnd < 0 || lineEnd >= jobBodyEnd) {
            return string.Empty;
        }

        int start = lineEnd + 1;
        int cursor = start;
        while (cursor < jobBodyEnd) {
            int next = workflow.IndexOf('\n', cursor);
            if (next < 0 || next > jobBodyEnd) {
                next = jobBodyEnd;
            }

            string line = workflow[cursor..next];
            // Allow blank lines inside the permissions block, but stop on the first
            // non-blank line that does not start with six spaces (the permission-entry indent).
            string trimmed = line.TrimEnd('\r');
            if (trimmed.Length > 0 && !trimmed.StartsWith("      ", StringComparison.Ordinal)) {
                break;
            }

            cursor = next < jobBodyEnd ? next + 1 : jobBodyEnd;
        }

        return workflow[start..cursor];
    }

    [Fact]
    public void PrepareManifest_UnsignedCandidate_HasNoAuthorSigningContract() {
        string tempRoot = Path.Combine(Path.GetTempPath(), $"fc-unsigned-candidate-{Guid.NewGuid():N}");
        try {
            const string version = "1.2.3";
            const string packageId = "Hexalith.FrontComposer.Contracts";
            (ProcessResult result, string preManifest, string diagnostics) =
                PrepareManifestWithoutAuthorSigning(tempRoot, packageId, version);

            result.ExitCode.ShouldBe(1, "missing exact-source provenance must remain fail-closed.");
            string preparationDiagnostics = File.ReadAllText(diagnostics);
            preparationDiagnostics.ShouldContain("CI handoff");
            preparationDiagnostics.ToLowerInvariant().ShouldNotContain("signing");
            preparationDiagnostics.ToLowerInvariant().ShouldNotContain("timestamp");
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(preManifest));
            doc.RootElement.GetProperty("manifest_schema").GetString().ShouldBe("hexalith.release-evidence.v3");
            JsonElement row = doc.RootElement.GetProperty("packages")[0];
            row.GetProperty("artifact_path").GetString().ShouldBe($"nupkgs/{packageId}.{version}.nupkg");
            row.TryGetProperty("signing_status", out _).ShouldBeFalse();
            row.TryGetProperty("timestamp_status", out _).ShouldBeFalse();
        }
        finally {
            if (Directory.Exists(tempRoot)) {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    private static (ProcessResult Result, string PreManifest, string Diagnostics) PrepareManifestWithoutAuthorSigning(
        string tempRoot, string packageId, string version) {
        string root = RepositoryRoot();
        Directory.CreateDirectory(Path.Combine(tempRoot, "nupkgs"));
        Directory.CreateDirectory(Path.Combine(tempRoot, "release-evidence"));

        string nupkg = Path.Combine(tempRoot, "nupkgs", $"{packageId}.{version}.nupkg");
        string snupkg = Path.Combine(tempRoot, "nupkgs", $"{packageId}.{version}.snupkg");
        string sbom = Path.Combine(tempRoot, "release-evidence", "sbom.json");
        File.WriteAllText(nupkg, "unsigned sealed package bytes");
        File.WriteAllText(snupkg, "symbol package bytes");
        File.WriteAllText(sbom, "{\"bomFormat\":\"CycloneDX\"}");

        string inventory = Path.Combine(tempRoot, "release-evidence", "package-inventory.json");
        File.WriteAllText(inventory, JsonSerializer.Serialize(new Dictionary<string, object?> {
            ["rows"] = new[] {
                new Dictionary<string, object?> {
                    ["package_id"] = packageId,
                    ["packable"] = true,
                    ["symbol_required"] = true,
                    ["exception"] = "not-required",
                },
            },
        }));

        string checksums = Path.Combine(tempRoot, "release-evidence", "checksums.json");
        File.WriteAllText(checksums, JsonSerializer.Serialize(new Dictionary<string, object?> {
            ["files"] = new[] {
                new Dictionary<string, object?> { ["path"] = $"nupkgs/{packageId}.{version}.nupkg", ["sha256"] = Sha256File(nupkg) },
                new Dictionary<string, object?> { ["path"] = $"nupkgs/{packageId}.{version}.snupkg", ["sha256"] = Sha256File(snupkg) },
                new Dictionary<string, object?> { ["path"] = "release-evidence/sbom.json", ["sha256"] = Sha256File(sbom) },
            },
        }));

        string preManifest = Path.Combine(tempRoot, "release-evidence", "pre-manifest.json");
        string diagnostics = Path.Combine(tempRoot, "release-evidence", "prep-diagnostics.json");
        ProcessResult result = RunPython(root, [
            "eng/release_evidence.py",
            "prepare-manifest",
            "--inventory", inventory,
            "--checksums", checksums,
            "--version", version,
            "--tag", $"v{version}",
            "--sbom-hash", Sha256File(sbom),
            "--root", tempRoot,
            "--diagnostics-output", diagnostics,
            "--output", preManifest,
        ]);
        return (result, preManifest, diagnostics);
    }

    internal static string RepositoryRoot() {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null) {
            if (File.Exists(Path.Combine(dir.FullName, "Hexalith.FrontComposer.slnx"))) {
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

    internal static ProcessResult RunPython(string root, IReadOnlyList<string> arguments, IReadOnlyDictionary<string, string>? environment = null) {
        string executable = OperatingSystem.IsWindows() ? "python" : "python3";
        return RunProcess(root, executable, arguments, environment);
    }

    private static ProcessResult RunPwsh(string root, IReadOnlyList<string> arguments, IReadOnlyDictionary<string, string>? environment = null) =>
        RunProcess(root, "pwsh", arguments, environment);

    private static ProcessResult RunProcess(string root, string executable, IReadOnlyList<string> arguments, IReadOnlyDictionary<string, string>? environment = null) {
        ProcessStartInfo startInfo = new(executable) {
            WorkingDirectory = root,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };

        foreach (string argument in arguments) {
            startInfo.ArgumentList.Add(argument);
        }

        foreach ((string name, string value) in environment ?? new Dictionary<string, string>()) {
            startInfo.Environment[name] = value;
        }

        using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start governance script.");
        Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
        Task<string> errorTask = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(30_000)) {
            process.Kill(entireProcessTree: true);
            process.WaitForExit();
            return new ProcessResult(-1, outputTask.GetAwaiter().GetResult(), $"{executable} timed out");
        }

        string output = outputTask.GetAwaiter().GetResult();
        string error = errorTask.GetAwaiter().GetResult();
        return new ProcessResult(process.ExitCode, output, error);
    }

    private static string Sha256File(string path) {
        using FileStream stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static string Sha256Text(string text) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();

    private static string ExtractPythonHeredoc(string stepBlock) {
        Match match = Regex.Match(
            stepBlock,
            @"python3 - <<'PY'\r?\n(?<body>.*?)\r?\n[ \t]*PY\b",
            RegexOptions.Singleline);
        match.Success.ShouldBeTrue("step must contain a python3 <<'PY' printer heredoc.");
        return string.Join(
            '\n',
            match.Groups["body"].Value.Split('\n').Select(line => {
                string trimmed = line.TrimEnd('\r');
                return trimmed.StartsWith("          ", StringComparison.Ordinal) ? trimmed[10..] : trimmed;
            }));
    }

    private static string ExtractRunScript(string workflow, string stepName) {
        int stepStart = workflow.IndexOf($"      - name: {stepName}", StringComparison.Ordinal);
        stepStart.ShouldBeGreaterThanOrEqualTo(0, $"workflow is missing the named step '{stepName}'.");
        int runStart = workflow.IndexOf("        run: |", stepStart, StringComparison.Ordinal);
        runStart.ShouldBeGreaterThanOrEqualTo(0, $"step '{stepName}' must contain a run block.");
        int bodyStart = workflow.IndexOf('\n', runStart) + 1;
        int nextStep = workflow.IndexOf("\n      - name:", bodyStart, StringComparison.Ordinal);
        string body = workflow[bodyStart..(nextStep < 0 ? workflow.Length : nextStep)];
        return string.Join(
            '\n',
            body.Split('\n').Select(line => {
                string trimmed = line.TrimEnd('\r');
                return trimmed.StartsWith("          ", StringComparison.Ordinal) ? trimmed[10..] : trimmed;
            }));
    }

    internal sealed record ProcessResult(int ExitCode, string Output, string Error);

    [Fact]
    public void EventStoreRuntimeIdentityPinsOwnerApprovedTupleAndTruthfulDriftEvidence() {
        const string sourceSha = "38967215e6c1b13e77f2b0006efd95d88d7ad7b8";
        const string buildsSha = "2b0faab931ec581c7503270e7dd73074654e2eee";
        const string version = "3.99.0";
        // The immutable Story 11.24 owner capture remains historical evidence. The current root
        // identity is separately approved in the active implementation spec.
        const string evidenceSourceSha = "bb94d93e9b84132cff83a38fba84f25455820d31";
        const string evidenceVersion = "3.91.1";
        string root = RepositoryRoot();
        string approvalContractPath = Path.Combine(
            root,
            "_bmad-output",
            "contracts",
            "frontcomposer-eventstore-approved-runtime-identity-v1.json");
        using JsonDocument approvalContract = JsonDocument.Parse(File.ReadAllText(approvalContractPath));
        JsonElement approval = approvalContract.RootElement;
        approval.GetProperty("schema").GetString()
            .ShouldBe("hexalith.frontcomposer.eventstore-approved-runtime-identity.v1");
        approval.GetProperty("approvalRecord").GetString().ShouldBe(
            "_bmad-output/implementation-artifacts/spec-actions-33264036185-33264035739-fix-cicd-release.md");
        approval.GetProperty("eventStoreSourceGitlink").GetString().ShouldBe(sourceSha);
        approval.GetProperty("eventStorePackageVersion").GetString().ShouldBe(version);
        approval.GetProperty("buildsCatalogGitlink").GetString().ShouldBe(buildsSha);
        approval.GetProperty("submodulePointerChangedByApproval").GetBoolean().ShouldBeFalse();
        JsonElement historicalCapture = approval.GetProperty("historicalCapture");
        historicalCapture.GetProperty("path").GetString().ShouldBe(
            "_bmad-output/implementation-artifacts/evidence/frontcomposer-story-11-24");
        historicalCapture.GetProperty("eventStoreSourceSha").GetString().ShouldBe(evidenceSourceSha);
        historicalCapture.GetProperty("eventStorePackageVersion").GetString().ShouldBe(evidenceVersion);
        historicalCapture.GetProperty("immutable").GetBoolean().ShouldBeTrue();
        string quality = File.ReadAllText(Path.Combine(root, ".github/workflows/quality.yml"));
        string artifactLane = ExtractNamedStep(quality, "Gate 2c: Validate contract artifacts");
        artifactLane.ShouldContain("python3 -m unittest tests/eng/test_eventstore_runtime_evidence.py");
        // pwsh does not fail the step on a native non-zero exit, and the validator below resets
        // $LASTEXITCODE, so a red evidence suite is only fail-closed with explicit propagation.
        // The propagation must not exit with $LASTEXITCODE itself: it is $null until a native
        // command sets it, and `exit $null` exits 0, so a suite that never launched would pass.
        artifactLane.ShouldContain("if (-not $? -or $LASTEXITCODE -ne 0) { exit 1 }");
        artifactLane.ShouldNotContain("exit $LASTEXITCODE");
        artifactLane.ShouldContain("-RequireProviderVerification");
        artifactLane.ShouldContain("_bmad-output/implementation-artifacts/evidence/frontcomposer-story-11-24/provider-verification/provider-verification.json");
        artifactLane.ShouldNotContain("BLOCKED_HANDOFF");
        artifactLane.ShouldNotContain("continue-on-error: true");
        string uploadLane = ExtractNamedStep(quality, "Upload contract artifacts");
        uploadLane.ShouldContain("if: success()");
        uploadLane.ShouldNotContain("if: always()");
        // A rejected evidence tree is never published, but its validator diagnostics must be.
        string diagnosticsLane = ExtractNamedStep(quality, "Upload contract diagnostics");
        diagnosticsLane.ShouldContain("if: always()");
        diagnosticsLane.ShouldContain("artifacts/contracts/**");
        diagnosticsLane.ShouldNotContain("_bmad-output/implementation-artifacts/evidence/frontcomposer-story-11-24");
        string evidenceRoot = Path.Combine(
            root,
            "_bmad-output",
            "implementation-artifacts",
            "evidence",
            "frontcomposer-story-11-24");

        ProcessResult validation = RunPython(root, [
            "eng/eventstore_runtime_evidence.py",
            "--evidence-root", evidenceRoot,
            "--pact-dir", "tests/Hexalith.FrontComposer.Shell.Tests/Pact",
        ]);
        validation.ExitCode.ShouldBe(0, validation.Output + validation.Error);

        ProcessResult eventStoreGitlink = RunProcess(
            root,
            "git",
            ["ls-tree", "HEAD", "--", "references/Hexalith.EventStore"]);
        eventStoreGitlink.ExitCode.ShouldBe(0, eventStoreGitlink.Error);
        eventStoreGitlink.Output.Trim().ShouldBe($"160000 commit {sourceSha}\treferences/Hexalith.EventStore");

        ProcessResult eventStoreHead = RunProcess(
            root,
            "git",
            ["-C", "references/Hexalith.EventStore", "rev-parse", "HEAD"]);
        eventStoreHead.ExitCode.ShouldBe(0, eventStoreHead.Error);
        eventStoreHead.Output.Trim().ShouldBe(sourceSha);

        ProcessResult buildsGitlink = RunProcess(
            root,
            "git",
            ["ls-tree", "HEAD", "--", "references/Hexalith.Builds"]);
        buildsGitlink.ExitCode.ShouldBe(0, buildsGitlink.Error);
        buildsGitlink.Output.Trim().ShouldBe($"160000 commit {buildsSha}\treferences/Hexalith.Builds");

        ProcessResult buildsHead = RunProcess(
            root,
            "git",
            ["-C", "references/Hexalith.Builds", "rev-parse", "HEAD"]);
        buildsHead.ExitCode.ShouldBe(0, buildsHead.Error);
        buildsHead.Output.Trim().ShouldBe(buildsSha);

        XDocument catalog = XDocument.Load(Path.Combine(root, "references/Hexalith.Builds/Props/Directory.Packages.props"));
        catalog.Descendants("HexalithEventStoreVersion").Single().Value.ShouldBe(version);

        using JsonDocument report = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            evidenceRoot,
            "provider-verification",
            "provider-verification.json")));
        JsonElement reportRoot = report.RootElement;
        reportRoot.GetProperty("complete").GetBoolean().ShouldBeTrue();
        reportRoot.GetProperty("requestedInteractionCount").GetInt32().ShouldBe(19);
        reportRoot.GetProperty("reportedInteractionCount").GetInt32().ShouldBe(19);
        reportRoot.GetProperty("setupEventCount").GetInt32().ShouldBe(19);
        reportRoot.GetProperty("teardownEventCount").GetInt32().ShouldBe(19);
        reportRoot.GetProperty("finalVerdict").GetString().ShouldBe("failed");
        reportRoot.GetProperty("identity").GetProperty("runtimeMatches").GetBoolean().ShouldBeFalse(
            "the complete provider report is compatibility evidence, not migration authority");

        using JsonDocument packageManifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            evidenceRoot,
            evidenceSourceSha,
            "package-manifest.json")));
        packageManifest.RootElement.GetProperty("source_sha").GetString().ShouldBe(evidenceSourceSha);
        packageManifest.RootElement.GetProperty("version").GetString().ShouldBe(evidenceVersion);
        packageManifest.RootElement.GetProperty("packages").GetArrayLength().ShouldBe(14);
    }
}
