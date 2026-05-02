using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Mcp;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Mcp.Invocation;

public sealed class FrontComposerMcpCommandInvoker(
    FrontComposerMcpDescriptorRegistry registry,
    IFrontComposerMcpAgentContextAccessor agentContextAccessor,
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
        if (string.IsNullOrWhiteSpace(toolName) || !registry.TryGetCommand(toolName, out McpCommandDescriptor? descriptor)) {
            return FrontComposerMcpResult.Failure(FrontComposerMcpFailureCategory.UnknownTool);
        }

        try {
            FrontComposerMcpAgentContext context = agentContextAccessor.GetContext();

            // Story 7-3 policy fail-closed gate: if descriptor carries a policy name, a host-supplied
            // gate must approve before dispatch. Hosts without a gate registered fail-closed.
            if (!string.IsNullOrWhiteSpace(descriptor.AuthorizationPolicyName)) {
                IFrontComposerMcpCommandPolicyGate? gate = services.GetService<IFrontComposerMcpCommandPolicyGate>();
                if (gate is null) {
                    return FrontComposerMcpResult.Failure(FrontComposerMcpFailureCategory.PolicyGateMissing);
                }

                bool approved = await gate.EvaluateAsync(descriptor.AuthorizationPolicyName!, context, cancellationToken).ConfigureAwait(false);
                if (!approved) {
                    return FrontComposerMcpResult.Failure(FrontComposerMcpFailureCategory.AuthFailed);
                }
            }

            ValidateArguments(descriptor, arguments);
            Type commandType = ResolveType(descriptor.CommandTypeName);
            object command = CreateInstanceOrThrow(commandType);
            ApplyArguments(command, descriptor, arguments);
            ApplyDerivableValues(command, context);

            ICommandService commandService = services.GetRequiredService<ICommandService>();
            MethodInfo? method = typeof(ICommandService).GetMethod(nameof(ICommandService.DispatchAsync));
            if (method is null) {
                throw new FrontComposerMcpException(FrontComposerMcpFailureCategory.UnsupportedSchema);
            }

            object? resultTask;
            try {
                resultTask = method.MakeGenericMethod(commandType).Invoke(commandService, [command, cancellationToken]);
            }
            catch (TargetInvocationException ex) when (ex.InnerException is not null) {
                throw ex.InnerException;
            }

            CommandResult result = await ((Task<CommandResult>)resultTask!).ConfigureAwait(false);
            JsonObject structured = new() {
                ["status"] = result.Status,
            };
            if (!string.IsNullOrWhiteSpace(result.MessageId)) {
                structured["messageId"] = result.MessageId;
            }

            if (!string.IsNullOrWhiteSpace(result.CorrelationId)) {
                structured["correlationId"] = result.CorrelationId;
            }

            return FrontComposerMcpResult.Success("Command acknowledged.", structured);
        }
        catch (CommandRejectedException) {
            return FrontComposerMcpResult.Failure(FrontComposerMcpFailureCategory.CommandRejected);
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

    private static object CreateInstanceOrThrow(Type commandType) {
        // Records and types with positional / parameterized primary constructors do not have a public
        // parameterless ctor — surface that deterministically as UnsupportedSchema rather than letting
        // Activator's MissingMethodException collapse into the generic catch.
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

        // Allowed names use Ordinal to match descriptor casing exactly; spoofed-derivable names use
        // OrdinalIgnoreCase so case-variant TenantId / tenantid / TENANTID all fail-closed.
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
        }
    }

    private Type ResolveType(string typeName) {
        // Resolution preference: assembly-qualified Type.GetType, then explicitly registered
        // ManifestAssemblies, then a load-context scan as a last-resort fallback for hosts that
        // registered manifests through Options.Manifests without listing their assemblies. The
        // bounded preference reduces type-confusion risk from arbitrary plugin assemblies that
        // happen to share an FQN; the fallback preserves the minimal-config developer flow.
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

            object? typed = value.Deserialize(property.PropertyType);
            property.SetValue(command, typed);
        }
    }

    private static void ApplyDerivableValues(object command, FrontComposerMcpAgentContext context) {
        Type commandType = command.GetType();
        SetIfWritable(command, commandType, "TenantId", context.TenantId);
        SetIfWritable(command, commandType, "UserId", context.UserId);

        // MessageId and CommandId share a single value so EventStore idempotency stays consistent
        // with a single MCP invocation. CorrelationId honors the ambient OpenTelemetry trace when
        // present so agent → EventStore traces can be joined.
        string messageId = Guid.NewGuid().ToString("N");
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
}
