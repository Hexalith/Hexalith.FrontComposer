using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Emitters;

/// <summary>
/// Prevents SourceTools emitters from reintroducing hand-rolled C# string-literal escaping.
/// </summary>
[Trait("Category", "Governance")]
public sealed class GeneratedLiteralGovernanceTests {
    private static readonly HashSet<string> RequiredSeams = new(StringComparer.Ordinal) {
        "RoleBodyHelpers.EscapeString",
        "McpManifestEmitter.Escape",
    };

    [Fact]
    public void EmitterSources_KeepResidualLiteralSeamsAsGeneratedLiteralDelegates() {
        SourceFile[] sources = LoadSources(LocateEmitterRoot());

        List<string> violations = FindViolations(sources, requireNamedSeams: true);

        violations.ShouldBeEmpty(
            "C# literal escaping must delegate to GeneratedLiteral.Escape. Violations: "
            + string.Join(", ", violations));
    }

    [Fact]
    public void LiteralGuard_SyntheticHandRolledEscape_IsReported() {
        SourceFile[] sources = [
            new(
                "SyntheticEmitter.cs",
                "using System.Text;\n"
                + "internal static class SyntheticEmitter {\n"
                + "    private static string Escape(string value) {\n"
                + "        var builder = new StringBuilder(value.Length);\n"
                + "        return builder.Append(value.Replace(\"\\\\\", \"\\\\\\\\\")).ToString();\n"
                + "    }\n"
                + "}"),
        ];

        List<string> violations = FindViolations(sources, requireNamedSeams: false);

        violations.ShouldContain(violation => violation.Contains("hand-rolled", StringComparison.Ordinal));
    }

    private static List<string> FindViolations(
        IEnumerable<SourceFile> sources,
        bool requireNamedSeams) {
        List<string> violations = [];
        Dictionary<string, int> seamCounts = RequiredSeams.ToDictionary(seam => seam, _ => 0, StringComparer.Ordinal);

        foreach (SourceFile source in sources) {
            CompilationUnitSyntax root = CSharpSyntaxTree.ParseText(source.Content).GetCompilationUnitRoot();
            foreach (MethodDeclarationSyntax method in root.DescendantNodes().OfType<MethodDeclarationSyntax>()) {
                string className = method.Ancestors().OfType<ClassDeclarationSyntax>()
                    .FirstOrDefault()?.Identifier.ValueText ?? string.Empty;
                string seam = className + "." + method.Identifier.ValueText;

                if (RequiredSeams.Contains(seam)) {
                    seamCounts[seam]++;
                    if (!IsGeneratedLiteralDelegate(method)) {
                        violations.Add($"{source.Path}: {seam} is not a thin GeneratedLiteral.Escape delegate");
                    }
                }

                if (IsLiteralEscapeMethod(method) && ContainsHandRolledEscaping(method)) {
                    violations.Add($"{source.Path}: {seam} contains hand-rolled literal escaping");
                }
            }
        }

        if (requireNamedSeams) {
            foreach (KeyValuePair<string, int> seamCount in seamCounts.Where(pair => pair.Value != 1)) {
                violations.Add($"{seamCount.Key}: expected one declaration, actual {seamCount.Value}");
            }
        }

        return violations;
    }

    private static bool IsGeneratedLiteralDelegate(MethodDeclarationSyntax method) {
        ExpressionSyntax? returned = method.ExpressionBody?.Expression;
        if (returned is null
            && method.Body?.Statements is [ReturnStatementSyntax returnStatement]) {
            returned = returnStatement.Expression;
        }

        return returned is InvocationExpressionSyntax invocation
            && invocation.Expression is MemberAccessExpressionSyntax memberAccess
            && string.Equals(memberAccess.Expression.ToString(), "GeneratedLiteral", StringComparison.Ordinal)
            && string.Equals(memberAccess.Name.Identifier.ValueText, "Escape", StringComparison.Ordinal)
            && invocation.ArgumentList.Arguments is [ArgumentSyntax argument]
            && argument.Expression is IdentifierNameSyntax identifier
            && string.Equals(identifier.Identifier.ValueText, "value", StringComparison.Ordinal);
    }

    private static bool IsLiteralEscapeMethod(MethodDeclarationSyntax method)
        => method.Identifier.ValueText is "Escape" or "EscapeString";

    private static bool ContainsHandRolledEscaping(MethodDeclarationSyntax method)
        => method.DescendantNodes().OfType<ObjectCreationExpressionSyntax>()
            .Any(creation => creation.Type.ToString().EndsWith("StringBuilder", StringComparison.Ordinal))
            || method.DescendantNodes().OfType<InvocationExpressionSyntax>()
                .Any(invocation => invocation.Expression is MemberAccessExpressionSyntax memberAccess
                    && string.Equals(memberAccess.Name.Identifier.ValueText, "Replace", StringComparison.Ordinal));

    private static SourceFile[] LoadSources(string emitterRoot) => [
        .. Directory.EnumerateFiles(emitterRoot, "*.cs", SearchOption.AllDirectories)
            .Select(path => new SourceFile(
                Normalize(Path.GetRelativePath(emitterRoot, path)),
                File.ReadAllText(path))),
    ];

    private static string LocateEmitterRoot() {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null) {
            string candidate = Path.Combine(
                directory.FullName,
                "src",
                "Hexalith.FrontComposer.SourceTools",
                "Emitters");
            if (Directory.Exists(candidate)) {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate SourceTools/Emitters.");
    }

    private static string Normalize(string path) => path.Replace('\\', '/');

    private sealed record SourceFile(string Path, string Content);
}
