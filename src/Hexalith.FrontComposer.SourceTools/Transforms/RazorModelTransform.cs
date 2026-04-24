using System;
using System.Collections.Generic;
using System.Collections.Immutable;

using Hexalith.FrontComposer.SourceTools.Parsing;

namespace Hexalith.FrontComposer.SourceTools.Transforms;
/// <summary>
/// Transforms a DomainModel IR into a RazorModel for DataGrid component generation.
/// </summary>
public static class RazorModelTransform {
    private static readonly char[] WhenStateSeparator = [','];

    /// <summary>
    /// Transforms a parsed domain model into a Razor output model with any Transform-stage
    /// diagnostics collected for caller emission. Story 4-1 T2.3-T2.5 adds role → strategy
    /// dispatch and HFC1023 / HFC1024 fallbacks.
    /// </summary>
    /// <param name="model">The domain model IR from the Parse stage.</param>
    /// <param name="diagnostics">
    /// Appended with any role-mapping diagnostics (<c>HFC1022</c> fallback warnings,
    /// <c>HFC1023</c> for Dashboard fallback, <c>HFC1024</c> for unknown role values).
    /// </param>
    /// <returns>A RazorModel ready for the Emit stage.</returns>
    public static RazorModel Transform(DomainModel model, List<DiagnosticInfo> diagnostics) {
        ImmutableArray<ColumnModel>.Builder columnsBuilder = ImmutableArray.CreateBuilder<ColumnModel>();

        foreach (PropertyModel property in model.Properties) {
            if (property.IsUnsupported) {
                continue;
            }

            TypeCategory category = MapTypeCategory(property.TypeName);
            string? formatHint = GetFormatHint(property.TypeName, category);
            string header = ResolveHeader(property);

            columnsBuilder.Add(new ColumnModel(
                property.Name,
                header,
                category,
                formatHint,
                property.IsNullable,
                property.BadgeMappings,
                property.EnumMemberNames));
        }

        EquatableArray<ColumnModel> columns = new(columnsBuilder.ToImmutable());
        ProjectionRenderStrategy strategy = MapStrategy(model, diagnostics);
        EquatableArray<string> whenStates = SplitWhenStates(model.ProjectionRoleWhenState);
        EmitFallbackDiagnostics(model, strategy, columns, whenStates, diagnostics);
        string entityLabel = ResolveEntityLabel(model);
        string entityPluralLabel = ResolveEntityPluralLabel(model, entityLabel);

        return new RazorModel(
            model.TypeName,
            model.Namespace,
            model.BoundedContext,
            columns,
            strategy,
            whenStates,
            entityLabel,
            entityPluralLabel);
    }

    /// <summary>
    /// Diagnostics-free overload used by tests and cache-equality assertions; discards
    /// any Transform-stage HFC1023 / HFC1024 payload.
    /// </summary>
    public static RazorModel Transform(DomainModel model) {
        List<DiagnosticInfo> scratch = [];
        return Transform(model, scratch);
    }

    /// <summary>
    /// Story 4-1 T2.3 / T2.5 — maps the parsed <c>DomainModel.ProjectionRole</c> string
    /// to the Transform-stage <see cref="ProjectionRenderStrategy"/>. Exhaustive
    /// switch-expression form with a throwing default arm (ADR-052) so a future role
    /// added to the enum without a case fails the BUILD loudly rather than silently
    /// becoming Default. Unknown parsed values (e.g., an unsafe cast numeric) map to
    /// Default; HFC1024 was already emitted at Parse (see <c>AttributeParser</c>).
    /// Dashboard emits HFC1023 Information once per compilation per-type (D16 —
    /// <see cref="IIncrementalGenerator"/> per-input invocation model provides the
    /// "once per type" guarantee natively).
    /// </summary>
    private static ProjectionRenderStrategy MapStrategy(DomainModel model, List<DiagnosticInfo> diagnostics) {
        string? role = model.ProjectionRole;

        ProjectionRenderStrategy strategy = role switch {
            null => ProjectionRenderStrategy.Default,
            "" => ProjectionRenderStrategy.Default,
            "ActionQueue" => ProjectionRenderStrategy.ActionQueue,
            "StatusOverview" => ProjectionRenderStrategy.StatusOverview,
            "DetailRecord" => ProjectionRenderStrategy.DetailRecord,
            "Timeline" => ProjectionRenderStrategy.Timeline,
            "Dashboard" => ProjectionRenderStrategy.Dashboard,
            _ => ProjectionRenderStrategy.Default,
        };

        if (strategy == ProjectionRenderStrategy.Dashboard) {
            diagnostics.Add(new DiagnosticInfo(
                "HFC1023",
                string.Format(
                    "Dashboard projection rendering is deferred to Story 6-3 - {0} falls back to Default DataGrid rendering in v1.",
                    model.TypeName),
                "Info",
                string.Empty,
                0,
                0));
        }

        return strategy;
    }

    private static void EmitFallbackDiagnostics(
        DomainModel model,
        ProjectionRenderStrategy strategy,
        EquatableArray<ColumnModel> columns,
        EquatableArray<string> whenStates,
        List<DiagnosticInfo> diagnostics) {
        if (strategy == ProjectionRenderStrategy.ActionQueue
            && whenStates.Count > 0
            && ResolveStatusEnumColumn(columns) is null) {
            diagnostics.Add(CreateTransformDiagnostic(
                model,
                "HFC1022",
                string.Format(
                    "ProjectionRole.WhenState on {0} requires an enum status property - ActionQueue filtering falls back to the unfiltered item list at runtime.",
                    model.TypeName),
                "Warning"));
        }

        if (strategy == ProjectionRenderStrategy.Timeline
            && ResolveFirstDateTimeColumn(columns) is null) {
            diagnostics.Add(CreateTransformDiagnostic(
                model,
                "HFC1022",
                string.Format(
                    "ProjectionRole Timeline on {0} requires a DateTime/DateTimeOffset/DateOnly/TimeOnly property - timeline ordering falls back to declaration order.",
                    model.TypeName),
                "Warning"));
        }

        // Story 4-2 D6 / AC3 — emit HFC1025 once per enum column that carries partial
        // [ProjectionBadge] coverage on the projection. One diagnostic per (projection,
        // column) pair; the IIncrementalGenerator per-type invocation model provides the
        // "once per projection type per compile" guarantee natively.
        foreach (ColumnModel column in columns) {
            if (column.TypeCategory != TypeCategory.Enum) {
                continue;
            }

            int annotated = column.BadgeMappings.Count;
            int total = column.EnumMemberNames.Count;
            if (annotated == 0 || total == 0 || annotated >= total) {
                continue;
            }

            List<string> unannotated = new(total - annotated);
            HashSet<string> annotatedNames = new(StringComparer.Ordinal);
            foreach (BadgeMappingEntry mapping in column.BadgeMappings) {
                _ = annotatedNames.Add(mapping.EnumMemberName);
            }

            foreach (string memberName in column.EnumMemberNames) {
                if (!annotatedNames.Contains(memberName)) {
                    unannotated.Add(memberName);
                }
            }

            diagnostics.Add(CreateTransformDiagnostic(
                model,
                "HFC1025",
                string.Format(
                    "Enum property '{0}' on projection {1} has {2} of {3} members annotated with [ProjectionBadge] - unannotated members ({4}) render as plain text. Annotate every member or none for visual consistency.",
                    column.PropertyName,
                    model.TypeName,
                    annotated,
                    total,
                    string.Join(", ", unannotated)),
                "Info"));
        }

        // Story 4-3 D14 / D20 — emit HFC1027 once per projection type when it carries any
        // Collection-typed column. Per-projection deduped (one entry regardless of how many
        // Collection columns the projection declares).
        List<string>? collectionColumnNames = null;
        foreach (ColumnModel column in columns) {
            if (column.TypeCategory == TypeCategory.Collection) {
                (collectionColumnNames ??= []).Add(column.PropertyName);
            }
        }

        if (collectionColumnNames is not null) {
            diagnostics.Add(CreateTransformDiagnostic(
                model,
                "HFC1027",
                string.Format(
                    "Projection {0} has collection column(s) {1} which do not support automatic filtering. Filter affordance is omitted. Annotate with a slot-level override (Epic 6) for custom filter logic.",
                    model.TypeName,
                    string.Join(", ", collectionColumnNames)),
                "Info"));
        }
    }

    /// <summary>
    /// Story 4-1 T2.4 / D3 — canonical CSV split: trim each entry, drop empties, ordinal
    /// order preserved. Null/empty input yields an empty array.
    /// </summary>
    private static EquatableArray<string> SplitWhenStates(string? raw) {
        if (string.IsNullOrWhiteSpace(raw)) {
            return new EquatableArray<string>(ImmutableArray<string>.Empty);
        }

        string[] tokens = raw!.Split(WhenStateSeparator, StringSplitOptions.RemoveEmptyEntries);
        ImmutableArray<string>.Builder builder = ImmutableArray.CreateBuilder<string>();
        foreach (string token in tokens) {
            string trimmed = token.Trim();
            if (trimmed.Length > 0) {
                builder.Add(trimmed);
            }
        }

        return new EquatableArray<string>(builder.ToImmutable());
    }

    private static TypeCategory MapTypeCategory(string typeName) => typeName switch {
        "String" or "Guid" => TypeCategory.Text,
        "Int32" or "Int64" or "Decimal" or "Double" or "Single" => TypeCategory.Numeric,
        "Boolean" => TypeCategory.Boolean,
        "DateTime" or "DateTimeOffset" or "DateOnly" or "TimeOnly" => TypeCategory.DateTime,
        "Enum" => TypeCategory.Enum,
        "Collection" => TypeCategory.Collection,
        _ => TypeCategory.Unsupported,
    };

    private static string? GetFormatHint(string typeName, TypeCategory category) => typeName switch {
        "Int32" or "Int64" => "N0",
        "Decimal" or "Double" or "Single" => "N2",
        "Boolean" => "Yes/No",
        "DateTime" or "DateTimeOffset" or "DateOnly" => "d",
        "TimeOnly" => "t",
        "Enum" => "Humanize:30",
        "Guid" => "Truncate:8",
        "Collection" => "Count",
        _ => null,
    };

    private static string ResolveHeader(PropertyModel property) {
        // Priority 1: [Display(Name)] attribute value
        if (!string.IsNullOrEmpty(property.DisplayName)) {
            return property.DisplayName!;
        }

        // Priority 2: Humanized CamelCase
        string? humanized = CamelCaseHumanizer.Humanize(property.Name);
        if (!string.IsNullOrEmpty(humanized)) {
            return humanized!;
        }

        // Priority 3: Raw property name (fallback)
        return property.Name;
    }

    private static string ResolveEntityLabel(DomainModel model) {
        if (!string.IsNullOrWhiteSpace(model.DisplayName)) {
            return model.DisplayName!;
        }

        string baseName = StripProjectionSuffix(model.TypeName);
        string? humanized = CamelCaseHumanizer.Humanize(baseName);
        return string.IsNullOrWhiteSpace(humanized) ? baseName : humanized!;
    }

    private static string ResolveEntityPluralLabel(DomainModel model, string entityLabel) {
        if (!string.IsNullOrWhiteSpace(model.DisplayGroupName)) {
            return model.DisplayGroupName!;
        }

        if (!string.IsNullOrWhiteSpace(model.DisplayName)) {
            return model.DisplayName!;
        }

        string plural = entityLabel.ToLowerInvariant();
        return plural.EndsWith("s", StringComparison.Ordinal) ? plural : plural + "s";
    }

    private static string StripProjectionSuffix(string typeName)
        => typeName.EndsWith("Projection", StringComparison.Ordinal)
            ? typeName.Substring(0, typeName.Length - "Projection".Length)
            : typeName;

    private static ColumnModel? ResolveStatusEnumColumn(EquatableArray<ColumnModel> columns) {
        ColumnModel? badgedEnum = null;
        ColumnModel? firstEnum = null;

        foreach (ColumnModel column in columns) {
            if (column.TypeCategory != TypeCategory.Enum) {
                continue;
            }

            firstEnum ??= column;
            if (badgedEnum is null && column.BadgeMappings.Count > 0) {
                badgedEnum = column;
            }
        }

        return badgedEnum ?? firstEnum;
    }

    private static ColumnModel? ResolveFirstDateTimeColumn(EquatableArray<ColumnModel> columns) {
        foreach (ColumnModel column in columns) {
            if (column.TypeCategory == TypeCategory.DateTime) {
                return column;
            }
        }

        return null;
    }

    private static DiagnosticInfo CreateTransformDiagnostic(
        DomainModel model,
        string id,
        string message,
        string severity)
        => new(
            id,
            message,
            severity,
            model.SourceFilePath,
            model.SourceLine,
            model.SourceColumn);
}
