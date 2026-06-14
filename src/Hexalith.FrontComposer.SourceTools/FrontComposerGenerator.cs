using System.Collections.Immutable;

using Hexalith.FrontComposer.SourceTools.Diagnostics;
using Hexalith.FrontComposer.SourceTools.Drift;
using Hexalith.FrontComposer.SourceTools.Emitters;
using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Hexalith.FrontComposer.SourceTools;

/// <summary>
/// Roslyn incremental source generator for the FrontComposer framework.
/// Discovers [Projection]- and [Command]-annotated types and emits UI/Fluxor/registration artifacts.
/// </summary>
[Generator]
public sealed class FrontComposerGenerator : IIncrementalGenerator {
    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context) {
        IncrementalValuesProvider<ParseResult> projectionResults = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "Hexalith.FrontComposer.Contracts.Attributes.ProjectionAttribute",
                predicate: static (node, _) => node is TypeDeclarationSyntax,
                transform: static (ctx, ct) => AttributeParser.Parse(ctx, ct))
            .Where(static result => result.Model is not null || result.Diagnostics.Count > 0)
            .WithTrackingName("Parse");

        IncrementalValuesProvider<CommandParseResult> commandResults = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "Hexalith.FrontComposer.Contracts.Attributes.CommandAttribute",
                predicate: static (node, _) => node is TypeDeclarationSyntax,
                transform: static (ctx, ct) => CommandParser.Parse(ctx, ct))
            .Where(static result => result.Model is not null || result.Diagnostics.Count > 0)
            .WithTrackingName("ParseCommand");

        // Story 6-2 T3 — incremental discovery of [ProjectionTemplate] markers.
        IncrementalValuesProvider<ProjectionTemplateMarkerResult> templateResults = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                ProjectionTemplateMarkerParser.ProjectionTemplateAttributeName,
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, ct) => ProjectionTemplateMarkerParser.Parse(ctx, ct))
            .Where(static result => result.Marker is not null || result.Diagnostics.Count > 0)
            .WithTrackingName("ParseProjectionTemplate");

        IncrementalValueProvider<ImmutableArray<ProjectionTemplateMarkerResult>> collectedTemplates = templateResults.Collect();

        IncrementalValueProvider<ImmutableArray<ParseResult>> collectedProjections = projectionResults.Collect();
        IncrementalValueProvider<ImmutableArray<CommandParseResult>> collectedCommands = commandResults.Collect();
        IncrementalValueProvider<DriftOptionsResult> driftOptions = context.AnalyzerConfigOptionsProvider
            .Select(static (provider, _) => DriftOptions.Bind(provider));
        IncrementalValuesProvider<DriftBaselineInput> driftBaselineInputs = context.AdditionalTextsProvider
            .Where(static text => DriftBaselineInput.IsCandidate(text.Path))
            .Select(static (text, ct) => DriftBaselineInput.FromAdditionalText(text, ct))
            .WithTrackingName("LoadDriftBaselines");
        IncrementalValueProvider<ImmutableArray<DriftBaselineInput>> collectedDriftBaselines = driftBaselineInputs.Collect();

        // Story 9-1 review P12 (AC10): drift comparison no longer depends on the compilation.
        // Source locations come from parsed declaration models, so combining CompilationProvider
        // here was invalidating drift output on every unrelated compilation change.
        context.RegisterSourceOutput(
            collectedProjections.Combine(collectedCommands).Combine(collectedDriftBaselines.Combine(driftOptions)),
            static (spc, source) => {
                ImmutableArray<ParseResult> projections = source.Left.Left;
                ImmutableArray<CommandParseResult> commands = source.Left.Right;
                ImmutableArray<DriftBaselineInput> baselineInputs = source.Right.Left;
                DriftOptionsResult optionsResult = source.Right.Right;

                foreach (DriftDiagnosticFact diagnostic in optionsResult.Diagnostics) {
                    spc.ReportDiagnostic(diagnostic.ToDiagnostic());
                }

                // Story 9-1 review P4: drift detection is opt-in. Don't load or compare unless
                // the adopter explicitly set HfcDriftDetectionEnabled — a stray *.json
                // AdditionalText must not produce HFC1060/HFC1064 noise.
                if (!optionsResult.Options.Enabled) {
                    return;
                }

                DriftBaselineLoadResult baseline = DriftBaselineLoader.Load(baselineInputs, optionsResult.Options);
                foreach (DriftDiagnosticFact diagnostic in baseline.Diagnostics) {
                    spc.ReportDiagnostic(diagnostic.ToDiagnostic());
                }

                // Story 9-1 review P3 (AC2): a baseline-with-no-current-source run must still
                // emit RemovedDeclaration / RemovedProperty drift. Don't gate on hasContracts.
                if (!baseline.ComparisonEnabled) {
                    return;
                }

                var current = DriftCurrentSnapshot.From(projections, commands);
                DriftComparisonResult comparison = DriftComparisonService.Compare(
                    current,
                    baseline.Baseline,
                    optionsResult.Options.MaxDiagnostics,
                    optionsResult.Options.DriftSeverity);
                foreach (DriftDiagnosticFact diagnostic in comparison.Diagnostics) {
                    spc.ReportDiagnostic(diagnostic.ToDiagnostic());
                }
            });

        // Story 9-1 review P-D1 (AC14): trim/AOT diagnostic requires looking at the compilation
        // for adopter-supplied IActionQueueProjectionCatalog evidence. Run as a separate
        // RegisterSourceOutput so the compilation dependency does not leak into drift comparison.
        // The advisory is independent of HfcDriftDetectionEnabled — trim/AOT safety advice
        // applies whether or not drift detection is opted in.
        context.RegisterSourceOutput(
            context.CompilationProvider.Combine(driftOptions).Combine(collectedProjections.Combine(collectedCommands)),
            static (spc, source) => {
                Compilation compilation = source.Left.Left;
                DriftOptionsResult optionsResult = source.Left.Right;
                ImmutableArray<ParseResult> projections = source.Right.Left;
                ImmutableArray<CommandParseResult> commands = source.Right.Right;

                if (!optionsResult.Options.TrimOrAotAdvisoryEnabled) {
                    return;
                }

                bool hasContracts = projections.Any(static p => p.Model is not null)
                    || commands.Any(static c => c.Model is not null);
                if (!hasContracts) {
                    return;
                }

                if (HasActionQueueProjectionCatalogOverride(compilation)) {
                    return;
                }

                spc.ReportDiagnostic(DriftDiagnosticFact.TrimAot().ToDiagnostic());
            });

        context.RegisterSourceOutput(
            collectedProjections.Combine(collectedCommands).Combine(collectedTemplates),
            static (spc, source) => {
                ImmutableArray<ParseResult> projections = source.Left.Left;
                ImmutableArray<CommandParseResult> commands = source.Left.Right;
                ImmutableArray<ProjectionTemplateMarkerResult> templates = source.Right;
                // Story 6-2 — Razor/Web compilations may carry only [ProjectionTemplate] markers
                // (templates) and reference projections from a separate domain assembly. Suppress
                // HFC1001 when at least one marker was discovered so adopters do not see a
                // misleading "no types found" warning on template-only projects.
                if (projections.Length == 0 && commands.Length == 0 && templates.Length == 0) {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.NoAnnotatedTypesFound,
                        Location.None,
                        "No [Command] or [Projection] types found in compilation"));
                }
            });

        context.RegisterSourceOutput(
            collectedProjections.Combine(collectedCommands),
            static (spc, source) => {
                ImmutableArray<DomainModel>.Builder projectionBuilder = ImmutableArray.CreateBuilder<DomainModel>();
                foreach (ParseResult projection in source.Left) {
                    if (projection.Model is not null) {
                        projectionBuilder.Add(projection.Model);
                    }
                }

                ImmutableArray<CommandModel>.Builder commandBuilder = ImmutableArray.CreateBuilder<CommandModel>();
                foreach (CommandParseResult command in source.Right) {
                    if (command.Model is not null) {
                        commandBuilder.Add(command.Model);
                    }
                }

                if (projectionBuilder.Count == 0 && commandBuilder.Count == 0) {
                    return;
                }

                spc.AddSource(
                    McpManifestEmitter.GeneratedHintName,
                    McpManifestEmitter.Emit(commandBuilder.ToImmutable(), projectionBuilder.ToImmutable()));
            });

        context.RegisterSourceOutput(projectionResults, static (spc, result) => {
            foreach (DiagnosticInfo diagInfo in result.Diagnostics) {
                spc.ReportDiagnostic(Diagnostic.Create(
                    GetDescriptor(diagInfo.Id, diagInfo.Severity),
                    diagInfo.ToLocation(),
                    diagInfo.Message));
            }

            if (result.Model is not null) {
                // Story 4-1 T2.5 — Transform-stage diagnostics (HFC1023 Dashboard fallback)
                // travel through the same per-projection reporter.
                List<DiagnosticInfo> transformDiagnostics = [];
                RazorModel razorModel = RazorModelTransform.Transform(result.Model, transformDiagnostics);
                foreach (DiagnosticInfo diagInfo in transformDiagnostics) {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        GetDescriptor(diagInfo.Id, diagInfo.Severity),
                        diagInfo.ToLocation(),
                        diagInfo.Message));
                }

                FluxorModel fluxorModel = FluxorModelTransform.Transform(result.Model);
                RegistrationModel registrationModel = RegistrationModelTransform.Transform(result.Model);

                string hintPrefix = GetQualifiedHintPrefix(result.Model.Namespace, result.Model.TypeName);
                spc.AddSource(hintPrefix + ".g.razor.cs", RazorEmitter.Emit(razorModel));
                spc.AddSource(hintPrefix + "Feature.g.cs", FluxorFeatureEmitter.Emit(fluxorModel));
                spc.AddSource(hintPrefix + "Actions.g.cs", FluxorActionsEmitter.EmitActions(fluxorModel));
                spc.AddSource(hintPrefix + "Reducers.g.cs", FluxorActionsEmitter.EmitReducers(fluxorModel));
                spc.AddSource(hintPrefix + "Registration.g.cs", RegistrationEmitter.Emit(registrationModel));
            }
        });

        // Story 6-2 T3 — per-marker diagnostics (parser-level: HFC1033/HFC1034/HFC1035/HFC1036).
        context.RegisterSourceOutput(templateResults, static (spc, result) => {
            foreach (DiagnosticInfo diagInfo in result.Diagnostics) {
                spc.ReportDiagnostic(Diagnostic.Create(
                    GetDescriptor(diagInfo.Id, diagInfo.Severity),
                    diagInfo.ToLocation(),
                    diagInfo.Message));
            }
        });

        // Story 6-2 T3 — emit the consolidated template manifest plus duplicate diagnostics
        // (HFC1037). Driven from collectedTemplates so the manifest source is regenerated
        // only when a marker actually changes.
        context.RegisterSourceOutput(collectedTemplates, static (spc, results) => {
            ImmutableArray<ProjectionTemplateMarkerInfo>.Builder builder = ImmutableArray.CreateBuilder<ProjectionTemplateMarkerInfo>();
            foreach (ProjectionTemplateMarkerResult result in results) {
                // Story 6-2 T7 — exclude markers whose major contract version is incompatible.
                // The marker still keeps its HFC1035 warning (already emitted in the per-marker
                // pass above); but the descriptor is suppressed so runtime selection cannot
                // run a template against a mismatched context shape.
                if (result.Marker is null) {
                    continue;
                }

                int markerMajor = result.Marker.ExpectedContractVersion / 1_000_000;
                if (markerMajor != ProjectionTemplateMarkerParser.CurrentContractMajor) {
                    continue;
                }

                builder.Add(result.Marker);
            }

            ProjectionTemplateManifestEmitter.ManifestEmissionResult emission =
                ProjectionTemplateManifestEmitter.Emit(builder.ToImmutable());

            foreach (DiagnosticInfo diagInfo in emission.DuplicateDiagnostics) {
                spc.ReportDiagnostic(Diagnostic.Create(
                    GetDescriptor(diagInfo.Id, diagInfo.Severity),
                    diagInfo.ToLocation(),
                    diagInfo.Message));
            }

            spc.AddSource(ProjectionTemplateManifestEmitter.GeneratedHintName, emission.Source);
        });

        context.RegisterSourceOutput(commandResults, static (spc, result) => {
            foreach (DiagnosticInfo diagInfo in result.Diagnostics) {
                spc.ReportDiagnostic(Diagnostic.Create(
                    GetDescriptor(diagInfo.Id, diagInfo.Severity),
                    diagInfo.ToLocation(),
                    diagInfo.Message));
            }

            if (result.Model is not null) {
                CommandFluxorModel fluxorModel = CommandFluxorTransform.Transform(result.Model);
                CommandFormModel formModel = CommandFormTransform.Transform(result.Model);
                RegistrationModel registrationModel = RegistrationModelTransform.TransformCommand(result.Model);
                CommandRendererModel rendererModel = CommandRendererTransform.Transform(result.Model, fluxorModel);

                // Suffix with ".Command" segment so command-pipeline hints never collide with
                // projection-pipeline hints when a type carries both attributes.
                // (See code-review 2026-04-15, patch P6.)
                string hintPrefix = GetQualifiedHintPrefix(result.Model.Namespace, result.Model.TypeName) + ".Command";
                spc.AddSource(hintPrefix + "Form.g.razor.cs", CommandFormEmitter.Emit(formModel, fluxorModel));
                spc.AddSource(hintPrefix + "Actions.g.cs", CommandFluxorActionsEmitter.Emit(fluxorModel));
                spc.AddSource(hintPrefix + "LifecycleFeature.g.cs", CommandFluxorFeatureEmitter.Emit(fluxorModel));
                spc.AddSource(hintPrefix + "Registration.g.cs", RegistrationEmitter.Emit(registrationModel));

                // Story 2-2 Task 8: density-driven renderer + per-command LastUsed subscriber +
                // (conditional) routable FullPage page.
                spc.AddSource(hintPrefix + "Renderer.g.razor.cs", CommandRendererEmitter.Emit(rendererModel));
                spc.AddSource(hintPrefix + "LastUsedSubscriber.g.cs", LastUsedSubscriberEmitter.Emit(fluxorModel));
                if (result.Model.Density == Hexalith.FrontComposer.SourceTools.Parsing.CommandDensity.FullPage) {
                    spc.AddSource(hintPrefix + "Page.g.razor.cs", CommandPageEmitter.Emit(rendererModel));
                }

                // Story 2-3 Decision D4/D16: per-command lifecycle bridge — forwards Fluxor actions
                // to ILifecycleStateService so cross-command correlation-keyed consumers (Story 2-4
                // FcLifecycleWrapper) can subscribe to a single service instead of reflecting over
                // N generated features.
                spc.AddSource(hintPrefix + "LifecycleBridge.g.cs", CommandLifecycleBridgeEmitter.Emit(fluxorModel));
            }
        });
    }

    private static string GetQualifiedHintPrefix(string @namespace, string typeName)
        => string.IsNullOrEmpty(@namespace) ? typeName : @namespace + "." + typeName;

    /// <summary>
    /// Story 9-1 P-D1 (AC14): scans the compilation for any non-default
    /// <c>IActionQueueProjectionCatalog</c> implementation in source. Returns <c>true</c> when
    /// adopter override evidence is present (so HFC1070 should NOT fire). When the contracts
    /// interface is not referenced (e.g., trim/AOT setting flipped on a project that does not
    /// reference <c>Hexalith.FrontComposer.Contracts</c>), returns <c>true</c> to fail-quiet —
    /// the trim/AOT warning targets adopters who use the framework, not unrelated builds.
    /// </summary>
    private static bool HasActionQueueProjectionCatalogOverride(Compilation compilation) {
        const string interfaceMetadataName = "Hexalith.FrontComposer.Contracts.Badges.IActionQueueProjectionCatalog";
        const string defaultImplName = "ReflectionActionQueueProjectionCatalog";
        INamedTypeSymbol? interfaceSymbol = compilation.GetTypeByMetadataName(interfaceMetadataName);
        if (interfaceSymbol is null) {
            return true;
        }

        foreach (SyntaxTree tree in compilation.SyntaxTrees) {
            SemanticModel semanticModel = compilation.GetSemanticModel(tree);
            foreach (SyntaxNode node in tree.GetRoot().DescendantNodes()) {
                if (node is not TypeDeclarationSyntax typeDecl) {
                    continue;
                }

                if (semanticModel.GetDeclaredSymbol(typeDecl) is not INamedTypeSymbol typeSymbol) {
                    continue;
                }

                if (typeSymbol.IsAbstract || typeSymbol.TypeKind != TypeKind.Class) {
                    continue;
                }

                if (string.Equals(typeSymbol.Name, defaultImplName, StringComparison.Ordinal)) {
                    continue;
                }

                if (typeSymbol.AllInterfaces.Contains(interfaceSymbol, SymbolEqualityComparer.Default)) {
                    return true;
                }
            }
        }

        return false;
    }

    private static DiagnosticDescriptor GetDescriptor(string id, string severity) => id switch {
        "HFC1001" => DiagnosticDescriptors.NoAnnotatedTypesFound,
        "HFC1002" => DiagnosticDescriptors.UnsupportedFieldType,
        "HFC1003" => DiagnosticDescriptors.ProjectionShouldBePartial,
        "HFC1004" => DiagnosticDescriptors.UnsupportedTypeKind,
        "HFC1005" => DiagnosticDescriptors.InvalidAttributeArgument,
        "HFC1006" => DiagnosticDescriptors.CommandMissingMessageId,
        "HFC1007" => severity == "Error" ? CreateError(id, "Command has too many non-derivable properties") : DiagnosticDescriptors.CommandTooManyProperties,
        "HFC1008" => DiagnosticDescriptors.CommandFlagsEnumProperty,
        "HFC1009" => DiagnosticDescriptors.CommandMissingParameterlessCtor,
        "HFC1011" => DiagnosticDescriptors.CommandTooManyTotalProperties,
        "HFC1012" => DiagnosticDescriptors.DefaultValueTypeMismatch,
        "HFC1014" => DiagnosticDescriptors.NestedCommandUnsupported,
        "HFC1015" => DiagnosticDescriptors.RenderModeIncompatibleWithDensity,
        "HFC1016" => DiagnosticDescriptors.CommandPropertyNotWritable,
        "HFC1017" => DiagnosticDescriptors.CommandTypeIsGeneric,
        "HFC1020" => DiagnosticDescriptors.DestructiveNamePatternMissingAttribute,
        "HFC1021" => DiagnosticDescriptors.DestructiveCommandHasZeroFields,
        "HFC1022" => DiagnosticDescriptors.ProjectionWhenStateMemberUnknown,
        "HFC1023" => DiagnosticDescriptors.ProjectionRoleDashboardFallback,
        "HFC1024" => DiagnosticDescriptors.UnknownProjectionRoleValue,
        "HFC1025" => DiagnosticDescriptors.BadgeSlotFallbackApplied,
        "HFC1026" => DiagnosticDescriptors.ColorOnlyBadgeDetected,
        "HFC1027" => DiagnosticDescriptors.CollectionColumnNotFilterable,
        "HFC1028" => DiagnosticDescriptors.ColumnPriorityCollision,
        "HFC1029" => DiagnosticDescriptors.ColumnPrioritizerActivated,
        "HFC1030" => DiagnosticDescriptors.FieldGroupNameCollidesWithCatchAll,
        "HFC1031" => DiagnosticDescriptors.FieldGroupIgnoredForNonDetailRole,
        "HFC1032" => DiagnosticDescriptors.Level1FormatAnnotationInvalid,
        "HFC1033" => DiagnosticDescriptors.ProjectionTemplateInvalidProjectionType,
        "HFC1034" => DiagnosticDescriptors.ProjectionTemplateContextParameterMissing,
        "HFC1035" => DiagnosticDescriptors.ProjectionTemplateContractVersionMismatch,
        "HFC1036" => DiagnosticDescriptors.ProjectionTemplateContractVersionDrift,
        "HFC1037" => DiagnosticDescriptors.ProjectionTemplateDuplicate,
        "HFC1038" => DiagnosticDescriptors.ProjectionSlotSelectorInvalid,
        "HFC1039" => DiagnosticDescriptors.ProjectionSlotComponentInvalid,
        "HFC1040" => DiagnosticDescriptors.ProjectionSlotDuplicate,
        "HFC1041" => DiagnosticDescriptors.ProjectionSlotContractVersionMismatch,
        "HFC1056" => DiagnosticDescriptors.CommandAuthorizationPolicyInvalid,
        "HFC1057" => DiagnosticDescriptors.CommandAuthorizationPolicyDuplicate,
        "HFC1058" => DiagnosticDescriptors.GeneratedUiBaselineMissing,
        "HFC1059" => DiagnosticDescriptors.GeneratedUiBaselinePathInvalid,
        "HFC1060" => DiagnosticDescriptors.GeneratedUiBaselineContentInvalid,
        "HFC1061" => DiagnosticDescriptors.GeneratedUiBaselineSchemaUnsupported,
        "HFC1062" => DiagnosticDescriptors.GeneratedUiBaselineAlgorithmUnsupported,
        "HFC1063" => DiagnosticDescriptors.GeneratedUiBaselineBoundsExceeded,
        "HFC1064" => DiagnosticDescriptors.GeneratedUiBaselineIdentityInvalid,
        "HFC1065" => DiagnosticDescriptors.GeneratedUiStructuralDrift,
        "HFC1066" => DiagnosticDescriptors.GeneratedUiMetadataDrift,
        "HFC1067" => DiagnosticDescriptors.GeneratedUiDriftOptionInvalid,
        "HFC1068" => DiagnosticDescriptors.GeneratedUiDriftTruncated,
        "HFC1069" => DiagnosticDescriptors.GeneratedUiDriftRedactionSuppressed,
        "HFC1070" => DiagnosticDescriptors.TrimAotReflectionCatalogWarning,
        _ => new DiagnosticDescriptor(
            id,
            "FrontComposer Diagnostic",
            "{0}",
            "HexalithFrontComposer",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true),
    };

    private static DiagnosticDescriptor CreateError(string id, string title) => new(
        id: id,
        title: title,
        messageFormat: "{0}",
        category: "HexalithFrontComposer",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
