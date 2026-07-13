using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Architecture;

[Trait("Category", "Governance")]
public sealed class FatalExceptionGuardGovernanceTests {
    private const int ExpectedCatchFilterCount = 35;
    private const string ExceptionGuardPath = "Services/ExceptionGuard.cs";
    private static readonly HashSet<string> FatalTypeNames = new(StringComparer.Ordinal) {
        nameof(OutOfMemoryException),
        nameof(StackOverflowException),
        nameof(ThreadAbortException),
        nameof(AccessViolationException),
    };

    [Fact]
    public void ShellSources_UseSingleFatalExceptionGuardTaxonomy() {
        SourceFile[] sources = LoadSources(LocateShellRoot());

        List<string> violations = FindViolations(sources, enforceProductionShape: true);

        violations.ShouldBeEmpty(
            "Fatal catch filters must delegate to the single Services/ExceptionGuard helper, with no "
            + "local IsRecoverable declarations or ad-hoc fatal-type lists. Violations: "
            + string.Join(", ", violations));
    }

    [Fact]
    public void FatalExceptionGuard_SyntheticLocalClassifierAndFatalList_AreReported() {
        SourceFile[] sources = [
            new(
                "Services/Authorization/SyntheticGate.cs",
                "namespace Hexalith.FrontComposer.Shell.Services.Authorization;\n"
                + "internal sealed class SyntheticGate {\n"
                + "    private static bool IsRecoverable(Exception ex) => ex is not OutOfMemoryException;\n"
                + "    internal void Invoke() { try { } catch (Exception ex) when (ex is not StackOverflowException) { } }\n"
                + "}"),
        ];

        List<string> violations = FindViolations(sources, enforceProductionShape: false);

        violations.ShouldContain(violation => violation.Contains("IsRecoverable", StringComparison.Ordinal));
        violations.ShouldContain(violation => violation.Contains("ad-hoc fatal taxonomy", StringComparison.Ordinal));
    }

    private static List<string> FindViolations(
        IEnumerable<SourceFile> sources,
        bool enforceProductionShape) {
        List<string> violations = [];
        List<(string Path, string Namespace)> guardDeclarations = [];
        int guardFilterInvocations = 0;

        foreach (SourceFile source in sources) {
            CompilationUnitSyntax root = CSharpSyntaxTree.ParseText(source.Content).GetCompilationUnitRoot();
            foreach (ClassDeclarationSyntax declaration in root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .Where(declaration => string.Equals(
                    declaration.Identifier.ValueText,
                    "ExceptionGuard",
                    StringComparison.Ordinal))) {
                string guardNamespace = declaration.Ancestors()
                    .OfType<BaseNamespaceDeclarationSyntax>()
                    .FirstOrDefault()?.Name.ToString() ?? string.Empty;
                guardDeclarations.Add((source.Path, guardNamespace));
            }

            foreach (MethodDeclarationSyntax declaration in root.DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Where(declaration => string.Equals(
                    declaration.Identifier.ValueText,
                    "IsRecoverable",
                    StringComparison.Ordinal))) {
                violations.Add($"{source.Path}: local IsRecoverable declaration");
            }

            foreach (LocalFunctionStatementSyntax declaration in root.DescendantNodes()
                .OfType<LocalFunctionStatementSyntax>()
                .Where(declaration => string.Equals(
                    declaration.Identifier.ValueText,
                    "IsRecoverable",
                    StringComparison.Ordinal))) {
                violations.Add($"{source.Path}: local IsRecoverable declaration");
            }

            foreach (CatchClauseSyntax catchClause in root.DescendantNodes().OfType<CatchClauseSyntax>()) {
                CatchFilterClauseSyntax? filter = catchClause.Filter;
                if (filter is null) {
                    continue;
                }

                if (filter.FilterExpression.DescendantNodesAndSelf()
                    .OfType<IdentifierNameSyntax>()
                    .Any(identifier => FatalTypeNames.Contains(identifier.Identifier.ValueText))) {
                    violations.Add($"{source.Path}: catch filter contains an ad-hoc fatal taxonomy");
                }

                guardFilterInvocations += filter.FilterExpression.DescendantNodesAndSelf()
                    .OfType<InvocationExpressionSyntax>()
                    .Count(IsExceptionGuardInvocation);
            }
        }

        if (enforceProductionShape) {
            if (guardDeclarations.Count != 1) {
                violations.Add($"ExceptionGuard declarations: expected 1, actual {guardDeclarations.Count}");
            }
            else {
                guardDeclarations[0].Path.ShouldBe(ExceptionGuardPath);
                guardDeclarations[0].Namespace.ShouldBe("Hexalith.FrontComposer.Shell.Services");
            }

            if (guardFilterInvocations != ExpectedCatchFilterCount) {
                violations.Add(
                    $"ExceptionGuard.IsFatal catch-filter calls: expected {ExpectedCatchFilterCount}, actual {guardFilterInvocations}");
            }
        }

        return violations;
    }

    private static bool IsExceptionGuardInvocation(InvocationExpressionSyntax invocation)
        => invocation.Expression is MemberAccessExpressionSyntax memberAccess
            && string.Equals(memberAccess.Name.Identifier.ValueText, "IsFatal", StringComparison.Ordinal)
            && memberAccess.Expression.ToString().EndsWith("ExceptionGuard", StringComparison.Ordinal);

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
