using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Shell.Infrastructure.EventStore;

using Shouldly;

using Xunit;

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
    public void CentralPackageVersions_DoNotIntroduceForbiddenProviderPackages() {
        string root = RepositoryRoot();
        string centralProps = Path.Combine(root, "Directory.Packages.props");

        List<GovernanceViolation> violations = InfrastructureGovernance.ScanCentralPackageVersions(centralProps, root);

        violations.ShouldBeEmpty(FormatViolations(violations));
    }

    [Fact]
    public void RestoredAssets_DoNotContainForbiddenTransitiveProviderPackages() {
        string root = RepositoryRoot();
        List<GovernanceViolation> violations = [];
        foreach (string project in FrameworkProjectRelativePaths) {
            violations.AddRange(InfrastructureGovernance.ScanRestoredAssets(Path.Combine(root, project), root));
        }

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

        List<string> violations = [];
        foreach (string path in paths) {
            foreach (string file in Directory.EnumerateFiles(Path.Combine(root, path), "*.cs", SearchOption.AllDirectories)) {
                string text = File.ReadAllText(file);
                string relative = Path.GetRelativePath(root, file).Replace('\\', '/');
                if (Regex.IsMatch(text, @"\.Log(?:Trace|Debug|Information|Warning|Error|Critical)\s*\(\s*\$""", RegexOptions.Multiline)) {
                    violations.Add(relative + ": interpolated ILogger message template");
                }

                if (Regex.IsMatch(text, @"\.Log(?:Trace|Debug|Information|Warning|Error|Critical)\s*\(\s*ex\s*,", RegexOptions.Multiline)) {
                    violations.Add(relative + ": raw exception object passed to ILogger in sensitive path");
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
        using TempRepo temp = TempRepo.Create();
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
        using TempRepo temp = TempRepo.Create();
        string props = temp.Write(
            "Directory.Packages.props",
            """
            <Project>
              <ItemGroup>
                <PackageVersion Include="StackExchange.Redis" Version="1.0.0" />
              </ItemGroup>
            </Project>
            """);

        List<GovernanceViolation> violations = InfrastructureGovernance.ScanCentralPackageVersions(props, temp.Root);

        violations.Single().ProviderFamily.ShouldBe("Redis");
    }

    [Fact]
    public void SyntheticSourceNamespace_FailsButDocsAndBinAreExcluded() {
        using TempRepo temp = TempRepo.Create();
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
        using TempRepo temp = TempRepo.Create();
        string project = temp.Write(
            "src/Hexalith.FrontComposer.Shell/Shell.csproj",
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
        new("Confluent.Kafka", "Kafka", "Confluent.Kafka"),
        new("Npgsql", "PostgreSQL", "Npgsql"),
        new("Microsoft.Azure.Cosmos", "Cosmos DB", "Microsoft.Azure.Cosmos"),
        new("Azure.Storage", "Azure Storage", "Azure.Storage"),
        new("Azure.Messaging.ServiceBus", "Azure Service Bus", "Azure.Messaging.ServiceBus"),
        new("Amazon.S3", "AWS provider SDK", "Amazon.S3"),
        new("Google.Cloud", "GCP provider SDK", "Google.Cloud"),
    ];

    public static List<GovernanceViolation> ScanProjectReferences(string projectPath, string root) {
        XDocument document = XDocument.Load(projectPath);
        bool allowSignalRClient = IsShellProject(projectPath);
        IEnumerable<string> references = document
            .Descendants()
            .Where(static e => e.Name.LocalName is "PackageReference" or "ProjectReference")
            .Select(static e => (string?)e.Attribute("Include") ?? (string?)e.Attribute("Update") ?? string.Empty)
            .Where(static value => value.Length > 0);

        return ScanReferences(projectPath, references, root, allowSignalRClient);
    }

    public static List<GovernanceViolation> ScanCentralPackageVersions(string propsPath, string root) {
        XDocument document = XDocument.Load(propsPath);
        IEnumerable<string> references = document
            .Descendants()
            .Where(static e => e.Name.LocalName is "PackageVersion")
            .Select(static e => (string?)e.Attribute("Include") ?? string.Empty)
            .Where(static value => value.Length > 0);

        return ScanReferences(propsPath, references, root, allowSignalRClient: false);
    }

    public static List<GovernanceViolation> ScanRestoredAssets(string projectPath, string root) {
        string assetsPath = Path.Combine(Path.GetDirectoryName(projectPath)!, "obj", "project.assets.json");
        if (!File.Exists(assetsPath)) {
            return [];
        }

        bool allowSignalRClient = IsShellProject(projectPath);
        using FileStream stream = File.OpenRead(assetsPath);
        using JsonDocument document = JsonDocument.Parse(stream);
        HashSet<string> references = new(StringComparer.OrdinalIgnoreCase);
        if (document.RootElement.TryGetProperty("libraries", out JsonElement libraries)) {
            foreach (JsonProperty library in libraries.EnumerateObject()) {
                string id = library.Name.Split('/')[0];
                _ = references.Add(id);
            }
        }

        return ScanReferences(assetsPath, references, root, allowSignalRClient);
    }

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
        List<GovernanceViolation> violations = [];
        foreach (string file in Directory.EnumerateFiles(normalizedRoot, "*", SearchOption.AllDirectories)) {
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

        List<GovernanceViolation> violations = [];
        foreach (string file in Directory.EnumerateFiles(rootPath, "*.verified.txt", SearchOption.AllDirectories)) {
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
            if (allowSignalRClient && string.Equals(reference, "Microsoft.AspNetCore.SignalR.Client", StringComparison.OrdinalIgnoreCase)) {
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

    private static bool ContainsNamespaceUse(string text, string namespaceRoot)
        => text.Contains("using " + namespaceRoot, StringComparison.Ordinal)
            || text.Contains("global using " + namespaceRoot, StringComparison.Ordinal)
            || text.Contains(namespaceRoot + ".", StringComparison.Ordinal);

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
        return extension is ".cs" or ".razor" or ".cshtml";
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
