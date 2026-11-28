using System.Globalization;
using System.Runtime.Loader;
using System.Text;

using Hexalith.FrontComposer.SourceTools.Emitters;
using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Mutation;

/// <summary>Checks the identities shared by nested projections and their generated consumers.</summary>
[Trait("Category", "MutationErrorRegression")]
public sealed class NestedProjectionMutationTests
{
    // A reserved root keeps the negated loop's control finite for nested fixtures.
    private const string OccupiedRoot = "namespace FrontComposerGeneratedNestedProjections; public class Existing {}";

    /// <summary>Rejects a nonpublic ancestor and preserves the neighboring public projection.</summary>
    [Fact]
    public void NonPublicOuterAncestorRejectsOnlyItsProjection()
    {
        const string source = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace Fixtures;
            internal partial class HiddenOuter
            {
                public partial class Middle
                {
                    [Projection]
                    public partial class Rejected { public int Count { get; set; } }
                }
            }
            public partial class PublicOuter
            {
                [Projection]
                public partial class Accepted { public int Count { get; set; } }
            }
            """;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation([source, OccupiedRoot]);
        ParseResult rejected = CompilationHelper.ParseProjection(compilation, "Fixtures.HiddenOuter+Middle+Rejected");
        rejected.Model.ShouldBeNull();
        DiagnosticInfo diagnostic = rejected.Diagnostics.Single(item => item.Id == "HFC1004");
        diagnostic.Severity.ShouldBe("Warning");
        diagnostic.Message.ShouldBe("[Projection] type 'Rejected' has non-public declaration 'HiddenOuter'. Make the nested projection and every containing type public to generate its view and state.");

        GeneratorDriverRunResult result = Generate(compilation);
        result.Diagnostics.Single(item => item.Id == "HFC1004").Severity.ShouldBe(DiagnosticSeverity.Warning);
        result.Diagnostics.Single(item => item.Id == "HFC1004").GetMessage(CultureInfo.InvariantCulture).ShouldBe(diagnostic.Message);
        var generated = result.Results.Single().GeneratedSources;
        generated.ShouldContain(item => item.SourceText.ToString().Contains("PublicOuter_1AcceptedView", StringComparison.Ordinal));
        string rejectedHint = NestedHintPrefix("Fixtures.HiddenOuter+Middle+Rejected");
        generated.ShouldNotContain(item => item.HintName.StartsWith(rejectedHint, StringComparison.Ordinal));
        generated.ShouldNotContain(item => item.SourceText.ToString().Contains("HiddenOuter.Middle.Rejected", StringComparison.Ordinal));
        generated.ShouldNotContain(item => item.SourceText.ToString().Contains("HiddenOuter+Middle+Rejected", StringComparison.Ordinal));
    }

    /// <summary>Checks exact ancestor order and escaping in generated stems and type references.</summary>
    [Fact]
    public void AncestorNamesReachTheGeneratedViewAndFluxorArtifacts()
    {
        const string source = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace Fixtures;
            public partial class Team_A
            {
                public partial class Department
                {
                    [Projection]
                    public partial class Item { public int Count { get; set; } }
                }
            }
            public partial class TeamA
            {
                public partial class Department
                {
                    [Projection]
                    public partial class Item { public bool Enabled { get; set; } }
                }
            }
            [Projection]
            public partial class Item { public string? Name { get; set; } }
            """;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation([source, OccupiedRoot]);
        DomainModel first = Parse(compilation, "Fixtures.Team_A+Department+Item");
        DomainModel second = Parse(compilation, "Fixtures.TeamA+Department+Item");
        AssertArtifactModels(first, "Team_0A_1Department_1Item", "global::Fixtures.Team_A.Department.Item");
        AssertArtifactModels(second, "TeamA_1Department_1Item", "global::Fixtures.TeamA.Department.Item");

        var generated = Generate(compilation).Results.Single().GeneratedSources;
        generated.Select(item => item.HintName).Distinct(StringComparer.OrdinalIgnoreCase).Count().ShouldBe(generated.Length);
        generated.ShouldContain(item => item.SourceText.ToString().Contains("Team_0A_1Department_1ItemView", StringComparison.Ordinal));
        generated.ShouldContain(item => item.SourceText.ToString().Contains("TeamA_1Department_1ItemState", StringComparison.Ordinal));
        generated.ShouldContain(item => item.SourceText.ToString().Contains("IReadOnlyList<global::Fixtures.Team_A.Department.Item>", StringComparison.Ordinal));
    }

    /// <summary>Checks global and escaped CLR names, generated namespaces, and MCP routes.</summary>
    [Fact]
    public void DescriptorsPreserveGlobalAndEscapedNamespaceIdentity()
    {
        const string globalSource = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            public partial class Outer
            {
                public partial class Middle
                {
                    [BoundedContext("Nested")]
                    [Projection]
                    public partial class Item { public int Count { get; set; } }
                }
            }
            """;
        const string namedSource = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace @event.Core;
            public partial class Outer
            {
                public partial class Middle
                {
                    [BoundedContext("Nested")]
                    [Projection]
                    public partial class Item { public int Count { get; set; } }
                }
            }
            [BoundedContext("Nested")]
            [Projection]
            public partial class Item { public int Count { get; set; } }
            """;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation([globalSource, namedSource, OccupiedRoot]);
        DomainModel global = Parse(compilation, "Outer+Middle+Item");
        DomainModel named = Parse(compilation, "event.Core.Outer+Middle+Item");
        const string globalName = "Outer+Middle+Item";
        const string namedName = "event.Core.Outer+Middle+Item";
        const string globalDestination = "FrontComposerGeneratedNestedProjections_";
        const string namedDestination = globalDestination + ".@event.Core";
        AssertDescriptor(global, globalName, globalDestination);
        AssertDescriptor(named, namedName, namedDestination);
        McpManifestTransform.TransformProjection(Parse(compilation, "event.Core.Item"))
            .ProtocolUri.ShouldBe("frontcomposer://Nested/projections/Item");

        var generated = Generate(compilation, out Compilation output).Results.Single().GeneratedSources;
        foreach ((string name, string destination) in new[] { (globalName, globalDestination), (namedName, namedDestination) })
        {
            foreach (string suffix in new[] { ".g.razor.cs", "Feature.g.cs" })
            {
                generated.Single(item => item.HintName == NestedHintPrefix(name) + suffix)
                    .SourceText.ToString().ShouldContain("namespace " + destination + ";");
            }
        }

        using MemoryStream image = new();
        var emission = output.Emit(image, cancellationToken: TestContext.Current.CancellationToken);
        emission.Success.ShouldBeTrue(string.Join(Environment.NewLine, emission.Diagnostics));
        image.Position = 0;
        var loadContext = new AssemblyLoadContext("NestedProjectionMutationFixture", isCollectible: true);
        try
        {
            var assembly = loadContext.LoadFromStream(image);
            assembly.GetType(globalName).ShouldNotBeNull();
            assembly.GetType(namedName).ShouldNotBeNull();
        }
        finally
        {
            loadContext.Unload();
        }
    }

    /// <summary>Checks that both occupied generated roots are skipped.</summary>
    [Fact]
    public void OccupiedGeneratedRootsSelectTheNextAvailableSuffix()
    {
        const string projection = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace Fixtures;
            public partial class Outer
            {
                [Projection]
                public partial class Item { public int Count { get; set; } }
            }
            """;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation([
            projection,
            OccupiedRoot,
            "namespace FrontComposerGeneratedNestedProjections_; public class Existing {}",
        ]);
        DomainModel model = Parse(compilation, "Fixtures.Outer+Item");
        const string expected = "FrontComposerGeneratedNestedProjections__.Fixtures";

        RazorModelTransform.Transform(model).GeneratedNamespace.ShouldBe(expected);
        FluxorModelTransform.Transform(model).GeneratedNamespace.ShouldBe(expected);
        var sources = Generate(compilation).Results.Single().GeneratedSources;
        sources.ShouldContain(item => item.SourceText.ToString().Contains("namespace " + expected + ";", StringComparison.Ordinal));
    }

    /// <summary>Checks CLR qualification of the top-level fallback with and without a namespace.</summary>
    [Theory]
    [InlineData("", "Item")]
    [InlineData("Fixtures.Data", "Fixtures.Data.Item")]
    public void TopLevelRazorMetadataNameRespectsNamespace(string sourceNamespace, string expected)
    {
        var model = new RazorModel("Item", sourceNamespace, "Fixtures", default);

        model.ProjectionTypeName.ShouldBe(expected);
    }

    /// <summary>Checks projection registration against a command with a full-page route.</summary>
    [Fact]
    public void RegistrationKeepsProjectionAndFullPageCommandFlagsDistinct()
    {
        const string source = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace Fixtures;
            [Projection]
            public partial class Item { public int Count { get; set; } }
            [Command]
            public partial class UpdateItem
            {
                public string MessageId { get; set; } = string.Empty;
                public int First { get; set; }
                public int Second { get; set; }
                public int Third { get; set; }
                public int Fourth { get; set; }
                public int Fifth { get; set; }
            }
            """;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source);
        RegistrationModel projection = RegistrationModelTransform.Transform(Parse(compilation, "Fixtures.Item"));
        RegistrationModel command = RegistrationModelTransform.TransformCommand(
            CompilationHelper.ParseCommand(compilation, "Fixtures.UpdateItem").Model.ShouldNotBeNull());

        projection.IsCommand.ShouldBeFalse();
        projection.HasFullPageRoute.ShouldBeFalse();
        command.IsCommand.ShouldBeTrue();
        command.HasFullPageRoute.ShouldBeTrue();

        string projectionRegistration = RegistrationEmitter.Emit(projection);
        projectionRegistration.ShouldContain("Projections: new List<string> { typeof(Item).FullName! }");
        projectionRegistration.ShouldContain("FullPageCommands = new List<string>()");
        string commandRegistration = RegistrationEmitter.Emit(command);
        commandRegistration.ShouldContain("Commands: new List<string> { typeof(UpdateItem).FullName! }");
        commandRegistration.ShouldContain("FullPageCommands = new List<string> { typeof(UpdateItem).FullName! }");
    }

    /// <summary>Checks nested identity equality and bounded hash participation across affected models.</summary>
    [Fact]
    public void DistinctNestedIdentitiesParticipateInModelHashCollections()
    {
        const int count = 32;
        string source = "namespace Fixtures; " + string.Join('\n', Enumerable.Range(0, count)
            .Select(index => $"public class Outer{index} {{ public class Item {{ }} }}"));
        CSharpCompilation compilation = CompilationHelper.CreateCompilation([source, OccupiedRoot]);
        HashSet<object>[] families =
        [
            new(new CountingModelComparer()),
            new(new CountingModelComparer()),
            new(new CountingModelComparer()),
            new(new CountingModelComparer()),
        ];
        for (int index = 0; index < count; index++)
        {
            INamedTypeSymbol symbol = compilation.GetTypeByMetadataName($"Fixtures.Outer{index}+Item").ShouldNotBeNull();
            object[] first = CreateModels(new NestedProjectionIdentity(symbol, compilation, "Fixtures"));
            object[] equal = CreateModels(new NestedProjectionIdentity(symbol, compilation, "Fixtures"));
            for (int family = 0; family < families.Length; family++)
            {
                first[family].ShouldBe(equal[family]);
                first[family].GetHashCode().ShouldBe(equal[family].GetHashCode());
                families[family].Add(first[family]).ShouldBeTrue();
            }
        }

        foreach (HashSet<object> family in families)
        {
            family.Count.ShouldBe(count);
            ((CountingModelComparer)family.Comparer).Probes.ShouldBeLessThan(count * (count - 1) / 2L);
        }
    }

    private static object[] CreateModels(NestedProjectionIdentity identity) =>
    [
        new DomainModel("Item", "Fixtures", "Nested", null, null, default, null, null, null, null, -1, -1, null, identity),
        new FluxorModel("Item", "Fixtures", "ItemState", "Item", identity),
        new RazorModel("Item", "Fixtures", "Nested", default, ProjectionRenderStrategy.Default, default, null, null, null, identity),
        new RegistrationModel("Nested", "Item", "Fixtures", null, false, null, false, identity),
    ];

    private static DomainModel Parse(CSharpCompilation compilation, string metadataName)
        => CompilationHelper.ParseProjection(compilation, metadataName).Model.ShouldNotBeNull();

    private static void AssertArtifactModels(DomainModel domain, string stem, string typeReference)
    {
        domain.ArtifactName.ShouldBe(stem);
        RazorModel razor = RazorModelTransform.Transform(domain);
        razor.ArtifactName.ShouldBe(stem);
        razor.TypeReference.ShouldBe(typeReference);
        FluxorModel fluxor = FluxorModelTransform.Transform(domain);
        fluxor.ArtifactName.ShouldBe(stem);
        fluxor.TypeReference.ShouldBe(typeReference);
        fluxor.StateName.ShouldBe(stem + "State");
    }

    private static void AssertDescriptor(DomainModel domain, string metadataName, string generatedNamespace)
    {
        NestedProjectionIdentity identity = domain.NestedProjection.ShouldNotBeNull();
        identity.MetadataName.ShouldBe(metadataName);
        identity.GeneratedNamespace.ShouldBe(generatedNamespace);
        RazorModelTransform.Transform(domain).ProjectionTypeName.ShouldBe(metadataName);
        McpResourceDescriptorModel descriptor = McpManifestTransform.TransformProjection(domain);
        descriptor.ProjectionTypeName.ShouldBe(metadataName);
        descriptor.ProtocolUri.ShouldBe("frontcomposer://Nested/projections/nested/" + Convert.ToHexStringLower(Encoding.UTF8.GetBytes(metadataName)));
    }

    private static string NestedHintPrefix(string metadataName)
        => "--nested-projection-" + Convert.ToHexStringLower(Encoding.UTF8.GetBytes(metadataName));

    private static GeneratorDriverRunResult Generate(CSharpCompilation compilation) => Generate(compilation, out _);

    private static GeneratorDriverRunResult Generate(CSharpCompilation compilation, out Compilation output)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new FrontComposerGenerator())
            .RunGeneratorsAndUpdateCompilation(compilation, out output, out _, cancellationToken);
        output.GetDiagnostics(cancellationToken).Where(item => item.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        return driver.GetRunResult();
    }
}
