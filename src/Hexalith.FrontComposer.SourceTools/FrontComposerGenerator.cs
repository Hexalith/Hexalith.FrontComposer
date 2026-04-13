#nullable enable

namespace Hexalith.FrontComposer.SourceTools;

using Hexalith.FrontComposer.SourceTools.Diagnostics;
using Hexalith.FrontComposer.SourceTools.Parsing;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>
/// Roslyn incremental source generator for the FrontComposer framework.
/// Discovers [Projection]-annotated types and builds a typed intermediate representation.
/// </summary>
[Generator]
public sealed class FrontComposerGenerator : IIncrementalGenerator
{
    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Parse stage: discover [Projection]-annotated types
        // ForAttributeWithMetadataName is the REQUIRED approach (not CreateSyntaxProvider)
        IncrementalValuesProvider<ParseResult> parseResults = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "Hexalith.FrontComposer.Contracts.Attributes.ProjectionAttribute",
                predicate: static (node, _) => node is TypeDeclarationSyntax,
                transform: static (ctx, ct) => AttributeParser.Parse(ctx, ct))
            .Where(static result => result.Model is not null || result.Diagnostics.Count > 0)
            .WithTrackingName("Parse");

        // Output registration -- unpack ParseResult, report diagnostics, forward model
        context.RegisterSourceOutput(parseResults, static (spc, result) =>
        {
            // Report diagnostics collected during parse (cannot emit from transform callback)
            foreach (DiagnosticInfo diagInfo in result.Diagnostics)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    GetDescriptor(diagInfo.Id),
                    diagInfo.ToLocation(),
                    diagInfo.Message));
            }

            // Transform + Emit will go here in Story 1.5
            if (result.Model is not null)
            {
                // Placeholder for Story 1.5 Transform + Emit stages
            }
        });
    }

    private static DiagnosticDescriptor GetDescriptor(string id)
    {
        switch (id)
        {
            case "HFC1002":
                return DiagnosticDescriptors.UnsupportedFieldType;
            case "HFC1003":
                return DiagnosticDescriptors.ProjectionShouldBePartial;
            case "HFC1004":
                return DiagnosticDescriptors.UnsupportedTypeKind;
            case "HFC1005":
                return DiagnosticDescriptors.InvalidAttributeArgument;
            default:
                return new DiagnosticDescriptor(
                    id,
                    "FrontComposer Diagnostic",
                    "{0}",
                    "HexalithFrontComposer",
                    DiagnosticSeverity.Warning,
                    isEnabledByDefault: true);
        }
    }
}
