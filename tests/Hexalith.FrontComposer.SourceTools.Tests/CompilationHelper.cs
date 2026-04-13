namespace Hexalith.FrontComposer.SourceTools.Tests;

using System;
using System.IO;
using System.Linq;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.SourceTools.Parsing;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Xunit;

internal static class CompilationHelper
{
    internal static CSharpCompilation CreateCompilation(string source, bool enableNullable = true)
    {
        MetadataReference[] references =
        [
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ProjectionAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.ComponentModel.DataAnnotations.DisplayAttribute).Assembly.Location),
        ];

        // Add all runtime assemblies needed for netcoreapp compilation
        string runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        string[] additionalDlls = ["System.Runtime.dll", "netstandard.dll"];
        MetadataReference[] additionalRefs = additionalDlls
            .Select(dll => Path.Combine(runtimeDir, dll))
            .Where(File.Exists)
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .ToArray();

        CSharpCompilationOptions options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            .WithNullableContextOptions(enableNullable ? NullableContextOptions.Enable : NullableContextOptions.Disable);

        return CSharpCompilation.Create(
            "TestAssembly",
            [CreateSyntaxTree(source, "Test0.cs")],
            references.Concat(additionalRefs),
            options);
    }

    internal static CSharpCompilation CreateCompilation(string[] sources, bool enableNullable = true)
    {
        MetadataReference[] references =
        [
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ProjectionAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.ComponentModel.DataAnnotations.DisplayAttribute).Assembly.Location),
        ];

        string runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        string[] additionalDlls = ["System.Runtime.dll", "netstandard.dll"];
        MetadataReference[] additionalRefs = additionalDlls
            .Select(dll => Path.Combine(runtimeDir, dll))
            .Where(File.Exists)
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .ToArray();

        CSharpCompilationOptions options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            .WithNullableContextOptions(enableNullable ? NullableContextOptions.Enable : NullableContextOptions.Disable);

        return CSharpCompilation.Create(
            "TestAssembly",
            sources.Select((source, index) => CreateSyntaxTree(source, $"Test{index}.cs")),
            references.Concat(additionalRefs),
            options);
    }

    internal static ParseResult ParseProjection(string source, string metadataName, bool enableNullable = true)
        => ParseProjection(CreateCompilation(source, enableNullable), metadataName);

    internal static ParseResult ParseProjection(string[] sources, string metadataName, bool enableNullable = true)
        => ParseProjection(CreateCompilation(sources, enableNullable), metadataName);

    internal static ParseResult ParseProjection(CSharpCompilation compilation, string metadataName)
    {
        INamedTypeSymbol typeSymbol = compilation.GetTypeByMetadataName(metadataName)
            ?? throw new InvalidOperationException($"Could not find projection type '{metadataName}' in the test compilation.");

        SyntaxReference syntaxReference = typeSymbol.DeclaringSyntaxReferences.FirstOrDefault()
            ?? throw new InvalidOperationException($"Projection type '{metadataName}' has no declaring syntax reference.");

        SyntaxNode targetNode = syntaxReference.GetSyntax(TestContext.Current.CancellationToken);
        return AttributeParser.Parse(typeSymbol, targetNode, TestContext.Current.CancellationToken);
    }

    private static SyntaxTree CreateSyntaxTree(string source, string filePath)
        => CSharpSyntaxTree.ParseText(source, path: filePath);
}
