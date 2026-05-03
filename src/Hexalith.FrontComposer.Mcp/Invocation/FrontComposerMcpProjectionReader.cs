using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text.Json.Nodes;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Mcp;
using Hexalith.FrontComposer.Mcp.Rendering;

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
            McpProjectionRenderRequest renderRequest = await BuildRenderRequestAsync(
                descriptor,
                queryResult,
                cancellationToken).ConfigureAwait(false);
            McpProjectionRenderResult render = McpMarkdownProjectionRenderer.Render(
                renderRequest,
                options.Value,
                cancellationToken);

            return render.IsSuccess
                ? FrontComposerMcpResult.Success(render.Document!.Text, new JsonObject { ["contentType"] = render.ContentType })
                : FrontComposerMcpResult.Failure(render.Category);
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

    private async Task<McpProjectionRenderRequest> BuildRenderRequestAsync(
        McpResourceDescriptor descriptor,
        object queryResult,
        CancellationToken cancellationToken) {
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

        IReadOnlyList<string>? suggestions = items.Count == 0
            ? await BuildSafeSuggestionsAsync(descriptor, cancellationToken).ConfigureAwait(false)
            : null;

        return new McpProjectionRenderRequest(
            descriptor,
            items,
            totalCount,
            SafeCommandSuggestions: suggestions);
    }

    private async ValueTask<IReadOnlyList<string>?> BuildSafeSuggestionsAsync(
        McpResourceDescriptor descriptor,
        CancellationToken cancellationToken) {
        if (string.IsNullOrWhiteSpace(descriptor.EmptyStateCtaCommandName)) {
            return null;
        }

        var admission = services.GetService<FrontComposerMcpToolAdmissionService>();
        if (admission is null) {
            return null;
        }

        McpVisibleToolCatalog catalog = await admission.BuildVisibleCatalogAsync(cancellationToken).ConfigureAwait(false);
        string cta = descriptor.EmptyStateCtaCommandName!;
        string[] suggestions = [.. catalog.Tools
            .Where(t => MatchesEmptyStateCta(t.Descriptor, cta))
            .OrderBy(t => t.Name, StringComparer.Ordinal)
            .Select(t => t.Title)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Take(Math.Max(1, options.Value.MaxProjectionSuggestions))];
        return suggestions.Length == 0 ? null : suggestions;
    }

    private static bool MatchesEmptyStateCta(McpCommandDescriptor descriptor, string cta)
        => string.Equals(descriptor.CommandTypeName, cta, StringComparison.Ordinal)
            || descriptor.CommandTypeName.EndsWith("." + cta, StringComparison.Ordinal)
            || string.Equals(descriptor.ProtocolName, cta, StringComparison.Ordinal)
            || descriptor.ProtocolName.Contains("." + cta + ".", StringComparison.Ordinal);

    private static long ReadTotalCount(Type resultType, object queryResult) {
        object? raw = resultType.GetProperty("TotalCount")?.GetValue(queryResult);
        return raw is null ? 0L : Convert.ToInt64(raw, CultureInfo.InvariantCulture);
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
