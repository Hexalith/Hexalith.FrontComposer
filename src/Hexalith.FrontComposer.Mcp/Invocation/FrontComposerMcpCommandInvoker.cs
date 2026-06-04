using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Mcp;
using Hexalith.FrontComposer.Mcp.Schema;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Mcp.Invocation;

public sealed class FrontComposerMcpCommandInvoker(
    IFrontComposerMcpAgentContextAccessor agentContextAccessor,
    FrontComposerMcpToolAdmissionService admissionService,
    IServiceProvider services,
    IOptions<FrontComposerMcpOptions> options,
    ILogger<FrontComposerMcpCommandInvoker> logger) {
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
        // D8: capture messageId/correlationId on the outer scope so the catch blocks can emit
        // them in the rejection envelope. ApplyDerivableValues populates them before validation
        // (DN17), so domain-rejection and validation-rejection envelopes both carry the handle;
        // admission/schema failures that fire upstream of allocation legitimately have none.
        string? capturedMessageId = null;
        string? capturedCorrelationId = null;
        try {
            McpToolResolutionResult resolution = await admissionService.ResolveAsync(toolName, cancellationToken).ConfigureAwait(false);
            if (!resolution.Accepted || resolution.Tool is null) {
                if (resolution.Category is FrontComposerMcpFailureCategory.SchemaMismatch
                    or FrontComposerMcpFailureCategory.UnknownSchemaBaseline
                    or FrontComposerMcpFailureCategory.UnsupportedSchemaAlgorithm
                    or FrontComposerMcpFailureCategory.UnsupportedSchema
                    or FrontComposerMcpFailureCategory.SchemaIntegrityMismatch) {
                    return SchemaNegotiationRuntimeGate.ToStructuredFailure(resolution.Category);
                }

                return FrontComposerMcpResult.Failure(
                    FrontComposerMcpFailureCategory.UnknownTool,
                    FrontComposerMcpToolAdmissionService.BuildUnknownToolStructuredContent(resolution));
            }

            McpCommandDescriptor descriptor = resolution.Tool.Descriptor;
            FrontComposerMcpAgentContext context = agentContextAccessor.GetContext();
            McpSchemaNegotiationResult? schema = resolution.SchemaNegotiation
                ?? SchemaNegotiationRuntimeGate.EvaluateCommand(descriptor, agentContextAccessor, services);
            if (schema is not null && !schema.AllowsSideEffects) {
                return SchemaNegotiationRuntimeGate.ToStructuredFailure(schema.FailureCategory);
            }

            ValidateArguments(descriptor, arguments);
            Type commandType = ResolveType(descriptor.CommandTypeName);
            object command = CreateInstanceOrThrow(commandType);
            ApplyArguments(command, descriptor, arguments);
            // 11-5 review DN17: derivable values (TenantId, UserId, MessageId, CorrelationId)
            // are populated before the current-server-contract validation so commands that carry
            // [Required] on those properties do not fail validation merely because the framework
            // had not yet injected its server-controlled values. ValidateArguments above already
            // refused caller-supplied derivable names via SpoofedDerivableNames, so this ordering
            // does not let an agent bypass tenant validation.
            (capturedMessageId, capturedCorrelationId) = ApplyDerivableValues(command, context);
            ValidateCurrentCommandContract(command);

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
                    category: "rejected",
                    errorCode: "COMMAND_REJECTED",
                    reasonCategory: "domain_conflict",
                    message: "Command failed: the command was rejected by domain rules. No changes were applied.",
                    suggestedAction: "abort",
                    retryAppropriate: false,
                    docsCode: "HFC-MCP-COMMAND-REJECTED",
                    messageId: capturedMessageId,
                    correlationId: capturedCorrelationId));
        }
        catch (CommandValidationException) {
            // P49: validation failures use a distinct outer envelope category so agents can
            // branch on outcome.category between protocol-layer validation and domain rejection.
            return FrontComposerMcpResult.Failure(
                FrontComposerMcpFailureCategory.ValidationFailed,
                BuildRejectionPayload(
                    category: "validation",
                    errorCode: "COMMAND_VALIDATION_FAILED",
                    reasonCategory: "validation",
                    message: "Validation failed: the command arguments did not satisfy the contract. The command was not dispatched.",
                    suggestedAction: "correct-input",
                    retryAppropriate: true,
                    docsCode: "HFC-MCP-COMMAND-VALIDATION",
                    messageId: capturedMessageId,
                    correlationId: capturedCorrelationId));
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
            if (ex.Category is FrontComposerMcpFailureCategory.SchemaMismatch
                or FrontComposerMcpFailureCategory.UnknownSchemaBaseline
                or FrontComposerMcpFailureCategory.UnsupportedSchemaAlgorithm
                or FrontComposerMcpFailureCategory.UnsupportedSchema
                or FrontComposerMcpFailureCategory.SchemaIntegrityMismatch) {
                // 8-6a re-review: convert enum to string explicitly so structured-log sinks
                // produce a deterministic value across enricher configurations (D4 bounded fields).
                logger.LogInformation(
                    "MCP command invocation failed with schema category {Category}.",
                    ex.Category.ToString());
                return SchemaNegotiationRuntimeGate.ToStructuredFailure(ex.Category);
            }

            logger.LogWarning(
                "MCP command invocation failed with category {Category}.",
                ex.Category.ToString());
            return FrontComposerMcpResult.Failure(ex.Category);
        }
        catch (Exception ex) when (ex is not OperationCanceledException) {
            // 8-6a re-review: capture the exception object via the structured-log overload so
            // the type/stack survive in diagnostic sinks; previous logging emitted only the
            // `DownstreamFailed` token, losing the underlying signal entirely. The `when` guard
            // is defensive — the explicit OperationCanceledException handler above takes
            // precedence under normal CLR exception dispatch, but exception filters with side
            // effects could otherwise drop cancellation here.
            logger.LogWarning(
                ex,
                "MCP command invocation failed with category {Category}.",
                FrontComposerMcpFailureCategory.DownstreamFailed.ToString());
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

        // P43: pin parameters[1] as the open generic command parameter so a future 4-arg
        // overload whose second slot carries something else (logger, context, options) cannot
        // silently match this signature filter.
        return parameters[0].ParameterType == typeof(ICommandService)
            && parameters[1].ParameterType.IsGenericMethodParameter
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

    private static void ValidateCurrentCommandContract(object command) {
        List<ValidationResult> validationResults = [];
        ValidationContext validationContext = new(command);
        bool valid;
        try {
            valid = Validator.TryValidateObject(command, validationContext, validationResults, validateAllProperties: true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException) {
            // 11-5 review P2: an IValidatableObject.Validate implementation that throws would
            // otherwise bypass the post-admission contract gate and surface as DownstreamFailed
            // with the underlying exception captured by the outer catch (potentially leaking
            // type/stack into structured logs). Translate to bounded ValidationFailed instead;
            // the exception object itself is not logged here — the outer envelope is sufficient.
            throw new CommandValidationException(new ProblemDetailsPayload(
                Title: "Command validation failed.",
                Detail: "The command arguments did not satisfy the current server contract.",
                Status: null,
                EntityLabel: null,
                ValidationErrors: new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal),
                GlobalErrors: ["The command arguments did not satisfy the current server contract."]));
        }

        if (valid) {
            return;
        }

        Dictionary<string, List<string>> errors = new(StringComparer.Ordinal);
        List<string> globalErrors = [];
        foreach (ValidationResult validationResult in validationResults) {
            string[] memberNames = validationResult.MemberNames
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            string message = string.IsNullOrWhiteSpace(validationResult.ErrorMessage)
                ? "The command argument did not satisfy the current server contract."
                : validationResult.ErrorMessage!;
            if (memberNames.Length == 0) {
                globalErrors.Add(message);
                continue;
            }

            // 11-5 review P1: append messages per member instead of overwriting. A command
            // carrying both [Required] and [Range] on the same property would otherwise surface
            // only the last evaluated message, which is non-deterministic across BCL versions.
            foreach (string memberName in memberNames) {
                if (!errors.TryGetValue(memberName, out List<string>? messages)) {
                    messages = [];
                    errors[memberName] = messages;
                }

                messages.Add(message);
            }
        }

        throw new CommandValidationException(new ProblemDetailsPayload(
            Title: "Command validation failed.",
            Detail: "The command arguments did not satisfy the current server contract.",
            Status: null,
            EntityLabel: null,
            ValidationErrors: errors.ToDictionary(
                pair => pair.Key,
                pair => (IReadOnlyList<string>)pair.Value,
                StringComparer.Ordinal),
            GlobalErrors: globalErrors.Count == 0 && errors.Count == 0
                ? ["The command arguments did not satisfy the current server contract."]
                : globalErrors));
    }

    private (string MessageId, string CorrelationId) ApplyDerivableValues(object command, FrontComposerMcpAgentContext context) {
        Type commandType = command.GetType();
        SetIfWritable(command, commandType, "TenantId", context.TenantId);
        SetIfWritable(command, commandType, "UserId", context.UserId);

        IUlidFactory ulidFactory = services.GetService<IUlidFactory>()
            ?? throw new FrontComposerMcpException(FrontComposerMcpFailureCategory.UnsupportedSchema);
        string messageId = NewCanonicalUlid(ulidFactory);
        string correlationId = NewCanonicalUlid(ulidFactory);
        SetIfWritable(command, commandType, "MessageId", messageId);
        SetIfWritable(command, commandType, "CommandId", messageId);
        SetIfWritable(command, commandType, "CorrelationId", correlationId);
        return (messageId, correlationId);
    }

    private static string NewCanonicalUlid(IUlidFactory ulidFactory) {
        string value;
        try {
            value = ulidFactory.NewUlid();
        }
        catch (OperationCanceledException) {
            throw;
        }
        catch (Exception ex) when (ex is not FrontComposerMcpException) {
            throw new FrontComposerMcpException(FrontComposerMcpFailureCategory.UnsupportedSchema);
        }

        if (!IsCanonicalUlid(value)) {
            throw new FrontComposerMcpException(FrontComposerMcpFailureCategory.UnsupportedSchema);
        }

        return value;
    }

    private static bool IsCanonicalUlid(string? value) {
        if (value is null || value.Length != 26) {
            return false;
        }

        for (int i = 0; i < value.Length; i++) {
            char ch = value[i];
            bool valid = (ch >= '0' && ch <= '9')
                || (ch >= 'A' && ch <= 'H')
                || (ch >= 'J' && ch <= 'K')
                || (ch >= 'M' && ch <= 'N')
                || (ch >= 'P' && ch <= 'T')
                || (ch >= 'V' && ch <= 'Z');
            if (!valid) {
                return false;
            }
        }

        return true;
    }

    private static void SetIfWritable(object target, Type type, string propertyName, string value) {
        PropertyInfo? property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        if (property?.CanWrite == true && property.PropertyType == typeof(string)) {
            property.SetValue(target, value);
        }
    }

    private static JsonObject BuildRejectionPayload(
        string category,
        string errorCode,
        string reasonCategory,
        string message,
        string suggestedAction,
        bool retryAppropriate,
        string docsCode,
        string? messageId,
        string? correlationId) {
        JsonObject envelope = new() {
            ["state"] = "Rejected",
            ["terminal"] = true,
            ["outcome"] = new JsonObject {
                // P49: outer envelope category disambiguates protocol-layer validation from
                // domain-layer rejection at the field agents typically branch on.
                ["category"] = category,
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

        // D8: surface framework-issued correlation/message IDs on the rejection envelope so the
        // agent can pair the synchronous rejection with the dispatched handle for telemetry and
        // audit. IDs are framework-controlled, non-enumerable, and safe per the AC2/AC6 contract.
        if (!string.IsNullOrWhiteSpace(messageId)) {
            envelope["messageId"] = messageId;
        }

        if (!string.IsNullOrWhiteSpace(correlationId)) {
            envelope["correlationId"] = correlationId;
        }

        return envelope;
    }
}
