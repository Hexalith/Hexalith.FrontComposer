---
title: 'Story 9.8: Prove composed and live Epic 9 acceptance'
type: 'feature'
created: '2026-08-26'
status: in-review
baseline_commit: '6891baef28a35d4dcfc72842e454103beca54d8f'
baseline_revision: '6891baef28a35d4dcfc72842e454103beca54d8f'
story_id: '9.8'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-9-context.md'
  - '{project-root}/_bmad-output/contracts/fc-nip-command-target-identity-contract-2026-08-12.md'
warnings: []
deferred: []
---

<intent-contract>

## Intent

**Problem:** Epic 9 still lacks acceptance evidence that one real generated create/update flow crosses target capture, accepted pending registration, terminal resolution, indicator state, and an already-rendered generated grid. The approved contract's core rule permits a standalone create whose exact `EntityKey` is known before dispatch, but its downstream-ownership paragraph contradicts the canonical backlog by declaring a missing Story 9.9 preallocation prerequisite for every live standalone-create route.

**Approach:** Use one fresh exact `EntityKey` known before dispatch and carried by the real typed create command; the typed provider reports that same key without allocating it or mutating its cloned command, and the backend materializes the projection row under that key. Reconcile the contract's contradictory Story 9.9 ownership paragraph and pinned guards, then add one cohesive generated-command-to-grid composition lane and a repeatable AppHost-backed Playwright lane that preserves command logs and browser artifacts. Keep existing focused tests as supporting evidence, not substitutes for the composed and live gates.

## Boundaries & Constraints

**Always:** Exercise generated forms and `CounterProjectionView` in one scoped host using the real pending state, `PendingCommandOutcomeResolver`, and `NewItemIndicatorStateService`. Cover provider create, declared row-context update, provider cross-row/status move, early callback replay, and polling with their indicator/no-indicator results. For provider create, establish one fresh exact key before submission, carry it through the dispatched typed command, return it from the provider, and prove that the previously absent row materializes under that same key. Render the grid before mutation and prove automatic localized accessible DOM updates for materialization, filter/re-query, TTL, clear, tenant/user transition, and atomic first-wins without `cut.Render()`. Start FrontComposer through isolated Aspire orchestration, wait for and discover `counter-web`, preserve structured redacted command/log and browser evidence, and never stop unrelated AppHosts. Keep `_bmad-output/implementation-artifacts/sprint-status.yaml` read-only.

**Block If:** The provider reports a key that was not fixed before dispatch, the dispatched create does not use that same key, the backend does not materialize the matching projection row, or the live proof relies on a pre-seeded row, direct indicator-state mutation, or focused-test substitution. Block on any live run that cannot start without colliding with or stopping the unrelated running AppHost; record the exact failed command rather than substituting focused tests.

**Never:** Infer identity from projection nudges, `AggregateId`, routes, property-name conventions, visible rows, diffs, opaque result payloads, or undeclared ambient row context. Do not implement framework preallocation or post-dispatch identity in this story; creates whose exact key is first allocated after dispatch remain indicator-ineligible. Do not change public APIs, edit generated `obj/` output or submodules, weaken materiality/scope/first-wins rules, write or revert sprint tracking, or mark the story complete without durable live browser evidence.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Eligible create/update | Explicit target captured before dispatch; for create, one fresh exact key is carried by the dispatched command and reported by the provider; material confirmation via callback or poll | The previously absent create row materializes under that exact key and one matching-lane indicator appears through the real generated grid; pending entry is terminal | Key mismatch, server allocation only after dispatch, or unknown identity/materiality suppresses only FC-NIP; lifecycle remains truthful |
| Cross-row/status move | Provider resolves a destination distinct from the source | Only the declared destination lane/entity is marked | Missing or contradictory destination metadata fails closed |
| Duplicate active row | Duplicate or distinct message targets an active `(ViewKey, EntityKey)` | First message, capture time, timer, and one live announcement remain unchanged | Suppress the later publication without rerendering |
| Grid invalidation | Materialization, filter/re-query, TTL, clear, or scope transition after initial render | DOM removes the indicator automatically and previous-scope data is unreadable | Disposal and race paths remain bounded |
| Live environment failure | Isolated AppHost or browser lane cannot run safely | Exact command and blocker are recorded; Story 9.8, Epic 9, FR-13, and FR-26 remain open | Unit/bUnit success is not substituted |

</intent-contract>

## Code Map

- `_bmad-output/contracts/fc-nip-command-target-identity-contract-2026-08-12.md:100-106,273-276` -- authoritative pre-dispatch create eligibility; the 2026-08-26 human resolution authorizes reconciling the contradictory Story 9.9 blocker while preserving suppression for keys first allocated after dispatch.
- `_bmad-output/planning-artifacts/epics.md:1520-1541` -- canonical Story 9.8 acceptance omits Story 9.9 and requires composed plus live proof.
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CommandTargetGeneratedFormTests.cs:163-610` -- existing form-to-resolver callback, polling, create, delete, and status-move islands to compose rather than duplicate.
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterStoryVerificationTests.cs:145-253` -- existing real generated-grid invalidation island; it currently calls indicator state directly.
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CommandRendererTestFixtures.cs:130-216` -- generated provider/SameAsSource fixtures; lacks successful provider cross-row update.
- `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs:651-727,1107-1245` -- immutable target capture, early callback buffering, and accepted association path.
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandOutcomeResolver.cs:105-246,375-440` -- single terminal owner and eligible publication boundary.
- `src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs:1040-1088,1433-1494,1884-1889` -- generated-grid subscription, dismissal, and indicator rendering boundary.
- `samples/Counter/Counter.Web/Components/Pages/CounterPage.razor:21-59` and `samples/Counter/Counter.Domain/*.cs` -- live sample has update forms outside generated row identity and no provider create path.
- `src/Hexalith.FrontComposer.AppHost/Program.cs:141-146` and `tests/e2e/playwright.config.ts:1-55` -- AppHost counter resource and current direct-`Counter.Web` browser harness.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` -- orchestrator-owned bookkeeping; never write or revert.

## Tasks & Acceptance

**Execution:**
- [x] `_bmad-output/contracts/fc-nip-command-target-identity-contract-2026-08-12.md:273-276`, `_bmad-output/implementation-artifacts/deferred-work.md:29-31`, `tests/Hexalith.FrontComposer.SourceTools.Tests/Docs/FcNipRowIdentityProducerContractTests.cs`, and `tests/e2e/specs/fc-nip-row-identity-contract.spec.ts` -- reconcile the downstream Story 9.9 blocker, deferred note, and pinned guards with the authoritative rule at contract lines 100-104: a provider-reported exact key already known before dispatch is sufficient for Story 9.8, while server-allocated-after-dispatch keys remain suppressed and framework preallocation remains separate deferred work. Do not change the canonical Epic 9 dependency on Stories 9.3-9.7.
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Generated/Epic9CompositionTests.cs` and `CommandRendererTestFixtures.cs` -- add the single-scope composed callback/polling and generated-grid matrix.
- [x] `samples/Counter/**` -- expose approved generated create/update targets through the AppHost sample without ambient identity fallback; the provider-backed create must dispatch and materialize the same fresh pre-dispatch key rather than allocate through provider mutation.
- [x] `tests/e2e/specs/epic-9-fresh-row-acceptance.spec.ts`, `tests/e2e/package.json`, and CI artifact upload -- automate the AppHost-discovered browser lane, prove the fresh key is absent before submission and appears afterward in both the projection row and indicator, and retain redacted command logs, screenshots/traces, and JUnit/HTML evidence.
- [x] `_bmad-output/implementation-artifacts/tests/9-8-*` -- record the full commit, exact Aspire/Playwright commands, resource endpoint, results, artifact paths, and checksums after a successful live run.

**Acceptance Criteria:**
- Given Stories 9.3-9.7 are complete and a standalone create carries an exact provider-reported key known before dispatch, when the composed lane runs, then every required command shape crosses generated command, pending registration, resolver, indicator service, and already-rendered generated-grid boundaries with the specified indicator disposition.
- Given the grid is already rendered, when each state mutation and scope transition occurs, then the localized `role="status"` and `aria-live="polite"` DOM updates automatically, remains lane/scoped, and preserves first-wins provenance.
- Given the isolated FrontComposer AppHost reaches `counter-web`, when the Playwright lane runs against its discovered endpoint, then the create row is absent before submission, the dispatched create materializes that row under the exact captured key, and create/update plus dismissal/scope/first-wins evidence are preserved as durable redacted artifacts without stopping another AppHost.
- Given live verification is environment-blocked, when the run terminates, then the exact command and blocker are recorded and no focused lane is represented as Story 9.8 completion.

## Spec Change Log

- 2026-08-26 -- Human escalation resolution selected the pre-dispatch-known provider-key route. Story 9.9 no longer blocks Story 9.8; framework preallocation and post-dispatch identity remain out of scope and server-allocated-after-dispatch keys remain indicator-ineligible.
- 2026-08-26 -- Implemented the composed matrix, real Counter create/update lane, isolated Aspire/Playwright proof runner, credential-redacted artifact validation, CI upload, and durable evidence record. Moved the story to in-review.

## Review Triage Log

## Design Notes

Human Product/Architecture resolution on 2026-08-26 chose the contract's existing pre-dispatch-known-key rule for Story 9.8. The provider reports an exact key already fixed for the real dispatched create; it does not allocate by mutating its cloned command. The live proof must show that same previously absent key materializes as the projection row and indicator, so a hard-coded, pre-seeded, or direct-state fixture cannot close the epic. The contradictory downstream Story 9.9 blocker, deferred note, and pinned guards are authorized for reconciliation without weakening the core pre-dispatch/fail-closed rule. Framework preallocation for server-allocated keys and any post-dispatch identity proof remain separate deferred capabilities.

## File List

- `.github/workflows/quality.yml` -- adds the Linux AppHost-backed Epic 9 acceptance job and artifact upload.
- `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json` -- reseals the governed test identifier inventory for the new composed tests.
- `_bmad-output/contracts/fc-nip-command-target-identity-contract-2026-08-12.md` -- reconciles the Story 9.9 contradiction without weakening pre-dispatch identity.
- `_bmad-output/implementation-artifacts/deferred-work.md` -- retains framework preallocation as deferred but removes the false Story 9.8 blocker.
- `_bmad-output/implementation-artifacts/spec-9-8-prove-composed-and-live-epic-9-acceptance.md` -- records the story contract, completion, verification, and reconciliation.
- `_bmad-output/implementation-artifacts/tests/9-8-live-acceptance.md` -- records exact live commands, endpoint, results, artifact paths, and checksums.
- `eng/run-epic9-live-proof.sh` -- orchestrates isolated Aspire startup, endpoint discovery, browser proof, redaction, validation, checksums, and safe cleanup.
- `samples/Counter/Counter.Domain/CreateCounterCommand.cs` -- defines the typed generated create command carrying the pre-dispatch exact key.
- `samples/Counter/Counter.Domain/UpdateCounterCommand.cs` -- defines the typed generated update command carrying the exact target key.
- `samples/Counter/Counter.Web/Components/Pages/CounterCommandProjectionCatchUp.razor` -- applies terminal sample events to the live projection and exposes non-sensitive proof counters.
- `samples/Counter/Counter.Web/Components/Pages/CounterPage.razor` -- renders generated create/update forms, the catch-up bridge, and an already-rendered grid seeded only with an unrelated row.
- `samples/Counter/Counter.Web/CounterCommandProjectionCatchUpChannel.cs` -- snapshots pre-dispatch typed commands and publishes scoped confirmed materialization events without history.
- `samples/Counter/Counter.Web/CounterSampleCommandLog.cs` -- defines the non-sensitive structured terminal log event.
- `samples/Counter/Counter.Web/CounterSampleCommandService.cs` -- wraps the authorized lifecycle service, captures commands before dispatch, and forwards them unchanged.
- `samples/Counter/Counter.Web/CreateCounterTargetIdentityProvider.cs` -- reports the create command's exact key without allocation or mutation.
- `samples/Counter/Counter.Web/Program.cs` -- registers providers, catch-up services, and the authorized sample wrapper.
- `samples/Counter/Counter.Web/UpdateCounterTargetIdentityProvider.cs` -- reports the update command's exact key without ambient inference.
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CommandRendererTestFixtures.cs` -- adds the generated provider cross-row fixture.
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterStoryVerificationTests.cs` -- pins the sample's unrelated seed and already-rendered generated grid.
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/Epic9CompositionTests.cs` -- composes callback, polling, provider, declared-context, invalidation, scope, and first-wins acceptance.
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/Epic9CreateProjectionEffects.cs` -- supplies deterministic test-only create materialization.
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/Epic9CrossRowTargetIdentityProvider.cs` -- resolves the composed cross-row destination.
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/Epic9DeleteTargetIdentityProvider.cs` -- resolves the composed delete disposition.
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/Epic9PendingCommandStatusQuery.cs` -- supplies the deterministic fallback-polling observation.
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/Epic9ScriptedCommandService.cs` -- scripts callback timing and captures real dispatched command snapshots.
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/Epic9StatusMoveTargetIdentityProvider.cs` -- resolves destination and prior/expected status slots.
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/Epic9UserContextAccessor.cs` -- drives deterministic tenant/user scope transitions.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Docs/FcNipRowIdentityProducerContractTests.cs` -- pins the reconciled producer contract.
- `tests/e2e/package.json` -- adds live proof and artifact validation scripts.
- `tests/e2e/playwright.config.ts` -- supports isolated output, HTML/JUnit paths, and trace retention.
- `tests/e2e/scripts/validate-epic9-artifacts.mjs` -- validates commit/endpoint correlation, required evidence, dismissal/first-wins claims, and credential redaction.
- `tests/e2e/specs/epic-9-fresh-row-acceptance.spec.ts` -- proves live exact-key create/update, accessible indicator, first-wins, and materialization dismissal.
- `tests/e2e/specs/fc-nip-row-identity-contract.spec.ts` -- pins the reconciled browser-side contract wording.

## Known Blockers

- Pre-existing dependency catalog blocker: the exact default solution test command stops at `NU1109` because `FsCheck.Xunit.v3 3.3.4` requires `FsCheck 3.3.4`, while the root central catalog selects `FsCheck 3.3.3`. Story 9.8 did not change dependencies or submodules.
- Pre-existing release governance blocker: the full Shell fallback passes 2,656 of 2,657 tests; the sole failure reports root release workflow Builds SHA `4eb33928a1d8c7775f97221cf9edc171db0cb5f8` differs from the approved current Builds submodule SHA.
- Process constraint: the implementation remains uncommitted by policy. Live metadata records full HEAD `6891baef28a35d4dcfc72842e454103beca54d8f` with `workingTreeDirty: true`; the story validator reconciles the workspace snapshot, and the integrator may refresh browser evidence after creating the final Story 9.8 commit.

## Verification

**Commands:**
- `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --configuration Release --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` -- expected: the default lane passes after composed coverage is added.
- `npm --prefix tests/e2e run typecheck` -- expected: the browser acceptance source type-checks.
- `aspire start --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --isolated --non-interactive --format Json` followed by `aspire wait counter-web` and the AppHost-discovered Story 9.8 Playwright command -- expected: create/update evidence passes and artifacts are retained without touching unrelated AppHosts.
- `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-9-8-prove-composed-and-live-epic-9-acceptance.md --candidate HEAD` -- expected: every Story 9.8 commit and file is reconciled.

**Results:**
- Final live run passed 1/1 Playwright test against Aspire-discovered `https://localhost:37287`; credential-redacted JSON, JUnit, HTML, screenshot, trace, browser evidence, and complete checksums are retained under `artifacts/epic-9/` and described in `_bmad-output/implementation-artifacts/tests/9-8-live-acceptance.md`.
- E2E TypeScript type-check passed; focused contract guards passed 5/5; focused Story 9.8 composition plus seeded-grid checks passed 3/3; the governed identifier seal passed in the final full run.
- Full Shell fallback passed 2,656/2,657 in 2m37s with only the documented unrelated release pin failure.
- The exact default solution lane is blocked at restore by the documented `FsCheck` central-catalog mismatch.
