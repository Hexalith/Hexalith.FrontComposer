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
            McpProjectionRenderStrategy role = descriptor.RenderStrategy;
            string roleText = role.ToString();
            IReadOnlyList<McpParameterDescriptor> fields = descriptor.Fields
                .Take(Math.Max(1, options.MaxFieldsPerResource))
                .ToArray();
            bool fieldsTruncated = descriptor.Fields.Count > fields.Count;

            // Dashboard and unknown strategies must not silently fall through to a Default
            // table render — per the canonical contract they return the sanitized
            // unsupported-render category. DetailRecord falls back to a table only because
            // its descriptor exposes tabular fields safely.
            RenderedMarkdown rendered = role switch {
                McpProjectionRenderStrategy.Default or McpProjectionRenderStrategy.ActionQueue or McpProjectionRenderStrategy.DetailRecord
                    => RenderTableDocument(descriptor, fields, request.Items, request.TotalCount, roleText, request.IsTruncated || fieldsTruncated, request.SafeCommandSuggestions, options, cancellationToken),
                McpProjectionRenderStrategy.StatusOverview
                    => RenderStatusOverviewDocument(descriptor, fields, request.Items, request.TotalCount, request.IsTruncated || fieldsTruncated, request.SafeCommandSuggestions, options, cancellationToken),
                McpProjectionRenderStrategy.Timeline
                    => RenderTimelineDocument(descriptor, fields, request.Items, request.TotalCount, request.IsTruncated || fieldsTruncated, request.SafeCommandSuggestions, options, cancellationToken),
                _ => throw new FrontComposerMcpException(FrontComposerMcpFailureCategory.UnsupportedRender),
            };
            var document = new McpMarkdownProjectionDocument(
                descriptor.Name,
                roleText,
                descriptor.BoundedContext,
                request.RowCountCategory,
                rendered.IsTruncated,
                request.RequestId,
                request.CorrelationId,
                rendered.Text);
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

    private static RenderedMarkdown RenderTableDocument(
        McpResourceDescriptor descriptor,
        IReadOnlyList<McpParameterDescriptor> fields,
        IReadOnlyList<object> items,
        long totalCount,
        string role,
        bool requestIsTruncated,
        IReadOnlyList<string>? suggestions,
        FrontComposerMcpOptions options,
        CancellationToken cancellationToken) {
        var sb = new StringBuilder(capacity: Math.Min(4096, Math.Max(256, options.MaxProjectionMarkdownCharacters)));
        // P-17: redact title — descriptor titles flow into agent-visible Markdown and may carry
        // operator-supplied text that has not been scrubbed elsewhere.
        sb.Append("## ").AppendLine(EscapeMarkdownText(RedactSensitiveText(descriptor.Title)));
        sb.AppendLine();

        if (items.Count == 0) {
            AppendEmptyState(sb, descriptor, suggestions, options);
            AppendTruncationMarkerIfNeeded(sb, options, requestIsTruncated);
            return BoundDocument(sb, options, markerAlreadyEmitted: requestIsTruncated);
        }

        sb.Append("- Total: ").AppendLine(totalCount.ToString(CultureInfo.InvariantCulture));
        sb.Append("- Role: ").AppendLine(EscapeMarkdownText(role));
        sb.AppendLine();

        if (fields.Count == 0) {
            AppendTruncationMarkerIfNeeded(sb, options, requestIsTruncated);
            return BoundDocument(sb, options, markerAlreadyEmitted: requestIsTruncated);
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

        bool isTruncated = requestIsTruncated || items.Count > rendered;
        if (isTruncated) {
            sb.AppendLine();
            sb.AppendLine(EscapeMarkdownText(options.ProjectionTruncationMarker));
        }

        // P-13: tell BoundDocument the marker was already emitted to avoid double-emit when the
        // document also exceeds the character cap.
        return BoundDocument(sb, options, markerAlreadyEmitted: isTruncated);
    }

    private static RenderedMarkdown RenderStatusOverviewDocument(
        McpResourceDescriptor descriptor,
        IReadOnlyList<McpParameterDescriptor> fields,
        IReadOnlyList<object> items,
        long totalCount,
        bool requestIsTruncated,
        IReadOnlyList<string>? suggestions,
        FrontComposerMcpOptions options,
        CancellationToken cancellationToken) {
        var sb = new StringBuilder(capacity: 1024);
        sb.Append("## ").AppendLine(EscapeMarkdownText(RedactSensitiveText(descriptor.Title)));
        sb.AppendLine();
        sb.Append("- Total: ").AppendLine(totalCount.ToString(CultureInfo.InvariantCulture));

        if (items.Count == 0) {
            AppendEmptyState(sb, descriptor, suggestions, options);
            AppendTruncationMarkerIfNeeded(sb, options, requestIsTruncated);
            return BoundDocument(sb, options, markerAlreadyEmitted: requestIsTruncated);
        }

        // Search across all non-unsupported fields, not just the column-truncated subset, so a
        // status field at index >= MaxFieldsPerResource still drives the grouping.
        // R2-P11 / CD-Strategy-Filter: StatusOverview and Timeline filter unsupported fields out
        // of the field selection used to drive grouping/timestamp/title. Tables intentionally
        // surface unsupported columns as `(unsupported)` placeholders (CD-DN-1) so agents see
        // every input field; Status/Timeline pick fields by *purpose* (badge slot, datetime,
        // remaining-title), and an unsupported field cannot serve any of those purposes by
        // definition. The divergence is deliberate, not a silent drop.
        IReadOnlyList<McpParameterDescriptor> renderableFields = descriptor.Fields
            .Where(f => !f.IsUnsupported)
            .ToArray();
        McpParameterDescriptor? statusField = renderableFields.FirstOrDefault(f => f.BadgeMappings is { Count: > 0 })
            ?? renderableFields.FirstOrDefault(f => string.Equals(f.TypeName, "Enum", StringComparison.Ordinal));
        if (statusField is null) {
            AppendTruncationMarkerIfNeeded(sb, options, requestIsTruncated);
            return BoundDocument(sb, options, markerAlreadyEmitted: requestIsTruncated);
        }

        // Aggregate by slot first, with a stable label per slot derived from the first member
        // observed. Two enum members mapped to the same slot collapse into a single group.
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
            string key = slot + "" + member;
            if (!groups.TryGetValue(key, out StatusGroup? group)) {
                group = new StatusGroup(slot, Humanize(member));
                groups.Add(key, group);
            }

            group.Count++;
        }

        // Severity order: Total has already been rendered. Then Danger, Warning, Success,
        // Info, Accent, Neutral, then unknown labels sorted ordinally by sanitized label.
        StatusGroup[] ordered = [.. groups.Values
            .OrderBy(g => SlotSeverityIndex(g.Slot))
            .ThenBy(g => g.Slot, StringComparer.Ordinal)
            .ThenBy(g => g.Label, StringComparer.Ordinal)];

        int cap = Math.Max(1, options.MaxProjectionStatusGroups);
        foreach (StatusGroup group in ordered.Take(cap)) {
            sb.Append("- ")
                .Append(EscapeMarkdownText(group.Slot))
                .Append(": ")
                .Append(group.Count.ToString(CultureInfo.InvariantCulture))
                .Append(' ')
                .AppendLine(EscapeMarkdownText(group.Label));
        }

        bool isTruncated = requestIsTruncated || ordered.Length > cap;
        if (isTruncated) {
            sb.AppendLine(EscapeMarkdownText(options.ProjectionTruncationMarker));
        }

        return BoundDocument(sb, options, markerAlreadyEmitted: isTruncated);
    }

    private static int SlotSeverityIndex(string slot)
        => slot switch {
            "Danger" => 0,
            "Warning" => 1,
            "Success" => 2,
            "Info" => 3,
            "Accent" => 4,
            "Neutral" => 5,
            _ => 6,
        };

    private static RenderedMarkdown RenderTimelineDocument(
        McpResourceDescriptor descriptor,
        IReadOnlyList<McpParameterDescriptor> fields,
        IReadOnlyList<object> items,
        long totalCount,
        bool requestIsTruncated,
        IReadOnlyList<string>? suggestions,
        FrontComposerMcpOptions options,
        CancellationToken cancellationToken) {
        var sb = new StringBuilder(capacity: 2048);
        // R2-P1: Timeline title flows the same operator-supplied descriptor text as Table /
        // StatusOverview; redact before escaping so secrets pasted into Title cannot leak via
        // the timeline strategy.
        sb.Append("## ").AppendLine(EscapeMarkdownText(RedactSensitiveText(descriptor.Title)));
        sb.AppendLine();

        if (items.Count == 0) {
            AppendEmptyState(sb, descriptor, suggestions, options);
            AppendTruncationMarkerIfNeeded(sb, options, requestIsTruncated);
            return BoundDocument(sb, options, markerAlreadyEmitted: requestIsTruncated);
        }

        // Search across all non-unsupported fields so a timestamp/status at index >=
        // MaxFieldsPerResource still drives timeline rendering.
        IReadOnlyList<McpParameterDescriptor> renderableFields = descriptor.Fields
            .Where(f => !f.IsUnsupported)
            .ToArray();
        McpParameterDescriptor? timestampField = renderableFields.FirstOrDefault(IsDateTimeField);
        McpParameterDescriptor? statusField = renderableFields.FirstOrDefault(f => f.BadgeMappings is { Count: > 0 })
            ?? renderableFields.FirstOrDefault(f => string.Equals(f.TypeName, "Enum", StringComparison.Ordinal));
        McpParameterDescriptor? titleField = renderableFields.FirstOrDefault(f => f != timestampField && f != statusField);
        int maxEntries = Math.Max(1, Math.Min(options.MaxRowsPerResource, options.MaxProjectionTimelineEntries));

        TimelineEntry[] entries = [.. items.Select((item, index) => new TimelineEntry(
                item,
                index,
                timestampField is null ? null : ReadDateTimeOffset(ReadPropertyValue(item, timestampField.Name)),
                ReadStableItemKey(item)))
            .OrderBy(e => e.Timestamp is null ? 1 : 0)
            .ThenByDescending(e => e.Timestamp)
            .ThenBy(e => e.ItemKey, StringComparer.Ordinal)
            .ThenBy(e => e.Ordinal)
            .Take(maxEntries)];

        foreach (TimelineEntry entry in entries) {
            cancellationToken.ThrowIfCancellationRequested();
            // DN-1: timeline timestamp uses ISO 8601 round-trip ('o') matching the cell format;
            // do not change agent-visible grammar without a binding decision.
            string timestamp = entry.Timestamp?.ToString("o", CultureInfo.InvariantCulture)
                ?? "No timestamp";
            string status = statusField is null ? string.Empty : FormatCell(ReadPropertyValue(entry.Item, statusField.Name), statusField, options);
            string title = titleField is null ? string.Empty : FormatCell(ReadPropertyValue(entry.Item, titleField.Name), titleField, options);

            sb.Append("- ").Append(EscapeMarkdownText(timestamp)).Append(" - ");
            if (!string.IsNullOrWhiteSpace(status)) {
                sb.Append(status).Append(". ");
            }

            sb.AppendLine(title);
        }

        bool isTruncated = requestIsTruncated || items.Count > entries.Length;
        if (isTruncated) {
            sb.AppendLine();
            sb.AppendLine(EscapeMarkdownText(options.ProjectionTruncationMarker));
        }

        return BoundDocument(sb, options, markerAlreadyEmitted: isTruncated);
    }

    private static void AppendTruncationMarkerIfNeeded(StringBuilder sb, FrontComposerMcpOptions options, bool isTruncated) {
        if (!isTruncated) {
            return;
        }

        sb.AppendLine();
        sb.AppendLine(EscapeMarkdownText(options.ProjectionTruncationMarker));
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
        // R2-P2: redact plural label / title fallback before lower-casing and escaping; both
        // are operator-supplied descriptor text and must follow the same sanitization path as
        // the heading title (P-17).
        string plural = string.IsNullOrWhiteSpace(descriptor.EntityPluralLabel)
            ? descriptor.Title
            : descriptor.EntityPluralLabel!;
        sb.Append("No ").Append(EscapeMarkdownText(RedactSensitiveText(plural).ToLowerInvariant())).AppendLine(" found.");

        IReadOnlyList<string> safeSuggestions = suggestions ?? [];
        int max = Math.Max(0, options.MaxProjectionSuggestions);
        if (safeSuggestions.Count == 0 || max == 0) {
            return;
        }

        // Drop link-shaped suggestions before emit per the Inert Untrusted Text Contract:
        // "Empty-state suggestions use visible descriptor labels only, max 5, no links".
        sb.AppendLine();
        int emitted = 0;
        foreach (string suggestion in safeSuggestions) {
            if (emitted >= max) {
                break;
            }

            if (LooksLikeLinkOrCommand(suggestion)) {
                continue;
            }

            sb.Append("- ").AppendLine(EscapeMarkdownText(RedactSensitiveText(suggestion)));
            emitted++;
        }
    }

    private static bool LooksLikeLinkOrCommand(string value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return true;
        }

        // Reject Markdown link/image/reference syntax, autolinks, schemes, and slash-style
        // command payloads (anywhere in the suggestion text). Visible descriptor labels
        // never contain these constructs.
        ReadOnlySpan<char> trimmed = value.AsSpan().Trim();
        if (trimmed.IndexOfAny(LinkOrCommandChars) >= 0) {
            return true;
        }

        if (trimmed.IndexOf("://", StringComparison.Ordinal) >= 0) {
            return true;
        }

        if (trimmed[0] == '/') {
            return true;
        }

        // Whitespace followed by '/' followed by a non-whitespace char looks like an inline
        // slash command ("run /danger", "please /approve").
        for (int i = 1; i < trimmed.Length - 1; i++) {
            if (trimmed[i] == '/' && char.IsWhiteSpace(trimmed[i - 1]) && !char.IsWhiteSpace(trimmed[i + 1])) {
                return true;
            }
        }

        return false;
    }

    private static bool IsDateTimeField(McpParameterDescriptor field)
        => field.TypeName is "DateTime" or "DateTimeOffset" or "DateOnly";

    private static DateTimeOffset? ReadDateTimeOffset(object? value) {
        // Wrap conversions: new DateTimeOffset(DateTime.MinValue) throws on positive-offset
        // hosts; we degrade per-row to "No timestamp" rather than failing the whole render.
        // P-16: DateTimeKind.Unspecified is not silently treated as UTC (which would shift
        // local-time-stored values backwards by the host offset). We degrade the row to
        // "No timestamp" so timeline ordering is not poisoned by an arbitrary kind assumption.
        try {
            return value switch {
                null => null,
                DateTimeOffset dto => dto,
                DateTime { Kind: DateTimeKind.Unspecified } => null,
                DateTime dt => new DateTimeOffset(dt.ToUniversalTime(), TimeSpan.Zero),
                DateOnly d => new DateTimeOffset(d.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)),
                _ => null,
            };
        }
        catch (ArgumentOutOfRangeException) {
            return null;
        }
    }

    private static string FormatCell(object? value, McpParameterDescriptor field, FrontComposerMcpOptions options) {
        if (field.IsUnsupported) {
            return UnsupportedPlaceholder;
        }

        if (value is null) {
            return "-";
        }

        // DN-1: Story 8-4 happy-path Markdown grammar — bool true/false, ISO 8601 dates with
        // round-trip 'o' format, no empty-string substitution. Story 8-5 corpus depends on this
        // baseline; do not change agent-visible grammar without a P0 security justification and
        // a binding decision.
        string text = value switch {
            DateTime dt => dt.ToString("o", CultureInfo.InvariantCulture),
            DateTimeOffset dto => dto.ToString("o", CultureInfo.InvariantCulture),
            DateOnly d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            TimeOnly t => t.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
            bool b => b ? "true" : "false",
            Enum e => FormatEnum(e, field),
            string s => s,
            IEnumerable enumerable and not string => FormatEnumerable(enumerable),
            IFormattable formattable => FormatFormattable(formattable, field),
            _ => value.ToString() ?? string.Empty,
        };

        return EscapeMarkdownText(TrimCell(RedactSensitiveText(text), Math.Max(4, options.MaxProjectionCellCharacters)));
    }

    private static string FormatFormattable(IFormattable value, McpParameterDescriptor field) {
        string? format = string.Equals(field.DisplayFormat, "Currency", StringComparison.Ordinal)
            ? "C"
            : null;
        return value.ToString(format, CultureInfo.InvariantCulture) ?? string.Empty;
    }

    // R2-P8: precompiled non-backtracking regexes for redaction. NonBacktracking guarantees
    // linear-time matching against adversary-supplied descriptor titles, suggestions, and cell
    // content; the previous instance-per-call Regex.Replace had no MatchTimeout and used the
    // default backtracking engine, which is theoretically wedge-able with crafted dotted input.
    private static readonly Regex SecretAssignmentRegex = new(
        @"\b(api[_-]?key|client[_-]?secret|authorization|password|pwd|secret|token|connection[_-]?string)\s*[:=]\s*\S+",
        RegexOptions.IgnoreCase | RegexOptions.NonBacktracking | RegexOptions.CultureInvariant);
    // P-15: anchor on `\s` (not eaten by the token char class) so "Bearer auth. Then..." does
    // not consume the trailing period or following identifiers; previous regex included `.`
    // in the token class and over-redacted dotted prose.
    private static readonly Regex BearerTokenRegex = new(
        @"\bbearer\s+[A-Za-z0-9_\-]+(?:\.[A-Za-z0-9_\-]+)*",
        RegexOptions.IgnoreCase | RegexOptions.NonBacktracking | RegexOptions.CultureInvariant);
    private static readonly Regex JwtFragmentRegex = new(
        @"\beyJ[A-Za-z0-9_\-]+\.[A-Za-z0-9_\-]+(?:\.[A-Za-z0-9_\-]+)?",
        RegexOptions.NonBacktracking | RegexOptions.CultureInvariant);

    private static string RedactSensitiveText(string text) {
        if (string.IsNullOrEmpty(text)) {
            return string.Empty;
        }

        string redacted = SecretAssignmentRegex.Replace(text, "$1=[redacted]");
        redacted = BearerTokenRegex.Replace(redacted, "Bearer [redacted]");
        redacted = JwtFragmentRegex.Replace(redacted, "[redacted]");
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
        // Per the canonical formatting matrix, arrays/objects render as the unsupported
        // placeholder unless SourceTools provides a scalar safe value. We do not serialize
        // raw collection content into Markdown cells.
        _ = enumerable;
        return UnsupportedPlaceholder;
    }

    private const string UnsupportedPlaceholder = "(unsupported)";

    // P-27: only block characters that survive escaping as link/code constructs after
    // EscapeMarkdownText neutralizes `[`, `]`, `(`, `)`, `<`, `>`. Backticks remain in the
    // shape filter because consecutive backticks form code fences even with per-char escape.
    // The caller still drops `://` URLs and slash-style commands explicitly below.
    private static readonly System.Buffers.SearchValues<char> LinkOrCommandChars =
        System.Buffers.SearchValues.Create("`");

    private static string Humanize(string value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return value;
        }

        var sb = new StringBuilder(value.Length + 8);
        bool prevIsLetterOrDigit = false;
        int i = 0;
        while (i < value.Length) {
            int codePoint;
            int width;
            if (char.IsHighSurrogate(value[i]) && i + 1 < value.Length && char.IsLowSurrogate(value[i + 1])) {
                codePoint = char.ConvertToUtf32(value[i], value[i + 1]);
                width = 2;
            }
            else {
                codePoint = value[i];
                width = 1;
            }

            // P-7: Rune.TryCreate so an unpaired surrogate or invalid code point degrades to
            // non-letter rather than throwing ArgumentOutOfRangeException out of the renderer
            // and collapsing the whole document to DownstreamFailed.
            bool isValidRune = Rune.TryCreate(codePoint, out Rune rune);

            // Insert space before a new uppercase word only when the previous code point was
            // a letter/digit; this preserves "OnHold" → "On Hold" while leaving surrogate pairs
            // intact and avoiding spurious spaces after whitespace/punctuation.
            if (i > 0 && prevIsLetterOrDigit && isValidRune && Rune.IsUpper(rune)) {
                sb.Append(' ');
            }

            sb.Append(value, i, width);
            prevIsLetterOrDigit = isValidRune && Rune.IsLetterOrDigit(rune);
            i += width;
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

    private sealed record TimelineEntry(object Item, int Ordinal, DateTimeOffset? Timestamp, string ItemKey);

    private static readonly string[] StableKeyPropertyNames = ["Id", "Key", "Name"];

    private static string ReadStableItemKey(object item) {
        Type type = item.GetType();
        foreach (string property in StableKeyPropertyNames) {
            // P-6: ORM proxies and lazy properties can throw from a getter; treat any throw as
            // "no stable key" rather than collapsing the whole timeline render to DownstreamFailed.
            // R2-P7: do NOT swallow OperationCanceledException — it must propagate to the outer
            // cancellation handler so a cancelled render returns Canceled instead of silently
            // continuing to the next property and then mis-classifying as DownstreamFailed.
            object? value;
            try {
                value = type.GetProperty(property, BindingFlags.Instance | BindingFlags.Public)?.GetValue(item);
            }
            catch (TargetInvocationException tie) when (tie.InnerException is OperationCanceledException) {
                throw tie.InnerException;
            }
            catch (TargetInvocationException) {
                continue;
            }
            catch (OperationCanceledException) {
                throw;
            }
            catch (Exception) {
                continue;
            }

            if (value is null) {
                continue;
            }

            string? text = value.ToString();
            if (!string.IsNullOrWhiteSpace(text)) {
                return text;
            }
        }

        return string.Empty;
    }

    private static string TrimCell(string text, int maxLength) {
        if (text.Length <= maxLength) {
            return text;
        }

        // Cut on a Rune boundary so a non-BMP code point (astral-plane emoji, surrogate pair)
        // is never split, which would otherwise produce invalid UTF-16 / malformed UTF-8.
        int budget = Math.Max(0, maxLength - 3);
        if (budget == 0) {
            return "...";
        }

        if (budget < text.Length
            && char.IsHighSurrogate(text[budget - 1])
            && char.IsLowSurrogate(text[budget])) {
            budget--;
        }

        return text[..budget] + "...";
    }

    private static RenderedMarkdown BoundDocument(StringBuilder sb, FrontComposerMcpOptions options, bool markerAlreadyEmitted) {
        int max = Math.Max(1, options.MaxProjectionMarkdownCharacters);
        if (sb.Length <= max) {
            return new RenderedMarkdown(sb.ToString(), markerAlreadyEmitted);
        }

        string marker = EscapeMarkdownText(options.ProjectionTruncationMarker);
        int newlineLength = Environment.NewLine.Length;
        int budget = max - marker.Length - newlineLength;
        if (budget <= 0) {
            // Marker alone would exceed the bound; surface this as a sanitized failure rather than
            // returning a string longer than the configured cap.
            throw new FrontComposerMcpException(FrontComposerMcpFailureCategory.ResponseTooLarge);
        }

        // Cut on the last newline before `budget` so the truncation never lands mid-row of a table.
        string buffered = sb.ToString();
        int cut = buffered.LastIndexOf('\n', Math.Min(budget, buffered.Length) - 1);
        // P-13: when an outer renderer already appended the truncation marker, replicating it
        // here would produce two consecutive marker lines. Skip the appended marker and keep
        // the cut prefix only.
        string suffix = markerAlreadyEmitted ? string.Empty : marker + Environment.NewLine;
        // P-8: when no newline exists in the budget window, fall back to a Rune-boundary cut and
        // append the marker rather than discarding the entire document. This preserves the
        // common "single very long heading" case where the previous code surfaced ResponseTooLarge.
        string prefix;
        if (cut < 0) {
            int safeBudget = Math.Min(budget, buffered.Length);
            if (safeBudget > 0
                && safeBudget < buffered.Length
                && char.IsHighSurrogate(buffered[safeBudget - 1])
                && char.IsLowSurrogate(buffered[safeBudget])) {
                safeBudget--;
            }

            prefix = buffered[..safeBudget];
            // No trailing newline in the kept prefix; ensure the marker (when emitted) starts on
            // its own line.
            if (!markerAlreadyEmitted && safeBudget > 0 && buffered[safeBudget - 1] != '\n') {
                suffix = Environment.NewLine + suffix;
            }
        }
        else {
            prefix = buffered[..(cut + 1)];
        }

        string result = prefix + suffix;
        // P-9: defensive clamp — Environment.NewLine prepending can push the total past `max` by
        // newlineLength bytes. Trim the trailing whitespace/newline rather than ship a string
        // longer than the configured cap.
        if (result.Length > max) {
            result = result[..max];
        }

        return new RenderedMarkdown(result, true);
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

        // The per-character escape above already neutralizes GFM task-list markers (`[` → `\[`,
        // `]` → `\]`), Markdown links/images, autolinks, fenced code, and HTML.
        return sb.ToString();
    }

    private sealed record RenderedMarkdown(string Text, bool IsTruncated);
}
