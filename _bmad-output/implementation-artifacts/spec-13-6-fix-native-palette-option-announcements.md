---
title: 'Story 13.6: Fix Native Palette Option Announcements'
type: 'bugfix'
created: '2026-11-02'
status: 'done'
route: 'oneshot'
artifact_role: 'supporting-implementation-record'
primary_story: '_bmad-output/implementation-artifacts/spec-13-6-verify-responsive-and-assistive-accessibility.md'
baseline_commit: '73ff2f87e430d983661c42674c0e433b4b76045f'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Native NVDA/Firefox reads the palette search query instead of the selected result because combobox semantics live on the Fluent host while focus is on its shadow input.

**Approach:** Under Story 13.6's existing authorization to correct demonstrated accessibility failures, preserve FluentTextInput and bridge its current palette state to the focused native input with real cross-shadow element references. Retain the palette's existing live-status ownership, focus, selection, styling and localized copy. Verify the red native sequence, three browser engines, replaced/empty results and close/reopen behavior. Preserve original human acceptance requirements and primary story in-progress status.

</frozen-after-approval>

## Implementation Notes

Investigation: native Firefox 155.0.1 / NVDA 2026.2 selects ConfigureCounterCommand after a verified native Down Arrow but speaks only the query. Host has combobox/active-descendant metadata; focused INPUT lacks those associations. A runtime-only correction using ariaControlsElements and ariaActiveDescendantElement restores NVDA option speech. Fluent 5.0.1 exposes no native attribute C# parameter; dotnet-inspect and exact package source confirm this. Use the existing fc-keyboard.js palette registration/teardown lifecycle and current data attributes to synchronize native relationships; existing bUnit host-markup assertions must describe the interop state accurately.

This is a bounded implementation record, not a separate sprint story. Its completion must not advance primary 13.6 while manual acceptance remains incomplete. Existing worktree documentation belongs to the preceding authorized acceptance reconciliation and is preserved.

Final verification: Counter and Shell-tests Debug builds pass with zero warnings/errors; 72 focused Shell tests pass; 21 browser checks pass across Chromium/Firefox/WebKit with zero failures/skips/flaky/retries. The final native NVDA/Firefox run on the rebuilt product source generates ConfigureCounterCommand exactly once and returns focus after the documented two-Escape sequence. Exact source digests, commands, versions, initial failures, raw local pointers and final summary/log digests are recorded in the leading accessibility result. The runtime-only experiment and corrected-product evidence remain distinct. Original human acceptance and current-patch hosted qualification remain open.

Aspire baseline: exact FrontComposer AppHost started, Counter was Healthy, other resources were still starting/waiting; exact-target stop succeeded before source edits. No full-topology qualification is inferred. The owned native sessions were closed; the standalone Test host remains available.

## Review Triage Log

| Finding | Verdict and resolution |
| --- | --- |
| 1: delayed native input missed | Medium, patch: observe the current shadow root and disconnect it on replacement/disposal. Three-engine delayed/replacement test and independent reviewer probe pass. |
| 2: fallback untested | Medium, patch: isolated-page test removes reflection APIs and proves compatible host relationships, selection, empty clearing and retained native focus. No native-speech guarantee is inferred for fallback. |
| 3: populated replacement missing | Medium, patch: counter-to-configure query replacement verifies references point at the actual current option/listbox even when flat IDs are reused. |
| 4: teardown not proved | Medium, patch: unregister-before-initialization, delayed native creation, replacement, disposal and re-registration are executed in all three engines. |
| 5: missing native popup/expansion assertions | Medium, patch: assert native haspopup, autocomplete and populated/empty/reopened expanded states. |
| 6: stale candidate attribution | Medium, patch: current result, primary spec, guide and acceptance plan now distinguish initial source, the runtime experiment and the implemented correction. |
| 7: unsupported restoration claim | Medium, patch: final corrected-source native run, script/summary/log digests, exact versions/date, observer classification and pending human acceptance are recorded separately. |

The required one-shot Blind Hunter review and source/test follow-up completed; all findings were checked and addressed. No review layer failed and no new deferred item was added. This supporting record is complete; primary 13.6 remains in-progress.

## File List

- `src/Hexalith.FrontComposer.Shell/Components/Layout/FcCommandPalette.razor`
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FcPaletteResultList.razor`
- `src/Hexalith.FrontComposer.Shell/wwwroot/js/fc-keyboard.js`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcCommandPaletteTests.cs`
- `tests/e2e/specs/palette-native-accessibility.spec.ts`
- `_bmad-output/implementation-artifacts/13-6-accessibility-result.md`
- `_bmad-output/implementation-artifacts/spec-13-6-verify-responsive-and-assistive-accessibility.md`
- `_bmad-output/implementation-artifacts/spec-13-6-fix-native-palette-option-announcements.md`
- `_bmad-output/implementation-artifacts/spec-13-6-resolve-final-accessibility-acceptance.md`
- `docs/accessibility-verification/story-13-6-manual-walkthrough.md`

Pinned root commitlint CLI 21.2.4 validated the exact full commit message with zero problems/warnings before staging. Successful evidence is `/tmp/fc-native-palette-commitlint.log`; no validator was bypassed. Artifact validation, explicit File List validation and whitespace checks pass.

## User acceptance of the native palette correction — 2026-11-02

The user's **“I accept”** accepts the implemented correction and reported automated verification at `9b386d4f0b305c06f2a7b00e949506970fda4ba2`. The [owning result](13-6-accessibility-result.md#user-acceptance-of-the-native-palette-correction--2026-11-02) records its scope. This supporting implementation remains done; primary Story 13.6 remains in-progress for the existing human observation and hosted-CI requirements.
