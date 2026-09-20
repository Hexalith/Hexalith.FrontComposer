---
title: 'Reconcile Current EventStore Runtime Identity'
type: 'bugfix'
created: '2026-09-20'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/architecture.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-eventstore-builds-runtime-compatibility-successor.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The current root gitlinks select EventStore `2cf9bf49b4858db4b83a05e47b7bd280dc51c826` and Builds `2fba3497043fe5ffcfe4dc44c51a09eae9b950ab`, while the unsealed current-compatibility target still selects EventStore `ba7ac196e60db8820525961791eccfacec24633f` and Builds `410bd595f9e1c0f686e1edde7699517c47c8f126`, causing the default Shell governance lane to fail.

**Approach:** Align only the current/successor validator, workflow, governance assertion, and operator documentation with the checked-in gitlinks and catalog version `3.106.0`; preserve sealed identity v3, all historical approval/evidence bytes, the fail-closed gate, and every Story 11.29 surface.

</frozen-after-approval>

## Implementation Notes

- Retargeted only the unsealed successor/current tuple to EventStore `2cf9bf49b4858db4b83a05e47b7bd280dc51c826`, package `3.106.0`, and Builds `2fba3497043fe5ffcfe4dc44c51a09eae9b950ab` across the validator, Quality workflow, governance assertion, and operator guide.
- Preserved the sealed v3 tuple and SHA, all v1-v3 contracts and evidence, open approval state, fail-closed validation logic, and every Story 11.29 surface.
- Verification passed: the focused governance fact (1/1), the complete default Shell lane (2,733/2,733 with zero skips), the Release solution build (zero warnings/errors), and six focused successor-validator tests. Whitespace, immutable-evidence/Story 11.29 diffs, v3 SHA/open approval, and v4 absence checks also passed.
- Review patch independently binds the Python successor constants and documented dispatch flags to the repository-verified current tuple; touched text files were normalized to the repository CRLF policy.

## Review Triage Log

| ID | Verdict | Route | Evidence |
|---|---|---|---|
| BH-1 | medium | patch | Python successor tests derived their expected tuple from production constants; the C# governance fact now independently pins all three literal successor constants to the actual gitlink/catalog tuple. |
| BH-2 | medium | patch | Narrative documentation assertions did not pin the executable dispatch arguments; governance now asserts the exact EventStore source, package version, and Builds catalog flags. |
| BH-3 | low | reject | Run discovery/download ergonomics predate this repair. Candidate artifacts remain run-scoped and their exact revision/tuple is independently validated, so selecting a wrong candidate fails closed rather than authorizing it; expanding the operator procedure is disproportionate here. |
| BH-4 | false | reject | `in-progress` was the required workflow state while review was active; finalization changes it to `done` only after triage and post-patch verification. |
| BH-5 | false | reject | The one-shot workflow intentionally requires a minimal spec containing frontmatter, frozen intent, and implementation notes; full dispatch-only Code Map, task, and verification sections were deliberately excluded. |
| BH-6 | medium | patch | Touched files had LF or mixed endings despite the repository CRLF policy; all five were normalized to CRLF before final verification. |
