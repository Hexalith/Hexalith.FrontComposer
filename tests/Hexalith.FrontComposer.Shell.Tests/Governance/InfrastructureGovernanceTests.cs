using System.Diagnostics;
using System.IO.Enumeration;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Shell.Infrastructure.EventStore;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Governance;

[Trait("Category", "Governance")]
public sealed class InfrastructureGovernanceTests {
    private static readonly string[] FrameworkProjectRelativePaths = [
        "src/Hexalith.FrontComposer.Contracts/Hexalith.FrontComposer.Contracts.csproj",
        "src/Hexalith.FrontComposer.SourceTools/Hexalith.FrontComposer.SourceTools.csproj",
        "src/Hexalith.FrontComposer.Shell/Hexalith.FrontComposer.Shell.csproj",
    ];

    [Fact]
    public void FrameworkProjects_DoNotDeclareForbiddenInfrastructurePackages() {
        string root = RepositoryRoot();
        List<GovernanceViolation> violations = [];
        foreach (string project in FrameworkProjectRelativePaths) {
            violations.AddRange(InfrastructureGovernance.ScanProjectReferences(Path.Combine(root, project), root));
        }

        violations.ShouldBeEmpty(FormatViolations(violations));
    }

    [Fact]
    public void CentralPackageVersions_WhenCatalogIsMigrated_AreOwnedBySharedCatalog() {
        string root = RepositoryRoot();
        XDocument importShim = XDocument.Load(Path.Combine(root, "Directory.Packages.props"));
        XElement[] rootPackageVersions = importShim
            .Descendants()
            .Where(static element => element.Name.LocalName == "PackageVersion")
            .ToArray();
        rootPackageVersions.ShouldBeEmpty(
            "the FrontComposer root file is an import shim; package versions belong in Hexalith.Builds");

        string buildsRoot = Path.Combine(root, "references", "Hexalith.Builds");
        const string sharedCatalogRelativePath = "Props/Directory.Packages.props";
        ReadGitAttribute(buildsRoot, sharedCatalogRelativePath, "text").ShouldBe("set");
        ReadGitAttribute(buildsRoot, sharedCatalogRelativePath, "eol").ShouldBe("crlf");
        ReadGitAttribute(buildsRoot, "Directory.Build.props", "eol").ShouldBe(
            "unspecified",
            "the catalog-only checkout policy must not broaden to unrelated Builds files");

        string sharedCatalogPath = Path.Combine(buildsRoot, "Props", "Directory.Packages.props");
        AssertUtf8BomAndCrLf(sharedCatalogPath);
        XDocument sharedCatalog = XDocument.Load(sharedCatalogPath);
        sharedCatalog
            .Descendants()
            .Single(static element => element.Name.LocalName == "HexalithTenantsVersion")
            .Value
            .ShouldBe("3.2.18", "the accepted Builds catalog must select the published Tenants release");
        Dictionary<string, string> expectedVersions = new(StringComparer.OrdinalIgnoreCase) {
            ["BenchmarkDotNet"] = "0.15.8",
            ["FsCheck.Xunit.v3"] = "3.3.3",
            ["Microsoft.CodeAnalysis.Workspaces.Common"] = "5.6.0",
            ["Microsoft.Extensions.Localization"] = "10.0.9",
            ["Microsoft.Extensions.TimeProvider.Testing"] = "10.8.0",
            ["ModelContextProtocol.AspNetCore"] = "1.4.1",
            ["NUlid"] = "1.7.3",
            ["PactNet"] = "5.0.1",
            ["System.Collections.Immutable"] = "10.0.10",
            ["System.ComponentModel.Annotations"] = "5.0.0",
            ["System.Reactive"] = "7.0.0",
            ["System.Threading.Tasks.Extensions"] = "4.6.3",
            ["Verify"] = "31.24.2",
            ["Verify.XunitV3"] = "31.24.2",
        };

        foreach ((string packageId, string expectedVersion) in expectedVersions) {
            AssertAuthoritativePackageVersion(sharedCatalog, packageId, expectedVersion);
        }

        List<GovernanceViolation> migratedProviderViolations = InfrastructureGovernance.ScanCentralPackageVersions(
            sharedCatalogPath,
            root,
            expectedVersions.Keys);
        migratedProviderViolations.ShouldBeEmpty(FormatViolations(migratedProviderViolations));

        XDocument eventStoreCatalog = XDocument.Load(
            Path.Combine(root, "references", "Hexalith.EventStore", "Directory.Packages.props"));
        Dictionary<string, string> eventStoreInheritedVersions = new(StringComparer.OrdinalIgnoreCase) {
            ["Microsoft.Extensions.TimeProvider.Testing"] = "10.8.0",
            ["System.CommandLine"] = "2.0.10",
            ["ModelContextProtocol"] = "1.4.1",
        };
        foreach ((string packageId, string expectedVersion) in eventStoreInheritedVersions) {
            AssertAuthoritativePackageVersion(sharedCatalog, packageId, expectedVersion);
            FindPackageVersionOperations(eventStoreCatalog, packageId).ShouldBeEmpty(
                $"EventStore must inherit {packageId} {expectedVersion} from the shared catalog");
        }

        XDocument partiesBuild = XDocument.Load(
            Path.Combine(root, "references", "Hexalith.Parties", "Directory.Build.props"));
        partiesBuild
            .Descendants()
            .Where(static element => element.Name.LocalName == "PackageReference")
            .Where(element => ItemSpecSelectsPackage((string?)element.Attribute("Include"), "MinVer"))
            .ShouldBeEmpty("Parties release versioning is owned by semantic-release, not MinVer");
        partiesBuild
            .Descendants()
            .Where(static element => element.Name.LocalName.StartsWith("MinVer", StringComparison.Ordinal))
            .ShouldBeEmpty("Parties must not retain MinVer configuration after semantic-release ownership");

        XDocument memoriesCatalog = XDocument.Load(
            Path.Combine(root, "references", "Hexalith.Memories", "Directory.Packages.props"));
        FindPackageVersionOperations(memoriesCatalog, "ModelContextProtocol.AspNetCore").ShouldBeEmpty(
            "Memories must inherit ModelContextProtocol.AspNetCore 1.4.1 from the shared catalog");
        FindPackageVersionOperations(memoriesCatalog, "Microsoft.Extensions.TimeProvider.Testing").ShouldBeEmpty(
            "Memories must inherit Microsoft.Extensions.TimeProvider.Testing 10.8.0 from the shared catalog");

        // EventStore and Memories carry Builds commits that contain the centralized catalog.
        // Keep each expected commit aligned with the corresponding tracked submodule gitlink.
        const string eventStoreBuildsCommit = "f8981e8ec4a5dec9d574da139c2e00dd714f2a60";
        const string memoriesBuildsCommit = "cb8b2d412a937e09380387601c2682e080b66220";
        ReadGitlinkCommit(Path.Combine(root, "references", "Hexalith.EventStore"), "references/Hexalith.Builds")
            .ShouldBe(eventStoreBuildsCommit, "EventStore standalone restores need the migrated shared pins");
        ReadGitlinkCommit(Path.Combine(root, "references", "Hexalith.Memories"), "references/Hexalith.Builds")
            .ShouldBe(memoriesBuildsCommit, "Memories standalone restores need the migrated shared pins");
    }

    [Fact]
    public void PartiesPackageVersions_WhenCatalogIsCentralized_AreInheritedFromPinnedBuilds() {
        string root = RepositoryRoot();
        string partiesRoot = Path.Combine(root, "references", "Hexalith.Parties");
        XDocument sharedCatalog = XDocument.Load(
            Path.Combine(root, "references", "Hexalith.Builds", "Props", "Directory.Packages.props"));
        XDocument partiesCatalog = XDocument.Load(
            Path.Combine(partiesRoot, "Directory.Packages.props"));

        XElement[] imports = partiesCatalog
            .Descendants()
            .Where(static element => element.Name.LocalName == "Import")
            .ToArray();
        string[] expectedImportProjects = [
            "$(Hexalith1BuildPackageProps)",
            "$(Hexalith2BuildPackageProps)",
            "$(Hexalith3BuildPackageProps)",
        ];
        string[] expectedImportConditions = [
            "Exists('$(Hexalith1BuildPackageProps)') And '$(HexalithVersionsLoaded)' != 'true'",
            "Exists('$(Hexalith2BuildPackageProps)') And '$(HexalithVersionsLoaded)' != 'true'",
            "'$(HexalithVersionsLoaded)' != 'true'",
        ];
        imports.Length.ShouldBe(3, "Parties must preserve its three guarded shared-catalog import paths");
        for (int index = 0; index < imports.Length; index++) {
            ((string?)imports[index].Attribute("Project")).ShouldBe(expectedImportProjects[index]);
            ((string?)imports[index].Attribute("Condition")).ShouldBe(expectedImportConditions[index]);
        }

        Dictionary<string, string> expectedBuildProperties = new(StringComparer.Ordinal) {
            ["ManagePackageVersionsCentrally"] = "true",
            ["CentralPackageTransitivePinningEnabled"] = "true",
            ["Hexalith1BuildPackageProps"] = "$(MSBuildThisFileDirectory)references/Hexalith.Builds/Props/Directory.Packages.props",
            ["Hexalith2BuildPackageProps"] = "$(MSBuildThisFileDirectory)../Hexalith.Builds/Props/Directory.Packages.props",
            ["Hexalith3BuildPackageProps"] = "$(MSBuildThisFileDirectory)../../Hexalith.Builds/Props/Directory.Packages.props",
        };
        foreach ((string propertyName, string expectedValue) in expectedBuildProperties) {
            partiesCatalog
                .Descendants()
                .Single(element => element.Name.LocalName == propertyName)
                .Value
                .ShouldBe(expectedValue);
        }

        Dictionary<string, string> expectedVersions = new(StringComparer.OrdinalIgnoreCase) {
            ["Microsoft.AspNetCore.Components.CustomElements"] = "10.0.10",
            ["ModelContextProtocol"] = "1.4.1",
            ["ModelContextProtocol.AspNetCore"] = "1.4.1",
        };
        foreach ((string packageId, string expectedVersion) in expectedVersions) {
            AssertAuthoritativePackageVersion(sharedCatalog, packageId, expectedVersion);
        }

        foreach (string relativePath in ReadTrackedFiles(partiesRoot, "*.csproj", "*.props", "*.targets")) {
            XDocument consumerDocument = XDocument.Load(Path.Combine(partiesRoot, relativePath));
            consumerDocument
                .Descendants()
                .Where(static element => element.Name.LocalName == "PackageVersion" && element.Parent?.Name.LocalName == "ItemGroup")
                .ShouldBeEmpty($"{relativePath} must inherit every package version from the pinned Builds catalog");
            foreach (XElement packageReference in consumerDocument
                .Descendants()
                .Where(static element => element.Name.LocalName is "PackageReference" or "GlobalPackageReference")) {
                packageReference.Attribute("Version").ShouldBeNull($"{relativePath} must not declare an inline Version");
                packageReference.Attribute("VersionOverride").ShouldBeNull($"{relativePath} must not declare VersionOverride");
                packageReference
                    .Elements()
                    .Where(static element => element.Name.LocalName is "Version" or "VersionOverride")
                    .ShouldBeEmpty($"{relativePath} must not declare inline package-version metadata");
            }
        }

        const string rootBuildsCommit = "cb8b2d412a937e09380387601c2682e080b66220";
        const string partiesBuildsCommit = "c177c66af5d3f509328c2f568dc0737fe9f89e4e";
        ReadGitlinkCommit(root, "references/Hexalith.Builds")
            .ShouldBe(rootBuildsCommit, "the inspected catalog must match the repaired root Builds commit");
        ReadGitlinkCommit(Path.Combine(root, "references", "Hexalith.Parties"), "references/Hexalith.Builds")
            .ShouldBe(partiesBuildsCommit, "Parties standalone restores need the centralized shared pins");
    }

    [Fact]
    public void RestoredAssets_DoNotContainForbiddenTransitiveProviderPackages() {
        string root = RepositoryRoot();
        List<GovernanceViolation> violations = [];
        int scanned = 0;
        foreach (string project in FrameworkProjectRelativePaths) {
            (int scannedDelta, List<GovernanceViolation> projectViolations) = InfrastructureGovernance.ScanRestoredAssetsTracked(Path.Combine(root, project), root);
            scanned += scannedDelta;
            violations.AddRange(projectViolations);
        }

        // F10 — fail-fast when no project.assets.json could be located. CI restores before
        // governance runs; locally `dotnet test` without `dotnet restore` would otherwise
        // produce a silent false-green for the transitive deny-list. Surface the gap loudly.
        scanned.ShouldBeGreaterThan(
            0,
            "no project.assets.json files were located under any framework project; run `dotnet restore` before executing the governance lane locally");
        violations.ShouldBeEmpty(FormatViolations(violations));
    }

    [Fact]
    public void FrameworkAssemblies_DoNotReferenceProviderAssemblies() {
        string root = RepositoryRoot();
        Assembly contracts = typeof(ICommandService).Assembly;
        Assembly shell = typeof(EventStoreCommandClient).Assembly;

        List<GovernanceViolation> violations = [
            .. InfrastructureGovernance.ScanAssemblyReferences(contracts, "Hexalith.FrontComposer.Contracts", allowSignalRClient: false, root),
            .. InfrastructureGovernance.ScanAssemblyReferences(shell, "Hexalith.FrontComposer.Shell", allowSignalRClient: true, root),
        ];

        violations.ShouldBeEmpty(FormatViolations(violations));
    }

    [Fact]
    public void FrameworkSourceAndGeneratedBaselines_DoNotUseProviderNamespaces() {
        string root = RepositoryRoot();

        List<GovernanceViolation> violations = [
            .. InfrastructureGovernance.ScanSourceTree(Path.Combine(root, "src/Hexalith.FrontComposer.Contracts"), root, allowSignalRClient: false),
            .. InfrastructureGovernance.ScanSourceTree(Path.Combine(root, "src/Hexalith.FrontComposer.SourceTools"), root, allowSignalRClient: false),
            .. InfrastructureGovernance.ScanSourceTree(Path.Combine(root, "src/Hexalith.FrontComposer.Shell"), root, allowSignalRClient: true),
            .. InfrastructureGovernance.ScanGeneratedBaselines(Path.Combine(root, "tests/Hexalith.FrontComposer.SourceTools.Tests"), root),
        ];

        violations.ShouldBeEmpty(FormatViolations(violations));
    }

    [Fact]
    public void SensitiveTelemetryPaths_DoNotUseInterpolatedTemplatesOrRawExceptions() {
        string root = RepositoryRoot();
        string[] paths = [
            "src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore",
            "src/Hexalith.FrontComposer.Shell/State/ProjectionConnection",
            "src/Hexalith.FrontComposer.Shell/Services/Lifecycle",
            "src/Hexalith.FrontComposer.Shell/State/PendingCommands",
        ];

        // F21 — cover BeginScope, string-concat templates, LoggerExtensions static-call form,
        // and the existing interpolated/raw-ex/ex.Message patterns. Reading a file once and
        // running multiple regex passes keeps the scanner deterministic and easy to extend.
        List<string> violations = [];
        foreach (string path in paths) {
            EnumerationOptions options = new() {
                RecurseSubdirectories = true,
                AttributesToSkip = FileAttributes.ReparsePoint | FileAttributes.Hidden,
            };
            foreach (string file in Directory.EnumerateFiles(Path.Combine(root, path), "*.cs", options)) {
                string text = File.ReadAllText(file);
                string relative = Path.GetRelativePath(root, file).Replace('\\', '/');
                if (Regex.IsMatch(text, @"\.Log(?:Trace|Debug|Information|Warning|Error|Critical)\s*\(\s*\$""", RegexOptions.Multiline)) {
                    violations.Add(relative + ": interpolated ILogger message template");
                }

                if (Regex.IsMatch(text, @"\.Log(?:Trace|Debug|Information|Warning|Error|Critical)\s*\(\s*ex\s*,", RegexOptions.Multiline)) {
                    violations.Add(relative + ": raw exception object passed to ILogger in sensitive path");
                }

                if (Regex.IsMatch(text, @"LoggerExtensions\.Log\w*\s*\(\s*[A-Za-z_][A-Za-z0-9_]*\s*,\s*\$""", RegexOptions.Multiline)) {
                    violations.Add(relative + ": interpolated template passed via LoggerExtensions static call");
                }

                // Detect string concat with a non-literal: `"foo" + variable`. Compiler-folded
                // line-continuation concats `"foo" + "bar"` are functionally equivalent to a
                // single literal at runtime and are allowed (the right-hand side starts with a
                // quote, so the negated character class excludes them).
                if (Regex.IsMatch(text, @"\.Log(?:Trace|Debug|Information|Warning|Error|Critical)\s*\([^)]*""[^""]*""\s*\+\s*[^\s""]", RegexOptions.Singleline)) {
                    violations.Add(relative + ": string-concatenated message template with non-literal (use templated parameters)");
                }

                if (Regex.IsMatch(text, @"\.BeginScope\s*\(\s*\$""", RegexOptions.Multiline)) {
                    violations.Add(relative + ": interpolated string passed to BeginScope (raw payload risk)");
                }

                if (text.Contains("ex.Message", StringComparison.Ordinal)) {
                    violations.Add(relative + ": raw exception message in sensitive path");
                }
            }
        }

        violations.ShouldBeEmpty(string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void SyntheticForbiddenPackage_WithPrivateAssets_FailsWithRemediation() {
        using var temp = TempRepo.Create();
        string project = temp.Write(
            "src/Hexalith.FrontComposer.Shell/Forbidden.csproj",
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="Dapr.Client" Version="1.0.0" PrivateAssets="all" />
              </ItemGroup>
            </Project>
            """);

        List<GovernanceViolation> violations = InfrastructureGovernance.ScanProjectReferences(project, temp.Root);

        violations.Single().Message.ShouldContain("Dapr");
        violations.Single().Message.ShouldContain("route through EventStore contract/client or deployment/AppHost component configuration");
    }

    [Fact]
    public void SyntheticCentralPackageVersion_FailsEvenWithoutProjectReference() {
        using var temp = TempRepo.Create();
        string props = temp.Write(
            "Directory.Packages.props",
            """
            <Project>
              <ItemGroup>
                <PackageVersion Include="dapr.client" Version="1.0.0" />
                <PackageVersion Update="stackexchange.redis" Version="1.0.0" />
              </ItemGroup>
            </Project>
            """);

        List<GovernanceViolation> violations = InfrastructureGovernance.ScanCentralPackageVersions(
            props,
            temp.Root,
            ["DAPR.CLIENT", "STACKEXCHANGE.REDIS"]);

        violations.Count.ShouldBe(2);
        violations.ShouldContain(static violation => violation.ProviderFamily == "Dapr SDK");
        violations.ShouldContain(static violation => violation.ProviderFamily == "Redis");
    }

    [Fact]
    public void CentralPackageVersionOwnership_InvalidOperations_AreRejected() {
        string[] invalidAuthoritativeCatalogs = [
            """
            <Project><ItemGroup><PackageVersion Update="BenchmarkDotNet" Version="0.15.8" /></ItemGroup></Project>
            """,
            """
            <Project><ItemGroup Condition="'$(Configuration)' == 'Never'"><PackageVersion Include="BenchmarkDotNet" Version="0.15.8" /></ItemGroup></Project>
            """,
            """
            <Project><Choose><Otherwise><ItemGroup><PackageVersion Include="BenchmarkDotNet" Version="0.15.8" /></ItemGroup></Otherwise></Choose></Project>
            """,
            """
            <Project><ItemGroup><PackageVersion Include="BenchmarkDotNet" Exclude="BenchmarkDotNet" Version="0.15.8" /></ItemGroup></Project>
            """,
            """
            <Project><ItemGroup><PackageVersion Include="BenchmarkDotNet" Version="0.15.8" /><PackageVersion Remove="Verify;BenchmarkDotNet" /></ItemGroup></Project>
            """,
            """
            <Project><ItemGroup><PackageVersion Include="BenchmarkDotNet" Version="0.15.8" /><PackageVersion Remove="Benchmark*" /></ItemGroup></Project>
            """,
        ];
        foreach (string catalog in invalidAuthoritativeCatalogs) {
            XDocument document = XDocument.Parse(catalog);
            Should.Throw<ShouldAssertException>(
                () => AssertAuthoritativePackageVersion(document, "BenchmarkDotNet", "0.15.8"));
        }

        string[] invalidOverrides = [
            """
            <Project><ItemGroup Condition="'$(Configuration)' == 'Debug'"><PackageVersion Update="ModelContextProtocol.AspNetCore" Version="1.4.0" /></ItemGroup></Project>
            """,
            """
            <Project><ItemGroup><PackageVersion Update="ModelContextProtocol.AspNetCore" Exclude="ModelContextProtocol.AspNetCore" Version="1.4.0" /></ItemGroup></Project>
            """,
            """
            <Project><ItemGroup><PackageVersion Update="ModelContextProtocol.AspNetCore" Version="1.4.0" /><PackageVersion Remove="ModelContextProtocol*" /></ItemGroup></Project>
            """,
        ];
        foreach (string catalog in invalidOverrides) {
            XDocument document = XDocument.Parse(catalog);
            Should.Throw<ShouldAssertException>(() => AssertPackageOverride(
                document,
                "ModelContextProtocol.AspNetCore",
                "1.4.0",
                "the override must be effective unconditionally"));
        }
    }

    [Fact]
    public void SyntheticSourceNamespace_FailsButDocsAndBinAreExcluded() {
        using var temp = TempRepo.Create();
        _ = temp.Write("src/Hexalith.FrontComposer.Shell/Bad.cs", "using Npgsql;\nnamespace Bad;");
        _ = temp.Write("docs/example.md", "using Npgsql;");
        _ = temp.Write("src/Hexalith.FrontComposer.Shell/bin/Debug/Generated.cs", "using Dapr.Client;");

        List<GovernanceViolation> violations = InfrastructureGovernance.ScanSourceTree(
            Path.Combine(temp.Root, "src/Hexalith.FrontComposer.Shell"),
            temp.Root,
            allowSignalRClient: true);

        violations.Single().ProviderFamily.ShouldBe("PostgreSQL");
    }

    [Fact]
    public void ShellSignalRClient_IsExactAllowlistOnly() {
        using var temp = TempRepo.Create();
        string project = temp.Write(
            "src/Hexalith.FrontComposer.Shell/Hexalith.FrontComposer.Shell.csproj",
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="Microsoft.AspNetCore.SignalR.Client" />
                <PackageReference Include="Microsoft.AspNetCore.SignalR.StackExchangeRedis" />
              </ItemGroup>
            </Project>
            """);

        List<GovernanceViolation> violations = InfrastructureGovernance.ScanProjectReferences(project, temp.Root);

        violations.Single().ProviderFamily.ShouldBe("Redis");
    }

    [Fact]
    public void NonShellProject_RejectsSignalRClient() {
        // F01 — Microsoft.AspNetCore.SignalR.Client is allowed only in Shell.
        // Contracts/SourceTools must fail governance if they reference it.
        using var temp = TempRepo.Create();
        string project = temp.Write(
            "src/Hexalith.FrontComposer.Contracts/Hexalith.FrontComposer.Contracts.csproj",
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="Microsoft.AspNetCore.SignalR.Client" />
              </ItemGroup>
            </Project>
            """);

        List<GovernanceViolation> violations = InfrastructureGovernance.ScanProjectReferences(project, temp.Root);

        violations.Single().Reference.ShouldBe("Microsoft.AspNetCore.SignalR.Client");
        violations.Single().ProviderFamily.ShouldBe("SignalR Client (Shell-only)");
    }

    [Fact]
    public void ProjectReferences_AreNotScannedAgainstPackageDenyList() {
        // F22 — ProjectReference Include is a relative path to a peer .csproj. The package
        // deny-list must apply only to NuGet PackageReference elements; otherwise a benign
        // peer project named with a forbidden prefix (e.g. ..\Dapr.Foo\Dapr.Foo.csproj) would
        // false-fail governance.
        using var temp = TempRepo.Create();
        string project = temp.Write(
            "src/Hexalith.FrontComposer.Shell/Hexalith.FrontComposer.Shell.csproj",
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <ProjectReference Include="..\..\..\NeighbouringRepo\Dapr.Foo\Dapr.Foo.csproj" />
              </ItemGroup>
            </Project>
            """);

        List<GovernanceViolation> violations = InfrastructureGovernance.ScanProjectReferences(project, temp.Root);

        violations.ShouldBeEmpty();
    }

    [Fact]
    public void SyntheticTransitivePackage_FailsViaProjectAssetsJson() {
        // F35 — transitive provider packages surfaced only in `project.assets.json` must be
        // caught even when the .csproj does not declare them directly (e.g., PrivateAssets=all
        // on a wrapping framework or central package management transitive flow).
        using var temp = TempRepo.Create();
        string project = temp.Write(
            "src/Hexalith.FrontComposer.Shell/Hexalith.FrontComposer.Shell.csproj",
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="Hexalith.Some.Internal.Package" Version="1.0.0" />
              </ItemGroup>
            </Project>
            """);
        _ = temp.Write(
            "src/Hexalith.FrontComposer.Shell/obj/project.assets.json",
            """
            {
              "version": 3,
              "libraries": {
                "Hexalith.Some.Internal.Package/1.0.0": { "type": "package" },
                "Confluent.Kafka/2.5.0": { "type": "package" }
              }
            }
            """);

        (_, List<GovernanceViolation> violations) = InfrastructureGovernance.ScanRestoredAssetsTracked(project, temp.Root);

        violations.Single().Reference.ShouldBe("Confluent.Kafka");
        violations.Single().ProviderFamily.ShouldBe("Kafka");
    }

    [Theory]
    [InlineData("Confluent.Kafka", "Kafka")]
    [InlineData("Microsoft.Azure.Cosmos", "Cosmos DB")]
    [InlineData("Azure.Storage.Blobs", "Azure Storage")]
    [InlineData("Azure.Messaging.ServiceBus", "Azure Service Bus")]
    [InlineData("Amazon.S3", "AWS provider SDK")]
    [InlineData("Google.Cloud.Storage.V1", "GCP provider SDK")]
    public void DenyList_CoversFullProviderFamilyPanel(string packageId, string expectedFamily) {
        // F41 — exhaust the deny-list panel beyond Dapr/Redis to prove every story-named
        // provider family fails governance via a synthetic project reference.
        using var temp = TempRepo.Create();
        string project = temp.Write(
            "src/Hexalith.FrontComposer.Contracts/Hexalith.FrontComposer.Contracts.csproj",
            $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="{packageId}" Version="1.0.0" />
              </ItemGroup>
            </Project>
            """);

        List<GovernanceViolation> violations = InfrastructureGovernance.ScanProjectReferences(project, temp.Root);

        violations.Single().ProviderFamily.ShouldBe(expectedFamily);
    }

    private static string RepositoryRoot() {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null) {
            if (File.Exists(Path.Combine(dir.FullName, "Hexalith.FrontComposer.slnx"))) {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }

    private static XElement[] FindPackageVersionOperations(XDocument document, string packageId)
        => document
            .Descendants()
            .Where(static element => element.Name.LocalName == "PackageVersion")
            .Where(element => new[] { "Include", "Update", "Remove" }
                .Any(operation => ItemSpecSelectsPackage(
                    (string?)element.Attribute(operation),
                    packageId)))
            .ToArray();

    private static bool ItemSpecSelectsPackage(string? itemSpec, string packageId)
        => !string.IsNullOrWhiteSpace(itemSpec)
            && itemSpec
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Any(spec => FileSystemName.MatchesSimpleExpression(spec, packageId, ignoreCase: true));

    private static void AssertAuthoritativePackageVersion(
        XDocument document,
        string packageId,
        string expectedVersion) {
        XElement[] operations = FindPackageVersionOperations(document, packageId);
        operations.Length.ShouldBe(1, $"{packageId} must have exactly one unmasked shared operation");
        XElement declaration = operations[0];
        string.Equals(
            (string?)declaration.Attribute("Include"),
            packageId,
            StringComparison.OrdinalIgnoreCase).ShouldBeTrue(
                $"{packageId} must be an authoritative Include item, not an Update");
        declaration.Attribute("Update").ShouldBeNull($"{packageId} must not use Update in the shared catalog");
        declaration.Attribute("Exclude").ShouldBeNull($"{packageId} must not use Exclude in the shared catalog");
        declaration.AncestorsAndSelf()
            .Any(static element => element.Attribute("Condition") is not null)
            .ShouldBeFalse($"{packageId} must be unconditional in the shared catalog");
        declaration.Ancestors()
            .Any(static element => element.Name.LocalName is "Choose" or "When" or "Otherwise")
            .ShouldBeFalse($"{packageId} must not be selected through an MSBuild Choose branch");
        ((string?)declaration.Attribute("Version")).ShouldBe(expectedVersion);
    }

    private static void AssertPackageOverride(
        XDocument document,
        string packageId,
        string expectedVersion,
        string message) {
        XElement[] declarations = FindPackageVersionOperations(document, packageId);
        declarations.Length.ShouldBe(1, message);
        string.Equals(
            (string?)declarations[0].Attribute("Update"),
            packageId,
            StringComparison.OrdinalIgnoreCase).ShouldBeTrue(message);
        declarations[0].Attribute("Exclude").ShouldBeNull(message);
        declarations[0].AncestorsAndSelf()
            .Any(static element => element.Attribute("Condition") is not null)
            .ShouldBeFalse(message);
        declarations[0].Ancestors()
            .Any(static element => element.Name.LocalName is "Choose" or "When" or "Otherwise")
            .ShouldBeFalse(message);
        ((string?)declarations[0].Attribute("Version")).ShouldBe(expectedVersion, message);
    }

    private static void AssertUtf8BomAndCrLf(string path) {
        byte[] bytes = File.ReadAllBytes(path);
        byte[] preamble = Encoding.UTF8.GetPreamble();
        bytes.Length.ShouldBeGreaterThanOrEqualTo(preamble.Length);
        bytes.Take(preamble.Length).ShouldBe(preamble, "the shared catalog must start with a UTF-8 BOM");
        _ = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true)
            .GetString(bytes, preamble.Length, bytes.Length - preamble.Length);

        for (int index = preamble.Length; index < bytes.Length; index++) {
            if (bytes[index] == (byte)'\n') {
                (index > preamble.Length && bytes[index - 1] == (byte)'\r').ShouldBeTrue(
                    $"the shared catalog contains a bare LF at byte offset {index}");
            }

            if (bytes[index] == (byte)'\r') {
                (index + 1 < bytes.Length && bytes[index + 1] == (byte)'\n').ShouldBeTrue(
                    $"the shared catalog contains a bare CR at byte offset {index}");
            }
        }
    }

    private static string ReadGitlinkCommit(string repositoryPath, string gitlinkPath) {
        ProcessStartInfo startInfo = new("git") {
            WorkingDirectory = repositoryPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add("ls-files");
        startInfo.ArgumentList.Add("--stage");
        startInfo.ArgumentList.Add("--");
        startInfo.ArgumentList.Add(gitlinkPath);

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Could not start git in {repositoryPath}.");
        string standardOutput = process.StandardOutput.ReadToEnd();
        string standardError = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0) {
            throw new InvalidOperationException(
                $"git ls-files failed in {repositoryPath} with exit code {process.ExitCode}: {standardError}");
        }

        string[] fields = standardOutput.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (fields.Length < 4 || !string.Equals(fields[0], "160000", StringComparison.Ordinal)) {
            throw new InvalidOperationException(
                $"{gitlinkPath} is not a tracked gitlink in {repositoryPath}: {standardOutput}");
        }

        return fields[1];
    }

    private static string ReadGitAttribute(string repositoryPath, string relativePath, string attribute) {
        ProcessStartInfo startInfo = new("git") {
            WorkingDirectory = repositoryPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add("check-attr");
        startInfo.ArgumentList.Add(attribute);
        startInfo.ArgumentList.Add("--");
        startInfo.ArgumentList.Add(relativePath);

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Could not start git in {repositoryPath}.");
        string standardOutput = process.StandardOutput.ReadToEnd();
        string standardError = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0) {
            throw new InvalidOperationException(
                $"git check-attr failed in {repositoryPath} with exit code {process.ExitCode}: {standardError}");
        }

        string prefix = $"{relativePath}: {attribute}: ";
        standardOutput.ShouldStartWith(prefix);
        return standardOutput[prefix.Length..].Trim();
    }

    private static string[] ReadTrackedFiles(string repositoryPath, params string[] pathspecs) {
        ProcessStartInfo startInfo = new("git") {
            WorkingDirectory = repositoryPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add("ls-files");
        startInfo.ArgumentList.Add("-z");
        startInfo.ArgumentList.Add("--");
        foreach (string pathspec in pathspecs) {
            startInfo.ArgumentList.Add(pathspec);
        }

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Could not start git in {repositoryPath}.");
        string standardOutput = process.StandardOutput.ReadToEnd();
        string standardError = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0) {
            throw new InvalidOperationException(
                $"git ls-files failed in {repositoryPath} with exit code {process.ExitCode}: {standardError}");
        }

        return standardOutput.Split('\0', StringSplitOptions.RemoveEmptyEntries);
    }

    private static string FormatViolations(IEnumerable<GovernanceViolation> violations)
        => string.Join(Environment.NewLine, violations.Select(v => v.Message));
}

internal sealed record GovernanceViolation(
    string Path,
    string Reference,
    string ProviderFamily,
    string Message);

internal static class InfrastructureGovernance {
    private const string Remediation = "route through EventStore contract/client or deployment/AppHost component configuration";

    private static readonly ForbiddenRule[] PackageRules = [
        new("Dapr", "Dapr SDK", "Dapr."),
        new("StackExchange.Redis", "Redis", "StackExchange.Redis"),
        new("Microsoft.AspNetCore.SignalR.StackExchangeRedis", "Redis", "Microsoft.AspNetCore.SignalR.StackExchangeRedis"),
        // F01 — SignalR Client is only allowed in Hexalith.FrontComposer.Shell as the EventStore
        // projection nudge transport (Stories 5-1/5-3). The allowlist gate in ScanReferences
        // must skip this rule for Shell-marked scans; non-Shell projects fail governance.
        new("Microsoft.AspNetCore.SignalR.Client", "SignalR Client (Shell-only)", "Microsoft.AspNetCore.SignalR.Client"),
        new("Confluent.Kafka", "Kafka", "Confluent.Kafka"),
        new("Npgsql", "PostgreSQL", "Npgsql"),
        new("Microsoft.Azure.Cosmos", "Cosmos DB", "Microsoft.Azure.Cosmos"),
        new("Azure.Storage", "Azure Storage", "Azure.Storage"),
        new("Azure.Messaging.ServiceBus", "Azure Service Bus", "Azure.Messaging.ServiceBus"),
        new("Amazon.S3", "AWS provider SDK", "Amazon.S3"),
        new("Google.Cloud", "GCP provider SDK", "Google.Cloud"),
    ];

    private static readonly ForbiddenRule[] NamespaceRules = [
        new("Dapr", "Dapr SDK", "Dapr"),
        new("StackExchange.Redis", "Redis", "StackExchange.Redis"),
        new("Microsoft.AspNetCore.SignalR.StackExchangeRedis", "Redis", "Microsoft.AspNetCore.SignalR.StackExchangeRedis"),
        new("Microsoft.AspNetCore.SignalR.Client", "SignalR Client (Shell-only)", "Microsoft.AspNetCore.SignalR.Client"),
        new("Confluent.Kafka", "Kafka", "Confluent.Kafka"),
        new("Npgsql", "PostgreSQL", "Npgsql"),
        new("Microsoft.Azure.Cosmos", "Cosmos DB", "Microsoft.Azure.Cosmos"),
        new("Azure.Storage", "Azure Storage", "Azure.Storage"),
        new("Azure.Messaging.ServiceBus", "Azure Service Bus", "Azure.Messaging.ServiceBus"),
        new("Amazon.S3", "AWS provider SDK", "Amazon.S3"),
        new("Google.Cloud", "GCP provider SDK", "Google.Cloud"),
    ];

    public static List<GovernanceViolation> ScanProjectReferences(string projectPath, string root) {
        var document = XDocument.Load(projectPath);
        bool allowSignalRClient = IsShellProject(projectPath);
        // F22 — package deny-list applies to PackageReference only. ProjectReference Include is
        // a relative path to a peer project; conflating the two could false-fail governance for
        // benign sibling project names. Peer projects are themselves subject to assembly-reference
        // governance via ScanAssemblyReferences.
        IEnumerable<string> references = document
            .Descendants()
            .Where(static e => e.Name.LocalName == "PackageReference")
            .Select(static e => (string?)e.Attribute("Include") ?? (string?)e.Attribute("Update") ?? string.Empty)
            .Where(static value => value.Length > 0);

        return ScanReferences(projectPath, references, root, allowSignalRClient);
    }

    public static List<GovernanceViolation> ScanCentralPackageVersions(
        string propsPath,
        string root,
        IEnumerable<string>? packageIdsToScan = null) {
        var document = XDocument.Load(propsPath);
        HashSet<string>? packageFilter = packageIdsToScan is null
            ? null
            : new HashSet<string>(packageIdsToScan, StringComparer.OrdinalIgnoreCase);
        IEnumerable<string> references = document
            .Descendants()
            .Where(static e => e.Name.LocalName is "PackageVersion")
            .Select(static e => (string?)e.Attribute("Include") ?? (string?)e.Attribute("Update") ?? string.Empty)
            .Where(static value => value.Length > 0)
            .Where(value => packageFilter is null || packageFilter.Contains(value));

        // Central package versions don't dictate which projects USE the package; per-project
        // ScanProjectReferences enforces Shell-only consumption. SignalR.Client (and its
        // siblings shipped via the Client meta-package) may legitimately appear here so that
        // Shell.csproj can reference it.
        return ScanReferences(propsPath, references, root, allowSignalRClient: true);
    }

    public static (int Scanned, List<GovernanceViolation> Violations) ScanRestoredAssetsTracked(string projectPath, string root) {
        // F10 — return scan-count alongside violations so callers can detect silent skips when
        // `project.assets.json` is missing (no `dotnet restore` ran). Empty scan count is a
        // governance hole, not a clean run.
        string assetsPath = Path.Combine(Path.GetDirectoryName(projectPath)!, "obj", "project.assets.json");
        if (!File.Exists(assetsPath)) {
            return (0, []);
        }

        bool allowSignalRClient = IsShellProject(projectPath);
        using FileStream stream = File.OpenRead(assetsPath);
        using var document = JsonDocument.Parse(stream);
        HashSet<string> references = new(StringComparer.OrdinalIgnoreCase);
        if (document.RootElement.TryGetProperty("libraries", out JsonElement libraries)) {
            foreach (JsonProperty library in libraries.EnumerateObject()) {
                string id = library.Name.Split('/')[0];
                _ = references.Add(id);
            }
        }

        return (1, ScanReferences(assetsPath, references, root, allowSignalRClient));
    }

    public static List<GovernanceViolation> ScanRestoredAssets(string projectPath, string root)
        => ScanRestoredAssetsTracked(projectPath, root).Violations;

    public static List<GovernanceViolation> ScanAssemblyReferences(
        Assembly assembly,
        string displayPath,
        bool allowSignalRClient,
        string root)
        => ScanReferences(
            displayPath,
            assembly.GetReferencedAssemblies().Select(static name => name.Name ?? string.Empty),
            root,
            allowSignalRClient);

    public static List<GovernanceViolation> ScanSourceTree(string rootPath, string repositoryRoot, bool allowSignalRClient) {
        string normalizedRoot = NormalizeInsideRoot(rootPath, repositoryRoot);
        // F05 — refuse to follow symlinks/junctions/reparse points during enumeration. The
        // per-file ReparsePoint check earlier only stopped scanning the link itself, not the
        // contents reached through it. EnumerationOptions blocks the descent altogether.
        EnumerationOptions options = new() {
            RecurseSubdirectories = true,
            AttributesToSkip = FileAttributes.ReparsePoint | FileAttributes.Hidden,
            IgnoreInaccessible = true,
        };
        List<GovernanceViolation> violations = [];
        foreach (string file in Directory.EnumerateFiles(normalizedRoot, "*", options)) {
            if (!ShouldScanSourceFile(file, repositoryRoot)) {
                continue;
            }

            violations.AddRange(ScanSourceFile(file, repositoryRoot, allowSignalRClient));
        }

        return violations;
    }

    public static List<GovernanceViolation> ScanGeneratedBaselines(string rootPath, string repositoryRoot) {
        if (!Directory.Exists(rootPath)) {
            return [];
        }

        EnumerationOptions options = new() {
            RecurseSubdirectories = true,
            AttributesToSkip = FileAttributes.ReparsePoint | FileAttributes.Hidden,
            IgnoreInaccessible = true,
        };
        List<GovernanceViolation> violations = [];
        foreach (string file in Directory.EnumerateFiles(rootPath, "*.verified.txt", options)) {
            violations.AddRange(ScanSourceFile(file, repositoryRoot, allowSignalRClient: false));
        }

        return violations;
    }

    private static List<GovernanceViolation> ScanSourceFile(string file, string repositoryRoot, bool allowSignalRClient) {
        string relative = RelativePath(file, repositoryRoot);
        string text = File.ReadAllText(file);
        List<GovernanceViolation> violations = [];
        foreach (ForbiddenRule rule in NamespaceRules) {
            if (allowSignalRClient && rule.Reference == "Microsoft.AspNetCore.SignalR.Client") {
                continue;
            }

            if (ContainsNamespaceUse(text, rule.Reference)) {
                violations.Add(Violation(relative, rule.Reference, rule.ProviderFamily));
            }
        }

        return violations;
    }

    private static List<GovernanceViolation> ScanReferences(
        string path,
        IEnumerable<string> references,
        string root,
        bool allowSignalRClient) {
        string relative = Path.IsPathRooted(path) ? RelativePath(path, root) : path;
        List<GovernanceViolation> violations = [];
        foreach (string reference in references) {
            // Allow Microsoft.AspNetCore.SignalR.Client AND its sibling assemblies/packages
            // (`.Core`, `.Common`, etc.) which are transitive dependencies of the Client
            // meta-package. The Shell project legitimately consumes the family; the deny-list
            // rule still fires for non-Shell projects and for SignalR.StackExchangeRedis.
            if (allowSignalRClient
                && (string.Equals(reference, "Microsoft.AspNetCore.SignalR.Client", StringComparison.OrdinalIgnoreCase)
                    || reference.StartsWith("Microsoft.AspNetCore.SignalR.Client.", StringComparison.OrdinalIgnoreCase))) {
                continue;
            }

            foreach (ForbiddenRule rule in PackageRules) {
                if (MatchesPackageRule(reference, rule)) {
                    violations.Add(Violation(relative, reference, rule.ProviderFamily));
                }
            }
        }

        return violations;
    }

    private static bool MatchesPackageRule(string reference, ForbiddenRule rule)
        => string.Equals(reference, rule.Reference, StringComparison.OrdinalIgnoreCase)
            || reference.StartsWith(rule.Reference + ".", StringComparison.OrdinalIgnoreCase);

    private static bool ContainsNamespaceUse(string text, string namespaceRoot) {
        // F04 — restrict to authoritative namespace import lines. The previous substring scan
        // (text.Contains(namespaceRoot + ".")) would false-positive on any comment, XML doc, or
        // string literal that mentioned a forbidden provider. Adopters routinely reference
        // forbidden providers in human-readable commentary (e.g. ADRs, deprecation notes).
        // Scanning only `using` / `global using` lines keeps the deny-list authoritative for
        // actual symbol usage while letting docs/comments mention any namespace freely.
        string usingPrefix = "using " + namespaceRoot;
        string globalUsingPrefix = "global using " + namespaceRoot;
        foreach (string line in text.Split('\n')) {
            string trimmed = line.TrimStart();
            if (StartsWithBoundary(trimmed, usingPrefix) || StartsWithBoundary(trimmed, globalUsingPrefix)) {
                return true;
            }
        }

        return false;
    }

    private static bool StartsWithBoundary(string trimmed, string prefix) {
        if (!trimmed.StartsWith(prefix, StringComparison.Ordinal)) {
            return false;
        }

        if (trimmed.Length == prefix.Length) {
            return true;
        }

        char next = trimmed[prefix.Length];
        return next is '.' or ';' or ' ' or '\t' or '\r';
    }

    private static bool ShouldScanSourceFile(string file, string repositoryRoot) {
        FileInfo info = new(file);
        if ((info.Attributes & FileAttributes.ReparsePoint) != 0) {
            return false;
        }

        string relative = RelativePath(file, repositoryRoot).Replace('\\', '/');
        if (relative.Contains("/bin/", StringComparison.Ordinal)
            || relative.Contains("/obj/", StringComparison.Ordinal)
            || relative.StartsWith(".bmad/", StringComparison.Ordinal)
            || relative.StartsWith(".agents/", StringComparison.Ordinal)
            || relative.StartsWith(".github/skills/", StringComparison.Ordinal)) {
            return false;
        }

        string extension = Path.GetExtension(file);
        return extension is ".cs" or ".razor" or ".cshtml" or ".verified.txt";
    }

    private static bool IsShellProject(string projectPath)
        => Path.GetFileNameWithoutExtension(projectPath).Equals("Hexalith.FrontComposer.Shell", StringComparison.Ordinal);

    private static string NormalizeInsideRoot(string path, string root) {
        string fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string fullPath = Path.GetFullPath(path);
        if (!fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase)) {
            throw new InvalidOperationException($"Path escaped repository root: {path}");
        }

        return fullPath;
    }

    private static string RelativePath(string path, string root)
        => Path.GetRelativePath(root, NormalizeInsideRoot(path, root)).Replace('\\', '/');

    private static GovernanceViolation Violation(string path, string reference, string providerFamily)
        => new(
            path,
            reference,
            providerFamily,
            $"{path}: reference '{reference}' is forbidden provider family '{providerFamily}'; {Remediation}.");

    private sealed record ForbiddenRule(string Reference, string ProviderFamily, string NamespaceRoot);
}

internal sealed class TempRepo : IDisposable {
    private TempRepo(string root) => Root = root;

    public string Root { get; }

    public static TempRepo Create()
        => new(Path.Combine(Path.GetTempPath(), "fc-governance-" + Guid.NewGuid().ToString("N")));

    public string Write(string relativePath, string content) {
        string path = Path.Combine(Root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        return path;
    }

    public void Dispose() {
        if (Directory.Exists(Root)) {
            Directory.Delete(Root, recursive: true);
        }
    }
}
