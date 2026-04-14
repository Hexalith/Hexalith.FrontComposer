
using Hexalith.FrontComposer.SourceTools.Diagnostics;
using Hexalith.FrontComposer.SourceTools.Emitters;
using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Hexalith.FrontComposer.SourceTools;
/// <summary>
/// Roslyn incremental source generator for the FrontComposer framework.
/// Discovers [Projection]-annotated types and builds a typed intermediate representation.
/// </summary>
[Generator]
public sealed class FrontComposerGenerator : IIncrementalGenerator {
    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context) {
        // Parse stage: discover [Projection]-annotated types
        // ForAttributeWithMetadataName is the REQUIRED approach (not CreateSyntaxProvider)
        IncrementalValuesProvider<ParseResult> parseResults = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "Hexalith.FrontComposer.Contracts.Attributes.ProjectionAttribute",
                predicate: static (node, _) => node is TypeDeclarationSyntax,
                transform: static (ctx, ct) => AttributeParser.Parse(ctx, ct))
            .Where(static result => result.Model is not null || result.Diagnostics.Count > 0)
            .WithTrackingName("Parse");

        IncrementalValueProvider<System.Collections.Immutable.ImmutableArray<ParseResult>> collectedParseResults = parseResults.Collect();
        IncrementalValueProvider<System.Collections.Immutable.ImmutableArray<bool>> commandMatches = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "Hexalith.FrontComposer.Contracts.Attributes.CommandAttribute",
                predicate: static (node, _) => node is TypeDeclarationSyntax,
                transform: static (_, _) => true)
            .Collect();

        context.RegisterSourceOutput(collectedParseResults.Combine(commandMatches), static (spc, source) => {
            if (source.Left.Length == 0 && source.Right.Length == 0) {
                spc.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.NoAnnotatedTypesFound,
                    Location.None,
                    "No [Command] or [Projection] types found in compilation"));
            }
        });

        // Output registration -- unpack ParseResult, report diagnostics, forward model
        context.RegisterSourceOutput(parseResults, static (spc, result) => {
            // Report diagnostics collected during parse (cannot emit from transform callback)
            foreach (DiagnosticInfo diagInfo in result.Diagnostics) {
                spc.ReportDiagnostic(Diagnostic.Create(
                    GetDescriptor(diagInfo.Id),
                    diagInfo.ToLocation(),
                    diagInfo.Message));
            }

            if (result.Model is not null) {
                // Transform
                RazorModel razorModel = RazorModelTransform.Transform(result.Model);
                FluxorModel fluxorModel = FluxorModelTransform.Transform(result.Model);
                RegistrationModel registrationModel = RegistrationModelTransform.Transform(result.Model);

                // Emit -- use namespace-qualified hint names to avoid collisions
                // between same-named projections in different namespaces
                string hintPrefix = GetQualifiedHintPrefix(result.Model);
                spc.AddSource(hintPrefix + ".g.razor.cs", RazorEmitter.Emit(razorModel));
                spc.AddSource(hintPrefix + "Feature.g.cs", FluxorFeatureEmitter.Emit(fluxorModel));
                spc.AddSource(hintPrefix + "Actions.g.cs", FluxorActionsEmitter.EmitActions(fluxorModel));
                spc.AddSource(hintPrefix + "Reducers.g.cs", FluxorActionsEmitter.EmitReducers(fluxorModel));
                spc.AddSource(hintPrefix + "Registration.g.cs", RegistrationEmitter.Emit(registrationModel));
            }
        });
    }

    private static string GetQualifiedHintPrefix(DomainModel model) {
        if (string.IsNullOrEmpty(model.Namespace)) {
            return model.TypeName;
        }

        return model.Namespace + "." + model.TypeName;
    }

    private static DiagnosticDescriptor GetDescriptor(string id) => id switch {
        "HFC1001" => DiagnosticDescriptors.NoAnnotatedTypesFound,
        "HFC1002" => DiagnosticDescriptors.UnsupportedFieldType,
        "HFC1003" => DiagnosticDescriptors.ProjectionShouldBePartial,
        "HFC1004" => DiagnosticDescriptors.UnsupportedTypeKind,
        "HFC1005" => DiagnosticDescriptors.InvalidAttributeArgument,
        _ => new DiagnosticDescriptor(
                            id,
                            "FrontComposer Diagnostic",
                            "{0}",
                            "HexalithFrontComposer",
                            DiagnosticSeverity.Warning,
                            isEnabledByDefault: true),
    };
}
