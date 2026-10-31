---
title: 'Refresh the WebKit Linux engine baselines'
type: 'bugfix'
created: '2026-10-31'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_commit: '124cdfc14b39f8285b9fb46ac40f50699bbe417d'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Quality run `41186098021`, WebKit Linux job `124493316518`, fails only the six Light/Dark by Compact/Comfortable/Roomy visual comparisons after the authorized Playwright upgrade. The approved expected images retain the previous engine's text rasterization.

**Approach:** Inspect every hosted actual/expected/diff variant, prove retry stability and matching expected provenance, then adopt only the six correct WebKit Linux captures. Record dated visual-governance rationale and exact evidence under the operator's authorization to complete validation without another human checkpoint. Preserve all product source, assertions, tolerances, font configuration, other browser/platform images and historical evidence. Do not commit or push.

</frozen-after-approval>

## Implementation Notes

### File Scope

- `tests/e2e/specs/specimen-accessibility.spec.ts-snapshots/frontcomposer-type-light-compact-webkit-linux.png`
- `tests/e2e/specs/specimen-accessibility.spec.ts-snapshots/frontcomposer-type-light-comfortable-webkit-linux.png`
- `tests/e2e/specs/specimen-accessibility.spec.ts-snapshots/frontcomposer-type-light-roomy-webkit-linux.png`
- `tests/e2e/specs/specimen-accessibility.spec.ts-snapshots/frontcomposer-type-dark-compact-webkit-linux.png`
- `tests/e2e/specs/specimen-accessibility.spec.ts-snapshots/frontcomposer-type-dark-comfortable-webkit-linux.png`
- `tests/e2e/specs/specimen-accessibility.spec.ts-snapshots/frontcomposer-type-dark-roomy-webkit-linux.png`
- `docs/accessibility-verification/baseline-change-rationale.md`
- `_bmad-output/implementation-artifacts/spec-refresh-webkit-linux-engine-baselines.md`

- Entry is clean `main` at the full baseline above; nonrecursive fetch reports no origin divergence. Read owning instructions, shared Hexalith baseline, build/test and Playwright configuration, `.editorconfig`, `.gitattributes`, architecture UX invariants, visual-baseline governance, specimen preparation/assertions and historical baseline rationale. No nested submodule initialization or skills under `references/`.
- Hosted source is the baseline above, Playwright `1.64.1`, WebKit `27.2` revision `2370` on Ubuntu Linux. The previous approved capture source `cf5fe8da100bd5224a0d7f597fc92e1ebb98a5f4` locks Playwright `1.63.0`; do not substitute an assumed older version.
- Before evidence is the six matching tracked expected PNGs at the entry source. After and diff evidence comes from [Quality artifact 13037780715](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/41186098021/artifacts/13037780715), `browser-regressions-linux-results-webkit-1`. Downloaded only the 54 comparison PNG members (six variants, three attempts, expected/actual/diff) and report metadata using bounded ZIP range reads, leaving the unrelated trace/video payload unextracted.
- Inspected the full six actual frames and all six expected/actual/diff comparison crops through image viewing. Significant differences are 49 pixels per Light variant and 40 per Dark variant, confined to scattered glyph edges in lifecycle rows and expanded-detail text. Below the existing comparator threshold, the one-pixel navigation-rail separator also repaints at unchanged x=47 geometry from RGB 250/250/250 to 224/224/224, across 3570/3638/3686 pixels for the respective densities. Adjacent geometry remains fixed; this is stable painting drift, not a new layout or content change. Dimensions remain 1280 by 3894/3962/4010 for Compact/Comfortable/Roomy. No new content loss, clipping, overlap, layout/spacing movement, focus change or theme-color change appears; existing type-ramp and dark badge/metadata limitations remain as previously documented.
- Every hosted expected PNG matches its tracked baseline byte for byte. Each variant's three actual attempts has the same SHA-256. Copy the original hosted actual bytes, preserving platform identity and image encoding.
- Hosted job evidence is 503 passed, 25 skipped and exactly six visual failures. The later 50 MiB accessibility-artifact check also fails because retained failed-attempt traces inflate output; retain its bound and let the parent's fresh hosted run verify that downstream check. These captures do not claim human assistive-technology/device approval.
- Logs and inspection data: `/tmp/fc-webkit-visual-job.log` and `/tmp/fc-webkit-visual-41186098021/comparison.json`. No new producer build or live application start is needed for the artifact comparison.

### Capture Identity

| Variant | Before SHA-256 | After SHA-256 |
| --- | --- | --- |
| light/compact | `3dd9f24a1717ee68912e1075e481d7efa6ee9043d264db41e3aa5b285379dee3` | `8c8a750aade21e3a0ba98ca79f456e330b0eb802cb960d4b8bd8df6dbe72ebf2` |
| light/comfortable | `6301a175d23bcd92a64ab9a098d4f1cfc077889e7b4f991fa42addc7d50f3dbe` | `180d287727cebb2e5ccb02e02b507c0940fff7647cd2e0ce26c1f12db5ef582b` |
| light/roomy | `83a2049108406186fed05691bef2da84fa329c446bff1f22416f06748417f534` | `f8e1ab05378b52340f816699d33f6ef27b394ad4593f65ce66d9a8928611cca2` |
| dark/compact | `b6aa3fad71ba3d639e3a70994f6bf08729dd6007d4c0a46542b85cb2f8b82bd9` | `f4347eb53a8e3f2c7b2bcb2419898c7f9ce5d7e52b91cbe227d91e7256d7fea0` |
| dark/comfortable | `27f44ed96f1846736ca02a4259eea97db7f42b9906d0a526c11b32f212f2aa9c` | `e8530f5f4251597888e2aef1c827963c628a717a9c67b2e7cfbafbd688ba7401` |
| dark/roomy | `2c9279307fe10d2d4099a4c648a2808a39b004ca09f022875d80fc725cf0be3f` | `ae79f1dc2946b9725681e081d3d6039327576a2963c7bd24598b475e03604861` |

### Focused Verification

- `python3 /tmp/fc-verify-webkit-baselines.py` passed: six valid PNGs match all three hosted actual attempts; before hashes match entry HEAD; all 18 other browser/platform baselines are byte-unchanged. Log: `/tmp/fc-webkit-baseline-verification.log`.
- `npm --prefix tests/e2e run validate:visual-governance` passed for exactly six changed snapshots. Log: `/tmp/fc-webkit-visual-governance.log`.
- `git diff --check` passed. Diff is exactly the six PNGs plus the dated rationale paragraph and this standalone spec; no tolerance/assertion/configuration changes.
- No local fresh browser execution is claimed. The actual captures are the failing hosted lane's three stable attempts at the exact source above. The parent must rerun hosted Quality after committing the reviewed baseline bytes, including its downstream accessibility-artifact validation.

## Review Triage Log

- Low, patched: independent full-frame/crop review identified the below-threshold one-pixel rail separator repaint, so the original text-only description was too narrow. Corrected both rationale and implementation notes; the same six reviewed hosted PNGs remain appropriate, with no layout/content regression found.
- Independent reviewer confirmed all six expected-to-tracked identities, three stable actual attempts per variant, candidate hashes, exact hosted provenance and unchanged content/focus/spacing/density. Fresh hosted Quality remains the stated verification boundary; artifact inspection does not claim that later run or its artifact-size check has passed.
