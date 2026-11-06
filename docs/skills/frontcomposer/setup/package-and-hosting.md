---
id: setup-package-and-hosting
title: Package setup and MCP hosting
version: 2.0.0
audience: agent
docfx: true
mcpResource: true
resourceUri: frontcomposer://skills/setup/package-and-hosting
order: 20
sourceDoc: docs/skills/frontcomposer/setup/package-and-hosting.md
narrative: true
references: true
publicApiReferences: [Hexalith.FrontComposer.Mcp.Extensions.FrontComposerMcpServiceCollectionExtensions, Hexalith.FrontComposer.Mcp.Extensions.FrontComposerMcpEndpointRouteBuilderExtensions, Hexalith.FrontComposer.Mcp.Extensions.FrontComposerMcpAuthenticationBuilderExtensions, Hexalith.FrontComposer.Mcp.FrontComposerMcpAuthenticationDefaults]
---
<!-- frontcomposer:section narrative -->
# Package Setup

Human documentation can expand this into hosting walkthroughs.
<!-- /frontcomposer:section -->
<!-- frontcomposer:section agent-reference -->
# Package Setup and Hosting

Choose packages by responsibility:

- `Hexalith.FrontComposer.Contracts` for UI-clean attributes, communication contracts, registration,
  schema/MCP descriptors, diagnostics, and UI-neutral seams.
- `Hexalith.FrontComposer.Contracts.UI` for typography, Blazor render-fragment customization
  contexts, and keyboard-event shortcut contracts. Existing public namespaces are retained.
- `Hexalith.FrontComposer.Shell` for the runtime Blazor shell, options, services, and Fluxor actions.
- `Hexalith.FrontComposer.Testing` for the bUnit host, deterministic services, evidence helpers, and
  `InMemoryStorageService`.
- `Hexalith.FrontComposer.SourceTools` as the analyzer package; it remains netstandard2.0 and depends
  only on Contracts even when its generated UI requires Contracts.UI and Shell in the consumer.
- `Hexalith.FrontComposer.Mcp` for MCP hosting. Do not add MCP SDK references to Contracts,
  Contracts.UI, or SourceTools.

Register host-supplied `IFrontComposerMcpTenantToolGate` and
`IFrontComposerMcpResourceVisibilityGate` implementations before `AddFrontComposerMcp`; startup
fails closed when either is absent.

Register ASP.NET Core authentication and authorization services, add `UseAuthentication()` and
`UseAuthorization()`, then map endpoints with `MapFrontComposerMcp`. Mapping throws an
`InvalidOperationException` that names the missing `AddAuthentication` or `AddAuthorization`
call. That check verifies the service registrations only, not that a default challenge scheme
exists: without one, and without schemes on the policy, MCP requests fail with `500`.

Every mapped MCP endpoint requires an authenticated caller through one policy object, built at
map time: the host's default authorization policy combined with `RequireAuthenticatedUser()`. A
policy that the host appends with `RequireAuthorization(...)` adds requirements and cannot remove
that one. An anonymous caller gets the authenticating scheme's challenge: `401` for the API-key
and bearer schemes, a redirect for cookie and OpenID Connect schemes. `AllowAnonymous()` on the
returned builder is the endpoint-level opt-out. Host code can also bypass the requirement, for
example with an `IAuthorizationHandler` that succeeds `DenyAnonymousAuthorizationRequirement`, a
custom `IPolicyEvaluator` or `IAuthorizationMiddlewareResultHandler`, or an endpoint convention
that removes the authorization metadata. Each of these is a host-owned bypass that the framework
neither detects nor supports; do not use one for agent traffic.

To authenticate the keys in `FrontComposerMcpOptions.ApiKeys`, add the FrontComposer scheme with
`AddFrontComposerMcpApiKey()`. It reads the header named by
`FrontComposerMcpOptions.ApiKeyHeaderName` (`X-FrontComposer-Mcp-Key` by default). A matched key
authenticates a principal with `TenantId` and `UserId` claims, and the identity's authentication
type is the scheme name. A request without the header is left to other schemes. A multi-valued,
blank, or unknown key fails with a fixed message and the caller gets `401`; responses and logs
never contain the key. Do not write a second API-key handler.

When MCP is the host's only authentication, make the scheme the default:
`AddAuthentication(FrontComposerMcpAuthenticationDefaults.ApiKeyScheme).AddFrontComposerMcpApiKey()`.
When the host has other authentication, scope the API-key scheme to the MCP endpoints instead of
making it the app-wide default: append
`new AuthorizationPolicyBuilder(FrontComposerMcpAuthenticationDefaults.ApiKeyScheme).RequireAuthenticatedUser().Build()`
to the builder that `MapFrontComposerMcp` returns. The alternative is to list both schemes in the
default policy's authentication schemes. Either way, a request whose other credentials carry a
tenant or user that conflicts with the API key's identity is refused.

Outside Development, mapping rejects the shipped `AllowAllMcpTenantToolGate` and
`AllowAllResourceVisibilityGate` sample gates. Development is detected with `IsDevelopment()`,
which ignores the case of the environment name. These limits remain host responsibilities:

- Mapping resolves the gates once, from a temporary service scope that it disposes before
  returning. A gate whose constructor needs an active HTTP request cannot be resolved there.
- Only the exact allow-all types are rejected. A decorator that wraps one passes the check, so
  keep custom gates restrictive.
- The SDK's own `MapMcp`, the stdio transport, and `McpServer.Create` skip every FrontComposer
  host check. Use `MapFrontComposerMcp` for HTTP hosting.

Skill resources are packaged with `.Mcp`; do not create a separate skills package.
<!-- /frontcomposer:section -->
