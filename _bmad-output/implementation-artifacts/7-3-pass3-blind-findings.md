I have read the full diff. Now I'll produce the Blind Hunter findings list.

### B1: `DomainManifest` record `CommandPolicies` property has both ctor-defaulted and init-only initializer creating ambiguity
**Severity:** Medium
**File:** src/Hexalith.FrontComposer.Contracts/Registration/DomainManifest.cs:64-76
**Evidence:**
```
public record DomainManifest(
    ...
    IReadOnlyDictionary<string, string>? CommandPolicies = null) {
    public IReadOnlyDictionary<string, string> CommandPolicies { get; init; } =
        CommandPolicies ?? new Dictionary<string, string>(StringComparer.Ordinal);
}
```
**Why it bites:** Positional record parameters generate auto-properties. Re-declaring `CommandPolicies` here both overrides the generated property (CS8907 risk) AND changes its declared type from nullable `IReadOnlyDictionary<string,string>?` to non-nullable. Users who pattern-match the record with the positional pattern will still see a nullable parameter; those who use `with { CommandPolicies = null }` will break the non-null invariant via the `init` setter accepting the ctor-typed value, then NRE downstream because the `?? new Dictionary` only runs at construction.
**Suggested fix:** Either remove the positional parameter and keep only the init-only property, or assign in a constructor body and validate in the setter (`init` should also coerce null to empty).

### B2: `with` expressions on `DomainManifest` can re-introduce null `CommandPolicies`
**Severity:** High
**File:** src/Hexalith.FrontComposer.Contracts/Registration/DomainManifest.cs:74-75
**Evidence:**
```
public IReadOnlyDictionary<string, string> CommandPolicies { get; init; } =
    CommandPolicies ?? new Dictionary<string, string>(StringComparer.Ordinal);
```
**Why it bites:** The null coalescing only runs at the explicit constructor call; `with { CommandPolicies = null! }` or any caller using `with`-expressions can set this property to null because the `init` accessor accepts the nullable parameter type via the parameter shadowing. Downstream code (`FrontComposerAuthorizationPolicyCatalogValidator`, `EmptyStateCtaResolver`, `CommandPaletteEffects.CanSurfaceCommandAsync`) calls `manifest.CommandPolicies.TryGetValue(...)` without null-check → NRE.
**Suggested fix:** Make the init setter coerce null to empty dictionary, or guard every call site.

### B3: `FrontComposerRegistry.MergeCommandPolicies` is an instance method but should be static; captures `_logger` only by accident
**Severity:** Low
**File:** src/Hexalith.FrontComposer.Shell/Registration/FrontComposerRegistry.cs:175-198
**Evidence:**
```
private IReadOnlyDictionary<string, string> MergeCommandPolicies(...)
```
**Why it bites:** Pure formatting/style observation, but co-located `Clone` is `static`. More importantly, `MergeCommandPolicies` does not validate that `incoming` keys match well-formedness; a malformed key from a hand-built manifest now silently becomes a registry policy key.
**Suggested fix:** Make method static and inject logger, or apply same well-formedness validation as the SG-time HFC1056 check.

### B4: `MergeCommandPolicies` skips entries when key OR value is whitespace, silently swallowing duplicate-key collisions
**Severity:** Medium
**File:** src/Hexalith.FrontComposer.Shell/Registration/FrontComposerRegistry.cs:179-182
**Evidence:**
```
if (string.IsNullOrWhiteSpace(pair.Key) || string.IsNullOrWhiteSpace(pair.Value)) {
    continue;
}
```
**Why it bites:** A bad/whitespace key is dropped without any log or diagnostic. If a hand-rolled manifest passes a typo like `" "` for a command type FQN, the policy is silently lost. In an authz context, fail-silent on registration is exactly the wrong direction.
**Suggested fix:** Log a warning at minimum, or throw at registration.

### B5: `MergeCommandPolicies` "last-write-wins" log uses warn-only; subsequent reload changes the auth gate without operator visibility
**Severity:** Medium
**File:** src/Hexalith.FrontComposer.Shell/Registration/FrontComposerRegistry.cs:184-192
**Evidence:**
```
if (merged.TryGetValue(pair.Key, out string? prior)
    && !string.Equals(prior, incomingTrimmed, StringComparison.Ordinal)) {
    _logger.LogWarning("...overwritten from {PriorPolicy} to {IncomingPolicy}...");
}
merged[pair.Key] = incomingTrimmed;
```
**Why it bites:** Two manifests asserting different policies for the same command FQN is an inconsistent authorization configuration. Last-write-wins picks whichever happens to register last (load-order dependent, non-deterministic with parallel registration). Operator may see the warning, but the runtime decision is already silently changed.
**Suggested fix:** Either fail registration on conflict, or pin to first-write-wins with a hard-fail under `StrictPolicyCatalogValidation`.

### B6: `EmptyStateCtaResolver.ResolveCommandPolicy` linear scan iterates ALL manifests but only the LAST hit wins; ignores the manifest passed in by `Resolve*` callers
**Severity:** High
**File:** src/Hexalith.FrontComposer.Shell/Services/EmptyStateCtaResolver.cs:693-707
**Evidence:**
```
string? winning = null;
foreach (DomainManifest manifest in manifests) {
    if (manifest.CommandPolicies.TryGetValue(commandFqn, out string? policy)
        && !string.IsNullOrWhiteSpace(policy)) {
        winning = policy.Trim();
    }
}
return winning;
```
**Why it bites:** The CTA's bounded context already pinned a specific manifest; this loop ignores that and lets manifest enumeration order decide. In multi-domain hosts where the same command FQN appears in two manifests with different policies (e.g., extension scenario), the CTA gets a different policy from the runtime registry's `MergeCommandPolicies` dictionary depending on enumeration order.
**Suggested fix:** Filter to the matching `BoundedContext` first, or pull the registry's already-merged dictionary directly rather than re-scanning.

### B7: `EmptyStateCtaResolver.ResolveCommandPolicy` semantics differ from `FrontComposerRegistry.MergeCommandPolicies`
**Severity:** Medium
**File:** src/Hexalith.FrontComposer.Shell/Services/EmptyStateCtaResolver.cs:693-707
**Evidence:** Comment claims "Mirror FrontComposerRegistry.MergeCommandPolicies last-write-wins semantics" but the registry merges in registration order; the resolver scans `manifests` enumeration order which is not guaranteed identical (e.g., when manifests come from a re-snapshot).
**Why it bites:** Comment promises parity, but parity isn't guaranteed. CTA shows policy A, runtime check uses policy B → user sees enabled CTA, then gets denied at submit.
**Suggested fix:** Have the resolver query the registry/manifest already produced by `MergeCommandPolicies`, not re-implement the merge.

### B8: Render-time auth check + `Disabled=true` in `CommandFormEmitter` — disabled-button-only "security"
**Severity:** Critical
**File:** src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs:1043-1057
**Evidence:**
```
__b.AddAttribute(cseq++, "Disabled",
    (LifecycleState... && ...)
    || !_authorizationPresentationReady
    || !_authorizationPresentationAllowed);
```
**Why it bites:** `Disabled=true` on a Blazor button is purely client-side; an attacker can DOM-poke the disabled attribute or post the form via interop. The submit-time check exists, but if a user finds a way to bypass `OnValidSubmitAsync` (e.g., custom `OnSubmit` handler invoked elsewhere), only the disabled-button stops them. The diff also reorders `BeforeSubmit` to run AFTER auth — but `BeforeSubmit` could mutate `_model` and any custom dispatch path that bypasses the generated submit handler skips auth entirely.
**Suggested fix:** Server-side authorization handler at the dispatch boundary (likely already present elsewhere); ensure render-time check is documented as UX-only.

### B9: `OnInitializedAsync` calls async auth eval before the user has interacted — wasted call and potential thrash on every form mount
**Severity:** Low
**File:** src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs:866-874
**Evidence:**
```
protected override async Task OnInitializedAsync()
{
    _cts ??= new CancellationTokenSource();
    AuthenticationStateProvider.AuthenticationStateChanged += OnAuthenticationStateChanged;
    await RefreshPresentationAuthorizationAsync().ConfigureAwait(false);
}
```
**Why it bites:** Every page navigation re-mounts the form and triggers an auth round-trip even if the user never submits. With many forms on a dashboard, this fan-out can exceed expectations.
**Suggested fix:** Lazy-evaluate (run on first focus / hover / submit attempt), or cache results across the circuit with appropriate invalidation.

### B10: `OnInitializedAsync` with `ConfigureAwait(false)` followed by `StateHasChanged` is suspicious in Blazor
**Severity:** High
**File:** src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs:873, 909
**Evidence:**
```
await RefreshPresentationAuthorizationAsync().ConfigureAwait(false);
...
await InvokeAsync(StateHasChanged).ConfigureAwait(false);
```
**Why it bites:** Blazor components require Renderer.SyncContext for state mutations. `ConfigureAwait(false)` after the initial await drops the context, and then explicitly calling `InvokeAsync(StateHasChanged)` rejoins it — which is correct PATTERN, but mutating `_serverWarning`, `_authorizationPresentationReady`, `_authorizationPresentationAllowed` BEFORE `InvokeAsync` happens off-context. If multiple auth refreshes interleave (auth-state-changed event firing during in-flight refresh), there's a race on these fields with no lock.
**Suggested fix:** Wrap the entire mutation block in `InvokeAsync` to guarantee single-threaded mutation on the renderer's sync context.

### B11: `_authorizationPresentationReady` / `_authorizationPresentationAllowed` race between `OnAuthenticationStateChanged` and `OnInitializedAsync`
**Severity:** High
**File:** src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs:876-911
**Evidence:**
```
private void OnAuthenticationStateChanged(Task<AuthenticationState> _)
{
    if (_disposed) return;
    _ = InvokeAsync(async () => { ... await RefreshPresentationAuthorizationAsync()... });
}
```
**Why it bites:** Two concurrent `RefreshPresentationAuthorizationAsync` invocations can interleave: stale completion (older request) overwrites fresh auth-state result. There's no sequence number or "in-flight" guard. User logs in → `_authorizationPresentationAllowed=true`; user logs out → both refreshes race → form may stay enabled after logout.
**Suggested fix:** Token-versioned refresh: each refresh stamps a counter; a completing refresh ignores its result if a newer counter has been issued. Or single-flight via a SemaphoreSlim.

### B12: `_cts` is null-checked with `??=` but the `_cts.Token` reference in `RefreshPresentationAuthorizationAsync` uses `_cts?.Token ?? CancellationToken.None`
**Severity:** Medium
**File:** src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs:888
**Evidence:**
```
var token = _cts?.Token ?? CancellationToken.None;
```
**Why it bites:** If `Dispose` runs after the null-check but before the await, `_cts.Token` may throw `ObjectDisposedException`. Token retrieval after Dispose is unsafe in some BCL versions; safer to capture `_cts` first.
**Suggested fix:** Capture `var cts = _cts;` once, then `cts?.Token ?? default`.

### B13: `OnAuthenticationStateChanged` swallows `OperationCanceledException` filter but rethrows EVERYTHING ELSE inside fire-and-forget
**Severity:** Medium
**File:** src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs:880-883
**Evidence:**
```
_ = InvokeAsync(async () =>
{
    try { await RefreshPresentationAuthorizationAsync().ConfigureAwait(false); }
    catch (Exception ex) when (ex is not OperationCanceledException) { Logger?.LogWarning(ex, "Refresh after auth-state-changed failed."); }
});
```
**Why it bites:** `OperationCanceledException` is intentionally NOT caught — but this is fire-and-forget; an unobserved OCE will be wrapped by the .NET unobserved-task handler. Worse, if `RefreshPresentationAuthorizationAsync` throws after Dispose, the catch logs a warning, but `_serverWarning` may have been mutated mid-flight leaving inconsistent UI.
**Suggested fix:** Swallow OCE intentionally on dispose; otherwise log and clear partial state.

### B14: `SetAuthorizationWarning` falls back to ENGLISH literals when localizer can't resolve — French users see English on infra failure
**Severity:** Medium
**File:** src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs:939-940
**Evidence:**
```
string warningTitle = ResolveAuthorizationLocalized(titleKey, infrastructureFailure ? "Action temporarily unavailable" : "You don't have permission to perform this action");
string warningBody = ResolveAuthorizationLocalized(bodyKey, infrastructureFailure ? "Please retry. If the problem persists, contact support." : "You do not have permission to {0}.", "...");
```
**Why it bites:** Hardcoded English fallback strings are baked into generated code. If `FcShellResources.fr.resx` is missing the key (e.g., during partial migration), French users receive English fallbacks. Localization "in name only".
**Suggested fix:** Throw or log loudly if `ResourceNotFound`, or fail closed with a generic translated string supplied via injection.

### B15: `SetAuthorizationWarning` overrides body's `{0}` placeholder with `EscapeString(form.ButtonLabel)` — but this is the BUTTON label, not the action label
**Severity:** Medium
**File:** src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs:940
**Evidence:**
```
string warningBody = ResolveAuthorizationLocalized(bodyKey, ..., "\"" + EscapeString(form.ButtonLabel) + "\"");
```
**Why it bites:** ButtonLabel is the verb on the Submit button (e.g., "Send"). The localized message says "You do not have permission to {0}" — interpolating "Send" produces "You do not have permission to Send", which reads as nonsense. Should be a humanized command name or display label.
**Suggested fix:** Use the command's display name or a verbalized action; ButtonLabel may be too terse.

### B16: `AuthorizationActionUnavailableMessage` resource has no `{0}` placeholder but `ResolveAuthorizationLocalized` is called with a positional arg
**Severity:** Medium
**File:** src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs:940; FcShellResources.resx:243-244
**Evidence:**
Resource: `"This action could not be authorized right now. Please retry, and contact support..."` (no `{0}`)
Code: passes `args` for both title and body.
**Why it bites:** Calling `IStringLocalizer["Key", args]` with arguments when the format string has no placeholder may pass through literally on most implementations, but `String.Format` can throw `FormatException` if the implementation eagerly formats (some custom localizers do). The diff added the comment "intentionally has no {0} placeholder" but the call site still passes args.
**Suggested fix:** Branch the args supply on `infrastructureFailure`.

### B17: `CommandAuthorizationDecision.Pending` mapped after auth-changed → form returns to "ready=false, allowed=false" silently
**Severity:** High
**File:** src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs:893-908
**Evidence:**
```
bool isPending = authorization.Kind == ...Pending;
_authorizationPresentationReady = !isPending;
_authorizationPresentationAllowed = authorization.IsAllowed;
if (!authorization.IsAllowed && !isPending)
{
    SetAuthorizationWarning(authorization.Reason);
    ...
}
else if (isPending) { _serverWarning = null; }
else { _serverWarning = null; }
```
**Why it bites:** On Pending, the button stays disabled forever if no further auth-state-changed event fires. The "ready=false, allowed=false" combination disables the button but shows NO warning to the user. They see a disabled button with no explanation. AC violation in the typical SSR-to-interactive transition.
**Suggested fix:** Show a "checking…" hint, and re-poll on a timeout.

### B18: `CommandAuthorizationEvaluator` uses `Guid.NewGuid().ToString("N")` for correlation but tenant context returns its OWN correlation id — two-id confusion
**Severity:** Low
**File:** src/Hexalith.FrontComposer.Shell/Services/Authorization/CommandAuthorizationEvaluator.cs:382, 439-440
**Evidence:**
```
string correlationId = Guid.NewGuid().ToString("N");
...
LogBlocked(request, ..., trimmedPolicy, tenant.CorrelationId);
return CommandAuthorizationDecision.Blocked(..., tenant.CorrelationId);
```
**Why it bites:** When the tenant accessor fails, the logged correlation id (`tenant.CorrelationId`) differs from the locally generated one used for other branches. Aggregating logs across one user action requires correlating two ids. The decision returned uses the tenant's id for `StaleTenantContext` only — UI presents the form a different correlation id from the one logged in the success/denied paths.
**Suggested fix:** Either always use the local correlation id and log the tenant id as a separate field, or always use the tenant id when present.

### B19: `IsRecoverable` excludes `ThreadAbortException` but not `TypeInitializationException`/`OutOfMemoryException` chains
**Severity:** Low
**File:** src/Hexalith.FrontComposer.Shell/Services/Authorization/CommandAuthorizationEvaluator.cs:524-528
**Evidence:**
```
private static bool IsRecoverable(Exception ex)
    => ex is not (OutOfMemoryException
        or StackOverflowException
        or System.Threading.ThreadAbortException
        or AccessViolationException);
```
**Why it bites:** A nested `TypeInitializationException` wrapping an OOM (e.g., static ctor failed) is "recoverable" by this check. The whole policy of catching everything except a few sentinels is inherently fragile; in a tight loop the catch will mask process-fatal issues.
**Suggested fix:** Catch concrete expected exceptions only (`AuthorizationFailedException`, `InvalidOperationException`).

### B20: `CommandAuthorizationEvaluator.AuthorizeAsync` overload doesn't accept a token; cancellation re-check logs but in-flight handler keeps running
**Severity:** Medium
**File:** src/Hexalith.FrontComposer.Shell/Services/Authorization/CommandAuthorizationEvaluator.cs:452-461
**Evidence:**
```
AuthorizationResult? result = await authorizationService.AuthorizeAsync(user, resource, resource.PolicyName).ConfigureAwait(false);
if (cancellationToken.IsCancellationRequested) { ... return ...Canceled... }
```
**Why it bites:** Comment correctly notes the AuthorizeAsync overload ignores the token. But the post-await re-check only short-circuits the result; the actual handler is still running on a thread, possibly making expensive calls (DB lookups, claims enrichment) that consume resources past user cancellation.
**Suggested fix:** Use the resource overload that accepts requirements you can pass token to, or use `Task.WhenAny(authTask, cancellationTask)` to abandon early (but still flag handler as resource leak).

### B21: `tenant.CorrelationId` propagates into `Decision.CorrelationId` without sanitization — could leak tenant-internal id format
**Severity:** Low
**File:** src/Hexalith.FrontComposer.Shell/Services/Authorization/CommandAuthorizationEvaluator.cs:439-440
**Evidence:**
```
LogBlocked(request, CommandAuthorizationReason.StaleTenantContext, trimmedPolicy, tenant.CorrelationId);
return CommandAuthorizationDecision.Blocked(CommandAuthorizationReason.StaleTenantContext, tenant.CorrelationId);
```
**Why it bites:** The decision flows back to the UI; if `tenant.CorrelationId` includes any identifying segment (some tenant accessors stamp tenant id into correlation), this leaks via the warning correlation id displayed/logged in the browser.
**Suggested fix:** Treat the decision correlation id as a server-only identifier.

### B22: `CommandAuthorizationRequest.PrintMembers` returns `true` always — `IEquatable<>`-style record print contract requires `false` if no members
**Severity:** Low
**File:** src/Hexalith.FrontComposer.Shell/Services/Authorization/CommandAuthorizationDecision.cs:299-307
**Evidence:**
```
private bool PrintMembers(StringBuilder builder) {
    builder.Append(...)...;
    return true;
}
```
**Why it bites:** Returning `true` is correct only if at least one member was appended; the convention is fine here but if a base record adds members, the SB is now corrupt with no separator. Subtle; primarily a maintenance trap.
**Suggested fix:** Document the override clearly or use the compiler-generated PrintMembers and apply `[Sensitive]` attributes per property.

### B23: `CommandAuthorizationRequest` is a `record` with mutable reference (`Command` of type `object?`) — equality is by reference, hash unstable across Garbage Collection
**Severity:** Medium
**File:** src/Hexalith.FrontComposer.Shell/Services/Authorization/CommandAuthorizationDecision.cs:292-308
**Evidence:**
```
public sealed record CommandAuthorizationRequest(
    Type CommandType,
    string? PolicyName,
    object? Command,
    ...);
```
**Why it bites:** Records auto-generate Equals/GetHashCode using all members. `Command` is `object?` typed — its hash uses the `Command`'s GetHashCode, which for mutable command DTOs can change after construction. If anyone adds a memoization dictionary keyed on this request, the hash will be inconsistent.
**Suggested fix:** Override Equals/GetHashCode to exclude `Command` (which is also redacted), or document non-cacheability.

### B24: `CommandAuthorizationResource.TenantContext` is a `record` field — leaking `TenantContextSnapshot` (likely contains tenant id, user id) into `IAuthorizationHandler`
**Severity:** High
**File:** src/Hexalith.FrontComposer.Shell/Services/Authorization/CommandAuthorizationDecision.cs:310-316
**Evidence:**
```
public sealed record CommandAuthorizationResource(
    Type CommandType,
    string PolicyName,
    string? BoundedContext,
    string DisplayLabel,
    CommandAuthorizationSurface SourceSurface,
    TenantContextSnapshot? TenantContext);
```
**Why it bites:** Records auto-print all members. Any logging in an authorization handler that does `logger.LogInformation("Auth resource: {Resource}", resource)` will dump the tenant snapshot — defeating the redaction work done on `CommandAuthorizationRequest`.
**Suggested fix:** Override `PrintMembers` here too, or annotate `TenantContext` for redaction.

### B25: `CommandAuthorizationDecision.Allowed` accepts `reason` parameter but caller uses positional `correlationId` only — second positional `reason` defaults to `None`
**Severity:** Low
**File:** src/Hexalith.FrontComposer.Shell/Services/Authorization/CommandAuthorizationDecision.cs:336-339; CommandAuthorizationEvaluator.cs:472-473
**Evidence:**
```
public static CommandAuthorizationDecision Allowed(string correlationId, CommandAuthorizationReason reason = ...None) => ...

if (result.Succeeded) { return CommandAuthorizationDecision.Allowed(correlationId); }
```
**Why it bites:** Callers cannot distinguish "policy-required-and-allowed" from "no-policy-allowed" by looking at the result. The Pass-2 reasons (NoPolicy) only flow through one path. A consumer relying on the reason to produce telemetry will mis-bucket allowed-with-policy as `None`.
**Suggested fix:** Pass `CommandAuthorizationReason.Allowed` (or similar) explicitly in the success path.

### B26: `FrontComposerAuthorizationPolicyCatalogValidator` builds `payload` from `policy.Key + ":" + policy.Value` — policy value can contain `:` legally per HFC1056 well-formedness rules
**Severity:** Low
**File:** src/Hexalith.FrontComposer.Shell/Services/Authorization/FrontComposerAuthorizationPolicyCatalogValidator.cs:611
**Evidence:**
```
_ = missing.Add(policy.Key + ":" + trimmed);
```
**Why it bites:** Policy names allow `:` (per `RequiresPolicyAttribute.IsWellFormed`). So the encoded payload `"My.Command:Foo:Bar"` is ambiguous when parsed back. Operators who try to programmatically split the warning will mis-parse.
**Suggested fix:** Use a separator that is not in the allowed character set (e.g., `' '` or `'|'` already at the join — but the inner separator is the problem).

### B27: Catalog validator's `payload` string can grow unboundedly for huge mismatches and includes command FQN — log line truncation may hide entries
**Severity:** Low
**File:** src/Hexalith.FrontComposer.Shell/Services/Authorization/FrontComposerAuthorizationPolicyCatalogValidator.cs:620
**Evidence:**
```
string payload = string.Join("|", missing.OrderBy(static x => x, StringComparer.Ordinal));
```
**Why it bites:** Most logging sinks have an ~8KB hard limit. With dozens of missing policies (plausible during a big migration), entries past the limit are dropped. Operators investigating the warning won't see the full set.
**Suggested fix:** Log each missing entry on its own line, or paginate.

### B28: Catalog validator "no catalog configured" path bypasses validation when `KnownPolicies.Count==0` even though policies are declared — fails OPEN by default
**Severity:** High
**File:** src/Hexalith.FrontComposer.Shell/Services/Authorization/FrontComposerAuthorizationPolicyCatalogValidator.cs:591-599
**Evidence:**
```
if (catalog.Count == 0) {
    if (declaredPolicies.Count > 0) { logger.LogInformation("...empty but {DeclaredPolicyCount} command(s) declare policies..."); }
    return Task.CompletedTask;
}
```
**Why it bites:** A host that forgets to populate `KnownPolicies` gets only an INFORMATION log; startup proceeds, the runtime evaluator hits "No policy found" → `MissingPolicy` → fail-closed at runtime. But the security-critical "you forgot to wire authorization" condition is buried at Information level. Many production logging configs filter out Information.
**Suggested fix:** Log warning here too, especially when `StrictPolicyCatalogValidation=true`.

### B29: `RequiresPolicyAttribute.IsWellFormed` ignores Unicode normalization — `'O'` (U+006F) vs Cyrillic `'о'` (U+043E) both pass `char.IsLetterOrDigit` but compare unequal Ordinal
**Severity:** Medium
**File:** src/Hexalith.FrontComposer.Contracts/Attributes/RequiresPolicyAttribute.cs:45-53
**Evidence:**
```
private static bool IsWellFormed(string value) {
    foreach (char c in value) {
        if (!(char.IsLetterOrDigit(c) || c is '.' or ':' or '_' or '-')) { return false; }
    }
    return true;
}
```
**Why it bites:** Homoglyph attack surface: a misregistered policy with a Cyrillic 'O' looks identical to the Latin one in source. The attribute accepts it, the catalog validator compares ordinally → looks valid; but ASP.NET Core's `AddPolicy("OrderApprover")` registers the Latin form. Result: silently mis-routed authorization where attacker-controlled commands appear protected but route to a different (perhaps unprotected) policy.
**Suggested fix:** Restrict to ASCII alphanumerics or apply NFKC normalization before storing.

### B30: `RequiresPolicyAttribute` constructor `Trim()`s but stores trimmed value; reflection-based reads via `attribute.PolicyName` look fine, but the `ConstructorArguments` raw value still contains spaces
**Severity:** Low
**File:** src/Hexalith.FrontComposer.Contracts/Attributes/RequiresPolicyAttribute.cs:30-37; SourceTools/Parsing/CommandParser.cs:1186, 1204
**Evidence:** Two trim sites: attribute and parser. If a future tool uses `AttributeData.ConstructorArguments[0].Value` directly (without `.Trim()`), it gets the raw padded string. This already happened during the parse-result vs runtime comparison.
**Why it bites:** Mismatched trim discipline → catalog says `"OrderApprover"`, parser sees `" OrderApprover "`, `IsPolicyNameWellFormed` returns true (whitespace trimmed internally), but well-formedness check uses the trimmed comparison while the original `value` is used for `string.IsNullOrWhiteSpace` only. The returned `value!.Trim()` is correct, but reflection-based inspection elsewhere may not.
**Suggested fix:** Document trim contract centrally; add a helper `NormalizePolicyName(string)` used by all sites.

### B31: `CommandPaletteEffects.CanSurfaceCommandAsync` returns `false` when evaluator is missing — fails CLOSED but loops over many commands awaiting one round-trip per command
**Severity:** High
**File:** src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs:737-765
**Evidence:**
```
foreach (...) {
    if (!await CanSurfaceCommandAsync(manifest, command, cts.Token).ConfigureAwait(false)) { continue; }
    ...
}
```
**Why it bites:** N protected commands × M manifests × per-command `EvaluateAsync` round-trip = serial latency on every palette query. Each `EvaluateAsync` calls `GetAuthenticationStateAsync` and the tenant accessor (often a no-op cached call but not free). For a large catalog and slow auth handlers, palette becomes unusable.
**Suggested fix:** Batch-evaluate via a parallel `Task.WhenAll` or cache decisions per (user, policy) tuple for the palette session.

### B32: `CommandPaletteEffects.CanSurfaceCommandAsync` falls back to `false` when `commandType` is null — drops the command silently
**Severity:** Medium
**File:** src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs:748-751
**Evidence:**
```
Type? commandType = ProjectionTypeResolver.Resolve(commandTypeName);
if (commandType is null) { return false; }
```
**Why it bites:** Cannot resolve type → command is silently filtered with no telemetry. Operator has no way to know that a manifested command isn't surfacing. Could mask a deployment regression where assemblies are rebuilt and command types removed.
**Suggested fix:** Log a warning once per session per missing type.

### B33: Generated form's `OnValidSubmitAsync` reorders `BeforeSubmit` AFTER `_serverValidationMessages?.Clear()` — breaks API contract for callers
**Severity:** High
**File:** src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs:973-1009
**Evidence:**
```
- if (BeforeSubmit is not null) { await BeforeSubmit().ConfigureAwait(false); ... }
+ // ... clear validation messages ...
+ // ... auth check ...
+ if (BeforeSubmit is not null) { await BeforeSubmit().ConfigureAwait(false); ... }
```
**Why it bites:** Existing adopters relied on `BeforeSubmit` running BEFORE validation-message clearing — they could detect "previous error state" inside BeforeSubmit. Now BeforeSubmit runs after `_serverValidationMessages?.Clear()` and after auth, changing semantics. Verified files are updated to match, but existing third-party `BeforeSubmit` handlers will see different state.
**Suggested fix:** Document as breaking change; provide a migration path or revert the reorder.

### B34: Auth check runs BEFORE `BeforeSubmit` — if `BeforeSubmit` mutates `_model` (intended use case for derived field calculation), auth was evaluated against pre-mutation model
**Severity:** High
**File:** src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs:987-1008
**Evidence:**
```
var authorization = await CommandAuthorizationEvaluator.EvaluateAsync(CreateAuthorizationRequest(), _cts.Token).ConfigureAwait(false);
...
if (BeforeSubmit is not null) { await BeforeSubmit().ConfigureAwait(false); ... }
```
**Why it bites:** Resource-based authorization handlers receive `CommandAuthorizationResource` which holds the command (`_model`). If `BeforeSubmit` populates fields used by the auth policy (e.g., `OrderId` derived from URL), the policy gets stale/empty values, fails closed, and the user sees "permission denied" while the actual data was about to be valid. Inverse is worse: auth allows based on empty model, then `BeforeSubmit` populates risky data, then dispatch goes through.
**Suggested fix:** Either run `BeforeSubmit` first (back to pre-diff order) or document that `BeforeSubmit` cannot affect authorization-relevant state.

### B35: `ResolveAuthorizationLocalized` accesses `localized.ResourceNotFound` after potentially formatting — exception in formatting hides the resolved fallback
**Severity:** Low
**File:** src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs:921-925
**Evidence:**
```
var localized = args is { Length: > 0 } ? CommandAuthorizationLocalizer[key, args] : CommandAuthorizationLocalizer[key];
return localized.ResourceNotFound ? fallback : localized.Value;
```
**Why it bites:** If the resource exists but `string.Format` throws (mismatched placeholders, e.g., `{0}` referenced but no arg), the indexer throws before `ResourceNotFound` is checked. The exception bubbles and the form transitions to broken UI state.
**Suggested fix:** Wrap in try/catch and return `fallback` on `FormatException`.

### B36: `ResolveAuthorizationLocalized` does not handle `LocalizedString` being null on some custom IStringLocalizer implementations
**Severity:** Low
**File:** src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs:921-925
**Evidence:** `localized.ResourceNotFound` accessed without null-check.
**Why it bites:** Default `IStringLocalizer` implementations always return a `LocalizedString`; some custom DI overrides may return null on unknown keys, NRE.
**Suggested fix:** `localized?.ResourceNotFound ?? true ? fallback : localized.Value`.

### B37: `CommandAuthorizationDecision` records have no explicit `Equals` — `CorrelationId` is part of value equality
**Severity:** Low
**File:** src/Hexalith.FrontComposer.Shell/Services/Authorization/CommandAuthorizationDecision.cs:318-349
**Evidence:** `public sealed record CommandAuthorizationDecision { ... }` with `CorrelationId` as a property.
**Why it bites:** Records auto-include all properties in equality. Two "Denied" decisions with different correlation ids compare unequal — fine for diagnostics, but if any test asserts via `.Should().Be(...)` on a precomputed decision it'll fail.
**Suggested fix:** Document or override.

### B38: `RegistrationEmitter` emits `EscapeString(model.AuthorizationPolicyName!)` but the model type is `string?` — `!` masks NRE risk
**Severity:** Medium
**File:** src/Hexalith.FrontComposer.SourceTools/Emitters/RegistrationEmitter.cs:1077
**Evidence:**
```
string policyMap = hasPolicyMap
    ? "new Dictionary<string, string> { [typeof(" + model.TypeName + ").FullName!] = \"" + EscapeString(model.AuthorizationPolicyName!) + "\" }"
    : string.Empty;
```
**Why it bites:** `hasPolicyMap` is computed from `IsCommand && !IsNullOrWhiteSpace(...)` — but a whitespace-only `AuthorizationPolicyName` would be treated as `hasPolicyMap=false`. If a future change rephrases this check (e.g., to `!= null`), `EscapeString` would receive whitespace and emit `Dictionary<...> { [...] = "   " }`. The catalog then has whitespace policy values that pass the registry's null-or-whitespace check on read, are skipped, and produce no auth check at all → fail OPEN.
**Suggested fix:** Validate `model.AuthorizationPolicyName` is the trimmed/well-formed value at this point or assert.

### B39: `RegistrationEmitter` doesn't `EscapeString` the dictionary key (`typeof(...).FullName!`) — TypeName never escaped
**Severity:** Medium
**File:** src/Hexalith.FrontComposer.SourceTools/Emitters/RegistrationEmitter.cs:1077
**Evidence:**
```
"new Dictionary<string, string> { [typeof(" + model.TypeName + ").FullName!] = ...}"
```
**Why it bites:** `model.TypeName` is interpolated into a C# expression. If `TypeName` contains a backtick (legal — generic types like `Foo`1`) or a quote (illegal but defensive), the emitted code is malformed. The diff doesn't add validation that `TypeName` is a single non-generic identifier.
**Suggested fix:** Validate identifier early in the parser pipeline.

### B40: `CommandParser.ParseRequiresPolicyAttribute` short-circuits on diagnostics list scan with `O(N)` per attribute
**Severity:** Low
**File:** src/Hexalith.FrontComposer.SourceTools/Parsing/CommandParser.cs:1124-1128
**Evidence:**
```
string? authorizationPolicyName = ParseRequiresPolicyAttribute(typeSymbol, diagnostics, filePath, linePos);
if (diagnostics.Any(d => d.Id is "HFC1056" or "HFC1057")) {
    return new CommandParseResult(null, new EquatableArray<DiagnosticInfo>([.. diagnostics]));
}
```
**Why it bites:** Linear scan of all diagnostics on every command. Fine for now; bad pattern for SG hot paths.
**Suggested fix:** Have `ParseRequiresPolicyAttribute` return a tri-state (success | empty | failed) instead of scanning the list.

### B41: `CommandParser.ParseRequiresPolicyAttribute` uses `policyAttribute.ApplicationSyntaxReference?.GetSyntax()` cast to `AttributeSyntax` — silent null on cast failure
**Severity:** Low
**File:** src/Hexalith.FrontComposer.SourceTools/Parsing/CommandParser.cs:1187-1190
**Evidence:**
```
Microsoft.CodeAnalysis.Text.LinePosition attrLine = (policyAttribute.ApplicationSyntaxReference?.GetSyntax() as AttributeSyntax)
    ?.GetLocation()
    .GetLineSpan()
    .StartLinePosition ?? linePos;
```
**Why it bites:** If reference's syntax is some other shape (e.g., `AttributeListSyntax`), the diagnostic falls back to the type declaration's `linePos` — operator sees the diagnostic at the wrong line. Mostly cosmetic but subtle.
**Suggested fix:** Use `policyAttribute.GetLocation()` directly.

### B42: `CommandParser.ParseRequiresPolicyAttribute` doesn't handle `attr.AttributeClass?.ToDisplayString()` returning a different format (e.g., with global:: prefix or namespace alias)
**Severity:** Medium
**File:** src/Hexalith.FrontComposer.SourceTools/Parsing/CommandParser.cs:1152-1156
**Evidence:**
```
foreach (AttributeData attr in typeSymbol.GetAttributes()) {
    if (attr.AttributeClass?.ToDisplayString() == RequiresPolicyAttributeName) {
        attributes.Add(attr);
    }
}
```
**Why it bites:** `ToDisplayString()` default format may include namespaces; alias usage (`using Pol = ...RequiresPolicyAttribute;`) might display as the alias or the full name depending on Roslyn version. A user who imports the attribute under an alias may not be detected, silently skipping authorization metadata.
**Suggested fix:** Compare via `SymbolEqualityComparer` against the attribute symbol from the compilation, or check `ContainingNamespace` and `Name`.

### B43: `CommandParser` accepts attributes attached via type alias but discards them silently when `ConstructorArguments[0].Kind != Primitive`
**Severity:** Medium
**File:** src/Hexalith.FrontComposer.SourceTools/Parsing/CommandParser.cs:1175-1184
**Evidence:**
```
if (policyAttribute.ConstructorArguments.Length > 0) {
    TypedConstant ctorArg = policyAttribute.ConstructorArguments[0];
    if (ctorArg.Kind == TypedConstantKind.Primitive) {
        value = ctorArg.Value as string;
    }
}
if (string.IsNullOrWhiteSpace(value) || ...) { /* HFC1056 */ }
```
**Why it bites:** The diagnostic is raised for "string-cast-failed → value is null → IsNullOrWhiteSpace true → HFC1056." Comment claims "Defensive: typeof(...) / array constants surface as non-Primitive Kinds." But the EMPTY constructor case (`ConstructorArguments.Length == 0`) doesn't raise HFC1056 — `value` stays null, `IsNullOrWhiteSpace(null)` is true, so it does raise. OK, but the failure message says "Expected a non-empty policy name" which is misleading when the user actually tried `[RequiresPolicy(typeof(SomeType))]` (a typeof). The diagnostic is correct, the message is wrong.
**Suggested fix:** Differentiate "missing string arg" from "wrong arg type".

### B44: Test for `EvaluateAsync_ProtectedCommand_Allowed_PassesResourceWithTenantContext` asserts `captured.PolicyName.ShouldBe("OrderApprover")` but evaluator passes `trimmedPolicy`; test fixture uses unpadded value
**Severity:** Low
**File:** tests/Hexalith.FrontComposer.Shell.Tests/Services/Authorization/CommandAuthorizationEvaluatorTests.cs:1582-1597
**Evidence:** Test uses `Request("OrderApprover")` — no whitespace. Doesn't actually exercise the trim path through the evaluator.
**Why it bites:** Trim regression is uncovered by tests.
**Suggested fix:** Add a test with `Request(" OrderApprover ")` to assert the resource carries trimmed.

### B45: Test `EvaluateAsync_WhitespacePolicy_TreatedAsNoPolicy` comment says "NBSP-only" but test uses regular space
**Severity:** Low
**File:** tests/Hexalith.FrontComposer.Shell.Tests/Services/Authorization/CommandAuthorizationEvaluatorTests.cs:1556-1561
**Evidence:**
```
// NBSP-only is treated as whitespace and short-circuits to NoPolicy.
CommandAuthorizationDecision result = await sut.EvaluateAsync(Request(" "), ...);
```
**Why it bites:** Comment promises NBSP coverage; regular space is what's tested. `string.IsNullOrWhiteSpace` does treat NBSP as whitespace, but the test does NOT prove it. False sense of coverage.
**Suggested fix:** Use `"\u00A0"` literal.

### B46: Test `EvaluateAsync_AuthorizationService_ReturnsFailedWithEmptyRequirements_MapsToMissingPolicy` relies on internal `AuthorizationFailure.Failed(emptyRequirements)` semantics that may change between ASP.NET versions
**Severity:** Medium
**File:** tests/Hexalith.FrontComposer.Shell.Tests/Services/Authorization/CommandAuthorizationEvaluatorTests.cs:1639-1655
**Evidence:**
```
.Returns(Task.FromResult(AuthorizationResult.Failed(AuthorizationFailure.Failed(Array.Empty<IAuthorizationRequirement>()))));
```
**Why it bites:** `AuthorizationFailure.Failed(IEnumerable<IAuthorizationRequirement>)` with empty collection sets `FailedRequirements=[]` and `FailCalled=false`. If a future Microsoft change makes this set `FailCalled=true`, the test still passes (because Denied also returns FailedClosed-ish, but with different reason), test no longer covers the MissingPolicy branch.
**Suggested fix:** Construct an `AuthorizationFailure` with explicit known internal state via reflection, or document this as fragile.

### B47: `CanSurfaceCommandAsync` short-circuits with `cts.Token` but uses `cancellationToken` parameter — variable shadow
**Severity:** Low
**File:** src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs:726, 762
**Evidence:**
```
if (!await CanSurfaceCommandAsync(manifest, command, cts.Token).ConfigureAwait(false)) { continue; }
...
private async Task<bool> CanSurfaceCommandAsync(DomainManifest manifest, string commandTypeName, CancellationToken cancellationToken) { ... evaluator.EvaluateAsync(..., cancellationToken)... }
```
**Why it bites:** Caller passes `cts.Token`, callee parameter is `cancellationToken`. Fine, just confirms naming. But the caller's `cts` is the palette's own CTS — shared between many `CanSurfaceCommandAsync` calls. Cancelling the palette aborts all in-flight auth evals; on cancel mid-loop, partial filtering produces non-deterministic palette results.
**Suggested fix:** On cancellation, dispatch nothing rather than partial results.

### B48: `CommandFormEmitter` emits `private const string? BoundedContextName = ...` with explicit `?` on `const string?` — unusual; some compiler versions reject `const string?`
**Severity:** Medium
**File:** src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs:857
**Evidence:**
```
_ = sb.AppendLine("    private const string? BoundedContextName = " + boundedContextLiteral + ";");
```
**Why it bites:** `const string?` is valid in C# 10+ but the nullable annotation on `const` is essentially decorative. If the project targets a lower C# version, this fails to compile in adopter projects. Also, `null` const value cannot have `?` redundantly in some analyzer rule sets.
**Suggested fix:** Use `private const string BoundedContextName = ...` and accept that `null` literal compiles for plain `string`.

### B49: `OnAuthenticationStateChanged` discards the task `Task<AuthenticationState>` parameter without awaiting — if the new state task completes after `RefreshPresentationAuthorizationAsync` reads `GetAuthenticationStateAsync()`, race
**Severity:** High
**File:** src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs:876-884
**Evidence:**
```
private void OnAuthenticationStateChanged(Task<AuthenticationState> _)
{
    if (_disposed) return;
    _ = InvokeAsync(async () =>
    {
        try { await RefreshPresentationAuthorizationAsync().ConfigureAwait(false); }
        ...
    });
}
```
**Why it bites:** `RefreshPresentationAuthorizationAsync` calls `evaluator.EvaluateAsync` which calls `authenticationStateProvider.GetAuthenticationStateAsync()`. The change-notification task `_` is the NEW state, but the evaluator may end up reading the OLD cached state (depending on AuthenticationStateProvider implementation). Effectively, the auth-state-changed event handler doesn't actually use the new state.
**Suggested fix:** Pass the task forward into the evaluation, or await it before refresh.

### B50: Generated form's `OnInitializedAsync` creates `_cts` but no `OnInitializedAsync` was previously generated — overrides `OnInitializedAsync` from parent? Conflict?
**Severity:** Medium
**File:** src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs:866-874
**Evidence:**
```
_ = sb.AppendLine("    /// <inheritdoc />");
_ = sb.AppendLine("    protected override async Task OnInitializedAsync()");
```
**Why it bites:** Only emitted when `hasAuthorizationPolicy` is true. The non-protected branch has no `OnInitializedAsync`. If the user-supplied partial class also overrides `OnInitializedAsync`, this is a duplicate-override compile error. The diff doesn't gate on whether the user's partial class already has one.
**Suggested fix:** Detect partial class member presence (hard at SG time) or use `OnAfterRender`/`OnParametersSetAsync` instead.

### B51: `LogBlocked` parameter `trimmedPolicy` is non-nullable but the call from the InvalidOperationException path passes the trimmed value — fine, but not all `LogBlocked` callers pass a real policy
**Severity:** Low
**File:** src/Hexalith.FrontComposer.Shell/Services/Authorization/CommandAuthorizationEvaluator.cs:530-541
**Evidence:** The `LogBlocked` signature requires `string trimmedPolicy`, but `trimmedPolicy` may be empty string in code paths that reached this point — only the early-return path uses null/empty.
**Why it bites:** Future caller might pass empty string and the operator sees `PolicyName=""` in logs, less informative than `<unspecified>`.
**Suggested fix:** Handle empty in the log template or change to nullable.

### B52: `CommandAuthorizationEvaluator` does not log on the SUCCESS path — denied actions are auditable, allowed actions are not
**Severity:** Medium
**File:** src/Hexalith.FrontComposer.Shell/Services/Authorization/CommandAuthorizationEvaluator.cs:472-473
**Evidence:**
```
if (result.Succeeded) { return CommandAuthorizationDecision.Allowed(correlationId); }
```
**Why it bites:** Audit logging is asymmetric. Compliance-relevant actions (financial/medical) usually require successful authorization to be logged for forensics. Pass-2 may have removed this intentionally; just a callout.
**Suggested fix:** Optional Information-level log on success or rely on downstream command bus telemetry.

### B53: `EmptyStateCtaResolver.BuildCta` now takes `manifests` parameter on every call — the field is already a member of the resolver via constructor injection (likely)
**Severity:** Low
**File:** src/Hexalith.FrontComposer.Shell/Services/EmptyStateCtaResolver.cs:674-690
**Evidence:**
```
private EmptyStateCta? BuildCta(string boundedContext, string commandFqn, IReadOnlyList<DomainManifest> manifests) {
```
**Why it bites:** Threading `manifests` through the call stack increases coupling. If the caller is iterating with a stale snapshot vs. the registry's live list, behavior diverges.
**Suggested fix:** Inject the registry directly into `BuildCta` or capture the snapshot once.

### B54: `RequiresPolicyAttribute` is `sealed` — adopters cannot subclass to add domain-specific metadata; not a bug, design constraint without explanation
**Severity:** Low
**File:** src/Hexalith.FrontComposer.Contracts/Attributes/RequiresPolicyAttribute.cs:14
**Evidence:** `public sealed class RequiresPolicyAttribute : Attribute`
**Why it bites:** Can't extend; if Pass-3 review wants to add per-attribute resource (e.g., target tenant scope), must edit central attribute.
**Suggested fix:** Either justify sealed or open it.

### B55: `CommandAuthorizationDecision` exposes `CorrelationId` as a `string` property — could be empty string or `null`, breaking string operations downstream
**Severity:** Low
**File:** src/Hexalith.FrontComposer.Shell/Services/Authorization/CommandAuthorizationDecision.cs:332
**Evidence:** Constructor sets via parameter; no validation that non-null/non-empty.
**Why it bites:** A test `Returns(...)` with `null` correlation id (B47 above shows tenant accessor can return any string) propagates null to consumers expecting a guid string. NRE in any `.Length` or `.Contains` call.
**Suggested fix:** `ArgumentNullException.ThrowIfNull(correlationId)` in the private ctor.

### B56: Generated `RefreshPresentationAuthorizationAsync` always uses `_cts?.Token`, never honors the `cancellationToken` if `BeforeSubmit` later cancels — submit-path asymmetry
**Severity:** Low
**File:** src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs:887-911
**Evidence:** The OnInitializedAsync auth refresh uses `_cts?.Token`; `OnValidSubmitAsync` uses `_cts.Token` (different call path). Two refresh sites, two slightly different cancellation behaviors.
**Why it bites:** Subtle: one path can panic on null `_cts`, other path can't.
**Suggested fix:** Centralize.

### B57: Authorization warning `_serverWarning` is set in `SetAuthorizationWarning` but the publication happens via `CommandFeedbackPublisher.PublishWarning(_serverWarning)` only on submit; refresh sets `_serverWarning` but never publishes
**Severity:** High
**File:** src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs:898-908, 988-1003
**Evidence:** Refresh path: `SetAuthorizationWarning(authorization.Reason)` sets `_serverWarning`, then renders. Submit path: same setter THEN publishes via `CommandFeedbackPublisher.PublishWarning`. Refresh path publishes nothing — inline warning visible only on form, not in any global toast.
**Why it bites:** If the form is small or inline, users may miss the inline warning. Submit-time publishes a toast; render-time does not. Inconsistent UX between the two surfaces.
**Suggested fix:** Either publish from refresh too, or document the deliberate discrepancy.

### B58: `_serverWarning = null` reset in `OnValidSubmitAsync` BEFORE the auth check — if auth fails, the warning is set to null first, then `SetAuthorizationWarning` overwrites; intermediate state visible if `StateHasChanged` runs in between
**Severity:** Low
**File:** src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs:983-1003
**Evidence:** Order: clear `_serverValidationMessages`, clear `_serverFormLevelErrors`, set `_serverWarning = null`, `_editContext.NotifyValidationStateChanged()`, evaluate auth, on fail SetAuthorizationWarning, conditionally PublishWarning, InvokeAsync StateHasChanged.
**Why it bites:** `NotifyValidationStateChanged` may trigger a render where the warning briefly disappears, then reappears. Visual flicker.
**Suggested fix:** Defer the clear until after auth result is known.

### B59: `IsRecoverable` does not exclude `OperationCanceledException` — the catch-all `catch when (IsRecoverable(ex))` would catch OCE if not explicitly caught earlier
**Severity:** Low
**File:** src/Hexalith.FrontComposer.Shell/Services/Authorization/CommandAuthorizationEvaluator.cs:402-410, 510-519, 524-528
**Evidence:** OCE is caught by the explicit `catch (OperationCanceledException)` first, so the exclusion isn't needed — but the comment doesn't explain. If a future maintainer reorders catch clauses, OCE could be reported as `HandlerFailed`.
**Suggested fix:** Add OCE to `IsRecoverable` exclusion list as belt-and-suspenders.

### B60: `EmptyStateCtaResolver.ResolveCommandPolicy` is `static`, so it has no access to a logger — silently swallows policy mismatches
**Severity:** Low
**File:** src/Hexalith.FrontComposer.Shell/Services/EmptyStateCtaResolver.cs:693
**Evidence:** `private static string? ResolveCommandPolicy(...)` — no logger.
**Why it bites:** When the loop finds policy A in manifest 1 and policy B in manifest 2 for the same command, no warning is emitted; the registry version logs but the resolver's separate scan is silent.
**Suggested fix:** Make instance, log on conflict.

### B61: Catalog validator `IHostedService.StartAsync` ignores `cancellationToken` parameter — slow registry enumeration during shutdown blocks
**Severity:** Low
**File:** src/Hexalith.FrontComposer.Shell/Services/Authorization/FrontComposerAuthorizationPolicyCatalogValidator.cs:568
**Evidence:**
```
public Task StartAsync(CancellationToken cancellationToken) {
    ...registry.GetManifests()...   // never checks token
}
```
**Why it bites:** With a large manifest set, host-startup cancellation doesn't stop the validator. Probably unimportant in practice but a smell.
**Suggested fix:** `cancellationToken.ThrowIfCancellationRequested()` between manifest iterations.

### B62: `[RequiresPolicy]` attribute trims policy name in constructor but sources may still capture the un-trimmed string via `nameof`-style reflection
**Severity:** Low
**File:** src/Hexalith.FrontComposer.Contracts/Attributes/RequiresPolicyAttribute.cs:30, 37
**Evidence:** `string trimmed = policyName.Trim();` and `PolicyName = trimmed;`. Original `policyName` parameter is not stored.
**Why it bites:** If anyone uses `attribute.GetConstructorArguments()` (reflection emit, dynamic proxies) they get the original padded value. Inconsistent with `attribute.PolicyName`.
**Suggested fix:** Document at the constructor level.

### B63: Test `RequestRecord_RedactsCommandPayload_FromPrintMembers` only checks `ToString()` — does not assert what `JsonSerializer.Serialize` produces
**Severity:** Medium
**File:** tests/Hexalith.FrontComposer.Shell.Tests/Services/Authorization/CommandAuthorizationEvaluatorTests.cs:1738-1758
**Evidence:**
```
string text = request.ToString();
text.ShouldNotContain(sentinelToken);
```
**Why it bites:** A common leak path is `JsonSerializer.Serialize(request)` (e.g., for telemetry). The override of `PrintMembers` doesn't prevent JSON serialization from emitting `Command`. Test gives a false sense of security.
**Suggested fix:** Add `[JsonIgnore]` on `Command` and a test for `JsonSerializer.Serialize`.

### B64: `CommandAuthorizationResource.TenantContext` flows into auth handlers but the handler may persist or echo it — no test asserts that handlers can't leak tenant snapshot
**Severity:** Medium
**File:** src/Hexalith.FrontComposer.Shell/Services/Authorization/CommandAuthorizationDecision.cs:310-316
**Evidence:** `public sealed record CommandAuthorizationResource(... TenantContextSnapshot? TenantContext)` and no redaction on this record.
**Why it bites:** Custom auth handlers often log `IAuthorizationContext.Resource` for debugging. If anyone does that, tenant id and user id leak into logs.
**Suggested fix:** Override `PrintMembers` on the resource record too.

### B65: `CommandPaletteEffects.CanSurfaceCommandAsync` calls `TryGetService<ICommandAuthorizationEvaluator>()` — service-locator pattern in palette but evaluator is registered as `Scoped`
**Severity:** Medium
**File:** src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs:743-746
**Evidence:**
```
ICommandAuthorizationEvaluator? evaluator = TryGetService<ICommandAuthorizationEvaluator>();
if (evaluator is null) { return false; }
```
**Why it bites:** Service locator pattern + scoped service — if `CommandPaletteEffects` is itself singleton or longer-lived than scope, it resolves the wrong scope, possibly carrying stale auth/tenant state across users. The evaluator is registered scoped, so this matters.
**Suggested fix:** Inject via factory `IServiceProvider` and create scope explicitly per palette query.

### B66: `[RequiresPolicy]` only applies to class — commands declared as `record` (record class is `class` under the hood, OK) or `record struct` (would silently skip)
**Severity:** Medium
**File:** src/Hexalith.FrontComposer.Contracts/Attributes/RequiresPolicyAttribute.cs:13
**Evidence:** `[AttributeUsage(AttributeTargets.Class, ...)]`
**Why it bites:** A `record struct CreateOrderCommand` annotated with `[RequiresPolicy]` produces a compile error — but a struct command annotated with the attribute via reflection (e.g., extension scenario) silently has no effect. SourceTools' `CommandParser` probably also skips structs.
**Suggested fix:** Either include `Struct` target or document the constraint and add an HFC diagnostic for the mismatch.

### B67: Generated form's `Logger?.LogWarning` uses null-conditional but `Logger` would have been resolved via `[Inject]` — null only if DI is broken
**Severity:** Low
**File:** src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs:899, 1000
**Evidence:** `Logger?.LogWarning(...)` in two emitted spots.
**Why it bites:** Defensive null-conditional masks a misconfigured component lifecycle. If `Logger` is null, an actual problem is happening that gets silently ignored.
**Suggested fix:** Either drop the `?.` (let NRE surface) or inject `ILogger<T>` with a default `NullLogger` fallback.

### B68: `CommandFormEmitter` does not emit `_authorizationPresentationReady = true` after a successful refresh that had a transient `Pending` — initial render with no auth state can deadlock UI
**Severity:** Medium
**File:** src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs:893-908
**Evidence:**
```
bool isPending = authorization.Kind == ...Pending;
_authorizationPresentationReady = !isPending;
_authorizationPresentationAllowed = authorization.IsAllowed;
```
**Why it bites:** When the auth-state-provider returns null state on first render → `Pending` decision → `_authorizationPresentationReady = false`. The button stays disabled. The `OnAuthenticationStateChanged` event fires only when login/logout occurs, not on initial state arrival. The user logged in BEFORE form mount may never see a state-changed event after mount, so the disabled button never re-enables.
**Suggested fix:** Re-poll on a brief timer when in Pending, or schedule a one-shot retry via `OnAfterRender`.

### B69: Test fixture `FakeAuthenticationStateProvider` uses `state!` to suppress nullability — `state` can be null and is forwarded via `Task.FromResult(state!)`
**Severity:** Low
**File:** tests/Hexalith.FrontComposer.Shell.Tests/Services/Authorization/CommandAuthorizationEvaluatorTests.cs:1799-1802
**Evidence:**
```
public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(state!);
```
**Why it bites:** `state!` lies to the compiler — when `state` is null, the returned `Task<AuthenticationState>` actually contains null, but the type system says non-null. The evaluator's `state?.User` handles it correctly, but the test scaffolding is fragile and may break with future Roslyn nullable strictness.
**Suggested fix:** Return `Task.FromResult<AuthenticationState?>(state)` if testing pending; the evaluator already deals with `state?.User`.

### B70: The reordered submit clears `_serverValidationMessages?.Clear()` BEFORE checking auth — if auth fails, prior validation errors are lost
**Severity:** Medium
**File:** src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs:980-1003
**Evidence:**
```
_serverValidationMessages?.Clear();
...
_serverWarning = null;
_editContext?.NotifyValidationStateChanged();
...
var authorization = await CommandAuthorizationEvaluator.EvaluateAsync(...);
if (!authorization.IsAllowed) { ... return; }
```
**Why it bites:** User submits → existing server validation errors are cleared → auth check fails → user sees ONLY the auth error and not their previous validation errors. The validation state is destroyed before the submit goes anywhere.
**Suggested fix:** Move clearing into the success path (after auth pass).

### B71: Generated form's `_authorizationPresentationAllowed` is read by the render path while being mutated by `RefreshPresentationAuthorizationAsync` without `volatile` or interlocked
**Severity:** Medium
**File:** src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs:850-851, 893-895
**Evidence:** Plain `bool _authorizationPresentationAllowed;` read in `BuildRenderTree`'s `Disabled` expression; written in `RefreshPresentationAuthorizationAsync` without a memory barrier.
**Why it bites:** Render thread and the `InvokeAsync`-rejoined async continuation are technically on the same renderer sync context, so this is fine in Blazor Server. But Blazor WASM (single-threaded) is also fine. However, if a maintainer ever moves the refresh OFF the renderer, the missing memory barrier matters.
**Suggested fix:** Document that both fields must only be mutated on the renderer's sync context.

### B72: `IsPolicyNameWellFormed` in CommandParser duplicates the attribute's `IsWellFormed` — divergence risk
**Severity:** Low
**File:** src/Hexalith.FrontComposer.SourceTools/Parsing/CommandParser.cs:1207-1220 vs RequiresPolicyAttribute.cs:45-53
**Evidence:** Both implement identical logic but in different assemblies.
**Why it bites:** Pass-3 reviewer adds a new allowed character to the attribute (e.g., `+`) — must remember to update the parser too. Divergence shows up at runtime when reflection-discovered policies pass attribute validation but fail SG validation, or vice versa.
**Suggested fix:** Centralize the well-formedness check in the Contracts assembly and reference from the SG (or duplicate with shared test fixture).
agentId: a89086e59087e94b8 (use SendMessage with to: 'a89086e59087e94b8' to continue this agent)
<usage>total_tokens: 117975
tool_uses: 7
duration_ms: 379117</usage>
