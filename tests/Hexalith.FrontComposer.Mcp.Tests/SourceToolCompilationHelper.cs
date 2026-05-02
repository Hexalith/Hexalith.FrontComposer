using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.SourceTools.Parsing;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Hexalith.FrontComposer.Mcp.Tests;

internal static class SourceToolCompilationHelper {
    internal static CommandParseResult ParseCommand(string source, string metadataName) {
        CSharpCompilation compilation = CreateCompilation(source);
        INamedTypeSymbol typeSymbol = compilation.GetTypeByMetadataName(metadataName)
            ?? throw new InvalidOperationException("Could not find command type.");
        SyntaxNode targetNode = typeSymbol.DeclaringSyntaxReferences[0].GetSyntax(TestContext.Current.CancellationToken);
        return CommandParser.Parse(typeSymbol, targetNode, TestContext.Current.CancellationToken);
    }

    internal static ParseResult ParseProjection(string source, string metadataName) {
        CSharpCompilation compilation = CreateCompilation(source);
        INamedTypeSymbol typeSymbol = compilation.GetTypeByMetadataName(metadataName)
            ?? throw new InvalidOperationException("Could not find projection type.");
        SyntaxNode targetNode = typeSymbol.DeclaringSyntaxReferences[0].GetSyntax(TestContext.Current.CancellationToken);
        return AttributeParser.Parse(typeSymbol, targetNode, TestContext.Current.CancellationToken);
    }

    private static CSharpCompilation CreateCompilation(string source) {
        string runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        List<MetadataReference> refs = [
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ProjectionAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.ComponentModel.DescriptionAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.ComponentModel.DataAnnotations.DisplayAttribute).Assembly.Location),
        ];

        foreach (string dll in new[] { "System.Runtime.dll", "netstandard.dll", "System.Collections.dll", "System.Linq.dll" }) {
            string path = Path.Combine(runtimeDir, dll);
            if (File.Exists(path)) {
                refs.Add(MetadataReference.CreateFromFile(path));
            }
        }

        return CSharpCompilation.Create(
            "McpManifestTestAssembly",
            [CSharpSyntaxTree.ParseText(source, path: "Test0.cs")],
            refs,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithNullableContextOptions(NullableContextOptions.Enable));
    }
}

