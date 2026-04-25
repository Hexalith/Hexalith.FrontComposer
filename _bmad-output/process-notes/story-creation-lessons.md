# Story Creation Lessons Ledger

Patterns extracted from the Story 2-2 multi-agent review + advanced elicitation process (2026-04-14). Apply to upcoming story creation for Stories 2-3, 2-4, 2-5, and all of Epic 4+.

This ledger is append-only. Add new lessons at the bottom. Reference lessons by ID (L01, L02…) in new story files to cite pattern reuse.

---

## L01 — Cross-story contract clarity upfront

**Pattern:** When Story A emits artifacts that Story B consumes (e.g., `CommandModel` IR, `ICommandPageContext`, Fluxor feature state), the contract WILL couple badly unless stated explicitly in Story A's Critical Decisions table.

**Apply:** In every new story's Critical Decisions table, add explicit `Decision Dxx: Story A ships X, Story B consumes Y` entries for every cross-story seam. Reference ADR-016 (Story 2-2 ↔ Story 2-1 renderer/form split) as the canonical example.

**Triggered by:** Story 2-2 initially ambiguous about whether Renderer or Form owns `<EditForm>` — would have caused double-wrapping if Amelia hadn't raised it.

---

## L02 — Fluxor feature scope must list producer AND consumer

**Pattern:** A per-concern Fluxor feature (e.g., `DataGridNavigationState`) introduced in one story but with effects/producers/consumers scattered across multiple stories ships as dead code in the first story.

**Apply:** When introducing a new Fluxor feature, list the PRODUCER story (dispatches Capture-style actions) and the CONSUMER story (reads state). Ship:
- Reducer-only in the first story to land (contract lock)
- Effects in the producer story
- Consumer logic in the consumer story

**Triggered by:** Story 2-2 initially shipped `DataGridNavigationState` with effects (persistence, hydration, beforeunload) but the capture-side producers live in Epic 4. Winston (architect) flagged as "half a state machine — untested reducers rot."

---

## L03 — Tenant/user isolation guards, fail-closed

**Pattern:** Any service that persists data per-user MUST fail-closed when `tenantId` or `userId` is null/empty. Using `"anonymous"`, `"default"`, or empty string as fallback segments in storage keys creates cross-tenant PII leaks.

**Apply to:** Story 2-3 `CommandLifecycleState` (if persisted), Story 5-x `EventStore` client, Epic 6 customization gradient storage, Epic 7 tenant context, Epic 8 MCP tool server. Add a binding decision in the Critical Decisions table mirroring Story 2-2 Decision D31.

**Triggered by:** Story 2-2's `LastUsedValueProvider` initially would have leaked last-used values across tenants if adopter forgot `IHttpContextAccessor` wiring.

---

## L04 — Generated name collision detection

**Pattern:** Any emitter that transforms `TypeName` into a new artifact name (via stripping, prefixing, suffixing) must detect collisions at parse time OR use the full `TypeName` to guarantee determinism.

**Apply:** Prefer full-`TypeName` naming unless there's a strong cosmetic case for transformation. When transforming, add a parse-time collision detector (HFC diagnostic). In Story 2-2, the trailing-`Command` strip was initially proposed (D22), required HFC1013 collision detection, and was ultimately REVERTED during elicitation round 2 — "ugly but deterministic" beat "cosmetic but collision-prone."

**Triggered by:** Story 2-2 Chaos Monkey revealed two commands (`IncrementCommand` + `Increment`) would have collided to `IncrementRenderer`.

---

## L05 — Hand-written service + emitted per-type wiring

**Pattern:** When a service needs per-command typed behavior (e.g., subscribe to `{CommandName}Actions.ConfirmedAction`), emit the REGISTRATION/SUBSCRIBER code per command, keep the SERVICE generic and hand-written. Avoid reflection in hot paths — it's AOT-hostile.

**Apply:** This is the `LastUsedValueProvider` (hand-written) + `{CommandTypeName}LastUsedSubscriber.g.cs` (emitted per command) pattern from Decision D28. Use for any future need to bridge generic infrastructure with per-command typed Fluxor actions.

**Triggered by:** Story 2-2 initially considered reflection-based subscription or interface explosion; both were rejected.

---

## L06 — Defense-in-depth budget per story

**Pattern:** Each round of review (party mode, elicitation) adds defensive decisions. Left unchecked, a story's binding-decisions count grows unbounded. Budget it.

**Apply:**
- Feature story: ≤ 25 Critical Decisions
- Infrastructure story: ≤ 40 Critical Decisions
- When exceeded, trigger Occam's Razor + Matrix Scoring to trim bottom-quartile additions
- Story 2-2 ended at 35 (on the edge) after round 2 trim

**Triggered by:** Story 2-2 grew from 20 → 30 → 35 decisions across two review rounds; Matrix scoring identified 6 additions in the bottom quartile (score < 3.0) which were cut.

---

## L07 — Test count inflation is a cost

**Pattern:** Each new defensive decision adds 5-10 tests. Test maintenance is non-trivial. Score additions via cost-benefit matrix BEFORE adding.

**Apply:** When a review round proposes adding defense, score each against: Security impact (30%) / Implementation cost (25%, reversed) / Test cost (20%, reversed) / Adopter friction (25%, reversed). Cut bottom quartile.

**Triggered by:** Story 2-2 test count went 70 → 97 → 118 → trimmed to 111. The 118 figure included tests for items that ultimately didn't earn their keep.

---

## L08 — Party review vs. elicitation — different roles

**Pattern:** Party mode (multi-agent review) catches scope, coupling, architecture issues. Advanced elicitation (pre-mortem, red team, chaos, hindsight) catches security, edge cases, robustness gaps. They are complementary, not redundant.

**Apply:** Run party mode FIRST on a new story (captures "does the design make sense?"), then advanced elicitation (captures "what breaks?"). Don't conflate; running either alone leaves gaps.

**Triggered by:** Story 2-2 party review missed the tenant-isolation leak (PM-1) and the ReturnPath open-redirect (PM-3), both caught in the following pre-mortem round.

---

## L09 — ADR rejected-alternatives discipline

**Pattern:** Every ADR must document ≥ 2 rejected alternatives with explicit trade-off rationale. This surfaces whether a decision is "convention" (inherited from prior work) or "conviction" (deliberately chosen).

**Apply:** When writing any new ADR, fill the "Rejected alternatives" section with 2-3 alternatives at minimum. The first-principles analysis often surfaces one the original author missed (Story 2-2's ADR-016 missed "fold into `CommandFormEmitter`" until first-principles found it).

**Triggered by:** Story 2-2 ADR-016 initially documented only 3 rejected alternatives; first-principles analysis surfaced a 4th (fold-in) that had to be considered and documented to prevent re-litigation later.

---

## L10 — Deferrals need story specificity, not epic specificity

**Pattern:** "Deferred to Epic 9" is vague and becomes unactionable six months later. Name an owning STORY, not an epic.

**Apply:** In every Known Gaps table, every deferral row must link to a story number. If no story exists yet, create a backlog entry BEFORE deferring. "Deferred to future" is a smell.

**Triggered by:** Story 2-2's early drafts said "HFC1008 analyzer emission deferred to Epic 9 analyzer work" — vague. Corrected to "Story 9.4" with a backlog entry.

---

## L11 — Dev Agent Cheat Sheet for large stories

**Pattern:** Stories exceeding ~1500 lines are hard for implementer agents (Amelia-style terse thinkers) to scan. A 2-page top-of-doc cheat sheet preserves the full spec while giving a fast-path entry.

**Apply:** Any story with ≥ 30 decisions or ≥ 80 new tests should open with a Dev Agent Cheat Sheet section containing:
- Goal in one sentence
- Files to create/extend (table)
- Generated naming rules
- AC quick index (one-liner each)
- Binding contract(s) with predecessor stories
- Scope guardrails (what NOT to implement)
- Start-here task order
- Test count expectation

**Triggered by:** Story 2-2 grew to ~2500 lines; Amelia's consumption style ("file paths + AC IDs") warranted a fast-path entry.

---

## L12 — Full TCS / async-bridge lifecycle contract upfront

**Pattern:** Stories that introduce a `TaskCompletionSource<T>` (or similar async bridging primitive) between an external callback surface (Fluent `ItemsProvider`, third-party awaitable, DOM event stream) and the Fluxor pipeline require a COMPLETE lifecycle contract in the FIRST draft. Deferring defensive additions leads to 4-5 rounds of hardening (each round discovering a new orphan / leak window).

**The 7-point TCS lifecycle checklist:**
1. **Registration** — where/when does the TCS enter the state dictionary, with what idempotency guarantee if the key already exists?
2. **Cancellation** — how does external cancellation (token, scroll-away, navigation) propagate to `TrySetCanceled` + entry removal?
3. **Success** — how does `TrySetResult` land and who removes the entry?
4. **Failure** — how does `TrySetException` land and who removes the entry?
5. **Disposal** — how does component/store disposal sweep pending entries (`ClearPendingPagesAction` pattern)?
6. **Effect-body exception (guaranteed terminal dispatch)** — the effect body MUST wrap in `try/catch/finally` with a defensive finally-dispatch if no terminal action fired, so DI-resolution / cancellation-register / OOM exceptions don't orphan.
7. **Defensive-dispatch nested safety** — the defensive finally-dispatch itself MUST be wrapped in a nested try/catch — `dispatcher.Dispatch` can throw `ObjectDisposedException` during store-disposal race; the nested catch swallows-and-logs so the orphan is bounded to the disposing store's heap lifetime.
8. **Null-payload guard** — reducers consuming TCS-terminal actions MUST guard against null payloads (malformed effect output, serialization edge cases) and route to `TrySetException` instead of writing state.

**Apply:** in any story introducing an async bridge, enumerate all 8 items in the first-draft Critical Decision entry. Don't wait for a party review or elicitation pass to discover them. Reference Story 4-4 D3 as the canonical complete example.

**Triggered by:** Story 4-4 D3 took 4 revision rounds (Winston+Murat → PM elicitation pre-mortem → evening elicitation code-review+chaos-monkey) to land the complete contract. Each round discovered a new gap that was obvious in hindsight. Starting with the full checklist would have collapsed the rounds to one.

---

## L13 — JS interop helpers must spec `dispose*` from day 1

**Pattern:** Any `IJSRuntime`-bridged helper that holds state in a JS-side `Map` / `Set` / counter across .NET invocations MUST expose a `dispose*` export AND be wired into the .NET service's `IAsyncDisposable` chain from the FIRST draft. Deferring disposal to fold-time risks cross-navigation state leaks in the interim + complicates test harnesses.

**Apply:** for every JS module file in a story's cheat sheet, specify: (a) the stateful JS-side data structure; (b) the `dispose*(key)` export; (c) the .NET-side `IAsyncDisposable` call site that invokes it. Include a "`dispose*` wired into component `DisposeAsync` per L13" bullet in the T* task entry.

**Triggered by:** Story 4-4 `fc-datagrid.js` originally had `captureScrollThrottled` + `scrollToOffset` only. `disposeViewKey` was added during PM elicitation fold — preventing cross-nav leaks that would otherwise have surfaced as runtime bugs.

---

## L14 — Bounded-by-policy beats documented-unbounded for any in-memory cache

**Pattern:** Any cache structure (`Dictionary<K, V>`, `Queue<T>`, `MemoryCache`, etc.) that accumulates entries during user interaction MUST ship with a DEFAULT BOUND + simple eviction policy in v1 — EVEN IF a sophisticated policy (LRU, TTL, access-order) is deferred to a later story. "Documented-unbounded" is a slow memory leak waiting to surface in long-lived sessions (8-hour tab-open is a common adopter pattern).

**Apply:** for every caching layer in a new story: (a) define a `Max*` `FcShellOptions` property with a `[Range]` constraint; (b) specify a simple eviction policy (insertion-order FIFO via `ImmutableQueue`) in the reducer that writes the cache; (c) emit an Information-level log on eviction for operator visibility; (d) defer sophisticated policy (LRU, TTL) to a follow-up story while keeping the bound + simple eviction in v1. Avoid the "v1 ships unbounded + test-as-regression-rail only" anti-pattern — tests are not runtime bounds.

**Triggered by:** Story 4-4 `LoadedPageState.PagesByKey` was initially planned as documented-unbounded with Epic 9-5 as the LRU owner. Pre-mortem elicitation flagged silent memory leak in 8-hour-open sessions. Fold added `MaxCachedPages=200` + `ImmutableQueue`-based FIFO eviction + Information-level log — O(1) runtime complexity, simple to understand, operator-visible. LRU sophistication still deferred to Epic 9-5, but runtime is bounded from v1.

---

## L15 — Flip-the-silent-drop patches must bundle placeholder emission + scope-guardrail test

**Pattern:** When a story removes a "silent drop" / `continue` in an IR pipeline (e.g., Transform-stage `if (isUnsupported) continue`) that contradicts a PRD / UX requirement (like FR9 "never silently omit"), the story MUST bundle THREE things in the SAME story — (1) the pipeline flip, (2) the emit-side placeholder (or equivalent visible artifact), (3) a scope-guardrail test that asserts "every input property appears as an output column/artifact." Shipping the flip without the guardrail lets future refactors re-introduce the drop silently.

**Apply:** for any story removing a silent-drop / filter in an IR pipeline, add an explicit test named `EveryInputXAppearsInOutputY` (e.g., `EveryProjectionPropertyAppearsInEmittedColumns`) that scans approved baselines and asserts the invariant with a fail-message naming the owning story decision (e.g., "Unsupported-drop regression. See Story 4-6 D1"). Treat as a regression rail AT the pipeline boundary — emit-site assertions alone are too brittle (emit logic can also drop).

**Triggered by:** Story 4-6 D1 removed `RazorModelTransform.cs:29` `if (property.IsUnsupported) { continue; }` to honor FR9 + UX-DR55. The Parse-stage HFC1002 Warning was firing; the Transform-stage drop was silently reversing it at runtime. The combination made auto-generation "dishonest": build says "unsupported" but runtime says "nothing here." Scope-guardrail test lives in `UnsupportedColumnEmissionTests.EveryProjectionPropertyAppearsInEmittedColumns` (story 4-6 T6.1-T6.4).

---

## Process: How to use this ledger

- Before creating a new story, scan this file for relevant lessons
- Cite lessons in the new story's Dev Notes by ID (e.g., "applying L03 tenant guard")
- When a review round surfaces a new reusable pattern, add it as L{n+1} at the bottom with the 3-section structure (Pattern / Apply / Triggered by)
- Lessons that are superseded or disproven are NOT deleted — they are marked `[SUPERSEDED by Lxx]` at the top of the entry to preserve history
