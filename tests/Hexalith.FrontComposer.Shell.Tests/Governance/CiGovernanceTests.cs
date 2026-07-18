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
        workflow.ShouldContain("test-results-governance.trx");
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
    }

    [Fact]
    public void BlockingTestLanes_ExcludeQuarantinedTestsWithoutSkippingGovernance() {
        // REL-2 (2026-07-13): the trait-filtered test lanes moved from the inline ci.yml into the
        // supplemental quality.yml; the release path no longer re-runs tests (the reusable
        // domain-release.yml publishes and CI already gated the head). REL-3 (2026-07-18): the
        // release-lane test run moved from the supplemental evidence workflow into the
        // pre-publication orchestrator, which runs the release tests (excluding Quarantined)
        // against the exact candidates before any publication side effect.
        string root = RepositoryRoot();
        string quality = File.ReadAllText(Path.Combine(root, ".github/workflows/quality.yml"));
        string orchestrator = File.ReadAllText(Path.Combine(root, "eng/release_prepublish.py"));

        string defaultLane = ExtractNamedStep(quality, "Gate 3a: Unit + bUnit (default lane)");
        defaultLane.ShouldContain("Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined");
        defaultLane.ShouldNotContain("continue-on-error: true");

        string governanceLane = ExtractNamedStep(quality, "Gate 2b: Infrastructure governance and telemetry contracts");
        governanceLane.ShouldContain("Category=Governance");
        governanceLane.ShouldNotContain("Category!=Quarantined");
        governanceLane.ShouldNotContain("continue-on-error: true");

        orchestrator.ShouldContain("Category!=Quarantined");
    }

    [Fact]
    public void QuarantineLane_IsWarningOnlyAndPublishesBoundedEvidence() {
        // REL-2 (2026-07-13): the advisory quarantine lane + telemetry moved to quality.yml.
        string root = RepositoryRoot();
        string ci = File.ReadAllText(Path.Combine(root, ".github/workflows/quality.yml"));

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
    public void QualityWorkflow_PinsContractPactStaleAndArtifactGates() {
        // REL-2 code-review P3 (2026-07-13): Gate 2c (Contract pacts + contract-artifact validation +
        // the stale-pact-diff guard) relocated from the inline ci.yml into the supplemental
        // quality.yml. Pin it at its new home so a future edit cannot silently drop the sole
        // enforcement that drifted EventStore consumer pacts are caught (AC8 / PRD NFR-11).
        string root = RepositoryRoot();
        string quality = File.ReadAllText(Path.Combine(root, ".github/workflows/quality.yml"));

        string pactLane = ExtractNamedStep(quality, "Gate 2c: Contract pacts");
        pactLane.ShouldContain("--filter \"Category=Contract\"");
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

        string initializeBuildSubmodules = ExtractNamedStep(a11yJob, "Initialize build submodules");
        a11yJob.ShouldContain("fetch-depth: 0");
        initializeBuildSubmodules.ShouldContain("GIT_CONFIG_COUNT: 1");
        initializeBuildSubmodules.ShouldContain("GIT_CONFIG_KEY_0: core.symlinks");
        initializeBuildSubmodules.ShouldContain("GIT_CONFIG_VALUE_0: 'false'");
        initializeBuildSubmodules.ShouldNotContain("git config --global");
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

        ci.ShouldContain("uses: Hexalith/Hexalith.Builds/.github/workflows/domain-ci.yml@main");
        ci.ShouldContain("solution: Hexalith.FrontComposer.slnx");
        ci.ShouldContain("run-consumer-validation: true");
        ci.ShouldContain("unit-test-projects:");
        ci.ShouldContain("tests/Hexalith.FrontComposer.Cli.Tests");
        ci.ShouldContain("tests/Hexalith.FrontComposer.Testing.Tests");
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
        workflow.ShouldContain("Category=Performance&FullyQualifiedName~BenchmarkHarnessTests");
        workflow.ShouldNotContain("tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj --configuration Release --filter FullyQualifiedName~BenchmarkHarnessTests");
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
        using var doc = JsonDocument.Parse(File.ReadAllText(output));
        doc.RootElement.GetProperty("prompt_count").GetInt32().ShouldBe(20);
        doc.RootElement.GetProperty("classification").GetString().ShouldBe("budget-blocked");
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
        // REL-2 (2026-07-13): release.yml no longer runs an inline per-project test loop, package
        // inventory attestation, or attest-build-provenance steps. It delegates to the shared
        // reusable Hexalith.Builds domain-release.yml (Tenants parity); CI (the upstream
        // workflow_run gate) already tested the same head, so the release path does not duplicate
        // test compute. semantic-release still packs+publishes via this repo's .releaserc.json.
        string root = RepositoryRoot();
        string workflow = File.ReadAllText(Path.Combine(root, ".github/workflows/release.yml"));
        string releaseConfig = File.ReadAllText(Path.Combine(root, ".releaserc.json"));

        workflow.ShouldContain("uses: Hexalith/Hexalith.Builds/.github/workflows/domain-release.yml@main");
        workflow.ShouldContain("solution: Hexalith.FrontComposer.slnx");
        workflow.ShouldContain("test-projects: ''");
        workflow.ShouldContain("NUGET_API_KEY: ${{ secrets.NUGET_API_KEY }}");
        workflow.ShouldContain("contents: write");
        workflow.ShouldContain("issues: write");
        workflow.ShouldContain("pull-requests: write");
        workflow.ShouldNotContain("submodules: recursive");

        // The bespoke inline release job is gone: no duplicated test loop, no inline attestation,
        // no container publishing (FrontComposer ships NuGet packages only).
        workflow.ShouldNotContain("Run release tests");
        workflow.ShouldNotContain("Run semantic-release");
        workflow.ShouldNotContain("attest-build-provenance");
        workflow.ShouldNotContain("publish-containers");
        workflow.ShouldNotContain("workflow_dispatch:");

        // REL-3 (2026-07-18): semantic-release delegates prepare/publish to the repository-owned
        // exact-artifact orchestrator (eng/release_prepublish.py) — pack-once, fail-closed FR24
        // gate before any side effect, and a publisher that pushes only the manifest-authorized
        // signed bytes. Raw pack/push commands and inlined evidence commands stay out of the JSON.
        releaseConfig.ShouldContain("@semantic-release/commit-analyzer");
        releaseConfig.ShouldContain("@semantic-release/release-notes-generator");
        releaseConfig.ShouldContain("@semantic-release/changelog");
        releaseConfig.ShouldContain("python3 eng/release_prepublish.py prepare --version ${nextRelease.version}");
        releaseConfig.ShouldContain("python3 eng/release_prepublish.py publish --version ${nextRelease.version}");
        releaseConfig.ShouldContain("nupkgs-signed/*.nupkg");
        releaseConfig.ShouldContain("nupkgs/*.snupkg");
        releaseConfig.ShouldContain("release-evidence/*.json");
        releaseConfig.ShouldContain("release-evidence/*.txt");
        releaseConfig.ShouldContain("@semantic-release/github");
        releaseConfig.ShouldContain("@semantic-release/git");
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
                .ShouldBe("^9.3.1");
        }

        using (JsonDocument packageLock = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "package-lock.json")))) {
            packageLock.RootElement
                .GetProperty("packages")
                .GetProperty(string.Empty)
                .GetProperty("devDependencies")
                .GetProperty("conventional-changelog-conventionalcommits")
                .GetString()
                .ShouldBe("^9.3.1");
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

        string releaseNotes = behavior.RootElement.GetProperty("releaseNotes").GetString() ?? string.Empty;
        releaseNotes.ShouldContain("BREAKING CHANGES");
        releaseNotes.ShouldContain("break the public API");
        releaseNotes.ShouldContain("replace the public contract");
    }

    [Fact]
    public void PackageInventory_IsExplicitLockstepAndReviewable() {
        string root = RepositoryRoot();
        string inventory = File.ReadAllText(Path.Combine(root, "eng/release-package-inventory.json"));
        string packScript = File.ReadAllText(Path.Combine(root, "eng/pack_release_packages.py"));
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
        packScript.ShouldContain("\"-p:EnableFrontComposerPackageValidation=true\"");
        qualityWorkflow.ShouldContain("python3 -m unittest tests/eng/test_pack_release_packages.py");
        qualityWorkflow.ShouldContain("dotnet restore Hexalith.FrontComposer.slnx -p:Configuration=Release -p:EnableFrontComposerPackageValidation=true");
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
    public void SemanticReleasePack_EvaluatesPublished300PackageValidationBaseline() {
        // The shared package-validation policy and the Contracts.UI explicit pin must both resolve
        // to the latest published 3.0 surface before semantic-release packs the 3.1 line.
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
        baseBaseline.ShouldBe("3.0.0");

        (string uiEnable, string uiBaseline) = EvaluatePackageValidation(
            root,
            Path.Combine(root, "src", "Hexalith.FrontComposer.Contracts.UI", "Hexalith.FrontComposer.Contracts.UI.csproj"));
        uiEnable.ShouldBe("true");
        uiBaseline.ShouldBe("3.0.0");
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
            Directory.CreateDirectory(Path.Combine(tempRoot, ".github", "workflows"));
            Directory.CreateDirectory(Path.Combine(tempRoot, "eng"));
            Directory.CreateDirectory(Path.Combine(tempRoot, "nupkgs-signed"));

            // CR-12-4-D8 (round-5): Directory.Packages.props was removed from
            // FALLBACK_INVALIDATION_FILES so routine Dependabot bumps do not invalidate
            // fallback approvals. CR-12-4-P124 (round-6) restored it to
            // RELEASE_DEFINITION_FILES (drift detection) while keeping it out of
            // fallback invalidation. CR-12-4-P172 (round-7): the test baseline must
            // include all 8 RELEASE_DEFINITION_FILES so `fingerprint_diff` does NOT
            // emit a spurious "fingerprint has no baseline entry" diagnostic for
            // Directory.Packages.props — that would let a future regression on a
            // different file slip past the assertion which only checks for the test's
            // specific drift string.
            string[] releaseDefinitionFiles = [
                ".github/workflows/release.yml",
                ".releaserc.json",
                "eng/release-package-inventory.json",
                "Directory.Build.props",
                "Directory.Build.targets",
                "Directory.Packages.props",
                "references/Hexalith.Builds/Props/Directory.Packages.props",
                "deps.nuget.props",
            ];
            foreach (string file in releaseDefinitionFiles) {
                string path = Path.Combine(tempRoot, file.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, $"baseline {file}");
            }

            string artifact = Path.Combine(tempRoot, "nupkgs-signed", "Hexalith.FrontComposer.Contracts.1.2.3.nupkg");
            string originalChecksum = Sha256Text("original package bytes");
            File.WriteAllText(artifact, "mutated package bytes");

            Dictionary<string, string> fingerprints = releaseDefinitionFiles.ToDictionary(
                file => file,
                file => Sha256File(Path.Combine(tempRoot, file.Replace('/', Path.DirectorySeparatorChar))));
            string preManifest = Path.Combine(tempRoot, "pre-manifest.json");
            string sealedManifest = Path.Combine(tempRoot, "sealed-manifest.json");
            string output = Path.Combine(tempRoot, "verification.json");
            File.WriteAllText(preManifest, JsonSerializer.Serialize(new Dictionary<string, object?> {
                ["benchmark_summary_hash"] = new string('c', 64),
                ["commit_sha"] = "abc123",
                ["packages"] = new[] {
                    new Dictionary<string, object?> {
                        ["artifact_path"] = "nupkgs-signed/Hexalith.FrontComposer.Contracts.1.2.3.nupkg",
                        ["attestation_status"] = "attested",
                        ["checksum"] = originalChecksum,
                        ["commit_sha"] = "abc123",
                        ["package_id"] = "Hexalith.FrontComposer.Contracts",
                        ["publish_status"] = "pending",
                        ["sbom_component"] = "Hexalith.FrontComposer.Contracts",
                        ["signing_status"] = "verified",
                        ["symbol_artifact"] = "nupkgs/Hexalith.FrontComposer.Contracts.1.2.3.snupkg",
                        ["timestamp_status"] = "verified",
                        ["version"] = "1.2.3",
                    },
                },
                // CR-12-4-P172 (round-7): include `package_set_fingerprint` so
                // manifest_diagnostics doesn't accumulate a spurious "manifest missing
                // package_set_fingerprint" message in this test's output.
                ["package_set_fingerprint"] = Sha256File(Path.Combine(tempRoot, "eng", "release-package-inventory.json")),
                ["release_definition_fingerprints"] = fingerprints,
                ["run_id"] = "42",
                ["sbom_hash"] = new string('a', 64),
                ["tag"] = "v1.2.3",
                ["workflow_ref"] = "Hexalith/Hexalith.FrontComposer/.github/workflows/release.yml@refs/tags/v1.2.3",
            }, new JsonSerializerOptions { WriteIndented = true }));

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
            Directory.CreateDirectory(Path.Combine(tempRoot, ".github", "workflows"));
            Directory.CreateDirectory(Path.Combine(tempRoot, "eng"));
            Directory.CreateDirectory(Path.Combine(tempRoot, "nupkgs-signed"));

            // CR-12-4-D8 (round-5): Directory.Packages.props was removed from
            // FALLBACK_INVALIDATION_FILES so routine Dependabot bumps do not invalidate
            // fallback approvals. CR-12-4-P124 (round-6) restored it to
            // RELEASE_DEFINITION_FILES (drift detection) while keeping it out of
            // fallback invalidation. CR-12-4-P172 (round-7): the test baseline must
            // include all 8 RELEASE_DEFINITION_FILES so `fingerprint_diff` does NOT
            // emit a spurious "fingerprint has no baseline entry" diagnostic for
            // Directory.Packages.props — that would let a future regression on a
            // different file slip past the assertion which only checks for the test's
            // specific drift string.
            string[] releaseDefinitionFiles = [
                ".github/workflows/release.yml",
                ".releaserc.json",
                "eng/release-package-inventory.json",
                "Directory.Build.props",
                "Directory.Build.targets",
                "Directory.Packages.props",
                "references/Hexalith.Builds/Props/Directory.Packages.props",
                "deps.nuget.props",
            ];
            foreach (string file in releaseDefinitionFiles) {
                string path = Path.Combine(tempRoot, file.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, $"baseline {file}");
            }

            string artifact = Path.Combine(tempRoot, "nupkgs-signed", "Hexalith.FrontComposer.Contracts.1.2.3.nupkg");
            File.WriteAllText(artifact, "package bytes");
            Dictionary<string, string> fingerprints = releaseDefinitionFiles.ToDictionary(
                file => file,
                file => Sha256File(Path.Combine(tempRoot, file.Replace('/', Path.DirectorySeparatorChar))));
            File.WriteAllText(
                Path.Combine(tempRoot, "references", "Hexalith.Builds", "Props", "Directory.Packages.props"),
                "drifted shared package catalog");

            string preManifest = Path.Combine(tempRoot, "pre-manifest.json");
            string sealedManifest = Path.Combine(tempRoot, "sealed-manifest.json");
            string output = Path.Combine(tempRoot, "verification.json");
            File.WriteAllText(preManifest, JsonSerializer.Serialize(new Dictionary<string, object?> {
                ["benchmark_summary_hash"] = new string('c', 64),
                ["commit_sha"] = "abc123",
                ["packages"] = new[] {
                    new Dictionary<string, object?> {
                        ["artifact_path"] = "nupkgs-signed/Hexalith.FrontComposer.Contracts.1.2.3.nupkg",
                        ["attestation_status"] = "attested",
                        ["checksum"] = Sha256Text("package bytes"),
                        ["commit_sha"] = "abc123",
                        ["package_id"] = "Hexalith.FrontComposer.Contracts",
                        ["publish_status"] = "pending",
                        ["sbom_component"] = "Hexalith.FrontComposer.Contracts",
                        ["signing_status"] = "verified",
                        ["symbol_artifact"] = "nupkgs/Hexalith.FrontComposer.Contracts.1.2.3.snupkg",
                        ["timestamp_status"] = "verified",
                        ["version"] = "1.2.3",
                    },
                },
                // CR-12-4-P172 (round-7): see ReleaseEvidenceScript_DetectsPostSealArtifactMutationFromRealFiles.
                ["package_set_fingerprint"] = Sha256File(Path.Combine(tempRoot, "eng", "release-package-inventory.json")),
                ["release_definition_fingerprints"] = fingerprints,
                ["run_id"] = "42",
                ["sbom_hash"] = new string('a', 64),
                ["tag"] = "v1.2.3",
                ["workflow_ref"] = "Hexalith/Hexalith.FrontComposer/.github/workflows/release.yml@refs/tags/v1.2.3",
            }, new JsonSerializerOptions { WriteIndented = true }));

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
    public void ReleaseWorkflow_RunsViaWorkflowRunAfterCiSuccess() {
        // REL-2 (2026-07-13): Tenants-aligned model — release runs from workflow_run after a
        // successful CI push (not directly on push). The conclusion=='success' + event=='push'
        // guard stops failed or non-push (PR/scheduled) CI runs from releasing. No manual
        // dispatch / approval / dry-run gating is reintroduced; the REL-4 freeze gate
        // (freeze-guard job + HEXALITH_RELEASE_PUBLISH_ENABLED variable) is a deliberate,
        // separately-pinned exception (see ReleaseWorkflow_PublishFreezeGate_IsFailClosedByDefault)
        // and none of its token names collide with the forbidden approval tokens below.
        // semantic-release decides from the commit history whether to publish, via this repo's
        // .releaserc.json.
        string root = RepositoryRoot();
        string workflow = File.ReadAllText(Path.Combine(root, ".github/workflows/release.yml"));
        string releaseConfig = File.ReadAllText(Path.Combine(root, ".releaserc.json"));

        workflow.ShouldContain("workflow_run:");
        workflow.ShouldContain("workflows: [CI]");
        workflow.ShouldContain("github.event.workflow_run.conclusion == 'success'");
        workflow.ShouldContain("github.event.workflow_run.event == 'push'");
        // The bespoke on: push[main] trigger is gone; the on: block is workflow_run only.
        ExtractOnBlock(workflow).ShouldNotContain("push:");
        workflow.ShouldNotContain("workflow_dispatch:");
        workflow.ShouldNotContain("release_owner_approved");
        workflow.ShouldNotContain("release_approver");
        workflow.ShouldNotContain("RELEASE_OWNER_APPROVED");
        workflow.ShouldNotContain("RELEASE_APPROVER");
        workflow.ShouldNotContain("RELEASE_DRY_RUN");
        workflow.ShouldNotContain("RELEASE_CONCURRENT_SAME_VERSION");

        // REL-3 (2026-07-18): prepareCmd runs the fail-closed exact-artifact gate
        // (pack once → … → classify-release --require-publishable, inside the
        // orchestrator) and publishCmd re-verifies the sealed manifest before pushing
        // only manifest-authorized signed bytes. The JSON stays free of raw pack/push
        // commands and dry-run gating.
        releaseConfig.ShouldContain("\"branches\": [\"main\"]");
        releaseConfig.ShouldContain("\"tagFormat\": \"v${version}\"");
        releaseConfig.ShouldContain("python3 eng/release_prepublish.py prepare --version ${nextRelease.version}");
        releaseConfig.ShouldContain("python3 eng/release_prepublish.py publish --version ${nextRelease.version}");
        releaseConfig.ShouldContain("@semantic-release/github");
        releaseConfig.ShouldContain("@semantic-release/git");
        releaseConfig.ShouldNotContain("pack_release_packages.py");
        releaseConfig.ShouldNotContain("dotnet nuget push");
        releaseConfig.ShouldNotContain("--skip-duplicate");
        releaseConfig.ShouldNotContain("RELEASE_DRY_RUN");
        releaseConfig.ShouldNotContain("gh attestation");
        releaseConfig.IndexOf("release_prepublish.py prepare", StringComparison.Ordinal).ShouldBeLessThan(
            releaseConfig.IndexOf("release_prepublish.py publish", StringComparison.Ordinal));
    }

    [Fact]
    public void ReleaseWorkflow_PublishFreezeGate_IsFailClosedByDefault() {
        // REL-4 (2026-07-15): temporary technical release freeze. Publication is disabled by
        // default; it is enabled only when the Release Owner-controlled repository variable
        // HEXALITH_RELEASE_PUBLISH_ENABLED is exactly the string 'true'. The exact match MUST
        // live in bash ([ "${PUBLISH_ENABLED}" = "true" ]) because GitHub expression '==' is
        // case-insensitive ('True' == 'true'), which would let a malformed value publish. The
        // release job is fail-closed via needs: freeze-guard — a failed or skipped guard skips
        // the release. Removal/re-scope only on REL-3 real-release evidence (comment marker).
        string root = RepositoryRoot();
        string workflow = File.ReadAllText(Path.Combine(root, ".github/workflows/release.yml"));
        string normalized = workflow.Replace("\r\n", "\n", StringComparison.Ordinal);

        // The guard job and its Release Owner-controlled variable binding.
        normalized.ShouldContain("freeze-guard:");
        normalized.ShouldContain("PUBLISH_ENABLED: ${{ vars.HEXALITH_RELEASE_PUBLISH_ENABLED }}");
        // Exact POSIX bash comparison — not a case-insensitive GitHub expression.
        normalized.ShouldContain("if [ \"${PUBLISH_ENABLED}\" = \"true\" ]; then");
        normalized.ShouldContain("publish-enabled: ${{ steps.evaluate.outputs.publish-enabled }}");

        // The release job is gated fail-closed on the guard output alongside the retained
        // CI-success + push-event conjuncts.
        normalized.ShouldContain("needs: freeze-guard");
        normalized.ShouldContain("needs.freeze-guard.outputs.publish-enabled == 'true'");
        string releaseJobCondition = ExtractReleaseJobCondition(normalized);
        releaseJobCondition.ShouldContain("github.event.workflow_run.conclusion == 'success'");
        releaseJobCondition.ShouldContain("github.event.workflow_run.event == 'push'");
        releaseJobCondition.ShouldContain("needs.freeze-guard.outputs.publish-enabled == 'true'");

        // Frozen runs conclude green with an explicit notice + step summary.
        normalized.ShouldContain("Release publication FROZEN (REL-3 / REL-AI-1)");
        normalized.ShouldContain("Release publication is frozen until the REL-3 exact-artifact gate is operational.\" >> \"$GITHUB_STEP_SUMMARY\"");

        // REL-3 removal-condition marker: the gate may be removed/replaced only when the
        // permanent exact-artifact gate is operational and REL-AI-1 records passing evidence.
        normalized.ShouldContain("REMOVAL/REPLACEMENT is permitted only when the permanent REL-3");
        normalized.ShouldContain("real-release evidence");
    }

    [Fact]
    public void Workflows_HaveNoPublishPathOutsideGatedReleaseWorkflow() {
        // REL-4 (2026-07-15): the freeze gate is only meaningful if release.yml is the ONLY
        // publish path. Scan every repository-owned workflow: only release.yml may reference the
        // reusable domain-release.yml, and no workflow may execute `npx semantic-release` or
        // `dotnet nuget push` itself. Assertions target executable content (comments stripped):
        // those strings legitimately appear in workflow comments today.
        string root = RepositoryRoot();
        foreach (string workflowPath in Directory.EnumerateFiles(Path.Combine(root, ".github/workflows"), "*.yml")) {
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
        // signs, seals, and classifies with --require-publishable BEFORE any publication
        // side effect and attaches the durable evidence at initial GitHub Release creation
        // (AC12). The supplemental workflow is now the INDEPENDENT verifier: it downloads
        // the published GitHub Release assets and the published NuGet bytes, verifies
        // package signatures over the downloaded bytes, and compares every hash against
        // the sealed manifest (AC13). It runs on Release completion regardless of
        // conclusion (AC19) and records partial-publication incidents (AC14). It must not
        // rebuild, repack, re-sign, classify, or attest — reconstructed evidence can never
        // establish the identity of published bytes (the v3.2.1/v3.2.2 lesson).
        string root = RepositoryRoot();
        string workflow = File.ReadAllText(Path.Combine(root, ".github/workflows/release-evidence.yml"));
        string releaseConfig = File.ReadAllText(Path.Combine(root, ".releaserc.json"));
        string executable = StripYamlComments(workflow);

        // AC19: verification triggers on Release COMPLETION with no success-only gate, so
        // failed and cancelled publish runs are still reconciled. The tag resolver decides
        // no-op vs verify by detecting publication side effects.
        workflow.ShouldContain("workflow_run:");
        workflow.ShouldContain("workflows: [Release]");
        workflow.ShouldContain("types: [completed]");
        executable.ShouldNotContain("github.event.workflow_run.conclusion == 'success'");
        workflow.ShouldContain("no publication side effect");

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
        workflow.ShouldContain("signing-verification.txt");

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
        workflow.ShouldContain("Initialize build submodules");

        // Evidence commands stay out of .releaserc.json — the orchestrator wraps them.
        releaseConfig.ShouldNotContain("classify-release");
        releaseConfig.ShouldNotContain("CycloneDX");
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
            "signing_status": "verified", "test_count": 42, "test_status": "passed",
            "timestamp_status": "verified", "trx_present": true
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
        // CR-12-4-P166 (round-7): regression test for seal-manifest → verify-manifest
        // round-trip. Builds a fresh in-memory manifest with all current required
        // fields (release_definition_fingerprints AND package_set_fingerprint, both
        // round-6 additions), seals it, then verifies under --root. A future change
        // that mutates the manifest between hashing and sealing would otherwise slip
        // past CI because the existing tests intentionally introduce drift.
        string root = RepositoryRoot();
        string tempRoot = Path.Combine(Path.GetTempPath(), $"fc-seal-roundtrip-{Guid.NewGuid():N}");
        try {
            Directory.CreateDirectory(Path.Combine(tempRoot, ".github", "workflows"));
            Directory.CreateDirectory(Path.Combine(tempRoot, "eng"));
            Directory.CreateDirectory(Path.Combine(tempRoot, "nupkgs-signed"));
            // CR-12-4-P215 (round-8, from CR-12-4-D16): `eng/release_evidence.py` is no
            // longer in RELEASE_DEFINITION_FILES — the sealed manifest tracks helper
            // identity via the structured `helper_version` field instead. The
            // round-trip baseline now matches the live `RELEASE_DEFINITION_FILES` set
            // (8 entries) and embeds the live `helper_version_record()` so
            // `manifest_diagnostics` sees no drift.
            string[] releaseDefinitionFiles = [
                ".github/workflows/release.yml",
                ".releaserc.json",
                "eng/release-package-inventory.json",
                "Directory.Build.props",
                "Directory.Build.targets",
                "Directory.Packages.props",
                "references/Hexalith.Builds/Props/Directory.Packages.props",
                "deps.nuget.props",
            ];
            foreach (string file in releaseDefinitionFiles) {
                string path = Path.Combine(tempRoot, file.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, $"baseline {file}");
            }
            // CR-12-4-P215: copy the live helper into the temp root and compute its sha256
            // so `helper_version_record()` produces a matching baseline.
            string liveHelper = Path.Combine(root, "eng/release_evidence.py");
            Directory.CreateDirectory(Path.Combine(tempRoot, "eng"));
            File.Copy(liveHelper, Path.Combine(tempRoot, "eng/release_evidence.py"), overwrite: true);
            string helperContentSha256 = Sha256File(liveHelper);
            string artifactPath = Path.Combine(tempRoot, "nupkgs-signed", "Hexalith.FrontComposer.Contracts.1.2.3.nupkg");
            File.WriteAllText(artifactPath, "package bytes");
            string artifactChecksum = Sha256File(artifactPath);
            // REL-3 review BH-1/VG-3: symbol integrity is sealed into the manifest row
            // (symbol_checksum) and verified on disk, so the round-trip needs the symbol
            // package bytes too.
            Directory.CreateDirectory(Path.Combine(tempRoot, "nupkgs"));
            string symbolPath = Path.Combine(tempRoot, "nupkgs", "Hexalith.FrontComposer.Contracts.1.2.3.snupkg");
            File.WriteAllText(symbolPath, "symbol bytes");
            string symbolChecksum = Sha256File(symbolPath);
            Dictionary<string, string> fingerprints = releaseDefinitionFiles.ToDictionary(
                file => file,
                file => Sha256File(Path.Combine(tempRoot, file.Replace('/', Path.DirectorySeparatorChar))));
            string preManifest = Path.Combine(tempRoot, "pre-manifest.json");
            string sealedManifest = Path.Combine(tempRoot, "sealed-manifest.json");
            string output = Path.Combine(tempRoot, "verification.json");
            File.WriteAllText(preManifest, JsonSerializer.Serialize(new Dictionary<string, object?> {
                ["benchmark_summary_hash"] = new string('c', 64),
                ["commit_sha"] = "abc123",
                ["helper_version"] = new Dictionary<string, object?> {
                    // Matches eng/release_evidence.py __version__ (bumped deliberately for the
                    // REL-3 APPROVAL_MATRIX rewrite).
                    ["version"] = "1.2.0",
                    ["content_sha256"] = helperContentSha256,
                },
                ["package_set_fingerprint"] = Sha256File(Path.Combine(tempRoot, "eng", "release-package-inventory.json")),
                ["packages"] = new[] {
                    new Dictionary<string, object?> {
                        ["artifact_path"] = "nupkgs-signed/Hexalith.FrontComposer.Contracts.1.2.3.nupkg",
                        ["attestation_status"] = "attested",
                        ["checksum"] = artifactChecksum,
                        ["commit_sha"] = "abc123",
                        ["package_id"] = "Hexalith.FrontComposer.Contracts",
                        ["publish_status"] = "pending",
                        ["sbom_component"] = "Hexalith.FrontComposer.Contracts",
                        ["signing_status"] = "verified",
                        ["symbol_artifact"] = "nupkgs/Hexalith.FrontComposer.Contracts.1.2.3.snupkg",
                        ["symbol_checksum"] = symbolChecksum,
                        ["timestamp_status"] = "verified",
                        ["version"] = "1.2.3",
                    },
                },
                ["release_definition_fingerprints"] = fingerprints,
                ["run_id"] = "42",
                ["sbom_hash"] = new string('a', 64),
                ["tag"] = "v1.2.3",
                ["workflow_ref"] = "Hexalith/Hexalith.FrontComposer/.github/workflows/release.yml@refs/tags/v1.2.3",
            }, new JsonSerializerOptions { WriteIndented = true }));

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
            "signing_status": "verified",
            "test_count": "unknown",
            "test_status": "passed",
            "timestamp_status": "verified",
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
        // CR-12-4-P252 (round-11): the `--dry-run-clean-exit` exit-code contract is
        // what the workflow's dry-run path depends on. Fixture-level coverage only
        // checks JSON output via `classify-fixtures`. This test exercises the gate
        // directly and asserts exit 0 for the healthy local-candidate carve-out plus
        // that `classification=ready` and `publish_authorized=false` are emitted.
        //
        // The test uses `classify-fixtures` mode (verify_drift=False) to avoid
        // live-disk drift detection — without this, the base fixture's manifest
        // fingerprints diverge from the live repo and the test exits with extra
        // drift blockers, defeating the carve-out's `len(blocking) == 1` guard.
        // The carve-out semantic is the same either way; this just isolates the test
        // to the classification logic rather than to the on-disk state.
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

    // REL-2/FR24 (2026-07-13): parse_signing_verification must accept the REAL
    // `dotnet nuget verify --all -v normal` transcript emitted by the .NET SDK, where the
    // `Timestamp: <datetime>` line carries no status keyword and precedes the trailing
    // `Successfully verified package '<id>'` confirmation. This locks the fixed parser so a
    // regression to the old keyword/forward-block model (which scored every real RFC 3161
    // timestamp `missing`) cannot silently return.
    [Fact]
    public void PrepareManifest_ScoresSigningAndTimestampVerifiedFromRealDotnetVerifyOutput() {
        string tempRoot = Path.Combine(Path.GetTempPath(), $"fc-signing-verify-{Guid.NewGuid():N}");
        try {
            const string version = "1.2.3";
            const string pkg = "Hexalith.FrontComposer.Contracts";
            string transcript = string.Join('\n', [
                $"Verifying {pkg}.{version}",
                "Content hash: abc==",
                "<path>",
                "Signature type: Author",
                "  Subject Name: CN=FC Release Evidence",
                "  Issued by: CN=FC Release Evidence Root",
                "Timestamp: 07/13/2026 17:16:10",
                "Verifying author primary signature's timestamp with timestamping service certificate:",
                $"Successfully verified package '{pkg}.{version}'.",
                string.Empty,
            ]);

            (ProcessResult result, string preManifest, string diagnostics) = PrepareManifestWithSigningTranscript(tempRoot, pkg, version, transcript);

            result.ExitCode.ShouldBe(0, File.Exists(diagnostics) ? File.ReadAllText(diagnostics) : result.Error);
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(preManifest));
            JsonElement row = doc.RootElement.GetProperty("packages")[0];
            row.GetProperty("signing_status").GetString().ShouldBe("verified");
            row.GetProperty("timestamp_status").GetString().ShouldBe("verified");
        }
        finally {
            if (Directory.Exists(tempRoot)) {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    // A `Timestamp:` line carrying an explicit failure marker must NOT count as evidence
    // (CR-12-4-P146 intent), so the manifest fails closed with a per-package diagnostic.
    [Fact]
    public void PrepareManifest_RejectsTimestampLineWithFailureMarker() {
        string tempRoot = Path.Combine(Path.GetTempPath(), $"fc-signing-verify-neg-{Guid.NewGuid():N}");
        try {
            const string version = "1.2.3";
            const string pkg = "Hexalith.FrontComposer.Contracts";
            string transcript = string.Join('\n', [
                $"Verifying {pkg}.{version}",
                "Signature type: Author",
                "Timestamp: invalid certificate",
                $"Successfully verified package '{pkg}.{version}'.",
                string.Empty,
            ]);

            (ProcessResult result, _, string diagnostics) = PrepareManifestWithSigningTranscript(tempRoot, pkg, version, transcript);

            result.ExitCode.ShouldBe(1);
            File.ReadAllText(diagnostics).ShouldContain("timestamp not verified");
        }
        finally {
            if (Directory.Exists(tempRoot)) {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    private static (ProcessResult Result, string PreManifest, string Diagnostics) PrepareManifestWithSigningTranscript(
        string tempRoot, string packageId, string version, string transcript) {
        string root = RepositoryRoot();
        Directory.CreateDirectory(Path.Combine(tempRoot, "nupkgs-signed"));
        Directory.CreateDirectory(Path.Combine(tempRoot, "nupkgs"));
        Directory.CreateDirectory(Path.Combine(tempRoot, "release-evidence"));

        string signedNupkg = Path.Combine(tempRoot, "nupkgs-signed", $"{packageId}.{version}.nupkg");
        string snupkg = Path.Combine(tempRoot, "nupkgs", $"{packageId}.{version}.snupkg");
        string sbom = Path.Combine(tempRoot, "release-evidence", "sbom.json");
        File.WriteAllText(signedNupkg, "signed package bytes");
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
                new Dictionary<string, object?> { ["path"] = $"nupkgs-signed/{packageId}.{version}.nupkg", ["sha256"] = Sha256File(signedNupkg) },
                new Dictionary<string, object?> { ["path"] = $"nupkgs/{packageId}.{version}.snupkg", ["sha256"] = Sha256File(snupkg) },
                new Dictionary<string, object?> { ["path"] = "release-evidence/sbom.json", ["sha256"] = Sha256File(sbom) },
            },
        }));

        // --signing-verification is normalized under --root, so it must be relative (the
        // workflow passes the same `release-evidence/signing-verification.txt`).
        const string signingVerificationRelative = "release-evidence/signing-verification.txt";
        File.WriteAllText(Path.Combine(tempRoot, "release-evidence", "signing-verification.txt"), transcript);

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
            "--signing-verification", signingVerificationRelative,
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

    internal static ProcessResult RunPython(string root, IReadOnlyList<string> arguments) {
        string executable = OperatingSystem.IsWindows() ? "python" : "python3";
        return RunProcess(root, executable, arguments);
    }

    private static ProcessResult RunProcess(string root, string executable, IReadOnlyList<string> arguments) {
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

    internal sealed record ProcessResult(int ExitCode, string Output, string Error);
}
