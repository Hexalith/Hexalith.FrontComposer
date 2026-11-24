---
title: 'DW-1969 Reconcile the Story 11.27 implementation note'
type: 'bugfix'
created: '2026-11-24'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Make the Story 11.27 implementation note that says no application markup or route generation changed agree with the later owner-approved patches that retagged the shell banner and added the generated `FcPageHeader`.

</frozen-after-approval>

## Implementation Notes

- Scoped the "no application markup or route generation changed" sentence to the initial locator pass, with a dated DW-1969 amendment marker. It now lists the later shell and generated-output changes: banner retag, `FcPageHeader` in a 24 px stack, the emitted XML summary, and display-label normalization including the MCP descriptor title. It also records the concurrent Builds, Commons and EventStore gitlink advances, the SDK update and the package bumps from `spec-update-latest-submodules-and-package-versions.md`.
- Corrected the adjacent bullet's "selecting the shell banner by position" wording, which an earlier review had flagged as inaccurate.
- Documentation-only change; no build or test applies.

## Review Triage Log

- low, patched — the pointer named the Review Triage Log; the dependency work is recorded under Review Findings.
- low, patched — verified with `git diff aba29291 cffb1eb7 -- references` that Builds and Commons also advanced; all three gitlinks are now named.
- low, patched — the note now names the `aba29291` through `cffb1eb7` review ranges instead of "the reviewed range".
- low, patched — the SDK and package claim now cites the owning spec and commits `f20a1fc7` and `f1b60402`.
- low, patched — added a dated DW-1969 amendment marker, because the Review Findings quote the original sentence.
- low, patched — the list of later changes now includes the stack, the emitted XML summary and the display-label normalization that changed the MCP descriptor title.
- low, patched — dropped "owner-approved", which did not cover the Blind Hunter patches.
- low, patched — the adjacent "by position" wording is corrected.
- false — the DW-1969 ledger entry is closed when this change is finalized.
- low, rejected — a `## Documented Unrelated Changes` section in a done story's spec adds validator structure the reconciliation does not need.
