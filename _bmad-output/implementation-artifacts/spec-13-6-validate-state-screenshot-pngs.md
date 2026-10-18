---
title: 'Story 13.6: Validate state screenshot PNG integrity'
type: 'bugfix'
created: '2026-10-18'
status: 'done'
route: 'oneshot'
story_id: '13.6'
baseline_commit: '4ee1009c947886a0437b3b93b8cabd433920b8de'
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-13-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 13.6 still accepts matching nonempty screenshot and authoritative-attachment bytes even when they are not decodable PNG images. This leaves the PNG-integrity portion of deferred BH12 unresolved.

**Approach:** Require a complete, decodable PNG with positive dimensions and bounded decoding before accepting an owned system-state screenshot. Reuse the PNG decoder bundled with the pinned Playwright package without changing dependencies. Add meaningful valid-image and matching-corruption regressions, preserve existing image ownership and the attributed WebKit media-only branch, and replay the hash-bound retained three-engine matrix through the new validator. Record exact source identity, commands and results in a scoped evidence packet and the existing accessibility result; append the BH12 resolution without editing historical evidence. Preserve all approved images/text snapshots, dependencies, policies and submodules. Broad artifact-size failures, visual/catalog decisions, actual zoom/devices/manual AT, platform gaps and owner approvals remain open; parent Story 13.6 and its sprint entry remain in review.

</frozen-after-approval>

## Implementation Notes

- Investigation: `a11y-state-evidence.mjs` validates state-local image identity and exact final-attempt attachment bytes but performs no decoding. Its existing Node suite has two genuine 1×1 PNG fixtures and 18 tests, including a capability-bound WebKit branch without images. `playwright-core/lib/utilsBundle` is exported by the pinned 1.63.0 package and supplies `PNG.sync.read`; it verifies CRC and decodes image data. Element screenshots must not be constrained to viewport dimensions. The retained packet has 464 state PNGs with maximum 1,440,000 pixels (5,760,000 RGBA bytes).
- Scope facts: no unresolved intent choice or irreversible action; implementation is confined to the validator, its existing tests and scoped evidence/documentation. The unchanged complete artifact validator remains authoritative for its 52,428,800-byte per-root gate. The prior 18 visual and remaining snapshot/catalog failures require separate owner decisions.
- Runtime prerequisite: exact-target `aspire start --non-interactive --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj` failed with exit 2, MSB4018/file-in-use before readiness. Exact-target stop and describe both exited 0 and reported no running AppHost. Retain the raw logs. This script-only correction uses Node tests and retained browser replay; it establishes no new live runtime or browser execution.
- Sprint synchronization: the resolved key is `13-6-i-ux-e-verify-responsive-and-assistive-accessibility`, already at `review`, later than `in-progress`; leave it unchanged under the workflow's no-regression rule.
- Implementation: the two existing JavaScript files now preflight chunk boundaries/checksums, header formats/dimensions, contiguous image data and terminal IEND; strictly inflate the entire zlib stream with exact normal/Adam7 scanline length and consumption checks; then decode with the bundled PNG parser and require complete RGBA output. The 16,777,216-pixel allocation bound is independent of the unchanged 50 MiB artifact-root threshold. Existing image ownership runs first; the WebKit media-only exception is unchanged. No dependency or browser producer changed.
- Verification before independent review: the first new corruption test failed against the original validator (18 passed / 1 failed). Initial correction passed 19 tests; expanded format/header fixtures passed all 21 tests with zero failures/skips. Final Node suite, TypeScript, both JavaScript syntax checks and `git diff --check` pass. The existing portable replay verifies retained report/attachment hashes and accepts 116 required states per engine with zero errors; Chromium/Firefox PNGs decode and WebKit remains attributed media-only. This is no new browser run. The source manifest matches both changed files, 26 protected baseline hashes and eight root-submodule identities; the state producer matches its retained reviewed manifest. Exact logs/commands are in `evidence/story-13-6/2026-10-18-png-integrity/verification.json`.
- Review follow-up: add a 33,554,432-byte expanded-data ceiling before either inflater sees image data, alongside the existing pixel ceiling. Expand positives to grayscale-alpha, packed two-/four-bit rows and small/narrow/odd Adam7 images; cover missing/extra interlaced pixels, valid consecutive/empty IDAT, interrupted IDAT and invalid color-depth combinations. Preserve a frozen baseline validator and final regression source with hashes plus an isolated portable red reproducer. That attributable red run reports 19 passed / 5 failed; the original unbound warm-up red remains diagnostic only. The final reviewed 24-test suite passes, and all 116 retained rows per engine replay without errors. Full encoder/ancillary metadata conformance is outside this complete-image/pixel-decoding contract.

## Review Triage Log

The independent Blind Hunter returned nine findings; its floor was `min(floor(sqrt(69.43 KiB) + 1), 10) = 9`. Each claim was checked against the decoder, tests and retained proof.

| Finding | Verdict and disposition | Evidence |
| --- | --- | --- |
| F01 pixel cap permits expensive RGBA16 decoding | Medium, patched | The reviewed valid 4096×4096 RGBA16 probe reached about 750 MiB before the fix. The new expanded-byte guard rejects its 134,221,824-byte request before inflation; the regression proves resource rejection precedes its deliberately insufficient IDAT. |
| F02 palette metadata conformance | Low, rejected; scope clarified | Decoder tolerates duplicate/late/irrelevant palette metadata while still producing complete pixels. No pixel-decoding failure was demonstrated. Full palette encoder conformance adds a separate semantic validator for metadata genuine browser PNGs do not emit. |
| F03 tRNS metadata conformance | Low, rejected; scope clarified | Forbidden/duplicate/late transparency metadata is tolerated by the bundled decoder. Its accepted output remains complete; full metadata legality is outside the bounded decodability fix. |
| F04 gAMA metadata conformance | Low, rejected; scope clarified | Malformed gamma metadata can decode. Checks guarantee chunk CRC, complete compressed pixels and decodability; no complete ancillary-metadata certification is claimed. |
| F05 reserved chunk-type bit | Low, rejected; scope clarified | Alphabetic unknown ancillary names are tolerated, CRC-checked and ignored by the decoder. Adding a future/reserved-name conformance policy does not improve the demonstrated screenshot completeness defect. |
| F06 missing contiguous-IDAT regression | Medium, patched | Consecutive and empty IDAT streams pass; a CRC-valid intervening text chunk fails. |
| F07 small/odd Adam7 coverage | Medium, patched | Genuine 1×1, 1×9 and 9×5 interlaced PNGs pass, including empty passes; missing/extra filtered bytes fail. |
| F08 grayscale-alpha/packed/color-depth coverage | Low, patched | Genuine grayscale-alpha and packed two-/four-bit images pass; RGB1 and indexed16 fail before decoding. |
| F09 initial red run lacks source identity | Medium, patched | Preserve the original diagnostic log and supersede its attribution with `red-source.json`, frozen sources, `reproduce-red.mjs` and the actual 19-pass/5-fail baseline replay. The frozen final test hash equals the reviewed production test hash. |

## Completion

The independent reviewer rechecked the patches, confirmed the 24-pass suite and attributable 19-pass/5-fail red baseline, and reported no residual implementation issue within the clarified decodability scope. The parent independently ran the reviewer's genuine 130,527-byte RGBA16 PNG probe (134,221,824 expanded bytes): the new guard rejected it with zero inflater calls and zero pixel-decoder calls in approximately 0.50 ms, with RSS changing from 97,640,448 to 97,771,520 bytes. `resource-probe.mjs` and `resource-probe.json` retain the exact probe and observation. Final reviewed source/test patch is `sha256:f36c6f62d0a2c753deb8ea3db6faa398dd50f980382b348ddd4f32da39ba6fcb`; the reviewed verification manifest records the actual commands and source hashes. No new review deferral was created.

This bounded implementation spec is done. The workflow's final sprint synchronization is a no-op because Story 13.6 is already at `review`; the parent story remains incomplete pending the separately documented gates. Follow the workspace instruction to leave the changes unstaged and uncommitted; no commit or push was requested. Existing protected baselines, catalog/policy/dependencies, eight submodules and all historical evidence remain unchanged.
