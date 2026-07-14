using System.Reflection;

using Hexalith.FrontComposer.Mcp.Skills;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Shouldly;

namespace Hexalith.FrontComposer.Mcp.Tests.Skills;

[Trait("Category", "Governance")]
public sealed class SkillTypeOrganizationGovernanceTests {
    private const string SkillNamespace = "Hexalith.FrontComposer.Mcp.Skills";
    internal static readonly string[] RuntimeTypeNames = [
        "SkillCorpusDiagnosticCategory",
        "SkillCorpusDiagnostic",
        "SkillCorpusSource",
        "SkillCorpusResource",
        "SkillCorpusSnapshot",
        "SkillCorpusValidationResult",
        "SkillCorpusParser",
        "SkillCorpusLoader",
        "SkillCorpusReferenceValidator",
        "SkillCorpusSnippetValidator",
        "SkillResourceDescriptor",
        "SkillResourceReadResult",
        "SkillCorpusAggregateManifest",
        "SkillCorpusManifestEntry",
        "SkillCorpusAggregateManifestBuilder",
        "SkillResourceReadOptions",
        "FrontComposerSkillResourceProvider",
        "InvalidSkillCorpusException",
        "FrontComposerSkillMcpResource",
        "GeneratedCodeFailureCategory",
        "GeneratedCodeFile",
        "GeneratedCodeDiagnostic",
        "GeneratedCodeValidationResult",
        "GeneratedBoundedContextValidator",
        "ISkillCorpusBaselineProvider",
        "EmptySkillCorpusBaselineProvider",
        "SkillCorpusReleaseGuard",
    ];

    [Fact]
    public void ApprovedSkillsSlices_DirectDeclarations_UseOneSameNamedTypePerFile() {
        string repositoryRoot = LocateRepositoryRoot();
        (string Path, string Content)[] mcpSources = LoadProductionSources(Path.Combine(
            repositoryRoot,
            "src",
            "Hexalith.FrontComposer.Mcp",
            "Skills"));
        (string Path, string Content)[] benchSources = LoadProductionSources(Path.Combine(
            repositoryRoot,
            "tests",
            "Hexalith.FrontComposer.Shell.Tests.Bench",
            "Skills"));

        mcpSources.Length.ShouldBe(27, "The approved MCP Skills slice must contain the exact runtime declaration files.");
        benchSources.Length.ShouldBe(30, "The approved Bench Skills slice must contain 29 harness declarations plus BenchmarkHarnessTests.");
        FindOrganizationViolations(mcpSources.Concat(benchSources)).ShouldBeEmpty();
    }

    [Fact]
    public void OrganizationGuard_SyntheticConditionalNestedMultiKindSource_ReportsEveryDeclaration() {
        (string Path, string Content)[] sources = [
            (
                "Synthetic/MultiKind.cs",
                "namespace Hexalith { namespace FrontComposer { namespace Mcp { namespace Skills {\n"
                + "#if SYNTHETIC_FEATURE\n"
                + "public sealed class First { }\n"
                + "#else\n"
                + "public sealed record Second;\n"
                + "#endif\n"
                + "public readonly struct Third;\n"
                + "public enum Fourth { Value }\n"
                + "public delegate void Fifth();\n"
                + "} } } }\n"),
        ];

        IReadOnlyList<string> violations = FindOrganizationViolations(sources);

        violations.Count.ShouldBe(1);
        foreach (string declarationName in new[] { "First", "Second", "Third", "Fourth", "Fifth" }) {
            violations[0].ShouldContain(declarationName);
        }
    }

    [Fact]
    [Trait("Category", "Contract")]
    public void McpAssembly_ExportsExactRuntimeSkillsSurfaceWithoutBenchmarkTypes() {
        Assembly assembly = typeof(SkillCorpusParser).Assembly;

        string[] exportedSkillTypes = assembly.GetExportedTypes()
            .Where(type => string.Equals(type.Namespace, SkillNamespace, StringComparison.Ordinal))
            .Select(type => type.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        exportedSkillTypes.ShouldBe(RuntimeTypeNames.OrderBy(name => name, StringComparer.Ordinal));
        exportedSkillTypes.ShouldNotContain(name => name.StartsWith("SkillBenchmark", StringComparison.Ordinal));
        foreach (string generatedCodeTypeName in new[] {
            "GeneratedCodeFailureCategory",
            "GeneratedCodeFile",
            "GeneratedCodeDiagnostic",
            "GeneratedCodeValidationResult",
            "GeneratedBoundedContextValidator",
        }) {
            exportedSkillTypes.ShouldContain(generatedCodeTypeName);
        }
    }

    [Fact]
    public void BenchHarness_ConsumesOnlyTheApprovedMcpInternalHashSeam() {
        string repositoryRoot = LocateRepositoryRoot();
        (string Path, string Content)[] sources = LoadProductionSources(Path.Combine(
            repositoryRoot,
            "tests",
            "Hexalith.FrontComposer.Shell.Tests.Bench",
            "Skills"));
        SyntaxTree[] syntaxTrees = [.. sources
            .Where(source => !source.Path.EndsWith("/BenchmarkHarnessTests.cs", StringComparison.Ordinal))
            .Select(source => CSharpSyntaxTree.ParseText(
                source.Content,
                new CSharpParseOptions(LanguageVersion.Latest),
                source.Path))];
        string[] trustedPlatformAssemblies = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")
            ?? throw new InvalidOperationException("Trusted platform assemblies are unavailable."))
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
        string[] localAssemblies = Directory.EnumerateFiles(AppContext.BaseDirectory, "*.dll").ToArray();
        MetadataReference[] references = [.. trustedPlatformAssemblies
            .Concat(localAssemblies)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(path => MetadataReference.CreateFromFile(path))];
        CSharpCompilation compilation = CSharpCompilation.Create(
            "Hexalith.FrontComposer.Shell.Tests.Bench",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        List<string> internalMcpReferences = [];
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        foreach (SyntaxTree syntaxTree in syntaxTrees) {
            SemanticModel semanticModel = compilation.GetSemanticModel(syntaxTree);
            foreach (IdentifierNameSyntax identifier in syntaxTree.GetRoot(cancellationToken).DescendantNodes().OfType<IdentifierNameSyntax>()) {
                ISymbol? symbol = semanticModel.GetSymbolInfo(identifier, cancellationToken).Symbol;
                if (symbol is null
                    || !string.Equals(
                        symbol.ContainingAssembly?.Name,
                        typeof(SkillCorpusParser).Assembly.GetName().Name,
                        StringComparison.Ordinal)
                    || symbol.DeclaredAccessibility == Accessibility.Public) {
                    continue;
                }

                internalMcpReferences.Add($"{symbol.ContainingType?.Name}.{symbol.Name}");
            }
        }

        internalMcpReferences.Order(StringComparer.Ordinal).ShouldBe(new[] {
            "SkillCorpusParser.Sha256Hex",
            "SkillCorpusParser.Sha256Hex",
        });
    }

    private static IReadOnlyList<string> FindOrganizationViolations(
        IEnumerable<(string Path, string Content)> sources) {
        List<string> violations = [];

        foreach ((string path, string content) in sources) {
            IReadOnlyList<MemberDeclarationSyntax> declarations = GetDirectDeclarations(content);
            if (declarations.Count != 1) {
                violations.Add(
                    $"{path}: expected one declaration; found {declarations.Count} "
                    + $"({string.Join(", ", declarations.Select(GetDeclarationName))})");
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

    private static IReadOnlyList<MemberDeclarationSyntax> GetDirectDeclarations(string source) {
        string allConditionalBranches = ActivateAllConditionalBranches(source);
        CompilationUnitSyntax root = CSharpSyntaxTree.ParseText(
            allConditionalBranches,
            new CSharpParseOptions(LanguageVersion.Latest))
            .GetCompilationUnitRoot();
        return ExpandNamespaceMembers(root.Members)
            .Where(member => member is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax)
            .ToArray();
    }

    private static string ActivateAllConditionalBranches(string source) {
        char[] text = source.ToCharArray();
        SyntaxNode root = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest)).GetRoot();
        foreach (SyntaxTrivia trivia in root.DescendantTrivia(descendIntoTrivia: true)) {
            if (trivia.GetStructure() is not (IfDirectiveTriviaSyntax
                or ElifDirectiveTriviaSyntax
                or ElseDirectiveTriviaSyntax
                or EndIfDirectiveTriviaSyntax)) {
                continue;
            }

            for (int index = trivia.FullSpan.Start; index < trivia.FullSpan.End; index++) {
                if (text[index] is not ('\r' or '\n')) {
                    text[index] = ' ';
                }
            }
        }

        return new string(text);
    }

    private static IEnumerable<MemberDeclarationSyntax> ExpandNamespaceMembers(
        IEnumerable<MemberDeclarationSyntax> members) {
        foreach (MemberDeclarationSyntax member in members) {
            if (member is BaseNamespaceDeclarationSyntax namespaceDeclaration) {
                foreach (MemberDeclarationSyntax nested in ExpandNamespaceMembers(namespaceDeclaration.Members)) {
                    yield return nested;
                }
            }
            else {
                yield return member;
            }
        }
    }

    private static string GetDeclarationName(MemberDeclarationSyntax declaration)
        => declaration switch {
            BaseTypeDeclarationSyntax type => type.Identifier.ValueText,
            DelegateDeclarationSyntax @delegate => @delegate.Identifier.ValueText,
            _ => throw new InvalidOperationException($"Unsupported declaration kind {declaration.Kind()}."),
        };

    private static (string Path, string Content)[] LoadProductionSources(string root) {
        if (!Directory.Exists(root)) {
            return [];
        }

        return [
            .. Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
                .Select(path => new {
                    FullPath = path,
                    RelativePath = Normalize(Path.GetRelativePath(root, path)),
                })
                .Where(file => !IsBuildOrGeneratedPath(file.RelativePath))
                .OrderBy(file => file.RelativePath, StringComparer.Ordinal)
                .Select(file => ($"{Normalize(root)}/{file.RelativePath}", File.ReadAllText(file.FullPath))),
        ];
    }

    private static bool IsBuildOrGeneratedPath(string relativePath) {
        string[] segments = relativePath.Split('/');
        return segments.Any(segment => segment.Equals("bin", StringComparison.OrdinalIgnoreCase)
            || segment.Equals("obj", StringComparison.OrdinalIgnoreCase)
            || segment.Equals("generated", StringComparison.OrdinalIgnoreCase));
    }

    private static string LocateRepositoryRoot() {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null) {
            if (File.Exists(Path.Combine(directory.FullName, "Hexalith.FrontComposer.slnx"))) {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the FrontComposer repository root.");
    }

    private static string Normalize(string path) => path.Replace('\\', '/');
}
