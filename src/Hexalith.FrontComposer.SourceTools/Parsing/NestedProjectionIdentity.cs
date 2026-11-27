using System.Text;

using Microsoft.CodeAnalysis;

namespace Hexalith.FrontComposer.SourceTools.Parsing;

/// <summary>
/// Carries a nested projection's C# reference, CLR identity, and generated artifact stem.
/// </summary>
internal sealed record NestedProjectionIdentity
{
    internal NestedProjectionIdentity(INamedTypeSymbol type, Compilation compilation, string @namespace)
    {
        SourceName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        Stack<string> identifiers = new();
        Stack<string> metadataIdentifiers = new();
        for (INamedTypeSymbol? current = type; current is not null; current = current.ContainingType)
        {
            identifiers.Push(current.Name.Replace("_", "_0"));
            metadataIdentifiers.Push(current.MetadataName);
        }

        // An underscore in a source identifier cannot be mistaken for a nesting boundary.
        ArtifactName = string.Join("_1", identifiers);
        string metadataNamespace = @namespace.Replace("@", string.Empty);
        string nestedMetadataName = string.Join("+", metadataIdentifiers);
        MetadataName = metadataNamespace.Length == 0 ? nestedMetadataName : metadataNamespace + "." + nestedMetadataName;

        // Hints and protocol identifiers are compared without case. Encode the full name
        // losslessly so case-distinct and non-ASCII identifiers stay distinct.
        EncodedName = BitConverter.ToString(Encoding.UTF8.GetBytes(MetadataName)).Replace("-", string.Empty).ToLowerInvariant();

        const string rootName = "FrontComposerGeneratedNestedProjections";
        string generatedRoot = rootName;
        while (compilation.GlobalNamespace.GetMembers(generatedRoot).Any())
        {
            generatedRoot += "_";
        }

        GeneratedNamespace = @namespace.Length == 0 ? generatedRoot : generatedRoot + "." + @namespace;
    }

    /// <summary>Gets the qualified C# type reference.</summary>
    internal string SourceName { get; }

    /// <summary>Gets the qualified CLR type name used by reflection.</summary>
    internal string MetadataName { get; }

    /// <summary>Gets the case-safe, lossless name used in generated hints and MCP URIs.</summary>
    internal string EncodedName { get; }

    /// <summary>Gets the identifier used to name the emitted view and state artifacts.</summary>
    internal string ArtifactName { get; }

    /// <summary>Gets the generated namespace, isolated from source declarations and top-level projection artifacts.</summary>
    internal string GeneratedNamespace { get; }
}
