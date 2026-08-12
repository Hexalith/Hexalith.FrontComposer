---
title: 'Story 9.3: Define explicit command target identity'
type: 'feature'
created: '2026-08-12'
status: 'done'
baseline_commit: '8ba36a8c0494cd8f5640b4383ff2fab0742ff836'
review_loop_iteration: 2
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-9-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The approved FC-NIP base source silently treats an ambient generated source row as the command target. It cannot safely describe standalone create, cross-row, status-move, delete, or no-op outcomes, so Stories 9.4-9.6 have no trustworthy target contract to implement.

**Approach:** Approve a successor decision contract in which a generated command-target descriptor and FrontComposer-owned typed provider resolve one immutable target snapshot before asynchronous dispatch. Keep terminal materiality separate, fail closed on unknown identity/materiality, and pin the decision and complete outcome matrix with governance tests.

## Boundaries & Constraints

**Always:** Preserve the 2026-07-05 row-context decision as historical base authority. Define target `ProjectionTypeName`, canonical view/lane, exact `EntityKey`, change kind, prior/expected status, and `CapturedAt`; attach `MessageId` after acceptance and keep terminal `ObservedAt` separate. Resolve dynamic values only through an explicit command-to-projection declaration plus typed `ICommandTargetIdentityProvider<TCommand>`; an explicitly declared `SameAsSource` mode may consume a pre-dispatch source snapshot. Terminal adapters report `Material`, `NoOp`, or `Unknown`; `Unknown` suppresses the indicator.

**Ask First:** Any public runtime API implementation, EventStore contract change, multi-target command support, or change to the accepted idempotent/ten-second-linger UX requires a separate human decision.

**Never:** Implement Stories 9.4-9.6 here; infer a target from ambient source-row placement, property-name conventions, routes, projection nudges, visible-row diffs, broad lane marking, unproven EventStore `AggregateId`, or opaque result payloads; edit generated output or submodules; alter packages, schema fingerprints, deployment, or public API baselines.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Standalone create | Explicit projection declaration + typed target provider | Snapshot new target before dispatch; material confirmation is indicator-eligible | Missing/unknown target suppresses publication |
| Same-row update | Explicit `SameAsSource` declaration + source snapshot | Copy source as the named target before dispatch; material confirmation is eligible | Never fall back to ambient cascade |
| Cross-row update | Provider resolves a target distinct from source | Publish only for the declared target | Source reuse without declaration is invalid |
| Status move | Target row + prior and destination status | Target destination lane; preserve both statuses | Missing destination status suppresses publication |
| Delete | Explicit target + `Delete` kind | Preserve target for lifecycle/audit; no fresh-row indicator | Material delete remains suppressed |
| Idempotent confirmation | Material target + `IdempotentConfirmed` | Preserve existing eligible/TTL disposition | `NoOp` or `Unknown` materiality suppresses |
| Rejected / needs review | Any declared target | No indicator | Preserve rejection/review lifecycle state |
| No-op | Typed terminal `NoOp` (`EventCount == 0` or equivalent) | No indicator | Never infer materiality from status text |

</frozen-after-approval>

## Code Map

- `_bmad-output/contracts/fc-nip-row-identity-producer-contract-2026-07-04.md` -- historical base contract; retain its decision and link the approved successor.
- `_bmad-output/contracts/fc-nip-command-target-identity-contract-2026-08-12.md` -- new authoritative Story 9.3 decision and disposition matrix.
- `_bmad-output/planning-artifacts/prd.md` and `_bmad-output/planning-artifacts/architecture.md` -- resolve D-4 and name the target-provider/materiality invariants.
- `_bmad-output/project-docs/architecture.md` and `docs/reference/components/datagrid.md` -- synchronize developer/adopter truth without claiming composed completion.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Docs/FcNipRowIdentityProducerContractTests.cs` -- governance guard; replace stale pre-remediation wording with base-plus-successor assertions.
- `tests/e2e/specs/fc-nip-row-identity-contract.spec.ts` -- browserless contract guard mirroring the successor fields, forbidden sources, and eight dispositions.
- `_bmad-output/contracts/fc-tbl-table-api-contract-2026-06-04.md` and `_bmad-output/contracts/fc-cmd-pending-identity-correlation-contract-2026-06-04.md` -- sibling contracts; replace the superseded "Story 9.1 confirms / Story 9.2 wires" ownership wording and restore their guard coverage.
- `_bmad-output/implementation-artifacts/epic-9-context.md` -- epic context; restore the FC-NIP/FC-TBL/FC-CMD ownership split and the bounded-typed-payload requirement, and keep UX obligations to what the PRD and `ux-design.md` already back.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` -- story status tracking for `9-3-define-explicit-command-target-identity`.
- `_bmad-output/implementation-artifacts/deferred-work.md` -- ledger for the review deferrals and the three contract-declared deferrals (multi-target, post-dispatch key proof, public API shape).
- `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json` -- governance identifier inventory seal. Regenerate by running `AnalyzerPolicyGovernanceTests.AnalyzerPolicy_IdentifierInventory_MatchesSeal` and copying the reported count and SHA into `identifierInventory`; it changes whenever a `tests/**/*.cs` underscore identifier is added, removed, or moved to a different line.
- `.github/workflows/quality.yml` -- add the browserless `npm run test:fc-nip` step so the Playwright half of the contract actually executes in CI. `quality.yml` is not pinned by `evaluator_authorizations`, unlike `ci.yml` / `release.yml` / `release-evidence.yml`.
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandRowIdentity.cs` and `src/Hexalith.FrontComposer.SourceTools/Emitters/{CommandFormEmitter,RazorEmitter}.cs` -- read-only gap evidence for later stories; do not change in 9.3.
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStorePendingCommandStatusQuery.cs`, `src/Hexalith.FrontComposer.Shell/State/PendingCommands/NewItemIndicatorStateService.cs`, and `_bmad-output/implementation-artifacts/9-2-wire-fcnewitemindicator-producer-and-generated-grid-consumer.md` -- read-only guard inputs (negative no-smuggling pins, the ten-second TTL constant, and the Story 9.2 delivery record); do not change in 9.3.

## Tasks & Acceptance

**Execution:**
- [x] `_bmad-output/contracts/fc-nip-command-target-identity-contract-2026-08-12.md` -- record the approved provider, immutable snapshot, materiality model, complete matrix, and downstream ownership.
- [x] Base contract, PRD, both architecture sources, and DataGrid reference -- link the successor decision and resolve D-4 while keeping Epic 9/FR-13/FR-26 open through Story 9.8.
- [x] C# and Playwright FC-NIP contract guards -- pin required fields, pre-dispatch capture, explicit source-versus-target rules, fail-closed behavior, and all matrix rows without weakening no-guessing checks.
- [x] Code review remediation (second pass, 2026-08-12) -- amend the successor contract with the declaration authoring surface, `TenantId`/`UserId` scope capture, descriptor-versus-provider fail-closed precedence, `SameAsSource` change-kind validity, the seven behavioural rules routed to Story 9.4, and Story 9.9 as the preallocation owner.
- [x] Sibling contract synchronization and guard restoration -- update FC-TBL and FC-CMD ownership wording and re-pin both in the blocking C# guard.
- [x] `epic-9-context.md` repair -- restore the three-contract ownership split and the bounded-typed-payload requirement; reconcile UX obligations against the PRD and `ux-design.md`.
- [x] Guard hardening -- case-sensitive positive assertions, closed-set materiality phrases, section-bounded table parsing with separator verification, mirrored single-pipe cell stripping, full emitter slot bindings, restored base-contract payload pins, Story 9.2 delivery-record pins, and the ten-second TTL binding.
- [x] CI wiring -- run the browserless FC-NIP Playwright guards in `quality.yml`.

**Acceptance Criteria:**
- Given Product + Architecture approve the successor contract, when target identity is reviewed, then every required field has one framework-owned source and target capture precedes dispatch.
- Given every supported command/outcome shape, when the matrix is evaluated, then its target and indicator disposition are explicit and no source row is silently reused.
- Given the decision artifact or synchronized truth sources drift, when governance runs, then focused guards fail while Stories 9.4-9.6 remain implementation owners.

### Review Findings

Code review 2026-08-12 (layers: blind-hunter, edge-case-hunter, verification-gap, acceptance-auditor).
Verified clean: File List exact 12/12; all eight frozen matrix rows pinned cell-by-cell in both guards;
Never-list fully honored; all twelve review-order anchors resolve; analyzer-ledger reseal is exactly
story-owned (+3 = 5 added minus 2 removed test methods).

- [x] [Review][Decision] Sibling FC-TBL/FC-CMD contracts left stale and simultaneously unpinned — `fc-tbl-table-api-contract-2026-06-04.md:28` and `fc-cmd-pending-identity-correlation-contract-2026-06-04.md:85-86` still read "Story 9.1 confirms the row-identity payload and Story 9.2 wires the producer", contradicting this commit's own PRD/architecture/datagrid synchronization. Their guard assertions were deleted, so nothing detects the drift. Both files are outside the declared File List, so synchronizing them now versus deferring to 9.4/9.8 is a scope decision.
- [x] [Review][Decision] Successor contract underspecified for the stories it gates — 9.4-9.6 must implement from it, but it omits: tenant/user scope in the immutable snapshot (while `epic-9-context.md` mandates previous-scope rejection); any rule for clearing an active indicator on `Delete`/`StatusMove`; disposition when the same `MessageId` is re-observed with conflicting materiality (`Unknown` then `Material`); rejection of `SameAsSource` combined with `ChangeKind = Create`; a maximum `CapturedAt`-to-`ObservedAt` age and clock-skew rule; `(ViewKey, EntityKey)` canonicalization/comparison semantics; a bounded provider-resolution deadline; and early-observation buffer overflow disposition.
- [x] [Review][Decision] Declaration authoring surface never defined — the contract makes "an explicit command-to-projection declaration" the sole legitimate target source but never says what a declaration is (attribute, generator input, DI registration, metadata). Combined with the frozen Ask First clause deferring public API shape, Story 9.4 is left blocked or guessing on the most consequential choice.
- [x] [Review][Decision] **RESOLVED 2026-08-12 — FC-NIP stays opt-in per command; Story 9.4 owns a build-time diagnostic plus the sample migration; no implicit declaration.** No migration path for existing undeclared commands — every command generated today has no declaration, and the contract fails closed on a missing declaration, so landing 9.4 silently disables FC-NIP for all of them. No story owns the backfill and no artifact records the consequence. The second review pass confirmed the regression is **live, not theoretical**: `PendingCommandOutcomeResolver.cs:108-137` publishes an indicator for any confirmed/idempotent-confirmed outcome whose entry carries non-empty `ProjectionTypeName`, `LaneKey`, `EntityKey`, and `MessageId`, all supplied by the ambient `PendingCommandRowIdentity` cascade — so every grid-row-launched command publishes today with no declaration. Promoting that cascade into an implicit `SameAsSource` declaration was rejected because it would reintroduce the ambient source-row placement the frozen Never-list forbids. Recorded in the successor contract's new "Migration From The Historical Row Cascade" section and pinned by both guards.
- [x] [Review][Decision] `epic-9-context.md` substantially rewritten with no Code Map entry and no Execution task — a 32-line rewrite of Goal, Stories, Requirements, Technical Decisions, UX, and Cross-Story Dependencies. It appears only in the File List. It is also this spec's own declared `context:` source, so the story rewrote the document its acceptance criteria derive from, in the same commit.
- [x] [Review][Decision] `ux-design.md` not synchronized — `epic-9-context.md` newly asserts `role="status"`, `aria-live="polite"`, localization, reduced-motion, and forced-colors invariants, but the PRD D-2 canonical UX source is untouched and absent from the File List.
- [x] [Review][Patch] Red governance guard deleted rather than repaired; evidence claim overstated — `FcNipContractReferences_WhenAuthored_NameEpicNineOwnershipInDocs` was already failing at baseline `8ba36a8c` because commit `730d8595` removed three phrases it asserted ("Epic 9 / FC-NIP" and "current projection nudge does not include row identity" in datagrid; "FC-NIP owns the post-MVP command outcome payload and producer wiring" in architecture). The story deleted the test and did not restore the phrases (verified absent at `b50243df`). Verification reports 4,333/4,333 with no Documented Unrelated Changes entry. [tests/Hexalith.FrontComposer.SourceTools.Tests/Docs/FcNipRowIdentityProducerContractTests.cs:57]
- [x] [Review][Patch] Nine forbidden-source assertions live only in a lane CI never runs — `quality.yml` runs `typecheck`, `test:a11y`, `validate:visual-governance`, `validate:a11y-artifacts`; `npm run test:fc-nip` is invoked by no workflow. The whole "Forbidden Identity And Materiality Sources" list is pinned only in Playwright; the blocking C# guard pins just the section's two closing sentences. Mirror the nine bullets into the C# guard (or wire the spec into CI). [tests/e2e/specs/fc-nip-row-identity-contract.spec.ts:50]
- [x] [Review][Patch] Emitter no-smuggling assertions weakened without cause — pins reduced from `'EntityKey: PendingCommandRowIdentity?.EntityKey'` to bare right-hand-side substrings, and `'CommandTypeName: typeof('` dropped entirely. `CommandFormEmitter.cs:785-788` still contains every full string at `b50243df`, so nothing forced the relaxation; a crossed binding such as `EntityKey: PendingCommandRowIdentity?.ProjectionTypeName` now passes. Also add object-initializer forms (`EntityKey =`) to the negative assertions. [tests/Hexalith.FrontComposer.SourceTools.Tests/Docs/FcNipRowIdentityProducerContractTests.cs:56]
- [x] [Review][Patch] Base-contract minimum-payload pins lost `ViewKey`, `EntityKey`, `ProjectionTypeName` — the replacement `AssertContainsAll` list keeps only `MessageId`, `ExpectedStatusSlot`, `PriorStatusSlot`, `CreatedAt`, `TenantId`, `UserId`, `first-wins`. All three dropped fields still exist in the base contract's payload table, which can now be gutted with no guard failing. [tests/Hexalith.FrontComposer.SourceTools.Tests/Docs/FcNipRowIdentityProducerContractTests.cs:40]
- [x] [Review][Patch] Story status contradicts itself across three artifacts — spec frontmatter `status: 'done'` with `review_loop_iteration: 0`, `sprint-status.yaml` `review`, and the new contract's own "This records approved semantics, not Story 9.3 completion". A `done` spec is auto-loaded as continuity context by Stories 9.4+. [_bmad-output/implementation-artifacts/spec-9-3-define-explicit-command-target-identity.md:5]
- [x] [Review][Patch] Bare-token assertions cannot detect semantic inversion — `ShouldContain("Material")` is satisfied by the heading "Terminal Materiality" alone; `"Unknown"`, `"MessageId"`, `"SameAsSource"`, `"CapturedAt"`, `"ObservedAt"` would still pass if the surrounding prose said the opposite. Assert the full closed-set phrase "`Material`, `NoOp`, or `Unknown`" instead. [tests/Hexalith.FrontComposer.SourceTools.Tests/Docs/FcNipRowIdentityProducerContractTests.cs:56]
- [x] [Review][Patch] The three completion-blocking sentences are unpinned — no guard asserts "FR-13, FR-26, and Epic 9 remain open through Story 9.8", "This records approved semantics, not Story 9.3 completion", or "Story 9.3 does not add a public runtime API, change EventStore, or implement generated/runtime behavior". These are exactly the claims a later edit could reverse to manufacture completion. [_bmad-output/contracts/fc-nip-command-target-identity-contract-2026-08-12.md:1]
- [x] [Review][Patch] Published adopter docs cite an internal `_bmad-output/` path — `docs/reference/components/datagrid.md:85` embeds the contract path, which is not shipped with the DocFX site and is unresolvable for adopters. [docs/reference/components/datagrid.md:85]
- [x] [Review][Patch] Code Map omits `analyzer-policy-exception-ledger-v1.json` — it appears in File List and Suggested Review Order but has no Code Map entry, no Execution task, and no recorded regeneration command, despite the reseal being verified legitimate. [_bmad-output/implementation-artifacts/spec-9-3-define-explicit-command-target-identity.md:44]
- [x] [Review][Patch] Markdown table-parser robustness in both guards — C# `Trim('|')` strips any number of pipes while TS `replace(/^\||\|$/g,'')` strips one, so a row ending in an empty cell makes the two guards disagree; neither handles escaped or inline-code pipes; both assume the separator row sits at `headingIndex + 2` and neither stops the scan at the next heading, so a deleted table silently validates a different section's. [tests/e2e/specs/fc-nip-row-identity-contract.spec.ts:50]
- [x] [Review][Defer] C#/TS guards duplicate ~40 literal fragments and two full tables with no shared source of truth — deferred, pre-existing pattern; a single contract typo breaks both suites and the hand-written copies will drift. [tests/Hexalith.FrontComposer.SourceTools.Tests/Docs/FcNipRowIdentityProducerContractTests.cs:56]
- [x] [Review][Defer] Naming drift between guard identity and content — deferred, pre-existing; the spec file remains `fc-nip-row-identity-contract.spec.ts` and the class remains `FcNipRowIdentityProducerContractTests` while both now primarily guard target identity, and the base contract header still reads "Story 9.1 - Confirm the FC-NIP row-identity producer contract". [tests/e2e/specs/fc-nip-row-identity-contract.spec.ts:1]

### Review Findings — second pass (2026-08-12)

Independent re-review of the committed range `8ba36a8c..b50243df` only (working-tree edits excluded).
Layers: blind-hunter, edge-case-hunter, verification-gap, acceptance-auditor. No layer failed.

**Correction to the first pass:** the first-pass `[Patch]` claiming the emitter no-smuggling pins were
weakened is **withdrawn as a coverage claim**. `CommandFormEmitterTests.cs:286-290` asserts every full
`Key: PendingCommandRowIdentity?.Value` binding against real emitter output, `:267` pins
`CommandTypeName: typeof(`, and two `.verified.txt` snapshots pin the whole block. A crossed binding
fails CI today. The doc-guard relaxation is redundancy, not a hole.

- [x] [Review][Decision] **RESOLVED 2026-08-12 — split: scope-critical amended in 9.3, remainder routed to 9.4.** Successor contract underspecified on ten points that gate Stories 9.4-9.6 — no `TenantId`/`UserId` in the immutable snapshot (zero occurrences in the file) while `epic-9-context.md:26` mandates previous-scope rejection; no precedence rule when descriptor-fixed and provider-returned `ChangeKind`/`ExpectedStatus` disagree (`ViewKey` alone gets one); no rejection of `SameAsSource` combined with `Create`/`StatusMove`/`Delete`; no disposition when accepted dispatch returns an empty or non-ULID `MessageId`; no snapshot-equality rule to separate duplicate from conflict (a `CapturedAt`-inclusive comparison makes every re-observation a conflict); no bounded provider-resolution deadline (only failure and cancellation are dispositioned, so a hanging provider blocks dispatch itself); no `ViewKey`/`EntityKey` canonicalization or comparison ordinality; no max `CapturedAt`→`ObservedAt` age or clock-skew rule; no early-observation buffer capacity/eviction despite the word "bounded"; and no fail-closed rule for duplicate `ICommandTargetIdentityProvider<TCommand>` registrations (DI last-wins would silently pick one).
- [x] [Review][Decision] **RESOLVED 2026-08-12 — define the declaration surface in the contract now; file preallocation as a new story blocking 9.8 create-path evidence.** Declaration authoring surface never defined, and standalone create — the frozen Intent's headline problem — has no owner — the contract makes "an explicit command-to-projection declaration" the sole legitimate target source but never says what a declaration *is* (attribute, generator input, DI registration, metadata). Separately, `fc-nip-command-target-identity-contract-2026-08-12.md:63-67` makes create eligibility depend on a "framework-owned preallocation mechanism"; `grep -rni "preallocat" src/` returns zero hits and Downstream Ownership assigns it to no story, while `epic-9-context.md:30` requires live create-path browser evidence for Epic 9 closure. Story 9.4 is left guessing on the most consequential choice.
- [x] [Review][Decision] **RESOLVED 2026-08-12 — synchronize both contracts and restore their guard assertions now; File List expands by two.** Sibling FC-TBL/FC-CMD contracts left stale and simultaneously unguarded — `fc-tbl-table-api-contract-2026-06-04.md:28` and `fc-cmd-pending-identity-correlation-contract-2026-06-04.md:85-86` still read "Story 9.1 confirms the row-identity payload and Story 9.2 wires the producer", contradicting this commit's own PRD/architecture/datagrid synchronization. Their guard assertions were deleted in the same commit, so nothing detects the drift, and both files sit outside the declared File List. Synchronizing now versus deferring to 9.4/9.8 is a scope decision. (Re-raised and confirmed from the first pass.)
- [x] [Review][Decision] **RESOLVED 2026-08-12 — restore the ownership split and bounded-typed-payload requirement, add a Code Map entry plus Execution task, and keep the new UX obligations only where the PRD or `ux-design.md` already backs them.** `epic-9-context.md` rewritten with no Code Map entry and no Execution task, and it is this spec's own `context:` source — the rewrite deleted the only statement of the FC-NIP / FC-TBL / FC-CMD contract-ownership split (present at baseline, `grep "FC-TBL\|FC-CMD"` now returns nothing) and the "upstream row identity must publish a bounded typed payload" requirement, while adding new normative obligations no PRD requirement or guard backs: `role="status"`, `aria-live="polite"`, localization, reduced-motion, forced-colors, and "immediately and exactly once" delivery. A decision-only story silently created ungated acceptance surface for 9.5-9.8 by editing the document its own acceptance criteria derive from.

**Patches generated by the four resolutions above:**

- [x] [Review][Patch] Amend the successor contract with the three scope-critical rules — add `TenantId`/`UserId` to the immutable target snapshot (and to the Historical Carrier Compatibility table) with a scope-equality-before-publication rule; add a descriptor-vs-provider precedence rule that fails closed on disagreement for `ChangeKind` and `ExpectedStatus`; add explicit rejection of `SameAsSource` combined with `Create`, `StatusMove`, or `Delete`. Record the remaining seven behavioural rules (provider deadline, clock skew/max age, snapshot equality for duplicate-vs-conflict, `ViewKey`/`EntityKey` canonicalization and comparison ordinality, buffer capacity/eviction, duplicate provider registration, post-accept `MessageId` validation failure) as explicit Story 9.4 inputs under Downstream Ownership. [_bmad-output/contracts/fc-nip-command-target-identity-contract-2026-08-12.md:49]
- [x] [Review][Patch] Define the declaration authoring surface in the successor contract — state what a "command-to-projection declaration" is (attribute, generator input, DI registration, or metadata) so Story 9.4 has an unambiguous input to parse. This is the SourceTools input shape, not a public runtime API, so it does not trip the frozen Ask First clause. [_bmad-output/contracts/fc-nip-command-target-identity-contract-2026-08-12.md:37]
- [x] [Review][Patch] File a new story owning the framework-owned `EntityKey` preallocation mechanism — the contract makes standalone-create eligibility depend on it, nothing in `src/` implements it, and `epic-9-context.md:30` requires live create-path browser evidence for Epic 9 closure. Mark the new story a blocker for Story 9.8 and name it in Downstream Ownership. [_bmad-output/contracts/fc-nip-command-target-identity-contract-2026-08-12.md:63]
- [x] [Review][Patch] Synchronize and re-guard the two sibling contracts — update the superseded "Story 9.1 confirms / Story 9.2 wires" ownership wording in `fc-tbl-table-api-contract-2026-06-04.md:28` and `fc-cmd-pending-identity-correlation-contract-2026-06-04.md:85-86`, restore assertions over both in the CI-executed C# guard, and add both paths to the File List. [_bmad-output/contracts/fc-tbl-table-api-contract-2026-06-04.md:28]
- [x] [Review][Patch] Repair `epic-9-context.md` — restore the FC-NIP / FC-TBL / FC-CMD contract-ownership split and the "upstream row identity must publish a bounded typed payload" requirement, add a Code Map entry plus an Execution task for the file, and reconcile the newly added `role="status"` / `aria-live="polite"` / localization / reduced-motion / forced-colors / "exactly once" obligations against the PRD and `ux-design.md`, keeping only what those already back. [_bmad-output/implementation-artifacts/epic-9-context.md:32]

**Patches found directly by the review layers:**

- [x] [Review][Patch] Nine forbidden-source rules are pinned only in a lane CI never executes — the "Forbidden Identity And Materiality Sources" list is the contract's fail-closed core, and its nine bullets are asserted only at `fc-nip-row-identity-contract.spec.ts:110-127`. `npm run test:fc-nip` is invoked by no workflow; `quality.yml:463-494` runs only `typecheck`, `test:a11y` (which targets `specs/specimen-accessibility.spec.ts` alone), `validate:visual-governance`, and `validate:a11y-artifacts`, and the sole `playwright` reference in `.github/workflows/` is the browser install at `quality.yml:469`. The blocking Gate 3a C# guard (`quality.yml:217-222`, no `continue-on-error`) pins only the section's two closing sentences. Delete any bullet and the blocking lane stays green. [tests/Hexalith.FrontComposer.SourceTools.Tests/Docs/FcNipRowIdentityProducerContractTests.cs:85]
- [x] [Review][Patch] A red guard was deleted rather than repaired, and the reported evidence conceals it — `FcNipContractReferences_WhenAuthored_NameEpicNineOwnershipInDocs` was already failing at baseline `8ba36a8c`: commit `730d8595` had removed all three phrases it asserted. Verified absent at both `8ba36a8c` and `b50243df` — "Epic 9 / FC-NIP" and "current projection nudge does not include row identity" in `datagrid.md`, "FC-NIP owns the post-MVP command outcome payload and producer wiring" in `project-docs/architecture.md`. Gate 3a was therefore red on `main` before this story; the recorded `4,333/4,333 passed with 0 failures` is true only after the deletion, with no Documented Unrelated Changes entry. [tests/Hexalith.FrontComposer.SourceTools.Tests/Docs/FcNipRowIdentityProducerContractTests.cs:57]
- [x] [Review][Patch] Shouldly `ShouldContain` is case-insensitive by default, so bare-token assertions are far weaker than they read — empirically confirmed against Shouldly 4.3.0: `"Terminal Materiality".ShouldContain("material")` passes. `ShouldContain("Material")`, `"NoOp"`, `"Unknown"`, `"MessageId"`, `"SameAsSource"`, `"CapturedAt"`, `"ObservedAt"` are satisfied by a heading alone and by any casing, including prose that states the opposite. The TypeScript mirror uses case-sensitive `toContain`, so the two "mirrored" guards genuinely disagree. Assert the closed-set phrase "`Material`, `NoOp`, or `Unknown`" and pass `Case.Sensitive` (the repo already does this in `Cli.Tests`). [tests/Hexalith.FrontComposer.SourceTools.Tests/Docs/FcNipRowIdentityProducerContractTests.cs:56]
- [x] [Review][Patch] Both table parsers can silently validate a different section's table — neither `ParseTableRows` nor `parseTableRows` stops the forward scan at the next `#` heading, so deleting the "Immutable Target Snapshot" table makes the guard bind to the Historical Carrier Compatibility table instead of failing. Both also assume the separator sits at `headerIndex + 2` without verifying it. And the two disagree on cell splitting: C# `Trim('|')` strips any number of pipes while TS `replace(/^\||\|$/g,'')` strips exactly one, so a row ending in an empty cell yields different column counts. [tests/Hexalith.FrontComposer.SourceTools.Tests/Docs/FcNipRowIdentityProducerContractTests.cs:228]
- [x] [Review][Patch] Base-contract minimum-payload pins lost `ViewKey`, `EntityKey`, `ProjectionTypeName` — the replacement `AssertContainsAll` list keeps only `MessageId`, `ExpectedStatusSlot`, `PriorStatusSlot`, `CreatedAt`, `TenantId`, `UserId`, `first-wins`. All three dropped names still exist in the base contract's payload table, which can now be gutted with no guard failing. Same removal in both languages. [tests/Hexalith.FrontComposer.SourceTools.Tests/Docs/FcNipRowIdentityProducerContractTests.cs:40]
- [x] [Review][Patch] The three completion-blocking sentences are unpinned — no guard asserts "FR-13, FR-26, and Epic 9 remain open through Story 9.8", "This records approved semantics, not Story 9.3 completion", or "Story 9.3 does not add a public runtime API, change EventStore, or implement generated/runtime behavior". These are exactly the claims a later edit could reverse to manufacture completion, and the guard pins the adjacent approval-provenance line but not these. [_bmad-output/contracts/fc-nip-command-target-identity-contract-2026-08-12.md:52]
- [x] [Review][Patch] The entire Story 9.2 delivery-record pin block was deleted and replaced by nothing — baseline `fc-nip-row-identity-contract.spec.ts:83,92-97` asserted six facts about `9-2-wire-fcnewitemindicator-producer-and-generated-grid-consumer.md`, including the explicit prohibition "Do not hide FC-NIP row identity in optional EventStore/domain-defined `ResultPayload`". `grep -rn "9-2-wire-fcnewitemindicator" tests/ eng/` now returns nothing — that artifact is pinned by no guard in either language. [tests/e2e/specs/fc-nip-row-identity-contract.spec.ts:147]
- [x] [Review][Patch] Published adopter docs cite an internal path and document a type that exists nowhere — `docs/reference/components/datagrid.md:85` embeds `_bmad-output/contracts/…`, which is not shipped with the DocFX site (and `eng/validate-docs.ps1` does no link resolution, so it passes silently). The same page states the target "comes from … typed `ICommandTargetIdentityProvider<TCommand>`" with no "planned"/"not yet available" qualifier, on a page framed as covering *current* public types — while `grep -rn "ICommandTargetIdentityProvider\|CommandTargetSnapshot\|CommandMateriality" src/` returns zero hits and the frozen Ask First defers any public API shape. [docs/reference/components/datagrid.md:85]
- [x] [Review][Patch] Neither guard is a superset of the other, so the Code Map's "mirroring" claim holds in neither direction — the C# lane pins the materiality closed set but not the forbidden-source bullets; the TS lane pins the forbidden bullets but stops its successor-field list at `ObservedAt` and never pins materiality. Only the C# lane runs in CI. Either close both gaps or drop the "mirroring" wording. [_bmad-output/implementation-artifacts/spec-9-3-define-explicit-command-target-identity.md:51]
- [x] [Review][Patch] Code Map omits four files the change depends on — `analyzer-policy-exception-ledger-v1.json`, `epic-9-context.md`, and `sprint-status.yaml` appear only in the File List with no Code Map entry, no Execution task, and no recorded regeneration command; `EventStorePendingCommandStatusQuery.cs` is read and negatively pinned by both guards (`ShouldNotContain("EntityKey:")`, `ShouldNotContain("PriorStatusSlot:")` newly added) but appears in neither list. [_bmad-output/implementation-artifacts/spec-9-3-define-explicit-command-target-identity.md:44]
- [x] [Review][Patch] Three explicitly declared deferrals were never filed — the contract defers multi-target commands (`:22`), post-dispatch server-assigned-key identity proof (`:66-68`), and any public API shape (`:187`), and the frozen Ask First repeats them, but the commit adds no `deferred-work.md` entry; that file is modified only in the uncommitted working tree and is absent from the committed range. [_bmad-output/contracts/fc-nip-command-target-identity-contract-2026-08-12.md:22]
- [x] [Review][Patch] Story status contradicts itself across three artifacts — spec frontmatter `status: 'done'` with `review_loop_iteration: 0`, `sprint-status.yaml:127` `review`, and the new contract's own "This records approved semantics, not Story 9.3 completion". `eng/validate-story-artifacts.py` contains zero occurrences of `status` or `sprint-status`, so the validator the story ran cannot observe the divergence. A `done` spec is auto-loaded as continuity context by Stories 9.4+. [_bmad-output/implementation-artifacts/spec-9-3-define-explicit-command-target-identity.md:5]
- [x] [Review][Patch] The "ten-second TTL" prose is unbound to its implementation constant — the contract hardcodes it twice while the value lives at `src/Hexalith.FrontComposer.Shell/State/PendingCommands/NewItemIndicatorStateService.cs:11` (`DefaultLifetime = TimeSpan.FromSeconds(10)`). No guard binds prose to constant, so they drift silently. [_bmad-output/contracts/fc-nip-command-target-identity-contract-2026-08-12.md:154]
- [x] [Review][Patch] The Playwright guards can evaporate without a red build — `fc-nip-row-identity-contract.spec.ts:13` skips unless `browserName === 'chromium'`, but `package.json:31` already passes `--project=chromium` and the tests never open a page. Under a full run with a renamed or removed chromium project, all six guards skip silently instead of failing. [tests/e2e/specs/fc-nip-row-identity-contract.spec.ts:13]

- [x] [Review][Defer] Dual dating of the base decision never reconciled — deferred, cosmetic; the base file is named/dated `2026-07-04` while the successor, PRD, and this spec all call it "the 2026-07-05 row-context decision" (that is the `Decision update:` date inside the 07-04 file), so the contract set appears to cite a document that does not exist. [_bmad-output/contracts/fc-nip-command-target-identity-contract-2026-08-12.md:13]
- [x] [Review][Defer] Two names for one clock abstraction — deferred, cosmetic; `CapturedAt` is sourced from "FrontComposer `TimeProvider`" while the `ObservedAt` fallback is "the Shell `TimeProvider`", with no statement that these are the same seam. [_bmad-output/contracts/fc-nip-command-target-identity-contract-2026-08-12.md:82]
- [x] [Review][Defer] Failure diagnostic has no redaction or observability contract — deferred, belongs with the Story 9.4 implementation; the contract emits a bounded diagnostic carrying `EntityKey`/`PriorStatus`/`ExpectedStatus` (business data) with no redaction, category, or level rule, and requires no suppression-rate signal, so a fail-closed implementation suppressing 100% of indicators in production is indistinguishable from a working one. [_bmad-output/contracts/fc-nip-command-target-identity-contract-2026-08-12.md:83]
- [x] [Review][Defer] Gap-evidence prohibitions have no documented retirement path — deferred, owned by Story 9.4; `ShouldNotContain("EntityKey:")` on `EventStorePendingCommandStatusQuery.cs` makes the earlier `ShouldNotContain("EntityKey: status.AggregateId")` dead, and the broad form will block Story 9.4, which is expected to add target identity to that exact file. Nothing records when these retire. [tests/Hexalith.FrontComposer.SourceTools.Tests/Docs/FcNipRowIdentityProducerContractTests.cs:179]
- [x] [Review][Defer] The nudge-seam prohibition lost its pin — deferred, low risk; "Fresh-row indicators are not produced from the projection nudge seam" survives in `project-docs/architecture.md` but the deleted guard was its only assertion and the replacement `SynchronizedTruth_` test does not re-pin it. [_bmad-output/project-docs/architecture.md:103]

## Spec Change Log

## Design Notes

The provider is the trust boundary: SourceTools may generate its registration from an explicit declaration, but generic reflection over command fields is not authoritative. Terminal materiality is independent of target intent so an existing EventStore `EventCount` or equivalent typed callback can distinguish material work from no-op without treating EventStore identity as projection identity.

## Verification

**Commands:**
- `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` -- expected: blocking .NET lane passes.
- `cd tests/e2e && npm run test:fc-nip` -- expected: all FC-NIP contract checks pass without a web server.
- `pwsh ./eng/validate-docs.ps1` -- expected: canonical and published documentation validates.

**Actual evidence (2026-08-12):**
- `dotnet build tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj --configuration Release --no-restore` and `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Release/net10.0/Hexalith.FrontComposer.SourceTools.Tests.dll -class Hexalith.FrontComposer.SourceTools.Tests.Docs.FcNipRowIdentityProducerContractTests` -- Release build passed with 0 warnings / 0 errors; 5/5 focused governance tests passed.
- `cd tests/e2e && npm run test:fc-nip` -- 6/6 browserless FC-NIP tests passed.
- `pwsh ./eng/validate-docs.ps1` -- passed; emitted `artifacts/docs/validation-manifest.json`.
- `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --no-restore` and `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -method Hexalith.FrontComposer.Shell.Tests.Governance.AnalyzerPolicyGovernanceTests.AnalyzerPolicy_IdentifierInventory_MatchesSeal` -- Release build passed with 0 warnings / 0 errors; identifier inventory guard passed 1/1 after resealing the story-owned test identifier delta.
- `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` -- 4,333/4,333 passed with 0 failures.
- `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-9-3-define-explicit-command-target-identity.md --base 8ba36a8c0494cd8f5640b4383ff2fab0742ff836` -- passed.
- `git diff --check` -- passed.

## Documented Unrelated Changes

- **Pre-existing Gate 3a failure absorbed by this story (recorded 2026-08-12, second review pass).**
  `FcNipContractReferences_WhenAuthored_NameEpicNineOwnershipInDocs` was already red at baseline
  `8ba36a8c`. Commit `730d8595` ("Update UX design and experience documentation following Epic 9
  retrospective") removed the three phrases it asserted: "Epic 9 / FC-NIP" and "current projection
  nudge does not include row identity" from `docs/reference/components/datagrid.md`, and "FC-NIP owns
  the post-MVP command outcome payload and producer wiring" from `_bmad-output/project-docs/architecture.md`.
  All three were verified absent at both `8ba36a8c` and `b50243df`. Story 9.3's first pass deleted the
  test without recording that it was already failing, which made the reported "4,333/4,333 passed with
  0 failures" true only after the deletion. The second review pass did not restore the removed phrases
  (they describe superseded ownership); instead it restored equivalent coverage through
  `SynchronizedTruth_WhenReviewed_ResolvesDecisionAndKeepsCompositionOpen`, which now pins the FC-TBL
  and FC-CMD contracts against their corrected post-9.3 wording.

**Second review pass evidence (2026-08-12):**
- `cd tests/e2e && npm run test:fc-nip` -- 6/6 browserless FC-NIP contract guards passed.
- `dotnet build tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj --configuration Release --no-restore` -- 0 warnings / 0 errors.
- `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Release/net10.0/Hexalith.FrontComposer.SourceTools.Tests.dll -class Hexalith.FrontComposer.SourceTools.Tests.Docs.FcNipRowIdentityProducerContractTests` -- 5/5 passed.
- `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -class Hexalith.FrontComposer.Shell.Tests.Governance.AnalyzerPolicyGovernanceTests` -- 7/7 passed after resealing `testInventorySha256` to `54205e5041c81e61500a5091191e9409de15dff40f003f982da066c20247c2da`. The token count stayed at 6362 because the remediation added no new test methods; only line positions moved.
- `pwsh ./eng/validate-docs.ps1` -- passed; emitted `artifacts/docs/validation-manifest.json`. Note: a first run in a workspace without generated DocFX API metadata reports ~100 spurious "API summary baseline contains resolved or missing UID" failures because `docs/reference/api/*.yml` (gitignored) is produced by that same run; re-running validates cleanly.
- Copy-local flake reproduced and cleared: the first full-solution run failed `QueryRequestDeprecationTests.NetStandardConsumer_LegacySurface_EmitsCs0618Fallback` (missing Contracts netstandard2.0 assembly), `EventStorePactContractTests` (missing `PactNet.Abstractions`), and `HydrationStateConsolidationTests` (unresolvable references). An incremental `dotnet build Hexalith.FrontComposer.slnx --configuration Release --no-restore` restored the copy-local assemblies and all three passed -- the same remedy as CI's "Gate 2e: Restore missing copy-local test references" step.

## File List

- `.github/workflows/quality.yml`
- `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json`
- `_bmad-output/contracts/fc-cmd-pending-identity-correlation-contract-2026-06-04.md`
- `_bmad-output/contracts/fc-nip-command-target-identity-contract-2026-08-12.md`
- `_bmad-output/contracts/fc-nip-row-identity-producer-contract-2026-07-04.md`
- `_bmad-output/contracts/fc-tbl-table-api-contract-2026-06-04.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/epic-9-context.md`
- `_bmad-output/implementation-artifacts/spec-9-3-define-explicit-command-target-identity.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/planning-artifacts/architecture.md`
- `_bmad-output/planning-artifacts/prd.md`
- `_bmad-output/project-docs/architecture.md`
- `docs/reference/components/datagrid.md`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Docs/FcNipRowIdentityProducerContractTests.cs`
- `tests/e2e/specs/fc-nip-row-identity-contract.spec.ts`

## Suggested Review Order

**Approved target-identity decision**

- Start with the successor boundary and explicit provider resolution model.
  [`fc-nip-command-target-identity-contract-2026-08-12.md:11`](../contracts/fc-nip-command-target-identity-contract-2026-08-12.md#L11)

- Review immutable fields and fail-closed server-assigned-key handling.
  [`fc-nip-command-target-identity-contract-2026-08-12.md:49`](../contracts/fc-nip-command-target-identity-contract-2026-08-12.md#L49)

- Confirm successor concepts map unambiguously onto historical carrier fields.
  [`fc-nip-command-target-identity-contract-2026-08-12.md:88`](../contracts/fc-nip-command-target-identity-contract-2026-08-12.md#L88)

- Trace capture, acceptance, race buffering, conflict, and timestamp separation.
  [`fc-nip-command-target-identity-contract-2026-08-12.md:104`](../contracts/fc-nip-command-target-identity-contract-2026-08-12.md#L104)

- Validate all eight target, materiality, indicator, and duplicate dispositions.
  [`fc-nip-command-target-identity-contract-2026-08-12.md:145`](../contracts/fc-nip-command-target-identity-contract-2026-08-12.md#L145)

- Check inference prohibitions and later-story ownership boundaries.
  [`fc-nip-command-target-identity-contract-2026-08-12.md:169`](../contracts/fc-nip-command-target-identity-contract-2026-08-12.md#L169)

**Synchronized product and architecture truth**

- Verify the historical base remains authoritative and links its approved successor.
  [`fc-nip-row-identity-producer-contract-2026-07-04.md:5`](../contracts/fc-nip-row-identity-producer-contract-2026-07-04.md#L5)

- Confirm D-4 is resolved while Epic 9 completion remains blocked.
  [`prd.md:528`](../planning-artifacts/prd.md#L528)

- Review planning invariants without mistaking the decision for runtime delivery.
  [`architecture.md:56`](../planning-artifacts/architecture.md#L56)

- Check published architecture mirrors pre-dispatch and fail-closed boundaries.
  [`architecture.md:103`](../project-docs/architecture.md#L103)

- Review adopter-facing wording and remaining Story 9.4–9.8 ownership.
  [`datagrid.md:83`](../../docs/reference/components/datagrid.md#L83)

**Governance and evidence**

- Inspect structural C# guards for snapshot, matrix, and no-smuggling invariants.
  [`FcNipRowIdentityProducerContractTests.cs:56`](../../tests/Hexalith.FrontComposer.SourceTools.Tests/Docs/FcNipRowIdentityProducerContractTests.cs#L56)

- Cross-check browserless parity and source-level forbidden-mapping evidence.
  [`fc-nip-row-identity-contract.spec.ts:50`](../../tests/e2e/specs/fc-nip-row-identity-contract.spec.ts#L50)

- Confirm the intentional governance identifier inventory reseal.
  [`analyzer-policy-exception-ledger-v1.json:98`](../contracts/analyzer-policy-exception-ledger-v1.json#L98)
