# Story 13.6 — accessibility lens

**Verdict: changes required for a complete automated-coverage claim.** This inspection found **0 Critical, 0 High, 4 Medium, 0 Low** findings, all verification defects or coverage gaps. No new production accessibility failure was established. The two approved behaviors are represented by real generated browser journeys. Pending execution is separate from the findings below, and this review does not close the story or any human/owner gate.

Reviewed on 2026-10-16 against source candidate `sha256:c0500f85acc0a4dcff6e1b8baf55b1819ed768da0ec2764a5efe6e73538b5487`. All **36** file digests in [reviewed candidate.json](../failed-proof/pre-ux-candidate.json) matched current bytes; independently recomputing its sorted canonical rows reproduced the candidate digest. Findings apply to those bytes; a subsequent patch requires new hashes and affected verification.

## Scope and authority

- [Approved completion spec](../../../../spec-13-6-complete-automated-accessibility-coverage.md), especially lines 24–35 and 50–69: every changed state/AE row needs nonempty assertions or a precise external execution blocker.
- [Original approved Story 13.6](../../../../spec-13-6-verify-responsive-and-assistive-accessibility.md), lines 24–43 and 66–67: preserve defaults; neutral missing-item recovery only on actual focus loss; zero-field feedback keeps trigger focus and one speech owner.
- [Canonical UX-AE-1](../../../../../../_bmad-output/planning-artifacts/ux-design.md:305), especially AE-03–AE-09 at lines 315–321.
- [UX-VC-1 and UX-RM-1](../../../../../../_bmad-output/planning-artifacts/ux-design-detailed-2026-07-05.md:100), lines 104–112 and 203–212.
- [DESIGN.md](../../../../../../_bmad-output/planning-artifacts/ux-designs/ux-frontcomposer-2026-09-09/DESIGN.md:78) and [EXPERIENCE.md](../../../../../../_bmad-output/planning-artifacts/ux-designs/ux-frontcomposer-2026-09-09/EXPERIENCE.md:243): Fluent owns styling; motion/color cannot carry essential meaning alone.

The useful acceptance requirements are: 320 CSS-pixel reflow with all four spacing overrides; only essential labelled grid/tab bounded horizontal scrolling; every pointer target at least 24×24 or an individual permitted exception; the whole focused control and indicator visible; text at 4.5:1 (large text 3:1), meaningful nontext/focus at 3:1; forced-color current/focus/status cues; reduced-motion immediate state/focus and silent expiry. Actual 400% browser zoom and heard speech require their own observations.

## Findings

### A11Y-01 — Medium: a passing boundary sample clears unrelated failures

**Evidence:** [responsive-accessibility.ts:396](../../../../../../tests/e2e/helpers/responsive-accessibility.ts:396). For both `focus` and `border`, one sample at or above threshold makes the entire failures array empty. The inventory traverses all descendants (lines 383–387), so the exemption is broader than one Fluent two-color ring.

**Reproduction executed:** Chromium, an isolated page with two white-background buttons. The first had `outline:2px solid #eee`, the second `outline:2px solid black`. Calling the actual `expectSurfaceContrast(group, 'focus')` passed while its samples reported `button#bad = 1.160230471:1` and `button#good = 21:1`.

**Impact:** AE-06/VC-04 evidence can overlook an unrelated low-contrast control; one good descendant or pseudo-boundary can mask another.

**Recommended disposition:** Patch. Group alternative paints by the specific indicator/control, allow the reviewed Fluent separator exception within that group, and reject every required group without a conforming visible edge. Add a mixed-good/bad negative fixture, not only the existing all-bad fixture at accessibility-state-matrix.spec.ts:243–244. Rerun affected contrast assertions and rebind hashes.

### A11Y-02 — Medium: box-shadow focus extents are absent from geometry

**Evidence:** [responsive-accessibility.ts:145](../../../../../../tests/e2e/helpers/responsive-accessibility.ts:145) recognizes a box shadow as an indicator, but lines 151–153 expand the tested rectangle only for an outline.

**Reproduction executed:** Chromium, focused fixed button at `left:0;top:100px;width:40px;height:40px;outline:none;box-shadow:0 0 0 6px black` in a 320×300 viewport. Actual helper result was `box={left:0,top:100,right:40,bottom:140}, ring=0, failures=[]`. The visible six-pixel indicator extends left of the viewport.

**Impact:** AE-05/VC-04 can pass a clipped whole focus ring when a control uses shadow-based focus.

**Recommended disposition:** Patch. Include painted outer shadow offset/spread/blur extents or require explicit pixel evidence for unsupported shadow geometry. Account for pseudo-element indicator geometry too. Add viewport and scrollport shadow-ring negative fixtures; rerun affected focus checks and rebind hashes.

### A11Y-03 — Medium: occlusion scanning omits boxes inside shadow roots

**Evidence:** [responsive-accessibility.ts:177](../../../../../../tests/e2e/helpers/responsive-accessibility.ts:177) scans `document.querySelectorAll('*')`, unlike the composed-tree collection elsewhere. The host may have a zero-sized box even when its shadow child paints a fixed overlay; line 180 then skips it.

**Reproduction executed:** Chromium, a focused button at x=50..130, y=100..140, with a shadow-root child fixed at x=70..90, y=110..130, opaque red and z-index 20. The actual helper returned `failures=[], occluder=null` while the overlay covered the button.

**Impact:** AE-05 can miss an app-owned obstruction from Fluent/custom-element shadow content.

**Recommended disposition:** Patch. Traverse open shadow roots for candidate occluders and inspect hit-testing through shadow boundaries; retain explicit limits for inaccessible closed roots. Add a negative fixture with the zero-sized host above and rerun affected geometry checks.

### A11Y-04 — Medium: forced-color evidence does not verify required nontext/current/focus cues

**Evidence:** [accessibility-state-matrix.spec.ts:41](../../../../../../tests/e2e/specs/accessibility-state-matrix.spec.ts:41)–58 samples focus/current/border/icon cues before forced colors. Lines 60–75 switch modes, optionally sample text, check visibility/page overflow, and reset; they do not inspect the required forced-color boundaries, icons or selected/focus treatment. For example selected tabs are asserted before `evidence()` at lines 298–300, the rail samples are before the mode switch at line 48, and fresh presentation is checked in normal paint at lines 455–460.

**Impact:** AE-07/VC-03/VC-04/VC-05/VC-06 can stay green after a forced-color marker or indicator disappears, provided readable text and DOM state survive. This is an unmeasured requirement, not evidence that current rendering fails. Merely waiting for pending engine runs cannot supply the missing assertion.

**Recommended disposition:** Extend forced-color state sampling to require the pertinent system-color border/icon/shape/current/focus cue, with one retained snapshot per required state family and a negative fixture that removes the cue while preserving text/ARIA state. Alternatively record the exact cells as uncovered with an owner and execution follow-up; do not label those AE-07 cells complete. Keep WebKit's probed inability to replace authored colors as a separate capability limit.

## Positive evidence and practical limits

- Real generated zero-field outcomes are exercised at accessibility-state-matrix.spec.ts:88–120, including validation, returned/exception rejection, forbidden, unavailable and blank unavailable, rate limit and blank rate limit, transport, admission and policy denial. Assertions check visible safe nonempty copy, retained focus, a non-live local message, a matching shared owner, Retry-After, no pre-accept retry, lifetime through scenario selection, and clearing/accepted pending summary. These are DOM/dispatch assertions, not heard-speech observations.
- Real generated two-row removal is exercised at lines 124–167 with external-focus preservation, repeated-result revision stability, and lost-focus recovery to neutral non-live text. Delayed operator-focus/navigation cancellation is exercised at lines 428–450; filter-hidden recovery remains a distinct generated path at lines 411–424. The controls dispatch scoped LoadRequested/Loaded actions (AccessibilityScenarioControls.razor:157–175), not just a banner flag.
- Spacing injection covers delayed open shadow roots and cleanup (responsive-accessibility.ts:12–44). The route matrix traverses expected keyboard inventories in both directions (responsive-accessibility.spec.ts:315–410), and verifies targets/spacing/page overflow on actual routes. The settings radio exceptions use measured Spacing records rather than a broad exemption.
- Eleven lifecycle states are enumerated in lifecycle.fixture.ts:7 and exercised at accessibility-state-matrix.spec.ts:169–185. Motion assertions explicitly inspect wrapper pulse/transition paint; the broader motion matrix still needs careful per-family attribution, and this review supplies no live fresh-row acceptance.
- Native validation links retain ordinary href activation when focus enhancement fails (accessibility-state-matrix.spec.ts:477–498).

## Evidence actually inspected

Read the repository entry point and permitted Hexalith baseline/UX guidance; repository editor/build/attribute configuration and architecture summary; both specs and the canonical UX documents above; candidate inventory; the complete responsive helper and state-matrix specification; the keyboard-cycle/reflow portions of responsive-accessibility.spec.ts; lifecycle fixture; validation markup/CSS; relevant generated zero-field renderer/form source; missing-detail component and focus integration; shell/grid/focus-scroll CSS/JavaScript; gated scenario controls and page-loader source.

Independently checked all candidate file SHA-256 values. Ran three **isolated Chromium helper probes** using the current helper loaded in memory with Node's `stripTypeScriptTypes`, Playwright Chromium and synthetic DOM; they wrote no repository files. Their exact layouts and observed numeric outputs are recorded with A11Y-01–03 above. An initial attempted probe using the installed TypeScript package failed before execution because `ts.ScriptTarget` was unavailable; the Node stripping approach then executed successfully.

Read four historical Chromium state JSON records under `2026-10-16-coverage/chromium/`: tabs-active-light, fresh-presentation-light, lifecycle-syncing-light and generated-missing-lost-focus-light. They show text/media-state inventories and were used only to understand evidence shape; they do not establish a pass for the new completion candidate. Read the earlier result document, which still binds `789ad2…`; its stale platform/coverage declarations are not new findings while the authorized refresh is pending. No screenshot was visually reviewed in this lens. No full current browser suite, Shell suite, Firefox, WebKit, or Windows run was executed by this reviewer, and no in-progress run is claimed to pass.

Manual NVDA/Firefox, JAWS/Chrome, VoiceOver/Safari, actual browser zoom, physical tablet/phone, Product approval/reapproval, OI-16, G-4, FLUENT-APP-1, the exact Fluent V5 posture and live fresh-row acceptance remain open. This scoped review cannot supply them or infer their approval.


## Follow-up — patched source, 2026-10-16

**Verdict: the four original findings have source corrections; final executed proof remains pending. One residual Medium focus-ownership defect is reproduced below.** The original report and original candidate above remain historical evidence.

Requested candidate: `sha256:c8b64492819e19a7e425d6f3fa0cb6aeea500ba9249c1cc1d304c346677b7c59`, **38 files**, full source revision `b9e13697cbcd98a0ad0e8003545b779a7a6ce8dc`, baseline `c3708690f8e1dbebfb49682c67a8aba88fd4a092`. The parent reported an additional helper adjustment in progress for resting Fluent elevation shadows. At this follow-up's final independent hash check, `helpers/responsive-accessibility.ts` and `specs/responsive-accessibility.spec.ts` had changed after the c8 manifest; observed sorted-row digest was `4672f68246756057612973cbbb0f359074fb611db8a0e5800dfcedaabb81865e`. This is a transient source observation, not a newly sealed or verified candidate. Parent must refresh identity and attribute final test results after patches settle.

| Original finding | Source disposition and inspected evidence |
| --- | --- |
| A11Y-01 | **Corrected for the reported unrelated-control masking.** Contrast samples now retain a unique control owner (`responsive-accessibility.ts:406–424`), and only passing paints of that owner can supply the two-color edge alternative (`:476–484`). The mixed-good/bad negative fixture and same-control separator case are at `accessibility-state-matrix.spec.ts:269–276`. Re-executing the original mixed-control Chromium probe now rejected the contrast call. |
| A11Y-02 | **Corrected for the reported outer shadow clipping.** `responsive-accessibility.ts:137–144` parses outer shadow geometry; `:196–207` extends the tested box, with pseudo geometry and explicit unsupported cases. Negative fixtures cover viewport, offset, pseudo, scrollport and blur-only cases (`responsive-accessibility.spec.ts:12–28` at review input). Re-executing the original six-pixel shadow probe returned box left=-6, ring=6 and `viewport clips target or focus ring`. A further patch distinguishing resting blurred elevation from a measurable focus indicator is acknowledged as in progress; its final source/verification is not asserted here. |
| A11Y-03 | **Corrected for the reported open-shadow obstruction.** `responsive-accessibility.ts:232–247` collects shadow descendants and deep hit results. Fixture: `responsive-accessibility.spec.ts:30–35` at review input. Re-executing the original probe returned `app content obscures target or focus ring`, identifying the shadow DIV at 80,120. Closed-shadow limitations remain as previously stated. |
| A11Y-04 | **Corrected in source for the reported missing forced-color cue checks.** `accessibility-state-matrix.spec.ts:80–90` now measures system icon/boundaries/state cues and retains a system-state snapshot before resetting mode. Required fields appear in the JSON at `:94`; `a11y-state-evidence.mjs:37–48` requires the per-owner edges, matching system cue/boundary/icon inventories and nonempty named snapshot, with a separately supplied WebKit capability exception. `validate-a11y-artifacts.mjs:60–63` applies this validator. Negative fixtures cover removed cues despite readable text/selected semantics (`accessibility-state-matrix.spec.ts:279–287`) and missing/invalid system inventories (`a11y-state-evidence.test.mjs:27–30`). No finished current state artifact or screenshot was promoted to passing evidence by this reviewer. |

### A11Y-F01 — Medium: delayed palette return can still take focus after real entry consumed its tracker

**Evidence:** `fc-keyboard.js:113–116` clears `window.__fcOverlayOpenIntent` for the palette search. The new guard at `fc-focus.js:662–664` depends on that same object's `moved` flag. The new regression at `responsive-accessibility.spec.ts:161–184` manually seeds the flag object immediately before Tab, skipping the real palette-entry call that clears it. It proves the artificial retained-intent case, but misses the actual consumption path. `FcCommandPalette.razor.cs:334` invokes this real `focusElement` path.

**Focused reproduction executed:** Chromium with the current actual `fc-keyboard.js` and `fc-focus.js` imported in memory: focus a palette trigger; call `captureOverlayOrigin()`; call `focusElement()` on an input carrying `data-testid="fc-palette-search"`; wait 200ms for entry completion; focus the trigger to simulate native dialog invoker return; press a real Tab to Settings; call `restoreOverlayOrigin()` and wait two frames. Before restore: `{active:"settings",intent:null}`. After restore: `"trigger"`. This loses the operator's intervening keyboard focus move, contrary to canonical deterministic focus ownership. It does not require or imply heard speech.

**Recommended disposition:** Keep an independent dismissal/return ownership record that survives palette entry, or establish from actual focus/input history whether native return already occurred and the operator moved on. Cover the real entry/consumption sequence in the regression; retain ordinary invoker return when no intervening operator move occurs. Rerun the focused regression and affected overlay journeys, refresh candidate hashes, and record the disposition.

Only the three original helper probes and the narrow palette-consumption reproduction were executed by this follow-up reviewer; no broad suite was run. The combined browser verification was still active. All manual AT, actual zoom, physical-device, Product/Fluent/OI-16/G-4/live-fresh-row gates remain open as above.


## Final narrow source disposition — 2026-10-16

**Verdict: all four original source findings and follow-up A11Y-F01 are addressed; no unresolved actionable finding remains from this lens.** This supersedes the earlier source-blocking verdict for the final candidate only, preserving the original evidence above. Full browser execution and human/owner gates remain separately open until their own evidence is attributed.

Final requested candidate: **`sha256:cc7bbd3254a4e1cae96289b3e3e4cfae24feff356f04e6dad8b4b18c82bb4bd2`**. Full source revision: `b9e13697cbcd98a0ad0e8003545b779a7a6ce8dc`; baseline: `c3708690f8e1dbebfb49682c67a8aba88fd4a092`. Independently recomputed all **38** file SHA-256 values against `completion/candidate.json`: **zero mismatches**. Recomputing the sorted canonical rows reproduced the final candidate digest exactly.

**A11Y-F01 closed:** `fc-keyboard.js:12–13` and `:28–33` retain operator movement on the modal reservation independently of entry intent. `fc-focus.js:662–664` consults that reservation before delayed return, and releases the reservation at `:665–667`. The revised regression (`responsive-accessibility.spec.ts:163–194`) calls actual `captureOverlayOrigin` and `focusElement`, verifies entry intent consumption, closes a native dialog, presses real Tab, restores late, and checks that Settings keeps focus and can open. This covers the previously omitted consumption path. The helper's resting-blur adjustment is present at `responsive-accessibility.ts:183–214`: a measurable indicator can coexist with resting elevation; blur-only evidence still fails closed.

**Independent narrow Chromium reproduction rerun:** imported the final actual JS modules in memory, reserved/opened a native dialog from the trigger, called actual palette `focusElement`, waited 200ms, verified entry intent became null, then closed/removed the native dialog. With real Tab, before late restore was `{active:"settings",intent:null,reservationMoved:true}`; afterward `{active:"settings",reservationCleared:true}`. Without intervening Tab, before restore was `{active:"trigger",intent:null,reservationMoved:false}` and afterward `{active:"trigger",reservationCleared:true}`. The former defect is fixed while ordinary invoker return remains intact. No repository files were created by these probes.

The parent reported **18 focused three-engine probes passed** and **163 affected Shell/governance checks passed**; this reviewer did not independently inspect those result artifacts and leaves their final attribution to the parent. No broad suite was run by this recheck. Combined full-browser runs were still active. Manual AT/heard speech, actual browser zoom, physical devices, Product/Fluent/OI-16/G-4/live-fresh-row approvals remain unchanged and are not inferred.
