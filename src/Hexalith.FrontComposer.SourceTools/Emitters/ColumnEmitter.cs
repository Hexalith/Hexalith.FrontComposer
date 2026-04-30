using System.Collections.Generic;
using System.Globalization;
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
    /// <summary>
    /// Resource key for the localised fail-soft label rendered when an enum runtime value is
    /// outside the declared member set (unsafe cast). Kept here so every emit path agrees on
    /// the key name without embedding it as a magic string at multiple sites.
    /// </summary>
    internal const string UnknownStateFallbackResourceKey = "StatusBadgeUnknownStateFallback";

    /// <summary>
    /// Field name of the <c>IStringLocalizer&lt;FcShellResources&gt;</c> injected into generated
    /// views that render any badge-annotated enum column (Story 4-2 RF3 / T3.2). Exposed as a
    /// constant so both <see cref="RazorEmitter"/> (declares the property) and the badge-dispatch
    /// helpers (reference it) stay in lock-step.
    /// </summary>
    internal const string ShellLocalizerFieldName = "FcShellLocalizer";

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
            case TypeCategory.Unsupported:
                EmitUnsupportedColumn(sb, col, typeName);
                break;
        }
    }

    private static void EmitTextColumn(StringBuilder sb, ColumnModel col, string typeName, bool isDefaultSortColumn) {
        string formatHint = col.FormatHint ?? string.Empty;

        if (formatHint.StartsWith("Truncate:", StringComparison.Ordinal)) {
            string length = formatHint.Substring("Truncate:".Length);
            _ = sb.AppendLine("            // Guid column: " + col.PropertyName);
            EmitSlotColumnBranch(sb, col, typeName, isDefaultSortColumn);
            _ = sb.AppendLine("                b.OpenComponent<PropertyColumn<" + typeName + ", string?>>(colSeq++);");
            _ = sb.AppendLine("                b.AddAttribute(colSeq++, \"Title\", \"" + RoleBodyHelpers.EscapeString(col.Header) + "\");");
            EmitHeaderDescription(sb, col, indent: "                ");

            if (col.IsNullable) {
                _ = sb.AppendLine("                b.AddAttribute(colSeq++, \"Property\", (Expression<Func<" + typeName + ", string?>>)(x => x." + col.PropertyName + " == null ? \"\\u2014\" : x." + col.PropertyName + ".Value.ToString(\"N\").Substring(0, " + length + ")));");
            }
            else {
                _ = sb.AppendLine("                b.AddAttribute(colSeq++, \"Property\", (Expression<Func<" + typeName + ", string?>>)(x => x." + col.PropertyName + ".ToString(\"N\").Substring(0, " + length + ")));");
            }

            EmitSortAttributes(sb, col, typeName, isDefaultSortColumn, indent: "                ");
            _ = sb.AppendLine("                b.CloseComponent();");
            _ = sb.AppendLine("            }");
            return;
        }

        _ = sb.AppendLine("            // Text column: " + col.PropertyName);
        EmitSlotColumnBranch(sb, col, typeName, isDefaultSortColumn);
        _ = sb.AppendLine("                b.OpenComponent<PropertyColumn<" + typeName + ", string?>>(colSeq++);");
        _ = sb.AppendLine("                b.AddAttribute(colSeq++, \"Title\", \"" + RoleBodyHelpers.EscapeString(col.Header) + "\");");
        EmitHeaderDescription(sb, col, indent: "                ");
        _ = sb.AppendLine("                b.AddAttribute(colSeq++, \"Property\", (Expression<Func<" + typeName + ", string?>>)(x => x." + col.PropertyName + " ?? \"\\u2014\"));");
        EmitSortAttributes(sb, col, typeName, isDefaultSortColumn, indent: "                ");
        _ = sb.AppendLine("                b.CloseComponent();");
        _ = sb.AppendLine("            }");
    }

    private static void EmitNumericColumn(StringBuilder sb, ColumnModel col, string typeName, bool isDefaultSortColumn) {
        string format = col.FormatHint ?? "N0";
        _ = sb.AppendLine("            // Numeric column: " + col.PropertyName);
        EmitSlotColumnBranch(sb, col, typeName, isDefaultSortColumn, cssClass: "fc-col-numeric");
        _ = sb.AppendLine("                b.OpenComponent<PropertyColumn<" + typeName + ", string?>>(colSeq++);");
        _ = sb.AppendLine("                b.AddAttribute(colSeq++, \"Title\", \"" + RoleBodyHelpers.EscapeString(col.Header) + "\");");
        EmitHeaderDescription(sb, col, indent: "                ");

        if (col.IsNullable) {
            _ = sb.AppendLine("                b.AddAttribute(colSeq++, \"Property\", (Expression<Func<" + typeName + ", string?>>)(x => x." + col.PropertyName + ".HasValue ? x." + col.PropertyName + ".Value.ToString(\"" + format + "\", CultureInfo.CurrentCulture) : \"\\u2014\"));");
        }
        else {
            _ = sb.AppendLine("                b.AddAttribute(colSeq++, \"Property\", (Expression<Func<" + typeName + ", string?>>)(x => x." + col.PropertyName + ".ToString(\"" + format + "\", CultureInfo.CurrentCulture)));");
        }

        _ = sb.AppendLine("                b.AddAttribute(colSeq++, \"Class\", \"fc-col-numeric\");");
        EmitSortAttributes(sb, col, typeName, isDefaultSortColumn, indent: "                ");
        _ = sb.AppendLine("                b.CloseComponent();");
        _ = sb.AppendLine("            }");
    }

    private static void EmitBooleanColumn(StringBuilder sb, ColumnModel col, string typeName, bool isDefaultSortColumn) {
        _ = sb.AppendLine("            // Boolean column: " + col.PropertyName);
        EmitSlotColumnBranch(sb, col, typeName, isDefaultSortColumn);
        _ = sb.AppendLine("                b.OpenComponent<PropertyColumn<" + typeName + ", string?>>(colSeq++);");
        _ = sb.AppendLine("                b.AddAttribute(colSeq++, \"Title\", \"" + RoleBodyHelpers.EscapeString(col.Header) + "\");");
        EmitHeaderDescription(sb, col, indent: "                ");

        if (col.IsNullable) {
            _ = sb.AppendLine("                b.AddAttribute(colSeq++, \"Property\", (Expression<Func<" + typeName + ", string?>>)(x => x." + col.PropertyName + ".HasValue ? (x." + col.PropertyName + ".Value ? \"Yes\" : \"No\") : \"\\u2014\"));");
        }
        else {
            _ = sb.AppendLine("                b.AddAttribute(colSeq++, \"Property\", (Expression<Func<" + typeName + ", string?>>)(x => x." + col.PropertyName + " ? \"Yes\" : \"No\"));");
        }

        EmitSortAttributes(sb, col, typeName, isDefaultSortColumn, indent: "                ");
        _ = sb.AppendLine("                b.CloseComponent();");
        _ = sb.AppendLine("            }");
    }

    private static void EmitDateTimeColumn(StringBuilder sb, ColumnModel col, string typeName, bool isDefaultSortColumn) {
        string format = col.FormatHint ?? "d";
        _ = sb.AppendLine("            // DateTime column: " + col.PropertyName);
        EmitSlotColumnBranch(sb, col, typeName, isDefaultSortColumn);
        _ = sb.AppendLine("                b.OpenComponent<PropertyColumn<" + typeName + ", string?>>(colSeq++);");
        _ = sb.AppendLine("                b.AddAttribute(colSeq++, \"Title\", \"" + RoleBodyHelpers.EscapeString(col.Header) + "\");");
        EmitHeaderDescription(sb, col, indent: "                ");

        if (col.DisplayFormat == FieldDisplayFormat.RelativeTime) {
            int windowDays = col.RelativeTimeWindowDays ?? 7;
            if (col.IsNullable) {
                _ = sb.AppendLine("                b.AddAttribute(colSeq++, \"Property\", (Expression<Func<" + typeName + ", string?>>)(x => x." + col.PropertyName + ".HasValue ? FormatRelativeTime(x." + col.PropertyName + ".Value, relativeNow, " + windowDays.ToString(System.Globalization.CultureInfo.InvariantCulture) + ") : \"\\u2014\"));");
            }
            else {
                _ = sb.AppendLine("                b.AddAttribute(colSeq++, \"Property\", (Expression<Func<" + typeName + ", string?>>)(x => FormatRelativeTime(x." + col.PropertyName + ", relativeNow, " + windowDays.ToString(System.Globalization.CultureInfo.InvariantCulture) + ")));");
            }

            EmitSortAttributes(sb, col, typeName, isDefaultSortColumn, indent: "                ");
            _ = sb.AppendLine("                b.CloseComponent();");
            _ = sb.AppendLine("            }");
            return;
        }

        if (col.IsNullable) {
            _ = sb.AppendLine("                b.AddAttribute(colSeq++, \"Property\", (Expression<Func<" + typeName + ", string?>>)(x => x." + col.PropertyName + ".HasValue ? x." + col.PropertyName + ".Value.ToString(\"" + format + "\", CultureInfo.CurrentCulture) : \"\\u2014\"));");
        }
        else {
            _ = sb.AppendLine("                b.AddAttribute(colSeq++, \"Property\", (Expression<Func<" + typeName + ", string?>>)(x => x." + col.PropertyName + ".ToString(\"" + format + "\", CultureInfo.CurrentCulture)));");
        }

        EmitSortAttributes(sb, col, typeName, isDefaultSortColumn, indent: "                ");
        _ = sb.AppendLine("                b.CloseComponent();");
        _ = sb.AppendLine("            }");
    }

    private static void EmitEnumColumn(StringBuilder sb, ColumnModel col, string typeName, bool isDefaultSortColumn) {
        _ = sb.AppendLine("            // Enum column: " + col.PropertyName);
        EmitSlotColumnBranch(sb, col, typeName, isDefaultSortColumn);
        _ = sb.AppendLine("                b.OpenComponent<PropertyColumn<" + typeName + ", string?>>(colSeq++);");
        _ = sb.AppendLine("                b.AddAttribute(colSeq++, \"Title\", \"" + RoleBodyHelpers.EscapeString(col.Header) + "\");");
        EmitHeaderDescription(sb, col, indent: "                ");

        if (col.IsNullable) {
            _ = sb.AppendLine("                b.AddAttribute(colSeq++, \"Property\", (Expression<Func<" + typeName + ", string?>>)(x => x." + col.PropertyName + ".HasValue ? Truncate(HumanizeEnumLabel(x." + col.PropertyName + ".Value.ToString()), 30) : \"\\u2014\"));");
        }
        else {
            _ = sb.AppendLine("                b.AddAttribute(colSeq++, \"Property\", (Expression<Func<" + typeName + ", string?>>)(x => Truncate(HumanizeEnumLabel(x." + col.PropertyName + ".ToString()), 30)));");
        }

        if (col.BadgeMappings.Count > 0) {
            EmitEnumBadgeChildContent(sb, col, typeName, indent: "                ");
        }

        EmitSortAttributes(sb, col, typeName, isDefaultSortColumn, indent: "                ");
        _ = sb.AppendLine("                b.CloseComponent();");
        _ = sb.AppendLine("            }");
    }

    /// <summary>
    /// Story 4-2 D1 / D5 / D8 / AC1 / AC3 — emits the <c>ChildContent</c> render fragment for a
    /// DataGrid <c>PropertyColumn</c> that wraps annotated enum members in <c>FcStatusBadge</c>
    /// and falls back to humanized text / the localised unknown-state string for null / declared
    /// but unannotated / out-of-range values respectively. The <c>Property</c> lambda on the
    /// column is preserved verbatim so DataGrid sort / filter / default-aria paths continue to
    /// operate on the text representation (unchanged from Story 1-5).
    /// </summary>
    private static void EmitEnumBadgeChildContent(
        StringBuilder sb,
        ColumnModel col,
        string typeName,
        string indent = "            ") {
        string propertyAccess = col.IsNullable
            ? "item." + col.PropertyName + ".Value"
            : "item." + col.PropertyName;
        string headerLiteral = "\"" + RoleBodyHelpers.EscapeString(col.Header) + "\"";

        _ = sb.AppendLine(indent + "b.AddAttribute(colSeq++, \"ChildContent\", (RenderFragment<" + typeName + ">)(item => (RenderTreeBuilder rb) =>");
        _ = sb.AppendLine(indent + "{");

        if (col.IsNullable) {
            _ = sb.AppendLine(indent + "    if (!item." + col.PropertyName + ".HasValue)");
            _ = sb.AppendLine(indent + "    {");
            _ = sb.AppendLine(indent + "        rb.AddContent(0, \"\\u2014\");");
            _ = sb.AppendLine(indent + "        return;");
            _ = sb.AppendLine(indent + "    }");
            _ = sb.AppendLine();
        }

        _ = sb.AppendLine(indent + "    var _memberName = " + propertyAccess + ".ToString();");
        _ = sb.AppendLine(indent + "    var _label = HumanizeEnumLabel(_memberName);");
        EmitBadgeSwitch(sb, col, headerLiteral, builderName: "rb", indent: indent + "    ");
        _ = sb.AppendLine(indent + "}));");
    }

    /// <summary>
    /// Story 4-2 RF1 / RF2 / RF3 — shared badge-dispatch helper used by DataGrid columns,
    /// StatusOverview grouped grid cells, DetailRecord detail fields, and Timeline rows.
    /// Emits an inline <c>switch (memberName)</c> with three classes of arm:
    /// <list type="bullet">
    /// <item>annotated members → open <see cref="Hexalith.FrontComposer.Shell.Components.Badges"/><c>.FcStatusBadge</c> with the resolved slot;</item>
    /// <item>declared-but-unannotated members (partial coverage) → <c>AddContent</c> with the humanized label;</item>
    /// <item>out-of-range (unsafe cast) values → <c>AddContent</c> with the localised
    /// <c>StatusBadgeUnknownStateFallback</c> resource resolved through the view-scoped
    /// <c>IStringLocalizer&lt;FcShellResources&gt;</c> (Story 4-2 RF3; injected by
    /// <see cref="RazorEmitter.EmitInjections"/> when any column carries badge mappings).</item>
    /// </list>
    /// </summary>
    /// <param name="sb">Output builder receiving the switch lines.</param>
    /// <param name="col">Column model carrying <see cref="ColumnModel.BadgeMappings"/> and
    /// <see cref="ColumnModel.EnumMemberNames"/>.</param>
    /// <param name="headerLiteral">Escaped, quoted column-header string used for the
    /// <c>ColumnHeader</c> parameter on <c>FcStatusBadge</c> (e.g. <c>"\"Order Status\""</c>).</param>
    /// <param name="builderName">Name of the local <c>RenderTreeBuilder</c> variable in scope
    /// at the call site (e.g. <c>rb</c> or <c>b</c>).</param>
    /// <param name="indent">Leading whitespace for each emitted line — lets the helper nest
    /// correctly inside render fragments with different indentation depths.</param>
    /// <param name="seqVariable">Optional caller-managed sequence variable for inline builder
    /// scopes such as Timeline / DetailRecord. When <see langword="null"/>, the helper emits
    /// fixed literal sequence numbers suitable for self-contained nested render fragments.</param>
    internal static void EmitBadgeSwitch(
        StringBuilder sb,
        ColumnModel col,
        string headerLiteral,
        string builderName,
        string indent,
        string? seqVariable = null) {
        HashSet<string> annotatedNames = new(StringComparer.Ordinal);
        foreach (BadgeMappingEntry mapping in col.BadgeMappings) {
            _ = annotatedNames.Add(mapping.EnumMemberName);
        }

        _ = sb.AppendLine(indent + "switch (_memberName)");
        _ = sb.AppendLine(indent + "{");

        foreach (BadgeMappingEntry mapping in col.BadgeMappings) {
            string memberLiteral = "\"" + RoleBodyHelpers.EscapeString(mapping.EnumMemberName) + "\"";
            _ = sb.AppendLine(indent + "    case " + memberLiteral + ":");
            _ = sb.AppendLine(indent + "        " + builderName + ".OpenComponent<global::Hexalith.FrontComposer.Shell.Components.Badges.FcStatusBadge>(" + SequenceExpression(seqVariable, 0) + ");");
            _ = sb.AppendLine(indent + "        " + builderName + ".AddAttribute(" + SequenceExpression(seqVariable, 1) + ", \"Slot\", global::Hexalith.FrontComposer.Contracts.Attributes.BadgeSlot." + mapping.Slot + ");");
            _ = sb.AppendLine(indent + "        " + builderName + ".AddAttribute(" + SequenceExpression(seqVariable, 2) + ", \"Label\", _label);");
            _ = sb.AppendLine(indent + "        " + builderName + ".AddAttribute(" + SequenceExpression(seqVariable, 3) + ", \"ColumnHeader\", " + headerLiteral + ");");
            _ = sb.AppendLine(indent + "        " + builderName + ".CloseComponent();");
            _ = sb.AppendLine(indent + "        break;");
        }

        // Declared-but-unannotated members render as plain humanized text — partial coverage is
        // valid state during incremental domain modelling; HFC1025 already surfaces the gap.
        foreach (string memberName in col.EnumMemberNames) {
            if (annotatedNames.Contains(memberName)) {
                continue;
            }

            string memberLiteral = "\"" + RoleBodyHelpers.EscapeString(memberName) + "\"";
            _ = sb.AppendLine(indent + "    case " + memberLiteral + ":");
            _ = sb.AppendLine(indent + "        " + builderName + ".AddContent(" + SequenceExpression(seqVariable, 10) + ", _label);");
            _ = sb.AppendLine(indent + "        break;");
        }

        // Out-of-range runtime values (unsafe cast) → localised fail-soft label per Story 4-2 D4.
        _ = sb.AppendLine(indent + "    default:");
        _ = sb.AppendLine(indent + "        " + builderName + ".AddContent(" + SequenceExpression(seqVariable, 11) + ", "
            + ShellLocalizerFieldName + "[\"" + UnknownStateFallbackResourceKey + "\"].Value);");
        _ = sb.AppendLine(indent + "        break;");
        _ = sb.AppendLine(indent + "}");
    }

    /// <summary>
    /// Story 4-2 RF1 — shared inline enum-field emitter used outside the DataGrid column path
    /// (DetailRecord FluentText host, Timeline row stack, StatusOverview grouped-grid cell).
    /// Handles the null-check / badge-switch / plain-text dispatch in one place so every role
    /// body reaches the same <c>FcStatusBadge</c> rendering contract.
    /// </summary>
    /// <param name="sb">Output builder.</param>
    /// <param name="col">Column model (the enum column whose value is being rendered).</param>
    /// <param name="instanceName">Name of the in-scope instance variable whose
    /// <see cref="ColumnModel.PropertyName"/> we dereference (e.g. <c>entity</c> or <c>item</c>).</param>
    /// <param name="builderName">Name of the local <c>RenderTreeBuilder</c> in scope.</param>
    /// <param name="seqVariable">Name of the in-scope <c>int</c> sequence variable used when the
    /// helper emits non-badge <c>AddContent</c> fallback paths (e.g. <c>fieldSeq</c>, <c>rowSeq</c>).</param>
    /// <param name="indent">Leading whitespace for each emitted line.</param>
    internal static void EmitInlineEnumRenderFragment(
        StringBuilder sb,
        ColumnModel col,
        string instanceName,
        string builderName,
        string seqVariable,
        string indent) {
        string headerLiteral = "\"" + RoleBodyHelpers.EscapeString(col.Header) + "\"";
        string propertyAccess = instanceName + "." + col.PropertyName;
        string unwrappedAccess = col.IsNullable ? propertyAccess + ".Value" : propertyAccess;

        if (col.IsNullable) {
            _ = sb.AppendLine(indent + "if (!" + propertyAccess + ".HasValue)");
            _ = sb.AppendLine(indent + "{");
            _ = sb.AppendLine(indent + "    " + builderName + ".AddContent(" + seqVariable + "++, \"\\u2014\");");
            _ = sb.AppendLine(indent + "}");
            _ = sb.AppendLine(indent + "else");
            _ = sb.AppendLine(indent + "{");
            EmitInlineEnumBadgeBlock(sb, col, unwrappedAccess, headerLiteral, builderName, seqVariable, indent + "    ");
            _ = sb.AppendLine(indent + "}");
            return;
        }

        EmitInlineEnumBadgeBlock(sb, col, unwrappedAccess, headerLiteral, builderName, seqVariable, indent);
    }

    private static void EmitInlineEnumBadgeBlock(
        StringBuilder sb,
        ColumnModel col,
        string propertyAccess,
        string headerLiteral,
        string builderName,
        string seqVariable,
        string indent) {
        _ = sb.AppendLine(indent + "var _memberName = " + propertyAccess + ".ToString();");
        _ = sb.AppendLine(indent + "var _label = HumanizeEnumLabel(_memberName);");
        EmitBadgeSwitch(sb, col, headerLiteral, builderName, indent, seqVariable);
    }

    private static string SequenceExpression(string? seqVariable, int fixedValue)
        => seqVariable is null
            ? fixedValue.ToString()
            : seqVariable + "++";

    private static void EmitCollectionColumn(StringBuilder sb, ColumnModel col, string typeName, bool isDefaultSortColumn) {
        _ = sb.AppendLine("            // Collection column: " + col.PropertyName);
        EmitSlotColumnBranch(sb, col, typeName, isDefaultSortColumn);
        _ = sb.AppendLine("                b.OpenComponent<PropertyColumn<" + typeName + ", string?>>(colSeq++);");
        _ = sb.AppendLine("                b.AddAttribute(colSeq++, \"Title\", \"" + RoleBodyHelpers.EscapeString(col.Header) + "\");");
        EmitHeaderDescription(sb, col, indent: "                ");
        _ = sb.AppendLine("                b.AddAttribute(colSeq++, \"Property\", (Expression<Func<" + typeName + ", string?>>)(x => x." + col.PropertyName + " == null ? \"\\u2014\" : System.Linq.Enumerable.Count(x." + col.PropertyName + ").ToString() + \" items\"));");
        EmitSortAttributes(sb, col, typeName, isDefaultSortColumn, indent: "                ");
        _ = sb.AppendLine("                b.CloseComponent();");
        _ = sb.AppendLine("            }");
    }

    private static void EmitUnsupportedColumn(StringBuilder sb, ColumnModel col, string typeName) {
        _ = sb.AppendLine("            // Unsupported column: " + col.PropertyName);
        _ = sb.AppendLine("            b.OpenComponent<TemplateColumn<" + typeName + ">>(colSeq++);");
        _ = sb.AppendLine("            b.AddAttribute(colSeq++, \"Title\", \"" + RoleBodyHelpers.EscapeString(col.Header) + " \" + " + ShellLocalizerFieldName + "[\"UnsupportedColumnHeaderSuffix\"].Value);");
        // Description + unsupported-suffix are merged into a single tooltip so both signals
        // (author intent and FR9 unsupported warning) reach the user without duplication.
        EmitHeaderDescription(sb, col, mergeWith: ShellLocalizerFieldName + "[\"UnsupportedColumnHeaderSuffix\"].Value");
        _ = sb.AppendLine("            b.AddAttribute(colSeq++, \"Class\", \"fc-col-unsupported\");");
        EmitSlotChildContent(sb, col, typeName, slotFormatOverride: null);
        _ = sb.AppendLine("            b.CloseComponent();");
    }

    private static void EmitSlotChildContent(
        StringBuilder sb,
        ColumnModel col,
        string typeName,
        string? slotFormatOverride = null,
        string indent = "            ") {
        _ = sb.AppendLine(indent + "b.AddAttribute(colSeq++, \"ChildContent\", (RenderFragment<" + typeName + ">)(item => RenderSlotField(item,");
        _ = sb.AppendLine(indent + "    fieldName: \"" + RoleBodyHelpers.EscapeString(col.PropertyName) + "\",");
        _ = sb.AppendLine(indent + "    displayName: \"" + RoleBodyHelpers.EscapeString(col.Header) + "\",");
        _ = sb.AppendLine(indent + "    format: " + SlotStringLiteral(slotFormatOverride ?? col.FormatHint) + ",");
        _ = sb.AppendLine(indent + "    order: " + SlotNullableIntLiteral(col.Priority) + ",");
        _ = sb.AppendLine(indent + "    isFieldReadOnly: false,");
        _ = sb.AppendLine(indent + "    value: item." + col.PropertyName + ",");
        _ = sb.AppendLine(indent + "    renderDefault: __ctx => RenderTemplateDefaultField(__ctx.Parent, \"" + RoleBodyHelpers.EscapeString(col.PropertyName) + "\"))));");
    }

    private static void EmitSlotColumnBranch(
        StringBuilder sb,
        ColumnModel col,
        string typeName,
        bool isDefaultSortColumn,
        string? cssClass = null) {
        _ = sb.AppendLine("            if (HasProjectionSlot(\"" + RoleBodyHelpers.EscapeString(col.PropertyName) + "\"))");
        _ = sb.AppendLine("            {");
        _ = sb.AppendLine("                b.OpenComponent<TemplateColumn<" + typeName + ">>(colSeq++);");
        _ = sb.AppendLine("                b.AddAttribute(colSeq++, \"Title\", \"" + RoleBodyHelpers.EscapeString(col.Header) + "\");");
        EmitHeaderDescription(sb, col, indent: "                ");
        if (!string.IsNullOrEmpty(cssClass)) {
            _ = sb.AppendLine("                b.AddAttribute(colSeq++, \"Class\", \"" + RoleBodyHelpers.EscapeString(cssClass!) + "\");");
        }

        EmitSlotChildContent(sb, col, typeName, indent: "                ");
        EmitSortAttributes(sb, col, typeName, isDefaultSortColumn, indent: "                ");
        _ = sb.AppendLine("                b.CloseComponent();");
        _ = sb.AppendLine("            }");
        _ = sb.AppendLine("            else");
        _ = sb.AppendLine("            {");
    }

    private static string SlotStringLiteral(string? value)
        => string.IsNullOrEmpty(value)
            ? "(string?)null"
            : "\"" + RoleBodyHelpers.EscapeString(value!) + "\"";

    private static string SlotNullableIntLiteral(int? value)
        => value.HasValue
            ? value.Value.ToString(CultureInfo.InvariantCulture)
            : "(int?)null";

    private static void EmitHeaderDescription(
        StringBuilder sb,
        ColumnModel col,
        string? mergeWith = null,
        string indent = "            ") {
        if (string.IsNullOrWhiteSpace(col.Description)) {
            return;
        }

        string descriptionLiteral = "\"" + RoleBodyHelpers.EscapeString(col.Description!) + "\"";
        string tooltipExpression = mergeWith is null
            ? descriptionLiteral
            : descriptionLiteral + " + \" \\u2014 \" + " + mergeWith;
        _ = sb.AppendLine(indent + "b.AddAttribute(colSeq++, \"HeaderTooltip\", " + tooltipExpression + ");");
    }

    private static void EmitSortAttributes(
        StringBuilder sb,
        ColumnModel col,
        string typeName,
        bool isDefaultSortColumn,
        string indent = "            ") {
        if (!isDefaultSortColumn) {
            return;
        }

        _ = sb.AppendLine(indent + "b.AddAttribute(colSeq++, \"Sortable\", true);");
        _ = sb.AppendLine(indent + "b.AddAttribute(colSeq++, \"IsDefaultSortColumn\", true);");
        _ = sb.AppendLine(indent + "b.AddAttribute(colSeq++, \"InitialSortDirection\", DataGridSortDirection.Descending);");
        _ = sb.AppendLine(indent + "b.AddAttribute(colSeq++, \"SortBy\", " + BuildSortExpression(col, typeName) + ");");
    }

    private static string BuildSortExpression(ColumnModel col, string typeName)
        => col.TypeCategory == TypeCategory.Collection
            ? "GridSort<" + typeName + ">.ByDescending(x => x." + col.PropertyName + " == null ? 0 : System.Linq.Enumerable.Count(x." + col.PropertyName + "))"
            : "GridSort<" + typeName + ">.ByDescending(x => x." + col.PropertyName + ")";
}


