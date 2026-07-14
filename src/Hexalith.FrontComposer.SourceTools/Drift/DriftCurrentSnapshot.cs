using System.Collections.Immutable;

using Hexalith.FrontComposer.SourceTools.Parsing;

namespace Hexalith.FrontComposer.SourceTools.Drift;

internal sealed class DriftCurrentSnapshot(ImmutableArray<DriftCurrentContract> contracts) {
    internal ImmutableArray<DriftCurrentContract> Contracts { get; } = contracts;

    internal static DriftCurrentSnapshot From(
        ImmutableArray<ParseResult> projections,
        ImmutableArray<CommandParseResult> commands) {
        ImmutableArray<DriftCurrentContract>.Builder contracts = ImmutableArray.CreateBuilder<DriftCurrentContract>();

        foreach (ParseResult result in projections) {
            if (result.Model is not DomainModel model) {
                continue;
            }

            ImmutableArray<DriftCurrentProperty>.Builder properties = ImmutableArray.CreateBuilder<DriftCurrentProperty>();
            foreach (PropertyModel property in model.Properties) {
                properties.Add(new DriftCurrentProperty(
                    property.Name,
                    property.TypeName,
                    property.IsNullable,
                    null,
                    property.DisplayName,
                    property.Description,
                    property.ColumnPriority,
                    property.FieldGroup,
                    // Story 9-1 P-D3: serialize Default as null so a baseline missing the
                    // displayFormat field aligns with a current property using Default.
                    property.DisplayFormat == FieldDisplayFormat.Default ? null : property.DisplayFormat.ToString(),
                    property.RelativeTimeWindowDays,
                    BuildBadgeSignature(property.BadgeMappings)));
            }

            contracts.Add(new DriftCurrentContract(
                "projection",
                QualifiedType(model.Namespace, model.TypeName),
                model.BoundedContext ?? string.Empty,
                model.DisplayName,
                model.DisplayGroupName,
                model.ProjectionRole,
                null,
                null,
                null,
                model.EmptyStateCtaCommandTypeName,
                properties.ToImmutable(),
                model.SourceFilePath,
                model.SourceLine,
                model.SourceColumn));
        }

        foreach (CommandParseResult result in commands) {
            if (result.Model is not CommandModel model) {
                continue;
            }

            ImmutableArray<DriftCurrentProperty>.Builder properties = ImmutableArray.CreateBuilder<DriftCurrentProperty>();
            HashSet<string> derivable = new(model.DerivableProperties.Select(static p => p.Name), StringComparer.Ordinal);
            foreach (PropertyModel property in model.Properties) {
                properties.Add(new DriftCurrentProperty(
                    property.Name,
                    property.TypeName,
                    property.IsNullable,
                    derivable.Contains(property.Name),
                    property.DisplayName,
                    property.Description,
                    property.ColumnPriority,
                    property.FieldGroup,
                    property.DisplayFormat == FieldDisplayFormat.Default ? null : property.DisplayFormat.ToString(),
                    property.RelativeTimeWindowDays,
                    BuildBadgeSignature(property.BadgeMappings)));
            }

            // Story 9-1 P22: thread CommandModel source path/line/column so command drift
            // diagnostics get IDE squiggles like projection drift does.
            contracts.Add(new DriftCurrentContract(
                "command",
                QualifiedType(model.Namespace, model.TypeName),
                model.BoundedContext ?? string.Empty,
                model.DisplayName,
                null,
                null,
                model.IconName,
                model.IsDestructive,
                model.AuthorizationPolicyName,
                emptyStateCtaCommandTypeName: null,
                properties.ToImmutable(),
                model.SourceFilePath,
                model.SourceLine,
                model.SourceColumn));
        }

        return new DriftCurrentSnapshot(contracts.ToImmutable());
    }

    private static string QualifiedType(string @namespace, string typeName)
        => string.IsNullOrEmpty(@namespace) ? typeName : @namespace + "." + typeName;

    /// <summary>
    /// Story 9-1 P6 (AC7): produces the canonical badge signature used by drift comparison.
    /// Mirrors <see cref="DriftBaselineLoader"/>'s <c>ReadBadgeSignature</c> output shape so
    /// baseline and current-source signatures collide exactly when the mapping set matches,
    /// regardless of declaration order.
    /// </summary>
    private static string? BuildBadgeSignature(EquatableArray<BadgeMappingEntry> mappings) {
        if (mappings.Count == 0) {
            return null;
        }

        List<string> entries = new(mappings.Count);
        foreach (BadgeMappingEntry entry in mappings) {
            entries.Add(entry.EnumMemberName + "=" + entry.Slot);
        }

        entries.Sort(StringComparer.Ordinal);
        return string.Join(",", entries);
    }
}
