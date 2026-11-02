---
title: 'Story 13.6: Repair Palette Absence Verification'
type: 'bugfix'
created: '2026-11-03'
status: 'done'
route: 'oneshot'
artifact_role: 'supporting-implementation-record'
primary_story: '_bmad-output/implementation-artifacts/spec-13-6-verify-responsive-and-assistive-accessibility.md'
baseline_commit: '9724c7e5db5f744366f10e7ee175723cf1ae134c'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The toolbar's absent-search journey still asserts zero `searchbox` elements after the palette adopted native combobox semantics. That assertion cannot detect an accidentally opened palette. The palette correction's supporting record also omits its subsequent hosted regression and recovery.

**Approach:** Assert absence through the existing stable palette test ID, prove that the same locator detects an open palette, and run the affected toolbar journey across Chromium, Firefox and WebKit. Annotate the supporting correction with the already recorded failed Quality run and successful locator follow-up. Reconcile the deferred entry and owning result without changing product behavior, historical evidence, manual requirements, frozen primary criteria or Story/sprint in-progress status.

</frozen-after-approval>

## Implementation Notes

Investigation: `tests/e2e/specs/page-toolbar.spec.ts:83` uses `getByRole('searchbox').toHaveCount(0)` after `/` on the focused palette trigger at `/counter`. `fc-keyboard.js` assigns combobox semantics on either the native input or Fluent host. `getByTestId('fc-palette-search')` identifies both paths; reuse it for the closed assertion and an open/close positive control in the same journey. Preserve the trigger-focus assertion. The unnumbered deferred entry under the 2026-11-02 acceptance-plan review requests this repair and an append-only annotation to `spec-13-6-fix-native-palette-option-announcements.md`. Existing result records Quality 41502327525's six locator failures and recovery at full commit `25139b7f59e5ae5465d1665825e24c638928ccb2` / Quality 41526310096. Cite that existing record; do not claim a new hosted execution.

Route assessment: no user-visible intent gaps; no irreversible action; one existing browser test and related owning records. No product/API/dependency/CI/baseline/gitlink changes. Verify the focused toolbar spec on all engines without retries, TypeScript, the story-artifact validator and diff formatting. Raw reports stay in local scratch. Human observations, the UX outline decision and independent owner gates remain open.

Implementation: replaced the obsolete role selector with `getByTestId('fc-palette-search')`, retained the trigger-focus check and added real palette open/input focus/Escape/closed/return-focus checks in the same journey. Updated the original supporting palette record, leading result, primary story and append-only deferred resolution. The primary sprint row is already in-progress; this supporting record does not advance its incomplete primary story to review/done.

Verification: the complete toolbar spec passes 21/21 across all three engines, with zero skips/failures/flaky cases and retries disabled. `npm run typecheck` exits 0. The separate assertion control passes 3/3 engines and demonstrates both the old false-negative and the corrected rejection. Exact browser command, test patch SHA-256 and local paths are recorded in `13-6-accessibility-result.md#palette-absence-verification-repair--2026-11-03`. Runtime preparation used `aspire start --non-interactive --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj`, followed by exact-target `aspire describe --format Json` and stop. Counter was Healthy; no full-topology health proof is claimed. A standalone Debug DLL host used the Test environment/settings from `tests/e2e/playwright.config.ts` on port 5070. New hosted CI and human observations remain unperformed.

Review follow-up: after `/counter` navigation, wait for `.fc-shell-root[data-fc-interactive="true"]` before relying on shortcut behavior. `FrontComposerShell.OnAfterRenderAsync` sets that marker after `RegisterShellDefaultsAsync`; route-heading focus alone does not establish registration. The final 21/21 rerun is recorded separately under `browser-final/`; the earlier passing run remains under `browser/` at its original patch hash.

Exact standalone host command, cwd `/home/administrator/projects/hexalith/frontcomposer` (stdout/stderr redirected to local `test-host-final.log`):

```bash
ASPNETCORE_ENVIRONMENT=Test Hexalith__FrontComposer__Specimens__Enabled=true Hexalith__Shell__FormAbandonmentThresholdSeconds=5 Hexalith__Shell__TimeoutActionThresholdMs=5000 Hexalith__Shell__ProjectionReconnectedNoticeDurationMs=30000 Hexalith__FrontComposer__StubCommandService__ConfirmDelayMs=6500 dotnet samples/Counter/Counter.Web/bin/Debug/net10.0/Counter.Web.dll --urls http://127.0.0.1:5070 > /tmp/fc-13-6-palette-absence-Wioczt/test-host-final.log 2>&1
```

From the same root, `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-13-6-repair-palette-absence-verification.md` and `git diff --check` exit 0. Frozen-block comparison confirms the primary story and original palette-fix intent unchanged; the primary sprint file is unchanged. All six changed files retain CRLF. Owned Aspire and standalone Test hosts were stopped; port 5070 is closed.

The invoked one-shot workflow requires a local commit; tracked project guidance requires a fix branch rather than committing directly to main. The root lockfile and installed commitlint CLI both resolve 21.2.4. `./node_modules/.bin/commitlint --edit /tmp/fc-13-6-palette-absence-Wioczt/commit-message.txt --verbose` validated the exact full candidate with zero problems/warnings; successful evidence remains in `/tmp/fc-13-6-palette-absence-Wioczt/commitlint.log`. No push or hosted run is performed by this follow-up.

The stricter post-commit check, `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-13-6-repair-palette-absence-verification.md --candidate HEAD`, initially rejected unpushed `d74d1682f41804c7093cabcf0b3e1d65ef0379ea` because its subject omitted the story number. The corrected subject includes story 13.6 and passed the same pinned CLI before amending that newly created local commit: `./node_modules/.bin/commitlint --edit /tmp/fc-13-6-palette-absence-Wioczt/commit-message-final.txt --verbose`, zero problems/warnings, evidence `commitlint-final.log` in the same scratch directory. No story-scope rule or baseline was relaxed.

## Review Triage Log

- BH-1 — medium, patched: heading focus can precede shell shortcut registration. The revised journey waits for the existing interactivity marker, set only after default shortcut registration; final three-engine toolbar verification passes 21/21.
- BH-2 — false as a claim of a reachable current delayed-open defect: `/` binds only to `FocusSolePageSearchAsync`, whose absent-search JS branch returns false without any palette-open fallback. The reviewer subsequently confirmed no actual delayed-open path or passing erroneous mutation was demonstrated and withdrew the required fix. No internal callback/protocol instrumentation was added; the known-open assertion control retains its narrower stated scope.
- BH-3 — low, patched: added the exact successful story-artifact and whitespace validation commands to the implementation notes.
- BH-4 — low, patched: added the exact standalone Debug Test-host launch command, all relevant environment settings, working directory and log path. No review finding was deferred.

## File List

- `tests/e2e/specs/page-toolbar.spec.ts`
- `_bmad-output/implementation-artifacts/spec-13-6-repair-palette-absence-verification.md`
- `_bmad-output/implementation-artifacts/spec-13-6-fix-native-palette-option-announcements.md`
- `_bmad-output/implementation-artifacts/13-6-accessibility-result.md`
- `_bmad-output/implementation-artifacts/spec-13-6-verify-responsive-and-assistive-accessibility.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
