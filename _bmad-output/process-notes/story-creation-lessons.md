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

## Process: How to use this ledger

- Before creating a new story, scan this file for relevant lessons
- Cite lessons in the new story's Dev Notes by ID (e.g., "applying L03 tenant guard")
- When a review round surfaces a new reusable pattern, add it as L{n+1} at the bottom with the 3-section structure (Pattern / Apply / Triggered by)
- Lessons that are superseded or disproven are NOT deleted — they are marked `[SUPERSEDED by Lxx]` at the top of the entry to preserve history
