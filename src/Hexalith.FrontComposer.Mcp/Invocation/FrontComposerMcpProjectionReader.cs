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
    IOptions<FrontComposerMcpOptions> options,
    IFrontComposerMcpProjectionRenderer? renderer = null) {
    private static readonly IFrontComposerMcpProjectionRenderer DefaultRenderer = new DefaultFrontComposerMcpProjectionRenderer();

    public async Task<FrontComposerMcpResult> ReadAsync(string uri, CancellationToken cancellationToken = default) {
        if (IsMalformedResourceUri(uri)) {
            return FrontComposerMcpProjectionFailureMapper.ToResult(FrontComposerMcpFailureCategory.MalformedRequest);
        }

        // R2-P5: resolve the epoch provider and visibility gate once at entry. Per-call DI
        // resolution would yield up to four different instances under scoped/transient
        // registrations, defeating monotonic counters and gate-side per-read caches. The
        // captured references are passed to all downstream sampling/validation calls.
        IFrontComposerMcpDescriptorEpochProvider? epochProvider =
            services.GetService<IFrontComposerMcpDescriptorEpochProvider>();
        IFrontComposerMcpResourceVisibilityGate visibilityGate =
            services.GetRequiredService<IFrontComposerMcpResourceVisibilityGate>();

        try {
            // P-1: sample the descriptor epoch BEFORE the descriptor lookup and verify it has not
            // advanced after the lookup; otherwise the snapshot would stamp a fresh epoch onto a
            // stale descriptor, defeating the revalidation contract. P-4: keep registry access
            // inside the try so a non-trivial registry implementation cannot leak raw exceptions.
            McpDescriptorEpochs preLookupEpochs = CurrentEpochs(epochProvider);
            if (!registry.TryGetResource(uri, out McpResourceDescriptor? descriptor)) {
                return FrontComposerMcpProjectionFailureMapper.ToResult(FrontComposerMcpFailureCategory.UnknownResource);
            }

            McpDescriptorEpochs postLookupEpochs = CurrentEpochs(epochProvider);
            if (preLookupEpochs.DescriptorEpoch != postLookupEpochs.DescriptorEpoch
                || preLookupEpochs.CatalogEpoch != postLookupEpochs.CatalogEpoch) {
                return FrontComposerMcpProjectionFailureMapper.ToResult(FrontComposerMcpFailureCategory.StaleDescriptor);
            }

            FrontComposerMcpAgentContext context = agentContextAccessor.GetContext();
            ValidateContext(context);

            if (!await IsResourceVisibleAsync(visibilityGate, descriptor, context, cancellationToken).ConfigureAwait(false)) {
                return FrontComposerMcpProjectionFailureMapper.ToResult(FrontComposerMcpFailureCategory.UnknownResource);
            }

            FrontComposerMcpProjectionReadSnapshot snapshot = CreateSnapshot(descriptor, postLookupEpochs, cancellationToken);
            FrontComposerMcpFailureCategory? preQueryFailure = await ValidateSnapshotAsync(snapshot, context, epochProvider, visibilityGate, cancellationToken).ConfigureAwait(false);
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
            FrontComposerMcpFailureCategory? preRenderFailure = await ValidateSnapshotAsync(snapshot, context, epochProvider, visibilityGate, cancellationToken).ConfigureAwait(false);
            if (preRenderFailure is not null) {
                return FrontComposerMcpProjectionFailureMapper.ToResult(preRenderFailure.Value);
            }

            McpProjectionRenderRequest renderRequest = await BuildRenderRequestAsync(
                snapshot,
                queryResult,
                cancellationToken).ConfigureAwait(false);
            McpProjectionRenderResult render = (renderer ?? DefaultRenderer).Render(
                renderRequest,
                options.Value,
                cancellationToken);

            return render.IsSuccess
                ? FrontComposerMcpResult.Success(render.Document!.Text, new JsonObject { ["contentType"] = render.ContentType })
                : FrontComposerMcpProjectionFailureMapper.ToResult(render.Category);
        }
        catch (OperationCanceledException) {
            // DN-5: collapse to a single Canceled handler. Linked-CTS timeouts surface here too;
            // explicit timeouts must throw TimeoutException to differentiate.
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

        // P-2: null-check Principal before dereferencing Identity. A null Principal indicates
        // a misconfigured accessor; classify as AuthFailed rather than letting NullReferenceException
        // collapse to DownstreamFailed and lose the auth signal.
        if (string.IsNullOrWhiteSpace(context.UserId)
            || context.Principal is null
            || context.Principal.Identity is null
            || !context.Principal.Identity.IsAuthenticated) {
            throw new FrontComposerMcpException(FrontComposerMcpFailureCategory.AuthFailed);
        }
    }

    private static FrontComposerMcpProjectionReadSnapshot CreateSnapshot(
        McpResourceDescriptor descriptor,
        McpDescriptorEpochs epochs,
        CancellationToken cancellationToken) {
        // P-20: keep the snapshot to the safe immutable subset needed for query/render
        // handoff. Mutable registry handles and service instances never cross the admission
        // boundary; field metadata is copied with collection values detached below.
        FrontComposerMcpProjectionDescriptorSnapshot descriptorSnapshot =
            FrontComposerMcpProjectionDescriptorSnapshot.FromDescriptor(descriptor);
        return new FrontComposerMcpProjectionReadSnapshot(
            ProjectionKey: descriptorSnapshot.Name,
            ProtocolUriCategory: "frontcomposer_projection",
            RenderStrategy: descriptorSnapshot.RenderStrategy,
            BoundedContext: descriptorSnapshot.BoundedContext,
            DescriptorEpoch: epochs.DescriptorEpoch,
            CatalogEpoch: epochs.CatalogEpoch,
            QueryShapeCategory: "take",
            RequestId: Guid.NewGuid().ToString("n"),
            CancellationToken: cancellationToken,
            Descriptor: descriptorSnapshot);
    }

    private McpDescriptorEpochs CurrentEpochs(IFrontComposerMcpDescriptorEpochProvider? provider)
        => provider?.GetEpochs() ?? registry.GetEpochs();

    private async ValueTask<FrontComposerMcpFailureCategory?> ValidateSnapshotAsync(
        FrontComposerMcpProjectionReadSnapshot snapshot,
        FrontComposerMcpAgentContext context,
        IFrontComposerMcpDescriptorEpochProvider? epochProvider,
        IFrontComposerMcpResourceVisibilityGate visibilityGate,
        CancellationToken cancellationToken) {
        McpDescriptorEpochs current = CurrentEpochs(epochProvider);
        if (current.DescriptorEpoch != snapshot.DescriptorEpoch
            || current.CatalogEpoch != snapshot.CatalogEpoch) {
            return FrontComposerMcpFailureCategory.StaleDescriptor;
        }

        return await IsResourceVisibleAsync(visibilityGate, snapshot.Descriptor.ToDescriptor(), context, cancellationToken).ConfigureAwait(false)
            ? null
            : FrontComposerMcpFailureCategory.UnknownResource;
    }

    private static ValueTask<bool> IsResourceVisibleAsync(
        IFrontComposerMcpResourceVisibilityGate gate,
        McpResourceDescriptor descriptor,
        FrontComposerMcpAgentContext context,
        CancellationToken cancellationToken)
        // P-3 / R2-P5: visibility revalidation is a security contract; the gate is resolved once
        // per ReadAsync via GetRequiredService at the entry point so all three call sites (admission,
        // pre-query, pre-render) target the same instance. Hosts must register a real gate (or
        // AllowAllResourceVisibilityGate explicitly for sample/dev). The startup probe in
        // AddFrontComposerMcp enforces registration so misconfigured hosts fail at registration time
        // rather than silently shipping unrestricted reads.
        => gate.IsVisibleAsync(descriptor, context, cancellationToken);

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
        McpResourceDescriptor descriptor = snapshot.Descriptor.ToDescriptor();
        Type resultType = queryResult.GetType();
        object? itemsValue = resultType.GetProperty("Items")?.GetValue(queryResult);
        long totalCount = ReadTotalCount(resultType, queryResult);

        // Use non-generic IEnumerable so value-type projections (e.g. struct records) are not
        // silently rendered as zero rows by an IEnumerable<object> covariance miss.
        // P-10: count raw entries (including nulls) so IsTruncated reflects the underlying query
        // page rather than the post-null-filter count, otherwise a complete page with one null
        // entry would mark IsTruncated=true.
        List<object> items = [];
        long rawCount = 0;
        if (itemsValue is IEnumerable raw) {
            foreach (object? item in raw) {
                rawCount++;
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
            IsTruncated: totalCount > rawCount,
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

        IFrontComposerMcpVisibleToolCatalogProvider? admission =
            services.GetService<IFrontComposerMcpVisibleToolCatalogProvider>()
            ?? services.GetService<FrontComposerMcpToolAdmissionService>();
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
        // P-5: fail-closed when the projection has no bounded context — without an anchor we
        // cannot verify cross-context isolation, so suggestions are suppressed entirely.
        if (string.IsNullOrWhiteSpace(boundedContext)
            || !string.Equals(descriptor.BoundedContext, boundedContext, StringComparison.Ordinal)) {
            return false;
        }

        return SegmentMatches(descriptor.CommandTypeName, cta)
            || SegmentMatches(descriptor.ProtocolName, cta);
    }

    private static bool SegmentMatches(string fullName, string cta) {
        // R2-P3: anchor on the last dotted segment (the type / protocol name) instead of any
        // segment. A CTA like "Create" must not match `Other.Create.IrrelevantCommand` simply
        // because `Create` happens to be a namespace-internal segment. The full-string equality
        // check below preserves the case where `fullName` is itself the unqualified name.
        if (string.Equals(fullName, cta, StringComparison.Ordinal)) {
            return true;
        }

        int lastDot = fullName.LastIndexOf('.');
        if (lastDot < 0 || lastDot == fullName.Length - 1) {
            return false;
        }

        ReadOnlySpan<char> lastSegment = fullName.AsSpan(lastDot + 1);
        return lastSegment.Equals(cta.AsSpan(), StringComparison.Ordinal);
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
