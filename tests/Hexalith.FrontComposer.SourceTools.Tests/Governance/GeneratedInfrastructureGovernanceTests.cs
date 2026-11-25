using System.Text.Json;

using Hexalith.FrontComposer.SourceTools.Tests.Integration;
using Hexalith.FrontComposer.SourceTools.Tests.Parsing.TestFixtures;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Governance;

[Trait("Category", "Governance")]
public sealed class GeneratedInfrastructureGovernanceTests
{
    private static readonly Dictionary<string, string> NamespaceRules = LoadNamespaceRules();

    private static readonly string[][] GeneratedSymbolSets =
        [[], ["DEBUG"], ["NET11_0_OR_GREATER"], ["DEBUG", "NET11_0_OR_GREATER"]];

    [Theory]
    [InlineData(TestSources.CounterProjectionSource, "Counter.Domain.CounterProjection.g.razor.cs", null)]
    [InlineData(CommandTestSources.MultiFieldCommand, "TestDomain.PlaceOrderCommand.CommandPage.g.razor.cs", null)]
    [InlineData(ProjectionTemplateMarkerTests.ValidProjection, "__FrontComposerProjectionTemplatesRegistration.g.cs", ProjectionTemplateMarkerTests.ValidTemplate)]
    public void GeneratedSourcesDoNotReferenceProviderNamespaces(string source, string expectedFile, string? template)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(template is null ? [source] : [source, template]);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new FrontComposerGenerator()).RunGenerators(compilation, cancellationToken);
        GeneratorDriverRunResult result = driver.GetRunResult();

        result.Diagnostics.Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        SyntaxTree expectedOutput = result.GeneratedTrees.First(tree => Path.GetFileName(tree.FilePath) == expectedFile);
        if (template is not null)
        {
            expectedOutput.GetText(cancellationToken).ToString().ShouldContain("typeof(global::TestDomain.CounterCardTemplate)");
        }

        foreach (string[] symbols in GeneratedSymbolSets)
        {
            CSharpParseOptions parseOptions = ((CSharpParseOptions)compilation.SyntaxTrees.First().Options).WithPreprocessorSymbols(symbols);
            SyntaxTree[] generated = [.. result.GeneratedTrees.Select(tree =>
                CSharpSyntaxTree.ParseText(tree.GetText(cancellationToken), parseOptions, tree.FilePath, cancellationToken))];
            CSharpCompilation consumer = compilation.AddSyntaxTrees(generated);
            foreach (SyntaxTree tree in generated)
            {
                FindForbiddenNamespaces(consumer.GetSemanticModel(tree), cancellationToken)
                    .ShouldBeEmpty($"Generated file {Path.GetFileName(tree.FilePath)} with symbols [{string.Join(", ", symbols)}] must route through EventStore contracts or clients.");
            }
        }
    }

    [Theory]
    [InlineData("global using Npgsql;", "Npgsql")]
    [InlineData("global using @Npgsql;", "Npgsql")]
    [InlineData("using Redis = global::StackExchange.Redis;", "StackExchange.Redis")]
    [InlineData("using static Microsoft.AspNetCore.SignalR.Client.HubConnectionBuilderExtensions;", "Microsoft.AspNetCore.SignalR.Client")]
    [InlineData("class Sample { global::Dapr.Client.DaprClient? Client; }", "Dapr")]
    [InlineData("extern alias Provider; class Sample { Provider::Npgsql.NpgsqlConnection? Client; }", "Npgsql")]
    [InlineData("class Sample { object Connect() => StackExchange /* Redis SDK */.Redis.ConnectionMultiplexer.Connect(\"test\"); }", "StackExchange.Redis")]
    public void GeneratedNamespaceScanDetectsImportsAndQualifiedReferences(string source, string expectedNamespace)
    {
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source);
        SemanticModel model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
        FindForbiddenNamespaces(model, TestContext.Current.CancellationToken).ShouldHaveSingleItem().ShouldBe(expectedNamespace);
    }

    [Theory]
    [InlineData("global using Azure.Storage;", "BlobClient")]
    [InlineData("global using Storage = Azure.Storage;", "Storage.BlobClient")]
    public void GeneratedNamespaceScanFindsProviderThroughConsumerGlobalImport(string import, string type)
    {
        CSharpCompilation compilation = CompilationHelper.CreateCompilation([
            import + " namespace Azure.Storage { public class BlobClient {} }",
            $"class Sample {{ {type}? Client; }}",
        ]);
        SemanticModel model = compilation.GetSemanticModel(compilation.SyntaxTrees.Last());
        FindForbiddenNamespaces(model, TestContext.Current.CancellationToken).ShouldHaveSingleItem().ShouldBe("Azure.Storage");
    }

    [Fact]
    public void GeneratedNamespaceScanFindsForbiddenReferenceWhenConditionalIsEnabled()
    {
        const string source = """
            #if DEBUG && NET11_0_OR_GREATER
            using Dapr.Client;
            #endif
            """;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source);
        SyntaxTree original = compilation.SyntaxTrees.Single();
        SyntaxTree enabled = CSharpSyntaxTree.ParseText(source,
            ((CSharpParseOptions)original.Options).WithPreprocessorSymbols("DEBUG", "NET11_0_OR_GREATER"),
            cancellationToken: cancellationToken);

        FindForbiddenNamespaces(compilation.GetSemanticModel(original), cancellationToken).ShouldBeEmpty();
        FindForbiddenNamespaces(compilation.ReplaceSyntaxTree(original, enabled).GetSemanticModel(enabled), cancellationToken)
            .ShouldHaveSingleItem().ShouldBe("Dapr");
    }

    [Fact]
    public void GeneratedNamespaceScanIgnoresCommentsStringsAndSimilarNames()
    {
        const string source = """
            using Daprish.Client;
            using Acme.Dapr.Client;
            namespace Azure.Storage.Internal;
            // using StackExchange.Redis;
            class Sample
            {
                string Message => "global::Dapr.Client.DaprClient";
                string Format() => ("fixture::Npgsql.NpgsqlConnection").ToString();
            }
            """;

        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source);
        SemanticModel model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
        FindForbiddenNamespaces(model, TestContext.Current.CancellationToken).ShouldBeEmpty();
    }

    [Fact]
    public void GeneratedNamespaceScanIgnoresLocalsNamedLikeProviderRoots()
    {
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(
            "class Sample { string? Format(int Dapr) => Dapr.ToString(); }");
        SemanticModel model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
        FindForbiddenNamespaces(model, TestContext.Current.CancellationToken).ShouldBeEmpty();
    }

    private static HashSet<string> FindForbiddenNamespaces(SemanticModel model, CancellationToken cancellationToken)
    {
        HashSet<string> violations = new(StringComparer.Ordinal);
        foreach (SyntaxNode node in model.SyntaxTree.GetRoot(cancellationToken).DescendantNodes())
        {
            if (node is not (NameSyntax or MemberAccessExpressionSyntax)
                || node.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().Any(ns => ns.Name.Span.Contains(node.Span)))
            {
                continue;
            }

            ISymbol? symbol = model.GetSymbolInfo(node, cancellationToken).Symbol;
            INamespaceSymbol? containingNamespace = symbol as INamespaceSymbol ?? symbol?.ContainingNamespace;
            string name = symbol is not null ? containingNamespace?.ToDisplayString() ?? string.Empty : UnresolvedSpelling(node);
            foreach (string forbidden in NamespaceRules.Keys)
            {
                if (string.Equals(name, forbidden, StringComparison.Ordinal)
                    || name.StartsWith(forbidden + ".", StringComparison.Ordinal))
                {
                    _ = violations.Add(forbidden);
                }
            }
        }

        return violations;
    }

    private static Dictionary<string, string> LoadNamespaceRules()
    {
        DirectoryInfo? root = new(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "Hexalith.FrontComposer.slnx")))
        {
            root = root.Parent;
        }

        if (root is null)
        {
            throw new InvalidOperationException("Could not locate the repository root for governance fixtures.");
        }

        string fixture = Path.Combine(root.FullName, "tests", "contract-fixtures", "infrastructure-namespace-rules.json");
        return JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(fixture)) is { Count: > 0 } rules
            ? rules
            : throw new InvalidDataException("The shared infrastructure namespace policy is empty.");
    }

    private static string UnresolvedSpelling(SyntaxNode reference)
    {
        if (reference is SimpleNameSyntax && reference.Parent is not UsingDirectiveSyntax)
        {
            return string.Empty;
        }

        string name = string.Concat(reference.DescendantTokens().Select(
            static token => token.IsKind(SyntaxKind.IdentifierToken) ? token.ValueText : token.Text));
        SyntaxToken first = reference.GetFirstToken();
        SyntaxToken separator = first.GetNextToken();
        if ((first.IsKind(SyntaxKind.IdentifierToken) || first.IsKind(SyntaxKind.GlobalKeyword))
            && separator.IsKind(SyntaxKind.ColonColonToken)
            && reference.Span.Contains(separator.Span))
        {
            name = name[(first.ValueText.Length + 2)..];
        }

        return name;
    }
}
