# GOV-1 Owner Decisions

These decisions remain open after architecture and source reconciliation. Architecture adoption does
not imply Product Owner or Release Owner acceptance, and no item has a default answer.

| ID | Decision | Owner | Current disposition | Closure evidence |
| --- | --- | --- | --- | --- |
| OD-1 | Accept or revise AD-19, the production-release halt, and the no-exception posture | Product Owner + Release Owner | Open under PRD D-16/G-8 | Dated decision citing the finalized spine and reconciled sources |
| OD-2 | Apply and verify the literal-false emergency stop | Release Owner or repository administrator | Open; the spine records the 2026-09-09 observation as literal `true` | Authenticated API value and server timestamp for `HEXALITH_RELEASE_PUBLISH_ENABLED=false` |
| OD-3 | Accept the immutable `split-publication-v1` successor into AD-16 lineage | Hexalith.Builds owner + Release Owner | Open; `a8a50859…` is the lineage predecessor only | Accepted 40-hex revision plus exact reusable/action closure evidence |
| OD-4 | Approve the incident acknowledgement/containment target and runbook | Product Owner + Release Owner | Open | Approved `docs/release-incident-response.md` plus exercise or tabletop evidence |
| OD-5 | Establish the no-bypass protected-main process used by conformance and ledger approvals | Release Owner | Open | Authenticated ruleset/protection evidence matching the spine |
| OD-6 | Authorize any bounded risk exception | Product Owner + Release Owner + Architect | No exception authorized | Separate dated decision naming scope, expiry, evidence, compensating controls, migration/revocation trigger, and approvers |

Until OD-1 through OD-5 close, production release approval and dispatch remain ineligible. OD-6 stays
closed to "no exception" unless its full evidence contract is satisfied; environment approval alone
cannot create an exception.
