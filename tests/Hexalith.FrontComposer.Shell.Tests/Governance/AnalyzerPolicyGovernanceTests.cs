using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Xml.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Governance;

[Trait("Category", "Governance")]
public sealed class AnalyzerPolicyGovernanceTests
{
    private const string LedgerPath = "_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json";
    private const string ContractsProject = "src/Hexalith.FrontComposer.Contracts/Hexalith.FrontComposer.Contracts.csproj";
    private const string Story1122ByLocationDigest = "3d3fdd71f06585b85307381775ae03172aeedc33cc6c8b6fc4bed77661f5a239";
    private const int DotnetTimeoutMilliseconds = 180_000;
    private const int GitTimeoutMilliseconds = 60_000;

    private static readonly string[] _requiredDispositionFields =
    [
        "key",
        "sourceKind",
        "exactScope",
        "mechanism",
        "decision",
        "rationale",
        "owner",
        "decisionDate",
        "reviewDate",
        "trigger",
        "evidence",
    ];

    private static readonly string[] Story1122StrictProjects =
    [
        "tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj",
        "tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj",
        "tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj",
        "tests/Hexalith.FrontComposer.Testing.Tests/Hexalith.FrontComposer.Testing.Tests.csproj",
        "tests/Hexalith.FrontComposer.Contracts.Tests/Hexalith.FrontComposer.Contracts.Tests.csproj",
        "tests/Hexalith.FrontComposer.Contracts.UI.Tests/Hexalith.FrontComposer.Contracts.UI.Tests.csproj",
        "tests/Hexalith.FrontComposer.Cli.Tests/Hexalith.FrontComposer.Cli.Tests.csproj",
        "tests/Hexalith.FrontComposer.Shell.Tests.Bench/Hexalith.FrontComposer.Shell.Tests.Bench.csproj",
        "samples/Counter/Counter.Domain/Counter.Domain.csproj",
        "samples/Counter/Counter.Specimens.Domain/Counter.Specimens.Domain.csproj",
        "samples/Counter/Counter.Specimens/Counter.Specimens.csproj",
        "samples/Counter/Counter.Web/Counter.Web.csproj",
        "samples/IdeParityCounter/IdeParityCounter.csproj",
    ];

    private static readonly string[] Story1122ExecutedTestAssemblies =
    [
        "tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Release/net10.0/Hexalith.FrontComposer.SourceTools.Tests",
        "tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests",
        "tests/Hexalith.FrontComposer.Mcp.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Mcp.Tests",
        "tests/Hexalith.FrontComposer.Testing.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Testing.Tests",
        "tests/Hexalith.FrontComposer.Contracts.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Contracts.Tests",
        "tests/Hexalith.FrontComposer.Contracts.UI.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Contracts.UI.Tests",
        "tests/Hexalith.FrontComposer.Cli.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Cli.Tests",
        "tests/Hexalith.FrontComposer.Shell.Tests.Bench/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests.Bench",
    ];

    private static readonly (string RelativePath, string Method, string DiagnosticId, string SyntaxDigest)[] Story1122FixturePragmaSpans =
    [
        ("tests/Hexalith.FrontComposer.Shell.Tests/Services/DataGridFocusScopeTests.cs", "MethodsReuseImportedKeyboardModule_AndDisposeItOnce", "CA2012", "46acefd605194e61985cb5db9714d1734c36151aa024de2d5b1886fab9108cc9"),
        ("tests/Hexalith.FrontComposer.Shell.Tests/Services/DataGridFocusScopeTests.cs", "MethodsReuseImportedKeyboardModule_AndDisposeItOnce", "CA2012", "858fd0f8535dee4371fd79d0fae28de2646a5e205fee036c2fb57f6253aab2ac"),
        ("tests/Hexalith.FrontComposer.Shell.Tests/Services/DataGridFocusScopeTests.cs", "MethodsReuseImportedKeyboardModule_AndDisposeItOnce", "CA2012", "5050bcf4700f843fe935ebd7ca6a717191c6b496e1d69b36344b93f4b3f815dd"),
        ("tests/Hexalith.FrontComposer.Shell.Tests/Services/DataGridFocusScopeTests.cs", "MethodsReuseImportedKeyboardModule_AndDisposeItOnce", "CA2012", "e9d9efb05b0caca023399945d3f0e3173b394df0576109628452e3e3d9c62556"),
        ("tests/Hexalith.FrontComposer.Shell.Tests/Services/DataGridFocusScopeTests.cs", "MethodsReuseImportedKeyboardModule_AndDisposeItOnce", "CA2012", "7f4c652e7af9b4249d3e4fbacd36efef89cebe1d8a0a3d7a131ecbcd0bd98a02"),
        ("tests/Hexalith.FrontComposer.Shell.Tests/Services/DataGridFocusScopeTests.cs", "MethodsReuseImportedKeyboardModule_AndDisposeItOnce", "CA2012", "7b42ffd418f2d78b35e21ecb2467f4bd5c8c821d6b92f8f26ecc5f4939021ea3"),
        ("tests/Hexalith.FrontComposer.Shell.Tests/Services/DataGridFocusScopeTests.cs", "FaultedImport_IsClearedSoLaterCallCanRetry", "CA2012", "da7a4d048518a0034c96ddb1c95da76fbd8cc5601cd56b896d6dcfb089f08e23"),
        ("tests/Hexalith.FrontComposer.Shell.Tests/Services/DataGridFocusScopeTests.cs", "FaultedImport_IsClearedSoLaterCallCanRetry", "CA2012", "858fd0f8535dee4371fd79d0fae28de2646a5e205fee036c2fb57f6253aab2ac"),
        ("tests/Hexalith.FrontComposer.Shell.Tests/Services/DataGridFocusScopeTests.cs", "FaultedImport_IsClearedSoLaterCallCanRetry", "CA2012", "902b042a7b61380cfc7a3fb9200e813478390857276b0b78512ea2f9a7c5d102"),
        ("tests/Hexalith.FrontComposer.Shell.Tests/State/PendingCommands/PendingCommandPollingCoordinatorTests.cs", "PollOnce_ProcessesPendingCommandsOldestFirstWithinCap", "CA2012", "093ed87f1e0172954994115d22a6bdd7fe2a99bfcfa3d25667b58dd815c8d2b5"),
        ("tests/Hexalith.FrontComposer.Shell.Tests/State/PendingCommands/PendingCommandPollingCoordinatorTests.cs", "PollOnce_StatusQueryThrows_LogsAndContinues", "CA2012", "e750323b3aac474e90b1e057a2f469faf1dba28be5a77cd34e5016f9bb012f19"),
        ("tests/Hexalith.FrontComposer.Shell.Tests/Services/ExceptionGuardTests.cs", "IsFatal_FourAuthoritativeFatalTypes_ReturnsTrue", "CA2201", "aeff26a709cbc1e82a3309dc208b5f87c1c43eb27eb930082860ab30fc4821e2"),
        ("tests/Hexalith.FrontComposer.Shell.Tests/Services/ExceptionGuardTests.cs", "IsFatal_FourAuthoritativeFatalTypes_ReturnsTrue", "CA2201", "ca19a7b9a86cfc3bfa63599b300818d20a125e0907cba3d18f8d2e8391f23ccb"),
        ("tests/Hexalith.FrontComposer.Shell.Tests/Services/ExceptionGuardTests.cs", "IsFatal_FourAuthoritativeFatalTypes_ReturnsTrue", "CA2201", "7f93abfbe352155a226382b86ebed10c64080d476061f4d24d4a7dbe088993a0"),
        ("tests/Hexalith.FrontComposer.Shell.Tests/Services/EmptyStateCtaResolverTests.cs", "RegistryThrowsAccessViolation_PropagatesFatalException", "CA2201", "031dd52f093fed96696625cf74275276da125bdb19d1faa1d94313d6d239d286"),
    ];

    [Fact]
    public void AnalyzerPolicy_GovernanceContract_FailsClosed()
    {
        string root = RepositoryRoot();
        JsonObject ledger = LoadLedger(root);

        // Accumulate every positive lane before asserting so a single failing lane cannot
        // mask the others. The identifier seal and the executable proofs are separate facts
        // so that seal drift - historically the common failure - never skips them.
        List<string> errors =
        [
            .. ValidateDocument(ledger),
            .. ValidateControlParity(root, ledger),
            .. ValidateRepositoryPolicy(root, ledger),
            .. ValidateConfigurationClosure(root),
            .. ValidateStory1122Evidence(ledger),
            .. ValidateStory1122FixtureSourceScopes(root),
        ];
        errors.ShouldBeEmpty();

        JsonObject missingOwner = Clone(ledger);
        RequiredArray(missingOwner, "dispositions")[0]!["owner"] = string.Empty;
        ValidateDocument(missingOwner).ShouldContain(static error => error.Contains("owner", StringComparison.Ordinal));

        JsonObject expiredReview = Clone(ledger);
        RequiredArray(expiredReview, "dispositions")[0]!["reviewDate"] = "2026-01-01";
        ValidateDocument(expiredReview).ShouldContain(static error => error.Contains("expired", StringComparison.Ordinal));

        // A malformed review date must fail closed rather than silently skipping the expiry check.
        JsonObject unparseableReview = Clone(ledger);
        RequiredArray(unparseableReview, "dispositions")[0]!["reviewDate"] = "not-a-date";
        ValidateDocument(unparseableReview)
            .ShouldContain(static error => error.Contains("unparseable review date", StringComparison.Ordinal));

        // A culture-dependent parse would accept this on some machines; the invariant
        // yyyy-MM-dd contract must reject it everywhere.
        JsonObject localisedReview = Clone(ledger);
        RequiredArray(localisedReview, "dispositions")[0]!["reviewDate"] = "17/07/2027";
        ValidateDocument(localisedReview)
            .ShouldContain(static error => error.Contains("unparseable review date", StringComparison.Ordinal));

        string[] configuredKeys = ConfiguredControlKeys(root);
        ValidateParity(configuredKeys.Skip(1), configuredKeys)
            .ShouldContain(static error => error.Contains("unledgered", StringComparison.Ordinal));
        ValidateParity(configuredKeys.Append("stale-control"), configuredKeys)
            .ShouldContain(static error => error.Contains("stale", StringComparison.Ordinal));

        JsonObject rootNoWarn = Clone(ledger);
        RequiredArray(rootNoWarn, "warningControls").Add(new JsonObject
        {
            ["key"] = "invalid-root-ca-nowarn",
            ["sourceKind"] = "msbuild",
            ["path"] = "Directory.Build.props",
            ["property"] = "NoWarn",
            ["diagnosticIds"] = new JsonArray("CA9999"),
            ["exactScope"] = "repository",
            ["mechanism"] = "NoWarn",
            ["dispositionKey"] = "policy-root-twae",
        });
        ValidateDocument(rootNoWarn).ShouldContain(static error => error.Contains("root NoWarn", StringComparison.Ordinal));

        // WarningsNotAsErrors neutralises TreatWarningsAsErrors=true for a named CA rule, so a
        // root-scoped entry is a policy bypass even though the diagnostic still reports.
        foreach (string bypassProperty in new[] { "WarningsNotAsErrors", "WarningsAsErrors" })
        {
            JsonObject rootBypass = Clone(ledger);
            RequiredArray(rootBypass, "warningControls").Add(new JsonObject
            {
                ["key"] = "invalid-root-ca-" + bypassProperty,
                ["sourceKind"] = "msbuild",
                ["path"] = "Directory.Build.props",
                ["property"] = bypassProperty,
                ["diagnosticIds"] = new JsonArray("CA1707"),
                ["exactScope"] = "repository",
                ["mechanism"] = bypassProperty,
                ["dispositionKey"] = "policy-root-twae",
            });
            ValidateDocument(rootBypass)
                .ShouldContain(error => error.Contains("root " + bypassProperty, StringComparison.Ordinal));
        }

        (string Section, string Property, string Value)[] categoryDisables =
        [
            ("[*.cs]", "dotnet_analyzer_diagnostic.category-Naming.severity", "none"),
            ("[**.cs]", "dotnet_analyzer_diagnostic.category-Naming.severity", "silent"),
            ("[*]", "dotnet_analyzer_diagnostic.category-Naming.severity", "none"),
            ("[*.cs]", "dotnet_analyzer_diagnostic.category-Naming.severity", "suggestion"),
            ("[*.cs]", "dotnet_analyzer_diagnostic.category-Naming.severity", "hidden"),
            ("[*.cs]", "dotnet_analyzer_diagnostic.severity", "none"),
            ("[*]", "dotnet_analyzer_diagnostic.severity", "suggestion"),
        ];
        foreach ((string disableSection, string disableProperty, string disableValue) in categoryDisables)
        {
            JsonObject categoryDisable = Clone(ledger);
            RequiredArray(categoryDisable, "warningControls").Add(new JsonObject
            {
                ["key"] = $"invalid-category-disable|{disableSection}|{disableProperty}|{disableValue}",
                ["sourceKind"] = "editorconfig",
                ["path"] = ".editorconfig",
                ["section"] = disableSection,
                ["property"] = disableProperty,
                ["value"] = disableValue,
                ["diagnosticIds"] = new JsonArray("category-Naming"),
                ["exactScope"] = "repository",
                ["mechanism"] = "EditorConfig severity",
                ["dispositionKey"] = "policy-root-twae",
            });
            ValidateDocument(categoryDisable)
                .ShouldContain(static error => error.Contains("category disable", StringComparison.Ordinal));
        }

        JsonObject wildcardProduction = Clone(ledger);
        JsonObject productionDisposition = RequiredObject(RequiredArray(wildcardProduction, "dispositions")[1], "disposition");
        productionDisposition["exactScope"] = "src/**.cs";
        ValidateDocument(wildcardProduction).ShouldContain(static error => error.Contains("wildcard production", StringComparison.Ordinal));

        JsonObject countDrift = Clone(ledger);
        RequiredObject(countDrift, "implementationSnapshot")["namingFindings"] = 42;
        ValidateDocument(countDrift).ShouldContain(static error => error.Contains("count drift", StringComparison.Ordinal));

        JsonObject unmatchedFinding = Clone(ledger);
        RequiredObject(RequiredArray(unmatchedFinding, "findings")[0], "finding")["dispositionKey"] = "missing-disposition";
        ValidateDocument(unmatchedFinding)
            .ShouldContain(static error => error.Contains("unmatched finding disposition", StringComparison.Ordinal));

        JsonObject story1122CountDrift = Clone(ledger);
        RequiredObject(story1122CountDrift, "story1122Census")["visibleFindings"] = 344;
        ValidateStory1122Evidence(story1122CountDrift)
            .ShouldContain(static error => error.Contains("Story 11.22 census count drift", StringComparison.Ordinal));

        JsonObject broadenedFixtureScope = Clone(ledger);
        JsonObject broadenedDisposition = RequiredArray(broadenedFixtureScope, "dispositions")
            .Select(static item => RequiredObject(item, "disposition"))
            .Single(static disposition => StringValue(disposition, "key") == "story1122-ca2201-empty-state-fatal-fixture");
        broadenedDisposition["exactScope"] = "tests/Hexalith.FrontComposer.Shell.Tests/Services/EmptyStateCtaResolverTests.cs";
        ValidateStory1122Evidence(broadenedFixtureScope)
            .ShouldContain(static error => error.Contains("exact scope drift", StringComparison.Ordinal));

        JsonObject balancedByLocationDrift = Clone(ledger);
        JsonObject driftedLocations = RequiredObject(RequiredObject(balancedByLocationDrift, "story1122Census"), "byLocation");
        driftedLocations["tests/Hexalith.FrontComposer.Contracts.Tests/Communication/QueryRequestTests.cs"] = 4;
        driftedLocations["tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/CommandInvokerTests.cs"] = 2;
        ValidateStory1122Evidence(balancedByLocationDrift)
            .ShouldContain(static error => error.Contains("by-location path/count seal drift", StringComparison.Ordinal));

        JsonObject missingStrictProject = Clone(ledger);
        RequiredArray(RequiredObject(missingStrictProject, "story1122Completion"), "strictProjects").RemoveAt(0);
        ValidateStory1122Evidence(missingStrictProject)
            .ShouldContain(static error => error.Contains("strict project matrix drift", StringComparison.Ordinal));

        JsonObject wrongTestAssembly = Clone(ledger);
        RequiredArray(RequiredObject(wrongTestAssembly, "story1122Completion"), "executedTestAssemblies")[5]
            = "tests/Hexalith.FrontComposer.Contracts.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Contracts.Tests";
        ValidateStory1122Evidence(wrongTestAssembly)
            .ShouldContain(static error => error.Contains("executed test assembly matrix drift", StringComparison.Ordinal));

        JsonObject testError = Clone(ledger);
        RequiredObject(testError, "story1122Completion")["testErrors"] = 1;
        ValidateStory1122Evidence(testError)
            .ShouldContain(static error => error.Contains("completion evidence drift", StringComparison.Ordinal));

        JsonObject mismatchedCoordinate = Clone(ledger);
        RequiredObject(mismatchedCoordinate, "story1122Completion")["baselineCommit"]
            = "0000000000000000000000000000000000000000";
        ValidateStory1122Evidence(mismatchedCoordinate)
            .ShouldContain(static error => error.Contains("census/completion coordinate drift", StringComparison.Ordinal));

        JsonObject malformedCoordinate = Clone(ledger);
        RequiredObject(malformedCoordinate, "story1122Completion")["sdk"] = "10.0-preview";
        ValidateStory1122Evidence(malformedCoordinate)
            .ShouldContain(static error => error.Contains("completion SDK has an invalid shape", StringComparison.Ordinal));

        JsonObject broadenedHiddenControl = Clone(ledger);
        JsonObject aspProbe = RequiredObject(
            RequiredObject(RequiredObject(broadenedHiddenControl, "story1122Census"), "hiddenControlProbes"),
            "ASP0006");
        aspProbe["command"] = StringValue(aspProbe, "command").Replace(
            "-p:NoWarn=0419%3B1570%3B1572%3B1573%3B1574%3B1734",
            "-p:NoWarn=0419%3B1570%3B1572%3B1573%3B1574%3B1734%3BASP0006",
            StringComparison.Ordinal);
        ValidateStory1122Evidence(broadenedHiddenControl)
            .ShouldContain(static error => error.Contains("hidden-control scope/command drift", StringComparison.Ordinal));
    }

    /// <summary>
    /// The sealed identifier inventory drifts whenever ordinary repository evolution adds an
    /// underscore-named test identifier, so it is asserted on its own. Keeping it out of the
    /// contract fact stops a routine reseal from skipping the executable proofs below.
    /// </summary>
    [Fact]
    public void AnalyzerPolicy_IdentifierInventory_MatchesSeal()
    {
        string root = RepositoryRoot();
        ValidateIdentifierInventory(root, LoadLedger(root)).ShouldBeEmpty();
    }

    [Fact]
    public async Task AnalyzerPolicy_EffectiveBuildGraphs_MatchCanonicalPolicy()
        => await ValidateEffectiveBuildGraphsAsync(RepositoryRoot()).ConfigureAwait(true);

    [Fact]
    public async Task AnalyzerPolicy_CompileSpecimens_ProveExactCa1707Scopes()
        => await ValidateCompileSpecimensAsync(RepositoryRoot()).ConfigureAwait(true);

    [Fact]
    public async Task AnalyzerPolicy_Story1122RecordedProjects_RemainRecommendedClean()
    {
        string root = RepositoryRoot();
        Story1122StrictProjects.Length.ShouldBe(13);

        foreach (string project in Story1122StrictProjects)
        {
            string[] arguments = Story1122StrictBuildArguments(project);
            arguments.ShouldContain("-p:AnalysisMode=Recommended");
            arguments.ShouldNotContain(static argument => argument.StartsWith("-p:TreatWarningsAsErrors=", StringComparison.Ordinal));

            (int exitCode, string output) = await RunDotnetResultAsync(root, arguments).ConfigureAwait(true);
            exitCode.ShouldBe(0, $"Strict Recommended regression gate failed for {project}:{Environment.NewLine}{output}");
            Regex.IsMatch(output, @"^\s*0 Warning\(s\)\s*$", RegexOptions.Multiline | RegexOptions.CultureInvariant)
                .ShouldBeTrue($"Strict Recommended regression gate did not prove zero warnings for {project}:{Environment.NewLine}{output}");
            Regex.IsMatch(output, @"^\s*0 Error\(s\)\s*$", RegexOptions.Multiline | RegexOptions.CultureInvariant)
                .ShouldBeTrue($"Strict Recommended regression gate did not prove zero errors for {project}:{Environment.NewLine}{output}");
        }
    }

    private static string[] ValidateDocument(JsonObject ledger)
    {
        List<string> errors = [];
        RequireValue(ledger, "schemaVersion", errors);
        RequireValue(ledger, "contractId", errors);
        RequireValue(ledger, "decision", errors);
        RequireValue(ledger, "owner", errors);
        RequireValue(ledger, "approval", errors);

        JsonObject baseline = RequiredObject(ledger, "baseline");
        JsonObject refreshed = RequiredObject(ledger, "refreshedCensus");
        JsonObject implementation = RequiredObject(ledger, "implementationSnapshot");
        JsonObject toolchain = RequiredObject(ledger, "toolchain");
        foreach (string field in new[] { "commit", "sdk", "msbuild", "roslyn", "utcDate", "command" })
        {
            RequireValue(refreshed, field, errors);
        }

        foreach (string field in new[] { "sdk", "msbuild", "roslyn" })
        {
            RequireValue(toolchain, field, errors);
        }

        if (!string.Equals(StringValue(ledger, "schemaVersion"), "1.0", StringComparison.Ordinal))
        {
            errors.Add("unsupported schemaVersion");
        }

        JsonArray dispositions = RequiredArray(ledger, "dispositions");
        HashSet<string> dispositionKeys = new(StringComparer.Ordinal);
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        foreach (JsonNode? item in dispositions)
        {
            JsonObject disposition = RequiredObject(item, "disposition");
            foreach (string field in _requiredDispositionFields)
            {
                RequireValue(disposition, field, errors);
            }

            string key = StringValue(disposition, "key");
            if (!dispositionKeys.Add(key))
            {
                errors.Add($"duplicate disposition key {key}");
            }

            string decision = StringValue(disposition, "decision");
            if (decision is not ("remain" or "narrow" or "move" or "fix"))
            {
                errors.Add($"invalid decision for {key}");
            }

            // Parse exactly, invariantly, and fail closed: an ambient-culture TryParse guarded by
            // `&&` would let a malformed or localised value skip the expiry check entirely.
            string reviewDateText = StringValue(disposition, "reviewDate");
            if (!DateOnly.TryParseExact(
                    reviewDateText,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateOnly reviewDate))
            {
                errors.Add($"unparseable review date for {key}: {reviewDateText}");
            }
            else if (reviewDate < today)
            {
                errors.Add($"expired review date for {key}");
            }

            string exactScope = StringValue(disposition, "exactScope");
            if (exactScope.StartsWith("src/", StringComparison.Ordinal)
                && exactScope.Contains('*', StringComparison.Ordinal))
            {
                errors.Add($"wildcard production scope for {key}");
            }

            ValidateSafePath(exactScope, $"disposition {key}", errors);

            if (decision == "move" && string.IsNullOrWhiteSpace(StringValue(disposition, "followUpStory")))
            {
                errors.Add($"move disposition {key} has no follow-up story");
            }
        }

        JsonArray findings = RequiredArray(ledger, "findings");
        HashSet<string> findingKeys = new(StringComparer.Ordinal);
        int baselineCount = 0;
        int refreshedCount = 0;
        int implementationCount = 0;
        int outcomeCount = 0;
        foreach (JsonNode? item in findings)
        {
            JsonObject finding = RequiredObject(item, "finding");
            foreach (string field in new[]
            {
                "key", "diagnosticId", "project", "targetFramework", "path", "lineOrSymbol",
                "generatedSource", "dispositionKey", "baselineCount", "refreshedCount",
                "implementationCount", "policyOutcomeCount",
            })
            {
                RequireValue(finding, field, errors);
            }

            string key = StringValue(finding, "key");
            if (!findingKeys.Add(key))
            {
                errors.Add($"duplicate finding key {key}");
            }

            string dispositionKey = StringValue(finding, "dispositionKey");
            if (!dispositionKeys.Contains(dispositionKey))
            {
                errors.Add($"unmatched finding disposition {dispositionKey}");
            }

            ValidateSafePath(StringValue(finding, "path"), $"finding {key}", errors);
            baselineCount += IntValue(finding, "baselineCount");
            refreshedCount += IntValue(finding, "refreshedCount");
            implementationCount += IntValue(finding, "implementationCount");
            outcomeCount += IntValue(finding, "policyOutcomeCount");
        }

        if (baselineCount != IntValue(baseline, "namingFindings")
            || refreshedCount != IntValue(refreshed, "namingFindings")
            || implementationCount != IntValue(implementation, "namingFindings")
            || outcomeCount != IntValue(implementation, "policyOutcomeNamingFindings"))
        {
            errors.Add("finding count drift");
        }

        if (IntValue(baseline, "recommendedFindings") != 4070
            || IntValue(baseline, "namingFindings") != 2958
            || IntValue(refreshed, "namingFindings") != 2959)
        {
            errors.Add("approved or refreshed count drift");
        }

        JsonArray controls = RequiredArray(ledger, "warningControls");
        HashSet<string> controlKeys = new(StringComparer.Ordinal);
        foreach (JsonNode? item in controls)
        {
            JsonObject control = RequiredObject(item, "control");
            foreach (string field in new[]
            {
                "key", "sourceKind", "exactScope", "mechanism", "diagnosticIds", "dispositionKey",
            })
            {
                RequireValue(control, field, errors);
            }

            string key = StringValue(control, "key");
            if (!controlKeys.Add(key))
            {
                errors.Add($"duplicate warning control key {key}");
            }

            if (!dispositionKeys.Contains(StringValue(control, "dispositionKey")))
            {
                errors.Add($"unmatched control disposition for {key}");
            }

            string sourceKind = StringValue(control, "sourceKind");
            string path = StringValue(control, "path");
            string property = StringValue(control, "property");
            string section = StringValue(control, "section");
            string value = StringValue(control, "value");
            string[] diagnosticIds = StringArray(control, "diagnosticIds");
            // NoWarn silences a CA rule; WarningsNotAsErrors demotes it back to a warning and so
            // neutralises the canonical TreatWarningsAsErrors=true; WarningsAsErrors re-manages
            // severity per rule at root scope. All three belong in EditorConfig at an exact scope,
            // never in root MSBuild policy, so all three are policed identically.
            if (sourceKind == "msbuild"
                && property is "NoWarn" or "WarningsNotAsErrors" or "WarningsAsErrors"
                && IsRootPolicyPath(path)
                && diagnosticIds.Any(static id => id.StartsWith("CA", StringComparison.OrdinalIgnoreCase)))
            {
                errors.Add($"root {property} contains a CA entry in {key}");
            }

            // Catch every shape of bulk/category disable: the repository-wide `[*]` section, the
            // bulk `dotnet_analyzer_diagnostic.severity` property, and every severity that stops a
            // rule failing the build under TreatWarningsAsErrors.
            if (sourceKind == "editorconfig"
                && (section is "[*.cs]" or "[**.cs]" or "[*]")
                && (property.StartsWith("dotnet_analyzer_diagnostic.category-", StringComparison.Ordinal)
                    || property == "dotnet_analyzer_diagnostic.severity")
                && value is "none" or "silent" or "suggestion" or "hidden")
            {
                errors.Add($"root/category CA category disable in {key}");
            }
        }

        return [.. errors];
    }

    private static string[] ValidateControlParity(string root, JsonObject ledger)
    {
        string[] ledgerKeys = RequiredArray(ledger, "warningControls")
            .Select(static item => CanonicalLedgerControl(RequiredObject(item, "control")))
            .Order(StringComparer.Ordinal)
            .ToArray();
        return ValidateParity(ledgerKeys, ConfiguredControlKeys(root));
    }

    private static string[] ValidateStory1122Evidence(JsonObject ledger)
    {
        List<string> errors = [];
        JsonObject census = RequiredObject(ledger, "story1122Census");
        JsonObject completion = RequiredObject(ledger, "story1122Completion");
        foreach (string field in new[]
        {
            "commit", "historicalImplementationBaseline", "sdk", "msbuild", "roslyn", "utcDate",
            "command", "generatedCodeTreatment", "driftFromStory1121Handoff", "note",
        })
        {
            RequireValue(census, field, errors);
        }

        ValidateCommit(StringValue(census, "commit"), "Story 11.22 census commit", errors);
        ValidateCommit(StringValue(census, "historicalImplementationBaseline"), "Story 11.22 historical implementation baseline", errors);
        ValidateVersion(StringValue(census, "sdk"), threeComponents: true, "Story 11.22 census SDK", errors);
        ValidateVersion(StringValue(census, "msbuild"), threeComponents: false, "Story 11.22 census MSBuild", errors);
        ValidateVersion(StringValue(census, "roslyn"), threeComponents: true, "Story 11.22 census Roslyn", errors);
        ValidateUtcTimestamp(StringValue(census, "utcDate"), "Story 11.22 census UTC date", errors);

        JsonObject byProject = RequiredObject(census, "byProject");
        RequireCount(byProject, "Hexalith.FrontComposer.SourceTools.Tests", 229, errors);
        RequireCount(byProject, "Hexalith.FrontComposer.Shell.Tests", 70, errors);
        RequireCount(byProject, "Hexalith.FrontComposer.Mcp.Tests", 25, errors);
        RequireCount(byProject, "Hexalith.FrontComposer.Testing.Tests", 7, errors);
        RequireCount(byProject, "Hexalith.FrontComposer.Contracts.Tests", 6, errors);
        RequireCount(byProject, "Hexalith.FrontComposer.Shell.Tests.Bench", 4, errors);
        RequireCount(byProject, "Hexalith.FrontComposer.Cli.Tests", 2, errors);
        RequireCount(byProject, "Counter.Web", 2, errors);
        if (byProject.Count != 8)
        {
            errors.Add($"Story 11.22 project census is not closed: {byProject.Count} rows");
        }

        JsonObject byDiagnostic = RequiredObject(census, "byDiagnostic");
        RequireCount(byDiagnostic, "CA1305", 195, errors);
        RequireCount(byDiagnostic, "CA1859", 65, errors);
        RequireCount(byDiagnostic, "CA1861", 23, errors);
        RequireCount(byDiagnostic, "CA1875", 12, errors);
        RequireCount(byDiagnostic, "CA2012", 12, errors);
        RequireCount(byDiagnostic, "CA1826", 7, errors);
        RequireCount(byDiagnostic, "CA1865", 7, errors);
        RequireCount(byDiagnostic, "CA2201", 4, errors);
        RequireCount(byDiagnostic, "CA1863", 4, errors);
        RequireCount(byDiagnostic, "CA1869", 3, errors);
        RequireCount(byDiagnostic, "CA2263", 2, errors);
        RequireCount(byDiagnostic, "CA2249", 2, errors);
        RequireCount(byDiagnostic, "CA1512", 2, errors);
        RequireCount(byDiagnostic, "CA1310", 2, errors);
        RequireCount(byDiagnostic, "CA1854", 1, errors);
        RequireCount(byDiagnostic, "CA1848", 1, errors);
        RequireCount(byDiagnostic, "CA1827", 1, errors);
        RequireCount(byDiagnostic, "CA1513", 1, errors);
        RequireCount(byDiagnostic, "CA1507", 1, errors);
        if (byDiagnostic.Count != 19)
        {
            errors.Add($"Story 11.22 diagnostic census is not closed: {byDiagnostic.Count} rows");
        }

        JsonObject byOrigin = RequiredObject(census, "byOrigin");
        RequireCount(byOrigin, "handAuthoredTest", 339, errors);
        RequireCount(byOrigin, "handAuthoredBenchmark", 4, errors);
        RequireCount(byOrigin, "sample", 2, errors);
        RequireCount(byOrigin, "product", 0, errors);
        RequireCount(byOrigin, "generatedTree", 0, errors);
        if (byOrigin.Count != 5)
        {
            errors.Add($"Story 11.22 origin census is not closed: {byOrigin.Count} rows");
        }

        JsonObject byLocation = RequiredObject(census, "byLocation");
        foreach ((string path, JsonNode? count) in byLocation)
        {
            ValidateSafePath(path, "Story 11.22 census location", errors);
            if (count?.GetValue<int>() <= 0)
            {
                errors.Add($"Story 11.22 census location has no findings: {path}");
            }
        }

        string recordedByLocationDigest = StringValue(census, "byLocationDigest");
        if (recordedByLocationDigest != Story1122ByLocationDigest
            || CanonicalCountDigest(byLocation) != Story1122ByLocationDigest)
        {
            errors.Add("Story 11.22 by-location path/count seal drift");
        }

        int visibleFindings = IntValue(census, "visibleFindings");
        if (visibleFindings != 345
            || SumCounts(byProject) != visibleFindings
            || SumCounts(byDiagnostic) != visibleFindings
            || SumCounts(byOrigin) != visibleFindings
            || SumCounts(byLocation) != visibleFindings)
        {
            errors.Add("Story 11.22 census count drift");
        }

        JsonObject hiddenControlProbes = RequiredObject(census, "hiddenControlProbes");
        ValidateHiddenControlProbe(
            hiddenControlProbes,
            "ASP0006",
            17,
            "Shell.Tests hand-authored render fragments",
            "dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Release --no-restore --no-incremental -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0 -p:AnalysisMode=Recommended -p:NoWarn=0419%3B1570%3B1572%3B1573%3B1574%3B1734",
            errors);
        ValidateHiddenControlProbe(
            hiddenControlProbes,
            "CA2007",
            6,
            "Testing.Tests async-disposal sites",
            "dotnet build tests/Hexalith.FrontComposer.Testing.Tests/Hexalith.FrontComposer.Testing.Tests.csproj -c Release --no-restore --no-incremental -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0 -p:AnalysisMode=Recommended -p:NoWarn=0419%3B1570%3B1572%3B1573%3B1574%3B1734",
            errors);

        JsonObject testingDisposition = FindDisposition(ledger, "testing-ca2007-audit");
        JsonObject aspDisposition = FindDisposition(ledger, "asp0006-hand-authored-fixture-debt");
        if (StringValue(testingDisposition, "decision") != "fix"
            || !StringValue(testingDisposition, "exactScope").StartsWith("no remaining control", StringComparison.Ordinal)
            || StringValue(aspDisposition, "decision") != "fix"
            || !StringValue(aspDisposition, "exactScope").StartsWith("no remaining control", StringComparison.Ordinal))
        {
            errors.Add("Story 11.22 moved-control dispositions are not closed as fixes");
        }

        string[] warningControlKeys = RequiredArray(ledger, "warningControls")
            .Select(static item => StringValue(RequiredObject(item, "control"), "key"))
            .ToArray();
        if (warningControlKeys.Contains("msbuild-testing-tests-nowarn", StringComparer.Ordinal)
            || warningControlKeys.Contains("msbuild-shell-tests-nowarn", StringComparer.Ordinal))
        {
            errors.Add("Story 11.22 removed project control still has a ledger row");
        }

        ValidateFixtureDisposition(
            ledger,
            "story1122-ca2012-datagrid-focus-valuetask-fixtures",
            "CA2012",
            "tests/Hexalith.FrontComposer.Shell.Tests/Services/DataGridFocusScopeTests.cs#MethodsReuseImportedKeyboardModule_AndDisposeItOnce: six directive-adjacent NSubstitute statements; #FaultedImport_IsClearedSoLaterCallCanRetry: three directive-adjacent NSubstitute statements",
            errors);
        ValidateFixtureDisposition(
            ledger,
            "story1122-ca2012-pending-poll-valuetask-fixtures",
            "CA2012",
            "tests/Hexalith.FrontComposer.Shell.Tests/State/PendingCommands/PendingCommandPollingCoordinatorTests.cs: one directive-adjacent NSubstitute Returns statement in #PollOnce_ProcessesPendingCommandsOldestFirstWithinCap and one in #PollOnce_StatusQueryThrows_LogsAndContinues",
            errors);
        ValidateFixtureDisposition(
            ledger,
            "story1122-ca2201-exception-guard-fatal-fixtures",
            "CA2201",
            "tests/Hexalith.FrontComposer.Shell.Tests/Services/ExceptionGuardTests.cs#IsFatal_FourAuthoritativeFatalTypes_ReturnsTrue: three directive-adjacent fatal exception construction expressions",
            errors);
        ValidateFixtureDisposition(
            ledger,
            "story1122-ca2201-empty-state-fatal-fixture",
            "CA2201",
            "tests/Hexalith.FrontComposer.Shell.Tests/Services/EmptyStateCtaResolverTests.cs#RegistryThrowsAccessViolation_PropagatesFatalException: one directive-adjacent NSubstitute Returns statement",
            errors);

        if (IntValue(completion, "visibleFindingsBefore") != 345
            || IntValue(completion, "visibleFindingsAfter") != 0
            || IntValue(completion, "ownedProductFindingsAfter") != 0
            || IntValue(completion, "ownedGeneratedFindingsAfter") != 0
            || IntValue(completion, "asp0006NegativeControlAfter") != 0
            || IntValue(completion, "ca2007NegativeControlAfter") != 0
            || IntValue(completion, "strictProjectCount") != 13
            || IntValue(completion, "strictProjectWarnings") != 0
            || IntValue(completion, "strictProjectErrors") != 0
            || IntValue(completion, "changedTestAssemblyCount") != 8
            || IntValue(completion, "testTotal") != 4339
            || IntValue(completion, "testErrors") != 0
            || IntValue(completion, "testFailures") != 0
            || IntValue(completion, "testSkips") != 0
            || IntValue(completion, "testNotRun") != 0
            || IntValue(completion, "normalReleaseWarnings") != 0
            || IntValue(completion, "normalReleaseErrors") != 0)
        {
            errors.Add("Story 11.22 completion evidence drift");
        }

        foreach (string field in new[]
        {
            "baselineCommit", "sdk", "msbuild", "roslyn", "utcDate", "strictGate", "removedControlEvidence",
            "testEvidence", "protectedArtifactEvidence",
        })
        {
            RequireValue(completion, field, errors);
        }

        if (!StringArray(completion, "strictProjects").SequenceEqual(Story1122StrictProjects, StringComparer.Ordinal)
            || IntValue(completion, "strictProjectCount") != Story1122StrictProjects.Length)
        {
            errors.Add("Story 11.22 strict project matrix drift");
        }

        if (!StringArray(completion, "executedTestAssemblies").SequenceEqual(Story1122ExecutedTestAssemblies, StringComparer.Ordinal)
            || IntValue(completion, "changedTestAssemblyCount") != Story1122ExecutedTestAssemblies.Length)
        {
            errors.Add("Story 11.22 executed test assembly matrix drift");
        }

        ValidateCommit(StringValue(completion, "baselineCommit"), "Story 11.22 completion baseline commit", errors);
        ValidateVersion(StringValue(completion, "sdk"), threeComponents: true, "Story 11.22 completion SDK", errors);
        ValidateVersion(StringValue(completion, "msbuild"), threeComponents: false, "Story 11.22 completion MSBuild", errors);
        ValidateVersion(StringValue(completion, "roslyn"), threeComponents: true, "Story 11.22 completion Roslyn", errors);
        ValidateUtcTimestamp(StringValue(completion, "utcDate"), "Story 11.22 completion UTC date", errors);
        if (StringValue(census, "commit") != StringValue(completion, "baselineCommit")
            || StringValue(census, "sdk") != StringValue(completion, "sdk")
            || StringValue(census, "msbuild") != StringValue(completion, "msbuild")
            || StringValue(census, "roslyn") != StringValue(completion, "roslyn"))
        {
            errors.Add("Story 11.22 census/completion coordinate drift");
        }

        return [.. errors];
    }

    private static string[] ValidateStory1122FixtureSourceScopes(string root)
    {
        List<string> errors = [];
        string[] expected = Story1122FixturePragmaSpans
            .Select(static span => PragmaSpanKey(span.RelativePath, span.Method, span.DiagnosticId, span.SyntaxDigest))
            .Order(StringComparer.Ordinal)
            .ToArray();
        List<string> actual = [];

        foreach (IGrouping<string, (string RelativePath, string Method, string DiagnosticId, string SyntaxDigest)> fileSpans
            in Story1122FixturePragmaSpans.GroupBy(static span => span.RelativePath, StringComparer.Ordinal))
        {
            string source = File.ReadAllText(Path.Combine(root, fileSpans.Key));
            string[] lines = source.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
            MethodDeclarationSyntax[] methods = CSharpSyntaxTree.ParseText(source)
                .GetRoot()
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .ToArray();

            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                Match disable = Regex.Match(
                    lines[lineIndex],
                    @"^\s*#pragma\s+warning\s+disable\s+(CA2012|CA2201)\b(?:\s+//.*)?$",
                    RegexOptions.CultureInvariant);
                if (!disable.Success)
                {
                    continue;
                }

                string diagnosticId = disable.Groups[1].Value;
                int restoreIndex = lineIndex + 1;
                while (restoreIndex < lines.Length
                    && !Regex.IsMatch(
                        lines[restoreIndex],
                        @"^\s*#pragma\s+warning\s+restore\s+" + Regex.Escape(diagnosticId) + @"\s*$",
                        RegexOptions.CultureInvariant))
                {
                    restoreIndex++;
                }

                if (restoreIndex >= lines.Length
                    || restoreIndex == lineIndex + 1
                    || string.IsNullOrWhiteSpace(lines[lineIndex + 1])
                    || string.IsNullOrWhiteSpace(lines[restoreIndex - 1])
                    || lines[(lineIndex + 1)..restoreIndex].Any(string.IsNullOrWhiteSpace))
                {
                    errors.Add($"Story 11.22 {diagnosticId} directives are not immediately adjacent to one approved syntax span in {fileSpans.Key}:{lineIndex + 1}");
                    continue;
                }

                MethodDeclarationSyntax? method = methods.SingleOrDefault(candidate =>
                {
                    FileLinePositionSpan span = candidate.GetLocation().GetLineSpan();
                    return span.StartLinePosition.Line <= lineIndex && span.EndLinePosition.Line >= restoreIndex;
                });
                if (method is null)
                {
                    errors.Add($"Story 11.22 {diagnosticId} pragma is outside an approved method in {fileSpans.Key}:{lineIndex + 1}");
                    continue;
                }

                string normalizedSyntax = Regex.Replace(
                    string.Join(' ', lines[(lineIndex + 1)..restoreIndex].Select(static line => line.Trim())),
                    @"\s+",
                    " ",
                    RegexOptions.CultureInvariant);
                string syntaxDigest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalizedSyntax))).ToLowerInvariant();
                actual.Add(PragmaSpanKey(fileSpans.Key, method.Identifier.ValueText, diagnosticId, syntaxDigest));
                lineIndex = restoreIndex;
            }
        }

        if (!actual.Order(StringComparer.Ordinal).SequenceEqual(expected, StringComparer.Ordinal))
        {
            errors.Add("Story 11.22 fixture pragma directive-adjacent syntax/span/count seal drift");
        }

        return [.. errors];
    }

    private static string PragmaSpanKey(string relativePath, string method, string diagnosticId, string syntaxDigest)
        => $"{relativePath}|{method}|{diagnosticId}|{syntaxDigest}";

    private static void RequireCount(JsonObject counts, string key, int expected, List<string> errors)
    {
        if (IntValue(counts, key) != expected)
        {
            errors.Add($"Story 11.22 count drift for {key}");
        }
    }

    private static int SumCounts(JsonObject counts)
        => counts.Sum(static item => item.Value?.GetValue<int>() ?? 0);

    private static string CanonicalCountDigest(JsonObject counts)
    {
        string canonical = string.Concat(
            counts.OrderBy(static item => item.Key, StringComparer.Ordinal)
                .Select(static item => $"{item.Key}={item.Value?.GetValue<int>() ?? 0}\n"));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }

    private static void ValidateCommit(string value, string coordinate, List<string> errors)
    {
        if (!Regex.IsMatch(value, "^[0-9a-f]{40}$", RegexOptions.CultureInvariant))
        {
            errors.Add($"{coordinate} has an invalid shape");
        }
    }

    private static void ValidateVersion(string value, bool threeComponents, string coordinate, List<string> errors)
    {
        string pattern = threeComponents ? @"^\d+\.\d+\.\d+$" : @"^\d+\.\d+\.\d+\.\d+$";
        if (!Regex.IsMatch(value, pattern, RegexOptions.CultureInvariant))
        {
            errors.Add($"{coordinate} has an invalid shape");
        }
    }

    private static void ValidateUtcTimestamp(string value, string coordinate, List<string> errors)
    {
        if (!DateTimeOffset.TryParseExact(
            value,
            "yyyy-MM-dd'T'HH:mm:ss'Z'",
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out _))
        {
            errors.Add($"{coordinate} has an invalid shape");
        }
    }

    private static void ValidateHiddenControlProbe(
        JsonObject hiddenControlProbes,
        string diagnosticId,
        int expectedBefore,
        string expectedScope,
        string expectedCommand,
        List<string> errors)
    {
        JsonObject probe = RequiredObject(hiddenControlProbes, diagnosticId);
        if (IntValue(probe, "before") != expectedBefore || IntValue(probe, "after") != 0)
        {
            errors.Add($"Story 11.22 {diagnosticId} hidden-control probe drift");
        }

        string scope = StringValue(probe, "scope");
        string command = StringValue(probe, "command");
        if (scope != expectedScope || command != expectedCommand)
        {
            errors.Add($"Story 11.22 {diagnosticId} hidden-control scope/command drift");
        }

        if (!command.Contains("-p:AnalysisMode=Recommended", StringComparison.Ordinal)
            || !command.Contains("-p:NoWarn=0419%3B1570%3B1572%3B1573%3B1574%3B1734", StringComparison.Ordinal)
            || command.Contains("TreatWarningsAsErrors", StringComparison.Ordinal))
        {
            errors.Add($"Story 11.22 {diagnosticId} hidden-control command does not preserve the strict candidate policy while clearing only the intended project control");
        }
    }

    private static JsonObject FindDisposition(JsonObject ledger, string key)
        => RequiredArray(ledger, "dispositions")
            .Select(static item => RequiredObject(item, "disposition"))
            .Single(disposition => StringValue(disposition, "key") == key);

    private static void ValidateFixtureDisposition(
        JsonObject ledger,
        string key,
        string diagnosticId,
        string exactScope,
        List<string> errors)
    {
        JsonObject disposition = FindDisposition(ledger, key);
        if (StringValue(disposition, "exactScope") != exactScope)
        {
            errors.Add($"Story 11.22 exact scope drift for {key}");
        }

        if (StringValue(disposition, "decision") != "remain"
            || !StringArray(disposition, "diagnosticIds").SequenceEqual([diagnosticId], StringComparer.Ordinal))
        {
            errors.Add($"Story 11.22 diagnostic disposition drift for {key}");
        }
    }

    private static string[] ValidateParity(IEnumerable<string> ledgerKeys, IEnumerable<string> configuredKeys)
    {
        string[] ledger = ledgerKeys.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        string[] configured = configuredKeys.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        List<string> errors = [];
        errors.AddRange(configured.Except(ledger, StringComparer.Ordinal).Select(static key => $"unledgered control: {key}"));
        errors.AddRange(ledger.Except(configured, StringComparer.Ordinal).Select(static key => $"stale ledger row: {key}"));
        return [.. errors];
    }

    private static string[] ConfiguredControlKeys(string root)
    {
        string[] trackedFiles = TrackedFiles(root);
        List<string> controls = [];
        foreach (string relativePath in trackedFiles.Where(IsMsBuildFile))
        {
            XDocument document = XDocument.Load(Path.Combine(root, relativePath));
            foreach (XElement property in document.Descendants().Where(static element => IsWarningProperty(element.Name.LocalName)))
            {
                string propertyName = property.Name.LocalName;
                string[] values = propertyName == "NoWarn"
                    ? SplitDiagnosticIds(property.Value)
                    : [property.Value.Trim()];
                controls.Add(CanonicalMsBuild(relativePath, propertyName, values));
            }
        }

        string editorConfigPath = Path.Combine(root, ".editorconfig");
        string section = string.Empty;
        foreach (string rawLine in File.ReadLines(editorConfigPath))
        {
            string line = rawLine.Trim();
            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                section = line;
            }
            else if (line.StartsWith("dotnet_diagnostic.", StringComparison.Ordinal)
                || line.StartsWith("dotnet_analyzer_diagnostic.", StringComparison.Ordinal))
            {
                string[] parts = line.Split('=', 2, StringSplitOptions.TrimEntries);
                controls.Add(CanonicalEditorConfig(section, parts[0], parts[1]));
            }
        }

        string[] sourceFiles = trackedFiles.Where(static path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)).ToArray();
        controls.Add(CanonicalSourceSummary(root, sourceFiles, "pragma"));
        controls.Add(CanonicalSourceSummary(root, sourceFiles, "suppression-attribute"));
        controls.Add(CanonicalSourceSummary(root, sourceFiles, "emitter-pragma"));
        return [.. controls.Order(StringComparer.Ordinal)];
    }

    private static string CanonicalLedgerControl(JsonObject control)
    {
        string sourceKind = StringValue(control, "sourceKind");
        if (sourceKind == "msbuild")
        {
            return CanonicalMsBuild(
                StringValue(control, "path"),
                StringValue(control, "property"),
                StringArray(control, "diagnosticIds"));
        }

        if (sourceKind == "editorconfig")
        {
            return CanonicalEditorConfig(
                StringValue(control, "section"),
                StringValue(control, "property"),
                StringValue(control, "value"));
        }

        string[] paths = StringArray(control, "paths");
        string[] diagnosticIds = StringArray(control, "diagnosticIds");
        return CanonicalSourceSummary(
            sourceKind,
            IntValue(control, "entryCount"),
            paths,
            diagnosticIds);
    }

    private static string CanonicalMsBuild(string path, string property, IEnumerable<string> values)
        => $"msbuild|{Normalize(path)}|{property}|{string.Join(',', values.Where(static value => !string.IsNullOrWhiteSpace(value)).Order(StringComparer.OrdinalIgnoreCase))}";

    private static string CanonicalEditorConfig(string section, string property, string value)
        => $"editorconfig|{section}|{property}|{value}";

    private static string CanonicalSourceSummary(string root, IEnumerable<string> sourceFiles, string sourceKind)
    {
        List<string> paths = [];
        List<string> ids = [];
        int count = 0;
        Regex pragma = new(@"^\s*#pragma\s+warning\s+disable\s+(?<ids>[^/\r\n]+)", RegexOptions.Multiline | RegexOptions.CultureInvariant);
        Regex emitter = new(@"AppendLine\(""#pragma warning disable (?<ids>[^""/]+)", RegexOptions.CultureInvariant);

        foreach (string relativePath in sourceFiles)
        {
            string text = File.ReadAllText(Path.Combine(root, relativePath));
            if (sourceKind == "suppression-attribute")
            {
                foreach (AttributeSyntax attribute in CSharpSyntaxTree.ParseText(text).GetRoot().DescendantNodes().OfType<AttributeSyntax>())
                {
                    string attributeName = attribute.Name.ToString();
                    if (!attributeName.EndsWith("SuppressMessage", StringComparison.Ordinal)
                        && !attributeName.EndsWith("SuppressMessageAttribute", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    AttributeArgumentSyntax? checkIdArgument = attribute.ArgumentList?.Arguments
                        .FirstOrDefault(static argument => argument.NameEquals?.Name.Identifier.ValueText == "CheckId")
                        ?? attribute.ArgumentList?.Arguments.ElementAtOrDefault(1);
                    if (checkIdArgument?.Expression is not LiteralExpressionSyntax literal
                        || !literal.IsKind(SyntaxKind.StringLiteralExpression))
                    {
                        continue;
                    }

                    Match idMatch = Regex.Match(literal.Token.ValueText, @"^[A-Za-z]+\d+", RegexOptions.CultureInvariant);
                    if (!idMatch.Success)
                    {
                        continue;
                    }

                    paths.Add(Normalize(relativePath));
                    ids.Add(idMatch.Value);
                    count++;
                }

                continue;
            }

            Regex selected = sourceKind switch
            {
                "pragma" => pragma,
                "emitter-pragma" => emitter,
                _ => throw new InvalidOperationException($"Unknown source control kind {sourceKind}."),
            };
            MatchCollection matches = selected.Matches(text);
            foreach (Match match in matches)
            {
                string[] matchIds = SplitDiagnosticIds(match.Groups["ids"].Value);
                if (matchIds.Length == 0)
                {
                    continue;
                }

                paths.Add(Normalize(relativePath));
                ids.AddRange(matchIds);
                count += matchIds.Length;
            }
        }

        return CanonicalSourceSummary(sourceKind, count, paths, ids);
    }

    private static string CanonicalSourceSummary(
        string sourceKind,
        int count,
        IEnumerable<string> paths,
        IEnumerable<string> diagnosticIds)
        => $"{sourceKind}|{count}|{string.Join(',', diagnosticIds.Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase))}|{string.Join(',', paths.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal))}";

    private static string[] ValidateRepositoryPolicy(string root, JsonObject ledger)
    {
        List<string> errors = [];
        string editorConfig = File.ReadAllText(Path.Combine(root, ".editorconfig"));
        string testSection = EditorConfigSection(editorConfig, "[tests/**.cs]");
        string contractsSection = EditorConfigSection(
            editorConfig,
            "[src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs]");
        if (!testSection.Contains("dotnet_diagnostic.CA1707.severity = none", StringComparison.Ordinal)
            || !contractsSection.Contains("dotnet_diagnostic.CA1707.severity = none", StringComparison.Ordinal))
        {
            errors.Add("the two exact CA1707 scopes are missing");
        }

        string counterFixture = File.ReadAllText(Path.Combine(root, "samples/Counter/Counter.Domain/CounterProjection.cs"));
        string specimenFixture = File.ReadAllText(Path.Combine(root, "samples/Counter/Counter.Specimens.Domain/SpecimenFormattingProjection.cs"));
        if (Regex.Count(counterFixture, "Dictionary<string, string>", RegexOptions.CultureInvariant) != 1
            || Regex.Count(specimenFixture, "string\\[\\]|Dictionary<string, string>", RegexOptions.CultureInvariant) != 2)
        {
            errors.Add("the exact HFC1002 fixture count drifted from Metadata plus Approvers/OpaquePayload");
        }

        if (Regex.Count(counterFixture, "SuppressMessage\\(", RegexOptions.CultureInvariant) != 1
            || Regex.Count(specimenFixture, "SuppressMessage\\(", RegexOptions.CultureInvariant) != 2)
        {
            errors.Add("the HFC1002 fixture suppressions are not narrowed to exactly three properties");
        }

        if (EditorConfigSection(editorConfig, "[*.cs]").Contains("dotnet_diagnostic.CA1707", StringComparison.Ordinal))
        {
            errors.Add("CA1707 is disabled at repository scope");
        }

        string[] trackedTests = TrackedFiles(root)
            .Where(static path => path.StartsWith("tests/", StringComparison.Ordinal) && path.EndsWith(".cs", StringComparison.Ordinal))
            .ToArray();
        if (trackedTests.Length == 0 || trackedTests.Any(static path => !path.StartsWith("tests/", StringComparison.Ordinal)))
        {
            errors.Add("the test CA1707 scope is vacuous or escapes tests");
        }

        string diagnosticIdsPath = Path.Combine(root, "src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs");
        if (!File.Exists(diagnosticIdsPath))
        {
            errors.Add("the FcDiagnosticIds CA1707 scope is vacuous");
        }

        XDocument rootProps = XDocument.Load(Path.Combine(root, "Directory.Build.props"));
        XElement[] rootTreatWarningsAsErrors = rootProps
            .Descendants()
            .Where(static element => element.Name.LocalName == "TreatWarningsAsErrors")
            .ToArray();
        if (rootTreatWarningsAsErrors.Length != 1)
        {
            // Report rather than throw: 0 or 2+ declarations is itself a policy failure and must
            // surface as a named error alongside the other findings.
            errors.Add(
                "expected exactly one root TreatWarningsAsErrors declaration, found "
                + rootTreatWarningsAsErrors.Length.ToString(CultureInfo.InvariantCulture));
        }
        else if (rootTreatWarningsAsErrors[0].Value.Trim() != "true")
        {
            errors.Add("TreatWarningsAsErrors is not canonically true");
        }

        if (rootProps.Descendants().Any(static element => element.Name.LocalName.StartsWith("AnalysisMode", StringComparison.Ordinal)))
        {
            errors.Add("central AnalysisMode activation belongs to Story 11.23");
        }

        string[] trackedProjects = TrackedFiles(root).Where(IsMsBuildFile).ToArray();
        foreach (string relativePath in trackedProjects)
        {
            XDocument project = XDocument.Load(Path.Combine(root, relativePath));
            foreach (XElement reference in project.Descendants().Where(static element => element.Name.LocalName == "PackageReference"))
            {
                string package = reference.Attribute("Include")?.Value ?? string.Empty;
                if (package.StartsWith("SonarAnalyzer", StringComparison.OrdinalIgnoreCase)
                    || package.StartsWith("StyleCop", StringComparison.OrdinalIgnoreCase)
                    || package.StartsWith("Roslynator", StringComparison.OrdinalIgnoreCase)
                    || package.Equals("Microsoft.CodeAnalysis.NetAnalyzers", StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add($"third-party analyzer package {package} in {relativePath}");
                }
            }

            foreach (XElement reference in project.Descendants().Where(static element => element.Name.LocalName == "ProjectReference"))
            {
                string outputType = reference.Attribute("OutputItemType")?.Value ?? string.Empty;
                string include = reference.Attribute("Include")?.Value ?? string.Empty;
                if (outputType == "Analyzer" && !include.Contains("Hexalith.FrontComposer.SourceTools", StringComparison.Ordinal))
                {
                    errors.Add($"unapproved analyzer project reference {include} in {relativePath}");
                }
            }
        }

        int falseTreatWarningsAsErrors = trackedProjects
            .Select(path => XDocument.Load(Path.Combine(root, path)))
            .SelectMany(static document => document.Descendants())
            .Count(static element => element.Name.LocalName == "TreatWarningsAsErrors" && element.Value.Trim() == "false");
        if (falseTreatWarningsAsErrors != 1)
        {
            errors.Add($"expected one benchmark TreatWarningsAsErrors=false exception, found {falseTreatWarningsAsErrors}");
        }

        if (IntValue(RequiredObject(ledger, "implementationSnapshot"), "policyOutcomeNamingFindings") != 0)
        {
            errors.Add("the ledger does not require zero post-policy Naming findings");
        }

        return [.. errors];
    }

    /// <summary>
    /// The control-parity contract is a closed world over the root <c>.editorconfig</c> alone.
    /// EditorConfig and globalconfig files layer, so any additional analyzer-configuration file
    /// outside <c>references/**</c> could re-severity a rule without appearing in the ledger.
    /// Fail closed on discovery rather than silently reading only the root file.
    /// <c>references/Hexalith.Builds/Hexalith.globalconfig</c> stays excluded: it is external
    /// submodule configuration, not root FrontComposer analyzer policy.
    /// </summary>
    private static string[] ValidateConfigurationClosure(string root)
    {
        string[] configurationFiles = TrackedFiles(root)
            .Where(static path =>
                path.EndsWith(".editorconfig", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".globalconfig", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".ruleset", StringComparison.OrdinalIgnoreCase))
            .Order(StringComparer.Ordinal)
            .ToArray();
        List<string> errors = [];
        if (!configurationFiles.Contains(".editorconfig", StringComparer.Ordinal))
        {
            errors.Add("the root .editorconfig is missing");
        }

        errors.AddRange(configurationFiles
            .Where(static path => !string.Equals(path, ".editorconfig", StringComparison.Ordinal))
            .Select(static path =>
                "unledgered analyzer configuration file outside the root .editorconfig closed world: "
                + path));
        return [.. errors];
    }

    private static string[] ValidateIdentifierInventory(string root, JsonObject ledger)
    {
        JsonObject inventory = RequiredObject(ledger, "identifierInventory");
        string[] testFiles = TrackedFiles(root)
            .Where(static path => path.StartsWith("tests/", StringComparison.Ordinal) && path.EndsWith(".cs", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToArray();
        (int testCount, string testHash) = IdentifierInventory(root, testFiles);
        (int contractsCount, string contractsHash) = IdentifierInventory(
            root,
            ["src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs"]);
        List<string> errors = [];
        if (testCount != IntValue(inventory, "testUnderscoreIdentifierTokens")
            || !string.Equals(testHash, StringValue(inventory, "testInventorySha256"), StringComparison.Ordinal))
        {
            errors.Add($"test CA1707 scope identifier inventory drift: count={testCount}, sha256={testHash}");
        }

        if (contractsCount != IntValue(inventory, "contractsUnderscoreIdentifierTokens")
            || !string.Equals(contractsHash, StringValue(inventory, "contractsInventorySha256"), StringComparison.Ordinal))
        {
            errors.Add($"FcDiagnosticIds CA1707 scope identifier inventory drift: count={contractsCount}, sha256={contractsHash}");
        }

        return [.. errors];
    }

    private static (int Count, string Hash) IdentifierInventory(string root, IEnumerable<string> relativePaths)
    {
        List<string> inventory = [];
        foreach (string relativePath in relativePaths)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(root, relativePath)), path: relativePath);
            foreach (SyntaxToken token in tree.GetRoot().DescendantTokens().Where(static token =>
                token.IsKind(SyntaxKind.IdentifierToken) && token.ValueText.Contains('_', StringComparison.Ordinal)))
            {
                FileLinePositionSpan lineSpan = token.GetLocation().GetLineSpan();
                inventory.Add($"{Normalize(relativePath)}:{lineSpan.StartLinePosition.Line + 1}:{token.ValueText}");
            }
        }

        string material = string.Join('\n', inventory.Order(StringComparer.Ordinal));
        string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(material))).ToLowerInvariant();
        return (inventory.Count, hash);
    }

    private static async Task ValidateEffectiveBuildGraphsAsync(string root)
    {
        foreach (string targetFramework in new[] { "net10.0", "netstandard2.0" })
        {
            string output = await RunDotnetAsync(
                root,
                "msbuild",
                ContractsProject,
                "-p:Configuration=Release",
                $"-p:TargetFramework={targetFramework}",
                "-getProperty:NoWarn,TreatWarningsAsErrors,AnalysisMode,AnalysisModeNaming",
                "-nologo").ConfigureAwait(true);
            JsonObject properties = RequiredObject(RequiredObject(JsonNode.Parse(output), "evaluation"), "Properties");
            StringValue(properties, "TreatWarningsAsErrors").ShouldBe("true");
            StringValue(properties, "AnalysisMode").ShouldBeEmpty();
            SplitDiagnosticIds(StringValue(properties, "NoWarn")).ShouldBe(
                ["0419", "1570", "1572", "1573", "1574", "1734"],
                ignoreOrder: false);
        }

        string benchmarkOutput = await RunDotnetAsync(
            root,
            "msbuild",
            "tests/Hexalith.FrontComposer.Shell.Tests.Bench/Hexalith.FrontComposer.Shell.Tests.Bench.csproj",
            "-p:Configuration=Release",
            "-getProperty:TreatWarningsAsErrors,NoWarn",
            "-nologo").ConfigureAwait(true);
        JsonObject benchmarkProperties = RequiredObject(RequiredObject(JsonNode.Parse(benchmarkOutput), "evaluation"), "Properties");
        StringValue(benchmarkProperties, "TreatWarningsAsErrors").ShouldBe("false");
    }

    private static async Task ValidateCompileSpecimensAsync(string root)
    {
        string temporaryRoot = Path.Combine(Path.GetTempPath(), "fc-analyzer-policy-" + Guid.NewGuid().ToString("N"));
        string testDirectory = Path.Combine(temporaryRoot, "tests", "Synthetic");
        string contractsDirectory = Path.Combine(
            temporaryRoot,
            "src",
            "Hexalith.FrontComposer.Contracts",
            "Diagnostics");
        string productDirectory = Path.Combine(temporaryRoot, "src", "Product");
        try
        {
            _ = Directory.CreateDirectory(testDirectory);
            _ = Directory.CreateDirectory(contractsDirectory);
            _ = Directory.CreateDirectory(productDirectory);
            File.Copy(Path.Combine(root, ".editorconfig"), Path.Combine(temporaryRoot, ".editorconfig"));
            await File.WriteAllTextAsync(
                Path.Combine(temporaryRoot, "Directory.Build.props"),
                """
                <Project>
                  <PropertyGroup>
                    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
                    <AnalysisModeNaming>Recommended</AnalysisModeNaming>
                    <Nullable>enable</Nullable>
                  </PropertyGroup>
                </Project>
                """,
                TestContext.Current.CancellationToken).ConfigureAwait(true);
            await File.WriteAllTextAsync(
                Path.Combine(temporaryRoot, "Synthetic.csproj"),
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net10.0</TargetFramework>
                    <IsPackable>false</IsPackable>
                  </PropertyGroup>
                </Project>
                """,
                TestContext.Current.CancellationToken).ConfigureAwait(true);
            await File.WriteAllTextAsync(
                Path.Combine(testDirectory, "NamingPolicyTests.cs"),
                "namespace Synthetic.Tests; public sealed class NamingPolicyTests { public static void Subject_Scenario_Expectation() { } }",
                TestContext.Current.CancellationToken).ConfigureAwait(true);
            await File.WriteAllTextAsync(
                Path.Combine(contractsDirectory, "FcDiagnosticIds.cs"),
                "namespace Synthetic.Contracts; public static class FcDiagnosticIds { public const string HFC0001_Compatibility_Name = \"HFC0001\"; }",
                TestContext.Current.CancellationToken).ConfigureAwait(true);

            (int positiveExitCode, string positiveOutput) = await RunDotnetResultAsync(
                temporaryRoot,
                "build",
                "Synthetic.csproj",
                "-c",
                "Release",
                "-m:1",
                "/nr:false",
                "-p:NuGetAudit=false").ConfigureAwait(true);
            positiveExitCode.ShouldBe(0, positiveOutput);

            string ca1711Path = Path.Combine(testDirectory, "CollectionName.cs");
            await File.WriteAllTextAsync(
                ca1711Path,
                "namespace Synthetic.Tests; public sealed class SyntheticCollection { }",
                TestContext.Current.CancellationToken).ConfigureAwait(true);
            (int ca1711ExitCode, string ca1711Output) = await RunDotnetResultAsync(
                temporaryRoot,
                "build",
                "Synthetic.csproj",
                "-c",
                "Release",
                "--no-restore",
                "--no-incremental",
                "-m:1",
                "/nr:false").ConfigureAwait(true);
            ca1711ExitCode.ShouldNotBe(0);
            ca1711Output.ShouldContain("error CA1711");
            File.Delete(ca1711Path);

            await File.WriteAllTextAsync(
                Path.Combine(productDirectory, "ProductionApi.cs"),
                "namespace Synthetic.Product; public sealed class ProductionApi { public void Bad_Name() { } }",
                TestContext.Current.CancellationToken).ConfigureAwait(true);
            (int ca1707ExitCode, string ca1707Output) = await RunDotnetResultAsync(
                temporaryRoot,
                "build",
                "Synthetic.csproj",
                "-c",
                "Release",
                "--no-restore",
                "--no-incremental",
                "-m:1",
                "/nr:false").ConfigureAwait(true);
            ca1707ExitCode.ShouldNotBe(0);
            ca1707Output.ShouldContain("error CA1707");
        }
        finally
        {
            if (Directory.Exists(temporaryRoot))
            {
                Directory.Delete(temporaryRoot, recursive: true);
            }
        }
    }

    private static JsonObject LoadLedger(string root)
    {
        string path = Path.Combine(root, LedgerPath);
        File.Exists(path).ShouldBeTrue($"missing canonical analyzer-policy ledger {LedgerPath}");
        return RequiredObject(JsonNode.Parse(File.ReadAllText(path)), "ledger");
    }

    private static JsonObject Clone(JsonObject value)
        => RequiredObject(JsonNode.Parse(value.ToJsonString()), "clone");

    private static JsonObject RequiredObject(JsonNode? node, string name)
        => node as JsonObject ?? throw new InvalidDataException($"Expected object {name}.");

    private static JsonArray RequiredArray(JsonNode? node, string name)
        => RequiredObject(node, name)[name] as JsonArray
            ?? throw new InvalidDataException($"Expected array {name}.");

    private static JsonObject RequiredObject(JsonObject parent, string name)
        => parent[name] as JsonObject ?? throw new InvalidDataException($"Expected object {name}.");

    private static string StringValue(JsonObject value, string name)
        => value[name]?.GetValue<string>() ?? string.Empty;

    private static int IntValue(JsonObject value, string name)
        => value[name]?.GetValue<int>() ?? 0;

    private static string[] StringArray(JsonObject value, string name)
        => value[name] is JsonArray array
            ? array.Select(static item => item?.GetValue<string>() ?? string.Empty).ToArray()
            : [];

    private static void RequireValue(JsonObject value, string name, List<string> errors)
    {
        JsonNode? node = value[name];
        if (node is null
            || (node is JsonValue jsonValue
                && jsonValue.TryGetValue<string>(out string? text)
                && string.IsNullOrWhiteSpace(text)))
        {
            errors.Add($"missing or empty {name}");
        }
    }

    private static void ValidateSafePath(string path, string subject, List<string> errors)
    {
        // A wildcard is permitted only under tests/; every other scope must be an exact path.
        if (string.IsNullOrWhiteSpace(path)
            || Path.IsPathRooted(path)
            || path.Contains("..", StringComparison.Ordinal)
            || path.Contains('\\', StringComparison.Ordinal)
            || (path.Contains('*', StringComparison.Ordinal) && !path.StartsWith("tests/", StringComparison.Ordinal)))
        {
            errors.Add($"unsafe path for {subject}: {path}");
        }
    }

    private static string[] TrackedFiles(string root)
    {
        using Process process = new()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "git",
                WorkingDirectory = root,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            },
        };
        // Tracked files only. `--others` would make an untracked scratch .cs file under tests/
        // indistinguishable from genuine sealed-inventory drift.
        process.StartInfo.ArgumentList.Add("ls-files");
        process.StartInfo.ArgumentList.Add("-z");
        process.StartInfo.ArgumentList.Add("--cached");
        process.Start().ShouldBeTrue();

        // Drain both pipes concurrently and bound the wait: sequential ReadToEnd calls with both
        // streams redirected deadlock if git fills the stderr buffer.
        Task<string> standardOutput = process.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
        Task<string> standardError = process.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
        if (!process.WaitForExit(GitTimeoutMilliseconds))
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
                // The process exited between the timeout and the kill; nothing to terminate.
            }

            throw new InvalidOperationException(
                $"git ls-files did not complete within {GitTimeoutMilliseconds} ms.");
        }

        // The int overload can return before the redirected streams are flushed.
        process.WaitForExit();
        string output = standardOutput.GetAwaiter().GetResult();
        string error = standardError.GetAwaiter().GetResult();
        process.ExitCode.ShouldBe(0, error);
        return output.Split('\0', StringSplitOptions.RemoveEmptyEntries)
            .Select(Normalize)
            .Where(path => File.Exists(Path.Combine(root, path)))
            .Where(static path => !path.StartsWith("references/", StringComparison.Ordinal))
            .ToArray();
    }

    private static string[] SplitDiagnosticIds(string value)
        => value.Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(static item => !item.StartsWith("$(", StringComparison.Ordinal))
            .ToArray();

    private static bool IsWarningProperty(string name)
        => name is "NoWarn" or "WarningsAsErrors" or "WarningsNotAsErrors" or "TreatWarningsAsErrors"
            || name.StartsWith("AnalysisMode", StringComparison.Ordinal);

    private static bool IsMsBuildFile(string path)
        => path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".props", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".targets", StringComparison.OrdinalIgnoreCase);

    private static bool IsRootPolicyPath(string path)
        => path is "Directory.Build.props" or "Directory.Build.targets" or "src/Directory.Build.props";

    /// <summary>
    /// Returns the concatenation of every section whose header matches exactly. A first-match-only
    /// lookup would let a second, later section with the same header evade the repository-scope
    /// CA1707 check. Matching is line-anchored so a header cannot be found inside another header.
    /// </summary>
    private static string EditorConfigSection(string editorConfig, string header)
    {
        StringBuilder builder = new();
        bool inSection = false;
        foreach (string rawLine in editorConfig.Split('\n'))
        {
            string line = rawLine.Trim();
            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                inSection = string.Equals(line, header, StringComparison.Ordinal);
            }

            if (inSection)
            {
                _ = builder.Append(line).Append('\n');
            }
        }

        return builder.ToString();
    }

    private static string Normalize(string path)
        => path.Replace('\\', '/');

    private static string[] Story1122StrictBuildArguments(string project)
        =>
        [
            "build",
            project,
            "-c",
            "Release",
            "--no-restore",
            "--no-incremental",
            "-m:1",
            "/nr:false",
            "-p:NuGetAudit=false",
            "-p:MinVerVersionOverride=4.0.0",
            "-p:AnalysisMode=Recommended",
        ];

    private static string RepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Hexalith.FrontComposer.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }

    private static async Task<string> RunDotnetAsync(string workingDirectory, params string[] arguments)
    {
        (int exitCode, string output) = await RunDotnetResultAsync(workingDirectory, arguments).ConfigureAwait(true);
        exitCode.ShouldBe(0, output);
        return output;
    }

    private static async Task<(int ExitCode, string Output)> RunDotnetResultAsync(
        string workingDirectory,
        params string[] arguments)
    {
        using Process process = new()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            },
        };
        process.StartInfo.Environment["DOTNET_CLI_UI_LANGUAGE"] = "en-US";
        foreach (string argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(DotnetTimeoutMilliseconds);
        process.Start().ShouldBeTrue();
        Task<string> standardOutput = process.StandardOutput.ReadToEndAsync(timeout.Token);
        Task<string> standardError = process.StandardError.ReadToEndAsync(timeout.Token);
        try
        {
            await process.WaitForExitAsync(timeout.Token).ConfigureAwait(true);
        }
        catch (OperationCanceledException) when (!TestContext.Current.CancellationToken.IsCancellationRequested)
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException(
                $"dotnet {string.Join(' ', arguments)} exceeded the {DotnetTimeoutMilliseconds / 1000}-second governance bound.");
        }

        return (
            process.ExitCode,
            await standardOutput.ConfigureAwait(true) + await standardError.ConfigureAwait(true));
    }
}
