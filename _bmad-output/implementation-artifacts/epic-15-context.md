# Epic 15 Context: Maintainers Evolve FrontComposer Without Contract Drift

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Enable framework maintainers to customize, inspect, migrate, document, test, and upgrade FrontComposer while preserving public contracts and exact runtime identity. Reconcile current documentation and planning drift and maintain evidence-backed compatibility baselines without changing accepted delivery history or conferring separately owned approvals. Apply the current owner evidence policy wherever older story text specifies superseded storage mechanisms.

## Stories

- Story 15.1: Restore Current Artifact-Integrity Validation
- Story 15.2: Reconcile Component and Page-Pattern Documentation
- Story 15.3: Align Migration and Classification Guidance
- Story 15.4: Correct the Fluent UI V5 Contingency Identity
- Story 15.5: Reconcile the Published Compatibility Baseline
- Story 15.6: External Browser Artifacts And Bounded Accessibility Receipts (superseded)
- Story 15.7: Retire Bulky Browser Evidence (superseded)

## Requirements & Constraints

- Current epic, story, and action statuses must agree, and story-owned changes must reconcile with complete, nonempty File Lists. Contradictions fail deterministically with support-safe diagnostics independent of machine paths or unrelated working-tree changes. Completed Epics 9–11 remain accepted regression history; residual work does not reopen their delivery records.
- Component, index, status, toolbar, tab, diagnostic, migration, and skill-corpus documentation must describe the implemented public contract. Check semantic content and identifier resolution, including supported APIs, state behavior, accessibility, and customization seams. Published documentation changes pass the existing documentation gate.
- Migration guidance distinguishes executable version edges from explicitly manual-only package/API changes. Unclassified or falsely executable edges fail parity checks. Migration remains dry-run by default; apply is atomic and refuses generated, submodule, symlinked, out-of-root, bin, obj, and .git targets. Inspection preserves deterministic text/JSON ordering, severity/fail behavior, relative paths, and redaction.
- Compatibility baselines advance only to the latest evidence-backed published packages. Tags and labels alone are insufficient. Intentional lag requires a dated Release Owner decision identifying reason, owner, expiry, and blocking evidence. Intentional breaking changes require baseline, documentation, and migration/deprecation handling.
- Reuse existing validators, inventories, parity checks, PublicAPI, ApiCompat, snapshots, drift, and migration lanes. Extend missing semantic coverage without creating parallel registries, compatibility scanners, package reconstruction, or evidence bundles. Preserve exact focused commands and results.
- Proof is repository tests plus CI. The owning story or spec records commands, pass/fail counts, commit SHA, and CI run link. Raw reports, traces, screenshots, logs, and binlogs stay local or in CI workflow artifacts. Do not create committed evidence packets, manifests, runtime-identity records, replay lanes, or external evidence stores. Every evidence folder is ignored scratch; source-owned Pact, visual, and Verify baselines remain tracked. Release assets are GitHub release attachments. This owner policy supersedes Stories 15.6/15.7 and their former receipts, storage budgets, and retirement protocol.
- Privacy applies to documentation, runtime diagnostics, tests, and recorded proof: never expose tokens, unrestricted personal information, raw payloads or metadata, or stack traces.

## Technical Decisions

- Governed public contracts include schema canonicalization and fingerprints, generated-output locations, HFC diagnostic allocations, CLI JSON, analyzer policy, and Contracts boundaries. Canonicalization changes invalidate drift baselines. Preserve the UI-neutral Contracts kernel, separate rendering contracts, Recommended built-in analyzers, warnings as errors, and narrowly owned exceptions.
- Preserve named machine-readable formats frontcomposer.cli.inspect.v1 and frontcomposer.cli.migrate.v1. Diagnostic identities, executable migration edges, and published manual-only guidance have distinct authorities whose semantic contents must agree.
- Hexalith.McpCli owns the target CLI/MCP presentation. Existing FrontComposer CLI/MCP packages are obsolete migration sources limited to safety and continuity work. Retirement needs an approved generic replacement or explicit withdrawal with owner-approved parity evidence.
- Fluent versions remain catalog-owned. Documentation resolves the exact selected package identity and reports its actual dated approval posture. Package selection grants no approval; Fluent/Product acceptance remains separately owned.
- Compatibility reconciliation does not authorize publication or repair release-ledger incidents. Release evidence is append-only and attempt-keyed; later observations never weaken historical incidents. Publication governance, current EventStore tuple verification, and migration approval retain their independent evidence and ownership requirements.

## UX & Interaction Patterns

- Document canonical projection and command states, distinguishing transport acknowledgement from projection confirmation. Status has visible text and accessible meaning beyond color or hover. Preserve WCAG 2.2 AA, focus, keyboard, routing, validation, and announcement contracts.
- FcPageToolbar is the public toolbar contract; its internal Fluent composition remains implementation-owned. Customization resolves full-view overrides before projection templates and generated defaults. Mismatch/render-fault guidance is bounded and Development-only. Testing helpers avoid application-specific selectors and hidden timing assumptions.
- Evidence-policy changes preserve accessibility acceptance semantics, required manual assistive-technology/device proof, opt-in UX review dispositions, and separate approval gates.

## Cross-Story Dependencies

- Component documentation and migration classification can complete independently; overall documentation parity requires both.
- Fluent contingency documentation consumes the catalog identity and the dated decision without closing Fluent or Product approval gates.
- Compatibility reconciliation consumes published-package/release-ledger evidence and any Release Owner lag decision. Exact EventStore runtime approval and publication governance remain in Epics 16 and 17.
- Stories 15.6/15.7 are superseded by the 2026-10-23 owner evidence policy and completed simplification work; do not implement their historical external storage or retirement requirements.
