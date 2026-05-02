I have enough context. Let me now produce the findings list.

### E1: `CommandAuthorizationDecision.Pending` factory yields `Reason = Pending` but generated form drops the warning, while caller `EvaluateAsync` returns `Reason.Pending` not `Reason.None` — palette filter rejects Pending as not-allowed, hiding commands instead of deferring
**Severity:** High
**File:** src/Hexalith.FrontComposer.Shell/Services/Authorization/CommandAuthorizationDecision.cs:91 / src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs:858
**Edge case:** AuthState is mid-transition (prerender/SSR). `EvaluateAsync` returns `Pending`. `CanSurfaceCommandAsync` returns `decision.IsAllowed` (false) → command excluded.
**Verification:** CommandAuthorizationEvaluator.cs:60 returns Pending when `state?.User is null`; CommandPaletteEffects.cs:858 returns `decision.IsAllowed`.
**Why it bites:** Users typing in the palette during the very-first interactive frame after SSR see protected commands disappear and never reappear (palette is debounced; no re-trigger on auth-state-changed). Command palette has no pending UI state.
**Suggested fix:** Treat `Pending` as transient — either retry once after AuthenticationStateProvider settles or surface the result with a "loading" hint distinct from Denied.

### E2: `KnownPolicies` is mutable `IList<string>` after `IOptions<>` binding; runtime mutation by host code goes undetected by validator
**Severity:** Medium
**File:** src/Hexalith.FrontComposer.Shell/Options/FrontComposerAuthorizationOptions.cs:19
**Edge case:** Adopter does `options.Value.KnownPolicies.Clear()` post-startup; validator already ran → silently bypasses fail-closed semantics.
**Verification:** FrontComposerAuthorizationOptions.cs:19 (`public IList<string> KnownPolicies { get; set; } = []`); validator only runs once during `StartAsync`.
**Why it bites:** Configuration is fail-closed at startup but fail-open during runtime; an adopter that uses `IOptionsMonitor` reload semantics has no re-check.
**Suggested fix:** Snapshot `KnownPolicies` to an immutable structure on bind, or document that runtime mutation has no effect; consider `IOptionsMonitor.OnChange` re-validation.

### E3: `KnownPolicies` setter accepts null and never re-initializes; null reference in validator
**Severity:** High
**File:** src/Hexalith.FrontComposer.Shell/Options/FrontComposerAuthorizationOptions.cs:19 / src/Hexalith.FrontComposer.Shell/Services/Authorization/FrontComposerAuthorizationPolicyCatalogValidator.cs:26
**Edge case:** `appsettings.json` with `"KnownPolicies": null` → IConfiguration binder leaves the property null. Validator dereferences `value.KnownPolicies.Where(...)` → NRE → host startup crashes.
**Verification:** No null-check in validator (FrontComposerAuthorizationPolicyCatalogValidator.cs:26); `IList<string> KnownPolicies { get; set; }` has no setter guard.
**Why it bites:** Adopter misconfiguration crashes startup with an opaque NRE rather than the documented behavior.
**Suggested fix:** Either guard `value.KnownPolicies ?? Enumerable.Empty<string>()` in the validator or harden the setter with `ArgumentNullException`/coalesce.

### E4: `RequiresPolicyAttribute` constructor allows policy names containing only allowed punctuation (e.g., `"-"`, `"."`, `"::"`) — passes well-formed check
**Severity:** Low
**File:** src/Hexalith.FrontComposer.Contracts/Attributes/RequiresPolicyAttribute.cs:39
**Edge case:** `[RequiresPolicy(".")]`, `[RequiresPolicy("-")]`, `[RequiresPolicy(":")]` are all accepted by `IsWellFormed` because every char is in the allow-set.
**Verification:** Loop at line 40-43 accepts any string containing only `letters/digits/.:_-`; no requirement that at least one alphanumeric appear.
**Why it bites:** Punctuation-only policy names register against ASP.NET AuthorizationOptions and silently fail with `MissingPolicy`; HFC1056 well-formedness check gives a false sense of strictness.
**Suggested fix:** Require at least one alphanumeric character (or first character must be a letter, like CLR identifier rules).

### E5: `CommandFormEmitter.SetAuthorizationWarning` ignores `Reason.Unauthenticated` — no infrastructure flag, no permission flag, but the check `infrastructureFailure` is `false` so user sees "You don't have permission" even though they're not signed in at all
**Severity:** Medium
**File:** src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs:212-218
**Edge case:** `EvaluateAsync` returns `Blocked` with `Reason.Unauthenticated`. `infrastructureFailure` evaluates to false (Unauthenticated isn't in the list). User sees "Permission required: You do not have permission to <action>" instead of a sign-in prompt or "Please sign in".
**Verification:** Generated `SetAuthorizationWarning` matches only MissingService/MissingPolicy/StaleTenantContext/HandlerFailed/Canceled/CatalogInconsistent.
**Why it bites:** UX confusion — anonymous users will see permission-denied copy and won't know they need to sign in. Story 7-1 has a separate auth-redirector seam that is bypassed here.
**Suggested fix:** Add a third copy variant for `Unauthenticated` that prompts sign-in; route through the existing IAuthRedirector seam if present.

### E6: `Reason.None` and `Reason.Denied` reach `SetAuthorizationWarning` only via the explicit `IsAllowed=false` and `!isPending` path — the helper handles every reason except `Pending` and `None`. If a caller invokes `SetAuthorizationWarning(Reason.None)` (decision.Reason for an Allowed result that somehow becomes blocked elsewhere), neither branch matches but the method still emits permission-denied copy
**Severity:** Low
**File:** src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs:209-228
**Edge case:** `Reason.None` (the default for `Allowed`) reaches `SetAuthorizationWarning` because `IsAllowed=false` filter is at the call site, not within the helper. If future refactoring decouples the two, `Reason.None` produces "You do not have permission to <button>" with the button label as `{0}`.
**Verification:** No defensive guard in `SetAuthorizationWarning`.
**Why it bites:** Silent contract violation if a future refactor lets `Reason.None` slip through.
**Suggested fix:** Add `Debug.Assert` or throw on unexpected `Reason.None`/`Reason.Pending` at entry to `SetAuthorizationWarning`.

### E7: `OnInitializedAsync` registers `AuthenticationStateChanged` BEFORE awaiting first `RefreshPresentationAuthorizationAsync` — two RefreshPresentation calls race when AuthState transitions during initialization
**Severity:** Medium
**File:** src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs:150-156
**Edge case:** AuthenticationStateProvider fires `AuthenticationStateChanged` between `+= OnAuthenticationStateChanged` (line 154) and `await RefreshPresentationAuthorizationAsync()` (line 155). Both refresh calls run; a slow handler could let the second's result land first; `_authorizationPresentationAllowed` flip-flops.
**Verification:** Emitter generates `AuthenticationStateProvider.AuthenticationStateChanged += OnAuthenticationStateChanged;` before the first refresh.
**Why it bites:** UI may briefly show stale-allowed state before settling, or the older auth state's decision wins. Submit button may flip enabled→disabled→enabled. No serialization between concurrent refresh tasks.
**Suggested fix:** Serialize refreshes through a SemaphoreSlim or a "latest result wins" guard token (compare-and-set on a sequence number).

### E8: `OnAuthenticationStateChanged` fire-and-forget via `_ = InvokeAsync(...)` swallows exceptions thrown synchronously before the lambda starts
**Severity:** Low
**File:** src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs:158-166
**Edge case:** `InvokeAsync` itself can throw if circuit is being disposed; the catch is *inside* the async lambda, not around `InvokeAsync` — the synchronous throw is unobserved.
**Verification:** Pattern `_ = InvokeAsync(async () => { try {...} catch {...} });`.
**Why it bites:** ObjectDisposedException from a torn-down `RendererSynchronizationContext` becomes UnobservedTaskException → process-level event handler may crash hosting in some configurations.
**Suggested fix:** Wrap the entire `InvokeAsync(...)` invocation, not just its inner body, in try/catch.

### E9: `RefreshPresentationAuthorizationAsync` builds new `_serverWarning` and clears it across re-runs, but `CommandFeedbackPublisher.PublishWarning` is NOT called from this code path — only the submit path publishes
**Severity:** Medium
**File:** src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs:178-182, 408-413
**Edge case:** Pre-submit denial (user opens form, evaluator returns Denied during `OnInitializedAsync`). `_serverWarning` is set locally; but `CommandFeedbackPublisher.PublishWarning` is never called → external feedback subscribers (toast, telemetry, observability sinks) receive nothing.
**Verification:** Submit path (line 408-413) calls `CommandFeedbackPublisher.PublishWarning(_serverWarning);`; refresh path (line 178-191) does not.
**Why it bites:** Inconsistent telemetry — denied attempts via the form's natural pre-render do not reach the publisher; only post-click denials do.
**Suggested fix:** Either both publish or document that pre-submit denial is local-only.

### E10: `EmptyStateCtaResolver.ResolveCommandPolicy` does last-write-wins iteration but never logs the conflict; a CTA disagrees with FrontComposerRegistry merge log because it sees the un-merged manifest list
**Severity:** Medium
**File:** src/Hexalith.FrontComposer.Shell/Services/EmptyStateCtaResolver.cs:236-250
**Edge case:** Multiple manifests with the same `BoundedContext` are merged by the registry into one (FrontComposerRegistry.cs:114-129), but `EmptyStateCtaResolver` calls `_registry.GetManifests()` returning the post-merge list. Merge has only one entry, so last-write-wins on raw manifests is moot — but if the registry implementation is custom and skips merging, the resolver's resolution silently differs from any policy lookup that goes through the merged registry.
**Verification:** EmptyStateCtaResolver:236 iterates the input `manifests`; FrontComposerRegistry:139 (`MergeCommandPolicies`) is the only place that merges.
**Why it bites:** Different implementations of `IFrontComposerRegistry` (mock, custom adopter) can produce CTA policies that don't match runtime decisions.
**Suggested fix:** Use a single canonical lookup method on `IFrontComposerRegistry` (e.g., `TryGetCommandPolicy(commandFqn)`) so all consumers go through the same path.

### E11: `EmptyStateCtaResolver.BuildCta` passes raw whitespace-trimmed policy to `EmptyStateCta` ctor — but if raw policy is e.g. `\t\n` only, `IsNullOrWhiteSpace` filter at line 244 returns true so `winning` stays null. However if value is `" OrderApprover "`, trimmed → `OrderApprover`; if it's `"\n"`, filter rejects so `winning=null` → no policy. Good — but if it's `"   "`, filter rejects. Edge case: `"OrderApprover\u200B"` (zero-width space) → not whitespace, not in well-formed character set, but the resolver doesn't validate well-formedness at runtime
**Severity:** Low
**File:** src/Hexalith.FrontComposer.Shell/Services/EmptyStateCtaResolver.cs:241-249
**Edge case:** A maliciously- or accidentally-crafted manifest declares a policy `"OrderApprover\u200B"` (zero-width space appended). The resolver passes it to `EmptyStateCta`, which only rejects `IsNullOrWhiteSpace`. Subsequent `<AuthorizeView Policy=...>` looks up the policy in ASP.NET → MissingPolicy → user sees nothing. No diagnostic.
**Verification:** EmptyStateCta:38 only checks IsNullOrWhiteSpace; no character-set guard.
**Why it bites:** Non-printing-character corruption from external sources (Excel-pasted, BOM-prefixed, etc.) silently disables CTAs without diagnostic.
**Suggested fix:** Apply the same well-formedness check to runtime policy values, or normalize via `string.IsNormalized`.

### E12: `MergeCommandPolicies` skips entries with whitespace key/value but does NOT skip entries with whitespace-only `Key`; ordinal HashSet would still keep duplicates of `" "` vs `""` in some paths
**Severity:** Low
**File:** src/Hexalith.FrontComposer.Shell/Registration/FrontComposerRegistry.cs:144
**Edge case:** Custom adopter manifest contains `CommandPolicies = {"   " => "OrderApprover"}`. Filter at 144 matches and skips correctly — OK. But if Key is `"Orders.Cmd"` and Value is `null` (boxing scenario), the dictionary types disallow null values via `IReadOnlyDictionary<string, string>`. However, a custom dict implementing `IReadOnlyDictionary<string,string?>` cast as non-nullable could deliver `null`; `IsNullOrWhiteSpace` handles null but `incomingTrimmed = pair.Value.Trim()` would then NRE. The contract is non-nullable but reflection users could construct otherwise.
**Verification:** Line 148 dereferences `pair.Value.Trim()` after the IsNullOrWhiteSpace short-circuit.
**Why it bites:** Reflection-built dictionary with null value crashes registry init via NRE in Trim despite the supposed null-guard.
**Suggested fix:** `pair.Value` is shadowed by IsNullOrWhiteSpace check, but it's safe — IsNullOrWhiteSpace returns true for null. So actually OK. **Demote to spurious.** (Verified: IsNullOrWhiteSpace handles null.)

### E13: `FrontComposerRegistry.MergeCommandPolicies` does NOT trim incoming key — `Orders.ApproveOrderCommand ` (trailing space) and `Orders.ApproveOrderCommand` are treated as distinct entries
**Severity:** Medium
**File:** src/Hexalith.FrontComposer.Shell/Registration/FrontComposerRegistry.cs:144-159
**Edge case:** Two manifests register `CommandPolicies` with one having a trailing space in the FQN key. Both entries persist; the catalog validator emits `"Orders.ApproveOrderCommand:OrderApprover"` AND `"Orders.ApproveOrderCommand :OrderApprover"` — adopter sees double-counted missing entries.
**Verification:** Line 158 stores `merged[pair.Key] = incomingTrimmed;` — only the value is trimmed.
**Why it bites:** Inconsistent trim policy between key and value; catalog validator output (line 64) `policy.Key + ":" + trimmed` carries the un-trimmed key.
**Suggested fix:** Trim the key as well, or document FQN keys must be exactly the type's FullName (which never contains whitespace).

### E14: `MergeCommandPolicies` sorts and merges using `StringComparer.Ordinal` for case-sensitive equality, but adopters may declare `[RequiresPolicy("orderapprover")]` and `[RequiresPolicy("OrderApprover")]` on different commands — both pass the well-formedness check but ASP.NET policy lookup is case-sensitive only when the policy provider uses ordinal. Documentation never warns adopters.
**Severity:** Low
**File:** src/Hexalith.FrontComposer.Shell/Registration/FrontComposerRegistry.cs:142,158
**Edge case:** Two commands declare differently-cased policy names; both register; one matches the catalog, the other reports "MissingPolicy".
**Verification:** Ordinal comparer in MergeCommandPolicies, FrontComposerAuthorizationPolicyCatalogValidator.cs:29.
**Why it bites:** Silent partial coverage — half the commands work, half don't.
**Suggested fix:** Add a startup diagnostic when two policies differ only in case.

### E15: `CommandAuthorizationEvaluator.EvaluateAsync` does NOT honor `cancellationToken.IsCancellationRequested` between AuthState resolution and tenant lookup
**Severity:** Low
**File:** src/Hexalith.FrontComposer.Shell/Services/Authorization/CommandAuthorizationEvaluator.cs:55-68
**Edge case:** `GetAuthenticationStateAsync()` returns. Caller cancels. Next line is the synchronous `tenantContextAccessor.TryGetContext` which may itself do I/O (e.g., HttpContext access in fallback chain).
**Verification:** No `cancellationToken.IsCancellationRequested` check between line 55 and line 70.
**Why it bites:** Late-cancel callers do a tenant lookup unnecessarily; minimal but observable.
**Suggested fix:** Add a `cancellationToken.IsCancellationRequested` short-circuit after auth resolution.

### E16: `EvaluateAsync` correlation ID drift — when tenant lookup fails, `tenant.CorrelationId` is returned to caller; when authorization service fails, the locally-generated `correlationId` is returned. A single submit attempt that triggers tenant invalidation followed by retry yields two unrelated correlation IDs in logs
**Severity:** Low
**File:** src/Hexalith.FrontComposer.Shell/Services/Authorization/CommandAuthorizationEvaluator.cs:84-86
**Edge case:** Stale tenant: returns `tenant.CorrelationId` (line 85); subsequent retry by caller generates a fresh `correlationId` (line 27). Operator correlating logs sees two unconnected events.
**Verification:** Different correlation sources at lines 27 vs 85.
**Why it bites:** Operator triage of fail-closed events fragments. Correlation must be stable across the lifecycle.
**Suggested fix:** Always emit local `correlationId`; include `tenant.CorrelationId` as a separate property in the log line.

### E17: `Allowed` factory accepts arbitrary `Reason`, including `Reason.Denied`, `Reason.HandlerFailed`, etc.
**Severity:** Low
**File:** src/Hexalith.FrontComposer.Shell/Services/Authorization/CommandAuthorizationDecision.cs:83-86
**Edge case:** `CommandAuthorizationDecision.Allowed(corr, Reason.Denied)` constructs an instance where `Kind=Allowed` and `Reason=Denied`. `IsAllowed` returns true, but log line shows `Reason=Denied`.
**Verification:** No validation in `Allowed` factory or constructor.
**Why it bites:** Lets buggy callers produce nonsensical decisions; passes through entire pipeline silently.
**Suggested fix:** Restrict `Allowed` factory to accept only `None` or `NoPolicy`; assert in constructor.

### E18: `Pending` factory hard-codes `Reason.Pending`; no overload to record WHY pending — caller can't distinguish "AuthState null" from "AuthState transitioning"
**Severity:** Low
**File:** src/Hexalith.FrontComposer.Shell/Services/Authorization/CommandAuthorizationDecision.cs:91-92
**Edge case:** Future evolution where Pending stems from different transient causes is blocked.
**Verification:** Pending factory takes only correlationId.
**Why it bites:** Diagnostic richness lost.
**Suggested fix:** Allow optional reason on Pending, or document the constraint.

### E19: `CommandAuthorizationRequest` record has no validation in PrintMembers — a `null` `DisplayLabel` (declared non-nullable but constructed via reflection) would NRE in `builder.Append`
**Severity:** Low
**File:** src/Hexalith.FrontComposer.Shell/Services/Authorization/CommandAuthorizationDecision.cs:51
**Edge case:** `CommandAuthorizationRequest` constructed with `DisplayLabel = null!` via reflection; `ToString` is called for logging → NRE inside `PrintMembers`.
**Verification:** No null-coalesce on `DisplayLabel` (line 51) unlike `BoundedContext` (line 50).
**Why it bites:** Custom command discovery (Epic 8 MCP enumeration referenced in the attribute docstring) could pass null; logging crashes.
**Suggested fix:** `Append(DisplayLabel ?? "<none>")` mirroring the `BoundedContext` pattern at line 50.

### E20: `CommandAuthorizationRequest` is a record but the `Command` property is typed `object?` — record equality compares Commands by reference; two requests for the same command instance equal each other but two requests for `equal` commands don't
**Severity:** Low
**File:** src/Hexalith.FrontComposer.Shell/Services/Authorization/CommandAuthorizationDecision.cs:39-45
**Edge case:** Caching or deduplication of decisions across requests fails because `Command` reference inequality breaks record equality.
**Verification:** Record default equality compares `object?` by reference unless overridden; only `PrintMembers` is overridden.
**Why it bites:** Future caching of decisions keyed on the request will silently miss valid hits.
**Suggested fix:** Document and exclude `Command` from equality (custom `Equals`/`GetHashCode`) or note that `CommandAuthorizationRequest` is not equality-safe.

### E21: `CommandAuthorizationDecisionKind.Pending` is missing from the generated form's button-disabled gate when `_authorizationPresentationReady` is false — but if Pending arrives AFTER first render, `_authorizationPresentationReady` is `true` (from prior allowed result) and `_authorizationPresentationAllowed` may still be true, so the button is enabled while we're re-evaluating
**Severity:** Medium
**File:** src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs:175-177
**Edge case:** First evaluation: Allowed → ready=true, allowed=true, button enabled. AuthState change → second evaluation in flight, returns Pending → ready=false, allowed=false. But during the in-flight period the user can click Submit. The submit handler does its own evaluation — but the gap between "auth-state-changed fires" and "RefreshPresentationAuthorizationAsync sets ready=false" is open.
**Verification:** Lines 175-177: `_authorizationPresentationReady = !isPending` is set only AFTER the await; before it, the button retains the prior render state.
**Why it bites:** Race window where user can click during transient pending; submit-time evaluation will catch it but the UX flickers and per-submit evaluation may produce a different result than the pre-render one.
**Suggested fix:** Synchronously set `_authorizationPresentationReady = false` at the start of `RefreshPresentationAuthorizationAsync` to force the button disabled during the in-flight check.

### E22: `OnInitializedAsync` emitted as `_cts ??= new CancellationTokenSource();` but `OnInitialized` does NOT initialize `_cts` — if `OnInitializedAsync` is never called (e.g., an unprotected form), `_cts` stays null until OnValidSubmitAsync. But for protected forms, OnInitializedAsync DOES run. Edge case: the `Dispose` method in line 567-569 calls `_cts?.Cancel()`, then `_cts?.Dispose()`, then `_cts = null;` — and line 158-166 `OnAuthenticationStateChanged` runs on a captured `_cts` reference indirectly through `RefreshPresentationAuthorizationAsync`'s `_cts?.Token` snapshot. If Dispose runs concurrently, the token could be disposed mid-evaluation
**Severity:** Medium
**File:** src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs:170, 567
**Edge case:** `RefreshPresentationAuthorizationAsync` reads `_cts?.Token` (line 170). Concurrent `Dispose` sets `_cts = null` and disposes it. The stored `token` value lives, but operations on it like `IsCancellationRequested` could throw `ObjectDisposedException` on the underlying CTS.
**Verification:** Line 170 grabs token snapshot; Dispose at 567 disposes underlying CTS.
**Why it bites:** Mid-flight evaluation observes ObjectDisposedException, surfaces as HandlerFailed in EvaluateAsync's recoverable catch — false positive.
**Suggested fix:** Snapshot the entire `CancellationTokenSource` and lock around token use, or accept that disposal during evaluation cancels via `IsCancellationRequested == true` (which a disposed CTS reports as well).

### E23: `_cts.Token` access pattern in OnValidSubmitAsync — line 405 reads `_cts.Token`, but if Dispose ran between CTS creation at line 386 and the await, the token's CTS was disposed. ObjectDisposedException propagates out of `EvaluateAsync` (since `IsCancellationRequested` on a disposed CTS throws)
**Severity:** Low
**File:** src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs:402-417
**Edge case:** Form is disposed between line 386 and line 403. `_cts.Token` access throws ObjectDisposedException. Existing catches don't include this.
**Verification:** `_cts` is reassigned at 386 to a fresh CTS; only Dispose disposes it. Code path 402-405 runs `EvaluateAsync(...)` with the token.
**Why it bites:** Race between disposal and authorization check produces an unhandled exception in the async submit method.
**Suggested fix:** Wrap submit's authorization check in try/catch for `ObjectDisposedException`.

### E24: `RefreshPresentationAuthorizationAsync` short-circuits `_disposed` AFTER the await but never cancels the in-flight evaluator — the evaluator continues running, hitting a disposed AuthenticationStateProvider in some hosting models (custom adopter providers may not be thread-safe)
**Severity:** Low
**File:** src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs:170-174
**Edge case:** Dispose runs while RefreshPresentationAuthorizationAsync is awaiting `EvaluateAsync`. The evaluator's nested AuthenticationStateProvider call can fail.
**Verification:** Line 174: `if (_disposed) return;` — only checks after the await; doesn't interrupt the evaluator. The cancellation token IS passed (line 173), so well-behaved providers cancel — but custom ones may not honor it.
**Why it bites:** Adopter providers that ignore cancellation tokens leak threads and CPU.
**Suggested fix:** Document the requirement that AuthenticationStateProvider implementations honor cancellation, or set a hard timeout.

### E25: Generated `CommandAuthorizationRequest` always passes `_model` as the `Command` parameter — but `_model` may be null if `OnInitialized` hasn't run yet. `CommandAuthorizationRequest` accepts `object?` so no NRE, but the resource handler can't inspect the model — silent loss of policy context
**Severity:** Low
**File:** src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs:194-201
**Edge case:** `OnInitializedAsync` runs before `OnInitialized` in some Blazor lifecycles — actually no, OnInitialized runs first synchronously. But `_model = InitialValue ?? new()` (line 139) — if `InitialValue` getter throws, `_model` is uninitialized → NRE. Not strictly a 7-3 issue but worsens with policy probing happening in `OnInitializedAsync`.
**Verification:** Line 139, 194-201.
**Why it bites:** Auth probe in OnInitializedAsync runs after OnInitialized only because Blazor sequences them; but `_model = new()` is always called for the synthesized request — so the resource handler can't see the user's actual InitialValue at probe-time, only a default-constructed instance. Resource-based auth that depends on command shape sees an empty command.
**Suggested fix:** Document that pre-render auth uses a default command instance; resource-based handlers must operate on null/default-tolerant input.

### E26: `RegistrationEmitter` policy map literal does not escape special characters in policy name — even though parser validates well-formed chars (`. : _ -` plus alnum), `EscapeString` is still called. But the parser's well-formed check rejects backslash/quote/newline so EscapeString is redundant. However if a future DiagnosticDescriptor message format changes the allowed set (or someone uses the Contracts attribute via reflection without validation), `EscapeString` is the only line of defense
**Severity:** Low
**File:** src/Hexalith.FrontComposer.SourceTools/Emitters/RegistrationEmitter.cs:77
**Edge case:** Parser path with reflective use of `[RequiresPolicy("...")]` (no Roslyn parsing) bypasses HFC1056. The Attribute constructor IS strict, so this is double-protected.
**Verification:** EscapeString call at line 77; attribute at RequiresPolicyAttribute.cs:25.
**Why it bites:** Defense in depth issue, not exploitable today.
**Suggested fix:** Document the invariant.

### E27: `FrontComposerAuthorizationPolicyCatalogValidator` strict mode fails in `StartAsync` — but since this is registered as `IHostedService` via `AddHostedService`, the throw aborts host startup with a stack-trace dump that includes the missing policy names. PII risk: policy names may contain customer data if adopters customize the well-formed regex
**Severity:** Low
**File:** src/Hexalith.FrontComposer.Shell/Services/Authorization/FrontComposerAuthorizationPolicyCatalogValidator.cs:74-77
**Edge case:** Adopter sets `StrictPolicyCatalogValidation=true` in production with policy names like `"customer-12345-approver"`. Startup fails; orchestrator (k8s) logs the exception including the missing policies → PII leak in cluster logs.
**Verification:** Line 75-77 string concat the `payload` (constructed at 73 with `policy.Key + ":" + trimmed`) into the exception message.
**Why it bites:** Customer identifiers in policy names get plastered into orchestration logs.
**Suggested fix:** Document that policy names must be PII-free, or hash the payload in the exception.

### E28: `CanSurfaceCommandAsync` in `CommandPaletteEffects` calls `evaluator.EvaluateAsync` with `CancellationToken cancellationToken` from the outer cts; but the outer token can be cancelled between `EvaluateAsync` start and result return. The result (`Blocked: Canceled`) is treated as `IsAllowed=false` → command hidden. On the next palette query, fresh evaluation runs and may show the command. UX flickers
**Severity:** Low
**File:** src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs:854-858
**Edge case:** Rapid typing in palette → debounce cancels prior queries → in-flight authorization check returns `Canceled` → command excluded for that snapshot.
**Verification:** Line 858 returns `decision.IsAllowed`. Canceled decisions are not allowed.
**Why it bites:** Functional but inelegant: cancelled evaluations leak as "denied" results; consistent with abort-and-retry but log spam can confuse operators.
**Suggested fix:** Treat `Reason.Canceled` distinctly — caller should already have aborted the dispatch; but if not, suppress logging for cancelled-by-token decisions.

### E29: `CanSurfaceCommandAsync` in palette returns `false` when `evaluator is null` — fail-closed when no service registered. But the palette itself was working pre-7-3 without this evaluator; existing adopters who don't register `ICommandAuthorizationEvaluator` (e.g., due to OOP test setups) now lose protected commands silently
**Severity:** Medium
**File:** src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs:838-840
**Edge case:** Test harness or adopter that only registers IFrontComposerRegistry without going through `ServiceCollectionExtensions.AddFrontComposer*` will not have `ICommandAuthorizationEvaluator`. Protected commands disappear.
**Verification:** Lines 838-840 hard-fail-closed.
**Why it bites:** Surfaces silently as "command palette does not show our protected command" — and the `_logger.LogWarning` for service-missing is absent (no log here, unlike line 312 for IShortcutService).
**Suggested fix:** Log when the evaluator is missing so operators know why protected commands are hidden; or surface a diagnostic ID.

### E30: `CanSurfaceCommandAsync` returns `false` when `commandType is null` (ProjectionTypeResolver can't resolve) — but the unprotected command surfaces correctly via the same loop path; only protected ones get filtered. This is asymmetric
**Severity:** Low
**File:** src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs:842-845
**Edge case:** A trimmed assembly removes a command type. Unprotected commands still surface in palette (no resolution needed); protected commands disappear because `Resolve` returns null. Users see inconsistent palette content.
**Verification:** Line 842-845 fails closed on resolution failure.
**Why it bites:** AOT/trim-mode mismatch where the same command appears or hides depending on whether a policy is declared.
**Suggested fix:** If resolution fails, surface command anyway (fail-open at this layer) or fail-close consistently for both protected and unprotected.

### E31: `ResolveCommandPolicy` in EmptyStateCtaResolver doesn't apply well-formedness check; if a malformed registry passes a malformed policy through, the EmptyStateCta constructor at IEmptyStateCtaResolver.cs:41 only rejects whitespace
**Severity:** Low
**File:** src/Hexalith.FrontComposer.Shell/Services/EmptyStateCtaResolver.cs:241-249
**Edge case:** Registry policy `"Order Approver"` (with space) — passes IsNullOrWhiteSpace, populates AuthorizationPolicy, then `<AuthorizeView Policy="Order Approver">` tries the policy provider which returns null → "No policy found" InvalidOperationException at render.
**Verification:** No well-formedness check in ResolveCommandPolicy.
**Why it bites:** Render-time crash instead of startup diagnostic.
**Suggested fix:** Apply the same well-formedness check or cross-reference the catalog.

### E32: `ParseRequiresPolicyAttribute` does not reject the attribute when `ConstructorArguments[0].Kind == TypedConstantKind.Type` (typeof(string) used as policy name) — falls through `value = null`, then hits IsNullOrWhiteSpace check, emits HFC1056. Good. But `ConstructorArguments[0].Kind == TypedConstantKind.Error` happens when `[RequiresPolicy(undefined_const)]` is used — also falls through, also caught. OK. But for `[RequiresPolicy(null)]` literal — `Kind=Primitive`, `Value=null`, `value=null`, IsNullOrWhiteSpace→true, HFC1056. OK
**Severity:** Low (documentation)
**File:** src/Hexalith.FrontComposer.SourceTools/Parsing/CommandParser.cs:548-551
**Edge case:** `[RequiresPolicy(typeof(SomeType))]` is rejected but the diagnostic message says "invalid value", unclear that a type was passed.
**Verification:** Line 559-567 message format.
**Why it bites:** Diagnostic message doesn't clarify the actual problem.
**Suggested fix:** Include the actual offending value/kind in the message for clearer diagnostics.

### E33: `ParseRequiresPolicyAttribute` triggers HFC1057 when AttributeUsage(AllowMultiple = false) should already prevent duplicates at compile time. Roslyn would normally produce CS0579 ("Duplicate attribute"). HFC1057 is therefore unreachable for CSC-compiled code; only AST-modifying generators or invalid roslyn states reach it
**Severity:** Low
**File:** src/Hexalith.FrontComposer.SourceTools/Parsing/CommandParser.cs:529-540
**Edge case:** Roslyn already errors on duplicate attributes on a class; HFC1057 is shadowed by CS0579.
**Verification:** RequiresPolicyAttribute.cs:7 declares `AllowMultiple = false`; tests only assert HFC1057 fires (line 2181-2189) but do they check that CS0579 is suppressed?
**Why it bites:** HFC1057 may never fire in real builds because Roslyn errors first; the diagnostic and unit test validate dead code.
**Suggested fix:** Validate that the parsing test compiles by tolerating CS0579 (ignoreErrorOnDuplicate flag in CompilationHelper) — otherwise the tests don't actually exercise the path.

### E34: `EvaluateAsync` resource is `CommandAuthorizationResource` with the user's tenant snapshot embedded — but `IAuthorizationService.AuthorizeAsync` in ASP.NET historically logs `ToString()` of the resource at Debug level. The record's auto-generated `ToString` includes the tenant snapshot fields (TenantId, UserId), which are PII
**Severity:** High
**File:** src/Hexalith.FrontComposer.Shell/Services/Authorization/CommandAuthorizationDecision.cs:57-63
**Edge case:** A diagnostic verbosity dial set to Debug or Trace causes ASP.NET's authorization middleware to dump the resource through the auto-generated `ToString` → tenant/user IDs leak to logs.
**Verification:** No PrintMembers override on `CommandAuthorizationResource`; record default `ToString` formats every property including `TenantContextSnapshot.TenantId` and `UserId`.
**Why it bites:** PII leak via log channel — worse than the `Command = <redacted>` mitigation on `CommandAuthorizationRequest`.
**Suggested fix:** Override `PrintMembers` on `CommandAuthorizationResource` to redact `TenantContext` (or its TenantId/UserId fields specifically).

### E35: Submit-path authorization re-check at `OnValidSubmitAsync` runs AFTER `_serverWarning = null` (line 399) and AFTER `_serverValidationMessages?.Clear()` — if EvaluateAsync throws an unrecoverable exception (recoverable filter doesn't match), the form is left in a state where prior server feedback is wiped but no new feedback shows
**Severity:** Low
**File:** src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs:399-405
**Edge case:** `EvaluateAsync` throws StackOverflowException (caught nowhere). State of submit method partial: validation cleared, no auth result, no submit dispatched.
**Verification:** Throw path bypasses lines 406-417.
**Why it bites:** Process-fatal exceptions are unrecoverable by design, so this is more of a defensive note.
**Suggested fix:** No change — process-fatal exceptions tear down the host anyway.

### E36: `RegistrationEmitter` emits `CommandPolicies` with `EscapeString` only on the value, not on the type's FullName key — but `typeof(...).FullName!` is computed at runtime; the emitter doesn't escape the source type name, relying on the type's identifier syntax. If a generic open type or `Type+Nested` form is used, `FullName` includes `+` and backticks. Catalog validator string-keys these — the registry's `policy.Key + ":" + trimmed` produces noisy entries
**Severity:** Low
**File:** src/Hexalith.FrontComposer.SourceTools/Emitters/RegistrationEmitter.cs:75-77
**Edge case:** Nested command class `OuterType.NestedCommand` produces `OuterType+NestedCommand` as `FullName`; catalog validator emits `OuterType+NestedCommand:OrderApprover` — adopter pasting the full name into appsettings as-is becomes confusing.
**Verification:** Type.FullName uses `+` for nested types.
**Why it bites:** Adopter UX — copy-paste workflow breaks subtly.
**Suggested fix:** Document that `[RequiresPolicy]` should not be applied to nested classes, or normalize FullName for catalog comparisons.

### E37: `FrontComposerAuthorizationPolicyCatalogValidator.StartAsync` returns `Task.CompletedTask` — does not honor `cancellationToken`. A graceful host shutdown during startup leaves the catalog validator running synchronously to completion
**Severity:** Low
**File:** src/Hexalith.FrontComposer.Shell/Services/Authorization/FrontComposerAuthorizationPolicyCatalogValidator.cs:21
**Edge case:** Container shutdown signal during startup; validator iterates large manifest set; no cancellation.
**Verification:** Line 21 takes `CancellationToken cancellationToken` but doesn't use it.
**Why it bites:** Slow startup cancellation — minor, but easy to add.
**Suggested fix:** Check `cancellationToken.ThrowIfCancellationRequested()` in inner loops if manifests are large.

### E38: `KnownPolicies` setter allows replacement of the IList — adopter could replace it with a thread-unsafe collection; concurrent reads during validator loop would NRE/InvalidOperationException
**Severity:** Low
**File:** src/Hexalith.FrontComposer.Shell/Options/FrontComposerAuthorizationOptions.cs:19
**Edge case:** Settor is exposed → host-side configurator does `options.KnownPolicies = customList;`. Validator iterates while host code mutates.
**Verification:** Public setter at line 19.
**Why it bites:** Race during startup if some other hosted service mutates options concurrently.
**Suggested fix:** Make `KnownPolicies` init-only or document immutability.

### E39: French `UnauthorizedCommandWarningMessage` resource `"Vous n'avez pas l'autorisation d'exécuter {0}."` — `{0}` placeholder is the `ButtonLabel` (action). The button label is English (e.g., "Approve Order"), producing French/English mixed copy: "Vous n'avez pas l'autorisation d'exécuter Approve Order."
**Severity:** Medium
**File:** src/Hexalith.FrontComposer.Shell/Resources/FcShellResources.fr.resx:521
**Edge case:** French user, English-only button labels (ButtonLabel comes from `[Display]` or compile-time class name humanization). Localized warning shows mixed-language sentence.
**Verification:** Resx line 521 + emitter line 222 passes `EscapeString(form.ButtonLabel)` as `{0}`.
**Why it bites:** Awkward UX in non-English locales; ButtonLabel is not localized at this point.
**Suggested fix:** Localize ButtonLabel before passing to the warning, or use a generic message that doesn't include the action name.

### E40: `AuthorizationActionUnavailableMessage` (FR) uses `&#160;` (non-breaking space) before the semicolon — French typographic convention. But this character entity in resx may render literally in some Blazor message-bar components if the consumer doesn't decode HTML entities
**Severity:** Low
**File:** src/Hexalith.FrontComposer.Shell/Resources/FcShellResources.fr.resx:527
**Edge case:** A consumer that displays the localized string verbatim shows `&#160;` instead of NBSP.
**Verification:** Line 527 contains `&#160;`.
**Why it bites:** UI glitch for FR users.
**Suggested fix:** Use the literal NBSP (U+00A0) directly instead of the entity reference.

### E41: `CommandPaletteEffects.HandlePaletteQueryChanged` debounces 150ms then evaluates each matching command. Each protected command triggers an async auth call. For 50 matching commands, that's 50 sequential auth checks (no `Task.WhenAll`) → palette becomes sluggish
**Severity:** High
**File:** src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs:387
**Edge case:** Many matching protected commands; each `await CanSurfaceCommandAsync` is sequential within the per-manifest loop.
**Verification:** Line 387 `await CanSurfaceCommandAsync(...)` inside the foreach; no parallelism.
**Why it bites:** Palette latency scales linearly with the number of protected commands. With 50 commands × 20ms per evaluator round-trip = 1s perceptible lag per keystroke past debounce.
**Suggested fix:** Parallelize via `Task.WhenAll` across protected commands (or batch evaluate via a future bulk API), with the same per-manifest cancellation token.

### E42: Catalog validator's exception message ordering is by ordinal-sorted `commandKey:policyName`, not just policy — operator searching for missing policy `"OrderApprover"` may scan many `Orders.X:OrderApprover` lines. Output format ambiguous
**Severity:** Low
**File:** src/Hexalith.FrontComposer.Shell/Services/Authorization/FrontComposerAuthorizationPolicyCatalogValidator.cs:73
**Edge case:** N commands all use `OrderApprover` policy that's missing → N entries `cmd1:OrderApprover|cmd2:OrderApprover|...` in output. Operator wants distinct policy list.
**Verification:** Line 73 joins all (cmd:policy) entries.
**Why it bites:** Hard to read for high-cardinality scenarios.
**Suggested fix:** Group missing policies by name with command counts, e.g., `OrderApprover (used by 12 commands)`.

### E43: `FrontComposerRegistry.MergeCommandPolicies` is an instance method (uses `_logger`), but `Clone` is static and constructs a fresh dictionary unconditionally — mutating the cloned manifest's policies post-Clone affects only the cloned copy. However, `existing.CommandPolicies` returned by `GetManifests()` IS the live merged dictionary; an external caller can downcast and mutate
**Severity:** Medium
**File:** src/Hexalith.FrontComposer.Shell/Registration/FrontComposerRegistry.cs:74, 137-138
**Edge case:** `_manifests` stores `DomainManifest` records returned by `GetManifests` (line 74) by reference. External caller does `manifest.CommandPolicies as Dictionary<string,string>` succeeds (the merged result IS a Dictionary), can mutate.
**Verification:** Line 137-138: `new Dictionary<string, string>(manifest.CommandPolicies, ...)` IS a mutable Dictionary, returned as `IReadOnlyDictionary` but downcastable.
**Why it bites:** Defensive immutability defeated by reference exposure; tests, mocks, or buggy consumers can mutate the live registry.
**Suggested fix:** Return `ImmutableDictionary` or wrap with `ReadOnlyDictionary` to enforce immutability.

### E44: `CanSurfaceCommandAsync` short-circuits when policy `IsNullOrWhiteSpace` → returns true (allowed). But `manifest.CommandPolicies.TryGetValue(commandTypeName, out string? policyName)` honors ordinal case-sensitivity. If the manifest stores `"Orders.ApproveOrderCommand"` but the palette iterates `command` from `manifest.Commands` which stores the FQN with potentially different casing (custom adopter), `TryGetValue` misses → unprotected by accident
**Severity:** Medium
**File:** src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs:832-835
**Edge case:** Adopter manually constructs a manifest with mismatched key casing in CommandPolicies vs Commands. Palette never finds the policy → command surfaces unprotected.
**Verification:** Line 832-833 uses ordinal lookup.
**Why it bites:** Silent fail-open via case mismatch in adopter manifests.
**Suggested fix:** Validate at registry-merge time that every CommandPolicies key exists in Commands.

### E45: `Logger?` in the form is nullable (line 99 emitter) — but generated code at line 415, 181, 164 uses `Logger?.LogWarning` correctly. However the `Logger?.LogWarning` in `OnAuthenticationStateChanged` lambda runs on the dispatcher thread; ILogger implementations that aren't thread-safe will corrupt state
**Severity:** Low
**File:** src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs:164
**Edge case:** Custom ILogger implementations with non-thread-safe state.
**Verification:** ILogger contract requires thread-safety; not actually a bug for compliant impls.
**Why it bites:** Adopters with non-compliant ILogger see corruption.
**Suggested fix:** None — adopter contract violation.

### E46: `RefreshPresentationAuthorizationAsync` reads `_cts?.Token ?? CancellationToken.None` — if `_cts` is null (first call before OnInitializedAsync's `??=` runs), uses `CancellationToken.None`. Permanent uncancellable evaluation if adopter overrides ComponentBase lifecycle order
**Severity:** Low
**File:** src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs:170
**Edge case:** `OnInitialized` at line 137-146 does NOT initialize `_cts`. `OnInitializedAsync` at line 150-156 initializes it BEFORE registering AuthStateChanged. So normal flow is OK. But if `OnAuthenticationStateChanged` fires synchronously during the `+=` line (some test substitutes), `_cts` could be null... actually `??= new CancellationTokenSource()` runs before `+=` so we're safe. **Demote to spurious.**
**Verification:** Line 153-154 ordering.

### E47: `EvaluateAsync` in the in-flight authorization service catch block uses `ex is not (OutOfMemoryException or StackOverflowException or ThreadAbortException or AccessViolationException)`. .NET 8+ does not actually throw `ThreadAbortException` (deprecated since .NET Core); `AccessViolationException` is uncatchable in default config. Filter is mostly OOM-only in practice
**Severity:** Low
**File:** src/Hexalith.FrontComposer.Shell/Services/Authorization/CommandAuthorizationEvaluator.cs:169-173
**Edge case:** Defense for exceptions that the runtime never raises.
**Verification:** Modern .NET runtime behavior.
**Why it bites:** Non-functional, clutter.
**Suggested fix:** Document or trim to actually-throwable exceptions.

### E48: HFC1057 message says "Expected at most one policy per command in v1" — implies forward-compatibility ("v2 may allow multiple"), but `[AttributeUsage(AllowMultiple=false)]` already forbids it at the language level. If v2 changes AllowMultiple=true, all consumers (parser, attribute reflection) break simultaneously
**Severity:** Low
**File:** src/Hexalith.FrontComposer.SourceTools/Parsing/CommandParser.cs:533
**Edge case:** Documentation hints at flexibility that doesn't exist.
**Verification:** Line 533 message vs RequiresPolicyAttribute.cs:7.
**Why it bites:** Misleading future intent.
**Suggested fix:** Drop "in v1" qualifier or clarify the framework constraint.

### E49: `EvaluateAsync` race: `cancellationToken.IsCancellationRequested` re-check at line 103 fires AFTER `AuthorizeAsync` completed. If the token was cancelled DURING the await, the result is discarded. But the handler already executed (potentially logging or side-effecting in a custom IAuthorizationHandler). Side-effects are NOT honored by the cancel-after-the-fact path
**Severity:** Medium
**File:** src/Hexalith.FrontComposer.Shell/Services/Authorization/CommandAuthorizationEvaluator.cs:96-105
**Edge case:** Custom IAuthorizationHandler logs each authorization attempt or increments a counter. The cancel-after-the-fact discards the result but keeps the side-effect; metrics show "denied" while the user never saw the result.
**Verification:** Line 103 only short-circuits the decision, not the handler execution.
**Why it bites:** Metric/log inconsistency between handler invocations and surfaced decisions.
**Suggested fix:** Document that cancellation does not unwind handler side-effects; or require handlers to honor a custom token in the resource.

### E50: `EmptyStateCtaResolver.ResolveCommandPolicy` linear scan iterates ALL manifests for every CTA build. For a registry with N manifests, BuildCta is O(N). Most CTA-render hot paths (empty grids, projections) call this on every render. Performance acceptable for small N but no caching
**Severity:** Low
**File:** src/Hexalith.FrontComposer.Shell/Services/EmptyStateCtaResolver.cs:241-249
**Edge case:** Many manifests + frequent re-renders → wasted CPU.
**Verification:** No memoization.
**Why it bites:** Minor perf; comment claims "linear scan is fine".
**Suggested fix:** Memoize per-commandFqn lookup behind a ConcurrentDictionary if the manifest set is stable.

### E51: `[RequiresPolicy("...")]` on a class with Inherited=false — derived classes do NOT inherit the policy. Two classes: `BaseCommand` with `[RequiresPolicy("Approver")]`, `DerivedCommand : BaseCommand`. Source generator emits NO policy for `DerivedCommand`. Adopter expects inheritance because conventional `[Authorize]` IS inheritable
**Severity:** Medium
**File:** src/Hexalith.FrontComposer.Contracts/Attributes/RequiresPolicyAttribute.cs:7
**Edge case:** Inheritance-based command hierarchies have unprotected derived commands.
**Verification:** `Inherited = false` at line 7; CommandParser uses `typeSymbol.GetAttributes()` which respects `Inherited`.
**Why it bites:** Adopter mental model violated; silent fail-open for derived commands.
**Suggested fix:** Either set `Inherited=true` or document the divergence from `[Authorize]`.

### E52: `FrontComposerAuthorizationPolicyCatalogValidator.StartAsync` enumerates `registry.GetManifests()` twice (once to gather declared, once to compute missing) — if the registry returns a different list between calls (some adopters refresh manifests on demand), declared vs missing diverge
**Severity:** Low
**File:** src/Hexalith.FrontComposer.Shell/Services/Authorization/FrontComposerAuthorizationPolicyCatalogValidator.cs:34, 56
**Edge case:** Custom registry refreshes manifests; second call returns superset; missing policies incorrectly classified.
**Verification:** Two enumerations.
**Why it bites:** Race in custom registries.
**Suggested fix:** Snapshot once: `var manifests = registry.GetManifests().ToList();`.

### E53: Submit-path authorization re-check evaluates with `_cts.Token` which was just allocated (line 386) — but `BeforeSubmit` callback (line 420-424) runs AFTER auth. If `BeforeSubmit` mutates `_model` (intended for derivable values), the auth check evaluated against the OLD model, not the dispatched one. Resource-based policies (e.g., approve only orders > $100) decided on stale state
**Severity:** High
**File:** src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs:402-424
**Edge case:** Resource-based policy uses `_model` properties. `BeforeSubmit` populates `Amount=$1000` (derivable). Auth ran against `Amount=0`. Approval allowed at $0 but disallowed at $1000 → command dispatches anyway because auth said yes.
**Verification:** Order: `EvaluateAsync(CreateAuthorizationRequest()` at line 403 BEFORE `BeforeSubmit()` at line 422.
**Why it bites:** Resource-based authorization is bypassed for any property populated via BeforeSubmit. A subtle order-of-operations bug that defeats the entire resource-handler contract.
**Suggested fix:** Run `BeforeSubmit` BEFORE the authorization re-check, then re-evaluate. Or document that resource-based handlers must not depend on derivable fields.

### E54: `RequiresPolicyAttribute` has no `nameof(...)` style overload — adopters can't use `RequiresPolicy(nameof(MyPolicies.OrderApprover))` because it'd be a string anyway. Refactoring rename of policy constant doesn't update the attribute usage
**Severity:** Low
**File:** src/Hexalith.FrontComposer.Contracts/Attributes/RequiresPolicyAttribute.cs:25
**Edge case:** Refactor of host-side policy constant doesn't update attribute string.
**Verification:** Constructor takes `string`.
**Why it bites:** Refactoring drift between adopter's policy definitions and command attributes.
**Suggested fix:** Document that `RequiresPolicy(nameof(MyPolicyConstants.OrderApprover))` is the recommended pattern, or add an overload accepting `Type+memberName`.

### E55: When `evaluator.EvaluateAsync` returns `Reason.MissingPolicy` for a registered-but-handler-misbehaving case (the handler raised an InvalidOperationException unrelated to "policy not found"), the user sees "Action temporarily unavailable" — same UX as "policy missing entirely". Operators can't distinguish from logs alone if they're looking at copy
**Severity:** Low
**File:** src/Hexalith.FrontComposer.Shell/Services/Authorization/CommandAuthorizationEvaluator.cs:140-153
**Edge case:** Generic InvalidOperationException from handler classified as MissingPolicy.
**Verification:** Line 140-153 maps any IOE to MissingPolicy.
**Why it bites:** Misleading classification — "MissingPolicy" reason in logs for a misbehaving registered handler.
**Suggested fix:** Match exception message ("No policy found:") to distinguish, or introduce a `HandlerInvalidOperation` reason.

### E56: `CommandPaletteEffects.HandlePaletteQueryChanged` `CanSurfaceCommandAsync` is invoked inside a `try { ... } catch (Exception ex when ex is not OOM/SOF)` (line 357-409). If `EvaluateAsync` throws, the inner catch logs and SKIPS THE ENTIRE MANIFEST — not just the protected command. All other commands in the manifest disappear too
**Severity:** Medium
**File:** src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs:357-409
**Edge case:** Auth evaluator throws for one command; entire manifest's commands and projections are lost from the palette.
**Verification:** Try/catch wraps the whole foreach over `manifest.Commands` AND `manifest.Projections`.
**Why it bites:** Single auth failure hides unrelated commands; user perceives broken palette.
**Suggested fix:** Tighten the try/catch to only the per-command auth call — log + continue with next command.

### E57: `RequiresPolicyAttribute` constructor's well-formed regex permits `':'` — but ASP.NET policy names with `':'` may collide with claim-policy syntax (`policy:claim:value` patterns in some custom providers); silent semantics drift
**Severity:** Low
**File:** src/Hexalith.FrontComposer.Contracts/Attributes/RequiresPolicyAttribute.cs:41
**Edge case:** Adopter uses `[RequiresPolicy("orders:approve")]` colliding with their custom claim-policy provider's syntax.
**Verification:** Line 41 explicitly allows `:`.
**Why it bites:** Subtle conflict with custom policy providers.
**Suggested fix:** Document the recommended naming convention (PascalCase identifiers).

### E58: `OnInitializedAsync` does NOT take a `CancellationToken` (Blazor lifecycle method has no token); generated code uses `_cts.Token` from internal state. If Blazor calls `Dispose` mid-OnInitializedAsync, `_cts` is null and the await of `RefreshPresentationAuthorizationAsync` proceeds with `CancellationToken.None`
**Severity:** Medium
**File:** src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs:148-156, 567-569
**Edge case:** Disposal during OnInitializedAsync. The `??=` at line 153 created `_cts`. Dispose disposes and nulls `_cts`. The next read at line 170 (`_cts?.Token ?? CancellationToken.None`) gets `None` → uncancellable evaluation.
**Verification:** Line 567-569 sets `_cts = null;` on dispose.
**Why it bites:** Disposed form runs an unstoppable auth probe; resources leak briefly.
**Suggested fix:** Capture the token reference once at line 153 instead of re-reading `_cts.Token`.

### E59: The Pass-2 P1 comment at CommandFormEmitterTests.cs:2120 says "surface is now a closed-set enum, not a free-form string literal." But `CommandAuthorizationSurface.GeneratedForm` is a strongly-named enum value embedded as a string literal in the generated source (line 201) — refactoring rename of the enum value silently breaks all pre-existing generated forms. There's no compile-time linkage between the generator's literal and the runtime enum
**Severity:** Low
**File:** src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs:201
**Edge case:** Renaming `CommandAuthorizationSurface.GeneratedForm` would require regeneration of all consumers.
**Verification:** Line 201 emits a string literal `CommandAuthorizationSurface.GeneratedForm` — compile-time check happens only when the regenerated source compiles.
**Why it bites:** Source generators run at compile-time so this is actually compile-checked when the generated code recompiles. **Demote.**

### E60: `CommandAuthorizationDecision` is a record with private constructor and no `with` expression support (record-with requires accessible init properties); equality is reference-only or value-based on the immutable scalar fields. Two `Allowed("X")` instances compare equal if records use value equality on `Kind/Reason/CorrelationId`. But correlationId is supposed to be unique; equality of decisions equates decisions for different requests — caller's `if (decisionA == decisionB)` in dedup logic fires falsely
**Severity:** Low
**File:** src/Hexalith.FrontComposer.Shell/Services/Authorization/CommandAuthorizationDecision.cs:65-95
**Edge case:** Two distinct correlation IDs produce records that differ only by CorrelationId — value equality differentiates them. But two same-correlation decisions equal each other. Future dedup logic that uses record equality on decisions has surprising semantics.
**Verification:** Record value-equality on all properties; CorrelationId is part of equality.
**Why it bites:** Record value-equality semantics for a class designed to be a per-request singleton are fragile.
**Suggested fix:** Document that decisions should not be compared by equality, or override Equals/GetHashCode.

### E61: `CommandAuthorizationEvaluator` constructor uses primary constructor pattern — no null guards on `IAuthorizationService`, `AuthenticationStateProvider`, `IFrontComposerTenantContextAccessor`, `ILogger<>`. DI typically prevents nulls but tests bypassing DI can pass null
**Severity:** Low
**File:** src/Hexalith.FrontComposer.Shell/Services/Authorization/CommandAuthorizationEvaluator.cs:17-21
**Edge case:** Test constructs evaluator with `null` AuthenticationStateProvider; first call NREs.
**Verification:** No `ArgumentNullException.ThrowIfNull` on constructor params.
**Why it bites:** Test failure modes are NRE rather than informative.
**Suggested fix:** Add null guards in primary constructor body.

### E62: `EvaluateAsync` log line includes `request.CommandType.FullName ?? request.CommandType.Name` repeatedly (5 places); `request.CommandType` is dereferenced 5 times despite having only one entry point. Acceptable but if a future refactor makes `CommandType` lazy or proxy, repeated computation could hit a side-effect
**Severity:** Low
**File:** src/Hexalith.FrontComposer.Shell/Services/Authorization/CommandAuthorizationEvaluator.cs:51,77,111,150,160,184
**Edge case:** Hot-path log message construction.
**Why it bites:** Minor inefficiency only.
**Suggested fix:** Capture `string commandTypeName = request.CommandType.FullName ?? request.CommandType.Name;` once.

### E63: The test `RequestRecord_RedactsCommandPayload_FromPrintMembers` asserts `text.ShouldNotContain(sentinelClaim)` but the test request never embeds a claim into the request object — the "sentinel-tenant-id-leak" string is never put in the test request, so the assertion is vacuously true. Test does not actually exercise tenant-snapshot redaction
**Severity:** Low
**File:** tests/Hexalith.FrontComposer.Shell.Tests/Services/Authorization/CommandAuthorizationEvaluatorTests.cs:1742-1757
**Edge case:** Test claims to verify principal claims aren't leaked but never injects claims into the request.
**Verification:** Test creates `CommandAuthorizationRequest(typeof(TestCommand), "OrderApprover", command, "Orders", "Approve Order", ...)` — no tenant snapshot, no claim.
**Why it bites:** Test gives false confidence; tenant snapshot/claims redaction (see E34) untested.
**Suggested fix:** Add a test that builds a `CommandAuthorizationResource` (which embeds TenantContext) and asserts its ToString redacts.

### E64: `OnAuthenticationStateChanged` runs through `InvokeAsync(async () => ...)` — this dispatcher hop happens AFTER auth state changed. If multiple state changes fire rapidly, refresh tasks queue up; no deduplication. Latest state may not be the latest decision
**Severity:** Medium
**File:** src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs:158-166
**Edge case:** Adopter AuthenticationStateProvider fires state-changed on every claim refresh (e.g., minute-by-minute token refresh). Multiple refresh tasks pile up; the order of completion is non-deterministic. The form's `_authorizationPresentationAllowed` final state may not match the latest auth state.
**Verification:** No serialization or last-write-wins guard.
**Why it bites:** Stale auth decision presentation under high state-change rates.
**Suggested fix:** Use a sequence-number guard: store latest "request id" before await; on completion, only apply if sequence matches.

### E65: `FrontComposerRegistry.MergeCommandPolicies` warning log contains the prior and new policy names. Policy names are user-supplied strings (validated for charset but otherwise free-form). If an adopter mistakenly puts a customer ID in the policy name (against documentation), the warning leaks it to logs
**Severity:** Low
**File:** src/Hexalith.FrontComposer.Shell/Registration/FrontComposerRegistry.cs:151-155
**Edge case:** Adopter mistakes; PII in logs.
**Verification:** Line 153-154 logs policy names.
**Why it bites:** Same PII concern as E27.
**Suggested fix:** Document policy name PII rules.

### E66: `EvaluateAsync` does NOT verify that `request.CommandType.IsAssignableFrom(request.Command?.GetType())` — a request can be constructed with a TypeA but a CommandB instance. Resource-based handlers checking `resource.CommandType` may behave differently than checking `resource.Command.GetType()`
**Severity:** Low
**File:** src/Hexalith.FrontComposer.Shell/Services/Authorization/CommandAuthorizationEvaluator.cs:88-94
**Edge case:** Mismatched CommandType vs Command instance; resource handler confused.
**Verification:** No type-shape assertion in EvaluateAsync.
**Why it bites:** Subtle handler bugs from caller-side typos.
**Suggested fix:** Assert type assignability in DEBUG builds.
agentId: ae57bcfa47633dc7d (use SendMessage with to: 'ae57bcfa47633dc7d' to continue this agent)
<usage>total_tokens: 185294
tool_uses: 40
duration_ms: 562112</usage>
