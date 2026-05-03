using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

using Hexalith.FrontComposer.Contracts.Mcp;

namespace Hexalith.FrontComposer.Mcp.Rendering;

public static class McpMarkdownProjectionRenderer {
    public static McpProjectionRenderResult Render(
        McpProjectionRenderRequest request,
        FrontComposerMcpOptions options,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(options);

        try {
            cancellationToken.ThrowIfCancellationRequested();

            McpResourceDescriptor descriptor = request.Descriptor;
            string role = string.IsNullOrWhiteSpace(descriptor.RenderStrategy) ? "Default" : descriptor.RenderStrategy;
            IReadOnlyList<McpParameterDescriptor> fields = descriptor.Fields
                .Where(f => !f.IsUnsupported)
                .Take(Math.Max(1, options.MaxFieldsPerResource))
                .ToArray();

            string text = role switch {
                "StatusOverview" => RenderStatusOverviewDocument(descriptor, fields, request.Items, request.TotalCount, request.SafeCommandSuggestions, options, cancellationToken),
                "Timeline" => RenderTimelineDocument(descriptor, fields, request.Items, request.TotalCount, request.SafeCommandSuggestions, options, cancellationToken),
                _ => RenderTableDocument(descriptor, fields, request.Items, request.TotalCount, role, request.SafeCommandSuggestions, options, cancellationToken),
            };
            var document = new McpMarkdownProjectionDocument(
                descriptor.Name,
                role,
                descriptor.BoundedContext,
                request.RowCountCategory,
                request.IsTruncated || descriptor.Fields.Count(f => !f.IsUnsupported) > fields.Count,
                request.RequestId,
                request.CorrelationId,
                text);
            return McpProjectionRenderResult.Success(document);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            return McpProjectionRenderResult.Failure(FrontComposerMcpFailureCategory.Canceled);
        }
        catch (FrontComposerMcpException ex) {
            return McpProjectionRenderResult.Failure(ex.Category);
        }
        catch {
            return McpProjectionRenderResult.Failure(FrontComposerMcpFailureCategory.DownstreamFailed);
        }
    }

    private static string RenderTableDocument(
        McpResourceDescriptor descriptor,
        IReadOnlyList<McpParameterDescriptor> fields,
        IReadOnlyList<object> items,
        long totalCount,
        string role,
        IReadOnlyList<string>? suggestions,
        FrontComposerMcpOptions options,
        CancellationToken cancellationToken) {
        var sb = new StringBuilder(capacity: Math.Min(4096, Math.Max(256, options.MaxProjectionMarkdownCharacters)));
        sb.Append("## ").AppendLine(EscapeMarkdownText(descriptor.Title));
        sb.AppendLine();

        if (items.Count == 0) {
            AppendEmptyState(sb, descriptor, suggestions, options);
            return BoundDocument(sb, options);
        }

        sb.Append("- Total: ").AppendLine(totalCount.ToString(CultureInfo.InvariantCulture));
        sb.Append("- Role: ").AppendLine(EscapeMarkdownText(role));
        sb.AppendLine();

        if (fields.Count == 0) {
            return BoundDocument(sb, options);
        }

        sb.Append("| ").Append(string.Join(" | ", fields.Select(f => EscapeMarkdownText(f.Title)))).AppendLine(" |");
        sb.Append("| ").Append(string.Join(" | ", fields.Select(_ => "---"))).AppendLine(" |");

        int rendered = 0;
        int maxRows = Math.Max(1, options.MaxRowsPerResource);
        foreach (object item in items) {
            cancellationToken.ThrowIfCancellationRequested();
            if (rendered >= maxRows) {
                break;
            }

            sb.Append("| ");
            sb.Append(string.Join(" | ", fields.Select(f => FormatCell(ReadPropertyValue(item, f.Name), f, options))));
            sb.AppendLine(" |");
            rendered++;
        }

        if (descriptor.Fields.Count(f => !f.IsUnsupported) > fields.Count || items.Count > rendered) {
            sb.AppendLine();
            sb.AppendLine(EscapeMarkdownText(options.ProjectionTruncationMarker));
        }

        return BoundDocument(sb, options);
    }

    private static string RenderStatusOverviewDocument(
        McpResourceDescriptor descriptor,
        IReadOnlyList<McpParameterDescriptor> fields,
        IReadOnlyList<object> items,
        long totalCount,
        IReadOnlyList<string>? suggestions,
        FrontComposerMcpOptions options,
        CancellationToken cancellationToken) {
        var sb = new StringBuilder(capacity: 1024);
        sb.Append("## ").AppendLine(EscapeMarkdownText(descriptor.Title));
        sb.AppendLine();
        sb.Append("- Total: ").AppendLine(totalCount.ToString(CultureInfo.InvariantCulture));

        if (items.Count == 0) {
            AppendEmptyState(sb, descriptor, suggestions, options);
            return BoundDocument(sb, options);
        }

        McpParameterDescriptor? statusField = fields.FirstOrDefault(f => f.BadgeMappings is { Count: > 0 })
            ?? fields.FirstOrDefault(f => string.Equals(f.TypeName, "Enum", StringComparison.Ordinal));
        if (statusField is null) {
            return BoundDocument(sb, options);
        }

        Dictionary<string, StatusGroup> groups = new(StringComparer.Ordinal);
        foreach (object item in items) {
            cancellationToken.ThrowIfCancellationRequested();
            object? raw = ReadPropertyValue(item, statusField.Name);
            if (raw is null) {
                continue;
            }

            string member = raw is Enum e ? e.ToString() : raw.ToString() ?? string.Empty;
            string slot = statusField.BadgeMappings is not null
                && statusField.BadgeMappings.TryGetValue(member, out string? mapped)
                && IsSemanticBadgeSlot(mapped)
                    ? mapped
                    : "Neutral";
            if (!groups.TryGetValue(member, out StatusGroup? group)) {
                group = new StatusGroup(slot, Humanize(member));
                groups.Add(member, group);
            }

            group.Count++;
        }

        foreach (StatusGroup group in groups.Values.Take(Math.Max(1, options.MaxProjectionStatusGroups))) {
            sb.Append("- ")
                .Append(EscapeMarkdownText(group.Slot))
                .Append(": ")
                .Append(group.Count.ToString(CultureInfo.InvariantCulture))
                .Append(' ')
                .AppendLine(EscapeMarkdownText(group.Label));
        }

        if (groups.Count > Math.Max(1, options.MaxProjectionStatusGroups)) {
            sb.AppendLine(EscapeMarkdownText(options.ProjectionTruncationMarker));
        }

        return BoundDocument(sb, options);
    }

    private static string RenderTimelineDocument(
        McpResourceDescriptor descriptor,
        IReadOnlyList<McpParameterDescriptor> fields,
        IReadOnlyList<object> items,
        long totalCount,
        IReadOnlyList<string>? suggestions,
        FrontComposerMcpOptions options,
        CancellationToken cancellationToken) {
        var sb = new StringBuilder(capacity: 2048);
        sb.Append("## ").AppendLine(EscapeMarkdownText(descriptor.Title));
        sb.AppendLine();

        if (items.Count == 0) {
            AppendEmptyState(sb, descriptor, suggestions, options);
            return BoundDocument(sb, options);
        }

        McpParameterDescriptor? timestampField = fields.FirstOrDefault(IsDateTimeField);
        McpParameterDescriptor? statusField = fields.FirstOrDefault(f => f.BadgeMappings is { Count: > 0 })
            ?? fields.FirstOrDefault(f => string.Equals(f.TypeName, "Enum", StringComparison.Ordinal));
        McpParameterDescriptor? titleField = fields.FirstOrDefault(f => f != timestampField && f != statusField);
        int maxEntries = Math.Max(1, Math.Min(options.MaxRowsPerResource, options.MaxProjectionTimelineEntries));

        var entries = items.Select((item, index) => new TimelineEntry(
                item,
                index,
                timestampField is null ? null : ReadDateTimeOffset(ReadPropertyValue(item, timestampField.Name))))
            .OrderBy(e => e.Timestamp is null ? 1 : 0)
            .ThenByDescending(e => e.Timestamp)
            .ThenBy(e => e.Ordinal)
            .Take(maxEntries)
            .ToArray();

        foreach (TimelineEntry entry in entries) {
            cancellationToken.ThrowIfCancellationRequested();
            string timestamp = entry.Timestamp?.ToString("o", CultureInfo.InvariantCulture) ?? "No timestamp";
            string status = statusField is null ? string.Empty : FormatCell(ReadPropertyValue(entry.Item, statusField.Name), statusField, options);
            string title = titleField is null ? string.Empty : FormatCell(ReadPropertyValue(entry.Item, titleField.Name), titleField, options);

            sb.Append("- ").Append(EscapeMarkdownText(timestamp)).Append(" - ");
            if (!string.IsNullOrWhiteSpace(status)) {
                sb.Append(status).Append(". ");
            }

            sb.AppendLine(title);
        }

        if (items.Count > entries.Length) {
            sb.AppendLine();
            sb.AppendLine(EscapeMarkdownText(options.ProjectionTruncationMarker));
        }

        return BoundDocument(sb, options);
    }

    private static object? ReadPropertyValue(object item, string name) {
        PropertyInfo? property = item.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
        return property?.GetValue(item);
    }

    private static void AppendEmptyState(
        StringBuilder sb,
        McpResourceDescriptor descriptor,
        IReadOnlyList<string>? suggestions,
        FrontComposerMcpOptions options) {
        string plural = string.IsNullOrWhiteSpace(descriptor.EntityPluralLabel)
            ? descriptor.Title
            : descriptor.EntityPluralLabel!;
        sb.Append("No ").Append(EscapeMarkdownText(plural.ToLowerInvariant())).AppendLine(" found.");

        IReadOnlyList<string> safeSuggestions = suggestions ?? [];
        int max = Math.Max(0, options.MaxProjectionSuggestions);
        if (safeSuggestions.Count == 0 || max == 0) {
            return;
        }

        sb.AppendLine();
        sb.AppendLine("Suggestions:");
        foreach (string suggestion in safeSuggestions.Take(max)) {
            sb.Append("- ").AppendLine(EscapeMarkdownText(RedactSensitiveText(suggestion)));
        }
    }

    private static bool IsDateTimeField(McpParameterDescriptor field)
        => field.TypeName is "DateTime" or "DateTimeOffset" or "DateOnly" or "TimeOnly"
            || string.Equals(field.DisplayFormat, "RelativeTime", StringComparison.Ordinal);

    private static DateTimeOffset? ReadDateTimeOffset(object? value)
        => value switch {
            null => null,
            DateTimeOffset dto => dto,
            DateTime dt => new DateTimeOffset(dt),
            DateOnly d => new DateTimeOffset(d.ToDateTime(TimeOnly.MinValue)),
            _ => null,
        };

    private static string FormatCell(object? value, McpParameterDescriptor field, FrontComposerMcpOptions options) {
        if (value is null) {
            return "-";
        }

        string text = value switch {
            DateTime dt => dt.ToString("o", CultureInfo.InvariantCulture),
            DateTimeOffset dto => dto.ToString("o", CultureInfo.InvariantCulture),
            DateOnly d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            TimeOnly t => t.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
            bool b => b ? "true" : "false",
            Enum e => FormatEnum(e, field),
            string s => s,
            IEnumerable enumerable and not string => FormatEnumerable(enumerable),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty,
            _ => value.ToString() ?? string.Empty,
        };

        return EscapeMarkdownText(TrimCell(RedactSensitiveText(text), Math.Max(1, options.MaxProjectionCellCharacters)));
    }

    private static string RedactSensitiveText(string text) {
        if (string.IsNullOrEmpty(text)) {
            return string.Empty;
        }

        string redacted = Regex.Replace(
            text,
            @"(?i)\b(api[_-]?key|client[_-]?secret|authorization)\s*[:=]\s*\S+",
            "$1=[redacted]");
        redacted = Regex.Replace(
            redacted,
            @"(?i)\bbearer\s+[A-Za-z0-9_\-]+\.[A-Za-z0-9_\-]+(?:\.[A-Za-z0-9_\-]+)?",
            "Bearer [redacted]");
        redacted = Regex.Replace(
            redacted,
            @"\beyJ[A-Za-z0-9_\-]+\.[A-Za-z0-9_\-]+(?:\.[A-Za-z0-9_\-]+)?",
            "[redacted]");
        return redacted;
    }

    private static string FormatEnum(Enum value, McpParameterDescriptor field) {
        string name = value.ToString();
        string label = Humanize(name);
        if (field.BadgeMappings is not null && field.BadgeMappings.TryGetValue(name, out string? slot) && IsSemanticBadgeSlot(slot)) {
            return slot + ": " + label;
        }

        return label;
    }

    private static string FormatEnumerable(IEnumerable enumerable) {
        List<string> values = [];
        foreach (object? value in enumerable) {
            values.Add(value?.ToString() ?? "-");
            if (values.Count >= 5) {
                values.Add("...");
                break;
            }
        }

        return string.Join(", ", values);
    }

    private static string Humanize(string value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return value;
        }

        var sb = new StringBuilder(value.Length + 8);
        for (int i = 0; i < value.Length; i++) {
            char c = value[i];
            if (i > 0 && char.IsUpper(c) && !char.IsWhiteSpace(value[i - 1])) {
                sb.Append(' ');
            }

            sb.Append(c);
        }

        return sb.ToString();
    }

    private static bool IsSemanticBadgeSlot(string value)
        => value is "Neutral" or "Info" or "Success" or "Warning" or "Danger" or "Accent";

    private sealed class StatusGroup(string slot, string label) {
        public string Slot { get; } = slot;

        public string Label { get; } = label;

        public int Count { get; set; }
    }

    private sealed record TimelineEntry(object Item, int Ordinal, DateTimeOffset? Timestamp);

    private static string TrimCell(string text, int maxLength)
        => text.Length <= maxLength ? text : text[..Math.Max(0, maxLength - 3)] + "...";

    private static string BoundDocument(StringBuilder sb, FrontComposerMcpOptions options) {
        int max = Math.Max(1, options.MaxProjectionMarkdownCharacters);
        if (sb.Length <= max) {
            return sb.ToString();
        }

        string marker = options.ProjectionTruncationMarker;
        int keep = Math.Max(0, max - marker.Length - Environment.NewLine.Length);
        return sb.ToString(0, keep) + Environment.NewLine + marker;
    }

    internal static string EscapeMarkdownText(string value) {
        if (string.IsNullOrEmpty(value)) {
            return string.Empty;
        }

        var sb = new StringBuilder(value.Length);
        foreach (char c in value) {
            switch (c) {
                case '\\':
                    sb.Append("\\\\");
                    break;
                case '|':
                    sb.Append("\\|");
                    break;
                case '`':
                    sb.Append("\\`");
                    break;
                case '*':
                    sb.Append("\\*");
                    break;
                case '_':
                    sb.Append("\\_");
                    break;
                case '[':
                    sb.Append("\\[");
                    break;
                case ']':
                    sb.Append("\\]");
                    break;
                case '(':
                    sb.Append("\\(");
                    break;
                case ')':
                    sb.Append("\\)");
                    break;
                case '<':
                    sb.Append("\\<");
                    break;
                case '>':
                    sb.Append("\\>");
                    break;
                case '#':
                    sb.Append("\\#");
                    break;
                case '!':
                    sb.Append("\\!");
                    break;
                case '\r':
                case '\n':
                case '\t':
                    sb.Append(' ');
                    break;
                default:
                    if (char.IsControl(c) || c == '\uFEFF') {
                        sb.Append(' ');
                    }
                    else {
                        sb.Append(c);
                    }

                    break;
            }
        }

        return sb.ToString().Replace("- [ ]", "\\- \\[ \\]", StringComparison.Ordinal);
    }
}
