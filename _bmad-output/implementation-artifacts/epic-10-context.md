# Epic 10 Context: Tooling Governance Follow-Through

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Give adopter developers trustworthy FrontComposer authoring-tooling evidence: mechanically reconciled story ownership, consistent human and machine CLI output, accurate migration promises, and redacted Testing evidence. This post-MVP hardening carries forward Epic 7 retrospective actions without reopening its completed stories. The current plan marks Epic 10 complete and defines only Stories 10.1–10.5; its outcomes remain regression constraints, and the retired FR-27 identifier creates no new implementation scope.

## Stories

- Story 10.1: Mechanical story evidence reconciliation
- Story 10.2: Adopter-facing historical-label cleanup
- Story 10.3: CLI text-output parity guard
- Story 10.4: HFCM9002 production-emission decision
- Story 10.5: Testing evidence redaction default-lane guard

## Requirements & Constraints

- Story evidence must agree with actual story-owned changes and completion claims before review promotion. Reconciliation uses the story baseline and distinguishes pre-existing or explicitly unrelated work rather than assigning it to the story. Test evidence and documented blockers must support completion claims.
- Adopter documentation must describe current contracts and features without stale historical ownership labels. Non-adopter-facing provenance may remain when it accurately describes history. CLI, diagnostic, migration, Testing, and published how-to documentation must remain mutually consistent.
- Text and JSON CLI output must cover the same shared behavior for summaries, filtering, fail flags, deterministic ordering, and migration/inspection budgets. A deliberate format difference needs an explicit explanation; changing a JSON cap also requires intentional text-output and omitted-budget behavior.
- HFCM9002 production emission is not approved. Migration documentation must keep its synthetic/manual sidecar-only boundary clear and must not promise a SourceTools production emitter. The completed decision is an enduring boundary, not an unresolved choice to implement emission.
- Testing evidence is redacted by default, including tenant/user identifiers, token/secret/password values, oversized payloads, and punctuation-heavy secrets. This coverage belongs in the default Testing lane. Public evidence-emitting helpers require intentional public API baseline, README, and redaction coverage updates.
- Privacy applies across evidence, snapshots, logs, and telemetry: raw tokens, JWT contents, EventStore metadata, raw payloads, stack traces, and unrestricted personal information must not escape. Operator-relevant logging uses sanitized structured data and source-generated logging sites, with evidence proving sensitive values are absent.
- Published documentation changes must pass the documentation validation gate. Public API changes update the shipped baseline intentionally. Passing unrelated tests cannot substitute for the lane that proves the changed behavior.

## Technical Decisions

- FrontComposer retains UI composition and descriptor semantics. Under the approved McpCli direction, `Hexalith.McpCli` owns the target Hexalith CLI/MCP presentation. Proprietary FrontComposer CLI/MCP packages are obsolete migration sources, limited to continuity and safety work; replacement or approved withdrawal requires a generic contract and owner-approved parity evidence before retirement. Preserve the legacy behavior below until cutover rather than extending it into a new proprietary transport.
- Inspection reports generated forms, grids, registrations, manifests, warnings, and errors. Its JSON contract is `frontcomposer.cli.inspect.v1`; ordering and severity/failure behavior are deterministic, and absolute paths are rewritten relative to the inspected root.
- Migration uses allowlisted Roslyn transformations across supported version edges. Dry-run is the default. Apply is atomic and rejects generated output, submodule roots, symlinks, out-of-root targets, and protected build/repository directories. Its JSON contract is `frontcomposer.cli.migrate.v1`.
- SourceTools remains a netstandard2.0-clean producer referencing the Contracts kernel only. UI rendering contracts belong in Contracts.UI; runtime implementations and test fakes must not move into the kernel. Generated output is a public consumer boundary and is never hand-edited to repair drift.

## UX & Interaction Patterns

Developer tooling presents deterministic text or JSON success, filtered findings, actionable-findings failure, and unavailable-output outcomes. Paths in presented evidence are relative or redacted. Migration previews changes before an explicit apply operation and fails closed for unsafe targets. Testing failures remain useful behavioral evidence while suppressing sensitive values; a redaction regression must fail the focused/default lane.

## Cross-Story Dependencies

Epic 7 supplies the completed inspection, migration, documentation, and Testing baseline hardened here; none of its stories are reopened. Evidence reconciliation and accurate documentation support the CLI parity and redaction guards. Later Epic 11 work consumes completed Epic 10 evidence and extends the Testing harness with realistic failure/policy scenarios without reopening this epic. Current release-readiness and documentation/UX expansion work retains its separately assigned ownership and cannot be inferred from Epic 10 completion.
