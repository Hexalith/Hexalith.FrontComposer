using Hexalith.FrontComposer.Contracts.Communication;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Diagnostics;

public sealed class QueryRequestDeprecationTests
{
    private const string CanonicalSource = """
        using Hexalith.FrontComposer.Contracts.Communication;

        public static class Consumer
        {
            public static QueryRequest Create() => QueryRequest.Create(new ProjectionQuery("Orders", Take: 25), "tenant-a");

            public static int? Read(QueryRequest request) => request.Criteria.Take;
        }
        """;

    private const string LegacySource = """
        using Hexalith.FrontComposer.Contracts.Communication;

        public static class Consumer
        {
            public static QueryRequest Create() => new("Orders", "tenant-a", Take: 25);

            public static int? Read(QueryRequest request)
            {
                _ = request.ProjectionType;
                _ = request.Filter;
                _ = request.Skip;
                _ = request.Take;
                _ = request.ColumnFilters;
                _ = request.StatusFilters;
                _ = request.SearchQuery;
                _ = request.SortColumn;
                _ = request.SortDescending;
                return request.Take;
            }

            public static string Deconstruct(QueryRequest request)
            {
                var (projectionType, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _, _) = request;
                return projectionType;
            }
        }
        """;

    [Fact]
    public void Net10Consumer_CanonicalSurface_EmitsNoObsoleteDiagnostic()
    {
        Diagnostic[] diagnostics = Compile(CanonicalSource, typeof(QueryRequest).Assembly.Location);

        diagnostics.Where(diagnostic => diagnostic.Severity >= DiagnosticSeverity.Warning).ShouldBeEmpty();
    }

    [Fact]
    public void Net10Consumer_LegacySurface_EmitsHfc0001WithCanonicalHelpLink()
    {
        Diagnostic[] diagnostics = Compile(LegacySource, typeof(QueryRequest).Assembly.Location);
        Diagnostic[] obsolete = diagnostics.Where(diagnostic => diagnostic.Id == "HFC0001").ToArray();

        obsolete.Length.ShouldBe(12);
        obsolete.All(diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning).ShouldBeTrue();
        obsolete.All(diagnostic => diagnostic.Descriptor.HelpLinkUri == "https://hexalith.github.io/FrontComposer/diagnostics/HFC0001").ShouldBeTrue();
        diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
    }

    [Fact]
    public void NetStandardConsumer_CanonicalSurface_EmitsNoObsoleteDiagnostic()
    {
        Diagnostic[] diagnostics = Compile(CanonicalSource, NetStandardContractsAssembly());

        diagnostics.Where(diagnostic => diagnostic.Severity >= DiagnosticSeverity.Warning).ShouldBeEmpty();
    }

    [Fact]
    public void NetStandardConsumer_LegacySurface_EmitsCs0618Fallback()
    {
        Diagnostic[] diagnostics = Compile(LegacySource, NetStandardContractsAssembly());
        Diagnostic[] obsolete = diagnostics.Where(diagnostic => diagnostic.Id == "CS0618").ToArray();

        obsolete.Length.ShouldBe(12);
        obsolete.All(diagnostic => diagnostic.GetMessage().Contains("HFC0001", StringComparison.Ordinal)).ShouldBeTrue();
        obsolete.All(diagnostic => diagnostic.GetMessage().Contains("v3.0.0", StringComparison.Ordinal)).ShouldBeTrue();
        diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
    }

    private static Diagnostic[] Compile(string source, string contractsAssembly)
    {
        string[] trustedAssemblies = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))!
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
        List<MetadataReference> references = trustedAssemblies
            // The fixture selects one Contracts TFM explicitly; exclude the in-process runner's copy.
            .Where(path => !string.Equals(
                Path.GetFileName(path),
                "Hexalith.FrontComposer.Contracts.dll",
                StringComparison.OrdinalIgnoreCase))
            .Select(path => MetadataReference.CreateFromFile(path))
            .Cast<MetadataReference>()
            .ToList();
        references.Add(MetadataReference.CreateFromFile(contractsAssembly));
        CSharpCompilation compilation = CSharpCompilation.Create(
            "QueryRequestConsumer",
            [CSharpSyntaxTree.ParseText(source)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        return compilation.GetDiagnostics().ToArray();
    }

    private static string NetStandardContractsAssembly()
    {
        string path = Path.Combine(
            ProjectRoot(),
            "src",
            "Hexalith.FrontComposer.Contracts",
            "bin",
            "Release",
            "netstandard2.0",
            "Hexalith.FrontComposer.Contracts.dll");
        File.Exists(path).ShouldBeTrue($"Build the Contracts netstandard2.0 target before running this test: {path}");
        return path;
    }

    private static string ProjectRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Hexalith.FrontComposer.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate the FrontComposer project root.");
    }
}
