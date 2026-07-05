---
title: Sprint Change Proposal - Authoring Sentinel Guard
status: implemented
created: 2026-07-05
approved_by: Administrator
approved_on: 2026-07-05
scope: minor
mode: batch
---

# Sprint Change Proposal - Authoring Sentinel Guard

Approval: Approved by Administrator on 2026-07-05 for Developer-agent direct implementation.

## 1. Issue Summary

BMAD, story, and test Markdown artifacts can accidentally retain raw tool-call wrapper lines from an authoring transcript. The existing artifact validator already scans `_bmad-output` and `docs`, but it only rejected raw `content` and `invoke` tag lines. That left newer wrapper forms such as `tool_call`, `function_call`, `arguments`, and `parameters` outside the durable guard.

The correction must keep legitimate documentation examples valid. Quoted examples, inline-code examples, and fenced-code examples are allowed; only raw tag-only lines fail.

## 2. Impact Analysis

Epic impact: This is a minor Epic 10 / FR-27 tooling-governance hardening change. It strengthens the existing mechanical artifact gate and does not alter product runtime, UI, public API, package surface, or release inventory.

Story impact: No sprint-status reshaping is required. Future story and test summaries benefit automatically because `eng/validate-story-artifacts.py` is already invoked by review and artifact-validation workflows.

Artifact conflicts: PRD, architecture, and UX planning artifacts do not need requirement changes. The correction aligns with existing NFR-11 testing and FR-27 evidence-governance expectations.

Technical impact: The existing Python validator gains an explicit authoring-sentinel tag allowlist and focused regression coverage. Current repository artifacts continue to pass the default scan.

## 3. Recommended Approach

Recommended path: Direct Adjustment.

Rationale: The existing validator is already the durable gate for story artifacts. Extending it avoids parallel validators and keeps review-promotion behavior centralized. Risk is low because the scan remains limited to raw tag-only lines outside fenced code, block quotes, and inline code.

Effort: Low. Risk: Low. Timeline impact: None.

## 4. Detailed Change Proposals

### Validator

File: `eng/validate-story-artifacts.py`

OLD:

- Raw authoring sentinel detection recognized only `content` and `invoke` tag lines.
- Fenced code and inline-code examples were skipped.

NEW:

- Raw authoring sentinel detection recognizes the tool-call wrapper family: `content`, `invoke`, `tool`, `tool_call`, `tool_calls`, `tool-call`, `tool-calls`, `tool_use`, `tool-use`, `function`, `function_call`, `function_calls`, `argument`, `arguments`, `parameter`, and `parameters`.
- Blockquoted examples are explicitly skipped alongside fenced code and inline code.

Rationale: This blocks accidental raw transcript/tool-call residue while preserving intentional quoted examples in BMAD/story/test Markdown artifacts.

### Regression Tests

File: `eng/tests/test_validate_story_artifacts.py`

OLD:

- The validator test suite covered File List reconciliation, documented unrelated changes, checked-task evidence, submodule pointers, and review-promotion artifact validation.

NEW:

- Adds a failing case for a raw tool-call tag line inside `_bmad-output/implementation-artifacts/tests/test-summary.md`.
- Adds a passing case proving blockquoted, inline-code, and fenced-code examples remain valid.

Rationale: The sentinel rule is now pinned against both the failure mode and the allowed documentation pattern.

## 5. Implementation Handoff

Change scope: Minor.

Route: Developer agent direct implementation.

Success criteria:

- Raw tool-call wrapper tag-only lines fail with `raw authoring sentinel`.
- Blockquoted, inline-code, and fenced-code examples pass.
- Existing default artifact validation remains green.
- No sprint-status or product-runtime changes are required.

## 6. Checklist Summary

- [x] Trigger understood: raw tool-call wrapper lines can leak into generated authoring artifacts.
- [x] Epic impact assessed: minor Epic 10 tooling-governance hardening.
- [x] Artifact impact assessed: validator and validator tests only.
- [x] Path selected: Direct Adjustment.
- [x] Implementation handoff: Developer agent completed the focused tooling change.
