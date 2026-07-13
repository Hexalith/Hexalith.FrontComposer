using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Architecture;

/// <summary>
/// Prevents production Shell code from introducing JSON option construction outside <c>FcJson</c>.
/// </summary>
[Trait("Category", "Governance")]
public sealed class FcJsonGovernanceTests {
    private const string FcJsonPath = "Services/FcJson.cs";

    [Fact]
    public void ShellSources_ConstructJsonSerializerOptionsOnlyInFcJson() {
        SourceFile[] sources = LoadSources(LocateShellRoot());

        List<string> violations = FindViolations(sources, requireCanonicalHolder: true);

        violations.ShouldBeEmpty(
            "Production JSON options must be constructed only by Services/FcJson.cs. Violations: "
            + string.Join(", ", violations));
    }

    [Fact]
    public void JsonOptionsGuard_SyntheticProductionConstruction_IsReported() {
        SourceFile[] sources = [
            new(
                "Infrastructure/SyntheticAdapter.cs",
                "using System.Text.Json;\n"
                + "namespace Hexalith.FrontComposer.Shell.Infrastructure;\n"
                + "internal static class SyntheticAdapter {\n"
                + "    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);\n"
                + "}"),
        ];

        List<string> violations = FindViolations(sources, requireCanonicalHolder: false);

        violations.ShouldContain(violation => violation.Contains("SyntheticAdapter.cs", StringComparison.Ordinal));
    }

    private static List<string> FindViolations(
        IEnumerable<SourceFile> sources,
        bool requireCanonicalHolder) {
        List<string> violations = [];
        int holderCount = 0;

        foreach (SourceFile source in sources) {
            CompilationUnitSyntax root = CSharpSyntaxTree.ParseText(source.Content).GetCompilationUnitRoot();
            if (string.Equals(source.Path, FcJsonPath, StringComparison.Ordinal)) {
                holderCount++;
                continue;
            }

            foreach (BaseObjectCreationExpressionSyntax creation in root.DescendantNodes()
                .OfType<BaseObjectCreationExpressionSyntax>()
                .Where(IsJsonSerializerOptionsConstruction)) {
                FileLinePositionSpan location = creation.GetLocation().GetLineSpan();
                violations.Add($"{source.Path}:{location.StartLinePosition.Line + 1}: JsonSerializerOptions construction");
            }
        }

        if (requireCanonicalHolder && holderCount != 1) {
            violations.Add($"{FcJsonPath}: expected one canonical holder, actual {holderCount}");
        }

        return violations;
    }

    private static bool IsJsonSerializerOptionsConstruction(BaseObjectCreationExpressionSyntax creation) {
        if (creation is ObjectCreationExpressionSyntax explicitCreation) {
            return IsJsonSerializerOptionsType(explicitCreation.Type);
        }

        SyntaxNode? parent = creation.Parent;
        if (parent is EqualsValueClauseSyntax equalsValue) {
            SyntaxNode? declaration = equalsValue.Parent?.Parent;
            if (declaration is VariableDeclarationSyntax variableDeclaration) {
                return IsJsonSerializerOptionsType(variableDeclaration.Type);
            }

            if (declaration is PropertyDeclarationSyntax propertyDeclaration) {
                return IsJsonSerializerOptionsType(propertyDeclaration.Type);
            }
        }

        return creation.ArgumentList?.Arguments
            .Any(argument => argument.ToString().Contains("JsonSerializerDefaults", StringComparison.Ordinal)) == true;
    }

    private static bool IsJsonSerializerOptionsType(TypeSyntax type)
        => string.Equals(type.ToString(), "JsonSerializerOptions", StringComparison.Ordinal)
            || type.ToString().EndsWith(".JsonSerializerOptions", StringComparison.Ordinal);

    private static SourceFile[] LoadSources(string shellRoot) => [
        .. Directory.EnumerateFiles(shellRoot, "*.cs", SearchOption.AllDirectories)
            .Select(path => new {
                Path = path,
                Relative = Normalize(Path.GetRelativePath(shellRoot, path)),
            })
            .Where(file => !file.Relative.StartsWith("bin/", StringComparison.OrdinalIgnoreCase)
                && !file.Relative.StartsWith("obj/", StringComparison.OrdinalIgnoreCase))
            .Select(file => new SourceFile(file.Relative, File.ReadAllText(file.Path))),
    ];

    private static string LocateShellRoot() {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null) {
            string candidate = Path.Combine(directory.FullName, "src", "Hexalith.FrontComposer.Shell");
            if (Directory.Exists(candidate)) {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate src/Hexalith.FrontComposer.Shell.");
    }

    private static string Normalize(string path) => path.Replace('\\', '/');

    private sealed record SourceFile(string Path, string Content);
}
