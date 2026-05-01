using System.Text;
using System.Text.RegularExpressions;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.DevMode;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Shell.Services.DevMode;

/// <summary>
/// Emits deterministic starter Razor source from dev-mode component-tree metadata.
/// </summary>
public sealed partial class RazorEmitter : IRazorEmitter {
    private readonly FcShellOptions _options;
    private readonly ILogger<RazorEmitter>? _logger;

    /// <summary>Initializes a new instance of the <see cref="RazorEmitter"/> class.</summary>
    public RazorEmitter(IOptions<FcShellOptions>? options = null, ILogger<RazorEmitter>? logger = null) {
        _options = options?.Value ?? new FcShellOptions();
        _logger = logger;
    }

    /// <inheritdoc />
    public string EmitStarterTemplate(ComponentTreeNode node, CustomizationLevel level) {
        ArgumentNullException.ThrowIfNull(node);
        try {
            if (node.CurrentLevel != level) {
                _logger?.LogInformation(
                    "HFC1048: Unsupported customization level requested for starter emission. AnnotationKey={AnnotationKey} CurrentLevel={CurrentLevel} RequestedLevel={RequestedLevel}",
                    node.AnnotationKey,
                    node.CurrentLevel,
                    level);
                return Header(node) + $"// Starter template unavailable: requested level does not match node level ({node.CurrentLevel} != {level}).";
            }

            if (node.IsStale) {
                _logger?.LogInformation(
                    "HFC1049: Stale component-tree metadata suppressed starter emission. AnnotationKey={AnnotationKey} Reasons={Reasons}",
                    node.AnnotationKey,
                    string.Join(",", node.StaleReasons));
                return Header(node) + "// Starter template unavailable: stale metadata (" + string.Join(", ", node.StaleReasons) + ").";
            }

            if (level < CustomizationLevel.Level2) {
                return Header(node) + "// Starter template unavailable: default and Level 1 annotations do not need Razor starter source.";
            }

            return level switch {
                CustomizationLevel.Level2 => EmitLevel2(node),
                CustomizationLevel.Level3 => EmitLevel3(node),
                CustomizationLevel.Level4 => EmitLevel4(node),
                _ => Header(node) + "// Starter template unavailable: unsupported customization level.",
            };
        }
        catch (Exception ex) {
            _logger?.LogInformation(
                ex,
                "HFC1048: Starter emission failed. AnnotationKey={AnnotationKey} Level={Level}",
                node.AnnotationKey,
                level);
            return Header(node) + "// Starter template unavailable: emission failed.";
        }
    }

    private string EmitLevel2(ComponentTreeNode node) {
        string projectionType = TypeName(node.OriginatingProjectionTypeName);
        string componentName = Identifier(ShortTypeName(node.OriginatingProjectionTypeName) + "Template");
        StringBuilder sb = new(Header(node));
        _ = sb.AppendLine("@using Hexalith.FrontComposer.Contracts.Rendering");
        _ = sb.AppendLine();
        _ = sb.AppendLine($"<div class=\"fc-template-{Identifier(node.AnnotationKey).ToLowerInvariant()}\">");
        _ = sb.AppendLine("    @Context.DefaultBody");
        _ = sb.AppendLine("</div>");
        _ = sb.AppendLine();
        _ = sb.AppendLine("@code {");
        _ = sb.AppendLine($"    [Microsoft.AspNetCore.Components.Parameter] public ProjectionViewContext<{projectionType}> Context {{ get; set; }} = default!;");
        _ = sb.AppendLine("}");
        AppendTree(sb, node);
        _ = sb.AppendLine();
        _ = sb.AppendLine("/* Registration:");
        _ = sb.AppendLine($"services.AddProjectionTemplate<{projectionType}, {componentName}>();");
        _ = sb.AppendLine("*/");
        return sb.ToString();
    }

    private string EmitLevel3(ComponentTreeNode node) {
        string projectionType = TypeName(node.OriginatingProjectionTypeName);
        string fieldType = TypeName(node.ContractTypeName);
        string componentName = Identifier(ShortTypeName(node.OriginatingProjectionTypeName) + (node.FieldAccessor ?? "Field") + "Slot");
        StringBuilder sb = new(Header(node));
        _ = sb.AppendLine("@using Hexalith.FrontComposer.Contracts.Rendering");
        _ = sb.AppendLine();
        _ = sb.AppendLine("<span class=\"fc-slot-value\">@Context.Value</span>");
        _ = sb.AppendLine();
        _ = sb.AppendLine("@code {");
        _ = sb.AppendLine($"    [Microsoft.AspNetCore.Components.Parameter] public FieldSlotContext<{projectionType}, {fieldType}> Context {{ get; set; }} = default!;");
        _ = sb.AppendLine("}");
        AppendTree(sb, node);
        _ = sb.AppendLine();
        _ = sb.AppendLine("/* Registration:");
        _ = sb.AppendLine($"services.AddSlotOverride<{projectionType}>(o => o.{Identifier(node.FieldAccessor ?? "Field")}, typeof({componentName}));");
        _ = sb.AppendLine("*/");
        return sb.ToString();
    }

    private string EmitLevel4(ComponentTreeNode node) {
        string projectionType = TypeName(node.OriginatingProjectionTypeName);
        string componentName = Identifier(ShortTypeName(node.OriginatingProjectionTypeName) + "ViewReplacement");
        StringBuilder sb = new(Header(node));
        _ = sb.AppendLine("@using Hexalith.FrontComposer.Contracts.Rendering");
        _ = sb.AppendLine("@using Hexalith.FrontComposer.Shell.Components.Lifecycle");
        _ = sb.AppendLine();
        _ = sb.AppendLine("<section aria-label=\"@Context.EntityPluralLabel\">");
        _ = sb.AppendLine("    <FcLifecycleWrapper State=\"@Context.LifecycleState\">");
        _ = sb.AppendLine("        @Context.DefaultBody");
        _ = sb.AppendLine("    </FcLifecycleWrapper>");
        _ = sb.AppendLine("</section>");
        _ = sb.AppendLine();
        _ = sb.AppendLine("@code {");
        _ = sb.AppendLine($"    [Microsoft.AspNetCore.Components.Parameter] public ProjectionViewContext<{projectionType}> Context {{ get; set; }} = default!;");
        _ = sb.AppendLine("}");
        AppendTree(sb, node);
        _ = sb.AppendLine();
        _ = sb.AppendLine("/* Registration:");
        _ = sb.AppendLine($"services.AddViewOverride<{projectionType}, {componentName}>();");
        _ = sb.AppendLine("*/");
        return sb.ToString();
    }

    private static string Header(ComponentTreeNode node)
        => $"// Generated for FrontComposer contract v{ComponentTreeContractVersion.Current}; rebuild required when contract version changes (HFC1049).{Environment.NewLine}"
            + $"// Projection: {Comment(node.OriginatingProjectionTypeName)}{Environment.NewLine}"
            + $"// Convention: {Comment(node.Convention.Name)}{Environment.NewLine}"
            + $"// DescriptorHash: {Comment(node.DescriptorHash)}{Environment.NewLine}"
            + $"// SourceComponentIdentity: {Comment(node.SourceComponentIdentity)}{Environment.NewLine}";

    private void AppendTree(StringBuilder sb, ComponentTreeNode node) {
        _ = sb.AppendLine();
        _ = sb.AppendLine("@* Component tree snapshot:");
        AppendNode(sb, node, depth: 0);
        _ = sb.AppendLine("*@");
    }

    private void AppendNode(StringBuilder sb, ComponentTreeNode node, int depth) {
        if (depth >= _options.DevMode.MaxNodeDepth) {
            _ = sb.AppendLine($"{Indent(depth)}- Component tree truncated at MaxNodeDepth={_options.DevMode.MaxNodeDepth}");
            return;
        }

        _ = sb.AppendLine($"{Indent(depth)}- {Comment(node.AnnotationKey)} ({node.CurrentLevel})");
        int count = 0;
        if (node.Children.IsDefaultOrEmpty) {
            return;
        }

        foreach (ComponentTreeNode child in node.Children) {
            if (count >= _options.DevMode.MaxFanOut) {
                _ = sb.AppendLine($"{Indent(depth + 1)}- Component tree fan-out truncated after {_options.DevMode.MaxFanOut} children");
                return;
            }

            AppendNode(sb, child, depth + 1);
            count++;
        }
    }

    private static string Indent(int depth) => new(' ', Math.Max(0, depth) * 2);

    private static string TypeName(string value)
        => string.IsNullOrWhiteSpace(value) ? "object" : SanitizedTypeNameRegex().Replace(value, string.Empty);

    private static string ShortTypeName(string value) {
        string sanitized = TypeName(value);
        int index = sanitized.LastIndexOf('.');
        return index >= 0 ? sanitized[(index + 1)..] : sanitized;
    }

    private static string Identifier(string value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return "GeneratedStarter";
        }

        StringBuilder sb = new(value.Length);
        foreach (char ch in value) {
            if ((ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9') || ch == '_') {
                _ = sb.Append(ch);
            }
        }

        if (sb.Length == 0 || char.IsDigit(sb[0])) {
            _ = sb.Insert(0, "Generated");
        }

        return sb.ToString();
    }

    private static string Comment(string value)
        => (value ?? string.Empty)
            .Replace("*/", "* /", StringComparison.Ordinal)
            .Replace("*@", "* @", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);

    [GeneratedRegex(@"[^A-Za-z0-9_.,<> ]")]
    private static partial Regex SanitizedTypeNameRegex();
}
