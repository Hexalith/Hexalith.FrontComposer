using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Tests.Parsing.TestFixtures;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Parsing;

public class CancellationTests {
    [Fact]
    public void Parse_CancelledToken_ReturnsEmptyResult() {
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(TestSources.BasicProjection);
        INamedTypeSymbol typeSymbol = compilation.GetTypeByMetadataName("TestDomain.CounterProjection")!;
        SyntaxNode targetNode = typeSymbol.DeclaringSyntaxReferences[0].GetSyntax(TestContext.Current.CancellationToken);

        using CancellationTokenSource cts = new();
        cts.Cancel();

        ParseResult? result = null;

        _ = Should.NotThrow(() => result = AttributeParser.Parse(typeSymbol, targetNode, cts.Token));
        _ = result.ShouldNotBeNull();
        result.Model.ShouldBeNull();
        result.Diagnostics.Count.ShouldBe(0);
    }
}
