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
        if (IsMalformedResourceUri(uri)) {
            return FrontComposerMcpProjectionFailureMapper.ToResult(FrontComposerMcpFailureCategory.MalformedRequest);
        }

        if (!registry.TryGetResource(uri, out McpResourceDescriptor? descriptor)) {
            return FrontComposerMcpProjectionFailureMapper.ToResult(FrontComposerMcpFailureCategory.UnknownResource);
        }

        try {
            FrontComposerMcpAgentContext context = agentContextAccessor.GetContext();
            ValidateContext(context);

            if (!await IsResourceVisibleAsync(descriptor, context, cancellationToken).ConfigureAwait(false)) {
                return FrontComposerMcpProjectionFailureMapper.ToResult(FrontComposerMcpFailureCategory.UnknownResource);
            }

            FrontComposerMcpProjectionReadSnapshot snapshot = CreateSnapshot(descriptor, cancellationToken);
            FrontComposerMcpFailureCategory? preQueryFailure = await ValidateSnapshotAsync(snapshot, context, cancellationToken).ConfigureAwait(false);
            if (preQueryFailure is not null) {
                return FrontComposerMcpProjectionFailureMapper.ToResult(preQueryFailure.Value);
            }

            Type projectionType = ResolveType(snapshot.Descriptor.ProjectionTypeName);
            IQueryService queryService = services.GetRequiredService<IQueryService>();
            int take = Math.Max(1, Math.Min(options.Value.DefaultResourceTake, options.Value.MaxResourceTake));
            QueryRequest request = new(
                ProjectionType: snapshot.Descriptor.ProjectionTypeName,
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
            FrontComposerMcpFailureCategory? preRenderFailure = await ValidateSnapshotAsync(snapshot, context, cancellationToken).ConfigureAwait(false);
            if (preRenderFailure is not null) {
                return FrontComposerMcpProjectionFailureMapper.ToResult(preRenderFailure.Value);
            }

            McpProjectionRenderRequest renderRequest = await BuildRenderRequestAsync(
                snapshot,
                queryResult,
                cancellationToken).ConfigureAwait(false);
            McpProjectionRenderResult render = McpMarkdownProjectionRenderer.Render(
                renderRequest,
                options.Value,
                cancellationToken);

            return render.IsSuccess
                ? FrontComposerMcpResult.Success(render.Document!.Text, new JsonObject { ["contentType"] = render.ContentType })
                : FrontComposerMcpProjectionFailureMapper.ToResult(render.Category);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            return FrontComposerMcpProjectionFailureMapper.ToResult(FrontComposerMcpFailureCategory.Canceled);
        }
        catch (OperationCanceledException) {
            return FrontComposerMcpProjectionFailureMapper.ToResult(FrontComposerMcpFailureCategory.Canceled);
        }
        catch (TimeoutException) {
            return FrontComposerMcpProjectionFailureMapper.ToResult(FrontComposerMcpFailureCategory.Timeout);
        }
        catch (FrontComposerMcpException ex) {
            return FrontComposerMcpProjectionFailureMapper.ToResult(ex.Category);
        }
        catch {
            return FrontComposerMcpProjectionFailureMapper.ToResult(FrontComposerMcpFailureCategory.DownstreamFailed);
        }
    }

    private static bool IsMalformedResourceUri(string uri) {
        if (string.IsNullOrWhiteSpace(uri)) {
            return true;
        }

        if (!Uri.TryCreate(uri, UriKind.Absolute, out Uri? parsed)) {
            return true;
        }

        return !string.Equals(parsed.Scheme, "frontcomposer", StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(parsed.Host);
    }

    private static void ValidateContext(FrontComposerMcpAgentContext context) {
        if (string.IsNullOrWhiteSpace(context.TenantId)) {
            throw new FrontComposerMcpException(FrontComposerMcpFailureCategory.TenantMissing);
        }

        if (string.IsNullOrWhiteSpace(context.UserId)
            || context.Principal.Identity is null
            || !context.Principal.Identity.IsAuthenticated) {
            throw new FrontComposerMcpException(FrontComposerMcpFailureCategory.AuthFailed);
        }
    }

    private FrontComposerMcpProjectionReadSnapshot CreateSnapshot(
        McpResourceDescriptor descriptor,
        CancellationToken cancellationToken) {
        McpDescriptorEpochs epochs = CurrentEpochs();
        McpResourceDescriptor descriptorCopy = CopyDescriptor(descriptor);
        return new FrontComposerMcpProjectionReadSnapshot(
            ProjectionKey: descriptorCopy.Name,
            ProtocolUriCategory: "frontcomposer_projection",
            RenderStrategy: descriptorCopy.RenderStrategy,
            BoundedContext: descriptorCopy.BoundedContext,
            DescriptorEpoch: epochs.DescriptorEpoch,
            CatalogEpoch: epochs.CatalogEpoch,
            QueryShapeCategory: "take",
            RequestId: Guid.NewGuid().ToString("n"),
            CancellationToken: cancellationToken,
            Descriptor: descriptorCopy);
    }

    private static McpResourceDescriptor CopyDescriptor(McpResourceDescriptor descriptor)
        => descriptor with {
            Fields = descriptor.Fields.ToArray(),
        };

    private McpDescriptorEpochs CurrentEpochs()
        => services.GetService<IFrontComposerMcpDescriptorEpochProvider>()?.GetEpochs()
            ?? registry.GetEpochs();

    private async ValueTask<FrontComposerMcpFailureCategory?> ValidateSnapshotAsync(
        FrontComposerMcpProjectionReadSnapshot snapshot,
        FrontComposerMcpAgentContext context,
        CancellationToken cancellationToken) {
        McpDescriptorEpochs current = CurrentEpochs();
        if (current.DescriptorEpoch != snapshot.DescriptorEpoch
            || current.CatalogEpoch != snapshot.CatalogEpoch) {
            return FrontComposerMcpFailureCategory.StaleDescriptor;
        }

        return await IsResourceVisibleAsync(snapshot.Descriptor, context, cancellationToken).ConfigureAwait(false)
            ? null
            : FrontComposerMcpFailureCategory.UnknownResource;
    }

    private async ValueTask<bool> IsResourceVisibleAsync(
        McpResourceDescriptor descriptor,
        FrontComposerMcpAgentContext context,
        CancellationToken cancellationToken) {
        IFrontComposerMcpResourceVisibilityGate? gate = services.GetService<IFrontComposerMcpResourceVisibilityGate>();
        if (gate is null) {
            return true;
        }

        return await gate.IsVisibleAsync(descriptor, context, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<object> AwaitDynamic(object taskObject) {
        await ((Task)taskObject).ConfigureAwait(false);
        PropertyInfo? resultProperty = taskObject.GetType().GetProperty("Result");
        return resultProperty?.GetValue(taskObject)
            ?? throw new FrontComposerMcpException(FrontComposerMcpFailureCategory.DownstreamFailed);
    }

    private async Task<McpProjectionRenderRequest> BuildRenderRequestAsync(
        FrontComposerMcpProjectionReadSnapshot snapshot,
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
            ? await BuildSafeSuggestionsAsync(snapshot.Descriptor, cancellationToken).ConfigureAwait(false)
            : null;

        return new McpProjectionRenderRequest(
            snapshot.Descriptor,
            items,
            totalCount,
            IsTruncated: totalCount > items.Count,
            RequestId: snapshot.RequestId,
            SafeCommandSuggestions: suggestions);
    }

    private async ValueTask<IReadOnlyList<string>?> BuildSafeSuggestionsAsync(
        McpResourceDescriptor descriptor,
        CancellationToken cancellationToken) {
        if (string.IsNullOrWhiteSpace(descriptor.EmptyStateCtaCommandName)) {
            return null;
        }

        int max = options.Value.MaxProjectionSuggestions;
        if (max <= 0) {
            return null;
        }

        var admission = services.GetService<FrontComposerMcpToolAdmissionService>();
        if (admission is null) {
            return null;
        }

        McpVisibleToolCatalog catalog = await admission.BuildVisibleCatalogAsync(cancellationToken).ConfigureAwait(false);
        string cta = descriptor.EmptyStateCtaCommandName!;
        string[] suggestions = [.. catalog.Tools
            .Where(t => MatchesEmptyStateCta(t.Descriptor, descriptor.BoundedContext, cta))
            .OrderBy(t => t.Name, StringComparer.Ordinal)
            .Select(t => t.Title)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Take(max)];
        return suggestions.Length == 0 ? null : suggestions;
    }

    private static bool MatchesEmptyStateCta(McpCommandDescriptor descriptor, string boundedContext, string cta) {
        // Anchor on full namespace-segment equality so "Create" cannot match
        // "Other.Foo.CreateInvoiceCommand". Cross-bounded-context bleed is rejected unless
        // the descriptor advertises the same bounded context as the projection.
        if (!string.IsNullOrWhiteSpace(boundedContext)
            && !string.Equals(descriptor.BoundedContext, boundedContext, StringComparison.Ordinal)) {
            return false;
        }

        return SegmentMatches(descriptor.CommandTypeName, cta)
            || SegmentMatches(descriptor.ProtocolName, cta);
    }

    private static bool SegmentMatches(string fullName, string cta) {
        if (string.Equals(fullName, cta, StringComparison.Ordinal)) {
            return true;
        }

        foreach (string segment in fullName.Split('.', StringSplitOptions.RemoveEmptyEntries)) {
            if (string.Equals(segment, cta, StringComparison.Ordinal)) {
                return true;
            }
        }

        return false;
    }

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
