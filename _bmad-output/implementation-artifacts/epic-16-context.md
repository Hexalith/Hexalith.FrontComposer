# Epic 16 Context: Maintainers Verify and Approve Exact Runtime Compatibility

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Let the FrontComposer, EventStore, and Release maintainers bind one exact runtime tuple to live compatibility evidence and then, separately, to named migration approval, without rewriting historical identity records. The tuple is: FrontComposer commit, EventStore source gitlink, Builds gitlink, and the catalog-owned EventStore package version. This epic closes the release-readiness gate for EventStore runtime identity (G-3, carried forward from Epic 11 action E11R-AI-1). Story 16.1 now records the later green successor e0eef14ddbbcd03823863a4f417a4b07f6f5bafc with fully successful primary Quality43755163930 and engineering acceptance. Earlier failed/cancelled captures remain history; named migration approvals and exact lineage resolution are still open.

## Stories

- Story 16.1: [I · EVT-ID-1] Capture the Current EventStore Runtime Tuple
- Story 16.2: [A · EVT-XFER-1 · Conditional] Transfer Migration-Approval Ownership Explicitly
- Story 16.3: [A · EVT-APP-1] Approve or Reject the Exact Runtime Migration

## Requirements & Constraints

- **Tuple contents.** Record four identities as the repository selects them at execution: FrontComposer HEAD, the EventStore source gitlink, the Builds gitlink, and the EventStore package version from the selected Builds catalog. Each commit is the full lowercase SHA. FrontComposer consumes EventStore in both source and package modes, so the gitlink and the package version are distinct identities. Record both; never assume they match. Story 16.1 is owned by the runtime maintainer and the Architect.
- **Tuple evidence.** Link a passing Quality workflow run on the same FrontComposer commit. The run must cover Gate 2c (live Pact provider verification) and the `epic9-live-acceptance` AppHost lane. Results from Story 11.24, Story 11.25, the withdrawn v3 candidate, or any other tuple are history and cannot satisfy this requirement. A green result from an unrelated lane never substitutes for these lanes.
- **Evidence policy.** Proof is the repository's tests plus CI. The story or spec records the commands run, pass/fail counts, the commit SHA, and the CI run link. That record is the only committed artifact. Do not add an identity schema or validator, an evidence packet or manifest, a package ledger, a replay lane, or a reconstructed bundle. Raw output stays local or in CI artifacts. Older wording that mentions a "packet", "record digest", or "artifact hashes" now means the owning story or spec record plus its CI run.
- **Immutability and drift.** Never edit a recorded tuple. If FrontComposer HEAD, either gitlink, or the catalog package version changes, record a new tuple with a new CI run link. Prior records and completed Stories 11.24 and 11.25 stay untouched. The 2026-09-22 snapshot (EventStore `db1e9d73…`, package `3.106.0`, Builds `2fba3497…`) is history, not the target.
- **Evidence and approval are separate.** The 16.1 record claims no migration approval, carries no sign-off, and does not close G-3. Only implementation items (I) can claim repository completion. Approval items (A) close only through their named, durable decision receipt. A missing receipt or a rejection keeps G-3 visibly open.
- **Who must approve.** The EventStore maintainer, the FrontComposer maintainer, and the Release Owner each record approve or reject against the exact 16.1 tuple. A missing, stale, role-inferred, or tuple-mismatched receipt leaves G-3 open. Role equivalence is never inferred: FrontComposer or Release ownership does not imply EventStore authority.
- **Approval scope.** Approvals cite the existing evidence instead of copying it, and no evidence workflow is created just for approval. A rejection creates a new bounded residual item only when corrective work is needed.
- **Support-safe records.** Records and diagnostics must not contain tokens, JWT payloads, raw EventStore metadata or event payloads, stack traces, or unrestricted PII.

## Technical Decisions

- The release review compares the recorded tuple with the candidate's gitlinks and catalog contents. A missing record, a tuple mismatch, a failing provider run, or a missing required signature blocks the release (G-3). Historical SHAs must not be embedded as compatibility constants.
- The selected Hexalith.Builds catalog owns package versions. Do not pin or override the EventStore version locally to manufacture a match.
- Use only root-declared submodules. Never initialize or update nested submodules recursively, and never move gitlinks or edit submodule files to produce a tuple. Record what HEAD selects.
- Reuse the existing Pact consumer/provider, provider-verification, and CI lanes. Pact JSON baselines remain source-owned and tracked.
- Rolling back the EventStore identity was rejected, and no semantic-compatibility exception is approved. This epic adds no adapter, topology, or behavioral migration.
- Decision receipts follow the precedent set by Stories 13.8 and 13.9: a dated decision-receipt section in the owning spec that cites exact SHAs and the CI run URL.

## Cross-Story Dependencies

- 16.1 must come before 16.3. Approval covers only the exact 16.1 tuple; if the tuple drifts before approval, it must be captured again first.
- Story 16.2 runs only when no distinct EventStore maintainer can approve (open item OI-18). In that case, the Product Owner, Architect, and Release Owner record a dated transfer before 16.3. The transfer names the receiving role and the accountable person or group, plus the trigger, required evidence, due condition, and milestone effect. The receiving owner replaces only the transferred role and inherits the same review duty; the transfer approves nothing. If no transfer is needed, record 16.2 as "not invoked" rather than fabricating a transfer.
- Epic 15's compatibility reconciliation (Story 15.5) grants no EventStore approval. Epic 17's final Product readiness decision waits on G-3 evidence from this epic. Repository work in Epic 16 can proceed independently, but its acceptance waits for the exact evidence and receipts.

## Approved qualification and decision cadence — 2026-11-15

**Approved scheduling rule (2026-11-15).** Reserve one qualification/decision window of at most 24 hours, starting immediately before the separate seal push. Capture within two hours of the required same-attempt green results and schedule the three named migration decisions within that window. A repository-wide hold requires coordination with all active sessions and the workspace automation owner; a session-only hold does not satisfy or remove that prerequisite. Hold unrelated commits and dependency/catalog/policy changes only within the coordinated authority; external automation or coordination requires its owner's explicit authorization. An urgent fix ends the attempt. The Release Owner, with Architect involvement, records a bounded disposition within four working hours of drift, missing authority, failed qualification, timeout or a latest-main lineage conflict: schedule one fresh attempt, reject with corrective work, or request an explicit planning amendment. End the hold at decision or deadline; never extend it silently. G-3 stays open without exact evidence and the required decisions.

**Candidate lineage.** Approval covers only the exact tested commit C. Record/receipt descendants are distinct commits. A latest-main mismatch requires evidence or an explicitly approved planning resolution; no prior receipt transfers automatically. The record commit is not the tested tuple. No documentation exemption, semantic-compatibility exception or release-selector change is authorized.

**Current incident clarification (2026-11-15).** Repository-wide coordination was not achieved; it remains an open Release Owner action with Architect involvement, not a prerequisite silently removed by the session-only implementation. No working-hours calendar was supplied. Use conservative UTC disposition due time `2026-11-15T21:49:35Z`, no later than four elapsed hours after first drift `2026-11-15T17:49:35Z`; this defines no business calendar or named owner receipt and does not extend the outer deadline `2026-11-16T17:27:10Z`. Failed qualification means failed successor required steps; the expected seal-transition refusal under the previous policy is not itself a failed successor qualification. Other approved triggers, roles and deadlines remain unchanged.

For each new capture, evaluate `HexalithEventStoreVersion` per consuming project, configuration, target framework and source/package mode. Identify the exact EventStore source or package identity exercised by each runtime lane. A source lane proves no package-mode runtime compatibility. The requesting user's engineering/planning approval does not establish any EventStore approval authority or close G-3.

## Current capture status — 2026-11-15

Story 16.1 engineering capture is done for the later green successor `e0eef14ddbbcd03823863a4f417a4b07f6f5bafc`, primary automatic Quality43755163930 attempt1: all eight jobs succeed, including catalog, four Gate2c and all27 Epic9 steps. Default lane has6685 passes/zero failures or skips; all five former failed invocations pass. Primary browser evidence has one final passing test with one configured-retry flaky outcome. Supplemental isolated main manual43755164106 and CI43755164227 also succeed, but no supplemental counts enter the capture. D4 property scope covers35 source consumer contexts/27 projects, seven standalone SDK42 controls and own AppHost/UI Release controls; nonconsumer Contracts/Schema negotiation, launch, restored-package and runtime coverage limits remain explicit. Named migration receipts, authority/transfer decisions and the Release Owner/Architect coordination/lineage disposition remain open. No evidence or approval transfers to a record descendant.
