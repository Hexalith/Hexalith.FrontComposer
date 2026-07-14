using System.Reflection;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Cli.Tests.Architecture;

[Trait("Category", "Governance")]
public sealed class CliTypeOrganizationGovernanceTests {
    private const string CliNamespace = "Hexalith.FrontComposer.Cli";
    private static readonly string[] TargetTypeNames = [
        "MigrationCommand",
        "MigrationEdge",
        "MigrationCatalog",
        "MigrationPlan",
        "PlannedFileEdit",
        "SourceFileContent",
        "MigrationEntry",
        "MigrationResult",
        "MigrationSummary",
        "MigrationPlanner",
        "ProjectDocumentSet",
        "ProjectDocument",
        "ProjectDocumentLoader",
        "SourceFile",
        "MigrationDiagnostics",
        "MigrationDiagnosticScanner",
        "MigrationDiagnosticSidecarReader",
        "FrontComposerMigrationCodeFixProvider",
        "MigrationApplier",
        "WriteSafetyPolicy",
        "SubmoduleBoundaryReader",
        "UnifiedDiff",
        "MigrationJson",
        "InspectCommand",
        "InspectReport",
        "InspectSummary",
        "GeneratedFileInfo",
        "GeneratedSourceFamily",
        "InspectDiagnostic",
        "InspectLoadResult",
        "GeneratedOutputLoader",
        "FrameworkSelection",
        "GeneratedFileClassifier",
        "TypeMatcher",
        "TypeMatchResult",
        "DiagnosticFileReader",
        "InspectJson",
        "CommandLineException",
        "CommandOptions",
    ];

    [Fact]
    public void ProductionSources_DirectTopLevelDeclarations_HaveAtMostOnePerFile() {
        (string Path, string Content)[] sources = LoadProductionSources(LocateCliRoot());

        IReadOnlyList<string> violations = FindMultiTypeViolations(sources);

        violations.ShouldBeEmpty(
            "CLI production files may contain at most one direct namespace/compilation-unit type or delegate. "
            + "Violations: "
            + string.Join(", ", violations));
    }

    [Fact]
    public void OrganizationGuard_SyntheticMultiTypeSource_ReportsPathAndNames() {
        (string Path, string Content)[] sources = [
            (
                "Synthetic/MultiType.cs",
                "namespace Hexalith.FrontComposer.Cli;\n"
                + "internal sealed class First { }\n"
                + "internal delegate void Second();\n"),
        ];

        IReadOnlyList<string> violations = FindMultiTypeViolations(sources);

        violations.Count.ShouldBe(1);
        violations[0].ShouldContain("Synthetic/MultiType.cs");
        violations[0].ShouldContain("First");
        violations[0].ShouldContain("Second");
    }

    [Fact]
    [Trait("Category", "Contract")]
    public void TargetTypes_AfterMechanicalSplit_PreserveInternalTopLevelIdentity() {
        Assembly assembly = typeof(CliApplication).Assembly;

        string[] fullNames = TargetTypeNames.Select(name => CliNamespace + "." + name).ToArray();

        fullNames.Length.ShouldBe(39);
        foreach (string fullName in fullNames) {
            Type type = assembly.GetType(fullName, throwOnError: true)!;
            type.FullName.ShouldBe(fullName);
            type.DeclaringType.ShouldBeNull();
            type.IsNested.ShouldBeFalse();
            type.IsPublic.ShouldBeFalse();
            type.IsNotPublic.ShouldBeTrue();
        }
    }

    [Fact]
    [Trait("Category", "Contract")]
    public void ExportedAuthoredTypes_AfterMechanicalSplit_RemainExact() {
        Assembly assembly = typeof(CliApplication).Assembly;

        string[] exportedTypeNames = assembly.GetTypes()
            .Where(type => string.Equals(type.Namespace, CliNamespace, StringComparison.Ordinal)
                && type.IsPublic
                && !type.IsNested)
            .Select(type => type.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        exportedTypeNames.ShouldBe(["CliApplication", "ExitCodes", "OutputSanitizer"]);
    }

    private static IReadOnlyList<string> FindMultiTypeViolations(
        IEnumerable<(string Path, string Content)> sources) {
        List<string> violations = [];

        foreach ((string path, string content) in sources) {
            IReadOnlyList<MemberDeclarationSyntax> declarations = GetDirectTopLevelDeclarations(content);
            if (declarations.Count <= 1) {
                continue;
            }

            violations.Add($"{path}: {string.Join(", ", declarations.Select(GetDeclarationName))}");
        }

        return violations;
    }

    private static IReadOnlyList<MemberDeclarationSyntax> GetDirectTopLevelDeclarations(string source) {
        CompilationUnitSyntax root = CSharpSyntaxTree.ParseText(source).GetCompilationUnitRoot();
        return root.Members
            .SelectMany(member => member is BaseNamespaceDeclarationSyntax namespaceDeclaration
                ? namespaceDeclaration.Members
                : [member])
            .Where(member => member is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax)
            .ToArray();
    }

    private static string GetDeclarationName(MemberDeclarationSyntax declaration)
        => declaration switch {
            BaseTypeDeclarationSyntax type => type.Identifier.ValueText,
            DelegateDeclarationSyntax @delegate => @delegate.Identifier.ValueText,
            _ => throw new InvalidOperationException($"Unsupported declaration kind {declaration.Kind()}."),
        };

    private static (string Path, string Content)[] LoadProductionSources(string cliRoot) => [
        .. Directory.EnumerateFiles(cliRoot, "*.cs", SearchOption.AllDirectories)
            .Select(path => new {
                FullPath = path,
                RelativePath = Normalize(Path.GetRelativePath(cliRoot, path)),
            })
            .Where(file => !IsBuildOrGeneratedPath(file.RelativePath))
            .OrderBy(file => file.RelativePath, StringComparer.Ordinal)
            .Select(file => (file.RelativePath, File.ReadAllText(file.FullPath))),
    ];

    private static bool IsBuildOrGeneratedPath(string relativePath) {
        string[] segments = relativePath.Split('/');
        return segments.Any(segment => segment.Equals("bin", StringComparison.OrdinalIgnoreCase)
            || segment.Equals("obj", StringComparison.OrdinalIgnoreCase)
            || segment.Equals("generated", StringComparison.OrdinalIgnoreCase));
    }

    private static string LocateCliRoot() {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null) {
            string candidate = Path.Combine(directory.FullName, "src", "Hexalith.FrontComposer.Cli");
            if (Directory.Exists(candidate)) {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate src/Hexalith.FrontComposer.Cli.");
    }

    private static string Normalize(string path) => path.Replace('\\', '/');
}
