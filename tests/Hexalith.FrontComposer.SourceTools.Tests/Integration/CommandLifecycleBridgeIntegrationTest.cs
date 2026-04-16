using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Integration;

/// <summary>Story 2-3 Task 11.5 — structural integration test for the emitted lifecycle bridge.</summary>
public class CommandLifecycleBridgeIntegrationTest {
    [Fact]
    public void BridgeEmitter_EmitsSixSubscriptions_EachDispatchingToTransition() {
        const string source = """
            using Hexalith.FrontComposer.Contracts.Attributes;

            namespace TestDomain;

            [Command]
            public class PlaceOrderCommand {
                public string MessageId { get; set; } = string.Empty;
                public int Quantity { get; set; }
            }
            """;

        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source);
        FrontComposerGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation, ct);
        GeneratorDriverRunResult result = driver.GetRunResult();

        SyntaxTree bridgeTree = result.GeneratedTrees
            .Single(t => System.IO.Path.GetFileName(t.FilePath).EndsWith("CommandLifecycleBridge.g.cs", StringComparison.Ordinal));
        string bridgeSource = bridgeTree.GetText(ct).ToString();

        int subscribeCount = System.Text.RegularExpressions.Regex.Matches(bridgeSource, "SubscribeToAction<").Count;
        subscribeCount.ShouldBe(6, "bridge must subscribe to exactly the 6 lifecycle actions");

        int transitionCallCount = System.Text.RegularExpressions.Regex.Matches(bridgeSource, "_service\\.Transition\\(").Count;
        transitionCallCount.ShouldBe(6, "each subscription must forward via _service.Transition");
    }
}
