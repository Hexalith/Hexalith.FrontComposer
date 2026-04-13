namespace Hexalith.FrontComposer.SourceTools.Tests.Parsing;

using System;
using System.Threading;

using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Tests.Parsing.TestFixtures;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Shouldly;

using Xunit;

public class CancellationTests
{
    [Fact]
    public void Parse_CancelledToken_ReturnsEmptyResult()
    {
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(TestSources.BasicProjection);
        INamedTypeSymbol typeSymbol = compilation.GetTypeByMetadataName("TestDomain.CounterProjection")!;
        SyntaxNode targetNode = typeSymbol.DeclaringSyntaxReferences[0].GetSyntax(TestContext.Current.CancellationToken);

        using CancellationTokenSource cts = new();
        cts.Cancel();

        ParseResult? result = null;

        Should.NotThrow(() => result = AttributeParser.Parse(typeSymbol, targetNode, cts.Token));
        result.ShouldNotBeNull();
        result.Model.ShouldBeNull();
        result.Diagnostics.Count.ShouldBe(0);
    }
}
