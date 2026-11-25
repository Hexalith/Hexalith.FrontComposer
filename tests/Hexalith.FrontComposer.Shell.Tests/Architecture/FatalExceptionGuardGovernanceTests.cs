using System.Text.RegularExpressions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Architecture;

[Trait("Category", "Governance")]
public sealed partial class FatalExceptionGuardGovernanceTests {
    // Story 13.4 adds subscriber, page-load, refresh-scheduler and accepted-cleanup shared fatal filters;
    // Story 13.5 adds the fresh-row ledger's fail-closed scope read.
    // .NET 11 account-state observation adds its shared fatal filter for asynchronous authentication failures.
    // DW-900 adds a guard for authorization failures during ranked command selection.
    // DW-925 adds a guard for failing identity accessors.
    // DW-994 routes location callback failures through the component error boundary.
    private const int ExpectedCatchFilterCount = 84;
    private const string ExceptionGuardPath = "Services/ExceptionGuard.cs";
    private static readonly HashSet<string> FatalTypeNames = new(StringComparer.Ordinal) {
        nameof(OutOfMemoryException),
        nameof(StackOverflowException),
        nameof(ThreadAbortException),
        nameof(AccessViolationException),
    };

    // Existing handlers are pinned per method. New broad handlers must filter or rethrow.
    private static readonly Dictionary<string, int> ExistingUnfilteredHandlerCounts = new(StringComparer.Ordinal)
    {
        ["Badges/BadgeCountService.cs:FetchOneAsync"] = 1,
        ["Badges/BadgeCountService.cs:OnProjectionChanged"] = 1,
        ["Components/Diagnostics/FcCustomizationDiagnosticPanel.razor:OnAfterRenderAsync"] = 1,
        ["Components/Home/FcHomeDirectory.razor.cs:OnParametersSetAsync"] = 1,
        ["Components/Layout/FcAccountMenu.razor.cs:ObserveAuthenticationStateChangeAsync"] = 1,
        ["Components/Layout/FcLayoutBreakpointWatcher.razor.cs:OnAfterRenderAsync"] = 1,
        ["Components/Layout/FrontComposerShell.razor.cs:IsBoundedContextRegistered"] = 1,
        ["Components/Rendering/FcAuthorizedCommandRegion.razor.cs:RefreshAsync"] = 1,
        ["Components/Rendering/FcProjectionSubtitle.razor.cs:Dispose"] = 1,
        ["Components/Rendering/FcProjectionSubtitle.razor.cs:OnInitialized"] = 1,
        ["Infrastructure/EventStore/EventStoreQueryClient.cs:PersistCacheEntryAsync"] = 1,
        ["Infrastructure/EventStore/EventStoreQueryClient.cs:TryInvalidateSchemaMismatchAsync"] = 1,
        ["Infrastructure/EventStore/EventStoreResponseClassifier.cs:DrainBodyAsync"] = 1,
        ["Infrastructure/EventStore/ProjectionSubscriptionService.cs:DisposeAsync"] = 1,
        ["Infrastructure/ProjectionConnection/ProjectionFallbackPollingDriver.cs:DisposeAsync"] = 1,
        ["Infrastructure/ProjectionConnection/ProjectionFallbackPollingDriver.cs:DisposeRegistration"] = 1,
        ["Infrastructure/Storage/LocalStorageService.cs:DrainLoopAsync"] = 2,
        ["Services/DevMode/RazorEmitter.cs:EmitStarterTemplate"] = 1,
        ["Services/Feedback/CommandFeedbackPublisher.cs:PublishWarning"] = 1,
        ["Services/Feedback/FcOptimisticReconciler.cs:ExecuteAsync"] = 2,
        ["Services/Feedback/FcOptimisticReconciler.cs:ExecuteAsync/ObserveFallbackAsync"] = 1,
        ["Services/Feedback/FcOptimisticReconciler.cs:ExecuteAsync/ObserveRetriesAsync"] = 1,
        ["Services/Feedback/FcOptimisticReconciler.cs:ExecuteAsync/TryReconcileAsync"] = 2,
        ["Services/InlinePopoverRegistry.cs:OpenAsync"] = 1,
        ["Services/ScopeBoundaryService.cs:OnAuthenticationStateChanged"] = 1,
        ["Shortcuts/ShortcutService.cs:TryInvokeBindingAsync"] = 1,
        ["State/CapabilityDiscovery/CapabilityDiscoveryEffects.cs:HandleCapabilityVisited"] = 1,
        ["State/CapabilityDiscovery/CapabilityDiscoveryEffects.cs:HydrateSeenSetAsync"] = 1,
        ["State/DataGridNavigation/DataGridNavigationEffects.cs:HandleCaptureGridState"] = 1,
        ["State/DataGridNavigation/DataGridNavigationEffects.cs:HandleClearGridState"] = 1,
        ["State/DataGridNavigation/DataGridNavigationEffects.cs:HandleRestoreGridState"] = 1,
        ["State/DataGridNavigation/DataGridNavigationEffects.cs:HydrateAsync"] = 5,
        ["State/DataGridNavigation/DataGridNavigationEffects.cs:ResolveRegisteredBoundedContexts"] = 1,
        ["State/Density/DensityEffects.cs:HydrateAsync"] = 1,
        ["State/Density/DensityEffects.cs:PersistAsync"] = 1,
        ["State/ETagCache/ETagCacheService.cs:EvictIfOverCapAsync"] = 1,
        ["State/ETagCache/ETagCacheService.cs:RemoveAsync"] = 1,
        ["State/ETagCache/ETagCacheService.cs:RemoveByProjectionTypeCoreAsync"] = 1,
        ["State/ETagCache/ETagCacheService.cs:SetAsync"] = 1,
        ["State/ETagCache/ETagCacheService.cs:TryGetAsync"] = 2,
        ["State/ETagCache/ETagCacheService.cs:TrySeedPersistedLruAsync"] = 2,
        ["State/Navigation/NavigationEffects.cs:HydrateAsync"] = 1,
        ["State/Navigation/NavigationEffects.cs:IsUnregisteredBoundedContext"] = 1,
        ["State/Navigation/NavigationEffects.cs:WriteBlobAsync"] = 1,
        ["State/PendingCommands/PendingCommandOutcomeResolver.cs:ApplyObservation"] = 2,
        ["State/PendingCommands/PendingCommandOutcomeResolver.cs:AssociateAccepted"] = 2,
        ["State/PendingCommands/PendingCommandOutcomeResolver.cs:TryGetCommittedRegistration"] = 1,
        ["State/PendingCommands/PendingCommandOutcomeResolver.cs:TryGetCurrentScope"] = 1,
        ["State/PendingCommands/PendingCommandOutcomeResolver.cs:TryPublishIndicator"] = 1,
        ["State/PendingCommands/PendingCommandOutcomeResolver.cs:TryResolveObservedAt"] = 1,
        ["State/PendingCommands/PendingCommandStateService.cs:EnforceScopeBoundary"] = 1,
        ["State/PendingCommands/PendingCommandStateService.cs:NotifyChanged"] = 1,
        ["State/PendingCommands/PendingCommandStateService.cs:TryDispatchCurrentTerminalLifecycle"] = 1,
        ["State/Theme/ThemeEffects.cs:HydrateAsync"] = 1,
    };

    [Fact]
    public void ShellSources_UseSingleFatalExceptionGuardTaxonomy() {
        SourceFile[] sources = LoadSources(LocateShellRoot());

        List<string> violations = FindViolations(sources, enforceProductionShape: true);

        violations.ShouldBeEmpty(
            "Fatal catch filters must delegate to the single Services/ExceptionGuard helper, with no "
            + "local classifiers, ad-hoc fatal-type lists, or new unfiltered catch-all handlers that swallow exceptions. Violations: "
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

    [Theory]
    [InlineData("catch (Exception)", "", true)]
    [InlineData("catch (System.Exception ex)", "Log(ex);", true)]
    [InlineData("catch (global :: System . Exception)", "", true)]
    [InlineData("catch", "", true)]
    [InlineData("catch (ExceptionAlias)", "", true)]
    [InlineData("catch (IOException)", "", false)]
    [InlineData("catch (Exception ex) when (!ExceptionGuard.IsFatal(ex))", "", false)]
    [InlineData("catch", "Cleanup(); throw;", false)]
    [InlineData("catch (Exception)", "if (skip) return; throw;", true)]
    [InlineData("catch", "if (skip) throw new InvalidOperationException(); throw;", true)]
    [InlineData("catch", "while (skip) { break; } throw;", false)]
    public void FatalExceptionGuard_SyntheticCatch_ReportsOnlyUnguardedSwallowing(
        string catchClause,
        string body,
        bool expectedViolation)
    {
        SourceFile[] sources = [new("Synthetic.cs", $$"""
            using ExceptionAlias = System.Exception;
            internal sealed class Synthetic {
                internal void Invoke(bool skip) { try { } {{catchClause}} { {{body}} } }
            }
            """)];

        FindViolations(sources, enforceProductionShape: false).Count.ShouldBe(expectedViolation ? 1 : 0);
    }

    [Fact]
    public void FatalExceptionGuard_RazorCodeBlock_ReportsUnguardedCatch()
    {
        const string Content = """
            <div>Specimen</div>
            @code {
                private void Invoke() { try { } catch { } }
            }
            """;
        SourceFile[] sources = [new("Synthetic.razor", Content)];

        FindViolations(sources, enforceProductionShape: false).ShouldHaveSingleItem()
            .ShouldStartWith("Synthetic.razor:Invoke:");
    }

    private static List<string> FindViolations(
        IEnumerable<SourceFile> sources,
        bool enforceProductionShape) {
        List<string> violations = [];
        List<(string Path, string Namespace)> guardDeclarations = [];
        int guardFilterInvocations = 0;
        Dictionary<string, int> unfilteredHandlerCounts = new(StringComparer.Ordinal);

        SyntaxTree[] trees = [.. sources.Select(ParseSource)];
        CSharpCompilation compilation = CSharpCompilation.Create(
            "ShellFatalExceptionGovernance",
            [.. trees, CSharpSyntaxTree.ParseText("global using System;")],
            [MetadataReference.CreateFromFile(typeof(Exception).Assembly.Location)]);
        INamedTypeSymbol exceptionType = compilation.GetTypeByMetadataName("System.Exception")!;

        foreach (SyntaxTree tree in trees) {
            string path = tree.FilePath;
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
            SemanticModel semanticModel = compilation.GetSemanticModel(tree);
            foreach (ClassDeclarationSyntax declaration in root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .Where(declaration => string.Equals(
                    declaration.Identifier.ValueText,
                    "ExceptionGuard",
                    StringComparison.Ordinal))) {
                string guardNamespace = declaration.Ancestors()
                    .OfType<BaseNamespaceDeclarationSyntax>()
                    .FirstOrDefault()?.Name.ToString() ?? string.Empty;
                guardDeclarations.Add((path, guardNamespace));
            }

            foreach (MethodDeclarationSyntax declaration in root.DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Where(declaration => string.Equals(
                    declaration.Identifier.ValueText,
                    "IsRecoverable",
                    StringComparison.Ordinal))) {
                violations.Add($"{path}: local IsRecoverable declaration");
            }

            foreach (LocalFunctionStatementSyntax declaration in root.DescendantNodes()
                .OfType<LocalFunctionStatementSyntax>()
                .Where(declaration => string.Equals(
                    declaration.Identifier.ValueText,
                    "IsRecoverable",
                    StringComparison.Ordinal))) {
                violations.Add($"{path}: local IsRecoverable declaration");
            }

            foreach (CatchClauseSyntax catchClause in root.DescendantNodes().OfType<CatchClauseSyntax>()) {
                CatchFilterClauseSyntax? filter = catchClause.Filter;
                if (filter is null) {
                    if (IsUnguardedSwallow(catchClause, semanticModel, exceptionType)) {
                        string methodPath = string.Join('/', catchClause.Ancestors().Reverse().Select(node => node switch
                        {
                            MethodDeclarationSyntax method => method.Identifier.ValueText,
                            LocalFunctionStatementSyntax function => function.Identifier.ValueText,
                            AnonymousFunctionExpressionSyntax => "(anonymous function)",
                            _ => null,
                        }).Where(name => name is not null));
                        string location = $"{path}:{methodPath}";
                        int count = unfilteredHandlerCounts.GetValueOrDefault(location) + 1;
                        unfilteredHandlerCounts[location] = count;
                        int allowed = ExistingUnfilteredHandlerCounts.GetValueOrDefault(location);
                        string? method = catchClause.Ancestors().OfType<MethodDeclarationSyntax>()
                            .FirstOrDefault()?.Identifier.ValueText;
                        if (count > allowed || (allowed > 0 && root.DescendantNodes().OfType<MethodDeclarationSyntax>()
                            .Count(declaration => declaration.Identifier.ValueText == method) != 1)) {
                            violations.Add($"{location}: unfiltered catch-all can swallow fatal exceptions");
                        }
                    }

                    continue;
                }

                if (filter.FilterExpression.DescendantNodesAndSelf()
                    .OfType<IdentifierNameSyntax>()
                    .Any(identifier => FatalTypeNames.Contains(identifier.Identifier.ValueText))) {
                    violations.Add($"{path}: catch filter contains an ad-hoc fatal taxonomy");
                }

                guardFilterInvocations += filter.FilterExpression.DescendantNodesAndSelf()
                    .OfType<InvocationExpressionSyntax>()
                    .Count(IsExceptionGuardInvocation);
            }
        }

        if (enforceProductionShape) {
            foreach ((string location, int allowed) in ExistingUnfilteredHandlerCounts) {
                if (unfilteredHandlerCounts.GetValueOrDefault(location) < allowed) {
                    violations.Add($"{location}: remove the unused unfiltered catch-all allowance");
                }
            }

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

    private static bool IsUnguardedSwallow(CatchClauseSyntax clause, SemanticModel model, INamedTypeSymbol exceptionType)
    {
        if (clause.Declaration is { Type: var type }
            && !SymbolEqualityComparer.Default.Equals(model.GetTypeInfo(type).Type, exceptionType))
        {
            return false;
        }

        // Cleanup handlers can catch broadly when they rethrow the original exception.
        ControlFlowAnalysis? flow = model.AnalyzeControlFlow(clause.Block);
        return clause.Block.Statements.LastOrDefault() is not ThrowStatementSyntax { Expression: null }
            || flow is null || !flow.Succeeded || flow.ExitPoints.Length > 0
            || clause.Block.DescendantNodes(node => node is not (LocalFunctionStatementSyntax or AnonymousFunctionExpressionSyntax))
                .Any(node => node is ThrowStatementSyntax { Expression: not null });
    }

    private static SyntaxTree ParseSource(SourceFile source)
    {
        if (!source.Path.EndsWith(".razor", StringComparison.Ordinal))
        {
            return CSharpSyntaxTree.ParseText(source.Content, path: source.Path);
        }

        // Parse component member blocks as C# while leaving Razor markup to the Razor compiler.
        IEnumerable<string> blocks = RazorCodeBlock().Matches(source.Content).Select(match =>
        {
            int brace = match.Index + match.Length - 1;
            MemberDeclarationSyntax? members = SyntaxFactory.ParseMemberDeclaration(
                "partial class Component " + source.Content[brace..], consumeFullText: false);
            return members?.ToFullString() ?? string.Empty;
        });
        return CSharpSyntaxTree.ParseText(string.Join('\n', blocks), path: source.Path);
    }

    [GeneratedRegex(@"(?m)^\s*@code\s*\{")]
    private static partial Regex RazorCodeBlock();

    private static bool IsExceptionGuardInvocation(InvocationExpressionSyntax invocation)
        => invocation.Expression is MemberAccessExpressionSyntax memberAccess
            && string.Equals(memberAccess.Name.Identifier.ValueText, "IsFatal", StringComparison.Ordinal)
            && memberAccess.Expression.ToString().EndsWith("ExceptionGuard", StringComparison.Ordinal);

    private static SourceFile[] LoadSources(string shellRoot) => [
        .. Directory.EnumerateFiles(shellRoot, "*.cs", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(shellRoot, "*.razor", SearchOption.AllDirectories))
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
