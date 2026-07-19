using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Architecture;

[Trait("Category", "Governance")]
public sealed class ShellTypeOrganizationGovernanceTests
{
    private const string ShellAssemblyName = "Hexalith.FrontComposer.Shell";
    private const string ShellNamespace = "Hexalith.FrontComposer.Shell";
    private const string ShellRepositoryPathPrefix = "src/Hexalith.FrontComposer.Shell/";

    private static readonly IReadOnlyDictionary<string, string[]> ActionGroupExceptions
        = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["State/CapabilityDiscovery/CapabilityDiscoveryActions.cs"] =
            [
                "BadgeCountsSeededAction",
                "BadgeCountChangedAction",
                "CapabilityVisitedAction",
                "SeenCapabilitiesHydratedAction",
            ],
            ["State/CommandPalette/CommandPaletteActions.cs"] =
            [
                "PaletteOpenedAction",
                "PaletteClosedAction",
                "PaletteQueryChangedAction",
                "PaletteScopeChangedAction",
                "PaletteResultsComputedAction",
                "PaletteSelectionMovedAction",
                "PaletteResultActivatedAction",
                "RecentRouteVisitedAction",
                "PaletteHydratedAction",
                "PaletteHydratingAction",
                "PaletteHydratedCompletedAction",
            ],
            ["State/DataGridNavigation/GridViewHydratedAction.cs"] =
            [
                "GridViewHydratedAction",
                "DataGridNavigationHydratingAction",
                "DataGridNavigationHydratedCompletedAction",
            ],
            ["State/Density/DensityActions.cs"] =
            [
                "DensityChangedAction",
                "UserPreferenceChangedAction",
                "UserPreferenceClearedAction",
                "DensityHydratedAction",
                "EffectiveDensityRecomputedAction",
                "DensityHydratingAction",
                "DensityHydratedCompletedAction",
            ],
            ["State/Navigation/NavigationActions.cs"] =
            [
                "SidebarToggledAction",
                "NavGroupToggledAction",
                "ViewportTierChangedAction",
                "SidebarExpandedAction",
                "NavigationHydratedAction",
                "LastActiveRouteChangedAction",
                "LastActiveRouteHydratedAction",
                "StorageReadyAction",
                "NavigationHydratingAction",
                "NavigationHydratedCompletedAction",
            ],
            ["State/Theme/ThemeActions.cs"] =
            [
                "ThemeChangedAction",
                "ThemeHydratingAction",
                "ThemeHydratedCompletedAction",
            ],
        };

    [Fact]
    public void HandwrittenShellSources_DirectDeclarations_FollowExactFileOrganization()
    {
        SourceFile[] sources = LoadShellSources();

        sources.ShouldNotBeEmpty("the organization scan must cover handwritten Shell sources");
        sources.ShouldContain(source => source.Path.EndsWith(".razor.cs", StringComparison.Ordinal));
        FindOrganizationViolations(sources, ActionGroupExceptions, ShellRepositoryPathPrefix).ShouldBeEmpty();
    }

    [Fact]
    public void SplitDeclarations_SourceShapes_MatchExactPinnedManifest()
    {
        SourceFile[] sources = LoadShellSources();
        DeclarationPin[] expected = ParseDeclarationPins();
        DirectDeclaration[] actual = [.. sources.SelectMany(ParseDirectDeclarations)];

        expected.Length.ShouldBe(111);
        expected.Count(pin => pin.Accessibility == "public").ShouldBe(97);
        expected.Count(pin => pin.Accessibility == "internal").ShouldBe(14);
        FindManifestUniquenessViolations(expected).ShouldBeEmpty();
        actual.ShouldNotBeEmpty("the source-shape pin must scan real Shell declarations");

        foreach (DeclarationPin pin in expected)
        {
            DirectDeclaration[] matches =
            [
                .. actual.Where(declaration => string.Equals(
                    declaration.Identity,
                    pin.Identity,
                    StringComparison.Ordinal)),
            ];
            matches.Length.ShouldBe(1, $"{pin.Identity} must have exactly one direct source declaration");
            matches[0].Path.ShouldBe(pin.Path);
            matches[0].Kind.ShouldBe(pin.Kind);
            matches[0].Accessibility.ShouldBe(pin.Accessibility);
            matches[0].Modifiers.ShouldBe(pin.Modifiers);
        }
    }

    [Fact]
    public void SplitDeclarations_RuntimeShapes_RemainTopLevelInShellAssembly()
    {
        Assembly shellAssembly = typeof(Hexalith.FrontComposer.Shell.Options.FcShellOptions).Assembly;
        DeclarationPin[] expected = ParseDeclarationPins();

        shellAssembly.GetName().Name.ShouldBe(ShellAssemblyName);
        foreach (DeclarationPin pin in expected)
        {
            Type? type = shellAssembly.GetType(pin.Identity, throwOnError: false, ignoreCase: false);

            type.ShouldNotBeNull($"{pin.Identity} must remain loadable from {ShellAssemblyName}");
            type.Assembly.ShouldBe(shellAssembly);
            type.DeclaringType.ShouldBeNull($"{pin.Identity} must remain top-level");
            type.IsNested.ShouldBeFalse($"{pin.Identity} must remain top-level");
            (type.IsPublic ? "public" : "internal").ShouldBe(pin.Accessibility);
            RuntimeKind(type).ShouldBe(pin.Kind);
        }
    }

    [Fact]
    public void OrganizationGuard_InterfaceImplementationAndDtoBundle_IsReported()
    {
        SourceFile[] sources =
        [
            new(
                "Services/Widget.cs",
                "namespace Hexalith.FrontComposer.Shell.Services; "
                + "public interface IWidget { } public sealed class Widget : IWidget { } "
                + "public sealed record WidgetDto(string Value);"),
        ];

        AssertSyntheticViolation(sources, "Services/Widget.cs", "3 direct declarations");
    }

    [Fact]
    public void OrganizationGuard_MixedDeclarationKinds_IsReported()
    {
        SourceFile[] sources =
        [
            new(
                "State/Example/Example.cs",
                "namespace Hexalith.FrontComposer.Shell.State.Example; "
                + "public enum ExampleKind { None } public sealed record Example(string Value);"),
        ];

        AssertSyntheticViolation(sources, "State/Example/Example.cs", "2 direct declarations");
    }

    [Fact]
    public void OrganizationGuard_InactiveConditionalDeclaration_IsReported()
    {
        SourceFile[] sources =
        [
            new(
                "Services/ConditionalOwner.cs",
                "namespace Hexalith.FrontComposer.Shell.Services;\n"
                + "public sealed class ConditionalOwner { }\n"
                + "#if false\npublic sealed record HiddenDto(string Value);\n#endif"),
        ];

        AssertSyntheticViolation(sources, "Services/ConditionalOwner.cs", "2 direct declarations");
    }

    [Fact]
    public void OrganizationGuard_NestedNamespaceDeclarations_AreReported()
    {
        SourceFile[] sources =
        [
            new(
                "Services/NestedOwner.cs",
                "namespace Hexalith.FrontComposer.Shell.Services { public sealed class NestedOwner { } "
                + "namespace Hidden { public sealed record NestedDto(string Value); } }"),
        ];

        AssertSyntheticViolation(sources, "Services/NestedOwner.cs", "2 direct declarations");
    }

    [Fact]
    public void OrganizationGuard_FileNameMismatch_IsReported()
    {
        SourceFile[] sources =
        [
            new(
                "Services/ExpectedOwner.cs",
                "namespace Hexalith.FrontComposer.Shell.Services; public sealed class OtherOwner { }"),
        ];

        AssertSyntheticViolation(sources, "Services/ExpectedOwner.cs", "must match direct declaration OtherOwner");
    }

    [Fact]
    public void OrganizationGuard_UnallowlistedActionGroup_IsReported()
    {
        SourceFile[] sources =
        [
            new(
                "State/Example/ExampleActions.cs",
                "namespace Hexalith.FrontComposer.Shell.State.Example; "
                + "public sealed record FirstAction; public sealed record SecondAction;"),
        ];

        AssertSyntheticViolation(sources, "State/Example/ExampleActions.cs", "2 direct declarations");
    }

    [Fact]
    public void OrganizationGuard_ConditionalDeclarationsWithSameIdentity_AreReported()
    {
        SourceFile[] sources =
        [
            new(
                "Services/ConditionalOwner.cs",
                "namespace Hexalith.FrontComposer.Shell.Services;\n"
                + "#if FIRST\npublic sealed record ConditionalOwner(string First);\n"
                + "#else\npublic sealed record ConditionalOwner(string Second);\n#endif"),
        ];

        AssertSyntheticViolation(sources, "Services/ConditionalOwner.cs", "2 direct declarations");
    }

    [Fact]
    public void OrganizationGuard_GenericAllowlistedAction_IsReported()
    {
        const string path = "State/Example/ExampleActions.cs";
        SourceFile[] sources =
        [
            new(
                path,
                "namespace Hexalith.FrontComposer.Shell.State.Example; "
                + "public sealed record ExampleAction<T>(T Value);"),
        ];
        Dictionary<string, string[]> exceptions = new(StringComparer.Ordinal)
        {
            [path] = ["ExampleAction"],
        };

        FindOrganizationViolations(sources, exceptions).ShouldContain(
            violation => violation.Contains(path, StringComparison.Ordinal)
                && violation.Contains("non-generic", StringComparison.Ordinal));
    }

    [Fact]
    public void OrganizationGuard_ProductionViolation_ReportsRepositoryRelativePath()
    {
        SourceFile[] sources =
        [
            new(
                "Services/ExpectedOwner.cs",
                "namespace Hexalith.FrontComposer.Shell.Services; public sealed class OtherOwner { }"),
        ];

        FindOrganizationViolations(
            sources,
            new Dictionary<string, string[]>(StringComparer.Ordinal),
            ShellRepositoryPathPrefix).ShouldContain(
                violation => violation.StartsWith(
                    $"{ShellRepositoryPathPrefix}Services/ExpectedOwner.cs:",
                    StringComparison.Ordinal));
    }

    [Fact]
    public void DeclarationManifest_DuplicatePathAndIdentity_AreReported()
    {
        DeclarationPin duplicate = new(
            "Services/Example.cs",
            "Hexalith.FrontComposer.Shell.Services.Example",
            "class",
            "public",
            "public sealed");

        List<string> violations = FindManifestUniquenessViolations([duplicate, duplicate]);

        violations.ShouldContain("duplicate manifest path Services/Example.cs");
        violations.ShouldContain("duplicate manifest identity Hexalith.FrontComposer.Shell.Services.Example");
    }

    private static void AssertSyntheticViolation(
        IReadOnlyList<SourceFile> sources,
        string expectedPath,
        string expectedMessage)
    {
        List<string> violations = FindOrganizationViolations(
            sources,
            new Dictionary<string, string[]>(StringComparer.Ordinal));

        violations.ShouldContain(
            violation => violation.Contains(expectedPath, StringComparison.Ordinal)
                && violation.Contains(expectedMessage, StringComparison.Ordinal),
            customMessage: string.Join(", ", violations));
    }

    private static List<string> FindOrganizationViolations(
        IReadOnlyList<SourceFile> sources,
        IReadOnlyDictionary<string, string[]> actionGroupExceptions,
        string reportPathPrefix = "")
    {
        List<string> violations = [];
        Dictionary<string, SourceFile> sourceByPath = sources.ToDictionary(source => source.Path, StringComparer.Ordinal);

        foreach ((string path, string[] expectedNames) in actionGroupExceptions)
        {
            string reportPath = $"{reportPathPrefix}{path}";
            if (!sourceByPath.TryGetValue(path, out SourceFile? source))
            {
                violations.Add($"{reportPath}: exact action-group exception file is missing");
                continue;
            }

            DirectDeclaration[] declarations = ParseDirectDeclarations(source);
            string expectedNamespace = NamespaceFromPath(path);
            declarations.Select(declaration => declaration.Name).ShouldBe(
                expectedNames,
                ignoreOrder: false,
                $"{path} must retain its exact ordered action identity set");
            if (declarations.Length != expectedNames.Length)
            {
                violations.Add($"{reportPath}: expected {expectedNames.Length} exact action declarations, found {declarations.Length}");
            }

            foreach (DirectDeclaration declaration in declarations)
            {
                string expectedModifiers = declaration.Name == "ThemeChangedAction" ? "public" : "public sealed";
                if (!declaration.Name.EndsWith("Action", StringComparison.Ordinal)
                    || declaration.Kind != "record"
                    || declaration.Accessibility != "public"
                    || declaration.Modifiers != expectedModifiers
                    || declaration.Arity != 0
                    || declaration.Namespace != expectedNamespace)
                {
                    violations.Add(
                        $"{reportPath}: {declaration.Identity} must be an exact public{(expectedModifiers.Contains("sealed", StringComparison.Ordinal) ? " sealed" : string.Empty)} non-generic top-level record action");
                }
            }
        }

        foreach (SourceFile source in sources)
        {
            string reportPath = $"{reportPathPrefix}{source.Path}";
            if (actionGroupExceptions.ContainsKey(source.Path))
            {
                continue;
            }

            DirectDeclaration[] declarations = ParseDirectDeclarations(source);
            if (declarations.Length > 1)
            {
                violations.Add(
                    $"{reportPath}: found {declarations.Length} direct declarations "
                    + $"({string.Join(", ", declarations.Select(declaration => declaration.Name))}); exactly one is allowed");
                continue;
            }

            if (declarations.Length == 1)
            {
                string ownerName = NormalizeOwnerName(source.Path);
                if (!string.Equals(ownerName, declarations[0].Name, StringComparison.Ordinal))
                {
                    violations.Add(
                        $"{reportPath}: normalized file owner {ownerName} must match direct declaration {declarations[0].Name}");
                }
            }
        }

        return violations;
    }

    private static SourceFile[] LoadShellSources()
    {
        string repositoryRoot = FindRepositoryRoot();
        string shellRoot = Path.Combine(repositoryRoot, "src", ShellAssemblyName);

        return
        [
            .. Directory.EnumerateFiles(shellRoot, "*.cs", SearchOption.AllDirectories)
                .Select(path => new SourceFile(
                    Normalize(Path.GetRelativePath(shellRoot, path)),
                    File.ReadAllText(path)))
                .Where(source => !IsGeneratedPath(source.Path))
                .OrderBy(source => source.Path, StringComparer.Ordinal),
        ];
    }

    private static bool IsGeneratedPath(string path)
    {
        string normalized = $"/{Normalize(path)}/";
        return normalized.Contains("/bin/", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("/obj/", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".g.i.cs", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".generated.cs", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".designer.cs", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".AssemblyInfo.cs", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".AssemblyAttributes.cs", StringComparison.OrdinalIgnoreCase);
    }

    private static DirectDeclaration[] ParseDirectDeclarations(SourceFile source)
    {
        CSharpParseOptions initialOptions = new(LanguageVersion.Latest);
        CompilationUnitSyntax initialRoot = CSharpSyntaxTree.ParseText(
            source.Content,
            initialOptions,
            source.Path).GetCompilationUnitRoot();
        string[] conditionalSymbols =
        [
            .. initialRoot.DescendantTrivia(descendIntoTrivia: true)
                .Select(trivia => trivia.GetStructure())
                .OfType<ConditionalDirectiveTriviaSyntax>()
                .SelectMany(directive => directive.Condition.DescendantTokens())
                .Where(token => token.IsKind(SyntaxKind.IdentifierToken))
                .Select(token => token.ValueText)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(symbol => symbol, StringComparer.Ordinal),
        ];
        conditionalSymbols.Length.ShouldBeLessThanOrEqualTo(
            8,
            $"{source.Path} has too many conditional symbols for exhaustive organization analysis");

        Dictionary<(string Identity, string Kind, string Accessibility, string Modifiers, int SpanStart), DirectDeclaration>
            declarations = [];
        int combinations = 1 << conditionalSymbols.Length;
        for (int mask = 0; mask < combinations; mask++)
        {
            string[] activeSymbols =
            [
                .. conditionalSymbols.Where((_, index) => (mask & (1 << index)) != 0),
            ];
            CSharpParseOptions options = new(LanguageVersion.Latest, preprocessorSymbols: activeSymbols);
            CompilationUnitSyntax root = CSharpSyntaxTree.ParseText(
                source.Content,
                options,
                source.Path).GetCompilationUnitRoot();

            foreach (MemberDeclarationSyntax member in root.DescendantNodes().OfType<MemberDeclarationSyntax>()
                .Where(IsDirectDeclaration))
            {
                DirectDeclaration declaration = CreateDeclaration(source.Path, member);
                var key = (
                    declaration.Identity,
                    declaration.Kind,
                    declaration.Accessibility,
                    declaration.Modifiers,
                    declaration.SpanStart);
                declarations.TryAdd(key, declaration);
            }
        }

        string allBranchesActive = ActivateAllConditionalBranches(source.Content, initialRoot);
        CompilationUnitSyntax allBranchesRoot = CSharpSyntaxTree.ParseText(
            allBranchesActive,
            initialOptions,
            source.Path).GetCompilationUnitRoot();
        foreach (MemberDeclarationSyntax member in allBranchesRoot.DescendantNodes().OfType<MemberDeclarationSyntax>()
            .Where(IsDirectDeclaration))
        {
            DirectDeclaration declaration = CreateDeclaration(source.Path, member);
            var key = (
                declaration.Identity,
                declaration.Kind,
                declaration.Accessibility,
                declaration.Modifiers,
                declaration.SpanStart);
            declarations.TryAdd(key, declaration);
        }

        return [.. declarations.Values.OrderBy(declaration => declaration.SpanStart)];
    }

    private static string ActivateAllConditionalBranches(string content, CompilationUnitSyntax root)
    {
        char[] activated = content.ToCharArray();
        foreach (DirectiveTriviaSyntax directive in root.DescendantTrivia(descendIntoTrivia: true)
            .Select(trivia => trivia.GetStructure())
            .OfType<DirectiveTriviaSyntax>()
            .Where(directive => directive.IsKind(SyntaxKind.IfDirectiveTrivia)
                || directive.IsKind(SyntaxKind.ElifDirectiveTrivia)
                || directive.IsKind(SyntaxKind.ElseDirectiveTrivia)
                || directive.IsKind(SyntaxKind.EndIfDirectiveTrivia)))
        {
            for (int index = directive.FullSpan.Start; index < directive.FullSpan.End && index < activated.Length; index++)
            {
                if (activated[index] is not ('\r' or '\n'))
                {
                    activated[index] = ' ';
                }
            }
        }

        return new string(activated);
    }

    private static bool IsDirectDeclaration(MemberDeclarationSyntax member)
        => member is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax
            && member.Parent is CompilationUnitSyntax or BaseNamespaceDeclarationSyntax;

    private static DirectDeclaration CreateDeclaration(string path, MemberDeclarationSyntax member)
    {
        SyntaxTokenList modifiers;
        string name;
        string kind;
        int spanStart = member.SpanStart;
        switch (member)
        {
            case RecordDeclarationSyntax record:
                modifiers = record.Modifiers;
                name = record.Identifier.ValueText;
                kind = record.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword) ? "record struct" : "record";
                break;
            case ClassDeclarationSyntax @class:
                modifiers = @class.Modifiers;
                name = @class.Identifier.ValueText;
                kind = "class";
                break;
            case InterfaceDeclarationSyntax @interface:
                modifiers = @interface.Modifiers;
                name = @interface.Identifier.ValueText;
                kind = "interface";
                break;
            case StructDeclarationSyntax @struct:
                modifiers = @struct.Modifiers;
                name = @struct.Identifier.ValueText;
                kind = "struct";
                break;
            case EnumDeclarationSyntax @enum:
                modifiers = @enum.Modifiers;
                name = @enum.Identifier.ValueText;
                kind = "enum";
                break;
            case DelegateDeclarationSyntax @delegate:
                modifiers = @delegate.Modifiers;
                name = @delegate.Identifier.ValueText;
                kind = "delegate";
                break;
            default:
                throw new InvalidOperationException($"Unsupported direct declaration in {path}: {member.Kind()}");
        }

        string @namespace = string.Join(
            '.',
            member.Ancestors()
                .OfType<BaseNamespaceDeclarationSyntax>()
                .Reverse()
                .Select(declaration => declaration.Name.ToString()));
        string modifierText = string.Join(' ', modifiers.Select(modifier => modifier.ValueText));
        string accessibility = modifiers.Any(SyntaxKind.PublicKeyword) ? "public" : "internal";
        int arity = member switch
        {
            TypeDeclarationSyntax type => type.TypeParameterList?.Parameters.Count ?? 0,
            DelegateDeclarationSyntax @delegate => @delegate.TypeParameterList?.Parameters.Count ?? 0,
            _ => 0,
        };
        string metadataName = arity == 0 ? name : $"{name}`{arity}";
        return new(
            path,
            @namespace,
            name,
            string.IsNullOrEmpty(@namespace) ? metadataName : $"{@namespace}.{metadataName}",
            kind,
            accessibility,
            modifierText,
            arity,
            spanStart);
    }

    private static List<string> FindManifestUniquenessViolations(IReadOnlyList<DeclarationPin> pins)
    {
        List<string> violations = [];
        violations.AddRange(
            pins.GroupBy(pin => pin.Path, StringComparer.Ordinal)
                .Where(group => group.Count() > 1)
                .Select(group => $"duplicate manifest path {group.Key}"));
        violations.AddRange(
            pins.GroupBy(pin => pin.Identity, StringComparer.Ordinal)
                .Where(group => group.Count() > 1)
                .Select(group => $"duplicate manifest identity {group.Key}"));
        return violations;
    }

    private static DeclarationPin[] ParseDeclarationPins()
        =>
        [
            .. TargetDeclarationManifest.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim().Split('|'))
                .Select(parts => new DeclarationPin(parts[0], parts[1], parts[2], parts[3], parts[4])),
        ];

    private static string RuntimeKind(Type type)
    {
        if (type.IsEnum)
        {
            return "enum";
        }

        if (type.IsInterface)
        {
            return "interface";
        }

        return type.IsValueType ? "record struct" : type.GetMethod("<Clone>$") is not null ? "record" : "class";
    }

    private static string NormalizeOwnerName(string path)
    {
        string owner = Path.GetFileNameWithoutExtension(path);
        return owner.EndsWith(".razor", StringComparison.Ordinal) ? owner[..^".razor".Length] : owner;
    }

    private static string NamespaceFromPath(string path)
    {
        string? directory = Path.GetDirectoryName(path);
        return string.IsNullOrEmpty(directory)
            ? ShellNamespace
            : $"{ShellNamespace}.{Normalize(directory).Replace('/', '.')}";
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Hexalith.FrontComposer.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }

    private static string Normalize(string path) => path.Replace('\\', '/');

    private sealed record SourceFile(string Path, string Content);

    private sealed record DirectDeclaration(
        string Path,
        string Namespace,
        string Name,
        string Identity,
        string Kind,
        string Accessibility,
        string Modifiers,
        int Arity,
        int SpanStart);

    private sealed record DeclarationPin(
        string Path,
        string Identity,
        string Kind,
        string Accessibility,
        string Modifiers);

    private const string TargetDeclarationManifest = """
Components/Badges/OptimisticBadgeState.cs|Hexalith.FrontComposer.Shell.Components.Badges.OptimisticBadgeState|enum|public|public
Components/Badges/FcDesaturatedBadge.razor.cs|Hexalith.FrontComposer.Shell.Components.Badges.FcDesaturatedBadge|class|public|public partial
Components/DataGrid/FcColumnPrioritizer.razor.cs|Hexalith.FrontComposer.Shell.Components.DataGrid.FcColumnPrioritizer|class|public|public partial
Components/DataGrid/ColumnDescriptor.cs|Hexalith.FrontComposer.Shell.Components.DataGrid.ColumnDescriptor|record|public|public sealed
Components/DataGrid/ColumnVisibilityContext.cs|Hexalith.FrontComposer.Shell.Components.DataGrid.ColumnVisibilityContext|class|public|public sealed
Components/Home/FcHomeDirectory.razor.cs|Hexalith.FrontComposer.Shell.Components.Home.FcHomeDirectory|class|public|public partial
Components/Home/HomeCardModel.cs|Hexalith.FrontComposer.Shell.Components.Home.HomeCardModel|record|public|public sealed
Components/Home/HomeProjectionRow.cs|Hexalith.FrontComposer.Shell.Components.Home.HomeProjectionRow|record|public|public sealed
Components/Lifecycle/LifecycleTimerPhase.cs|Hexalith.FrontComposer.Shell.Components.Lifecycle.LifecycleTimerPhase|enum|public|public
Components/Lifecycle/LifecycleUiState.cs|Hexalith.FrontComposer.Shell.Components.Lifecycle.LifecycleUiState|record|public|public sealed
Extensions/FrontComposerBootstrapStage.cs|Hexalith.FrontComposer.Shell.Extensions.FrontComposerBootstrapStage|enum|internal|internal
Extensions/IFrontComposerBootstrapMarker.cs|Hexalith.FrontComposer.Shell.Extensions.IFrontComposerBootstrapMarker|interface|internal|internal
Extensions/QuickstartBootstrapMarker.cs|Hexalith.FrontComposer.Shell.Extensions.QuickstartBootstrapMarker|record|internal|internal sealed
Extensions/DomainBootstrapMarker.cs|Hexalith.FrontComposer.Shell.Extensions.DomainBootstrapMarker|record|internal|internal sealed
Extensions/EventStoreBootstrapMarker.cs|Hexalith.FrontComposer.Shell.Extensions.EventStoreBootstrapMarker|record|internal|internal sealed
Infrastructure/EventStore/EventStoreResponseClassifier.cs|Hexalith.FrontComposer.Shell.Infrastructure.EventStore.EventStoreResponseClassifier|class|public|public sealed
Infrastructure/EventStore/EventStoreCommandClassification.cs|Hexalith.FrontComposer.Shell.Infrastructure.EventStore.EventStoreCommandClassification|record|public|public sealed
Infrastructure/EventStore/EventStoreQueryClassification.cs|Hexalith.FrontComposer.Shell.Infrastructure.EventStore.EventStoreQueryClassification|record|public|public sealed
Infrastructure/EventStore/QueryClassificationOutcome.cs|Hexalith.FrontComposer.Shell.Infrastructure.EventStore.QueryClassificationOutcome|enum|public|public
Infrastructure/EventStore/IProjectionHubConnection.cs|Hexalith.FrontComposer.Shell.Infrastructure.EventStore.IProjectionHubConnection|interface|internal|internal
Infrastructure/EventStore/IProjectionHubConnectionFactory.cs|Hexalith.FrontComposer.Shell.Infrastructure.EventStore.IProjectionHubConnectionFactory|interface|internal|internal
Infrastructure/EventStore/ProjectionHubConnectionState.cs|Hexalith.FrontComposer.Shell.Infrastructure.EventStore.ProjectionHubConnectionState|enum|internal|internal
Infrastructure/EventStore/ProjectionHubConnectionStateChanged.cs|Hexalith.FrontComposer.Shell.Infrastructure.EventStore.ProjectionHubConnectionStateChanged|record|internal|internal sealed
Infrastructure/Storage/LocalStorageService.cs|Hexalith.FrontComposer.Shell.Infrastructure.Storage.LocalStorageService|class|public|public sealed
Infrastructure/Storage/PendingWrite.cs|Hexalith.FrontComposer.Shell.Infrastructure.Storage.PendingWrite|record struct|internal|internal readonly
Options/FrontComposerAuthenticationOptions.cs|Hexalith.FrontComposer.Shell.Options.FrontComposerAuthenticationOptions|class|public|public sealed
Options/FrontComposerAuthenticationProviderKind.cs|Hexalith.FrontComposer.Shell.Options.FrontComposerAuthenticationProviderKind|enum|internal|internal
Options/FrontComposerOpenIdConnectOptions.cs|Hexalith.FrontComposer.Shell.Options.FrontComposerOpenIdConnectOptions|class|public|public sealed
Options/FrontComposerSaml2Options.cs|Hexalith.FrontComposer.Shell.Options.FrontComposerSaml2Options|class|public|public sealed
Options/FrontComposerGitHubOAuthOptions.cs|Hexalith.FrontComposer.Shell.Options.FrontComposerGitHubOAuthOptions|class|public|public sealed
Options/FrontComposerCustomBrokeredOptions.cs|Hexalith.FrontComposer.Shell.Options.FrontComposerCustomBrokeredOptions|class|public|public sealed
Options/FrontComposerAuthRedirectOptions.cs|Hexalith.FrontComposer.Shell.Options.FrontComposerAuthRedirectOptions|class|public|public sealed
Options/FrontComposerAuthCookieOptions.cs|Hexalith.FrontComposer.Shell.Options.FrontComposerAuthCookieOptions|class|public|public sealed
Options/FrontComposerTokenRelayOptions.cs|Hexalith.FrontComposer.Shell.Options.FrontComposerTokenRelayOptions|class|public|public sealed
Services/Auth/FrontComposerClaimExtractor.cs|Hexalith.FrontComposer.Shell.Services.Auth.FrontComposerClaimExtractor|class|internal|internal static
Services/Auth/FrontComposerClaimExtractionResult.cs|Hexalith.FrontComposer.Shell.Services.Auth.FrontComposerClaimExtractionResult|record|internal|internal sealed
Services/Auth/FrontComposerUserTokenStore.cs|Hexalith.FrontComposer.Shell.Services.Auth.FrontComposerUserTokenStore|class|public|public sealed
Services/Auth/CircuitServicesAccessor.cs|Hexalith.FrontComposer.Shell.Services.Auth.CircuitServicesAccessor|class|public|public sealed
Services/Auth/FrontComposerCircuitServicesHandler.cs|Hexalith.FrontComposer.Shell.Services.Auth.FrontComposerCircuitServicesHandler|class|public|public sealed
Services/Auth/FrontComposerGatewayAuthorizationHandler.cs|Hexalith.FrontComposer.Shell.Services.Auth.FrontComposerGatewayAuthorizationHandler|class|public|public sealed
Services/Authorization/CommandAuthorizationDecisionKind.cs|Hexalith.FrontComposer.Shell.Services.Authorization.CommandAuthorizationDecisionKind|enum|public|public
Services/Authorization/CommandAuthorizationReason.cs|Hexalith.FrontComposer.Shell.Services.Authorization.CommandAuthorizationReason|enum|public|public
Services/Authorization/CommandAuthorizationSurface.cs|Hexalith.FrontComposer.Shell.Services.Authorization.CommandAuthorizationSurface|enum|public|public
Services/Authorization/CommandAuthorizationRequest.cs|Hexalith.FrontComposer.Shell.Services.Authorization.CommandAuthorizationRequest|record|public|public sealed
Services/Authorization/CommandAuthorizationResource.cs|Hexalith.FrontComposer.Shell.Services.Authorization.CommandAuthorizationResource|record|public|public sealed
Services/Authorization/CommandAuthorizationDecision.cs|Hexalith.FrontComposer.Shell.Services.Authorization.CommandAuthorizationDecision|record|public|public sealed
Services/Feedback/ICommandFeedbackPublisher.cs|Hexalith.FrontComposer.Shell.Services.Feedback.ICommandFeedbackPublisher|interface|public|public
Services/Feedback/CommandFeedbackWarning.cs|Hexalith.FrontComposer.Shell.Services.Feedback.CommandFeedbackWarning|record|public|public sealed
Services/IEmptyStateCtaResolver.cs|Hexalith.FrontComposer.Shell.Services.IEmptyStateCtaResolver|interface|public|public
Services/EmptyStateCta.cs|Hexalith.FrontComposer.Shell.Services.EmptyStateCta|record|public|public sealed
Services/IExpandInRowJSModule.cs|Hexalith.FrontComposer.Shell.Services.IExpandInRowJSModule|interface|public|public
Services/ExpandInRowJSModule.cs|Hexalith.FrontComposer.Shell.Services.ExpandInRowJSModule|class|public|public sealed
State/CapabilityDiscovery/CapabilityDiscoveryHydrationState.cs|Hexalith.FrontComposer.Shell.State.CapabilityDiscovery.CapabilityDiscoveryHydrationState|enum|public|public
State/CapabilityDiscovery/FrontComposerCapabilityDiscoveryState.cs|Hexalith.FrontComposer.Shell.State.CapabilityDiscovery.FrontComposerCapabilityDiscoveryState|record|public|public sealed
State/CommandPalette/PaletteResultCategory.cs|Hexalith.FrontComposer.Shell.State.CommandPalette.PaletteResultCategory|enum|public|public
State/CommandPalette/PaletteLoadState.cs|Hexalith.FrontComposer.Shell.State.CommandPalette.PaletteLoadState|enum|public|public
State/CommandPalette/PaletteResult.cs|Hexalith.FrontComposer.Shell.State.CommandPalette.PaletteResult|record|public|public sealed
State/DataGridNavigation/IProjectionPageLoader.cs|Hexalith.FrontComposer.Shell.State.DataGridNavigation.IProjectionPageLoader|interface|public|public
State/DataGridNavigation/ProjectionPageResult.cs|Hexalith.FrontComposer.Shell.State.DataGridNavigation.ProjectionPageResult|record|public|public sealed
State/DataGridNavigation/NullProjectionPageLoader.cs|Hexalith.FrontComposer.Shell.State.DataGridNavigation.NullProjectionPageLoader|class|public|public sealed
State/DataGridNavigation/LoadedPageReducers.cs|Hexalith.FrontComposer.Shell.State.DataGridNavigation.LoadedPageReducers|class|public|public sealed
State/DataGridNavigation/VirtualizationViewStateReducers.cs|Hexalith.FrontComposer.Shell.State.DataGridNavigation.VirtualizationViewStateReducers|class|public|public static
State/PendingCommands/ICommandExecutionAdmissionGate.cs|Hexalith.FrontComposer.Shell.State.PendingCommands.ICommandExecutionAdmissionGate|interface|public|public
State/PendingCommands/CommandExecutionAdmissionRequest.cs|Hexalith.FrontComposer.Shell.State.PendingCommands.CommandExecutionAdmissionRequest|record|public|public sealed
State/PendingCommands/CommandExecutionAdmissionDenialReason.cs|Hexalith.FrontComposer.Shell.State.PendingCommands.CommandExecutionAdmissionDenialReason|enum|public|public
State/PendingCommands/CommandExecutionAdmission.cs|Hexalith.FrontComposer.Shell.State.PendingCommands.CommandExecutionAdmission|class|public|public sealed
State/PendingCommands/ICommandExecutionAdmissionReleaser.cs|Hexalith.FrontComposer.Shell.State.PendingCommands.ICommandExecutionAdmissionReleaser|interface|internal|internal
State/PendingCommands/NewItemIndicatorEntry.cs|Hexalith.FrontComposer.Shell.State.PendingCommands.NewItemIndicatorEntry|record|public|public sealed
State/PendingCommands/INewItemIndicatorStateService.cs|Hexalith.FrontComposer.Shell.State.PendingCommands.INewItemIndicatorStateService|interface|public|public
State/PendingCommands/NewItemIndicatorStateService.cs|Hexalith.FrontComposer.Shell.State.PendingCommands.NewItemIndicatorStateService|class|public|public sealed
State/PendingCommands/PendingCommandRegistration.cs|Hexalith.FrontComposer.Shell.State.PendingCommands.PendingCommandRegistration|record|public|public sealed
State/PendingCommands/PendingCommandStatus.cs|Hexalith.FrontComposer.Shell.State.PendingCommands.PendingCommandStatus|enum|public|public
State/PendingCommands/PendingCommandTerminalOutcome.cs|Hexalith.FrontComposer.Shell.State.PendingCommands.PendingCommandTerminalOutcome|enum|public|public
State/PendingCommands/PendingCommandRegistrationStatus.cs|Hexalith.FrontComposer.Shell.State.PendingCommands.PendingCommandRegistrationStatus|enum|public|public
State/PendingCommands/PendingCommandResolutionStatus.cs|Hexalith.FrontComposer.Shell.State.PendingCommands.PendingCommandResolutionStatus|enum|public|public
State/PendingCommands/PendingCommandEntry.cs|Hexalith.FrontComposer.Shell.State.PendingCommands.PendingCommandEntry|record|public|public sealed
State/PendingCommands/PendingCommandRegistrationResult.cs|Hexalith.FrontComposer.Shell.State.PendingCommands.PendingCommandRegistrationResult|record|public|public sealed
State/PendingCommands/PendingCommandTerminalObservation.cs|Hexalith.FrontComposer.Shell.State.PendingCommands.PendingCommandTerminalObservation|record|public|public sealed
State/PendingCommands/PendingCommandResolutionResult.cs|Hexalith.FrontComposer.Shell.State.PendingCommands.PendingCommandResolutionResult|record|public|public sealed
State/PendingCommands/PendingCommandOutcomeSource.cs|Hexalith.FrontComposer.Shell.State.PendingCommands.PendingCommandOutcomeSource|enum|public|public
State/PendingCommands/PendingCommandOutcomeResolutionStatus.cs|Hexalith.FrontComposer.Shell.State.PendingCommands.PendingCommandOutcomeResolutionStatus|enum|public|public
State/PendingCommands/PendingCommandOutcomeObservation.cs|Hexalith.FrontComposer.Shell.State.PendingCommands.PendingCommandOutcomeObservation|record|public|public sealed
State/PendingCommands/PendingCommandOutcomeResolutionResult.cs|Hexalith.FrontComposer.Shell.State.PendingCommands.PendingCommandOutcomeResolutionResult|record|public|public sealed
State/PendingCommands/IPendingCommandOutcomeResolver.cs|Hexalith.FrontComposer.Shell.State.PendingCommands.IPendingCommandOutcomeResolver|interface|public|public
State/PendingCommands/PendingCommandOutcomeResolver.cs|Hexalith.FrontComposer.Shell.State.PendingCommands.PendingCommandOutcomeResolver|class|public|public sealed
State/PendingCommands/IPendingCommandStatusQuery.cs|Hexalith.FrontComposer.Shell.State.PendingCommands.IPendingCommandStatusQuery|interface|public|public
State/PendingCommands/NullPendingCommandStatusQuery.cs|Hexalith.FrontComposer.Shell.State.PendingCommands.NullPendingCommandStatusQuery|class|public|public sealed
State/PendingCommands/IPendingCommandPollingCoordinator.cs|Hexalith.FrontComposer.Shell.State.PendingCommands.IPendingCommandPollingCoordinator|interface|public|public
State/PendingCommands/PendingCommandPollingCoordinator.cs|Hexalith.FrontComposer.Shell.State.PendingCommands.PendingCommandPollingCoordinator|class|public|public sealed
State/ProjectionConnection/ProjectionConnectionStatus.cs|Hexalith.FrontComposer.Shell.State.ProjectionConnection.ProjectionConnectionStatus|enum|public|public
State/ProjectionConnection/ProjectionConnectionSnapshot.cs|Hexalith.FrontComposer.Shell.State.ProjectionConnection.ProjectionConnectionSnapshot|record|public|public sealed
State/ProjectionConnection/ProjectionConnectionTransition.cs|Hexalith.FrontComposer.Shell.State.ProjectionConnection.ProjectionConnectionTransition|record|public|public sealed
State/ProjectionConnection/IProjectionConnectionState.cs|Hexalith.FrontComposer.Shell.State.ProjectionConnection.IProjectionConnectionState|interface|public|public
State/ProjectionConnection/ProjectionConnectionStateService.cs|Hexalith.FrontComposer.Shell.State.ProjectionConnection.ProjectionConnectionStateService|class|public|public sealed
State/ProjectionConnection/ProjectionFallbackLane.cs|Hexalith.FrontComposer.Shell.State.ProjectionConnection.ProjectionFallbackLane|record|public|public sealed
State/ProjectionConnection/ProjectionFallbackLaneRefreshOutcome.cs|Hexalith.FrontComposer.Shell.State.ProjectionConnection.ProjectionFallbackLaneRefreshOutcome|enum|public|public
State/ProjectionConnection/ProjectionFallbackGroupKey.cs|Hexalith.FrontComposer.Shell.State.ProjectionConnection.ProjectionFallbackGroupKey|record struct|public|public readonly
State/ProjectionConnection/IProjectionFallbackRefreshScheduler.cs|Hexalith.FrontComposer.Shell.State.ProjectionConnection.IProjectionFallbackRefreshScheduler|interface|public|public
State/ProjectionConnection/ProjectionReconciliationRefreshResult.cs|Hexalith.FrontComposer.Shell.State.ProjectionConnection.ProjectionReconciliationRefreshResult|record|public|public sealed
State/ReconnectionReconciliation/ReconciliationSweepState.cs|Hexalith.FrontComposer.Shell.State.ReconnectionReconciliation.ReconciliationSweepState|record|public|public sealed
State/ReconnectionReconciliation/ReconciliationSweepMarker.cs|Hexalith.FrontComposer.Shell.State.ReconnectionReconciliation.ReconciliationSweepMarker|record|public|public sealed
State/ReconnectionReconciliation/MarkReconciliationSweepAction.cs|Hexalith.FrontComposer.Shell.State.ReconnectionReconciliation.MarkReconciliationSweepAction|record|public|public sealed
State/ReconnectionReconciliation/ClearExpiredReconciliationSweepsAction.cs|Hexalith.FrontComposer.Shell.State.ReconnectionReconciliation.ClearExpiredReconciliationSweepsAction|record|public|public sealed
State/ReconnectionReconciliation/ReconciliationSweepFeature.cs|Hexalith.FrontComposer.Shell.State.ReconnectionReconciliation.ReconciliationSweepFeature|class|public|public sealed
State/ReconnectionReconciliation/ReconciliationSweepReducers.cs|Hexalith.FrontComposer.Shell.State.ReconnectionReconciliation.ReconciliationSweepReducers|class|public|public static
State/ReconnectionReconciliation/IReconnectionReconciliationCoordinator.cs|Hexalith.FrontComposer.Shell.State.ReconnectionReconciliation.IReconnectionReconciliationCoordinator|interface|public|public
State/ReconnectionReconciliation/ReconnectionReconciliationCoordinator.cs|Hexalith.FrontComposer.Shell.State.ReconnectionReconciliation.ReconnectionReconciliationCoordinator|class|public|public sealed
State/ReconnectionReconciliation/ReconnectionReconciliationStatus.cs|Hexalith.FrontComposer.Shell.State.ReconnectionReconciliation.ReconnectionReconciliationStatus|enum|public|public
State/ReconnectionReconciliation/ReconnectionReconciliationSnapshot.cs|Hexalith.FrontComposer.Shell.State.ReconnectionReconciliation.ReconnectionReconciliationSnapshot|record|public|public sealed
State/ReconnectionReconciliation/IReconnectionReconciliationState.cs|Hexalith.FrontComposer.Shell.State.ReconnectionReconciliation.IReconnectionReconciliationState|interface|public|public
State/ReconnectionReconciliation/ReconnectionReconciliationStateService.cs|Hexalith.FrontComposer.Shell.State.ReconnectionReconciliation.ReconnectionReconciliationStateService|class|public|public sealed
""";
}
