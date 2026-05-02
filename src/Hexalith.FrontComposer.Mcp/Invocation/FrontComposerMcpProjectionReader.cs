using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Mcp;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Mcp.Invocation;

public sealed class FrontComposerMcpProjectionReader(
    FrontComposerMcpDescriptorRegistry registry,
    IFrontComposerMcpAgentContextAccessor agentContextAccessor,
    IServiceProvider services,
    IOptions<FrontComposerMcpOptions> options) {
    public async Task<FrontComposerMcpResult> ReadAsync(string uri, CancellationToken cancellationToken = default) {
        if (string.IsNullOrWhiteSpace(uri)) {
            return FrontComposerMcpResult.Failure(FrontComposerMcpFailureCategory.MalformedRequest);
        }

        if (!registry.TryGetResource(uri, out McpResourceDescriptor? descriptor)) {
            return FrontComposerMcpResult.Failure(FrontComposerMcpFailureCategory.UnknownResource);
        }

        try {
            FrontComposerMcpAgentContext context = agentContextAccessor.GetContext();
            Type projectionType = ResolveType(descriptor.ProjectionTypeName);
            IQueryService queryService = services.GetRequiredService<IQueryService>();
            int take = Math.Max(1, Math.Min(options.Value.DefaultResourceTake, options.Value.MaxResourceTake));
            QueryRequest request = new(
                ProjectionType: descriptor.ProjectionTypeName,
                TenantId: context.TenantId,
                Take: take);
            MethodInfo? method = typeof(IQueryService).GetMethod(nameof(IQueryService.QueryAsync));
            if (method is null) {
                throw new FrontComposerMcpException(FrontComposerMcpFailureCategory.UnsupportedSchema);
            }

            object? resultTask;
            try {
                resultTask = method.MakeGenericMethod(projectionType).Invoke(queryService, [request, cancellationToken]);
            }
            catch (TargetInvocationException ex) when (ex.InnerException is not null) {
                throw ex.InnerException;
            }

            object queryResult = await AwaitDynamic(resultTask!).ConfigureAwait(false);
            string text = RenderResult(descriptor, queryResult, options.Value);
            return FrontComposerMcpResult.Success(text, new JsonObject { ["contentType"] = "text/markdown" });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            return FrontComposerMcpResult.Failure(FrontComposerMcpFailureCategory.Canceled);
        }
        catch (TimeoutException) {
            return FrontComposerMcpResult.Failure(FrontComposerMcpFailureCategory.Timeout);
        }
        catch (FrontComposerMcpException ex) {
            return FrontComposerMcpResult.Failure(ex.Category);
        }
        catch {
            return FrontComposerMcpResult.Failure(FrontComposerMcpFailureCategory.DownstreamFailed);
        }
    }

    private static async Task<object> AwaitDynamic(object taskObject) {
        await ((Task)taskObject).ConfigureAwait(false);
        PropertyInfo? resultProperty = taskObject.GetType().GetProperty("Result");
        return resultProperty?.GetValue(taskObject)
            ?? throw new FrontComposerMcpException(FrontComposerMcpFailureCategory.DownstreamFailed);
    }

    private static string RenderResult(McpResourceDescriptor descriptor, object queryResult, FrontComposerMcpOptions options) {
        Type resultType = queryResult.GetType();
        object? itemsValue = resultType.GetProperty("Items")?.GetValue(queryResult);
        long totalCount = ReadTotalCount(resultType, queryResult);

        // Use non-generic IEnumerable so value-type projections (e.g. struct records) are not
        // silently rendered as zero rows by an IEnumerable<object> covariance miss.
        List<object> items = [];
        if (itemsValue is IEnumerable raw) {
            foreach (object? item in raw) {
                if (item is not null) {
                    items.Add(item);
                }
            }
        }

        int maxFields = Math.Max(1, options.MaxFieldsPerResource);
        int maxRows = Math.Max(1, options.MaxRowsPerResource);
        List<McpParameterDescriptor> allFields = [.. descriptor.Fields.Where(f => !f.IsUnsupported)];
        List<McpParameterDescriptor> fields = [.. allFields.Take(maxFields)];

        var sb = new StringBuilder();
        sb.AppendLine("# " + descriptor.Title);
        sb.AppendLine();
        sb.AppendLine("Total: " + totalCount.ToString(CultureInfo.InvariantCulture));
        if (fields.Count == 0) {
            return sb.ToString();
        }

        sb.AppendLine();
        sb.AppendLine("| " + string.Join(" | ", fields.Select(f => f.Title)) + " |");
        sb.AppendLine("| " + string.Join(" | ", fields.Select(_ => "---")) + " |");
        int rendered = 0;
        foreach (object item in items) {
            if (rendered >= maxRows) {
                break;
            }

            sb.Append("| ");
            sb.Append(string.Join(" | ", fields.Select(f => SanitizeCell(item.GetType().GetProperty(f.Name)?.GetValue(item)))));
            sb.AppendLine(" |");
            rendered++;
        }

        // Sanitized truncation hints — do not reveal hidden tenant data or actual omitted-row count
        // beyond what items.Count already exposes; the marker simply tells the agent the response
        // was capped.
        if (allFields.Count > fields.Count) {
            sb.AppendLine();
            sb.AppendLine("_Additional fields omitted; raise MaxFieldsPerResource to expose them._");
        }

        if (items.Count > rendered) {
            sb.AppendLine();
            sb.AppendLine("_Result truncated; raise MaxRowsPerResource or refine query to expose more rows._");
        }

        return sb.ToString();
    }

    private static long ReadTotalCount(Type resultType, object queryResult) {
        object? raw = resultType.GetProperty("TotalCount")?.GetValue(queryResult);
        return raw is null ? 0L : Convert.ToInt64(raw, CultureInfo.InvariantCulture);
    }

    private static string SanitizeCell(object? value) {
        string text = value switch {
            null => string.Empty,
            DateTime dt => dt.ToString("o", CultureInfo.InvariantCulture),
            DateTimeOffset dto => dto.ToString("o", CultureInfo.InvariantCulture),
            DateOnly d => d.ToString("o", CultureInfo.InvariantCulture),
            TimeOnly t => t.ToString("o", CultureInfo.InvariantCulture),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty,
        };

        // Order matters: pre-escape backslash before introducing escape sequences for pipe so
        // a literal "\|" in source data survives round-trip without collapsing into "\\|" being
        // re-parsed as escaped-pipe by markdown clients. Backticks/asterisks/brackets are escaped
        // so cell content cannot inject formatting.
        StringBuilder sb = new(text.Length);
        foreach (char c in text) {
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
                case '<':
                    sb.Append("\\<");
                    break;
                case '>':
                    sb.Append("\\>");
                    break;
                case '\r':
                case '\n':
                    sb.Append(' ');
                    break;
                default:
                    sb.Append(c);
                    break;
            }
        }

        return sb.ToString();
    }

    private Type ResolveType(string typeName) {
        // See FrontComposerMcpCommandInvoker.ResolveType for the bounded-then-fallback rationale.
        Type? direct = Type.GetType(typeName);
        if (direct is not null) {
            return direct;
        }

        foreach (Assembly assembly in options.Value.ManifestAssemblies) {
            Type? hit = assembly.GetType(typeName, throwOnError: false, ignoreCase: false);
            if (hit is not null) {
                return hit;
            }
        }

        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
            Type? hit = assembly.GetType(typeName, throwOnError: false, ignoreCase: false);
            if (hit is not null) {
                return hit;
            }
        }

        throw new FrontComposerMcpException(FrontComposerMcpFailureCategory.UnsupportedSchema);
    }
}
