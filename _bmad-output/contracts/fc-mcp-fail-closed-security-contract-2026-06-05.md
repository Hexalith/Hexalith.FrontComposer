# FC-MCP-SECURITY v1 Fail-Closed Security Contract

Date: 2026-06-05
Owner: FrontComposer MCP surface
Status: v1 implementation contract

## Scope

FC-MCP-SECURITY v1 defines the fail-closed security contract for the FrontComposer MCP server. It
covers startup gate registration, explicit sample/dev permissive registrations, hidden-equivalent
public failures, `tools/list` empty-list behavior, projection-vs-skill resource visibility, and
sanitized logging.

The contract applies to `AddFrontComposerMcp(...)`, generated command tools, the fixed lifecycle
tool, tenant-scoped projection resources, and framework-global skill resources.

## Mandatory Startup Gates

`AddFrontComposerMcp(...)` must fail before the MCP server can be mapped or used unless the host has
already registered both mandatory gates:

- `IFrontComposerMcpTenantToolGate`
- `IFrontComposerMcpResourceVisibilityGate`

The extension must not register `AllowAllMcpTenantToolGate`,
`AllowAllResourceVisibilityGate`, or any other permissive default implicitly. A missing gate is a
host configuration error. The thrown startup message must name the missing interface and instruct
the host to register a real gate or explicitly register the allow-all implementation for sample/dev
hosts.

The gate probe is descriptor-based against the `IServiceCollection`, not a root-provider scoped
resolution. Scoped, singleton, and transient host registrations are therefore valid.

## Explicit Sample and Dev Hosts

Sample, test, and development hosts that intentionally want permissive MCP behavior must register
the allow-all gates explicitly before calling `AddFrontComposerMcp(...)`:

```csharp
services.AddSingleton<IFrontComposerMcpTenantToolGate, AllowAllMcpTenantToolGate>();
services.AddSingleton<IFrontComposerMcpResourceVisibilityGate, AllowAllResourceVisibilityGate>();
services.AddFrontComposerMcp(...);
```

This explicit ordering is part of the security contract. Production extension methods must not add
permissive fallbacks.

## Opaque Public Failure Shapes

Hidden-equivalent failures at the MCP protocol surface must collapse to the single opaque public
shape for that surface.

For generated command `tools/call`, tenant-hidden, policy-hidden, unknown-tool, auth-equivalent,
tenant-missing, and gate-failure paths must return the same public unknown-tool envelope produced by
`FrontComposerMcpToolAdmissionService.BuildHiddenUnknownStructuredContent()`.

For lifecycle `tools/call`, malformed handles, unknown handles, auth failures, tenant-missing
failures, tenant-hidden handles, policy-hidden handles, stale handles after visibility loss, and
unexpected context-accessor exceptions must return hidden/unknown structured content or the generic
protocol-edge text `Request failed.` as appropriate. These paths must not reveal whether a handle
ever existed.

For projection resources, `AuthFailed`, `TenantMissing`, `PolicyFiltered`, visibility loss,
`UnknownResource`, malformed attacker-supplied URI values, downstream exceptions, schema failures,
canceled reads, timeouts, and oversized responses must map to the sanitized MCP projection failure
taxonomy. Hidden-equivalent projection failures share the public `unknown_resource` token.

For skill resources, `UnknownResource` and auth-equivalent failures must keep the stable public
`unknown_resource` token. Malformed requests, canceled reads, and oversized resources keep their
stable public tokens.

Public responses must not include tenant IDs, user IDs, policy names, hidden tool names, hidden
resource names, lifecycle handles, raw URIs with query or fragment data, JWT-looking values,
API-key-looking values, descriptor internals, exception messages, stack traces, command arguments,
raw payloads, or context-sensitive descriptor descriptions.

## `tools/list` Behavior

`tools/list` is a read/catalog operation and differs from side-effecting `tools/call`.

If the MCP SDK list handler has no request services, cannot resolve a valid authenticated tenant
context, or catalog admission fails with `AuthFailed` or `TenantMissing`, it must return a successful
`ListToolsResult` with an empty `Tools` collection. It must not return a protocol error and must not
include exception text.

With a valid context, generated command tools are built only from the current visible catalog after
tenant and policy gates run. If every generated command is hidden, the list may contain only the
fixed lifecycle tool. Hidden command descriptors must not appear in tool names, descriptions,
suggestions, metadata, or logs returned to the client.

Tenant gate exceptions during catalog construction are treated as not visible for the affected
descriptor and must not become protocol errors.

## Sanitized Logging

Internal logging may record bounded diagnostic facts needed by operators, such as the generic
failure category, descriptor bounded context, or sanitized protocol surface. Logs must not record
tenant IDs, user IDs, policy names, raw command arguments, lifecycle handles, raw resource URIs with
query/fragment data, bearer tokens, API keys, exception messages, stack traces, or raw payloads for
hidden-equivalent paths.

Gate exceptions are logged with sanitized context and treated as non-visible admission results.

## Resource Security Split

Projection resources are tenant-scoped and remain protected by
`IFrontComposerMcpResourceVisibilityGate`. Projection reads must validate authenticated tenant
context, descriptor admission, visibility, schema compatibility, query execution, and pre-render
visibility revalidation before any Markdown is returned.

Skill resources are framework-global reference resources and intentionally bypass
`IFrontComposerMcpResourceVisibilityGate`. They do not query tenant data, do not carry tenant
descriptors, and are hidden only by host-wide transport/auth decisions outside this projection gate.
The reserved `frontcomposer://skills/` namespace must not be used by projection descriptors.

## Non-Goals

FC-MCP-SECURITY v1 does not introduce a new authorization framework, schema negotiation redesign,
command dispatch ordering change, resource URI grammar change, lifecycle wire-shape change, MCP
retry loop, streaming or long-poll server behavior, new skill-corpus authoring model, command
batching, queue policy, package upgrade, `Directory.Packages.props` change, or change to
`CanonicalSchemaMaterial`.
