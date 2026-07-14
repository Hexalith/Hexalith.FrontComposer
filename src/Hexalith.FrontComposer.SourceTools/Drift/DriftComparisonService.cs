using System.Collections.Immutable;
using System.Globalization;

using Microsoft.CodeAnalysis;

namespace Hexalith.FrontComposer.SourceTools.Drift;

internal sealed class DriftComparisonService {
    // Story 9-1 P27: tightened from `public` to `internal` (see DriftComparisonResult).
    internal static DriftComparisonResult Compare(DriftCurrentSnapshot current, DriftBaselineSet baseline)
        => Compare(current, baseline, DriftConstants.DefaultMaxDiagnostics, DiagnosticSeverity.Warning);

    internal static DriftComparisonResult Compare(
        DriftCurrentSnapshot current,
        DriftBaselineSet baseline,
        int maxDiagnostics,
        DiagnosticSeverity severity) {
        ImmutableArray<DriftDiagnosticFact>.Builder facts = ImmutableArray.CreateBuilder<DriftDiagnosticFact>();
        Dictionary<string, DriftCurrentContract> currentByType = new(StringComparer.Ordinal);
        foreach (DriftCurrentContract contract in current.Contracts) {
            currentByType[contract.IdentityWithoutContext] = contract;
        }

        HashSet<string> baselineKeys = new(StringComparer.Ordinal);
        foreach (DriftBaselineContract baselineContract in baseline.Contracts) {
            _ = baselineKeys.Add(baselineContract.IdentityWithoutContext);
            if (!currentByType.TryGetValue(baselineContract.IdentityWithoutContext, out DriftCurrentContract? currentContract)) {
                facts.Add(DriftDiagnosticFact.Structural(
                    "RemovedDeclaration",
                    baselineContract,
                    null,
                    null,
                    "What: structural drift removed declaration. Expected: declaration " + DriftSanitizer.Safe(baselineContract.Type) + ". Got: declaration not found. Fix: restore the declaration or update the checked-in generated UI baseline. DocsLink: " + Docs(DriftConstants.StructuralDriftId),
                    severity));
                continue;
            }

            CompareContract(baselineContract, currentContract, severity, facts);
        }

        foreach (DriftCurrentContract currentContract in current.Contracts) {
            if (baselineKeys.Contains(currentContract.IdentityWithoutContext)) {
                continue;
            }

            facts.Add(DriftDiagnosticFact.Structural(
                "AddedDeclaration",
                null,
                currentContract,
                null,
                "What: structural drift added declaration " + DriftSanitizer.Safe(currentContract.Type) + ". Expected: declaration from baseline. Got: new generated UI declaration. Fix: update the checked-in generated UI baseline if intentional. DocsLink: " + Docs(DriftConstants.StructuralDriftId),
                severity));
        }

        var sorted = facts
            .OrderBy(static f => f.SortKey, StringComparer.Ordinal)
            .ToImmutableArray();

        if (sorted.Length <= maxDiagnostics) {
            return new DriftComparisonResult(sorted);
        }

        ImmutableArray<DriftDiagnosticFact>.Builder capped = ImmutableArray.CreateBuilder<DriftDiagnosticFact>();
        capped.AddRange(sorted.Take(maxDiagnostics));
        capped.Add(DriftDiagnosticFact.Truncation(sorted.Length - maxDiagnostics));
        return new DriftComparisonResult(capped.ToImmutable());
    }

    private static void CompareContract(
        DriftBaselineContract baseline,
        DriftCurrentContract current,
        DiagnosticSeverity severity,
        ImmutableArray<DriftDiagnosticFact>.Builder facts) {
        if (!string.Equals(baseline.BoundedContext, current.BoundedContext, StringComparison.Ordinal)) {
            facts.Add(DriftDiagnosticFact.Structural(
                "BoundedContextChanged",
                baseline,
                current,
                null,
                "What: bounded context drift on " + DriftSanitizer.Safe(current.Type) + ". Expected: " + DriftSanitizer.Safe(baseline.BoundedContext) + ". Got: " + DriftSanitizer.Safe(current.BoundedContext) + ". Fix: reconcile the generated UI baseline; navigation grouping, generated registration, persisted session grouping, MCP resource grouping, and badge/action queue grouping may be affected. DocsLink: " + Docs(DriftConstants.StructuralDriftId),
                severity));
        }

        CompareContractMetadata(baseline, current, severity, facts);

        var baselineProperties = baseline.Properties.ToDictionary(static p => p.Name, StringComparer.Ordinal);
        var currentProperties = current.Properties.ToDictionary(static p => p.Name, StringComparer.Ordinal);
        List<DriftBaselineProperty> removed = [.. baseline.Properties.Where(p => !currentProperties.ContainsKey(p.Name))];
        List<DriftCurrentProperty> added = [.. current.Properties.Where(p => !baselineProperties.ContainsKey(p.Name))];

        if (removed.Count == 1
            && added.Count == 1
            && string.Equals(removed[0].Category, added[0].Category, StringComparison.Ordinal)) {
            string id = DriftConstants.StructuralDriftId;
            string message = "Property '" + DriftSanitizer.Safe(removed[0].Name) + "' was expected on " + DriftSanitizer.Safe(baseline.Type) + " but not found. '" + DriftSanitizer.Safe(added[0].Name) + "' was added. If this is a rename, update the generated output. See " + id + ". Expected: " + DriftSanitizer.Safe(removed[0].Category) + ". Got: " + DriftSanitizer.Safe(added[0].Category) + ". Fix: update the checked-in generated UI baseline. DocsLink: " + Docs(id);
            facts.Add(DriftDiagnosticFact.Structural("Rename", baseline, current, added[0].Name, message, severity));
        }
        else {
            foreach (DriftBaselineProperty property in removed) {
                facts.Add(DriftDiagnosticFact.Structural(
                    "RemovedProperty",
                    baseline,
                    current,
                    property.Name,
                    "What: structural drift removed property. Expected: property '" + DriftSanitizer.Safe(property.Name) + "' on " + DriftSanitizer.Safe(baseline.Type) + ". Got: property not found. Fix: restore the member or update the checked-in generated UI baseline. Affected surface: " + SurfaceFor(baseline.Family, property.Category) + ". DocsLink: " + Docs(DriftConstants.StructuralDriftId),
                    severity));
            }

            foreach (DriftCurrentProperty property in added) {
                facts.Add(DriftDiagnosticFact.Structural(
                    "AddedProperty",
                    baseline,
                    current,
                    property.Name,
                    "What: structural drift added property. Expected: no property '" + DriftSanitizer.Safe(property.Name) + "' in baseline. Got: property added on " + DriftSanitizer.Safe(current.Type) + ". Fix: update the checked-in generated UI baseline if intentional. Affected surface: " + SurfaceFor(current.Family, property.Category) + ". DocsLink: " + Docs(DriftConstants.StructuralDriftId),
                    severity));
            }
        }

        foreach (KeyValuePair<string, DriftBaselineProperty> kvp in baselineProperties.OrderBy(static p => p.Key, StringComparer.Ordinal)) {
            if (!currentProperties.TryGetValue(kvp.Key, out DriftCurrentProperty? currentProperty)) {
                continue;
            }

            CompareProperty(baseline, current, kvp.Value, currentProperty, severity, facts);
        }
    }

    private static void CompareContractMetadata(
        DriftBaselineContract baseline,
        DriftCurrentContract current,
        DiagnosticSeverity severity,
        ImmutableArray<DriftDiagnosticFact>.Builder facts) {
        AddIfChanged("Display.Name", baseline.DisplayName, current.DisplayName);
        AddIfChanged("Display.GroupName", baseline.DisplayGroupName, current.DisplayGroupName);
        AddIfChanged("ProjectionRole", baseline.Role, current.Role);
        AddIfChanged("Icon", baseline.Icon, current.Icon);
        AddIfChanged("RequiresPolicy", baseline.RequiresPolicy, current.RequiresPolicy);
        // Story 9-1 P6 (AC7): empty-state CTA drift is contract-level metadata.
        AddIfChanged("ProjectionEmptyStateCta", baseline.EmptyStateCtaCommandTypeName, current.EmptyStateCtaCommandTypeName);

        // Story 9-1 P9: symmetric Destructive comparison. Previously fired only when
        // baseline.Destructive was non-null, so adding a [Destructive] flag against a null
        // baseline (a fresh declaration becoming destructive) was silently ignored.
        if (baseline.Destructive != current.Destructive) {
            facts.Add(DriftDiagnosticFact.Metadata(
                "Destructive",
                baseline,
                current,
                null,
                "What: metadata drift changed Destructive on " + DriftSanitizer.Safe(current.Type)
                    + ". Expected: " + (baseline.Destructive?.ToString(CultureInfo.InvariantCulture) ?? "<none>")
                    + ". Got: " + (current.Destructive?.ToString(CultureInfo.InvariantCulture) ?? "<none>")
                    + ". Fix: update source metadata or the checked-in generated UI baseline. DocsLink: " + Docs(DriftConstants.MetadataDriftId),
                severity));
        }

        void AddIfChanged(string kind, string? expected, string? got) {
            // Story 9-1 P9: drop the `expected is null` short-circuit. A null→value transition
            // (new metadata added in source against a null baseline) IS drift; AC7 must alert
            // on metadata addition, not just removal/change.
            if (string.Equals(expected, got, StringComparison.Ordinal)) {
                return;
            }

            facts.Add(DriftDiagnosticFact.Metadata(
                kind,
                baseline,
                current,
                null,
                "What: metadata drift changed " + kind + " on " + DriftSanitizer.Safe(current.Type)
                    + ". Expected: " + DriftSanitizer.Safe(expected ?? "<none>")
                    + ". Got: " + DriftSanitizer.Safe(got ?? "<none>")
                    + ". Fix: update source metadata or the checked-in generated UI baseline. DocsLink: " + Docs(DriftConstants.MetadataDriftId),
                severity));
        }
    }

    private static void CompareProperty(
        DriftBaselineContract baseline,
        DriftCurrentContract current,
        DriftBaselineProperty expected,
        DriftCurrentProperty got,
        DiagnosticSeverity severity,
        ImmutableArray<DriftDiagnosticFact>.Builder facts) {
        if (!string.Equals(expected.Category, got.Category, StringComparison.Ordinal)) {
            facts.Add(DriftDiagnosticFact.Structural(
                "TypeCategoryChanged",
                baseline,
                current,
                got.Name,
                "What: structural drift changed property category for '" + DriftSanitizer.Safe(got.Name) + "'. Expected: " + DriftSanitizer.Safe(expected.Category) + ". Got: " + DriftSanitizer.Safe(got.Category) + ". Fix: update source or reconcile the checked-in generated UI baseline. Affected surface: " + SurfaceFor(current.Family, got.Category) + ", form input, DataGrid column, filter, badge/format, MCP descriptor metadata, currency formatting. DocsLink: " + Docs(DriftConstants.StructuralDriftId),
                severity));
        }

        if (expected.Nullable != got.Nullable) {
            string hint = expected.Nullable && !got.Nullable ? "required/breaking/tightened" : "nullable";
            facts.Add(DriftDiagnosticFact.Structural(
                "NullabilityChanged",
                baseline,
                current,
                got.Name,
                "What: structural drift changed nullability for '" + DriftSanitizer.Safe(got.Name) + "'. Expected: nullable=" + expected.Nullable.ToString(CultureInfo.InvariantCulture) + ". Got: nullable=" + got.Nullable.ToString(CultureInfo.InvariantCulture) + " (" + hint + "). Fix: update source or reconcile the checked-in generated UI baseline. Affected surface: form input, DataGrid column, filter behavior, MCP descriptor metadata. DocsLink: " + Docs(DriftConstants.StructuralDriftId),
                severity));
        }

        AddMetadataIfChanged("Display.Name", expected.DisplayName, got.DisplayName);
        AddMetadataIfChanged("Description", expected.Description, got.Description);
        AddMetadataIfChanged("ColumnPriority", expected.ColumnPriority?.ToString(CultureInfo.InvariantCulture), got.ColumnPriority?.ToString(CultureInfo.InvariantCulture));

        // Story 9-1 P20: previously emitted both ProjectionFieldGroup AND Display.GroupName for
        // the same source change, doubling the diagnostic count toward the 50-cap. Single
        // diagnostic now carries both surface labels in the message text.
        if (!string.Equals(expected.FieldGroup, got.FieldGroup, StringComparison.Ordinal)) {
            facts.Add(DriftDiagnosticFact.Metadata(
                "ProjectionFieldGroup",
                baseline,
                current,
                got.Name,
                "What: metadata drift changed ProjectionFieldGroup for '" + DriftSanitizer.Safe(got.Name)
                    + "' on " + DriftSanitizer.Safe(current.Type)
                    + ". Expected: " + DriftSanitizer.Safe(expected.FieldGroup ?? "<none>")
                    + ". Got: " + DriftSanitizer.Safe(got.FieldGroup ?? "<none>")
                    + ". Fix: update source metadata or reconcile the checked-in generated UI baseline. Affected surface: ProjectionFieldGroup, Display.GroupName, DataGrid grouping, detail field, MCP descriptor metadata. DocsLink: " + Docs(DriftConstants.MetadataDriftId),
                severity));
        }

        AddMetadataIfChanged("DisplayFormat", expected.DisplayFormat, got.DisplayFormat);

        // Story 9-1 P6 (AC7): drift coverage for relative-time window and badge mappings.
        AddMetadataIfChanged(
            "RelativeTime",
            expected.RelativeTimeWindowDays?.ToString(CultureInfo.InvariantCulture),
            got.RelativeTimeWindowDays?.ToString(CultureInfo.InvariantCulture));
        AddMetadataIfChanged("ProjectionBadge", expected.BadgeSignature, got.BadgeSignature);

        // Story 9-1 P9: symmetric Derivable comparison.
        if (expected.Derivable != got.Derivable) {
            AddMetadata(
                "Derivable",
                expected.Derivable?.ToString(CultureInfo.InvariantCulture) ?? "<none>",
                got.Derivable?.ToString(CultureInfo.InvariantCulture) ?? "<none>");
        }

        void AddMetadataIfChanged(string kind, string? expectedValue, string? gotValue) {
            // Story 9-1 P9: drop the `expectedValue is null` short-circuit.
            if (string.Equals(expectedValue, gotValue, StringComparison.Ordinal)) {
                return;
            }

            AddMetadata(kind, expectedValue, gotValue);
        }

        void AddMetadata(string kind, string? expectedValue, string? gotValue) => facts.Add(DriftDiagnosticFact.Metadata(
                kind,
                baseline,
                current,
                got.Name,
                "What: metadata drift changed " + kind + " for '" + DriftSanitizer.Safe(got.Name) + "' on " + DriftSanitizer.Safe(current.Type) + ". Expected: " + DriftSanitizer.Safe(expectedValue ?? "<none>") + ". Got: " + DriftSanitizer.Safe(gotValue ?? "<none>") + ". Fix: update source metadata or reconcile the checked-in generated UI baseline. Affected surface: renderer-impacting metadata, DataGrid, detail, MCP descriptor metadata. DocsLink: " + Docs(DriftConstants.MetadataDriftId),
                severity));
    }

    private static string SurfaceFor(string family, string category) {
        if (family == "command") {
            return "generated form input and command registration";
        }

        if (category is "Collection" or "Unsupported") {
            return "unsupported placeholder, detail field, and MCP projection field metadata";
        }

        if (category == "Enum") {
            return "DataGrid column, filter behavior, badge/format behavior, detail field, and MCP projection field metadata";
        }

        if (category.Contains("Date", StringComparison.Ordinal) || category.Contains("Time", StringComparison.Ordinal)) {
            return "DataGrid column, detail field, filter behavior, format behavior, and MCP projection field metadata";
        }

        return "DataGrid column, detail field, filter behavior, and MCP projection field metadata";
    }

    private static string Docs(string id) => "https://hexalith.github.io/FrontComposer/diagnostics/" + id;
}
