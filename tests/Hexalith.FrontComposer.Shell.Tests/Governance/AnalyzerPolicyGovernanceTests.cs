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
            if (line.StartsWith("[", StringComparison.Ordinal) && line.EndsWith("]", StringComparison.Ordinal))
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
        if (Regex.Matches(counterFixture, "Dictionary<string, string>", RegexOptions.CultureInvariant).Count != 1
            || Regex.Matches(specimenFixture, "string\\[\\]|Dictionary<string, string>", RegexOptions.CultureInvariant).Count != 2)
        {
            errors.Add("the exact HFC1002 fixture count drifted from Metadata plus Approvers/OpaquePayload");
        }

        if (Regex.Matches(counterFixture, "SuppressMessage\\(", RegexOptions.CultureInvariant).Count != 1
            || Regex.Matches(specimenFixture, "SuppressMessage\\(", RegexOptions.CultureInvariant).Count != 2)
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

    private static void RequireValue(JsonObject value, string name, ICollection<string> errors)
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

    private static void ValidateSafePath(string path, string subject, ICollection<string> errors)
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
            if (line.StartsWith("[", StringComparison.Ordinal) && line.EndsWith("]", StringComparison.Ordinal))
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
        foreach (string argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        process.Start().ShouldBeTrue();
        Task<string> standardOutput = process.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
        Task<string> standardError = process.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
        await process.WaitForExitAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        return (
            process.ExitCode,
            await standardOutput.ConfigureAwait(true) + await standardError.ConfigureAwait(true));
    }
}
