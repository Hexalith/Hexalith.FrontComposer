# Approved Course Correction: Bounded Browser Evidence Storage

Date: 2026-10-21
Status: Approved by Administrator in the course-correction conversation, with the reply “approve”
Scope: Moderate; FrontComposer planning application and PO/developer handoff

## Approval and Source

The full cross-repository proposal is owned by Hexalith.EventStore at `_bmad-output/planning-artifacts/sprint-change-proposal-2026-10-21.md`, prepared against EventStore `77c3300c12c07494530e1f203389d3147d04ea37` and FrontComposer `248cdb7aac650cab015ab49a45210f61b80e2f43`. The reviewed proposal bytes have SHA-256 `5c8539222acbd91bc7c514bd8ca88ab5164f73f1b08ed24ee97ec092de126d51`. The proposal's later application record does not change that reviewed input identity. This local amendment records the approved FrontComposer slice without copying run evidence or the full cross-repository report.

Approval covers the storage policy, four backlog work packages, shared `/pushall` selection guard, dependency-safe retirement, and the planning updates. It is a conversation approval, not a cryptographic evidence seal or independent Product/Release/UX acceptance. No new approval is inferred for existing readiness, publication or compatibility gates.

## Applied Planning Changes

- PRD §7 adds the generated-evidence storage constraint; §5.7 retains durable release evidence and append-only dispositions.
- Architecture's FR-24 evidence guidance specifies verified external materialization while preserving publisher authorization boundaries.
- UX Governance Rules retain AE-01–AE-09, manual evidence, accessible behavior and tracked visual baseline fixtures.
- Epic 15 adds Story 15.6, External Browser Artifacts And Bounded Accessibility Receipts, and Story 15.7, Retire Bulky Browser Evidence. Both tracker entries are `backlog`.
- Story 13.6 receives a dated storage amendment; the completed status and accepted scope of 13.6/13.7 remain unchanged.

## Approved Storage Rules

Generated qualification evidence retains one bounded candidate/run/attempt summary and content-bound references in Git. Full output and generated reports, traces, videos, screenshots, history captures, and archives use external artifact storage. Acceptance-relevant bytes remain durably retrievable and verified; missing or mismatched proof fails its consuming gate. New attempt metadata totals at most 10 MiB, command excerpts at most 2,048 UTF-8 bytes, and automatic Git admission omits blobs over 10 MiB and generated-evidence selections over 50 MiB with an explicit warning. Archive splitting cannot bypass these limits. Existing bound evidence is retired only through an enumerated consumer migration preserving historical identities and dispositions.

Diagnostics default to 30-day retention. Enduring accepted proof needs durable immutable retrieval, and active failures/holds retain required bytes until disposition. Redaction precedes storage. Missing, expired, corrupt or substituted output cannot pass the consuming validator. Full output lives outside the checkout or in explicitly ignored CI staging paths. Existing source-owned snapshot baselines remain tracked.

## Implementation Handoff

The Product owner/backlog maintainer hands these approved work packages to FrontComposer development/QA:

1. Start Story 15.6: route existing Playwright output variables through one external run root, retain one bounded result/attempt summary, and adapt the existing validators to verified materialized proof. Keep engine/run/test/retry and PNG integrity checks, manual evidence and owner approvals intact. Reuse focused lanes; do not introduce duplicate report workflows.
2. Complete Story 15.7 only after 15.6 and the consumer migration pass: enumerate exact paths, hashes, consumers and durable artifact references; append preservation amendments; preserve bounded offline fixtures; verify clean retrieval and the post-removal candidate; then remove the selected tracked output with `git rm` and report measured working-tree savings.
3. Coordinate with Story 15.1 for current artifact integrity. Hexalith.AI.Tools owns the shared `/pushall` prerequisite: warn/omit generated artifacts and blobs over 10 MiB, cap newly selected generated evidence at 50 MiB, select complete receipt groups, preserve excluded user changes and index state, and report remaining omissions. Do not execute `/pushall` as part of this planning change.
4. EventStore owns corresponding 3.22/3.23 work and its P1R/OQ8/security consumers. No FrontComposer story removes EventStore data or updates another workspace's submodule pointers.

Verification for implementation includes `node --test scripts/a11y-state-evidence.test.mjs`, `npm run validate:a11y-artifacts` against materialized proof, and a focused existing accessibility lane from `tests/e2e`. Unit/fixture and negative identity checks remain meaningful; no wholesale expensive browser rerun is required solely to record this plan.

## Completion and Limits

Course-correction planning is complete: requirements, epic stories, tracker entries and this handoff are applied. The storage code, `/pushall` distribution, artifact publication, fixture migration and evidence removal remain implementation work in the named backlog stories. No evidence bytes, release ledger rows, historical receipt identities, submodule pointers or Git history are changed by this application.
