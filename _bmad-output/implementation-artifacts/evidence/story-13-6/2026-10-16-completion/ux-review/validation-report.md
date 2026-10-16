# Validation Report — Story 13.6 scoped UX evidence

- **DESIGN.md:** [Canonical visual contract](../../../../../planning-artifacts/ux-designs/ux-frontcomposer-2026-09-09/DESIGN.md)
- **EXPERIENCE.md:** [Canonical behavior contract](../../../../../planning-artifacts/ux-designs/ux-frontcomposer-2026-09-09/EXPERIENCE.md)
- **Run at:** 2026-10-16T08:58:03.360412+00:00
- **Verification updated:** 2026-10-16T10:24:54.049134+00:00
- **Final source candidate:** `sha256:f8d9a2b2a0608d5ed0bc461f433734e153b2836fa1237658539cc638bb2c03cf` — [manifest](../build-review/candidate.json), 40 source/test/CI/evidence-policy files.
- **Source HEAD:** `b9e13697cbcd98a0ad0e8003545b779a7a6ce8dc` plus the inventoried changes.
- **Initial reviewed candidate:** `sha256:c0500f85acc0a4dcff6e1b8baf55b1819ed768da0ec2764a5efe6e73538b5487` — [preserved manifest](../failed-proof/pre-ux-candidate.json).
- **Intent:** scoped headless Validate; accessibility and Fluent conformance lenses. Canonical UX documents were read, not edited. No full document rubric was selected.

## Overall verdict

Fluent ownership and the approved default contracts conform within the inspected delta. Four original Medium accessibility verification findings and one Medium follow-up focus-return finding are resolved. Independent follow-up reproduced correct behavior for the original helper defects and the actual palette-entry race; the current parent browser run supplies affected proof. All five scoped findings have patches and regression evidence; the subsequent Build review separately records sixteen resolved evidence findings.

This review is complete within its evidence scope. Story acceptance remains open: twelve Firefox/WebKit visual comparisons fail, the latest full configured Shell run has one failure, eight browser project skips remain explicit, Windows has not run, and all 116 Linux WebKit actual forced-palette cells remain uncovered. Manual AT/heard speech, actual browser zoom, physical devices, OI-16, G-4, FLUENT-APP-1, Product reapproval and live fresh-row acceptance remain separate gates.

## Category verdicts

- Accessibility evidence — five recorded Medium findings resolved; platform and manual limitations remain explicit.
- Fluent conformance — source-conformant within the inspected delta; separate owner approval remains open.
- Flow coverage, token completeness, component coverage, state coverage, visual reference coverage, bloat and overspecification, inheritance discipline, shape fit — not selected for this scoped document review.

## Findings by severity

### Critical (0)

### High (0)

### Medium (5 recorded; 0 unresolved)

**[Accessibility] A11Y-01 — Contrast ownership**

One passing boundary could mask another control’s failing contrast.

Disposition: Resolved: alternative paints are grouped per actual control; mixed good/bad fixtures reject unrelated failures.

**[Accessibility] A11Y-02 — Focus paint geometry**

Shadow and pseudo-element focus extents were not included in clipping geometry.

Disposition: Resolved: sharp shadow/pseudo extents are measured; clipped rings and unsupported sole-blur geometry reject. Near-scrollport elevation-plus-sharp-ring positives and enlarged-ring negatives pass.

**[Accessibility] A11Y-03 — Open-shadow occlusion**

Document-only enumeration missed fixed opaque children of zero-sized shadow hosts.

Disposition: Resolved: occluder enumeration traverses open shadow roots; the original negative reproduction detects the obstruction.

**[Accessibility] A11Y-04 — Forced-color state evidence**

Post-switch evidence did not require meaningful icons, boundaries and focus/current cues.

Disposition: Resolved: actual forced-palette resampling, per-state screenshots and strict artifact requirements include those cues. Missing-cue negatives reject; WebKit unsupported actual paint remains uncovered.

**[Accessibility] A11Y-F01 — Delayed modal focus return**

Palette entry consumed the temporary intent before delayed dismissal could preserve a later operator Tab.

Disposition: Resolved: the modal reservation retains operator movement independently of entry intent. Actual native-dialog/entry-consumption/Tab repro preserves Settings; its no-Tab control retains normal invoker return. Subsequent tracing also identified the pinned vendor 25ms second return; only the owned palette/settings slots are cleared, and the held actual callback regression preserves real Tab/Enter to Settings.

### Low (0)

## Executed evidence and limits

- [Current verification](../build-review/verification.json): 363 nonvisual accessibility cases, 361 passed, 0 failed, 2 existing skips. All 116 state rows per engine are retained. The preceding ten-spec16abc result remains 532 passed / 12 visual failures / 8 skips; those comparison failures are retained with no baseline replacement.
- [Parent audit](../build-review/parent-audit.json): all 40 raw source digests, aggregate identity, both unchanged frozen intent blocks, current JUnit and 348 ledger paths independently checked.
- Chromium/Firefox strict state gates pass. WebKit strict validation remains failed for 116 actual forced-palette cells; explicitly capability-limited media evidence supplies no system-palette pass.
- Final 27-case targeted proof passes actual forward/reverse keyboard cycles and ownership negatives; normal/forced light/dark inset contours and desktop spacing preservation pass. The canonical broader run supersedes the focused counts. Original failed and interrupted attempts remain retained.
- Latest configured Shell: 3680 passed / 1 failed; affected 163 pass within that run. Isolated scheduler class 88 passes without replacing the full failed gate. Current affected 163 checks pass after the shared grid focus CSS change; latest full results retain their preceding source attribution.
- One instrumented selected-cue inventory failure remains unattributed in its retained diagnostic packet and was not reproduced in the canonical current run. No passing result is inferred for that failed diagnostic.
- Windows, human observations, preserved visual failures and separate owner approvals remain open. This is scoped automated evidence review.

## Reviewer files

- [Accessibility](review-accessibility.md) — original four findings, one follow-up finding, reproductions and subsequent resolutions retained.
- [Fluent conformance](review-fluent.md) — zero actionable findings; source constraints and verification limits retained.

## Headless result

```json
{"status":"complete","intent":"validate","validation_report":"validation-report.md","findings_summary":{"critical":0,"high":0,"medium":5,"low":0},"unresolved_findings":{"critical":0,"high":0,"medium":0,"low":0},"offer_to_update":true}
```

The selected evidence review is complete. Canonical UX contract updates are available if requested; no such update or owner approval is inferred.
