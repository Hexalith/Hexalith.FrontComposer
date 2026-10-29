# Epic 15 Context: Maintainers Evolve FrontComposer Without Contract Drift

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Enable maintainers to evolve FrontComposer while preserving public contracts and exact runtime identity. Repair planning/documentation drift and compatibility baselines without rewriting accepted history or granting independent approvals. Apply current evidence policy to superseded storage requirements.

## Stories

- Story 15.1: Restore Current Artifact-Integrity Validation
- Story 15.2: Reconcile Component and Page-Pattern Documentation
- Story 15.3: Align Migration and Classification Guidance
- Story 15.4: Correct the Fluent UI V5 Contingency Identity
- Story 15.5: Reconcile the Published Compatibility Baseline
- Story 15.6: External Browser Artifacts And Bounded Accessibility Receipts (superseded)
- Story 15.7: Retire Bulky Browser Evidence (superseded)

## Requirements & Constraints

- Epic, story, and action statuses must agree. Story changes need complete, nonempty File Lists. Contradictions fail deterministically with support-safe diagnostics independent of machine paths or unrelated changes. Completed Epics 9–11 stay closed regression history.
- Component, index, status, toolbar, tab, diagnostic, migration, and skill-corpus documentation must match public APIs, state behavior, accessibility, and customization seams and pass the existing semantic documentation gate.
- Migration guidance distinguishes executable edges from manual-only package/API changes; missing or false classifications fail parity. Migration defaults to dry-run; atomic apply refuses generated, submodule, symlinked, out-of-root, bin, obj, and .git targets. Inspection preserves deterministic ordering, severity/fail behavior, relative paths, and redaction.
- Compatibility baselines use the latest evidence-backed published packages, never tags/labels alone. Intentional lag needs a dated Release Owner decision naming reason, owner, expiry, and blocking evidence. Intentional breaks need baseline, documentation, and migration/deprecation handling.
- Reuse validators, inventories, parity, PublicAPI, ApiCompat, drift, snapshot, and migration lanes. Extend missing semantics without parallel scanners/registries, reconstructed packages, or evidence bundles.
- Proof is tests plus CI. Story/spec records exact commands, pass/fail counts, commit SHA, and CI link. Raw output stays local or in CI artifacts. No committed evidence packets, manifests, runtime-identity records, replay lanes, or external stores. Evidence folders are ignored scratch; source-owned Pact, visual, and Verify baselines stay tracked. Release assets are GitHub attachments. This policy supersedes Stories 15.6/15.7, including receipts, budgets, and retirement protocol.
- Privacy covers documentation, diagnostics, tests, snapshots, and proof: no tokens, raw payloads or metadata, stack traces, or unrestricted personal information.

## Technical Decisions

- Public contracts include schema canonicalization/fingerprints, generated-output locations, diagnostic bands, CLI JSON, analyzer policy, and Contracts boundaries. Canonicalization changes invalidate drift baselines. Preserve the UI-neutral kernel, separate rendering contracts, Recommended built-in analyzers, warnings as errors, and narrow owned exceptions.
- Preserve frontcomposer.cli.inspect.v1 and frontcomposer.cli.migrate.v1. Diagnostic, migration-edge, and manual-only authorities must agree semantically.
- Hexalith.McpCli owns target CLI/MCP presentation. FrontComposer CLI/MCP packages are migration sources limited to safety/continuity fixes. Retirement needs an approved generic replacement or withdrawal with owner-approved parity evidence.
- Fluent versions remain catalog-owned; docs name the exact identity and dated approval posture. The [2026-10-21 Product Owner + Architect receipt](spec-13-9-decide-the-exact-fluent-ui-v5-posture.md#product-decision-receipt) does not approve Components + Icons 5.0.1 GA. Evidence binds the exact candidate and selecting Builds revision; equal package versions on another revision transfer no approval. Later identities need revalidation and a separate decision.
- Dependency governance uses committed objects and one active event-base policy. Candidate policy cannot authorize itself; it becomes eligible as a later event’s base. Consumer projections seal root selectors and committed MSBuild/SDK/NuGet configuration. Drift fails closed; candidate validation alone cannot prove a prior-policy transition.
- Compatibility reconciliation grants no publication approval or incident repair. Release observations stay append-only and attempt-keyed; later results cannot weaken incidents. Publication, exact EventStore runtime identity, and migration approvals retain independent evidence/owners.

## UX & Interaction Patterns

- Document canonical states and distinguish transport acknowledgement from projection confirmation. Status needs visible text and meaning beyond color/hover. Preserve WCAG 2.2 AA, keyboard, focus, routing, validation, and announcements.
- FcPageToolbar is public; internal Fluent composition remains implementation-owned. Customization resolves Level 4, Level 2, then generated defaults. Mismatch/render-fault guidance requires Shell compiled with DEBUG and a registered Development IHostEnvironment; missing environments fail closed. Guidance is bounded and never exposes raw payloads, tenant/user values, tokens, or stack traces. Testing helpers avoid application-specific selectors and hidden timing.
- Evidence-policy changes preserve accessibility semantics, manual assistive-technology/device proof, opt-in UX dispositions, and independent approvals.

## Cross-Story Dependencies

- Documentation parity requires both component documentation and migration classification.
- Fluent documentation consumes catalog identity and dated decisions without closing acceptance.
- Compatibility uses published-package/release-ledger evidence or a bounded Release Owner lag decision. EventStore approval and publication governance remain in Epics 16 and 17.
- Stories 15.6/15.7 are superseded; do not implement historical storage/retirement requirements.
