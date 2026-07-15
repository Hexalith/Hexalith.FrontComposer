using Hexalith.FrontComposer.Shell.State.CapabilityDiscovery;
using Hexalith.FrontComposer.Shell.State.CommandPalette;
using Hexalith.FrontComposer.Shell.State.DataGridNavigation;
using Hexalith.FrontComposer.Shell.State.Density;
using Hexalith.FrontComposer.Shell.State.Navigation;
using Hexalith.FrontComposer.Shell.State.Theme;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.State;

[Trait("Category", "Governance")]
public sealed class HydrationStateConsolidationTests {
    private static readonly HashSet<string> FormerTypeNames = new(StringComparer.Ordinal) {
        "CommandPaletteHydrationState",
        "DataGridNavigationHydrationState",
        "DensityHydrationState",
        "NavigationHydrationState",
        "ThemeHydrationState",
    };

    [Fact]
    public void HydrationState_MembersAndNumericValues_RemainStable() {
        Enum.GetNames<HydrationState>().ShouldBe(["Idle", "Hydrating", "Hydrated"]);
        ((int)HydrationState.Idle).ShouldBe(0);
        ((int)HydrationState.Hydrating).ShouldBe(1);
        ((int)HydrationState.Hydrated).ShouldBe(2);
    }

    [Fact]
    public void PublicStateSignatures_UseSharedHydrationState() {
        Type[] stateTypes = [
            typeof(FrontComposerCommandPaletteState),
            typeof(DataGridNavigationState),
            typeof(FrontComposerDensityState),
            typeof(FrontComposerNavigationState),
            typeof(FrontComposerThemeState),
        ];

        foreach (Type stateType in stateTypes) {
            stateType.GetProperty("HydrationState")!.PropertyType.ShouldBe(typeof(HydrationState));
        }

        Type shellAssemblyMarker = typeof(FrontComposerThemeState);
        foreach (string formerTypeName in FormerTypeNames) {
            shellAssemblyMarker.Assembly.GetTypes().ShouldAllBe(
                type => !string.Equals(type.Name, formerTypeName, StringComparison.Ordinal));
        }
    }

    [Fact]
    public void CapabilityDiscoveryHydrationState_RemainsSemanticallyDistinct() {
        Enum.GetNames<CapabilityDiscoveryHydrationState>().ShouldBe(["Idle", "Seeding", "Seeded"]);
        typeof(FrontComposerCapabilityDiscoveryState)
            .GetProperty("HydrationState")!
            .PropertyType
            .ShouldBe(typeof(CapabilityDiscoveryHydrationState));
    }

    [Fact]
    public void ShellSources_ContainOnlySharedThreeMemberHydrationEnum() {
        SourceFile[] sources = LoadSources(LocateShellRoot());

        List<string> violations = FindViolations(sources, enforceProductionShape: true);

        violations.ShouldBeEmpty(
            "The five identical hydration enums must stay consolidated in State/HydrationState.cs; "
            + "CapabilityDiscoveryHydrationState remains the only allowed distinct hydration enum. Violations: "
            + string.Join(", ", violations));
    }

    [Fact]
    public void HydrationStateGuard_SyntheticDuplicateEnum_IsReported() {
        SourceFile[] sources = [
            new(
                "State/Theme/ThemeHydrationState.cs",
                "namespace Hexalith.FrontComposer.Shell.State.Theme;\n"
                + "public enum ThemeHydrationState { Idle, Hydrating, Hydrated }"),
        ];

        List<string> violations = FindViolations(sources, enforceProductionShape: false);

        violations.ShouldContain(violation => violation.Contains("ThemeHydrationState", StringComparison.Ordinal));
        violations.ShouldContain(violation => violation.Contains("duplicate three-member hydration enum", StringComparison.Ordinal));
    }

    [Fact]
    public void RepresentativeConsumer_CompilesAgainstSharedStateAndHydrationActions() {
        const string source = """
            using Hexalith.FrontComposer.Shell.State;
            using Hexalith.FrontComposer.Shell.State.CommandPalette;
            using Hexalith.FrontComposer.Shell.State.DataGridNavigation;
            using Hexalith.FrontComposer.Shell.State.Density;
            using Hexalith.FrontComposer.Shell.State.Navigation;
            using Hexalith.FrontComposer.Shell.State.Theme;

            public static class Consumer
            {
                public static HydrationState ReadTheme(FrontComposerThemeState state) => state.HydrationState;
                public static HydrationState ReadNavigation(FrontComposerNavigationState state) => state.HydrationState;
                public static object[] CreateActions() =>
                [
                    new PaletteHydratingAction(),
                    new DataGridNavigationHydratingAction(),
                    new DensityHydratingAction(),
                    new NavigationHydratingAction(),
                    new ThemeHydratingAction(),
                ];
            }
            """;
        string[] trustedPlatformAssemblies = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))?
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            ?? throw new InvalidOperationException("Trusted platform assemblies are unavailable.");
        MetadataReference[] references = [
            .. trustedPlatformAssemblies
                .Concat(Directory.EnumerateFiles(AppContext.BaseDirectory, "*.dll", SearchOption.TopDirectoryOnly))
                .Distinct(StringComparer.Ordinal)
                .Select(path => MetadataReference.CreateFromFile(path)),
        ];
        CSharpCompilation compilation = CSharpCompilation.Create(
            "HydrationStateConsumer",
            [CSharpSyntaxTree.ParseText(
                source,
                new CSharpParseOptions(LanguageVersion.Latest),
                cancellationToken: TestContext.Current.CancellationToken)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        Diagnostic[] errors = [.. compilation.GetDiagnostics(TestContext.Current.CancellationToken).Where(
            diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)];

        errors.ShouldBeEmpty(string.Join(Environment.NewLine, errors.Select(static diagnostic => diagnostic.ToString())));
    }

    private static List<string> FindViolations(
        IEnumerable<SourceFile> sources,
        bool enforceProductionShape) {
        List<string> violations = [];
        List<(string Path, string Name, string[] Members)> hydrationEnums = [];

        foreach (SourceFile source in sources) {
            CompilationUnitSyntax root = CSharpSyntaxTree.ParseText(source.Content).GetCompilationUnitRoot();
            foreach (EnumDeclarationSyntax declaration in root.DescendantNodes().OfType<EnumDeclarationSyntax>()) {
                string name = declaration.Identifier.ValueText;
                string[] members = [.. declaration.Members.Select(member => member.Identifier.ValueText)];
                if (FormerTypeNames.Contains(name)) {
                    violations.Add($"{source.Path}: former hydration enum identifier {name}");
                }

                if (members.SequenceEqual(["Idle", "Hydrating", "Hydrated"], StringComparer.Ordinal)) {
                    hydrationEnums.Add((source.Path, name, members));
                    if (!string.Equals(name, "HydrationState", StringComparison.Ordinal)) {
                        violations.Add($"{source.Path}: duplicate three-member hydration enum {name}");
                    }
                }
            }

            foreach (IdentifierNameSyntax identifier in root.DescendantNodes().OfType<IdentifierNameSyntax>()) {
                if (FormerTypeNames.Contains(identifier.Identifier.ValueText)) {
                    violations.Add($"{source.Path}: former hydration enum identifier {identifier.Identifier.ValueText}");
                }
            }
        }

        if (enforceProductionShape) {
            hydrationEnums.Count.ShouldBe(1, "Exactly one Idle/Hydrating/Hydrated enum must exist.");
            hydrationEnums[0].Path.ShouldBe("State/HydrationState.cs");
            hydrationEnums[0].Name.ShouldBe("HydrationState");

            SourceFile capabilitySource = sources.Single(
                source => source.Path == "State/CapabilityDiscovery/CapabilityDiscoveryHydrationState.cs");
            EnumDeclarationSyntax capability = CSharpSyntaxTree.ParseText(capabilitySource.Content)
                .GetCompilationUnitRoot()
                .DescendantNodes()
                .OfType<EnumDeclarationSyntax>()
                .Single(declaration => declaration.Identifier.ValueText == "CapabilityDiscoveryHydrationState");
            capability.Members.Select(member => member.Identifier.ValueText)
                .ShouldBe(["Idle", "Seeding", "Seeded"]);
        }

        return [.. violations.Distinct(StringComparer.Ordinal)];
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
