using System.Text;

using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Transforms;

namespace Hexalith.FrontComposer.SourceTools.Emitters;

/// <summary>
/// Story 4-1 T3.1 refactor — per-category column emitters extracted from the
/// pre-Story-4-1 <c>RazorEmitter</c>. Consumed by
/// <see cref="ProjectionRoleBodyEmitter.EmitStandardDataGrid"/>.
/// </summary>
internal static class ColumnEmitter {
    public static void EmitColumn(StringBuilder sb, ColumnModel col, string typeName, bool isDefaultSortColumn = false) {
        switch (col.TypeCategory) {
            case TypeCategory.Text:
                EmitTextColumn(sb, col, typeName, isDefaultSortColumn);
                break;
            case TypeCategory.Numeric:
                EmitNumericColumn(sb, col, typeName, isDefaultSortColumn);
                break;
            case TypeCategory.Boolean:
                EmitBooleanColumn(sb, col, typeName, isDefaultSortColumn);
                break;
            case TypeCategory.DateTime:
                EmitDateTimeColumn(sb, col, typeName, isDefaultSortColumn);
                break;
            case TypeCategory.Enum:
                EmitEnumColumn(sb, col, typeName, isDefaultSortColumn);
                break;
            case TypeCategory.Collection:
                EmitCollectionColumn(sb, col, typeName, isDefaultSortColumn);
                break;
        }
    }

    private static void EmitTextColumn(StringBuilder sb, ColumnModel col, string typeName, bool isDefaultSortColumn) {
        string formatHint = col.FormatHint ?? string.Empty;

        if (formatHint.StartsWith("Truncate:", StringComparison.Ordinal)) {
            string length = formatHint.Substring("Truncate:".Length);
            _ = sb.AppendLine("            // Guid column: " + col.PropertyName);
            _ = sb.AppendLine("            b.OpenComponent<PropertyColumn<" + typeName + ", string?>>(colSeq++);");
            _ = sb.AppendLine("            b.AddAttribute(colSeq++, \"Title\", \"" + RoleBodyHelpers.EscapeString(col.Header) + "\");");

            if (col.IsNullable) {
                _ = sb.AppendLine("            b.AddAttribute(colSeq++, \"Property\", (Expression<Func<" + typeName + ", string?>>)(x => x." + col.PropertyName + " == null ? \"\\u2014\" : x." + col.PropertyName + ".Value.ToString(\"N\").Substring(0, " + length + ")));");
            }
            else {
                _ = sb.AppendLine("            b.AddAttribute(colSeq++, \"Property\", (Expression<Func<" + typeName + ", string?>>)(x => x." + col.PropertyName + ".ToString(\"N\").Substring(0, " + length + ")));");
            }

            EmitSortAttributes(sb, col, typeName, isDefaultSortColumn);
            _ = sb.AppendLine("            b.CloseComponent();");
            return;
        }

        _ = sb.AppendLine("            // Text column: " + col.PropertyName);
        _ = sb.AppendLine("            b.OpenComponent<PropertyColumn<" + typeName + ", string?>>(colSeq++);");
        _ = sb.AppendLine("            b.AddAttribute(colSeq++, \"Title\", \"" + RoleBodyHelpers.EscapeString(col.Header) + "\");");
        _ = sb.AppendLine("            b.AddAttribute(colSeq++, \"Property\", (Expression<Func<" + typeName + ", string?>>)(x => x." + col.PropertyName + " ?? \"\\u2014\"));");
        EmitSortAttributes(sb, col, typeName, isDefaultSortColumn);
        _ = sb.AppendLine("            b.CloseComponent();");
    }

    private static void EmitNumericColumn(StringBuilder sb, ColumnModel col, string typeName, bool isDefaultSortColumn) {
        string format = col.FormatHint ?? "N0";
        _ = sb.AppendLine("            // Numeric column: " + col.PropertyName);
        _ = sb.AppendLine("            b.OpenComponent<PropertyColumn<" + typeName + ", string?>>(colSeq++);");
        _ = sb.AppendLine("            b.AddAttribute(colSeq++, \"Title\", \"" + RoleBodyHelpers.EscapeString(col.Header) + "\");");

        if (col.IsNullable) {
            _ = sb.AppendLine("            b.AddAttribute(colSeq++, \"Property\", (Expression<Func<" + typeName + ", string?>>)(x => x." + col.PropertyName + ".HasValue ? x." + col.PropertyName + ".Value.ToString(\"" + format + "\", CultureInfo.CurrentCulture) : \"\\u2014\"));");
        }
        else {
            _ = sb.AppendLine("            b.AddAttribute(colSeq++, \"Property\", (Expression<Func<" + typeName + ", string?>>)(x => x." + col.PropertyName + ".ToString(\"" + format + "\", CultureInfo.CurrentCulture)));");
        }

        _ = sb.AppendLine("            b.AddAttribute(colSeq++, \"Class\", \"fc-col-numeric\");");
        EmitSortAttributes(sb, col, typeName, isDefaultSortColumn);
        _ = sb.AppendLine("            b.CloseComponent();");
    }

    private static void EmitBooleanColumn(StringBuilder sb, ColumnModel col, string typeName, bool isDefaultSortColumn) {
        _ = sb.AppendLine("            // Boolean column: " + col.PropertyName);
        _ = sb.AppendLine("            b.OpenComponent<PropertyColumn<" + typeName + ", string?>>(colSeq++);");
        _ = sb.AppendLine("            b.AddAttribute(colSeq++, \"Title\", \"" + RoleBodyHelpers.EscapeString(col.Header) + "\");");

        if (col.IsNullable) {
            _ = sb.AppendLine("            b.AddAttribute(colSeq++, \"Property\", (Expression<Func<" + typeName + ", string?>>)(x => x." + col.PropertyName + ".HasValue ? (x." + col.PropertyName + ".Value ? \"Yes\" : \"No\") : \"\\u2014\"));");
        }
        else {
            _ = sb.AppendLine("            b.AddAttribute(colSeq++, \"Property\", (Expression<Func<" + typeName + ", string?>>)(x => x." + col.PropertyName + " ? \"Yes\" : \"No\"));");
        }

        EmitSortAttributes(sb, col, typeName, isDefaultSortColumn);
        _ = sb.AppendLine("            b.CloseComponent();");
    }

    private static void EmitDateTimeColumn(StringBuilder sb, ColumnModel col, string typeName, bool isDefaultSortColumn) {
        string format = col.FormatHint ?? "d";
        _ = sb.AppendLine("            // DateTime column: " + col.PropertyName);
        _ = sb.AppendLine("            b.OpenComponent<PropertyColumn<" + typeName + ", string?>>(colSeq++);");
        _ = sb.AppendLine("            b.AddAttribute(colSeq++, \"Title\", \"" + RoleBodyHelpers.EscapeString(col.Header) + "\");");

        if (col.IsNullable) {
            _ = sb.AppendLine("            b.AddAttribute(colSeq++, \"Property\", (Expression<Func<" + typeName + ", string?>>)(x => x." + col.PropertyName + ".HasValue ? x." + col.PropertyName + ".Value.ToString(\"" + format + "\", CultureInfo.CurrentCulture) : \"\\u2014\"));");
        }
        else {
            _ = sb.AppendLine("            b.AddAttribute(colSeq++, \"Property\", (Expression<Func<" + typeName + ", string?>>)(x => x." + col.PropertyName + ".ToString(\"" + format + "\", CultureInfo.CurrentCulture)));");
        }

        EmitSortAttributes(sb, col, typeName, isDefaultSortColumn);
        _ = sb.AppendLine("            b.CloseComponent();");
    }

    private static void EmitEnumColumn(StringBuilder sb, ColumnModel col, string typeName, bool isDefaultSortColumn) {
        _ = sb.AppendLine("            // Enum column: " + col.PropertyName);
        _ = sb.AppendLine("            b.OpenComponent<PropertyColumn<" + typeName + ", string?>>(colSeq++);");
        _ = sb.AppendLine("            b.AddAttribute(colSeq++, \"Title\", \"" + RoleBodyHelpers.EscapeString(col.Header) + "\");");

        if (col.IsNullable) {
            _ = sb.AppendLine("            b.AddAttribute(colSeq++, \"Property\", (Expression<Func<" + typeName + ", string?>>)(x => x." + col.PropertyName + ".HasValue ? Truncate(HumanizeEnumLabel(x." + col.PropertyName + ".Value.ToString()), 30) : \"\\u2014\"));");
        }
        else {
            _ = sb.AppendLine("            b.AddAttribute(colSeq++, \"Property\", (Expression<Func<" + typeName + ", string?>>)(x => Truncate(HumanizeEnumLabel(x." + col.PropertyName + ".ToString()), 30)));");
        }

        if (col.BadgeMappings.Count > 0) {
            EmitEnumBadgeChildContent(sb, col, typeName);
        }

        EmitSortAttributes(sb, col, typeName, isDefaultSortColumn);
        _ = sb.AppendLine("            b.CloseComponent();");
    }

    /// <summary>
    /// Story 4-2 D1 / D5 / D8 / AC1 / AC3 — emits the <c>ChildContent</c> render fragment that
    /// renders <c>FcStatusBadge</c> for annotated enum members and falls back to humanized
    /// text for unannotated members (partial coverage) or null values. The <c>Property</c>
    /// lambda on the column is preserved verbatim so DataGrid sort / filter / default-aria
    /// paths continue to operate on the text representation (unchanged from Story 1-5).
    /// </summary>
    private static void EmitEnumBadgeChildContent(StringBuilder sb, ColumnModel col, string typeName) {
        string propertyAccess = col.IsNullable
            ? "item." + col.PropertyName + ".Value"
            : "item." + col.PropertyName;
        string headerLiteral = "\"" + RoleBodyHelpers.EscapeString(col.Header) + "\"";

        _ = sb.AppendLine("            b.AddAttribute(colSeq++, \"ChildContent\", (RenderFragment<" + typeName + ">)(item => (RenderTreeBuilder rb) =>");
        _ = sb.AppendLine("            {");

        if (col.IsNullable) {
            _ = sb.AppendLine("                if (!item." + col.PropertyName + ".HasValue)");
            _ = sb.AppendLine("                {");
            _ = sb.AppendLine("                    rb.AddContent(0, \"\\u2014\");");
            _ = sb.AppendLine("                    return;");
            _ = sb.AppendLine("                }");
            _ = sb.AppendLine();
        }

        _ = sb.AppendLine("                var _memberName = " + propertyAccess + ".ToString();");
        _ = sb.AppendLine("                var _label = HumanizeEnumLabel(_memberName);");
        EmitBadgeSwitch(sb, col, headerLiteral);
        _ = sb.AppendLine("            }));");
    }

    /// <summary>
    /// Story 4-2 D5 — emits the inline switch over enum member names. Mapped members emit an
    /// <c>FcStatusBadge</c> with the resolved slot; unmapped members fall back to
    /// <c>rb.AddContent(..., _label)</c>.
    /// </summary>
    private static void EmitBadgeSwitch(StringBuilder sb, ColumnModel col, string headerLiteral) {
        _ = sb.AppendLine("                switch (_memberName)");
        _ = sb.AppendLine("                {");

        foreach (BadgeMappingEntry mapping in col.BadgeMappings) {
            string memberLiteral = "\"" + RoleBodyHelpers.EscapeString(mapping.EnumMemberName) + "\"";
            _ = sb.AppendLine("                    case " + memberLiteral + ":");
            _ = sb.AppendLine("                        rb.OpenComponent<global::Hexalith.FrontComposer.Shell.Components.Badges.FcStatusBadge>(0);");
            _ = sb.AppendLine("                        rb.AddAttribute(1, \"Slot\", global::Hexalith.FrontComposer.Contracts.Attributes.BadgeSlot." + mapping.Slot + ");");
            _ = sb.AppendLine("                        rb.AddAttribute(2, \"Label\", _label);");
            _ = sb.AppendLine("                        rb.AddAttribute(3, \"ColumnHeader\", " + headerLiteral + ");");
            _ = sb.AppendLine("                        rb.CloseComponent();");
            _ = sb.AppendLine("                        break;");
        }

        _ = sb.AppendLine("                    default:");
        _ = sb.AppendLine("                        rb.AddContent(10, _label);");
        _ = sb.AppendLine("                        break;");
        _ = sb.AppendLine("                }");
    }

    private static void EmitCollectionColumn(StringBuilder sb, ColumnModel col, string typeName, bool isDefaultSortColumn) {
        _ = sb.AppendLine("            // Collection column: " + col.PropertyName);
        _ = sb.AppendLine("            b.OpenComponent<PropertyColumn<" + typeName + ", string?>>(colSeq++);");
        _ = sb.AppendLine("            b.AddAttribute(colSeq++, \"Title\", \"" + RoleBodyHelpers.EscapeString(col.Header) + "\");");
        _ = sb.AppendLine("            b.AddAttribute(colSeq++, \"Property\", (Expression<Func<" + typeName + ", string?>>)(x => x." + col.PropertyName + " == null ? \"\\u2014\" : System.Linq.Enumerable.Count(x." + col.PropertyName + ").ToString() + \" items\"));");
        EmitSortAttributes(sb, col, typeName, isDefaultSortColumn);
        _ = sb.AppendLine("            b.CloseComponent();");
    }

    private static void EmitSortAttributes(StringBuilder sb, ColumnModel col, string typeName, bool isDefaultSortColumn) {
        if (!isDefaultSortColumn) {
            return;
        }

        _ = sb.AppendLine("            b.AddAttribute(colSeq++, \"Sortable\", true);");
        _ = sb.AppendLine("            b.AddAttribute(colSeq++, \"IsDefaultSortColumn\", true);");
        _ = sb.AppendLine("            b.AddAttribute(colSeq++, \"InitialSortDirection\", DataGridSortDirection.Descending);");
        _ = sb.AppendLine("            b.AddAttribute(colSeq++, \"SortBy\", " + BuildSortExpression(col, typeName) + ");");
    }

    private static string BuildSortExpression(ColumnModel col, string typeName)
        => col.TypeCategory == TypeCategory.Collection
            ? "GridSort<" + typeName + ">.ByDescending(x => x." + col.PropertyName + " == null ? 0 : System.Linq.Enumerable.Count(x." + col.PropertyName + "))"
            : "GridSort<" + typeName + ">.ByDescending(x => x." + col.PropertyName + ")";
}
