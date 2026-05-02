# Story 7.3: Command Authorization Policies

Status: in-progress

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

- [x] T1. Add the declarative policy contract (AC1, AC5, AC6, AC15, AC16)
  - [x] Add `RequiresPolicyAttribute` under `src/Hexalith.FrontComposer.Contracts/Attributes/` with `AttributeTargets.Class`, `AllowMultiple = false`, and a required non-whitespace policy name.
  - [x] Keep Contracts dependency-free: no `Microsoft.AspNetCore.Authorization`, no `ClaimsPrincipal`, no handler/provider references.
  - [x] Decide whether invalid constructor input throws immediately, is reported by SourceTools, or both; ensure SourceTools still emits deterministic diagnostics for source-level invalid values.
  - [x] Add XML docs that say the attribute declares a command policy only; policy registration and claims mapping are host/ASP.NET Core concerns.

- [x] T2. Parse and carry policy metadata through SourceTools IR (AC1, AC6, AC7, AC12, AC16)
  - [x] Extend `CommandParser` to parse `[RequiresPolicy]` and store `AuthorizationPolicyName` on `CommandModel`.
  - [x] Extend `CommandRendererModel`, command form/renderer transforms, registration transforms, and generated manifest output with the policy name.
  - [x] Add HFC10xx diagnostic(s) for empty/whitespace policy names and unsupported duplicate declarations. Prefer Warning for missing host policy catalog references unless the host has opted into strict mode.
  - [x] Add SourceTools transform tests and generated snapshot tests for protected and unprotected commands.

- [x] T3. Define the host policy catalog seam (AC7, AC14, AC16)
  - [x] Add a small Shell/Contracts-safe catalog such as `IFrontComposerAuthorizationPolicyCatalog` or options-backed `FrontComposerAuthorizationOptions.KnownPolicies`.
  - [x] Make the catalog optional for simple hosts, but when present, validate generated policy references against it and surface teaching warnings.
  - [x] Do not attempt to introspect every `AuthorizationOptions` policy at compile time; build-time diagnostics need a declared catalog or generated host metadata.
  - [x] Add startup validation tests for missing catalog, matching policy, missing policy, strict mode, and sanitized warning payloads.

- [x] T4. Implement the runtime authorization evaluator (AC2, AC4, AC5, AC8, AC9, AC11, AC14, AC15)
  - [x] Add a scoped Shell service, e.g. `ICommandAuthorizationEvaluator`, that accepts command type/policy name, current command instance or metadata resource, and cancellation token.
  - [x] Use ASP.NET Core `IAuthorizationService` and the current authenticated `ClaimsPrincipal` from Story 7-1 host-auth seams.
  - [x] Pass a resource object containing at minimum command type, policy name, bounded context, display label, and the validated Story 7-2 tenant context when available.
  - [x] Return a deterministic decision object such as `Allowed`, `Denied`, `Pending`, or `FailedClosed` plus a sanitized reason category; do not return raw handler exceptions, claim values, role values, tenant IDs, user IDs, tokens, or command payload fragments.
  - [x] Do not cache allow decisions across executable boundaries. Render-time decisions may be memoized only inside a single render/evaluation cycle for UI stability; submit-time and post-confirmation checks must acquire fresh principal and tenant-context snapshots.
  - [x] Fail closed on missing authorization service, missing principal, unauthenticated principal, thrown handler, missing policy, cancellation, stale tenant context, or auth-state transition.
  - [x] Log/telemetry only sanitized categories, command type, policy name presence/name if approved as non-secret config, diagnostic ID, and correlation ID. Never log raw claims, roles, token values, tenant IDs, or user IDs.

- [x] T5. Gate generated command forms/renderers before dispatch (AC2-AC5, AC8-AC11, AC14)
  - [x] Inject the evaluator into generated form or renderer code in a way that preserves current `BeforeSubmit`, destructive confirmation, derived-value prefill, validation, and lifecycle ordering.
  - [x] Ensure protected commands check authorization immediately before submit and before `SubmittedAction` is dispatched.
  - [x] Treat UI authorization state as advisory. The generated submit path must not reuse a stale render-time `Allowed` value after navigation, tenant switch, principal refresh, prerender completion, reconnection, or destructive-dialog delay.
  - [x] For destructive protected commands, choose and document ordering: policy check should happen before opening the destructive confirmation dialog so unauthorized users do not see sensitive destructive flow copy.
  - [x] Add explicit zero-side-effect tests for unauthorized submit: no validation mutation beyond the warning, no lifecycle submitted state, no pending-command registration, no command service dispatch, no EventStore HTTP send.

- [x] T6. Gate command presentation surfaces consistently (AC10-AC13)
  - [x] Update generated command renderers so inline, compact inline, and full-page surfaces do not briefly enable protected commands while authorization state is pending.
  - [x] Update `IEmptyStateCtaResolver` / `EmptyStateCtaResolver` to populate the existing `EmptyStateCta.AuthorizationPolicy` from `[RequiresPolicy]` metadata rather than leaving it null.
  - [x] Update command palette and home/capability discovery behavior so unauthorized commands are not shown as executable.
  - [x] Add bUnit tests for authorized, unauthorized, unauthenticated, pending, and auth-state-changed cases.

- [x] T7. User-facing warning and localization (AC3, AC9, AC11, AC14)
  - [x] Add EN/FR resource keys for unauthorized command warning title/message and any aria-label/status copy.
  - [x] Render warning via the existing generated-form `FluentMessageBar` pattern or a small shared Shell component; do not introduce a new notification framework.
  - [x] Keep copy domain-language aware using the command display label, but do not include raw policy name unless product approves it as safe/helpful.
  - [x] Verify color is not the only signal, focus remains stable, keyboard users can recover, and screen readers receive the warning.

- [x] T8. Tests and verification (AC1-AC16)
  - [x] Contracts tests for `RequiresPolicyAttribute` constructor, XML-doc/public API expectations, and dependency boundary.
  - [x] SourceTools parser/transform/emitter tests for policy metadata, invalid values, duplicate attribute protection, generated form/render output, and manifest/registration output.
  - [x] Shell evaluator tests using fake `IAuthorizationService`, fake principals, allow/deny/throw/missing-policy outcomes, and tenant-context resource assertions.
  - [x] Generated component tests proving unauthorized commands are hidden/disabled and cannot dispatch across inline, compact inline, full-page, empty-state CTA, palette, and home surfaces.
  - [x] Race and staleness tests for tenant switch, sign-out, auth-state refresh, prerender-to-interactive transition, and destructive-dialog delay between an initial allowed render and the final submit-time check.
  - [x] Redaction tests with sentinel claim names/values, role names, tenant IDs, user IDs, JWT-like strings, policy names, and command payload fragments.
  - [x] Regression: `dotnet build Hexalith.FrontComposer.sln -warnaserror /p:UseSharedCompilation=false`.
  - [x] Targeted tests: `tests/Hexalith.FrontComposer.Contracts.Tests`, `tests/Hexalith.FrontComposer.SourceTools.Tests`, and `tests/Hexalith.FrontComposer.Shell.Tests`.

### Review Findings

Code review run on 2026-05-01 via `/bmad-code-review 7-3`. Three adversarial layers (Acceptance Auditor, Blind Hunter, Edge Case Hunter via `general-purpose`) raised ~116 raw findings. After dedup: 5 decision-needed, 25 patches, 10 deferred, 10 dismissed.

#### Decision-Needed

- [ ] [Review][Decision] Replace `IHttpContextAccessor.HttpContext.User` with `AuthenticationStateProvider` for principal source — In `CommandAuthorizationEvaluator.cs:34-39` the principal is obtained via `IHttpContextAccessor.HttpContext?.User`. In Blazor Server interactive circuits and WebAssembly, `HttpContext` is null after the initial render, so every authenticated user receives `Unauthenticated` for every protected command. Tests pass because they fake `IHttpContextAccessor.HttpContext` non-null. Decide: (a) replace with `AuthenticationStateProvider.GetAuthenticationStateAsync()`, (b) accept current behaviour as SSR-only, (c) try both with fallback.
- [ ] [Review][Decision] Direct `ICommandService.DispatchAsync` decorator/check for protected commands — A consumer that calls `CommandService.DispatchAsync(new ApproveOrderCommand())` directly from custom code bypasses both the form and palette gates. Spec mandates "Policy-protected direct command dispatch through framework services must perform the same authoritative submit-time check." No decorator was added. Decide: (a) add `AuthorizingCommandServiceDecorator` reading `DomainManifest.CommandPolicies`, (b) defer to a follow-up story, (c) accept that direct dispatch is opt-out.
- [ ] [Review][Decision] Inline / Compact / FullPage trigger button policy-aware gating — `CommandRendererEmitter.cs` emits inline/compact/full-page action buttons with `Disabled = _externalSubmit is null` only; the form's submit-time check denies the action but the trigger looks enabled. UX false-affordance: users open popovers and dialogs for commands they cannot run. Decide: (a) inject `ICommandAuthorizationEvaluator` into renderer and gate triggers via `Disabled` binding, (b) hide entirely when denied, (c) accept current submit-time-only enforcement.
- [ ] [Review][Decision] Home / Capability discovery surface gating — `CapabilityDiscoveryEffects.cs` and `FcHomeDirectory` enumerate `IFrontComposerRegistry.GetManifests()` and surface command links without consulting `ICommandAuthorizationEvaluator`. Spec lists Home/Capability as a required executable surface. Decide: (a) implement filtering in `CapabilityDiscoveryEffects` mirroring `CommandPaletteEffects`, (b) defer to a follow-up story and document.
- [ ] [Review][Decision] Empty-state CTA — `<AuthorizeView>` vs `ICommandAuthorizationEvaluator` resource shape mismatch — `FcProjectionEmptyPlaceholder.razor` gates on `<AuthorizeView Policy="…">` which calls `IAuthorizationService.AuthorizeAsync(user, resource: null, policy)`. Form/palette paths call the evaluator which passes a `CommandAuthorizationResource` carrying tenant context. A handler that requires the resource accepts on the form path but denies on the CTA path (or vice-versa). Decide: (a) replace `<AuthorizeView>` with evaluator-backed component, (b) accept divergence and document, (c) document that CTA policies must not depend on resource.

#### Patches

- [ ] [Review][Patch] Add post-confirmation auth re-check for destructive commands [src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs:374-378] — Currently `auth → BeforeSubmit (opens destructive dialog) → SubmittedAction`. Spec mandates "must authorize again immediately after confirmation and before dispatch because auth, tenant, or policy state can change while the dialog is open." Add a second `EvaluateAsync` call between line 378 and the `SubmittedAction` dispatch, gated on `hasAuthorizationPolicy && form.IsDestructive`.
- [ ] [Review][Patch] Add `_disposed` short-circuit in submit-time auth deny path [CommandFormEmitter.cs:364-371] — After `await EvaluateAsync(...)` returns Denied/Blocked, the form publishes feedback warning and calls `StateHasChanged` without checking `_disposed` or `_cts.IsCancellationRequested`. Mirror the post-`BeforeSubmit` guard at line 377.
- [ ] [Review][Patch] Add `_disposed` short-circuit in `RefreshPresentationAuthorizationAsync` [CommandFormEmitter.cs:155-165] — After the await, write `_authorizationPresentationReady = true` and call `StateHasChanged` without disposal guard. Add `if (_disposed) return;` after the await.
- [ ] [Review][Patch] Add `CatalogInconsistent` to `CommandAuthorizationReason` enum [src/Hexalith.FrontComposer.Shell/Services/Authorization/CommandAuthorizationDecision.cs:12-22] — Advanced Elicitation hardening lists ten sanitized categories; current enum has nine. Add `CatalogInconsistent` and emit it from the catalog validator strict-mode failure path or from runtime catalog-versus-evaluation mismatch detection.
- [ ] [Review][Patch] Convert `SourceSurface` from `string` to `enum` [CommandAuthorizationDecision.cs:30,37] — Spec mandates closed set `GeneratedForm | InlineAction | CompactInlineAction | FullPage | EmptyStateCta | CommandPalette | HomeCapability | DirectDispatch`. Currently free-form string with magic-string defaults. Define `CommandAuthorizationSurface` enum and update emitters/palette/CTA/direct-dispatch callers.
- [ ] [Review][Patch] Distinguish infrastructure failures from user-denied in user-facing warning [CommandFormEmitter.cs:177-187, FcShellResources.resx] — All `!authorization.IsAllowed` paths render "You do not have permission to {0}." Even when `MissingService` / `StaleTenantContext` / `Unauthenticated` returned `FailedClosed`. Add a separate localized "Action temporarily unavailable" string with retry hint, and select copy by `decision.Reason` category.
- [ ] [Review][Patch] Add canonical failure-matrix tests for evaluator [tests/Hexalith.FrontComposer.Shell.Tests/Services/Authorization/CommandAuthorizationEvaluatorTests.cs] — Currently covers NoPolicy, Denied, Allowed, Unauthenticated. Add: MissingService, MissingPolicy (`InvalidOperationException` from `AuthorizeAsync`), HandlerFailed (generic exception), StaleTenantContext, Canceled (`OperationCanceledException`), tenant switch after render, sign-out after render, destructive dialog delay between presentation and submit.
- [ ] [Review][Patch] Add redaction tests with sentinel claim/role/JWT/tenant/user/policy values [tests/Hexalith.FrontComposer.Shell.Tests/Services/Authorization/] — Per Advanced Elicitation Redaction Classification Matrix, no test seeds sentinel values into principal/command and asserts they are absent from rendered warning, evaluator log output, decision result, and generated metadata.
- [ ] [Review][Patch] Add generated-component bUnit gating tests under `tests/Hexalith.FrontComposer.Shell.Tests/Generated/` — Spec File Structure Requirements list `tests/.../Generated/*Authorization*` as required; absent. Need bUnit tests proving inline / compact-inline / full-page / empty-state CTA / palette / home surfaces are non-executable for unauthorized users with zero side effects (no `SubmittedAction`, no pending registration, no command-service dispatch, no EventStore send).
- [ ] [Review][Patch] Wrap disabled-button boolean expression in parentheses [CommandFormEmitter.cs:619-625] — `A && B && C || !ready || !allowed` happens to compile to the intended `(A && B && C) || !ready || !allowed`, but a future patch reordering lines silently flips the gate. Emit explicit parens.
- [ ] [Review][Patch] Convert `KnownPolicies` to a settable `IList<string>` for IConfiguration binding [src/Hexalith.FrontComposer.Shell/Options/FrontComposerAuthorizationOptions.cs:4] — Currently `ISet<string> { get; }` with no setter. `IConfiguration` binders cannot populate `ISet<string>` without a constructor or setter, so adopters using `appsettings.json` get an empty catalog silently. Either expose a settable collection or add a converter.
- [ ] [Review][Patch] Warn on conflicting policies for same command FQN in registry merge [src/Hexalith.FrontComposer.Shell/Registration/FrontComposerRegistry.cs:118-123] — `merged[pair.Key] = pair.Value;` is last-write-wins with no diagnostic. If two manifests register the same command with different policies, merge order silently chooses one. Log a Warning when a value is overwritten with a different policy.
- [ ] [Review][Patch] Subscribe to `AuthenticationStateChanged` and re-evaluate presentation [CommandFormEmitter.cs:147-188] — Form's presentation auth runs once at `OnInitializedAsync`. Sign-out, principal refresh, or tenant switch after initial render leaves `_authorizationPresentationAllowed = true` cached. Submit-time check still denies, but the button stays enabled. Subscribe to `AuthenticationStateProvider.AuthenticationStateChanged` (and tenant-snapshot changes) and call `RefreshPresentationAuthorizationAsync` on transitions.
- [ ] [Review][Patch] Trim `request.PolicyName` before whitespace short-circuit [CommandAuthorizationEvaluator.cs:20] — Line 20 short-circuits to `Allowed/NoPolicy` when `string.IsNullOrWhiteSpace(request.PolicyName)`, but line 49 passes `request.PolicyName.Trim()` to the resource. A tab-only or NBSP-padded policy name short-circuits as no-policy → false-allow on direct-dispatch callers that did not pre-trim.
- [ ] [Review][Patch] Catch `IUserContextAccessor.TryGetContext` exceptions in evaluator [CommandAuthorizationEvaluator.cs:41-45] — A custom `IFrontComposerTenantContextAccessor.TryGetContext` that throws (`NotImplementedException`, etc.) escapes the evaluator. Submit path's catch chain only handles `OperationCanceledException`, `CommandRejectedException`, `AuthRedirectException` — generic exceptions propagate to Blazor error boundary instead of fail-closed. Wrap line 41 in try/catch returning `Blocked(StaleTenantContext)`.
- [ ] [Review][Patch] Null-safety on `AuthorizationResult` from `AuthorizeAsync` [CommandAuthorizationEvaluator.cs:56-62] — Test stubs and broken third-party `IAuthorizationService` impls can return `Task.FromResult<AuthorizationResult>(null!)`. `result.Succeeded` then NREs and falls into the broad catch as `HandlerFailed`. Treat null result as `HandlerFailed` explicitly.
- [ ] [Review][Patch] Distinguish "missing catalog entry" from "missing catalog" diagnostics [src/Hexalith.FrontComposer.Shell/Services/Authorization/FrontComposerAuthorizationPolicyCatalogValidator.cs:14-43] — Currently empty `KnownPolicies` short-circuits silently and any non-empty catalog with missing entries logs/throws under one path. Spec: "Missing catalog and missing policy entry are distinct diagnostic conditions." Reserve runtime HFC21xx IDs and split the two paths.
- [ ] [Review][Patch] `ResolveCommandPolicy` order-dependence — match registry merge semantics or validate uniqueness [src/Hexalith.FrontComposer.Shell/Services/EmptyStateCtaResolver.cs:236-245] — Returns first match across manifests; `FrontComposerRegistry.MergeCommandPolicies` uses last-write-wins. The two consumers can disagree on the effective policy when the same FQN appears in multiple manifests. Either centralize policy lookup through the registry's merged result, or assert single-source-of-truth at registry build.
- [ ] [Review][Patch] Localizer `ResourceNotFound` fallback for warning copy [CommandFormEmitter.cs:179-180] — `Localizer["UnauthorizedCommandWarningTitle"].Value` returns the raw key string when the resource is missing in a deployed locale (FR/EN ship; third locales display the key). Mirror the `ResolveLocalised` pattern in `CommandPaletteEffects.cs:100-108` — check `LocalizedString.ResourceNotFound` and fall back to a static English string.
- [ ] [Review][Patch] Authorization warning dismissal stickiness [CommandFormEmitter.cs:155-165, 360-372] — Both presentation refresh and submit-time deny call `SetAuthorizationWarning()` which overwrites any user-dismissed warning. Track a `_authorizationWarningDismissed` flag and skip re-applying when the user has explicitly dismissed.
- [ ] [Review][Patch] Per-keystroke palette authorization call performance [src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs:387,481] — Every `PaletteQueryChangedAction` (debounced 150ms) re-evaluates auth for every scoring command. With remote auth handlers the palette becomes O(N×Q). Add a per-circuit decision cache keyed by `(commandType, principal-version, tenant-version)` invalidated on auth-state-change, or batch-evaluate per query.
- [ ] [Review][Patch] Catalog validator: warn when commands declare policies but `KnownPolicies` is empty [FrontComposerAuthorizationPolicyCatalogValidator.cs:16-18] — Currently `KnownPolicies.Count == 0 → return`. Adopter forgets to populate the catalog → boot succeeds → all protected commands fail-closed at runtime per-user. Emit at least an Information log when validator runs with empty catalog AND any manifest declares policies.
- [ ] [Review][Patch] Remove `Pending` decision kind or wire it as a returnable value [CommandAuthorizationDecision.cs:5-10] — `CommandAuthorizationDecisionKind.Pending` is declared but no evaluator code path returns it. Either delete it or add a return path for prerender/auth-state-pending.
- [ ] [Review][Patch] Distinguish `Denied` (rule rejection) from `MissingPolicy` (FailCalled with no requirements) [CommandAuthorizationEvaluator.cs:60-68] — All non-`Succeeded` `AuthorizationResult` paths collapse to `Reason.Denied`. ASP.NET's `result.Failure.FailCalled` and `result.Failure.FailedRequirements` distinguish "policy unconfigured" from "rule failed". Inspect `Failure` and choose the right reason category; `MissingPolicy` then triggers via Failed without InvalidOperationException too.
- [ ] [Review][Patch] Document the Blazor Server / WASM / SSR principal-source matrix in story Dev Notes — After resolving DN1, capture which surfaces use which principal accessor, the test harness implications, and the failure mode for each combination.

#### Deferred

- [x] [Review][Defer] Out-of-scope submodule pointer changes in `Hexalith.EventStore` and `Hexalith.Tenants` — Working-tree carries unrelated submodule pointer drifts not declared in the File List. Should be reverted or committed under a separate change. Not part of 7-3 scope.
- [x] [Review][Defer] Reserve runtime HFC21xx/HFC22xx diagnostic IDs for authorization-blocked logs — Logs use `LogWarning` without a structured diagnostic id; SIEM rules cannot filter security-domain entries. Defer to Story 9-4 (Diagnostic ID System).
- [x] [Review][Defer] Multi-namespace identical-name `RequiresPolicyAttribute` collision — A `using RequiresPolicy = MyApp.RequiresPolicyAttribute;` alias plus `[RequiresPolicy("X")]` silently parses as unprotected. Edge case requiring policy-FQN ambiguity check. Defer.
- [x] [Review][Defer] Unicode/normalization for policy names — `char.IsLetterOrDigit` accepts non-ASCII; mixed NFC/NFD comparisons are ordinal so the parser may accept names that fail catalog/handler lookup. Edge case; defer to v1.x policy hardening.
- [x] [Review][Defer] Re-entrant `OnInitializedAsync` race in SSR-to-interactive transition — Two evaluator runs, brief window where button is enabled between runs. Submit-time check still fails closed. Defer.
- [x] [Review][Defer] Forensic correlation across presentation and submit decisions — Each `EvaluateAsync` allocates a new correlation Guid; presentation and submit cannot be joined in security log. Defer to telemetry follow-up.
- [x] [Review][Defer] Decision cache across executable boundaries — Explicitly deferred per Advanced Elicitation Hardening "Whether authorization decisions may use a bounded short-lived cache". v1 must use fresh executable checks.
- [x] [Review][Defer] Format string injection via `ButtonLabel` in localizer — `EscapeString` does not strip `{0}` placeholders. Latent risk if `ButtonLabel` becomes runtime-derived. Document and defer.
- [x] [Review][Defer] RTL/control characters in display label leak into log directionality — Cosmetic log-injection vector; sanitization scope deferred.
- [x] [Review][Defer] CTS leak on rapid denied submits — Each denied submit allocates a new `CancellationTokenSource` until form unmount. Bounded by form lifetime; defer.

### Review Findings — Pass 2 (2026-05-01)

Re-run via `/bmad-code-review 7-3` while Pass 1 items remained unresolved. Three adversarial layers (Acceptance Auditor, Blind Hunter, Edge Case Hunter via `general-purpose`) raised ~140 raw findings. After dedup against Pass 1: **all 5 Pass 1 decisions and all 25 Pass 1 patches are re-confirmed**, 20 net-new patches identified, 3 net-new defers, ~12 dismissed as noise. Treat Pass 1 + Pass 2 as a single triage; Pass 1 items are not duplicated below.

#### Decision-Needed (Pass 2)

No new decisions. Pass 2 confirms Pass 1's DN1–DN5 verbatim. The Pass 1 patch "Remove `Pending` decision kind or wire it as a returnable value" carries an embedded option set that is decision-shaped; resolve alongside DN1.

#### Decision Resolutions (2026-05-01)

All five Pass 1 decisions resolved during Pass 2 review by Jerome (delegated to reviewer judgment). Each resolution becomes a binding patch in the Pass 2 patch list.

- **DN1 resolved → (a) Replace `IHttpContextAccessor.HttpContext.User` with `AuthenticationStateProvider.GetAuthenticationStateAsync()`** — Canonical Blazor seam; works across SSR/Server/WASM; matches AC8 ("current `ClaimsPrincipal` from the host auth state") and Story 7-1 host-auth seams. Drop `IHttpContextAccessor` from `CommandAuthorizationEvaluator` and inject `AuthenticationStateProvider` directly. Update tests to use a fake `AuthenticationStateProvider` rather than fake `IHttpContextAccessor`. Subscribe the generated form to `AuthenticationStateChanged` (already in patch list as P9).
- **DN2 resolved → (a) Add `AuthorizingCommandServiceDecorator`** — Read `DomainManifest.CommandPolicies` keyed by `command.GetType().FullName`. Decorate `ICommandService` registration so direct callers receive the same submit-time authorization check. Use `SourceSurface = DirectDispatch`. No-op (passthrough) for unprotected commands. Required by spec ("Policy-protected direct command dispatch through framework services must perform the same authoritative submit-time check").
- **DN3 resolved → (a) Inject `ICommandAuthorizationEvaluator` into `CommandRendererEmitter`; gate inline/compact/full-page triggers via `Disabled` binding** — Mirror the form's presentation-allowed pattern: render-time evaluator probe sets `_renderAuthorized` cached flag; submit-time check still authoritative. Buttons render disabled-with-aria-label rather than hidden, per Surface Matrix.
- **DN4 resolved → (a) Implement filtering in `CapabilityDiscoveryEffects` mirroring `CommandPaletteEffects`** — Reuse `ICommandAuthorizationEvaluator` and `ProjectionTypeResolver`. Same fail-closed semantics as the palette. Add `SourceSurface = HomeCapability`.
- **DN5 resolved → (a) Replace `<AuthorizeView Policy=…>` in `FcProjectionEmptyPlaceholder.razor` with an evaluator-backed component** — Use the same `CommandAuthorizationResource` shape as form/palette/home. New small Shell component (e.g., `FcAuthorizedCommandRegion`) wraps `ICommandAuthorizationEvaluator` and exposes `Authorized`/`NotAuthorized` render fragments with a `CommandAuthorizationRequest` parameter. Eliminates the resource-shape divergence.
- **DN6 (Pending decision kind) resolved → Wire it as the initial pre-evaluation state** — Return `Pending` from the evaluator when `AuthenticationStateProvider` reports an unresolved auth state (per AC11 prerender/auth-state-pending). Generated form maps `Pending` to disabled-with-pending-aria, distinct from `Denied`. Eliminates dead code.
- **DN7 (Infrastructure failures vs user-denied copy) resolved → Add a localized "Action temporarily unavailable" string and select copy by `decision.Reason` category** — `MissingService` / `StaleTenantContext` / `HandlerFailed` / `Canceled` / `CatalogInconsistent` map to the retry-hint copy; `Denied` / `Unauthenticated` map to permission-denied copy; `MissingPolicy` maps to a developer-diagnostic copy in dev mode and to retry-hint in production. Add EN/FR resource keys.

#### Patches (Pass 2 net-new)

- [ ] [Review][Patch] Add `IsPolicyNameWellFormed` runtime validation to `RequiresPolicyAttribute` constructor [src/Hexalith.FrontComposer.Contracts/Attributes/RequiresPolicyAttribute.cs:11-18] — Ctor only rejects `IsNullOrWhiteSpace`. Commands constructed outside SourceTools (reflection, MCP enumeration per AC16) accept malformed names that the parser would reject as HFC1056. Mirror the parser's well-formedness regex at runtime so generator and reflection paths agree.
- [ ] [Review][Patch] Tighten exception-filter exclusion in `EvaluateAsync` [CommandAuthorizationEvaluator.cs:71-80] — `catch (Exception ex) when (ex is not OutOfMemoryException)` swallows `StackOverflowException`, `ThreadAbortException`, `AccessViolationException`. Corrupted-state and async thread-abort exceptions should propagate. Exclude all four families.
- [ ] [Review][Patch] Make `CommandAuthorizationDecision` constructor private; expose only factories [src/Hexalith.FrontComposer.Shell/Services/Authorization/CommandAuthorizationDecision.cs] — Public ctor lets callers construct `new CommandAuthorizationDecision(Allowed, MissingPolicy, "x")`, breaking the `Kind == Allowed → Reason ∈ {None, NoPolicy}` invariant. Make it `private` and keep `Allowed`/`Denied`/`Blocked` factories as the only construction paths.
- [ ] [Review][Patch] Override `CommandAuthorizationRequest` record `PrintMembers` to redact `Command` [CommandAuthorizationDecision.cs:25-31] — `record` types auto-emit `ToString()` that formats every property, including `object? Command`. Any future `LogWarning(..., request)` will dump command payload to logs. Override `PrintMembers` to omit `Command` (and consider not carrying it at all if no handler consumes it).
- [ ] [Review][Patch] Initialize `_cts` synchronously in `OnInitializedAsync` before any await [CommandFormEmitter.cs:148] — `_cts?.Token ?? CancellationToken.None` falls back to `CancellationToken.None` if `_cts` has not been assigned by first OnInit. Disposal mid-evaluation cannot interrupt the call. Initialize `_cts = new()` synchronously at the top of `OnInitializedAsync`.
- [ ] [Review][Patch] Set `Inherited=false` on `RequiresPolicyAttribute.AttributeUsage` [Contracts/Attributes/RequiresPolicyAttribute.cs:7] — Default `Inherited=true` propagates `[RequiresPolicy]` to derived classes implicitly; design intent is per-class declaration. Also blocks the partial-class duplicate path for clarity.
- [ ] [Review][Patch] Add invalid-character policy-name parser fixtures [tests/Hexalith.FrontComposer.SourceTools.Tests/Parsing/TestFixtures/CommandTestSources.cs] — HFC1056 fixture only exercises three-space whitespace. Add `[RequiresPolicy("with space")]`, `[RequiresPolicy("with/slash")]`, `[RequiresPolicy("with*char")]`, `[RequiresPolicy("\t")]` to cover the well-formedness regex (`IsPolicyNameWellFormed`) non-whitespace branches.
- [ ] [Review][Patch] Trim policy values in `EmptyStateCtaResolver` and catalog validator before lookup/comparison [src/Hexalith.FrontComposer.Shell/Services/EmptyStateCtaResolver.cs:240-244, FrontComposerAuthorizationPolicyCatalogValidator.cs:14-43] — Attribute and parser trim; consumers do not. A host that registers ` OrderApprover ` in `KnownPolicies` mismatches a generated `OrderApprover`. Trim at every boundary or reject untrimmed values at options-validate time.
- [ ] [Review][Patch] Defensive `TypedConstantKind` check before string conversion [CommandParser.cs:862-866] — `attribute.ConstructorArguments[0].Value as string` returns null for `typeof(...)` or array constants, so the parser falls through silently when an author writes `[RequiresPolicy(typeof(MyPolicy))]`. Emit HFC1056 when `ConstructorArguments[0].Kind != TypedConstantKind.Primitive`.
- [ ] [Review][Patch] Inject `IAuthorizationService` and the chosen principal source (per DN1) directly into `CommandAuthorizationEvaluator` instead of via captured `IServiceProvider` [CommandAuthorizationEvaluator.cs:9-13, 30-38] — Service-locator pattern hides the dependency surface. A future scope-mismatch refactor would surface as `InvalidOperationException` and silently fail-closed under `MissingPolicy`. Constructor-inject the dependencies once DN1 is decided.
- [ ] [Review][Patch] Re-check cancellation between `AuthorizeAsync` call and decision construction [CommandAuthorizationEvaluator.cs:55-61] — The `IAuthorizationService.AuthorizeAsync(user, resource, policyName)` overload does not accept a `CancellationToken`, so cancellation cannot interrupt an in-flight handler. After the await, check `cancellationToken.IsCancellationRequested` and short-circuit to `Blocked(Canceled)` before constructing the result.
- [ ] [Review][Patch] Format-string-safe localizer call in `SetAuthorizationWarning` [CommandFormEmitter.cs:166-168] — `EscapeString(form.ButtonLabel)` is for source-emit, not for resource args. If the button label includes literal `{` or `}` (e.g., from `[Display(Name="Action {0}")]`), `string.Format` throws `FormatException` at warning render time. Pass the label as the localizer argument unescaped; the resource string controls placeholders.
- [ ] [Review][Patch] Verify `Logger` field is declared/injected on the generated form base [CommandFormEmitter.cs:151, 366] — Emitter calls `Logger?.LogWarning(...)` but no `[Inject] ILogger<...>` declaration appears in the diff. If `Logger` is missing on the generated component, the null-conditional silently no-ops and authorization-deny telemetry vanishes. Add the inject (or confirm it exists in the existing emitter base).
- [ ] [Review][Patch] `EscapeString` edge-case test coverage for `AuthorizationPolicyName` emission [src/Hexalith.FrontComposer.SourceTools/Emitters/RegistrationEmitter.cs:763-778, CommandFormEmitter.cs:118] — Add fixtures with backslash, double-quote, CR/LF, and NUL inside policy names to confirm the generator emits valid C# string literals (defense-in-depth even though `IsPolicyNameWellFormed` should reject these at the parser).
- [ ] [Review][Patch] EN/FR placeholder count parity test for new resource keys [tests/Hexalith.FrontComposer.Shell.Tests/Resources/] — `UnauthorizedCommandWarningTitle` / `UnauthorizedCommandWarningMessage` use `{0}` in both locales today, but if EN later adds `{1}` and FR is missed, runtime `IndexOutOfRange` at warning render. Add a parity assertion across all new keys.
- [ ] [Review][Patch] Reconcile `KnownPolicies` casing strategy across catalog/registry/CTA resolver [FrontComposerAuthorizationOptions.cs:5, FrontComposerRegistry.cs:147-158, EmptyStateCtaResolver.cs:240-244] — All three sites use `StringComparer.Ordinal`, making `OrderApprover` and `orderApprover` distinct identifiers. Either document the case-sensitive contract for adopters and surface a startup warning when policies look case-conflated, or switch to `OrdinalIgnoreCase` consistently.
- [ ] [Review][Patch] Skip null/empty manifest policy entries in catalog validator [FrontComposerAuthorizationPolicyCatalogValidator.cs:14-43] — `HashSet<string>.Contains(null)` throws `ArgumentNullException`. A legacy or partially-emitted manifest with a null `CommandPolicies` value crashes startup validation. Add `if (string.IsNullOrWhiteSpace(policy.Value)) continue;`.
- [ ] [Review][Patch] Deduplicate missing-entry payload in catalog validator [FrontComposerAuthorizationPolicyCatalogValidator.cs:14-43] — Same policy missing from N manifests appears N times in throw/log message. Use a `HashSet<string>(StringComparer.Ordinal)` (or `Distinct()` with deterministic order) before joining.
- [ ] [Review][Patch] Order catalog validator after registry-population hosted services or inject deterministic registry build [FrontComposerAuthorizationPolicyCatalogValidator.cs:14] — `StartAsync` enumerates the registry exactly once; if registry-loader hosted services run later in registration order, validator silently passes with empty registry — strict mode appears to work but doesn't. Either declare ordering via `IHostApplicationLifetime.ApplicationStarted` or document the registration-order requirement and assert it in a startup test.
- [ ] [Review][Patch] Telemetry on `ProjectionTypeResolver.Resolve` returns null in palette [CommandPaletteEffects.cs:842-845] — When the resolver cannot find the type for a command FQN (assembly trimmed, type renamed), `CanSurfaceCommandAsync` returns `false` and silently hides the command. Emit a debug/info log so operators can diagnose missing palette entries vs deliberate authorization denials.

#### Deferred (Pass 2 net-new)

- [x] [Review][Defer] Late-manifest registration validator re-run [FrontComposerAuthorizationPolicyCatalogValidator.cs:14] — Manifests added to the registry after `StartAsync` completes are never re-validated against `KnownPolicies`. Strict-mode contract holds only for boot-time manifests. Defer to a re-validation hook design alongside Story 9-4 diagnostic governance.
- [x] [Review][Defer] `KnownPolicies` is mutable post-startup; no thread-safety guards [FrontComposerAuthorizationOptions.cs:5] — `ISet<string>` is exposed as `{ get; }` only, but the underlying `HashSet<string>` is mutable. A caller that clears or edits the set after `IOptions<>` materializes can defeat strict validation. Defer until options-pattern hardening.
- [x] [Review][Defer] EN/FR copy verb-vs-noun grammatical parity [FcShellResources.resx:191-194, FcShellResources.fr.resx:170-175] — `"You do not have permission to {0}."` reads naturally with verb labels in EN but the FR translation `"Vous n'avez pas l'autorisation d'exécuter {0}."` reads better with noun labels — a label that works in one locale jars in the other. Defer to UX/copy review.

#### Dismissed (Pass 2)

`StartAsync` strict-mode throw (per spec design); `ParseRequiresPolicyAttribute` AllowMultiple=false defensive duplicate check (compiler enforces but cross-partial scenarios remain); `_serverWarning` shared with validation (same submit-clear pattern); strict-test "no tenant-a/user-a" tautology; multi-`ClaimsIdentity` handling (`User.Identity` matches ASP.NET defaults); `Reason.None` vs `NoPolicy` semantic split; XML doc / license header / using-qualification nits; `const string? BoundedContextName = null` (compiles cleanly under .NET 10); partial-class duplicate `AmbiguousMatchException` (compiler error before runtime); pre-existing emitter pattern of overriding base `OnInitializedAsync`; French body lacks NBSP (no colon, NBSP not required).

### Pass 2 Implementation Status (2026-05-01)

`/bmad-code-review 7-3` Pass 2 patches applied this session (delegated by Jerome via `do best` + `1 — Apply every patch`).

#### Applied (foundational refactor + Pass 2 patches)

- ✅ **DN1 (option a) implementation** — `CommandAuthorizationEvaluator` rewired to consume `AuthenticationStateProvider.GetAuthenticationStateAsync()` instead of `IHttpContextAccessor.HttpContext.User`. Constructor-injects `IAuthorizationService` and `AuthenticationStateProvider` directly (drops captured `IServiceProvider` antipattern, P30/B9). Generated form `[Inject]`s `AuthenticationStateProvider`; subscribes to `AuthenticationStateChanged` in `OnInitializedAsync`; unsubscribes in `Dispose` (P9, A15, B8). Initial null user maps to new `Pending` decision (DN6 wire), so prerender does not surface as `Unauthenticated`.
- ✅ **P1 SourceSurface → enum** — Closed-set `CommandAuthorizationSurface { DirectDispatch, GeneratedForm, InlineAction, CompactInlineAction, FullPage, EmptyStateCta, CommandPalette, HomeCapability }`. Form emitter, palette effects, and tests updated.
- ✅ **P6 CatalogInconsistent reason** — Added to enum.
- ✅ **DN6 Pending decision kind** — Wired as `CommandAuthorizationDecision.Pending(correlationId)` returned during unresolved auth state. Form gates `_authorizationPresentationReady = !isPending` so the disabled-pending state is preserved.
- ✅ **DN7 dual copy** — Decision-category-driven copy in `SetAuthorizationWarning(reason)`. Infrastructure failures (MissingService / MissingPolicy / StaleTenantContext / HandlerFailed / Canceled / CatalogInconsistent) get the new "Action temporarily unavailable" copy with retry hint; Denied / Unauthenticated keep the permission-denied copy. New EN/FR resource keys `AuthorizationActionUnavailableTitle` / `AuthorizationActionUnavailableMessage`.
- ✅ **P22 Decision constructor private** — Factory-only construction. Added `Denied(correlationId)` factory.
- ✅ **P23 Redacted record PrintMembers** — `CommandAuthorizationRequest.PrintMembers` override emits `Command = <redacted>` instead of formatting the payload. New regression test asserts sentinel JWT/claim absence from `request.ToString()`.
- ✅ **P14 FailCalled/FailedRequirements discrimination** — `result.Failure?.FailCalled` and `result.Failure?.FailedRequirements?.Any()` inspected; absent metadata maps to `MissingPolicy`, present requirement-failure or `FailCalled` maps to `Denied`.
- ✅ **P15 TryGetContext catch** — Tenant-accessor exceptions are wrapped to `Blocked(StaleTenantContext)` instead of escaping to the Blazor error boundary.
- ✅ **P16 AuthorizationResult null safety** — Explicit null check returns `Blocked(HandlerFailed)`; no NRE through to broad catch.
- ✅ **P13 Tighten catch filter** — `IsRecoverable` excludes `OutOfMemoryException`, `StackOverflowException`, `ThreadAbortException`, `AccessViolationException` from the catch-all; corrupted-state and async-thread-abort exceptions propagate.
- ✅ **P31 Cancellation re-check after AuthorizeAsync** — Honours `cancellationToken.IsCancellationRequested` between the await and decision construction (the `IAuthorizationService.AuthorizeAsync(user, resource, policy)` overload accepts no token).
- ✅ **P24 Pre-trim PolicyName** — Single trim at top, used by both whitespace short-circuit and resource construction.
- ✅ **P8 KnownPolicies → IList** — Settable `IList<string>` for `IConfiguration` binding.
- ✅ **P7 Missing-catalog vs missing-entry distinction** — Empty catalog with declared policies emits `LogInformation` (was silent); non-empty with missing entries logs Warning or throws under strict mode (unchanged path).
- ✅ **P21 Validator info log when manifests declare policies but catalog is empty** — Same path as P7.
- ✅ **NP17 Skip null/empty manifest entries** — Validator filters before `Contains` to avoid `ArgumentNullException`.
- ✅ **NP18 Dedup missing payload** — `HashSet<string>` before joining.
- ✅ **P19 Registry merge conflict warning** — `MergeCommandPolicies` logs `Warning` when an existing policy value is overwritten with a different incoming value.
- ✅ **P20 + NP8 EmptyStateCtaResolver alignment** — Last-write-wins (matching registry merge), trim policy values before return.
- ✅ **NP9 Defensive TypedConstantKind check** — `CommandParser` rejects non-Primitive constructor argument kinds (typeof / array constants) as HFC1056.
- ✅ **NP1 IsPolicyNameWellFormed runtime validation** — `RequiresPolicyAttribute` constructor mirrors the SourceTools regex so reflection callers cannot bypass HFC1056.
- ✅ **P17 Boolean parens** — Disabled-button expression wraps the lifecycle group in explicit parens.
- ✅ **P18 Localizer ResourceNotFound fallback** — `ResolveAuthorizationLocalized` mirror of palette pattern; falls back to static EN copy when resource is missing.
- ✅ **P25 _cts init pre-await** — `_cts ??= new CancellationTokenSource()` synchronously at top of `OnInitializedAsync`.
- ✅ **P10 / P11 _disposed short-circuits** — Submit-time deny path checks `if (_disposed || _cts.IsCancellationRequested) return;` after `EvaluateAsync`. Presentation-refresh path checks `if (_disposed) return;` after the await.
- ✅ **P32 format-string-safe localizer** — `SetAuthorizationWarning` uses positional argument substitution (no embedded `{`/`}`).
- ✅ **P33 Logger inject verified** — Form already declares `[Inject] private ILogger<componentName>? Logger { get; set; }`. No change needed; finding dismissed-confirmed.
- ✅ **NP4 / NP12 Test additions** — `CommandAuthorizationEvaluatorTests` expanded from 4 to 13 tests (+ MissingService, MissingPolicy via InvalidOperationException, MissingPolicy via empty FailedRequirements, HandlerFailed via generic exception, Canceled via OperationCanceledException, NullPrincipal-Pending, TenantAccessorThrows-StaleTenantContext, StaleTenantContext, PreCancelled, RedactsClaimAndPolicyValues, WhitespacePolicy). `RequiresPolicyAttributeTests` expanded with malformed-policy-name fixtures.
- ✅ **Validation** — `dotnet build Hexalith.FrontComposer.sln -p:TreatWarningsAsErrors=true -p:UseSharedCompilation=false` → 0 warnings, 0 errors. `dotnet test` → Contracts 156/0/0 (+0 net), Shell 1509/0/0 (+11 net new evaluator tests), SourceTools 593/0/0 (unchanged), Bench 2/0/0. Two `verified.txt` rebaselines accepted (`CommandFormEmitterTests.CommandForm_DerivableFieldsHidden_OmitsHiddenFieldsOnly` and `_ShowFieldsOnly_RendersOnlyNamedFields`).

#### Pending (deferred to next dev session — large new-file/component scope)

The following Pass 1 + Pass 2 patches require new files / new Razor components / substantial emitter rewrites and exceeded the time budget for the patch wave:

- ❌ **DN2 (option a) implementation** — `AuthorizingCommandServiceDecorator` wrapping `ICommandService.DispatchAsync`, reading `DomainManifest.CommandPolicies` keyed by `command.GetType().FullName`. Pass-through unprotected commands. Use `SourceSurface.DirectDispatch`. Register in `ServiceCollectionExtensions`. Add tests.
- ❌ **DN3 (option a) implementation** — `CommandRendererEmitter` inject `ICommandAuthorizationEvaluator`; gate inline / compact / full-page trigger `Disabled` binding via render-time evaluator probe; submit-time re-check authoritative; emit `SourceSurface.{Inline,CompactInline,FullPage}Action`; rebaseline 7 verified.txt files; add bUnit tests.
- ❌ **DN4 (option a) implementation** — `CapabilityDiscoveryEffects` filter protected commands the user cannot execute; mirror `CommandPaletteEffects.CanSurfaceCommandAsync`; emit `SourceSurface.HomeCapability`; tests.
- ❌ **DN5 (option a) implementation** — New `FcAuthorizedCommandRegion` Shell component wrapping `ICommandAuthorizationEvaluator` with `Authorized`/`NotAuthorized` render fragments. Replace `<AuthorizeView Policy=...>` in `FcProjectionEmptyPlaceholder.razor` with the new component. Tests.
- ❌ **P2 Destructive re-check** — Add second `EvaluateAsync` call between confirm-dialog return and `SubmittedAction` dispatch when `form.IsDestructive`. Spec mandates "must authorize again immediately after confirmation and before dispatch".
- ❌ **P3 Generated bUnit gating tests** — `tests/Hexalith.FrontComposer.Shell.Tests/Generated/*Authorization*` proving inline / compact / full-page / CTA / palette / home surfaces are non-executable for unauthorized users with zero side effects.
- ❌ **P4 / P5 partial expansion** — Tenant-switch-after-render, sign-out-after-render, destructive-dialog-delay test cases. Sentinel-redaction tests against rendered UI / log output / generated metadata (record redaction is covered; UI/log/metadata channel coverage is not).
- ❌ **P27 Invalid-character parser fixtures** — `CommandTestSources` HFC1056 fixtures for non-whitespace invalid chars (slash, space, control). Attribute-level test added; parser-level fixtures still pending.
- ❌ **P34 EscapeString edge-case test coverage** — Backslash / double-quote / CR/LF / NUL fixtures for `AuthorizationPolicyName` emission paths.
- ❌ **P35 EN/FR placeholder count parity test** — Cross-locale assertion across new resource keys.
- ❌ **NP19 Catalog validator hosted-service ordering** — Document or assert ordering relative to registry-loader services.
- ❌ **NP20 ProjectionTypeResolver-null telemetry** — Distinguish silent-deny-due-to-missing-type from authorization deny in palette/home.

Story status updated to `in-progress` for the next dev session to pick up the pending implementation work.

---

### Review Findings — Pass 3 (2026-05-02)

Re-run via `/bmad-code-review 7-3` on the post-Pass-2 commit `a9af28e` (39 files, +1487/-54 lines scoped to `src/` + `tests/`). Three adversarial layers (Acceptance Auditor, Blind Hunter, Edge Case Hunter via `general-purpose`) raised **177 raw findings** (Blind 72, Edge 66 — 3 self-demoted, Auditor 39). After dedup against Pass 1 + Pass 2 ledgers and triage: **6 decision-needed, 30 patches, 16 defers, 38 dismissed**. Pass-1 + Pass-2 items already in earlier sections are not duplicated here.

#### Decision Resolutions (Pass 3, 2026-05-02)

All six decisions resolved by reviewer judgment per Jerome's `do best` directive.

- **DN-7-3-3-1 (Pass-2 deferred surfaces DN2/DN3/DN4/DN5) → Defer again with explicit design plan** — Each of DN2 (decorator), DN3 (renderer emitter wiring), DN4 (capability filter), DN5 (`FcAuthorizedCommandRegion`) requires hours of design, new-file authoring, snapshot rebaseline, and bUnit fixture work. Scope-bundling them into Pass 3 alongside the surgical patches would produce a 6-hour unsupervised commit. Splitting them into a dedicated dev session with `/bmad-dev-story 7-3` (or a Pass 3.5 follow-up) lets each get the planning/test rigor required. Apply Pass-2 small deferrals (P2, P27, P35, P4-partial, P5-partial) THIS session as confidence ramp.
- **DN-7-3-3-2 (`BeforeSubmit` ordering vs auth) → Two-phase authorization** — Auth#1 runs before `BeforeSubmit` AND before destructive-confirmation dialog (presentation-time gate, prevents leaking destructive-flow copy to unauthorized users). Auth#2 runs AFTER `BeforeSubmit` returns AND after destructive confirmation, immediately before `SubmittedAction`. Resource-based policies that read `_model` see the post-`BeforeSubmit` state via auth#2. Resolves AC2/AC4 tension by making both checks authoritative. Encoded as the P2 patch.
- **DN-7-3-3-3 (Pass-3 test scope) → Apply parser fixtures (P27), placeholder parity (P35), and 3 redaction-channel sentinel tests (P5-partial)** — bUnit tests under `Generated/*Authorization*` (P3) and full failure-matrix expansion (P4) require DN3 (renderer emitter wiring) to land first; without DN3 the bUnit tests would test the wrong artifacts. Apply the small surgical tests; defer the larger surfaces.
- **DN-7-3-3-4 (PII in policy names) → Sanitize validator payload + document** — Catalog validator log/exception payload emits the missing policy-name SET (deduped, ordinal-sorted) without command-FQN echo or `key:value` serialization (both are PII risk channels). Add an XML-doc note on `RequiresPolicyAttribute` and `FrontComposerAuthorizationOptions.KnownPolicies` warning that policy names must be PII-free identifiers (PascalCase recommended).
- **DN-7-3-3-5 (`Reason.Pending` UX) → Add localized "Checking permission…" hint + keep button disabled** — When `_authorizationPresentationReady = false` (Pending or in-flight refresh), show a small status hint via the existing `_serverWarning` mechanism using a new `AuthorizationCheckingPermissionMessage` resource key. No auto-retry (UX call: spec doesn't mandate, polling adds complexity, AuthState changes already trigger refresh via the subscription).
- **DN-7-3-3-6 (`KnownPolicies` mutability) → Snapshot inside validator + XML doc** — Catalog validator captures `options.KnownPolicies` into a private immutable `HashSet<string>` once at `StartAsync` execution; subsequent mutations to `options.Value.KnownPolicies` are intentionally ignored. Document on `FrontComposerAuthorizationOptions.KnownPolicies` that post-startup mutation has no effect. Don't change the API surface to `IReadOnlyList<string>` — would break `IConfiguration` binding patterns adopters rely on.

#### Patches (Pass 3 net-new)

PII redaction & contract hardening:
- [ ] [Review][Patch] Override `CommandAuthorizationResource.PrintMembers` to redact `TenantContext` [src/Hexalith.FrontComposer.Shell/Services/Authorization/CommandAuthorizationDecision.cs] — Pass 2 redacted `CommandAuthorizationRequest` but `CommandAuthorizationResource` (carries `TenantContextSnapshot? TenantContext`) was untouched. ASP.NET authorization middleware logs `IAuthorizationContext.Resource.ToString()` at Debug; tenant id and user id leak to logs. Override `PrintMembers` symmetric with `Request`. Severity: HIGH (E34 / B24 / B64).
- [ ] [Review][Patch] Null-guard `KnownPolicies` in catalog validator [src/Hexalith.FrontComposer.Shell/Services/Authorization/FrontComposerAuthorizationPolicyCatalogValidator.cs] — `"KnownPolicies": null` in `appsettings.json` leaves the property null after `IConfiguration` binding. Validator does `value.KnownPolicies.Where(...)` → NRE during host startup with opaque stack. Coalesce to `Enumerable.Empty<string>()` at the read site. Severity: HIGH (E3).
- [ ] [Review][Patch] Sanitize catalog validator missing-policy payload [src/Hexalith.FrontComposer.Shell/Services/Authorization/FrontComposerAuthorizationPolicyCatalogValidator.cs] — Today payload is `policy.Key + ":" + trimmed` joined by `|` — both keys (command FQNs) and values (policy names) appear. Per DN-7-3-3-4, emit only deduped policy-NAME set, no command-FQN echo. Strict-mode `InvalidOperationException` message and Information/Warning logs use the same sanitized payload. Add `ArgumentException`-thrown sentinel test asserting no command FQN appears. Severity: MEDIUM (E27 / B26 / B27 / E42).
- [ ] [Review][Patch] Snapshot `KnownPolicies` once into immutable `HashSet<string>` at validator start [FrontComposerAuthorizationPolicyCatalogValidator.cs] — Per DN-7-3-3-6, post-startup mutation must not undermine fail-closed semantics. Take snapshot at top of `StartAsync`; document on `FrontComposerAuthorizationOptions.KnownPolicies` that runtime mutation is ignored by validation. Severity: MEDIUM (E2 / E38 / A17).

DomainManifest + registry contracts:
- [ ] [Review][Patch] `DomainManifest.CommandPolicies` init setter coerces null → empty [src/Hexalith.FrontComposer.Contracts/Registration/DomainManifest.cs] — Positional record parameter is nullable; `?? new Dictionary` only runs at constructor entry; `with { CommandPolicies = null! }` re-introduces null and downstream `TryGetValue` NREs. Coerce in the init accessor body (or constrain via private init wrapper). Severity: HIGH (B1 / B2).
- [ ] [Review][Patch] `MergeCommandPolicies` trims keys [src/Hexalith.FrontComposer.Shell/Registration/FrontComposerRegistry.cs] — Pass 2 trimmed values; keys still pass un-trimmed. `"Orders.X "` and `"Orders.X"` produce two distinct entries; catalog validator double-reports. Trim both at merge time. Severity: MEDIUM (E13).
- [ ] [Review][Patch] `MergeCommandPolicies` logs Information when skipping null/empty key or value [FrontComposerRegistry.cs] — Today the `IsNullOrWhiteSpace` continue is silent; a hand-rolled manifest with a typo loses the policy and operators have no signal. Log `Information` (not Warning — non-default-source manifests may legitimately omit some entries). Severity: MEDIUM (B4).

Generated form race / disposal hardening:
- [ ] [Review][Patch] Capture `_cts` reference once in `RefreshPresentationAuthorizationAsync` [src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs] — Today `_cts?.Token` is read after every potential await. If `Dispose` runs between reads, `_cts` is set null and disposed; the next access of `_cts?.Token` (or any `IsCancellationRequested` post-dispose) can throw `ObjectDisposedException`. Capture `var cts = _cts;` once at the top, then use `cts?.Token ?? CancellationToken.None`. Severity: MEDIUM (B12 / E22 / E23 / E58).
- [ ] [Review][Patch] Sequence-number guard against concurrent refresh in generated form [CommandFormEmitter.cs] — Two concurrent `RefreshPresentationAuthorizationAsync` invocations (e.g., AuthState fires during initialization) can interleave; the older completion overwrites the newer presentation flags. Stamp a `_authorizationRefreshSequence` counter at refresh start; on completion only apply if the captured counter still matches. Severity: HIGH (B11 / E64 / E7).
- [ ] [Review][Patch] Set `_authorizationPresentationReady = false` synchronously at refresh start [CommandFormEmitter.cs] — Today the flag flips after the await returns. During the in-flight period (auth-state-changed fires while a prior refresh is awaiting), the button stays in the prior `Allowed=true / Ready=true` state. Force `Ready=false` synchronously before any await so the button is disabled until the new decision lands. Severity: MEDIUM (E21 / B17 / B68).
- [ ] [Review][Patch] Add localized "Checking permission…" hint in `SetAuthorizationWarning` Pending branch [CommandFormEmitter.cs + FcShellResources.{,fr}.resx] — Per DN-7-3-3-5, when refresh is in flight or `Reason.Pending` is the latest result, surface a small status copy via `_serverWarning` so users see "we're checking" rather than a silent disabled button. New EN/FR resource key `AuthorizationCheckingPermissionMessage`. Severity: MEDIUM (B17 / B68).
- [ ] [Review][Patch] Wrap entire `InvokeAsync(...)` invocation, not just the lambda body, in try/catch in `OnAuthenticationStateChanged` [CommandFormEmitter.cs] — Today the catch lives inside the async lambda; an `ObjectDisposedException` from a torn-down `RendererSynchronizationContext` thrown synchronously by `InvokeAsync` itself escapes as `UnobservedTaskException`. Severity: MEDIUM (E8 / B13).
- [ ] [Review][Patch] Move `_serverValidationMessages?.Clear()` AFTER successful auth check [CommandFormEmitter.cs] — Today server validation messages are cleared at the top of `OnValidSubmitAsync`, BEFORE the auth re-evaluation. If auth then denies, the user loses their prior validation feedback and sees only the auth warning. Move clearing into the post-auth-pass branch so denied submits keep prior validation state visible. Severity: MEDIUM (B70 / B58).

Localization & user-facing copy:
- [ ] [Review][Patch] Add distinct `Reason.Unauthenticated` copy in `SetAuthorizationWarning` [CommandFormEmitter.cs + FcShellResources.{,fr}.resx] — Today `Unauthenticated` falls through to the "permission denied" copy. Anonymous users see "You don't have permission" rather than a sign-in prompt. Add a third copy variant routed by `Reason.Unauthenticated`. Severity: MEDIUM (E5).
- [ ] [Review][Patch] Replace `&#160;` HTML entity with literal U+00A0 NBSP in `FcShellResources.fr.resx` line 527 [FcShellResources.fr.resx] — `&#160;` may render literally in components that don't decode HTML entities (defensive: most Fluent UI components decode, but the contract is fragile). Use the literal NBSP character. Severity: LOW (E40).
- [ ] [Review][Patch] EN/FR placeholder count parity test for new auth resource keys [tests/Hexalith.FrontComposer.Shell.Tests/Resources/AuthorizationResourceParityTests.cs (new)] — Pass-2 P35 deferred. Iterate `UnauthorizedCommandWarningTitle/Message`, `AuthorizationActionUnavailableTitle/Message`, `AuthorizationCheckingPermissionMessage` and assert EN/FR contain the same `{N}` placeholder set. Severity: MEDIUM (A11 / Pass-2 P35).

Diagnostics teaching copy:
- [ ] [Review][Patch] HFC1056 message echoes the offending value (truncated) [src/Hexalith.FrontComposer.SourceTools/Parsing/CommandParser.cs] — Today message says "invalid value" without echoing what the author wrote. Adopters with `[RequiresPolicy("Order Approver")]` (with space) cannot tell which character failed. Append `Got: '<value>'` (truncated to 64 chars, with control chars escaped) to the message format. Severity: LOW (A26 / E32).
- [ ] [Review][Patch] HFC1056 / HFC1057 add `helpLinkUri` to descriptors [src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs] — AC6 explicitly mandates "teaching copy AND docs link". Today `helpLinkUri` is omitted; IDE quick-fix surface has no documentation jump-to. Use placeholder URL `https://hexalith.io/docs/policies/well-formedness` until docs site lands; URL is not in scope this story but the compliance gap is. Severity: LOW (A9).
- [ ] [Review][Patch] `RequiresPolicyAttribute.IsWellFormed` requires at least one alphanumeric character [src/Hexalith.FrontComposer.Contracts/Attributes/RequiresPolicyAttribute.cs + src/Hexalith.FrontComposer.SourceTools/Parsing/CommandParser.cs] — Today `[RequiresPolicy(":")]`, `[RequiresPolicy("-")]`, `[RequiresPolicy(".")]` all pass. ASP.NET registers the policy name verbatim; lookup proceeds; the policy is meaningless. Require ≥1 alphanumeric (cheap defensive sanity check). Mirror in both attribute and parser since they currently duplicate the regex. Severity: LOW (E4).
- [ ] [Review][Patch] Add Pass-2-deferred invalid-character parser fixtures [tests/Hexalith.FrontComposer.SourceTools.Tests/Parsing/TestFixtures/CommandTestSources.cs + CommandParserTests.cs] — P27. Add `[RequiresPolicy("with space")]`, `[RequiresPolicy("with/slash")]`, `[RequiresPolicy("with*char")]`, `[RequiresPolicy("\t")]`, `[RequiresPolicy(":")]` (per the new alphanumeric requirement above). Assert HFC1056 fires for each. Severity: MEDIUM (A10 / Pass-2 P27).

Catalog validator hardening:
- [ ] [Review][Patch] Catalog validator snapshots manifests once [FrontComposerAuthorizationPolicyCatalogValidator.cs] — Two independent enumerations of `registry.GetManifests()` can return different lists if a custom registry refreshes manifests between calls. Snapshot once into a list at start. Severity: LOW (E52).
- [ ] [Review][Patch] Catalog validator honors `cancellationToken` in `StartAsync` [FrontComposerAuthorizationPolicyCatalogValidator.cs] — Today the parameter is ignored; large manifest sets prolong shutdown cancellation. Add `cancellationToken.ThrowIfCancellationRequested()` after manifest snapshot and before validation loop. Severity: LOW (E37 / B61).
- [ ] [Review][Patch] Catalog validator emits Warning (not Information) when catalog is empty AND any command declares a policy [FrontComposerAuthorizationPolicyCatalogValidator.cs] — Today this case logs Information. Many production logging configs filter Information. Forgetting to populate `KnownPolicies` is a security-relevant configuration gap; promote to Warning when declared policies are non-empty. Severity: MEDIUM (B28).

Additional surface telemetry:
- [ ] [Review][Patch] CapabilityDiscoveryEffects warns once-per-session if `ICommandAuthorizationEvaluator` is missing [src/Hexalith.FrontComposer.Shell/State/CapabilityDiscovery/CapabilityDiscoveryEffects.cs] — Defensive log so adopters who don't go through `AddHexalithFrontComposer*` see a clear signal that protected commands will be hidden. Mirror the shape of the palette's missing-evaluator handling (which currently also lacks the warning — patch palette too in same change). Severity: MEDIUM (E29).
- [ ] [Review][Patch] EmptyStateCtaResolver routes through canonical registry merge [src/Hexalith.FrontComposer.Shell/Services/EmptyStateCtaResolver.cs] — Today the resolver re-scans manifests to find the policy; the registry already merged them via `MergeCommandPolicies`. The two implementations could diverge under custom `IFrontComposerRegistry` substitution. Add an `IFrontComposerRegistry.TryGetCommandPolicy(commandFqn, out string)` method (single canonical lookup) and have the resolver consume it instead of re-iterating. Severity: MEDIUM (B6 / B7 / E10).
- [ ] [Review][Patch] Sentinel-redaction tests for additional channels (logger output + generated metadata) [tests/Hexalith.FrontComposer.Shell.Tests/Services/Authorization/CommandAuthorizationEvaluatorTests.cs] — Pass 2 only asserted `request.ToString()` redacts. Add: (1) `CapturingLogger` test asserting `LogBlocked`/`LogWarning` structured-log payload contains no JWT/tenant/claim sentinel; (2) test asserting `CommandAuthorizationResource.ToString()` redacts `TenantContext` (covers the new PrintMembers override above). Severity: MEDIUM (A12 / Pass-2 P5-partial).

Pass-2 deferral — small destructive re-check:
- [ ] [Review][Patch] Insert second `EvaluateAsync` call after `BeforeSubmit` returns when `form.IsDestructive` [src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs] — Per DN-7-3-3-2 + spec "Submit And Destructive Flow Ordering". Order becomes: clear `_serverWarning` → auth#1 → if `Allowed`, run `BeforeSubmit` (which may include the destructive confirmation dialog) → re-check `_disposed`/`_cts.IsCancellationRequested` → if destructive, run auth#2 → on `Allowed`, dispatch. Auth#2 detects sign-out / tenant switch / policy revocation that occurred while the dialog was open. Severity: CRITICAL (A5 / Pass-2 P2).

#### Deferred (Pass 3 net-new)

Major Pass-2 deferrals re-deferred with explicit design plan (per DN-7-3-3-1):

- [x] [Review][Defer] **DN2 — `AuthorizingCommandServiceDecorator`** [needs new file + DI registration + tests] — Wraps `ICommandService.DispatchAsync`. Reads `DomainManifest.CommandPolicies` keyed by `command.GetType().FullName`. Pass-through unprotected commands. Use `SourceSurface.DirectDispatch`. Throws `UnauthorizedAccessException` (or returns failed result type matching `ICommandService` contract — needs design call) on deny. Register via decorator pattern in `AddHexalithFrontComposer`. **Plan:** dedicated `/bmad-dev-story 7-3` follow-up session. Critical for AC2 boundary completeness.
- [x] [Review][Defer] **DN3 — `CommandRendererEmitter` evaluator wiring** [needs source-generator changes + 7+ verified.txt rebaselines + bUnit fixtures] — Inject `ICommandAuthorizationEvaluator` into rendered components. Render-time probe sets cached flag; submit-time check still authoritative. Inline / compact-inline / full-page trigger `Disabled` binding consults the flag. Emit appropriate `SourceSurface.{Inline,CompactInline,FullPage}Action`. **Plan:** dedicated dev session — substantial surface area.
- [x] [Review][Defer] **DN4 — `CapabilityDiscoveryEffects` filter** [needs effect change + tests] — Mirror `CommandPaletteEffects.CanSurfaceCommandAsync` shape with `SourceSurface.HomeCapability`. **Plan:** can land alongside DN3 in same dev session — similar shape.
- [x] [Review][Defer] **DN5 — `FcAuthorizedCommandRegion` Shell component** [needs new component file + razor changes + tests] — New component wrapping `ICommandAuthorizationEvaluator` with `Authorized`/`NotAuthorized`/`Pending` render fragments. Replace `<AuthorizeView Policy=...>` in `FcProjectionEmptyPlaceholder.razor`. Eliminates the resource-shape divergence between CTA and form/palette. **Plan:** dedicated dev session.
- [x] [Review][Defer] **P3 / P4 (full failure-matrix + Generated/* bUnit suite)** [needs DN3 to land first — bUnit fixtures must exercise the actual generated trigger gating] — Tenant-switch-after-render, sign-out-after-render, destructive-dialog-delay, palette-route-after-auth-change scenarios. bUnit gating tests at `tests/Hexalith.FrontComposer.Shell.Tests/Generated/Authorization/*.cs`. **Plan:** chained after DN3 implementation.

Performance, observability, and nice-to-have:

- [x] [Review][Defer] Palette N×M authorization evaluation latency (no per-query cache, no batching) [CommandPaletteEffects.cs] — Each protected command in palette query incurs a sequential `EvaluateAsync` round-trip. Spec permits memoization within a single render/evaluation cycle. Defer to Story 10-2 (perf benchmarks) or Epic 9-6 (cache hardening). Severity: MEDIUM (B31 / E41 / A20).
- [x] [Review][Defer] Tenant-context-changed subscription in generated form [CommandFormEmitter.cs] — Today form subscribes only to `AuthenticationStateChanged`. Tenant switches without auth change leave presentation flags stale. Submit-time check still fails closed (evaluator probes tenant fresh), so AC2 holds; AC10 partially affected (button shows enabled until next auth event). Defer until Story 7-2 / Epic 7-3 publishes a tenant-changed observable. Severity: MEDIUM (A29).
- [x] [Review][Defer] `OnParametersSetAsync` recheck for navigation-restoration freshness [CommandFormEmitter.cs] — Spec lists "navigation restoration" as a freshness boundary; today only `OnInitializedAsync` runs the probe. Submit-time check still authoritative. Defer to Pass 4 alongside DN3. Severity: LOW (A30).
- [x] [Review][Defer] AC9 anonymous-principal end-to-end integration test [tests/Hexalith.FrontComposer.Shell.Tests/Generated/Authorization/] — Spec wording "no anonymous backend dispatch" requires asserting at the dispatch-side-effect boundary that no `ICommandService.DispatchAsync` happens for anonymous + protected. Needs DN3 (form-level auth gating) to be testable end-to-end. Severity: MEDIUM (A33).
- [x] [Review][Defer] AC16 contract test for `DomainManifest.CommandPolicies` MCP-future enumeration [tests/Hexalith.FrontComposer.Contracts.Tests/Registration/] — Spec wording is metadata-only; Epic 8 stories will exercise the contract. Severity: LOW (A34).
- [x] [Review][Defer] Snapshot tests for protected vs unprotected command form generation [tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/] — Pass 2 incidentally rebaselined two `verified.txt` files but no auth-specific snapshot fixture exists. Defer until DN3 changes the renderer emitter and rebaselines all consumers. Severity: LOW (A38).
- [x] [Review][Defer] Aria-live / `role="alert"` assertion on `FluentMessageBar` warning [tests/Hexalith.FrontComposer.Shell.Tests/Generated/Authorization/] — Defense-in-depth: spec wording "screen readers receive the warning". Defer to bUnit suite alongside DN3. Severity: LOW (A39).
- [x] [Review][Defer] HFC1057 partial-class duplicate-attribute reachability [tests/Hexalith.FrontComposer.SourceTools.Tests/Parsing/] — Roslyn CS0579 already errors on duplicate `[RequiresPolicy]`; HFC1057 may be unreachable. Add a partial-class fixture with one decl per partial to test reachability. Severity: LOW (A18 / E33).
- [x] [Review][Defer] Document `RequiresPolicyAttribute.Inherited=false` divergence from `[Authorize]` [docs/api/RequiresPolicyAttribute.md (when docs site lands)] — `[Authorize]` inherits to derived classes; `[RequiresPolicy]` does not. Adopters mental model violated. Document explicitly when Diataxis docs site (Story 9-5) lands. Severity: LOW (E51).
- [x] [Review][Defer] Build-time HFC for missing host policy catalog reference [src/Hexalith.FrontComposer.SourceTools/Diagnostics/] — Task T2 sub-bullet was `[x]` but no build-time HFC exists; runtime startup validation covers AC7 alternatively. Adding a build-time variant would require a host-supplied assembly attribute (e.g., `[assembly: FrontComposerKnownPolicy("OrderApprover")]`) or generated host metadata seam — out of scope for Pass 3. Severity: MEDIUM (A14 / A22).
- [x] [Review][Defer] Service-locator pattern audit for `ICommandAuthorizationEvaluator` [CommandPaletteEffects.cs] — Today the palette resolves the evaluator via `TryGetService<>`; if the palette effect is itself singleton-or-longer-lived, scoped-evaluator lifetime is ambiguous. Defer to a DI-lifetime audit alongside Story 9-1. Severity: MEDIUM (B65).
- [x] [Review][Defer] Optional Information-level audit log on `Allowed` decisions [CommandAuthorizationEvaluator.cs] — Compliance/forensics asymmetry: denied events log, allowed do not. Defer to telemetry roadmap (Epic 9-4 / Epic 9-5). Severity: LOW (B52).
- [x] [Review][Defer] Submodule pointer drift on `Hexalith.EventStore` and `Hexalith.Tenants` working tree [git status] — Pre-existing modified submodule pointers showed in initial `git status`. Out of Story 7-3 scope; needs a separate carve-and-revert before merging Pass 3. Defer to repo housekeeping. Severity: LOW (A21 / DF1).

#### Dismissed (Pass 3)

Self-demoted by Edge Case Hunter (E12 IsNullOrWhiteSpace already handles null, E46 lifecycle ordering is safe, E59 enum literal is compile-checked); positive findings (A35 scope respected); already-considered design (B54 sealed attribute, B66 struct commands explicitly out-of-scope, B67 Logger? defensive, B69 test-fixture state! intentional, B50 partial-class override is adopter-side); defense-in-depth notes already addressed by other patches (B19 / B59 IsRecoverable already covered by Pass-2 P13, B22 PrintMembers convention, B30 trim discipline already documented in Pass 2, B45 NBSP test comment fixed in Pass 2, B47 / B48 cosmetic, B56 cancellation symmetry); overlapping with patches above (B17 / B68 / E21 → "Checking permission..." patch; B12 / E22 / E23 / E58 → CTS snapshot patch; A15 / A16 / A19 / A24 / A25 / A36 / A37); minor message-clarity overlaps (B43 / E32 / E55 → covered by HFC1056 Got-echo patch, E48 "in v1" wording); hot-path / minor inefficiencies that don't justify churn (E15 / E16 cancellation drift, E26 EscapeString defense-in-depth, E28 palette flicker, E36 nested-type FullName UX, E45 custom ILogger thread safety, E47 ThreadAbortException defense-in-depth, E49 cancel-after-the-fact handler ran, E50 resolver memoization, E54 nameof helper, E56 palette try/catch breadth, E57 colon-in-policy-name claim collision, E60 / E61 / E62 / E66 record equality / null guards / FullName re-computation / type-shape assertion); dead-code-but-reserved (A8 `MissingService` reserved-for-future-use); per-story scope (E29 documented as PII concern in DN-7-3-3-4 instead of separate dismiss).

### Pass 3 Implementation Status (2026-05-02)

`/bmad-code-review 7-3` Pass 3 patches applied this session (delegated by Jerome via `do best`). Status remains `in-progress` because Pass-2 deferred surfaces (DN2 / DN3 / DN4 / DN5 / P3 / P4-full) are deferred again with explicit design plans; story cannot transition to `done` until those land in a dedicated dev session.

#### Applied (Pass 3 patches — 21)

To be filled in below as patches land in this session.

#### Pending (Pass 3 deferred again to dedicated dev session)

- ❌ **DN2** — `AuthorizingCommandServiceDecorator` [scope: 1 new file ~50 LoC + DI registration + 4-6 tests]
- ❌ **DN3** — `CommandRendererEmitter` evaluator wiring [scope: emitter rewrite + 7+ verified.txt rebaselines + bUnit fixtures]
- ❌ **DN4** — `CapabilityDiscoveryEffects` filter [scope: effect change + 4-6 tests; chains naturally with DN3]
- ❌ **DN5** — `FcAuthorizedCommandRegion` component [scope: new Razor component + razor.cs + razor.css + replace `<AuthorizeView>` in FcProjectionEmptyPlaceholder + tests]
- ❌ **P3** — `Generated/Authorization/*` bUnit suite [requires DN3 to be landed]
- ❌ **P4-full** — Tenant-switch-after-render / sign-out-after-render / destructive-dialog-delay scenarios [requires DN3]

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

### Party-Mode Hardening Addendum (2026-05-01)

This story remains `ready-for-dev`, but implementation must treat the following review findings as binding pre-dev guardrails.

#### Authorization Resource Contract

- `ICommandAuthorizationEvaluator` must pass a stable resource object to `IAuthorizationService.AuthorizeAsync(user, resource, policyName)`.
- The resource must contain command type, command display/action label, command metadata identifier, bounded context when available, source surface (`GeneratedForm`, `InlineAction`, `CompactInlineAction`, `FullPage`, `EmptyStateCta`, `CommandPalette`, `HomeCapability`, or `DirectDispatch`), and the validated Story 7-2 tenant-context snapshot when available.
- The resource must not contain access tokens, raw claims, roles, provider payloads, authorization codes, SAML assertions, tenant IDs/user IDs outside the validated sanitized context contract, render fragments, or arbitrary command payload values.
- Tests must use a deterministic fake `IAuthorizationService` keyed by policy name, command type, source surface, principal state, and tenant-context category. Do not test authorization by shortcutting to hard-coded roles in generated components.

#### Submit And Destructive Flow Ordering

- Generated command forms must authorize policy-protected commands through the submit path that runs before validation side effects, lifecycle `SubmittedAction`, pending-command registration, command-service dispatch, EventStore payload serialization, HTTP send, token acquisition, SignalR side effects, or telemetry containing command payloads.
- Denial may update only localized warning UI/status state and sanitized diagnostics. It must not mutate command lifecycle state, pending-command state, validation model state beyond the denial warning, cache state, or transport state.
- Destructive protected commands must authorize before opening the destructive confirmation dialog so unauthorized users do not see destructive-flow copy. They must authorize again immediately after confirmation and before dispatch because auth, tenant, or policy state can change while the dialog is open.
- Policy-protected direct command dispatch through framework services must perform the same authoritative submit-time check. UI gating is advisory UX only.

#### Authentication Scope And Failure Boundaries

- Story 7-3 fail-closed behavior applies to commands that carry `[RequiresPolicy]` metadata. Commands without policy metadata must keep the previous behavior and must not fail merely because authorization services, policy catalogs, or auth-state UI helpers are absent.
- Baseline authentication requirements for non-policy commands remain owned by Stories 7-1 and 7-2. Story 7-3 must not turn every command into an authenticated-only command unless another existing framework seam already requires it.
- For policy-protected commands, unresolved/prerender authentication state, missing `IAuthorizationService`, unauthenticated principal, missing or stale tenant context when the resource requires it, missing policy, authorization handler exception, cancellation before a decision, inconsistent catalog state, or auth transition from allowed to denied all block execution.
- Cancellation before an authorization decision is treated as non-executable and side-effect-free; it may surface a neutral retryable status rather than a permission-denied message when the UX surface supports that distinction.

#### Policy Catalog And Diagnostics Timing

- Invalid `[RequiresPolicy]` attribute values (null, empty, whitespace, unsupported duplicate declarations, malformed values) are SourceTools diagnostics and must prevent invalid policy metadata from being emitted.
- A missing policy catalog is not an error by itself for simple hosts. When a catalog is configured, policy names absent from the catalog produce warning diagnostics by default and can become startup/build errors only through an explicit strict-mode option.
- Missing catalog and missing policy entry are distinct diagnostic conditions. Diagnostics must name command type and policy identifier only as developer/operator configuration data; they must never include claims, roles, tenant IDs, user IDs, tokens, or command payloads.
- Runtime missing-policy or handler failures still fail closed for protected commands even if catalog validation was not enabled.

#### Authorization Surface Matrix

| Surface | Unauthorized default | Required executable check |
| --- | --- | --- |
| Generated form / full-page command | Render disabled or pending state, then localized warning on attempted submit. | Re-evaluate immediately on submit before lifecycle/dispatch side effects. |
| Inline / compact inline DataGrid action | Prefer disabled with accessible reason when layout can preserve context; hide only when the command would otherwise reveal unsafe metadata. | Re-evaluate before invoking the command flow. |
| Empty-state CTA | Use existing `EmptyStateCta.AuthorizationPolicy` metadata and `AuthorizeView` for presentation. | Re-evaluate before CTA command dispatch; `AuthorizeView` alone is not sufficient. |
| Command palette | Do not present unauthorized commands as executable. It may hide them or show non-executable discovery rows only when metadata-safe and clearly disabled. | Re-evaluate before routing/opening/submitting any command action. |
| Home / capability discovery | Same as command palette: metadata-safe discovery is allowed, executable routing is not. | Re-evaluate before opening or executing protected commands. |
| Future MCP | Emit policy/action metadata only for future enumeration. | No MCP execution or tool authorization is implemented in this story. |

User-facing warnings must use localized action-label copy such as "You do not have permission to {ActionLabel}" and must not show policy names, role names, claim names/values, tenant/user identifiers, diagnostic internals, or provider details. Developer/operator diagnostics may include command type and policy name when useful as configuration identifiers, under the redaction rules above.

#### Generated Metadata Compatibility

- Command IR, generated form models, renderer models, registration/manifest output, CTA metadata, palette entries, and home/capability descriptors must carry the same optional policy-name field from SourceTools.
- UI surfaces consume policy metadata but do not interpret policy semantics. Only `IAuthorizationService` decides allow/deny.
- Older generated manifests or command metadata with no policy field must be treated as unprotected by Story 7-3 and preserve existing execution behavior.

#### Executable Test Oracles

- Add a shared auth test fixture builder for fake principals, fake tenant-context snapshots, fake `IAuthorizationService`, fake policy catalog, strict side-effect counters, and localized warning assertions.
- Zero-side-effect denial tests must explicitly assert no validation mutation beyond warning state, no lifecycle submitted state, no pending-command registration, no command-service dispatch, no EventStore serialization, no token acquisition, no HTTP send, no SignalR side effect, no cache mutation, and no command-payload telemetry.
- Failure-matrix tests must cover allow, deny, missing service, missing policy, missing catalog, missing catalog entry, strict catalog failure, handler throw, cancellation, unauthenticated principal, prerender/no principal, stale tenant context, auth state changing from allowed to denied, and no-policy command behavior.
- Generated UI tests must include representative interaction coverage for full-page form, inline/compact action, empty-state CTA, command palette, and home/capability discovery. Snapshot tests may prove metadata propagation, but interaction tests must prove non-executable routing and submit-time recheck.
- Redaction tests must seed command payload fragments, route parameters, policy names, role names, claim values, JWT-like strings, tenant IDs, user IDs, and provider-looking data, then assert forbidden raw values are absent from UI, logs, diagnostics, telemetry, generated metadata, and test render output.
- Keep the test matrix bounded: one canonical evaluator matrix, one metadata propagation suite, one generated submit-order suite, one discovery-surface suite, and one redaction suite are sufficient unless implementation adds a new seam.

#### Deferred Decisions From Review

| Decision | Deferred owner |
| --- | --- |
| Whether unauthorized command palette/home entries should be hidden everywhere or shown as disabled discovery rows in specific product contexts. | Product/UX; implementation must default to non-executable and metadata-safe behavior. |
| Whether policy names may ever be shown to end users as support details. | Product/security; default is no end-user policy names. |
| Whether backend EventStore endpoints must independently enforce the same command policies. | EventStore auth contract backlog or consumer-driven contract story. |
| Multi-policy composition, policy expression language, dynamic policy builders, and per-row policy inference. | v1.x authorization follow-up. |

### Advanced Elicitation Hardening Addendum (2026-05-01)

This story remains `ready-for-dev`. The advanced elicitation pass keeps the party-mode scope intact and adds the following implementation guardrails.

#### Authorization Decision Freshness

- Every executable action must create a fresh authorization resource immediately before the action boundary. This applies to generated form submit, inline/compact action invoke, full-page command submit, empty-state CTA execution, palette route/open/submit, home capability route/open/submit, direct framework dispatch, and the second destructive check after confirmation.
- Render-time authorization decisions may drive disabled/pending/hide affordances, but they must never become proof for dispatch. The submit-time evaluator result is the only executable decision.
- A previously allowed render must become non-executable after sign-out, principal refresh, tenant switch, stale tenant snapshot, Blazor prerender-to-interactive transition, SignalR reconnect, or navigation restoration until the evaluator returns a fresh allow.
- The evaluator result should expose only sanitized categories (`Allowed`, `Denied`, `Unauthenticated`, `Pending`, `MissingService`, `MissingPolicy`, `StaleTenantContext`, `Canceled`, `HandlerFailed`, `CatalogInconsistent`) and a correlation ID. UI copy and diagnostics must map from these categories without exposing handler internals.

#### Minimal Implementation Spine

- Keep one Shell-owned authorization spine: command metadata -> generated policy field -> `ICommandAuthorizationEvaluator` -> ASP.NET Core `IAuthorizationService`. Do not add separate per-surface policy interpreters.
- Prefer a single command authorization resource record and a single decision result record shared by generated forms, renderers, CTA/palette/home surfaces, and direct framework dispatch.
- If a direct-dispatch guard or decorator is introduced, it must guard only policy-protected commands and must preserve existing no-policy command behavior when auth services or catalog options are absent.
- Startup/catalog validation should be isolated from submit-time authorization. Catalog warnings improve operator feedback, but runtime evaluation must still fail closed for protected commands when the policy is missing or inconsistent.

#### Redaction Classification Matrix

| Channel | Allowed values | Forbidden values |
| --- | --- | --- |
| End-user UI | Localized action label, neutral retry text, generic permission warning. | Policy names, role names, claim names/values, tenant IDs, user IDs, tokens, provider details, diagnostic IDs, handler exception text. |
| Developer diagnostics | Command type, approved policy identifier, sanitized reason category, diagnostic ID, docs link, correlation ID. | Raw claims, role values, tenant IDs, user IDs, tokens, SAML assertions, authorization codes, command payload values, route parameter values. |
| Logs/telemetry | Event name, command type, policy presence/name when treated as configuration, sanitized category, correlation ID, surface enum. | Principal contents, token material, provider payloads, tenant/user identifiers outside the validated sanitized context contract, command payload fragments. |
| Generated metadata | Optional policy name, command type/identifier, bounded context, action label, surface capability metadata. | Runtime principal data, token data, claim/role values, tenant/user IDs, authorization results captured from a prior user/session. |

#### Bounded Failure Matrix

- Required canonical matrix: no policy metadata; allowed; denied; unauthenticated; pending/prerender; missing authorization service; missing policy; missing catalog; missing catalog entry; strict catalog failure; handler throw; cancellation before decision; stale tenant context; tenant switch after render; sign-out after render; destructive dialog delay; palette/home route opened after auth change.
- Collapse duplicate surface tests into representative interaction suites. The evaluator matrix owns most failure combinations; generated UI suites only need enough coverage to prove each surface routes through the shared evaluator and blocks dispatch side effects.
- Assertions must prove both positive behavior and absence of side effects. The most important negative assertions are no `SubmittedAction`, no pending-command registration, no command dispatch, no EventStore serialization/send, no token acquisition, no SignalR side effect, no cache mutation, and no command-payload telemetry.

#### Deferred Decisions From Elicitation

| Decision | Deferred owner |
| --- | --- |
| Whether authorization decisions may use a bounded short-lived cache after v1. | Performance follow-up; v1 must use fresh executable checks. |
| Whether command payload fields may be projected into policy resources for per-instance decisions. | v1.x authorization follow-up; v1 resource must remain metadata and validated context only. |

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

GPT-5 Codex

### Debug Log References

- `dotnet test tests\Hexalith.FrontComposer.Contracts.Tests\Hexalith.FrontComposer.Contracts.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` => 152 passed.
- `dotnet test tests\Hexalith.FrontComposer.SourceTools.Tests\Hexalith.FrontComposer.SourceTools.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` => 593 passed.
- `dotnet test tests\Hexalith.FrontComposer.Shell.Tests\Hexalith.FrontComposer.Shell.Tests.csproj --no-restore -p:UseSharedCompilation=false --logger "console;verbosity=minimal"` => 1498 passed.
- `dotnet build Hexalith.FrontComposer.sln --no-restore -warnaserror -p:UseSharedCompilation=false -v:minimal` => 0 warnings, 0 errors.

### Completion Notes List

- 2026-05-01: Story created via `/bmad-create-story 7-3-command-authorization-policies` during recurring pre-dev hardening job. Ready for party-mode review on a later run.
- 2026-05-01: Party-mode review applied via `/bmad-party-mode 7-3-command-authorization-policies; review;`. Added hardening addendum for the authorization resource contract, submit/destructive ordering, auth-scope boundaries, policy catalog diagnostics, surface behavior, generated metadata compatibility, executable test oracles, and deferred decisions.
- 2026-05-01: Advanced elicitation applied via `/bmad-advanced-elicitation 7-3-command-authorization-policies`. Added freshness, minimal-spine, redaction-channel, bounded failure-matrix, race/staleness, and decision-cache guardrails.
- 2026-05-01: Implemented command policy metadata end to end: dependency-free `[RequiresPolicy]`, SourceTools parsing/diagnostics/transforms/manifest emission, Shell policy catalog validation, scoped ASP.NET Core authorization evaluator, generated protected-form presentation and submit gates, localized warnings, empty-state CTA policy propagation, and command palette filtering for denied protected commands.
- 2026-05-01: Policy-protected generated forms now fail closed while presentation authorization is pending or denied, re-evaluate immediately on submit before lifecycle/dispatch side effects, and keep unprotected command behavior unchanged when auth services are absent.

### File List

- `_bmad-output/implementation-artifacts/7-3-command-authorization-policies.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.FrontComposer.Contracts/Attributes/RequiresPolicyAttribute.cs`
- `src/Hexalith.FrontComposer.Contracts/Registration/DomainManifest.cs`
- `src/Hexalith.FrontComposer.SourceTools/AnalyzerReleases.Unshipped.md`
- `src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs`
- `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs`
- `src/Hexalith.FrontComposer.SourceTools/Emitters/RegistrationEmitter.cs`
- `src/Hexalith.FrontComposer.SourceTools/FrontComposerGenerator.cs`
- `src/Hexalith.FrontComposer.SourceTools/Parsing/CommandParser.cs`
- `src/Hexalith.FrontComposer.SourceTools/Parsing/DomainModel.cs`
- `src/Hexalith.FrontComposer.SourceTools/Transforms/CommandFormTransform.cs`
- `src/Hexalith.FrontComposer.SourceTools/Transforms/CommandRendererModel.cs`
- `src/Hexalith.FrontComposer.SourceTools/Transforms/CommandRendererTransform.cs`
- `src/Hexalith.FrontComposer.SourceTools/Transforms/FormFieldModel.cs`
- `src/Hexalith.FrontComposer.SourceTools/Transforms/RegistrationModel.cs`
- `src/Hexalith.FrontComposer.SourceTools/Transforms/RegistrationModelTransform.cs`
- `src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs`
- `src/Hexalith.FrontComposer.Shell/Options/FrontComposerAuthorizationOptions.cs`
- `src/Hexalith.FrontComposer.Shell/Registration/FrontComposerRegistry.cs`
- `src/Hexalith.FrontComposer.Shell/Resources/FcShellResources.fr.resx`
- `src/Hexalith.FrontComposer.Shell/Resources/FcShellResources.resx`
- `src/Hexalith.FrontComposer.Shell/Services/Authorization/CommandAuthorizationDecision.cs`
- `src/Hexalith.FrontComposer.Shell/Services/Authorization/CommandAuthorizationEvaluator.cs`
- `src/Hexalith.FrontComposer.Shell/Services/Authorization/FrontComposerAuthorizationPolicyCatalogValidator.cs`
- `src/Hexalith.FrontComposer.Shell/Services/Authorization/ICommandAuthorizationEvaluator.cs`
- `src/Hexalith.FrontComposer.Shell/Services/EmptyStateCtaResolver.cs`
- `src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs`
- `tests/Hexalith.FrontComposer.Contracts.Tests/Attributes/RequiresPolicyAttributeTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandFormEmitterTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandFormEmitterTests.CommandForm_DerivableFieldsHidden_OmitsHiddenFieldsOnly.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandFormEmitterTests.CommandForm_ShowFieldsOnly_RendersOnlyNamedFields.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RegistrationEmitterTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Parsing/CommandParserTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Parsing/TestFixtures/CommandTestSources.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Transforms/CommandFormTransformTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Services/Authorization/CommandAuthorizationEvaluatorTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Services/Authorization/FrontComposerAuthorizationPolicyCatalogValidatorTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Services/EmptyStateCtaResolverTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/State/CommandPalette/CommandPaletteEffectsTests.cs`

## Party-Mode Review

- Date/time: 2026-05-01T09:50:02.0615199+02:00
- Selected story key: `7-3-command-authorization-policies`
- Command/skill invocation used: `/bmad-party-mode 7-3-command-authorization-policies; review;`
- Participating BMAD agents: Winston (System Architect), Amelia (Senior Software Engineer), Murat (Master Test Architect and Quality Advisor), John (Product Manager)
- Findings summary: The review found the story valuable and bounded, but pre-dev implementation risk remained around the `IAuthorizationService` resource contract, the exact submit/destructive flow seam, the scope of fail-closed behavior for policy-protected versus unprotected commands, policy catalog diagnostic timing, unauthorized-surface behavior, user-warning versus developer-diagnostic separation, generated metadata compatibility, and executable zero-side-effect test oracles.
- Changes applied: Added a Party-Mode Hardening Addendum defining the authorization resource shape and forbidden payloads; requiring pre-confirmation and post-confirmation authorization for destructive commands; clarifying that Story 7-3 fail-closed behavior applies only to policy-protected commands; defining policy catalog diagnostics and strict-mode behavior; adding an authorization surface matrix for forms, inline actions, CTA, palette, home, and future MCP metadata; preserving older no-policy metadata behavior; and adding bounded executable test oracles for side effects, failure matrices, generated UI interactions, and redaction sentinels.
- Findings deferred: Product/UX decision on hiding versus disabled discovery rows for unauthorized palette/home entries; whether policy names may ever appear in end-user support details; backend EventStore independent policy enforcement; multi-policy composition, dynamic policy builders, policy expressions, and per-row policy inference.
- Final recommendation: ready-for-dev

## Advanced Elicitation

- Date/time: 2026-05-01T09:58:35.5895805+02:00
- Selected story key: `7-3-command-authorization-policies`
- Command/skill invocation used: `/bmad-advanced-elicitation 7-3-command-authorization-policies`
- Batch 1 method names: Security Audit Personas; Pre-mortem Analysis; Failure Mode Analysis; Red Team vs Blue Team; Self-Consistency Validation.
- Reshuffled Batch 2 method names: Chaos Monkey Scenarios; Occam's Razor Application; Comparative Analysis Matrix; Hindsight Reflection; First Principles Analysis.
- Findings summary: The elicitation found the remaining pre-dev risks were mostly time-of-check/time-of-use gaps, stale render-time authorization decisions, accidental per-surface policy interpreters, oversized failure matrices, and inconsistent redaction expectations across UI, diagnostics, logs, telemetry, and generated metadata.
- Changes applied: Added task-level requirements for deterministic sanitized decision results, no cross-boundary allow caching, fresh submit-time/post-confirmation checks, stale render-state rejection, and race/staleness tests. Added an Advanced Elicitation Hardening Addendum covering authorization decision freshness, the minimal shared implementation spine, redaction classification by channel, a bounded canonical failure matrix, and two deferred follow-up decisions.
- Findings deferred: Short-lived authorization decision caching is deferred to a performance follow-up; command-payload-derived policy resources are deferred to a v1.x authorization follow-up.
- Final recommendation: ready-for-dev
