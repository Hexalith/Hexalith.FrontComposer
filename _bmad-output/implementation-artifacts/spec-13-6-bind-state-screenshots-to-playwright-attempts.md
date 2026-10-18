---
title: 'Story 13.6: Bind state screenshots to successful Playwright attempts'
type: 'bugfix'
created: '2026-10-18'
status: 'done'
route: 'oneshot'
story_id: '13.6'
baseline_commit: 'ba59327358b5639fa4db87c8eee2711858c77007'
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-13-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The accessibility state matrix binds its JSON measurements to the final successful Playwright attempt but accepts a nonempty system screenshot without matching that attempt's attachments. A stale or substituted image can therefore supply the required screenshot evidence.

**Approach:** Attach each captured system screenshot with a unique state name and image/png content type. Require exactly one matching path-backed attachment from the same authoritative successful attempt and byte equality with the state-local screenshot. Cover missing, duplicate, unreadable, substituted and failed-retry images with meaningful regressions and a genuine PNG fixture. Preserve the attributed WebKit media-only capability branch and existing JSON/engine/run/retry checks. Verify actual matrix output on the three configured engines and record exact candidate-bound results. This bounded fix adds no dependency or public API and leaves PNG decoding, the existing artifact-size failures, visual/catalog decisions, manual AT/zoom/devices and owner approvals open. Story 13.6 and its sprint entry remain in review.

</frozen-after-approval>

## Implementation Notes

- Investigation: `tests/e2e/specs/accessibility-state-matrix.spec.ts` captures `system-state-{state}.png` inside `evidence()` but attaches only `state-{state}` JSON. `tests/e2e/scripts/a11y-state-evidence.mjs` already resolves final successful attempt authority; reuse that attachment list for screenshot matching. `a11y-state-evidence.test.mjs` currently writes text as image fixtures; use actual PNG bytes and separate report attachment copies so altered-byte checks are independent.
- Route facts: no unresolved operator-visible intent choice, no irreversible operation, three existing implementation/test files plus this spec and scoped evidence/result notes. Preserve approved images, text snapshots, dependency declarations, policy and all submodule pointers.
- Sprint key `13-6-i-ux-e-verify-responsive-and-assistive-accessibility` is already `review`; synchronization never moves it backward to in-progress. Git started clean on main. Repository user instructions prohibit staging/committing/pushing unless explicitly required by the task; leave the resulting changes available for inspection.

- Implementation: the producer now attaches `system-state-{state}` PNGs before its state JSON; the validator checks the unique image/png attachment from existing final-attempt authority, catches unreadable/deleted files, and compares independent image bytes. Added three focused regressions; the suite grows from 12 to 15 tests, with all 15 passing. The initial three red cases and an intermediate missing-file diagnostic mismatch are retained, as is the corrected final green run. A genuine generated 1x1 PNG replaces the old text fixture; decoding remains outside this check. Added byte-preservation for this new evidence packet.
- Runtime: Aspire startup failed with MSB4018 GetAssemblyAttributes/file-in-use on Tenants.UI; `aspire stop` and `aspire describe` both reported no running AppHost. The serialized Debug Counter.Web build passed with zero warnings/errors. Browser verification uses a separately owned Test host at http://127.0.0.1:5096, with the existing safe specimen gates and configured delays. No live EventStore proof is claimed.

## Review Triage Log

Blind Hunter completed on the initial diff and in-progress evidence. Its ten findings were checked against source and callers; all were patched in the bounded screenshot evidence scope. No finding from this review is deferred.

| Finding | Verdict and disposition | Evidence |
| --- | --- | --- |
| F01 same-length substitution missing | Medium, patched | Two different valid 68-byte PNGs now test independent original and report copies; changing either copy fails. |
| F02 identical state images | Low, patched | Adjacent states use different PNGs; exchanging their report copies fails, then restored copies pass. |
| F03 shared engine attachments | Low, patched | Each engine has its own report copies. Removing Chromium's image fails even with Firefox/WebKit attachments present. |
| F04 retry content unchanged | Medium, patched | A separate failed-retry image remains on disk; final bytes must match the final attachment before validation passes. |
| F05 WebKit exemption untested without images | Low, patched | The capability fixture removes every PNG and image attachment before the attributed WebKit case passes; Chromium policy still fails. |
| F06 matching empty images untested | Low, patched | Both empty independent copies fail the retained nonempty guard. |
| F07 inline body untested | Low, patched | Valid base64 image body without a file path fails the path-backed requirement. |
| F08 replacement diagnostics lack case labels | Low, patched | Every replacement has a label passed to its assertion. |
| F09 malformed snapshot field throws | Low, patched | A snapshot lookup occurs only for its exact expected string; numeric/object/array/null metadata returns a validation error. |
| F10 image diagnostics ambiguous | Low, patched | Errors now identify expected state/name/count, missing path, differing file paths, or the caught filesystem reason. |

- Final verification: 18/18 Node tests pass. The complete three-engine matrix ran 225 cases: 224 passed and one existing WebKit overflow-clip-margin capability skip; zero failures/flaky outcomes. That initial WebKit report lacks its separate responsive-spec capability probe and is insufficient for media-only validation. A correctly selected final WebKit matrix plus same-run probe ran 76 cases: 75 passed, the same skip, zero failures/flaky outcomes. Final reviewed-validator replay accepts all 116 state rows per engine, using the first report for Chromium/Firefox and the final report for WebKit. WebKit actual forced-palette paint remains uncovered.
- Evidence is retained under `evidence/story-13-6/2026-10-18-attempt-ownership/`: exact commands/outcomes, initial and reviewed source manifests/patches, raw reports, all 697 hashed attachment paths, portable read-only replay and separate initial/final state-validation results. All four scoped output roots fit the unchanged 50 MiB cap; the complete artifact validator is not claimed because these packets omit specimen axe/focus artifacts. Prior broad size/visual/catalog gates and PNG decodability remain open. The shared result and append-only ledger record the partial BH12 resolution without changing prior evidence.
- The owned standalone Test host stopped cleanly. All ten scoped review findings are patched. This spec completes only the bounded screenshot-attempt correction; parent Story 13.6 and its sprint entry remain in review. Changes are left unstaged and uncommitted under the workspace instructions.
