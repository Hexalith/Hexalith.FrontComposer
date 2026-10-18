---
title: 'Story 13.6: Restore generated field description layout'
type: 'bugfix'
created: '2026-10-18'
status: 'done'
route: 'oneshot'
story_id: '13.6'
baseline_commit: '6ab8d2daac6d2ee8ef2f0ce8686cb446d0b6dd0b'
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-13-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Generated field descriptions became inline siblings when moved out of Fluent's replaceable validation slot. Helpers now sit beside their editors, displacing later specimen content by 48 pixels in the retained Chromium comparisons. The latest accessibility result also incorrectly says no artifact-size threshold exists.

**Approach:** Use the existing FluentText block-layout parameter while preserving persistent descriptions, stable IDs, projected accessibility relationships and single validation errors. Extend the existing generator and destructive-command browser regressions, rebuild the Debug host, and compare against unchanged approved images. Correct the result's artifact-size reporting using the existing 50 MiB per-root validator, preserving every historical packet. Restore stale build assets against the declared catalog before rerunning the existing affected analyzer checks. Attribute every result to the actual current root and submodule identities. Preserve all approved PNGs, the two generated-grid text approvals, dependency declarations, policy and submodule pointers; keep Story 13.6 and its sprint entry in review while human, platform and owner gates remain open.

</frozen-after-approval>

## Implementation Notes

- Planning investigation: `CommandFormEmitter.EmitFieldDescription` emits a sibling `FluentText` with `Size200` but no block layout. The restored Fluent 5.0.1 API exposes `FluentText.Block`. Reuse `CommandFormEmitterTests.Emit_DescribedAndGroupedFieldsPreserveEscapedMetadataAndDeclaredOrder` and the validation/rapid-click case in `destructive-command-confirmation.spec.ts`; retain its persistent description and exactly-one-error assertions.
- The previous packet's three changed source/test hashes still match, but five root submodule pointers have advanced. Its test totals remain historical. Current and retained Builds commits select identical catalog blobs. Three analyzer checks use `--no-restore` with mixed old/new assets; consistent restores may resolve their assembly conflicts, subject to execution. Frozen catalog expectations, Fluent 5.0.0/5.0.1 approval and grid-role text approvals require separate owner decisions.
- `validate-a11y-artifacts.mjs` enforces 52,428,800 bytes per configured root. Retained `browser-parent-final/results` contains 103,254,316 file bytes and `html` 87,959,744; both exceed that limit. A read-only replay of the existing validator rejects the packet as unexpectedly large. Do not weaken the limit or remove failed proof. Firefox/WebKit visual drift, manual AT/heard speech, actual zoom, real devices, Windows, WebKit forced-palette paint and release approvals remain unresolved.
- Implemented one production change: emit FluentText.Block=true for each persistent description. Reused all 68 existing emitter compile/metadata tests. Meaningful layout coverage is in the existing destructive-command browser journey rather than a new assertion mirroring the emitted source string.
- Before the fix, the new geometry check failed on all three inline descriptions. Afterward the two complete browser specs ran 90 cases: 70 passed, 18 visual failed, two existing skips, zero flaky. Chromium heights now match approved images; residual differences are 3,378 pixels per light combination and 6,524 per dark combination. No baseline/tolerance changed. Nine focused three-engine reflow/spacing/focus cases passed.
- Review strengthened desktop validation geometry and 320px validation with all spacing overrides plus real text-clipping/page-overflow checks. The final three-engine follow-up passed 3/3; its results/HTML roots contain 10,112/543,243 bytes. Source manifests and patches distinguish the initial and final browser test revisions; production source and Debug binaries are the same.
- Debug Counter.Web, Shell.Tests and SourceTools.Tests builds passed with zero warnings/errors. Full SourceTools: 1,313 total, one unchanged Fluent-approval failure. Affected Shell: 30 total, four existing snapshot/catalog failures. Consistent Release solution restore against the declared catalog corrected stale assets; all three previously failing analyzer governance methods passed under their unchanged canonical/forced Recommended and negative-control checks. TypeScript typecheck and git diff --check passed. No complete current Shell or three-spec browser pass is claimed.
- Corrected the artifact-limit report and appended the superseding ledger evidence. Added packet-specific log inclusion and byte-preservation rules, exact source/verification manifests, a current 18-row visual inventory and a portable attachment map. The broader packet remains oversized and failed; old proof is unchanged. Aspire startup hit MSB4018/file-in-use before readiness; CLI reported no running AppHost. Owned standalone Test hosts were shut down after browser checks.
- One blind review returned ten findings. Eight were patched, the implementation-string assertion was rejected as low value under the instruction against mirrored tests, and the pre-existing duplicate HTML/results retention issue was deferred. The review triage below and the packet record each disposition. This spec completes the bounded correction only. Story 13.6 and its sprint key were already in review, so status synchronization leaves them there.

## Review Triage Log

| Finding | Verdict | Evidence / route |
| --- | --- | --- |
| F01 generator string assertion | low, rejected | Existing unit suite lacks that literal; retained red/green real generated-form geometry catches its removal. A mirrored assertion is prohibited and an additional rendering harness disproportionate. |
| F02 ignored logs | medium, patched | Narrow .gitignore exception; full packet audit finds zero ignored files. |
| F03 desktop validation geometry | medium, patched | Added pre-resize geometry after errors; final 3/3 engines pass. |
| F04 glyph clipping | medium, patched | Existing clipping/overflow helpers now inspect the validated narrow form; final 3/3 pass. |
| F05 spacing with validation | medium, patched | All overrides applied with errors visible, then cleaned up in finally; final 3/3 pass. |
| F06 verification binding | medium, patched | Exact commands/filters/exits and raw hashes in verification.json; two source/test revisions explicit. |
| F07 duplicate evidence retention | medium, deferred | Existing HTML/results duplication exceeds both 50 MiB roots; failed proof retained and ledger entry appended. Compact follow-up cannot close the broader gate. |
| F08 attachment portability | medium, patched | 130 attachment paths mapped to packet/repository roots with hashes; all targets validated. |
| F09 current machine summary | medium, patched | Current candidate/scoped outcomes/size failure separate from historical full gates. |
| F10 current visual inventory | medium, patched | All 18 current comparisons have dimensions, pixel counts and hashed artifact references. |
