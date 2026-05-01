# Story 7.3: Command Authorization Policies

Status: ready-for-dev

> **Epic 7** - Authentication, Authorization & Multi-Tenancy. Covers **FR46**, consumes Story **7-1** authentication seams and Story **7-2** tenant-context seams, and enforces **NFR23** plus the existing command lifecycle and feedback contracts. Applies lessons **L01**, **L03**, **L06**, **L07**, **L08**, and **L10**.

---

## Executive Summary

Story 7-3 adds declarative command authorization without inventing a parallel security model:

- Add a dependency-free `[RequiresPolicy("PolicyName")]` command attribute in Contracts.
- Parse the attribute in SourceTools and carry policy metadata through command IR, generated registration, command forms/renderers, and command discovery surfaces.
- Evaluate command authorization through ASP.NET Core `IAuthorizationService` using the current authenticated `ClaimsPrincipal`.
- Enforce policy checks before EventStore dispatch, before command lifecycle submission moves into misleading progress states, and before inline/full-page command UI enables execution.
- Reuse Story 7-1 authentication and Story 7-2 canonical tenant context; do not perform claim extraction, tenant validation, role mapping, or provider-specific auth work here.
- Emit a build-time warning when a policy-protected command references a policy absent from the host's declared FrontComposer authorization policy catalog.
- Render unauthorized command attempts as a localized, accessible Fluent warning message and prevent backend dispatch.

---

## Story

As a developer,
I want to apply authorization policies to commands via declarative attributes that integrate with ASP.NET Core,
so that I can enforce role-based and policy-based access control on domain operations using the standard .NET authorization model.

### Adopter Job To Preserve

An adopter should be able to annotate a domain command once, register normal ASP.NET Core authorization policies in the host, and trust generated command buttons, forms, empty-state CTAs, palette entries, and dispatch paths to use the same policy decision without each UI surface reimplementing authorization.

---

## Acceptance Criteria

| AC | Given | When | Then |
| --- | --- | --- | --- |
| AC1 | A command type is annotated `[RequiresPolicy("OrderApprover")]` | SourceTools parses the command | The command IR records exactly one non-empty policy name and generated artifacts can access it without reflection over provider-specific auth types. |
| AC2 | A policy-protected command is submitted by a user without the required policy | The generated form, inline button, full-page command, palette result, or empty-state CTA attempts execution | The framework blocks before `ICommandService.DispatchAsync`, EventStore serialization, token acquisition, HTTP send, lifecycle SubmittedAction, pending-command registration, or SignalR side effects. |
| AC3 | Authorization blocks a command | The user-visible result is rendered | The UI shows localized warning copy equivalent to "You don't have permission to {command action}" via Fluent UI warning affordance, uses accessible status/error patterns, and includes no raw claims, roles, tokens, tenant IDs, user IDs, or policy internals. |
| AC4 | Authorization allows a command | The command is submitted | Existing command lifecycle, validation, destructive confirmation, derived-value prefill, tenant propagation, token relay, and EventStore dispatch behavior remain unchanged. |
| AC5 | A command has no `[RequiresPolicy]` attribute | The authenticated user submits it | No policy check is performed beyond the existing authentication and tenant-context checks owned by Stories 7-1 and 7-2. |
| AC6 | A command has `[RequiresPolicy]` with null, empty, whitespace, duplicate, or malformed policy value | SourceTools parses the command | A build-time diagnostic is emitted with teaching copy and docs link; invalid policy metadata is not silently emitted. |
| AC7 | A command references a policy not declared in the host's FrontComposer authorization policy catalog | SourceTools or startup validation runs | A build-time or startup warning names the policy and command type without raw claim data and links to policy registration guidance. |
| AC8 | ASP.NET Core authorization services are registered | The framework evaluates a protected command | It uses `IAuthorizationService.AuthorizeAsync(user, resource, policyName)` or the closest resource-aware ASP.NET Core policy API, with the current `ClaimsPrincipal` from the host auth state. |
| AC9 | The user is unauthenticated | A policy-protected command is rendered or submitted | The command is not executable; auth redirect/challenge behavior remains owned by Story 7-1 and no anonymous backend dispatch occurs. |
| AC10 | Inline DataGrid action buttons render for policy-protected commands | Authorization state is known | Buttons are hidden or disabled according to the story's chosen policy, and the dispatch path still performs the authoritative server-side/framework check. UI gating alone never counts as authorization. |
| AC11 | Authorization state is loading, unavailable during prerender, or changes during Blazor Server-to-Auto transition | Generated command UI renders | The UI defaults to non-executable/pending rather than briefly enabling protected commands, and re-checks before any submit. |
| AC12 | Empty-state CTA metadata resolves a protected command | `FcProjectionEmptyPlaceholder` renders the CTA | The existing `AuthorizationPolicy` field is populated from the command policy metadata and wrapped through the same policy semantics as command renderers. |
| AC13 | Command palette or home capability discovery lists commands | Protected commands are unauthorized for the current user | The surface must not present the command as executable; if discoverability remains product-visible, it must clearly show non-executable state and cannot route to a form that dispatches. |
| AC14 | Authorization evaluation throws, policy services are missing, or the policy catalog is inconsistent | A command is rendered or submitted | The framework fails closed, emits sanitized diagnostics, and does not dispatch the command. |
| AC15 | Multi-tenant authorization is needed | A policy handler evaluates the command | The handler can consume Story 7-2 validated tenant context as resource/context data; Story 7-3 does not create a separate tenant accessor or normalize tenant/user identifiers. |
| AC16 | The story completes | A developer prepares Epic 8 MCP tools | Command policy metadata is available to future MCP tool enumeration/execution so web and agent surfaces can share the same authorization contract later, but MCP implementation remains out of scope. |

---

## Tasks / Subtasks

- [ ] T1. Add the declarative policy contract (AC1, AC5, AC6, AC15, AC16)
  - [ ] Add `RequiresPolicyAttribute` under `src/Hexalith.FrontComposer.Contracts/Attributes/` with `AttributeTargets.Class`, `AllowMultiple = false`, and a required non-whitespace policy name.
  - [ ] Keep Contracts dependency-free: no `Microsoft.AspNetCore.Authorization`, no `ClaimsPrincipal`, no handler/provider references.
  - [ ] Decide whether invalid constructor input throws immediately, is reported by SourceTools, or both; ensure SourceTools still emits deterministic diagnostics for source-level invalid values.
  - [ ] Add XML docs that say the attribute declares a command policy only; policy registration and claims mapping are host/ASP.NET Core concerns.

- [ ] T2. Parse and carry policy metadata through SourceTools IR (AC1, AC6, AC7, AC12, AC16)
  - [ ] Extend `CommandParser` to parse `[RequiresPolicy]` and store `AuthorizationPolicyName` on `CommandModel`.
  - [ ] Extend `CommandRendererModel`, command form/renderer transforms, registration transforms, and generated manifest output with the policy name.
  - [ ] Add HFC10xx diagnostic(s) for empty/whitespace policy names and unsupported duplicate declarations. Prefer Warning for missing host policy catalog references unless the host has opted into strict mode.
  - [ ] Add SourceTools transform tests and generated snapshot tests for protected and unprotected commands.

- [ ] T3. Define the host policy catalog seam (AC7, AC14, AC16)
  - [ ] Add a small Shell/Contracts-safe catalog such as `IFrontComposerAuthorizationPolicyCatalog` or options-backed `FrontComposerAuthorizationOptions.KnownPolicies`.
  - [ ] Make the catalog optional for simple hosts, but when present, validate generated policy references against it and surface teaching warnings.
  - [ ] Do not attempt to introspect every `AuthorizationOptions` policy at compile time; build-time diagnostics need a declared catalog or generated host metadata.
  - [ ] Add startup validation tests for missing catalog, matching policy, missing policy, strict mode, and sanitized warning payloads.

- [ ] T4. Implement the runtime authorization evaluator (AC2, AC4, AC5, AC8, AC9, AC11, AC14, AC15)
  - [ ] Add a scoped Shell service, e.g. `ICommandAuthorizationEvaluator`, that accepts command type/policy name, current command instance or metadata resource, and cancellation token.
  - [ ] Use ASP.NET Core `IAuthorizationService` and the current authenticated `ClaimsPrincipal` from Story 7-1 host-auth seams.
  - [ ] Pass a resource object containing at minimum command type, policy name, bounded context, display label, and the validated Story 7-2 tenant context when available.
  - [ ] Fail closed on missing authorization service, missing principal, unauthenticated principal, thrown handler, missing policy, cancellation, stale tenant context, or auth-state transition.
  - [ ] Log/telemetry only sanitized categories, command type, policy name presence/name if approved as non-secret config, diagnostic ID, and correlation ID. Never log raw claims, roles, token values, tenant IDs, or user IDs.

- [ ] T5. Gate generated command forms/renderers before dispatch (AC2-AC5, AC8-AC11, AC14)
  - [ ] Inject the evaluator into generated form or renderer code in a way that preserves current `BeforeSubmit`, destructive confirmation, derived-value prefill, validation, and lifecycle ordering.
  - [ ] Ensure protected commands check authorization immediately before submit and before `SubmittedAction` is dispatched.
  - [ ] For destructive protected commands, choose and document ordering: policy check should happen before opening the destructive confirmation dialog so unauthorized users do not see sensitive destructive flow copy.
  - [ ] Add explicit zero-side-effect tests for unauthorized submit: no validation mutation beyond the warning, no lifecycle submitted state, no pending-command registration, no command service dispatch, no EventStore HTTP send.

- [ ] T6. Gate command presentation surfaces consistently (AC10-AC13)
  - [ ] Update generated command renderers so inline, compact inline, and full-page surfaces do not briefly enable protected commands while authorization state is pending.
  - [ ] Update `IEmptyStateCtaResolver` / `EmptyStateCtaResolver` to populate the existing `EmptyStateCta.AuthorizationPolicy` from `[RequiresPolicy]` metadata rather than leaving it null.
  - [ ] Update command palette and home/capability discovery behavior so unauthorized commands are not shown as executable.
  - [ ] Add bUnit tests for authorized, unauthorized, unauthenticated, pending, and auth-state-changed cases.

- [ ] T7. User-facing warning and localization (AC3, AC9, AC11, AC14)
  - [ ] Add EN/FR resource keys for unauthorized command warning title/message and any aria-label/status copy.
  - [ ] Render warning via the existing generated-form `FluentMessageBar` pattern or a small shared Shell component; do not introduce a new notification framework.
  - [ ] Keep copy domain-language aware using the command display label, but do not include raw policy name unless product approves it as safe/helpful.
  - [ ] Verify color is not the only signal, focus remains stable, keyboard users can recover, and screen readers receive the warning.

- [ ] T8. Tests and verification (AC1-AC16)
  - [ ] Contracts tests for `RequiresPolicyAttribute` constructor, XML-doc/public API expectations, and dependency boundary.
  - [ ] SourceTools parser/transform/emitter tests for policy metadata, invalid values, duplicate attribute protection, generated form/render output, and manifest/registration output.
  - [ ] Shell evaluator tests using fake `IAuthorizationService`, fake principals, allow/deny/throw/missing-policy outcomes, and tenant-context resource assertions.
  - [ ] Generated component tests proving unauthorized commands are hidden/disabled and cannot dispatch across inline, compact inline, full-page, empty-state CTA, palette, and home surfaces.
  - [ ] Redaction tests with sentinel claim names/values, role names, tenant IDs, user IDs, JWT-like strings, policy names, and command payload fragments.
  - [ ] Regression: `dotnet build Hexalith.FrontComposer.sln -warnaserror /p:UseSharedCompilation=false`.
  - [ ] Targeted tests: `tests/Hexalith.FrontComposer.Contracts.Tests`, `tests/Hexalith.FrontComposer.SourceTools.Tests`, and `tests/Hexalith.FrontComposer.Shell.Tests`.

---

## Dev Notes

### Existing State To Preserve

| File | Current state | Preserve / Change |
| --- | --- | --- |
| `src/Hexalith.FrontComposer.Contracts/Attributes/CommandAttribute.cs` | Marker for command generation, dependency-free. | Add `[RequiresPolicy]` beside it without changing `[Command]` semantics. |
| `src/Hexalith.FrontComposer.Contracts/Rendering/IUserContextAccessor.cs` | Flat tenant/user accessor; null/empty/whitespace means unauthenticated. | Do not extend it for roles or claims. Authorization uses ASP.NET Core auth services. |
| `src/Hexalith.FrontComposer.SourceTools/Parsing/CommandParser.cs` | Parses `[Command]`, `[BoundedContext]`, `[Display]`, `[Icon]`, `[DefaultValue]`, `[Destructive]`; marks `TenantId`/`UserId` derivable. | Add policy parsing without weakening existing command diagnostics or density/destructive behavior. |
| `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs` | Generated form owns EditForm, validation, lifecycle dispatch, server warning bars, auth redirect seam, and command service dispatch. | Insert authorization check before dispatch/lifecycle side effects while preserving validation and cancellation discipline. |
| `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandRendererEmitter.cs` | Generated renderer owns inline/compact/full-page chrome, derived-value prefill, destructive dialog, and return-path safety. | Gate protected command affordances and keep destructive confirmation behind successful authorization. |
| `src/Hexalith.FrontComposer.Shell/Services/IEmptyStateCtaResolver.cs` | `EmptyStateCta.AuthorizationPolicy` already exists; whitespace is rejected. | Populate this field from `[RequiresPolicy]` metadata and keep the existing `<AuthorizeView Policy=...>` path. |
| `src/Hexalith.FrontComposer.Shell/Components/Rendering/FcProjectionEmptyPlaceholder.razor` | Wraps CTA in `AuthorizeView`, with optional `Policy`. | Reuse this existing policy wrapper; do not add a second CTA authorization mechanism. |
| `src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs` | Quickstart registers `AddAuthorizationCore()` so `AuthorizeView` can render. | Story 7-3 may need full authorization services where policy checks execute; do not assume `AddAuthorizationCore` alone supplies every server-side policy dependency. |
| `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreCommandClient.cs` | EventStore dispatch validates tenant, applies token, serializes payload, sends HTTP, and classifies response. | Authorization should block before this service is called for generated commands; direct service callers may need an optional decorator if this story chooses to secure all `ICommandService` usage. |

### Architecture Contracts

- ASP.NET Core authorization is the policy engine. FrontComposer only carries command metadata and asks `IAuthorizationService` for a decision.
- UI gating is advisory UX. The authoritative framework check must happen immediately before dispatch and must fail closed.
- Policy metadata belongs to the command contract and generated artifacts. Claim extraction, provider setup, token relay, and authentication challenge remain Story 7-1.
- Tenant context, tenant mismatch handling, cache/group isolation, and tenant snapshot revalidation remain Story 7-2. Story 7-3 may pass validated context as an authorization resource but must not create parallel tenant logic.
- Policy names are configuration identifiers, not user data. They may appear in developer diagnostics when useful, but raw claims/roles/tokens/tenant/user identifiers must never appear in logs or UI.
- Contracts and SourceTools must stay provider-agnostic. ASP.NET Core authorization references belong in Shell/runtime tests unless a dependency already exists and is explicitly accepted.

### Authorization Flow Sequence

1. SourceTools parses `[RequiresPolicy("OrderApprover")]` into command metadata.
2. Generated command surfaces receive the policy name and request current authorization state through the Shell evaluator.
3. While auth state is unknown, command affordances remain non-executable.
4. On submit, the evaluator calls ASP.NET Core authorization with the current principal and command resource.
5. Denied or failed authorization records a sanitized warning and stops before lifecycle and dispatch side effects.
6. Allowed authorization proceeds into the existing command form flow, including validation, derived values, lifecycle, tenant propagation, token relay, and EventStore dispatch.
7. Future MCP command tools consume the same policy metadata and evaluator contract in Epic 8.

### Cross-Story Contract Table

| Producer | Consumer | Contract |
| --- | --- | --- |
| Story 7-1 auth bridge | Story 7-3 evaluator | Supplies authenticated principal and host auth services; 7-3 does not parse provider tokens or claims directly. |
| Story 7-2 tenant context | Story 7-3 policy handlers | Supplies validated tenant context as resource data when policies need tenant-aware authorization. |
| Story 2-2/2-5 command generation | Story 7-3 generated gates | Form/renderer lifecycle ordering and destructive confirmation behavior stay intact; policy check is inserted before dispatch side effects. |
| Story 4-6 empty-state CTA | Story 7-3 CTA policy metadata | Existing `EmptyStateCta.AuthorizationPolicy` becomes populated from command policy metadata. |
| Story 3-4 command palette | Story 7-3 command discovery | Protected commands cannot be surfaced as executable when unauthorized. |
| Epic 8 MCP | Story 7-3 policy metadata | Future MCP command tools must reuse command policy metadata and evaluator semantics; implementation deferred. |

### Binding Decisions

| Decision | Binding choice | Rationale | Rejected alternatives |
| --- | --- | --- | --- |
| D1 | `[RequiresPolicy]` is a Contracts attribute with a single required policy-name string. | Keeps the Layer 1 declarative surface simple and source-generator friendly. | Role-specific attributes; embedding ASP.NET Core `AuthorizeAttribute`; multiple policies per command in v1. |
| D2 | `IAuthorizationService` is the runtime decision authority. | Matches ASP.NET Core policy middleware and lets adopters reuse handlers/requirements. | Custom role parser; direct claim checks in generated code; backend-only enforcement. |
| D3 | Generated UI gates commands, but submit-time evaluator check is authoritative. | Prevents flicker and bad UX while keeping authorization correct under race/auth-state changes. | UI-only hide/disable; EventStore-only 403 handling. |
| D4 | Missing/invalid auth services fail closed for protected commands. | Security-sensitive systems must not become permissive because host registration is incomplete. | Best-effort allow; skip policy when service missing. |
| D5 | Destructive command authorization runs before destructive confirmation. | Unauthorized users should not see destructive flow details or trigger dialog lifecycle work. | Show dialog then deny; rely on backend 403. |
| D6 | Policy catalog validation is explicit and optional unless strict mode is enabled. | Compile-time discovery of host ASP.NET Core policy registrations is not generally available from domain assemblies. | Reflect `AuthorizationOptions` in SourceTools; require every adopter to maintain a catalog before first use. |
| D7 | Unauthorized copy is localized and command-label based, not policy-name based. | Business users need the action they cannot perform, not implementation policy IDs. | Display raw policy names; generic "Forbidden" only. |
| D8 | Story 7-3 does not implement role mapping, tenant membership, account provisioning, or backend authorization. | Keeps the story bounded to FrontComposer declarative policies and ASP.NET Core integration. | Expand into full IAM model; require EventStore policy enforcement changes. |

### Library / Framework Requirements

- Use the repository's current .NET 10 / Blazor / ASP.NET Core authorization stack.
- Prefer `IAuthorizationService.AuthorizeAsync` with a resource object so adopter handlers can inspect command metadata and tenant context.
- `AuthorizeView` is acceptable for CTA/render-state gating, but do not rely on it as the only authorization enforcement.
- Continue to use Fluent UI Blazor warning/error components already present in generated forms.
- Do not add provider-specific OIDC/SAML/GitHub/Google package references in this story.
- Do not add new external authorization libraries.

External references checked on 2026-05-01:

- Microsoft Learn: Policy-based authorization in ASP.NET Core: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/policies
- Microsoft Learn: Resource-based authorization in ASP.NET Core: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/resourcebased
- Microsoft Learn: ASP.NET Core Blazor authentication and authorization: https://learn.microsoft.com/en-us/aspnet/core/blazor/security/

### File Structure Requirements

Expected new or changed files:

| Path | Purpose |
| --- | --- |
| `src/Hexalith.FrontComposer.Contracts/Attributes/RequiresPolicyAttribute.cs` | Declarative command authorization policy attribute. |
| `src/Hexalith.FrontComposer.SourceTools/Parsing/CommandParser.cs` | Parse policy metadata and emit diagnostics. |
| `src/Hexalith.FrontComposer.SourceTools/Parsing/CommandModel.cs` or equivalent command IR file | Carry `AuthorizationPolicyName`. |
| `src/Hexalith.FrontComposer.SourceTools/Transforms/*Command*` | Preserve policy metadata through command form/renderer/registration models. |
| `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs` | Insert submit-time authorization check and warning rendering. |
| `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandRendererEmitter.cs` | Gate inline/compact/full-page command affordances. |
| `src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs` | Add HFC diagnostic descriptors for invalid/missing policy metadata. |
| `src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs` | Reserve any runtime HFC21xx/HFC22xx IDs used by Shell diagnostics. |
| `src/Hexalith.FrontComposer.Shell/Services/Auth/*CommandAuthorization*` | Runtime evaluator and resource model. |
| `src/Hexalith.FrontComposer.Shell/Resources/FcShellResources*.resx` | Unauthorized command warning resources. |
| `tests/Hexalith.FrontComposer.Contracts.Tests/Attributes/*` | Attribute contract tests. |
| `tests/Hexalith.FrontComposer.SourceTools.Tests/*Command*` | Parser/transform/emitter/snapshot tests. |
| `tests/Hexalith.FrontComposer.Shell.Tests/Services/Auth/*` | Evaluator, redaction, and resource tests. |
| `tests/Hexalith.FrontComposer.Shell.Tests/Generated/*Authorization*` | Generated component gating and zero-side-effect tests. |

### Testing Standards

- P0 tests must prove both visibility gating and dispatch prevention.
- Every denial/failure branch must assert zero side effects at the earliest boundary.
- Use fake `IAuthorizationService`, fake `AuthenticationStateProvider`/principal seams, and existing bUnit authorization helpers. Do not require live identity providers.
- Include auth-state transition tests: unauthenticated to authenticated, authorized to denied, prerender/pending auth state, and sign-out before submit.
- Pair generated snapshots with runtime/bUnit tests; snapshots alone cannot prove dispatch prevention.
- Redaction tests must include sentinel values and exact absence assertions.

### Scope Guardrails

Do not implement these in Story 7-3:

- OIDC/SAML/GitHub/Google provider setup, token validation, token storage, redirect/challenge flows, or claim extraction. Story 7-1 owns these.
- Tenant propagation, tenant mismatch enforcement, cache key scoping, SignalR group scoping, or tenant snapshot implementation. Story 7-2 owns these.
- Backend EventStore authorization rules or server-side policy enforcement beyond client/framework dispatch gating.
- Role mapping UI, account provisioning, tenant membership management, consent screens, audit log product features, or admin policy editors.
- MCP command execution, tenant-scoped tool enumeration, or agent authorization. Epic 8 owns these.
- Multiple policies per command, policy expression language, OR/AND composition, dynamic policy builders, or per-row resource policy inference beyond the command metadata resource.

### Non-Goals With Owning Stories

| Non-goal | Owner |
| --- | --- |
| Authentication provider setup and token relay. | Story 7-1 |
| Canonical tenant context and isolation enforcement. | Story 7-2 |
| MCP command/tool authorization execution. | Story 8-1 / Story 8-2 |
| Full policy cookbook and deployment recipes. | Story 9-5 |
| Diagnostic ID governance cleanup beyond story-owned IDs. | Story 9-4 |
| Browser/E2E login and policy matrix. | Story 10-2 or dedicated auth E2E follow-up |

### Known Gaps / Follow-Ups

| Gap | Owner |
| --- | --- |
| Backend-side EventStore proof that protected command endpoints reject unauthorized callers independently. | EventStore auth contract backlog or consumer-driven contract story |
| Multi-policy composition on one command. | v1.x authorization follow-up |
| Admin UI for policy assignment or tenant membership. | Product backlog outside FrontComposer v1 |
| MCP policy enforcement at tool enumeration and execution. | Epic 8 |
| Live IdP policy integration E2E. | Story 10-2 / manual integration lane |

---

## References

- [Source: `_bmad-output/planning-artifacts/epics/epic-7-authentication-authorization-multi-tenancy.md#Story-7.3`] - story statement, AC foundation, FR46/NFR23 scope.
- [Source: `_bmad-output/planning-artifacts/prd/functional-requirements.md#FR46`] - declarative command authorization policy capability.
- [Source: `_bmad-output/planning-artifacts/prd/developer-tool-specific-requirements.md#Layer-1`] - `[RequiresPolicy(policyName)]` as Layer 1 attribute.
- [Source: `_bmad-output/planning-artifacts/prd/non-functional-requirements.md#Authentication-&-authorization`] - ASP.NET Core policy integration and missing-policy warning.
- [Source: `_bmad-output/planning-artifacts/architecture.md#Authentication/Authorization`] - auth/authz cross-cutting concern and `[RequiresPolicy]` testing expectation.
- [Source: `_bmad-output/implementation-artifacts/7-1-oidc-saml-authentication-integration.md`] - authenticated principal, token relay, and provider boundary.
- [Source: `_bmad-output/implementation-artifacts/7-2-tenant-context-propagation-and-isolation.md`] - validated tenant context and isolation boundary.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L03`] - tenant/user isolation fail-closed.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L08`] - party review before advanced elicitation.
- [Source: `src/Hexalith.FrontComposer.Contracts/Attributes/CommandAttribute.cs`] - command attribute pattern.
- [Source: `src/Hexalith.FrontComposer.SourceTools/Parsing/CommandParser.cs`] - command IR parsing and diagnostics.
- [Source: `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs`] - generated form dispatch and warning-bar pattern.
- [Source: `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandRendererEmitter.cs`] - command render modes and destructive confirmation order.
- [Source: `src/Hexalith.FrontComposer.Shell/Services/IEmptyStateCtaResolver.cs`] - existing CTA authorization policy field.
- [Source: `src/Hexalith.FrontComposer.Shell/Components/Rendering/FcProjectionEmptyPlaceholder.razor`] - existing `AuthorizeView Policy` CTA wrapper.
- [Source: Microsoft Learn: Policy-based authorization in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/policies) - policy and handler model.
- [Source: Microsoft Learn: Resource-based authorization in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/resourcebased) - `IAuthorizationService` resource checks.
- [Source: Microsoft Learn: ASP.NET Core Blazor authentication and authorization](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/) - Blazor auth state and authorization UI considerations.

---

## Dev Agent Record

### Agent Model Used

(to be filled in by dev agent)

### Debug Log References

(to be filled in by dev agent)

### Completion Notes List

- 2026-05-01: Story created via `/bmad-create-story 7-3-command-authorization-policies` during recurring pre-dev hardening job. Ready for party-mode review on a later run.

### File List

(to be filled in by dev agent)
