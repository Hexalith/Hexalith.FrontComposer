using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Mcp;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Mcp.Invocation;

public sealed class FrontComposerMcpCommandInvoker(
    IFrontComposerMcpAgentContextAccessor agentContextAccessor,
    FrontComposerMcpToolAdmissionService admissionService,
    IServiceProvider services,
    IOptions<FrontComposerMcpOptions> options) {
    private static readonly HashSet<string> SpoofedDerivableNames = new(StringComparer.OrdinalIgnoreCase) {
        "TenantId",
        "UserId",
        "MessageId",
        "CommandId",
        "CorrelationId",
    };

    public async Task<FrontComposerMcpResult> InvokeAsync(
        string toolName,
        IReadOnlyDictionary<string, JsonElement>? arguments,
        CancellationToken cancellationToken = default) {
        try {
            McpToolResolutionResult resolution = await admissionService.ResolveAsync(toolName, cancellationToken).ConfigureAwait(false);
            if (!resolution.Accepted || resolution.Tool is null) {
                return FrontComposerMcpResult.Failure(
                    FrontComposerMcpFailureCategory.UnknownTool,
                    FrontComposerMcpToolAdmissionService.BuildUnknownToolStructuredContent(resolution));
            }

            McpCommandDescriptor descriptor = resolution.Tool.Descriptor;
            FrontComposerMcpAgentContext context = agentContextAccessor.GetContext();
            ValidateArguments(descriptor, arguments);
            Type commandType = ResolveType(descriptor.CommandTypeName);
            object command = CreateInstanceOrThrow(commandType);
            ApplyArguments(command, descriptor, arguments);
            ApplyDerivableValues(command, context);

            ICommandService commandService = services.GetRequiredService<ICommandService>();
            FrontComposerMcpLifecycleTracker? lifecycleTracker = services.GetService<FrontComposerMcpLifecycleTracker>();
            ConcurrentQueue<(CommandLifecycleState State, string? MessageId)> lifecycleTransitions = new();
            Action<CommandLifecycleState, string?>? onLifecycleChange = lifecycleTracker is null
                ? null
                : (state, messageId) => lifecycleTransitions.Enqueue((state, messageId));

            CommandResult result = await DispatchAsync(commandService, command, commandType, onLifecycleChange, cancellationToken)
                .ConfigureAwait(false);
            if (lifecycleTracker is not null) {
                McpCommandAcknowledgement acknowledgement = lifecycleTracker.TrackAcknowledged(
                    descriptor,
                    result,
                    [.. lifecycleTransitions],
                    cancellationToken);
                return FrontComposerMcpResult.Success("Command acknowledged.", acknowledgement.ToJson());
            }

            JsonObject fallback = new() {
                ["status"] = result.Status,
            };
            if (!string.IsNullOrWhiteSpace(result.MessageId)) {
                fallback["messageId"] = result.MessageId;
            }

            if (!string.IsNullOrWhiteSpace(result.CorrelationId)) {
                fallback["correlationId"] = result.CorrelationId;
            }

            return FrontComposerMcpResult.Success("Command acknowledged.", fallback);
        }
        catch (CommandRejectedException) {
            return FrontComposerMcpResult.Failure(
                FrontComposerMcpFailureCategory.CommandRejected,
                BuildRejectionPayload(
                    errorCode: "COMMAND_REJECTED",
                    reasonCategory: "domain_conflict",
                    message: "Command failed: the command was rejected by domain rules. No changes were applied.",
                    suggestedAction: "abort",
                    retryAppropriate: false,
                    docsCode: "HFC-MCP-COMMAND-REJECTED"));
        }
        catch (CommandValidationException) {
            return FrontComposerMcpResult.Failure(
                FrontComposerMcpFailureCategory.ValidationFailed,
                BuildRejectionPayload(
                    errorCode: "COMMAND_VALIDATION_FAILED",
                    reasonCategory: "validation",
                    message: "Validation failed: the command arguments did not satisfy the contract. The command was not dispatched.",
                    suggestedAction: "correct-input",
                    retryAppropriate: true,
                    docsCode: "HFC-MCP-COMMAND-VALIDATION"));
        }
        catch (OperationCanceledException) {
            return FrontComposerMcpResult.Failure(
                cancellationToken.IsCancellationRequested
                    ? FrontComposerMcpFailureCategory.Canceled
                    : FrontComposerMcpFailureCategory.Timeout);
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

    private static Task<CommandResult> DispatchAsync(
        ICommandService commandService,
        object command,
        Type commandType,
        Action<CommandLifecycleState, string?>? onLifecycleChange,
        CancellationToken cancellationToken) {
        // CommandServiceExtensions.DispatchAsync<TCommand> is a generic static extension; binding it
        // by full parameter signature avoids the FirstOrDefault collision risk that a future 4-arg
        // overload would create. The cached delegate-style reflection is unavoidable here because
        // commandType is only known at runtime, but the signature filter pins us to the right method.
        MethodInfo method = LifecycleDispatchMethod.Value
            ?? throw new FrontComposerMcpException(FrontComposerMcpFailureCategory.UnsupportedSchema);
        try {
            return (Task<CommandResult>)method.MakeGenericMethod(commandType).Invoke(
                null,
                [commandService, command, onLifecycleChange, cancellationToken])!;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null) {
            throw ex.InnerException;
        }
    }

    private static readonly Lazy<MethodInfo?> LifecycleDispatchMethod = new(() =>
        typeof(CommandServiceExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(m => m.Name == nameof(CommandServiceExtensions.DispatchAsync)
                && m.IsGenericMethodDefinition
                && MatchesLifecycleSignature(m.GetParameters())));

    private static bool MatchesLifecycleSignature(ParameterInfo[] parameters) {
        if (parameters.Length != 4) {
            return false;
        }

        return parameters[0].ParameterType == typeof(ICommandService)
            && parameters[2].ParameterType == typeof(Action<CommandLifecycleState, string?>)
            && parameters[3].ParameterType == typeof(CancellationToken);
    }

    private static object CreateInstanceOrThrow(Type commandType) {
        ConstructorInfo? ctor = commandType.GetConstructor(Type.EmptyTypes);
        if (ctor is null) {
            throw new FrontComposerMcpException(FrontComposerMcpFailureCategory.UnsupportedSchema);
        }

        return ctor.Invoke(null);
    }

    private void ValidateArguments(McpCommandDescriptor descriptor, IReadOnlyDictionary<string, JsonElement>? arguments) {
        arguments ??= new Dictionary<string, JsonElement>();
        int size = JsonSerializer.SerializeToUtf8Bytes(arguments).Length;
        if (size > options.Value.MaxArgumentBytes) {
            throw new FrontComposerMcpException(FrontComposerMcpFailureCategory.ValidationFailed);
        }

        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> allowed = descriptor.Parameters
            .Where(p => !p.IsUnsupported)
            .Select(p => p.Name)
            .ToHashSet(StringComparer.Ordinal);
        foreach (KeyValuePair<string, JsonElement> pair in arguments) {
            if (!seen.Add(pair.Key)
                || SpoofedDerivableNames.Contains(pair.Key)
                || !allowed.Contains(pair.Key)
                || pair.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array) {
                throw new FrontComposerMcpException(FrontComposerMcpFailureCategory.ValidationFailed);
            }
        }

        foreach (McpParameterDescriptor parameter in descriptor.Parameters) {
            if (parameter.IsUnsupported) {
                continue;
            }

            if (parameter.IsRequired) {
                if (!arguments.TryGetValue(parameter.Name, out JsonElement value)
                    || value.ValueKind is JsonValueKind.Null) {
                    throw new FrontComposerMcpException(FrontComposerMcpFailureCategory.ValidationFailed);
                }
            }

            if (arguments.TryGetValue(parameter.Name, out JsonElement supplied)
                && supplied.ValueKind is not JsonValueKind.Null) {
                ValidatePrimitiveShape(parameter, supplied);
            }
        }
    }

    private static void ValidatePrimitiveShape(McpParameterDescriptor parameter, JsonElement value) {
        bool valid = parameter.JsonType switch {
            "string" => value.ValueKind == JsonValueKind.String,
            "number" => value.ValueKind == JsonValueKind.Number,
            "integer" => value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out _),
            "boolean" => value.ValueKind is JsonValueKind.True or JsonValueKind.False,
            _ => false,
        };

        if (!valid) {
            throw new FrontComposerMcpException(FrontComposerMcpFailureCategory.ValidationFailed);
        }

        if (parameter.EnumValues.Count > 0
            && (value.ValueKind != JsonValueKind.String
                || !parameter.EnumValues.Contains(value.GetString(), StringComparer.Ordinal))) {
            throw new FrontComposerMcpException(FrontComposerMcpFailureCategory.ValidationFailed);
        }
    }

    private Type ResolveType(string typeName) {
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

    private static void ApplyArguments(object command, McpCommandDescriptor descriptor, IReadOnlyDictionary<string, JsonElement>? arguments) {
        if (arguments is null) {
            return;
        }

        Type commandType = command.GetType();
        foreach (McpParameterDescriptor parameter in descriptor.Parameters) {
            if (parameter.IsUnsupported || !arguments.TryGetValue(parameter.Name, out JsonElement value)) {
                continue;
            }

            if (value.ValueKind is JsonValueKind.Null) {
                continue;
            }

            PropertyInfo? property = commandType.GetProperty(parameter.Name, BindingFlags.Public | BindingFlags.Instance);
            if (property is null || !property.CanWrite) {
                throw new FrontComposerMcpException(FrontComposerMcpFailureCategory.UnsupportedSchema);
            }

            object? typed;
            try {
                typed = value.Deserialize(property.PropertyType);
            }
            catch (JsonException) {
                throw new FrontComposerMcpException(FrontComposerMcpFailureCategory.ValidationFailed);
            }

            property.SetValue(command, typed);
        }
    }

    private void ApplyDerivableValues(object command, FrontComposerMcpAgentContext context) {
        Type commandType = command.GetType();
        SetIfWritable(command, commandType, "TenantId", context.TenantId);
        SetIfWritable(command, commandType, "UserId", context.UserId);

        // AC17 / D11: when an IUlidFactory is registered the message identity is allocated up front
        // via that factory so the lifecycle tracker can key off the same canonical ULID before
        // dispatch. When no factory is configured (legacy hosts without the MCP lifecycle tracker)
        // we fall back to a Guid to preserve the historical contract.
        IUlidFactory? ulidFactory = services.GetService<IUlidFactory>();
        string messageId = ulidFactory?.NewUlid() ?? Guid.NewGuid().ToString("N");
        SetIfWritable(command, commandType, "MessageId", messageId);
        SetIfWritable(command, commandType, "CommandId", messageId);
        SetIfWritable(command, commandType, "CorrelationId", Activity.Current?.TraceId.ToString() ?? messageId);
    }

    private static void SetIfWritable(object target, Type type, string propertyName, string value) {
        PropertyInfo? property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        if (property?.CanWrite == true && property.PropertyType == typeof(string)) {
            property.SetValue(target, value);
        }
    }

    private static JsonObject BuildRejectionPayload(
        string errorCode,
        string reasonCategory,
        string message,
        string suggestedAction,
        bool retryAppropriate,
        string docsCode)
        => new() {
            ["state"] = "Rejected",
            ["terminal"] = true,
            ["outcome"] = new JsonObject {
                ["category"] = "rejected",
                ["retryAppropriate"] = retryAppropriate,
                ["rejection"] = new JsonObject {
                    ["errorCode"] = errorCode,
                    ["message"] = message,
                    ["dataImpact"] = "No changes were applied.",
                    ["suggestedAction"] = suggestedAction,
                    ["retryAppropriate"] = retryAppropriate,
                    ["reasonCategory"] = reasonCategory,
                    ["docsCode"] = docsCode,
                },
            },
        };
}
