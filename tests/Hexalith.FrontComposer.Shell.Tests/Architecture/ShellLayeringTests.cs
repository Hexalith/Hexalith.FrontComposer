using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Architecture;

[Trait("Category", "Governance")]
public sealed class ShellLayeringTests {
    private const string ShellNamespace = "Hexalith.FrontComposer.Shell";
    private const string TelemetryNamespace = ShellNamespace + ".Infrastructure.Telemetry";
    private const string EventStoreNamespace = ShellNamespace + ".Infrastructure.EventStore";
    private const string SchemaMismatchType = EventStoreNamespace + ".ProjectionSchemaMismatchException";
    private const string LoadPageEffectsPath = "State/DataGridNavigation/LoadPageEffects.cs";

    [Fact]
    public void ShellSources_NamespaceMatchesFolder_AndDependenciesFollowDeclaredLayers() {
        string shellRoot = LocateShellRoot();
        SourceFile[] sources = LoadSources(shellRoot);
        List<string> violations = FindViolations(sources);

        foreach (string razorPath in Directory.EnumerateFiles(shellRoot, "*.razor", SearchOption.AllDirectories)
            .Select(path => Normalize(Path.GetRelativePath(shellRoot, path)))
            .Where(path => path.StartsWith("State/", StringComparison.Ordinal)
                || path.StartsWith("Routing/", StringComparison.Ordinal))) {
            violations.Add($"{razorPath}: Razor files cannot live under State or Routing");
        }

        violations.ShouldBeEmpty(
            "Shell namespaces must match folders; State must not depend on Components; Routing must "
            + "remain pure except for cross-cutting telemetry; and only the exact LoadPageEffects "
            + "ProjectionSchemaMismatchException seam may point from State into EventStore. Violations: "
            + string.Join(", ", violations));
    }

    [Fact]
    public void ConcretePollingWorkers_HaveOneDeclaration_AtExactInfrastructurePath() {
        string shellRoot = LocateShellRoot();
        SourceFile[] sources = LoadSources(shellRoot);
        (string TypeName, string ExpectedPath, string ExpectedNamespace, string OldPath)[] expected = [
            (
                "PendingCommandPollingDriver",
                "Infrastructure/PendingCommands/PendingCommandPollingDriver.cs",
                ShellNamespace + ".Infrastructure.PendingCommands",
                "State/PendingCommands/PendingCommandPollingDriver.cs"),
            (
                "ProjectionFallbackPollingDriver",
                "Infrastructure/ProjectionConnection/ProjectionFallbackPollingDriver.cs",
                ShellNamespace + ".Infrastructure.ProjectionConnection",
                "State/ProjectionConnection/ProjectionFallbackPollingDriver.cs"),
            (
                "ProjectionFallbackRefreshScheduler",
                "Infrastructure/ProjectionConnection/ProjectionFallbackRefreshScheduler.cs",
                ShellNamespace + ".Infrastructure.ProjectionConnection",
                "State/ProjectionConnection/ProjectionFallbackRefreshScheduler.cs"),
        ];

        foreach ((string typeName, string expectedPath, string expectedNamespace, string oldPath) in expected) {
            List<TypeDeclaration> declarations = FindClassDeclarations(sources, typeName);
            declarations.Count.ShouldBe(1, $"{typeName} must have one concrete declaration.");
            declarations[0].Path.ShouldBe(expectedPath);
            declarations[0].Namespace.ShouldBe(expectedNamespace);
            declarations[0].IsAbstract.ShouldBeFalse($"{typeName} must remain concrete.");
            File.Exists(Path.Combine(shellRoot, NormalizeForFileSystem(oldPath))).ShouldBeFalse(
                $"Old concrete worker path must be absent: {oldPath}");
        }
    }

    [Fact]
    public void LayerGuard_SyntheticBypasses_ReportPathAndEdge() {
        SourceFile[] sources = [
            new(
                "State/FullyQualified.cs",
                "namespace Hexalith.FrontComposer.Shell.State;\n"
                + "internal sealed class FullyQualified { private Hexalith.FrontComposer.Shell.Components.Layout.FrontComposerNavigation? _value; }"),
            new(
                "State/Aliased.cs",
                "using RenderLayer = Hexalith.FrontComposer.Shell.Components.Layout;\n"
                + "namespace Hexalith.FrontComposer.Shell.State;\n"
                + "internal sealed class Aliased { private RenderLayer.FrontComposerNavigation? _value; }"),
            new(
                "State/GlobalStatic.cs",
                "global using static Hexalith.FrontComposer.Shell.Components.Layout.SyntheticStatics;\n"
                + "namespace Hexalith.FrontComposer.Shell.State;\n"
                + "internal sealed class GlobalStatic { }"),
        ];

        List<string> violations = FindViolations(sources);

        foreach (SourceFile source in sources) {
            violations.ShouldContain(violation => violation.Contains(source.Path, StringComparison.Ordinal)
                && violation.Contains("State -> Components", StringComparison.Ordinal),
                customMessage: string.Join(", ", violations));
        }
    }

    [Fact]
    public void LayerGuard_CommentsAndNamespaceText_CannotSpoofCompiledNamespace() {
        SourceFile source = new(
            "State/Spoofed.cs",
            "// namespace Hexalith.FrontComposer.Shell.State;\n"
            + "namespace Hexalith.FrontComposer.Shell.Components;\n"
            + "internal sealed class Spoofed { private const string Text = \"namespace Hexalith.FrontComposer.Shell.State;\"; }");

        List<string> violations = FindViolations([source]);

        violations.ShouldContain(violation => violation.Contains(
            "State/Spoofed.cs: namespace must be Hexalith.FrontComposer.Shell.State",
            StringComparison.Ordinal));
    }

    [Fact]
    public void LayerGuard_RoutingTelemetryEdge_IsExplicitlyAllowed() {
        SourceFile source = new(
            "Routing/TelemetryConsumer.cs",
            "using Hexalith.FrontComposer.Shell.Infrastructure.Telemetry;\n"
            + "namespace Hexalith.FrontComposer.Shell.Routing;\n"
            + "internal sealed class TelemetryConsumer { }");

        FindViolations([source]).ShouldBeEmpty();
    }

    private static List<string> FindViolations(IReadOnlyList<SourceFile> sources) {
        CSharpParseOptions parseOptions = new(LanguageVersion.Latest);
        Dictionary<SyntaxTree, SourceFile> sourceByTree = sources.ToDictionary(
            source => CSharpSyntaxTree.ParseText(source.Content, parseOptions, source.Path),
            source => source);
        CSharpCompilation compilation = CSharpCompilation.Create(
            "ShellLayeringAnalysis",
            sourceByTree.Keys,
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        HashSet<string> violations = new(StringComparer.Ordinal);
        foreach ((SyntaxTree tree, SourceFile source) in sourceByTree) {
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
            string expectedNamespace = ExpectedNamespace(source.Path);
            BaseNamespaceDeclarationSyntax[] namespaces = [.. root.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>()];
            if (!(string.Equals(source.Path, "GlobalUsings.cs", StringComparison.Ordinal) && namespaces.Length == 0)
                && (namespaces.Length != 1
                    || !string.Equals(namespaces[0].Name.ToString(), expectedNamespace, StringComparison.Ordinal))) {
                _ = violations.Add($"{source.Path}: namespace must be {expectedNamespace}");
            }

            SemanticModel semanticModel = compilation.GetSemanticModel(tree, ignoreAccessibility: true);
            foreach (UsingDirectiveSyntax directive in root.Usings) {
                if (directive.Name is null) {
                    continue;
                }

                Dependency dependency = ResolveDependency(directive.Name, semanticModel);
                AddViolationIfForbidden(violations, source, dependency);
            }

            foreach (NameSyntax name in root.DescendantNodes().OfType<NameSyntax>()) {
                if (name.Ancestors().Any(ancestor => ancestor is UsingDirectiveSyntax) || IsNamespaceName(name)) {
                    continue;
                }

                if (name.Parent is NameSyntax) {
                    continue;
                }

                Dependency dependency = ResolveDependency(name, semanticModel);
                AddViolationIfForbidden(violations, source, dependency);
            }

            foreach (QualifiedNameSyntax qualifiedName in root.DescendantNodes()
                .OfType<QualifiedNameSyntax>()
                .Where(name => name.Parent is not QualifiedNameSyntax
                    && !IsNamespaceName(name))) {
                string display = qualifiedName.ToString().Replace("global::", string.Empty, StringComparison.Ordinal);
                int start = display.IndexOf(ShellNamespace + ".", StringComparison.Ordinal);
                if (start >= 0) {
                    AddViolationIfForbidden(
                        violations,
                        source,
                        CreateDependency(display[start..], display[start..]));
                }
            }

            foreach (IdentifierNameSyntax identifier in root.DescendantNodes()
                .OfType<IdentifierNameSyntax>()
                .Where(name => string.Equals(name.Identifier.ValueText, "Hexalith", StringComparison.Ordinal)
                    && !IsNamespaceName(name))) {
                SyntaxNode completeName = identifier;
                while (completeName.Parent is QualifiedNameSyntax or MemberAccessExpressionSyntax or AliasQualifiedNameSyntax) {
                    completeName = completeName.Parent;
                }

                string display = completeName.ToString().Replace("global::", string.Empty, StringComparison.Ordinal).TrimEnd('?');
                if (display.StartsWith(ShellNamespace + ".", StringComparison.Ordinal)) {
                    AddViolationIfForbidden(violations, source, CreateDependency(display, display));
                }
            }
        }

        return [.. violations.OrderBy(static value => value, StringComparer.Ordinal)];
    }

    private static void AddViolationIfForbidden(
        ISet<string> violations,
        SourceFile source,
        Dependency dependency) {
        string sourceLayer = source.Path.Split('/')[0];
        if (!IsForbidden(source.Path, sourceLayer, dependency)) {
            return;
        }

        _ = violations.Add(
            $"{source.Path}: {sourceLayer} -> {dependency.TargetLayer} ({dependency.DisplayName})");
    }

    private static bool IsForbidden(string sourcePath, string sourceLayer, Dependency dependency) {
        if (dependency.TargetLayer is null) {
            return false;
        }

        if (string.Equals(dependency.Namespace, TelemetryNamespace, StringComparison.Ordinal)
            || dependency.Namespace.StartsWith(TelemetryNamespace + ".", StringComparison.Ordinal)) {
            return false;
        }

        if (string.Equals(sourceLayer, "Routing", StringComparison.Ordinal)) {
            return dependency.TargetLayer is "Components" or "State" or "Services" or "Infrastructure";
        }

        if (!string.Equals(sourceLayer, "State", StringComparison.Ordinal)) {
            return false;
        }

        if (string.Equals(dependency.TargetLayer, "Components", StringComparison.Ordinal)) {
            return true;
        }

        if (!string.Equals(dependency.TargetLayer, "Infrastructure", StringComparison.Ordinal)) {
            return false;
        }

        if (!string.Equals(sourcePath, LoadPageEffectsPath, StringComparison.Ordinal)) {
            return true;
        }

        return !string.Equals(dependency.DisplayName, EventStoreNamespace, StringComparison.Ordinal)
            && !string.Equals(dependency.DisplayName, SchemaMismatchType, StringComparison.Ordinal);
    }

    private static Dependency ResolveDependency(NameSyntax name, SemanticModel semanticModel) {
        ISymbol? symbol = semanticModel.GetAliasInfo(name)?.Target ?? semanticModel.GetSymbolInfo(name).Symbol;
        if (symbol is INamedTypeSymbol type) {
            string displayName = type.ToDisplayString();
            string targetNamespace = type.ContainingNamespace.ToDisplayString();
            Dependency resolved = CreateDependency(displayName, targetNamespace);
            if (resolved.TargetLayer is not null) {
                return resolved;
            }
        }

        if (symbol is INamespaceSymbol namespaceSymbol) {
            string targetNamespace = namespaceSymbol.ToDisplayString();
            Dependency resolved = CreateDependency(targetNamespace, targetNamespace);
            if (resolved.TargetLayer is not null) {
                return resolved;
            }
        }

        string display = name.ToString().Replace("global::", string.Empty, StringComparison.Ordinal);
        int layerStart = display.IndexOf(ShellNamespace + ".", StringComparison.Ordinal);
        if (layerStart >= 0) {
            display = display[layerStart..];
            return CreateDependency(display, display);
        }

        return new Dependency(display, string.Empty, null);
    }

    private static bool IsNamespaceName(SyntaxNode node)
        => node.Ancestors().OfType<BaseNamespaceDeclarationSyntax>()
            .Any(declaration => declaration.Name.Span.Contains(node.Span));

    private static Dependency CreateDependency(string displayName, string targetNamespace) {
        string prefix = ShellNamespace + ".";
        if (!targetNamespace.StartsWith(prefix, StringComparison.Ordinal)) {
            return new Dependency(displayName, targetNamespace, null);
        }

        string remainder = targetNamespace[prefix.Length..];
        string layer = remainder.Split('.')[0];
        return new Dependency(displayName, targetNamespace, layer);
    }

    private static List<TypeDeclaration> FindClassDeclarations(
        IEnumerable<SourceFile> sources,
        string typeName) {
        List<TypeDeclaration> declarations = [];
        foreach (SourceFile source in sources) {
            CompilationUnitSyntax root = CSharpSyntaxTree.ParseText(source.Content).GetCompilationUnitRoot();
            foreach (ClassDeclarationSyntax declaration in root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .Where(declaration => string.Equals(declaration.Identifier.ValueText, typeName, StringComparison.Ordinal))) {
                BaseNamespaceDeclarationSyntax? containingNamespace = declaration.Ancestors()
                    .OfType<BaseNamespaceDeclarationSyntax>()
                    .FirstOrDefault();
                declarations.Add(new TypeDeclaration(
                    source.Path,
                    containingNamespace?.Name.ToString() ?? string.Empty,
                    declaration.Modifiers.Any(SyntaxKind.AbstractKeyword)));
            }
        }

        return declarations;
    }

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

    private static string ExpectedNamespace(string relativePath) {
        string? directory = Path.GetDirectoryName(NormalizeForFileSystem(relativePath));
        return string.IsNullOrEmpty(directory)
            ? ShellNamespace
            : $"{ShellNamespace}.{directory.Replace(Path.DirectorySeparatorChar, '.')}";
    }

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

    private static string NormalizeForFileSystem(string path)
        => path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);

    private sealed record SourceFile(string Path, string Content);

    private sealed record Dependency(string DisplayName, string Namespace, string? TargetLayer);

    private sealed record TypeDeclaration(string Path, string Namespace, bool IsAbstract);
}
