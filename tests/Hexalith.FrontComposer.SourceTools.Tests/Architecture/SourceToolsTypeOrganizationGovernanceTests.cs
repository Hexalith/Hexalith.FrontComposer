using System.Reflection;

using Hexalith.FrontComposer.SourceTools;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Architecture;

[Trait("Category", "Governance")]
public sealed class SourceToolsTypeOrganizationGovernanceTests {
    private const string DriftNamespace = "Hexalith.FrontComposer.SourceTools.Drift";
    private static readonly string[] TargetTypeNames = [
        "DriftConstants",
        "DriftOptionsResult",
        "DriftOptions",
        "DriftBaselineInput",
        "DriftBaselineLoadResult",
        "DriftBaselineSet",
        "DriftBaselineContract",
        "DriftBaselineProperty",
        "DriftBaselineLoader",
        "DriftCurrentContract",
        "DriftCurrentProperty",
        "DriftCurrentSnapshot",
        "DriftComparisonResult",
        "DriftComparisonService",
        "DriftDiagnosticFact",
        "DriftSanitizer",
        "DriftDiagnosticDescriptors",
    ];

    [Fact]
    public void ProductionSources_DirectDeclarations_UseOneSameNamedTypePerFile() {
        (string Path, string Content)[] sources = LoadProductionSources(LocateDriftRoot());

        IReadOnlyList<string> violations = FindOrganizationViolations(sources);

        sources.Length.ShouldBe(17);
        violations.ShouldBeEmpty(
            "Drift production files must contain exactly one direct namespace/compilation-unit type or delegate "
            + "whose name matches the file. Violations: "
            + string.Join(", ", violations));
    }

    [Fact]
    public void OrganizationGuard_SyntheticMultiKindSource_ReportsPathAndNames() {
        (string Path, string Content)[] sources = [
            (
                "Synthetic/MultiType.cs",
                "namespace Hexalith.FrontComposer.SourceTools.Drift;\n"
                + "internal sealed class First { }\n"
                + "internal delegate void Second();\n"),
        ];

        IReadOnlyList<string> violations = FindOrganizationViolations(sources);

        violations.Count.ShouldBe(1);
        violations[0].ShouldContain("Synthetic/MultiType.cs");
        violations[0].ShouldContain("First");
        violations[0].ShouldContain("Second");
    }

    [Fact]
    [Trait("Category", "Contract")]
    public void TargetTypes_AfterMechanicalSplit_PreserveExactInternalTopLevelIdentity() {
        Assembly assembly = typeof(FrontComposerGenerator).Assembly;
        Type[] types = TargetTypeNames
            .Select(name => assembly.GetType(DriftNamespace + "." + name, throwOnError: true)!)
            .ToArray();

        types.Length.ShouldBe(17);
        foreach (Type type in types) {
            type.Namespace.ShouldBe(DriftNamespace);
            type.DeclaringType.ShouldBeNull();
            type.IsNested.ShouldBeFalse();
            type.IsPublic.ShouldBeFalse();
            type.IsNotPublic.ShouldBeTrue();
            type.IsClass.ShouldBeTrue();
            type.IsSealed.ShouldBeTrue();
        }

        types.Count(type => type.IsAbstract).ShouldBe(4);
        types.Count(type => !type.IsAbstract).ShouldBe(13);

        Type baselineInput = types.Single(type => type.Name == "DriftBaselineInput");
        baselineInput.GetInterfaces().ShouldContain(typeof(IEquatable<>).MakeGenericType(baselineInput));

        Type comparisonService = types.Single(type => type.Name == "DriftComparisonService");
        comparisonService.IsSealed.ShouldBeTrue();
        comparisonService.IsAbstract.ShouldBeFalse();
    }

    [Fact]
    [Trait("Category", "Contract")]
    public void DriftNamespace_AfterMechanicalSplit_ExportsNoPublicTypes() {
        Assembly assembly = typeof(FrontComposerGenerator).Assembly;

        Type[] exportedDriftTypes = assembly.GetExportedTypes()
            .Where(type => string.Equals(type.Namespace, DriftNamespace, StringComparison.Ordinal))
            .ToArray();

        exportedDriftTypes.ShouldBeEmpty();
    }

    private static IReadOnlyList<string> FindOrganizationViolations(
        IEnumerable<(string Path, string Content)> sources) {
        List<string> violations = [];

        foreach ((string path, string content) in sources) {
            IReadOnlyList<MemberDeclarationSyntax> declarations = GetDirectTopLevelDeclarations(content);
            if (declarations.Count != 1) {
                violations.Add($"{path}: expected one declaration; found {declarations.Count} ({string.Join(", ", declarations.Select(GetDeclarationName))})");
                continue;
            }

            string declarationName = GetDeclarationName(declarations[0]);
            string fileName = Path.GetFileNameWithoutExtension(path);
            if (!string.Equals(declarationName, fileName, StringComparison.Ordinal)) {
                violations.Add($"{path}: declaration {declarationName} does not match file {fileName}");
            }
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

    private static (string Path, string Content)[] LoadProductionSources(string driftRoot) => [
        .. Directory.EnumerateFiles(driftRoot, "*.cs", SearchOption.AllDirectories)
            .Select(path => new {
                FullPath = path,
                RelativePath = Normalize(Path.GetRelativePath(driftRoot, path)),
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

    private static string LocateDriftRoot() {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null) {
            string candidate = Path.Combine(
                directory.FullName,
                "src",
                "Hexalith.FrontComposer.SourceTools",
                "Drift");
            if (Directory.Exists(candidate)) {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate src/Hexalith.FrontComposer.SourceTools/Drift.");
    }

    private static string Normalize(string path) => path.Replace('\\', '/');
}
