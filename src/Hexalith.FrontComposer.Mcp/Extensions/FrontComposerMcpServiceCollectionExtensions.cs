using System.Text.Json;

using Hexalith.FrontComposer.Mcp.Invocation;
using Hexalith.FrontComposer.Contracts.Lifecycle;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Hexalith.FrontComposer.Mcp.Extensions;

public static class FrontComposerMcpServiceCollectionExtensions {
    public static IServiceCollection AddFrontComposerMcp(
        this IServiceCollection services,
        Action<FrontComposerMcpOptions>? configure = null) {
        ArgumentNullException.ThrowIfNull(services);

        if (configure is not null) {
            services.Configure(configure);
        }

        services.AddSingleton<IValidateOptions<FrontComposerMcpOptions>, FrontComposerMcpOptionsValidator>();
        services.AddHttpContextAccessor();
        services.TryAddSingleton<FrontComposerMcpDescriptorRegistry>();
        services.TryAddScoped<FrontComposerMcpToolAdmissionService>();
        // No default IFrontComposerMcpTenantToolGate registration: tenant isolation is fail-closed
        // by contract, so the host MUST register a real gate (or AllowAllMcpTenantToolGate explicitly
        // for samples). The probe step below confirms this so misconfiguration fails at startup.
        services.TryAddScoped<IFrontComposerMcpAgentContextAccessor, HttpFrontComposerMcpAgentContextAccessor>();
        services.TryAddScoped<FrontComposerMcpCommandInvoker>();
        services.TryAddScoped<FrontComposerMcpProjectionReader>();
        services.TryAddScoped<FrontComposerMcpLifecycleTracker>();
        services.TryAddSingleton<IUlidFactory, FrontComposerMcpUlidFactory>();

        // The MCP SDK's WithTools/WithResources takes a static enumerable, so the descriptor list
        // is materialized once at AddFrontComposerMcp time. Adopters MUST call AddFrontComposerMcp
        // AFTER all options Configure(...) calls; later mutations to FrontComposerMcpOptions are
        // not reflected in the SDK-side tool catalog. The IValidateOptions guard above runs at
        // probe time so misconfiguration fails fast with a deterministic message.
        using ServiceProvider probe = services.BuildServiceProvider();
        if (probe.GetService<IFrontComposerMcpTenantToolGate>() is null) {
            throw new InvalidOperationException(
                "AddFrontComposerMcp requires an IFrontComposerMcpTenantToolGate registration. " +
                "Register a host-supplied gate before AddFrontComposerMcp, or use AddSingleton<IFrontComposerMcpTenantToolGate, AllowAllMcpTenantToolGate>() " +
                "explicitly for sample/dev hosts.");
        }

        FrontComposerMcpDescriptorRegistry registry = probe.GetRequiredService<FrontComposerMcpDescriptorRegistry>();
        IEnumerable<ModelContextProtocol.Server.McpServerResource> resources = registry.Resources
            .Select(r => new FrontComposerMcpResource(r))
            .ToArray();

        services.AddMcpServer()
            .WithHttpTransport()
            .WithListToolsHandler(ListToolsAsync)
            .WithCallToolHandler(CallToolAsync)
            .WithResources(resources);

        return services;
    }

    private static async ValueTask<ListToolsResult> ListToolsAsync(
        RequestContext<ListToolsRequestParams> request,
        CancellationToken cancellationToken) {
        if (request.Services is null) {
            return new ListToolsResult { Tools = [] };
        }

        FrontComposerMcpToolAdmissionService admission = request.Services.GetRequiredService<FrontComposerMcpToolAdmissionService>();
        try {
            McpVisibleToolCatalog catalog = await admission.BuildVisibleCatalogAsync(cancellationToken).ConfigureAwait(false);
            FrontComposerMcpOptions options = request.Services.GetRequiredService<IOptions<FrontComposerMcpOptions>>().Value;
            return new ListToolsResult {
                Tools = [
                    .. catalog.Tools.Select(FrontComposerMcpProtocolMapper.ToProtocolTool),
                    FrontComposerMcpProtocolMapper.ToLifecycleTool(options),
                ],
            };
        }
        catch (FrontComposerMcpException) {
            // AC10/AC11: do not surface AuthFailed/TenantMissing as a distinct error to MCP clients —
            // the unified hidden/unknown public surface returns an empty list.
            return new ListToolsResult { Tools = [] };
        }
    }

    private static async ValueTask<CallToolResult> CallToolAsync(
        RequestContext<CallToolRequestParams> request,
        CancellationToken cancellationToken) {
        if (request.Services is null) {
            // Host wiring problem (no DI scope on the request) — surface as schema/config issue,
            // not as a downstream service failure that operators would chase.
            return FrontComposerMcpProtocolMapper.ToCallToolResult(
                FrontComposerMcpResult.Failure(FrontComposerMcpFailureCategory.UnsupportedSchema));
        }

        FrontComposerMcpOptions options = request.Services.GetRequiredService<IOptions<FrontComposerMcpOptions>>().Value;
        if (string.Equals(request.Params?.Name, options.LifecycleToolName, StringComparison.Ordinal)) {
            // D6 / AC10: the lifecycle tool requires a resolvable agent context (auth + tenant
            // claims) before any handle lookup. Per-handle revalidation runs again inside ReadAsync;
            // the entry-point check fails closed for callers without context so the lifecycle tool
            // cannot be probed anonymously.
            try {
                _ = request.Services.GetRequiredService<IFrontComposerMcpAgentContextAccessor>().GetContext();
            }
            catch (FrontComposerMcpException ex) {
                return FrontComposerMcpProtocolMapper.ToCallToolResult(FrontComposerMcpResult.Failure(ex.Category));
            }

            FrontComposerMcpLifecycleTracker tracker = request.Services.GetRequiredService<FrontComposerMcpLifecycleTracker>();
            FrontComposerMcpResult lifecycle = await tracker.ReadAsync(
                BuildArguments(request.Params?.Arguments),
                cancellationToken).ConfigureAwait(false);
            return FrontComposerMcpProtocolMapper.ToCallToolResult(lifecycle);
        }

        FrontComposerMcpCommandInvoker invoker = request.Services.GetRequiredService<FrontComposerMcpCommandInvoker>();
        FrontComposerMcpResult result = await invoker.InvokeAsync(
            request.Params?.Name!,
            BuildArguments(request.Params?.Arguments),
            cancellationToken).ConfigureAwait(false);

        return FrontComposerMcpProtocolMapper.ToCallToolResult(result);
    }

    private static IReadOnlyDictionary<string, JsonElement>? BuildArguments(IDictionary<string, JsonElement>? source) {
        if (source is null) {
            return null;
        }

        // Ordinal comparer: parameter names are canonical (D4 — agents must use canonical visible
        // names; same logic extends to argument keys). The downstream invoker re-checks for
        // case-variant duplicates with an OrdinalIgnoreCase HashSet so that two keys differing only
        // in case are reported as ValidationFailed rather than collapsed.
        return new Dictionary<string, JsonElement>(source, StringComparer.Ordinal);
    }
}

internal sealed class FrontComposerMcpOptionsValidator : IValidateOptions<FrontComposerMcpOptions> {
    public ValidateOptionsResult Validate(string? name, FrontComposerMcpOptions options) {
        List<string> errors = [];

        if (string.IsNullOrWhiteSpace(options.EndpointPattern) || !options.EndpointPattern.StartsWith('/')) {
            errors.Add($"{nameof(FrontComposerMcpOptions.EndpointPattern)} must be a non-empty path starting with '/'.");
        }

        if (string.IsNullOrWhiteSpace(options.ApiKeyHeaderName)) {
            errors.Add($"{nameof(FrontComposerMcpOptions.ApiKeyHeaderName)} must be a non-empty header name.");
        }

        if (options.MaxArgumentBytes <= 0) {
            errors.Add($"{nameof(FrontComposerMcpOptions.MaxArgumentBytes)} must be positive.");
        }

        if (options.DefaultResourceTake <= 0 || options.MaxResourceTake <= 0) {
            errors.Add("Resource take limits must be positive.");
        }

        if (options.DefaultResourceTake > options.MaxResourceTake) {
            errors.Add($"{nameof(FrontComposerMcpOptions.DefaultResourceTake)} must not exceed {nameof(FrontComposerMcpOptions.MaxResourceTake)}.");
        }

        if (options.MaxRowsPerResource <= 0 || options.MaxFieldsPerResource <= 0) {
            errors.Add("Per-resource render limits must be positive.");
        }

        if (options.MaxVisibleToolListItems <= 0) {
            errors.Add($"{nameof(FrontComposerMcpOptions.MaxVisibleToolListItems)} must be positive.");
        }

        if (options.MaxSuggestionCandidates <= 0) {
            errors.Add($"{nameof(FrontComposerMcpOptions.MaxSuggestionCandidates)} must be positive.");
        }

        if (options.MaxToolNameLength <= 0 || options.MaxToolDisplayTextLength <= 0) {
            errors.Add("Tool response bounds must be positive.");
        }

        if (string.IsNullOrWhiteSpace(options.LifecycleToolName)) {
            errors.Add($"{nameof(FrontComposerMcpOptions.LifecycleToolName)} must be non-empty.");
        }

        if (string.IsNullOrWhiteSpace(options.LifecycleUriPrefix)
            || !options.LifecycleUriPrefix.EndsWith("/", StringComparison.Ordinal)
            || !Uri.TryCreate(options.LifecycleUriPrefix, UriKind.Absolute, out Uri? prefixUri)
            || !string.IsNullOrEmpty(prefixUri.Query)
            || !string.IsNullOrEmpty(prefixUri.Fragment)) {
            errors.Add($"{nameof(FrontComposerMcpOptions.LifecycleUriPrefix)} must be a non-empty absolute URI ending with '/' and free of query/fragment.");
        }

        if (options.DefaultLifecycleRetryAfterMs <= 0
            || options.MinLifecycleRetryAfterMs <= 0
            || options.MaxLifecycleRetryAfterMs < options.MinLifecycleRetryAfterMs
            || options.DefaultLifecycleRetryAfterMs < options.MinLifecycleRetryAfterMs
            || options.DefaultLifecycleRetryAfterMs > options.MaxLifecycleRetryAfterMs
            || options.MaxLifecycleLongPollMs < 0
            || options.MaxLifecycleTransitionHistory <= 0
            || options.MaxActiveLifecycleEntries <= 0
            || options.MaxRetainedTerminalLifecycleEntries <= 0
            || options.MaxLifecycleInProgressMs <= 0) {
            errors.Add("Lifecycle bounds must be positive and internally consistent (Default within [Min, Max]).");
        }

        foreach (KeyValuePair<string, FrontComposerMcpApiKeyIdentity> entry in options.ApiKeys) {
            if (string.IsNullOrWhiteSpace(entry.Key)) {
                errors.Add("API key entries must not have empty/whitespace keys.");
            }

            if (entry.Value is null
                || string.IsNullOrWhiteSpace(entry.Value.TenantId)
                || string.IsNullOrWhiteSpace(entry.Value.UserId)) {
                errors.Add("API key identities must declare non-empty TenantId and UserId.");
            }
        }

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}
