---
title: '13.8 [A · E9-APP-1] Accept the Completed Fresh-Row Live Proof'
type: 'chore'
created: '2026-10-20'
status: 'done'
story_id: '13.8'
baseline_commit: 'ff93f53d25b3032fc2a0676b531258a2855949cc'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-13-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Completed Story 9.8 lacks Product acceptance; G-5/E9-APP-1 remain open independently of Epic 9 delivery.

**Approach:** Record the Product Owner's explicit decision in this spec, citing the immutable record and candidate, then reconcile approval tracking.

## Boundaries & Constraints

**Always:** Record approver, date, accept/reject, record revision, candidate, rationale, and consequences. Acceptance closes E9-APP-1/G-5/OI-1. Rejection leaves G-5 open; required correction becomes a bounded residual. No decision leaves approval open.

**Never:** Infer approval from this invocation or passing tests. Rewrite Story 9.8 evidence, Stories 9.1–9.8, or their retrospective. Claim server-allocated-key, unknown-target, multi-target, or other excluded coverage. Close DW-679, fallback residuals, G-4, Fluent/accessibility approval, or overall Product reapproval. Change runtime code, validators, dependencies, or submodules.

**Approved decision (2026-10-20):** The requesting Product Owner (session workspace label: Administrator) instructed “apply recommended” after the accept/reject/defer comparison. Record acceptance of the historical proof with its stated scope and limitations, preserve the unchanged raw bundle in a checksum-bound repository archive, and reconcile tracking. This authorizes implementation of this spec.

</frozen-after-approval>

## Code Map

- `_bmad-output/planning-artifacts/epics.md:3702` — acceptance authority.
- `_bmad-output/implementation-artifacts/tests/9-8-live-acceptance.md` — immutable record finalized at `4e9b2423c228d0f940884389ecf0bd90f0d4264b`. Cite this revision.
- `artifacts/epic-9-final-7a573763/` — raw bundle for candidate `7a5737630611b4d54b0180a3fa4c9c4ccd23a28c`. Git-ignored, local only; the narrative supplies no durable download URL.
- `tests/e2e/scripts/validate-epic9-artifacts.mjs` — existing verifier; replay its candidate revision unchanged.
- Story 13.7 continuity: UX assertions confer no Product acceptance; preserve completed implementation.

## Tasks & Acceptance

**Execution:**
- [x] This spec — append the authorized decision, rationale, exact evidence references, and limitations; reuse it as the decision receipt.
- [x] `_bmad-output/implementation-artifacts/evidence/story-13-8/9-8-live-proof-7a573763.tar.gz` — archive the original bundle unchanged; record the archive SHA-256, verify extraction against all 38 manifest entries and original bytes, and replay the candidate-era verifier. Include the unchanged candidate-era verifier beside the archive for portable replay.
- [x] `_bmad-output/planning-artifacts/prd.md` — reconcile G-5/OI-1/D-4, §8.2 and awaiting-confirmation references with a dated decision link; preserve unrelated gates/history.
- [x] Rejection correction is not applicable: the authorized decision is acceptance; the existing deferred-work ledger and its residuals remain unchanged.
- [x] This spec and `_bmad-output/implementation-artifacts/sprint-status.yaml` — reconcile Story 13.8 (line 208) and E9-APP-1 (line 722) after review/validation; preserve Epic 9 done.

**Acceptance Criteria:**
- Given the record/candidate, when Product decides, then a named, dated receipt says accept or reject and cites both identities.
- Given acceptance, when readiness is reconciled, then E9-APP-1/G-5/OI-1 close without expanding scope or changing Epic 9 completion.
- Given rejection requiring correction, when recorded, then a new residual identifies the deficiency and owner; preserve historical evidence/delivery.
- Given no decision, when status is checked, then G-5 remains open.

## Implementation Notes

- Footprint: this spec, PRD, sprint tracking, the archive plus unchanged historical verifier, and one exact-path `.gitattributes` rule preserving that verifier's original LF bytes across checkouts; no irreversible or external action. The owner selected acceptance, so no rejection residual is needed. The archive preserves existing evidence rather than adding a new proof mechanism. The current verifier remains unchanged.

## Product Decision Receipt

- **Approver:** the requesting Product Owner, identified in this session by the workspace label **Administrator**. No different personal identity or additional approving role is inferred.
- **Decision date:** 2026-10-20.
- **Decision:** **Accept** the completed historical Story 9.8 composed/live fresh-row proof within its recorded scope and limitations.
- **Authority:** the approved decision in this spec records the Product Owner's instruction **“apply recommended”** after the accept/reject/defer comparison. The selected recommendation was acceptance with the unchanged raw bundle preserved in a checksum-bound repository archive. This receipt records that explicit choice; execution of this story and passing checks do not supply approval.
- **Immutable record:** [`tests/9-8-live-acceptance.md`](tests/9-8-live-acceptance.md), finalized at revision `4e9b2423c228d0f940884389ecf0bd90f0d4264b` ([exact historical record](https://github.com/Hexalith/Hexalith.FrontComposer/blob/4e9b2423c228d0f940884389ecf0bd90f0d4264b/_bmad-output/implementation-artifacts/tests/9-8-live-acceptance.md)). The recorded live run was `2026-08-27T22:35:38Z`.
- **Accepted candidate:** `7a5737630611b4d54b0180a3fa4c9c4ccd23a28c`. Acceptance applies to that historical candidate; it is not projected onto current HEAD.

**Rationale:** the historical proof demonstrates an already-rendered generated Counter grid receiving an exact-key create and three updates, with values **41 → 44 → 52**, one first-wins localized polite status announcement, and indicator dismissal on materialization. The retained browser, runtime, report, trace, screenshot, and manifest evidence correlates to the same clean candidate and discovered endpoint. Preserving those original bytes provides a durable repository packet for the decision without rewriting the record or recapturing a new live run.

**Limitations accepted:** this is the historical exact-key Counter lane. It establishes no server-allocated-key, unknown-target, multi-target, or other excluded coverage. The exact solution default lane's historical `NU1109` restore blocker (`FsCheck.Xunit.v3 3.3.4` requiring `FsCheck 3.3.4` while the catalog selected `3.3.3`) remains part of the record. All three deferred items in the [historical Story 9.8 spec](spec-9-8-prove-composed-and-live-epic-9-acceptance.md) remain open:

1. Rebuild the complete candidate dependency graph in the serialized AppHost fallback, rather than relying on existing outputs beyond EventStore Aspire.
2. Retain structured fallback build invocations and per-command results rather than reconstructed commands and a non-empty log.
3. Apply an explicit redaction policy to the retained serialized-build log.

The unchanged candidate-era verifier passes the archived bundle; the baseline-pinned current verifier comparison below rejects its historical command sequence, so neither a current-validator pass nor a new live run is claimed.

**Raw demo-data retention:** JSON command evidence is redacted. Original traces/screenshots retain synthetic Counter inputs, and trace network captures retain local ASP.NET antiforgery fields. Trace inspection found no authentication cookie or `Authorization` header. The archive and verifier bytes are preserved unchanged; this acceptance does not rewrite or further redact the historical evidence.

**Consequences:** close **E9-APP-1, G-5, and OI-1** as accepted on 2026-10-20 and record D-4's Product acceptance as fulfilled. Epic 9, Stories 9.1–9.8, E9-AI-1…E9-AI-6, and their retrospective retain their completed delivery state and unchanged evidence. Acceptance creates no rejection-correction residual and closes neither DW-679 nor the fallback residuals. G-4, Fluent approval, accessibility approval, and overall Product re-approval remain separately open.

### Preserved proof and portable replay

The archive contains the original `artifacts/epic-9-final-7a573763/` tree under the single directory `epic-9-final-7a573763/`: **39 regular files**, comprising all **38 manifest-listed entries** plus the unchanged manifest. Every extracted file, including the manifest, was compared byte-for-byte with the original local bundle. Archive metadata is normalized; original evidence file bytes are unchanged.

| Preserved file | SHA-256 |
| --- | --- |
| [`9-8-live-proof-7a573763.tar.gz`](evidence/story-13-8/9-8-live-proof-7a573763.tar.gz) (6,051,480 bytes) | `43abe6796dcca77ba929ca33777b0816d249f840718f37c716268ebbfdca02bd` |
| [`validate-epic9-artifacts.mjs`](evidence/story-13-8/validate-epic9-artifacts.mjs), unchanged from `tests/e2e/scripts/validate-epic9-artifacts.mjs` at the accepted candidate | `62591e7d674008dcb436f041481f3adfedd663638170f47708dccb0957efadf9` |
| Extracted `epic-9-final-7a573763/checksums.sha256` | `d8133a775549eff4c4f450877cf6707062987c42bb59dd899f56ddebeba11ad0` |

Prerequisites: Bash, `tar` with gzip support, `mktemp`, GNU-compatible `sha256sum` supporting `--check`/`--quiet`, and Node (verified using `v26.4.0`). The baseline comparison also requires Git and the baseline commit's local objects. Run the following block in Bash from the repository root. It stops on checksum, extraction, or verification failure and removes temporary raw evidence on every exit. The absolute bundle path is required because the historical verifier is relocated unchanged:

```bash
(
  set -euo pipefail
  replay_dir="$(mktemp -d)"
  trap 'rm -rf -- "$replay_dir"' EXIT
  printf '%s  %s\n' \
    '43abe6796dcca77ba929ca33777b0816d249f840718f37c716268ebbfdca02bd' \
    '_bmad-output/implementation-artifacts/evidence/story-13-8/9-8-live-proof-7a573763.tar.gz' \
    '62591e7d674008dcb436f041481f3adfedd663638170f47708dccb0957efadf9' \
    '_bmad-output/implementation-artifacts/evidence/story-13-8/validate-epic9-artifacts.mjs' \
    | sha256sum --check -
  tar -xzf _bmad-output/implementation-artifacts/evidence/story-13-8/9-8-live-proof-7a573763.tar.gz -C "$replay_dir"
  (cd "$replay_dir/epic-9-final-7a573763" && sha256sum --check --quiet checksums.sha256)
  node _bmad-output/implementation-artifacts/evidence/story-13-8/validate-epic9-artifacts.mjs \
    "$replay_dir/epic-9-final-7a573763" \
    --candidate 7a5737630611b4d54b0180a3fa4c9c4ccd23a28c
)
```

The **current-verifier comparison** is bound to `tests/e2e/scripts/validate-epic9-artifacts.mjs` at baseline revision `ff93f53d25b3032fc2a0676b531258a2855949cc`. Replay it separately with the exact invocation below; its recorded result is **exit 1**, `runtime-metadata.json.commands does not equal the exact expected command sequence.` That failure is distinct from the historical verifier's exit 0 above and does not change current verifier behavior:

```bash
(
  set -euo pipefail
  comparison_dir="$(mktemp -d)"
  trap 'rm -rf -- "$comparison_dir"' EXIT
  printf '%s  %s\n' \
    '43abe6796dcca77ba929ca33777b0816d249f840718f37c716268ebbfdca02bd' \
    '_bmad-output/implementation-artifacts/evidence/story-13-8/9-8-live-proof-7a573763.tar.gz' \
    | sha256sum --check -
  tar -xzf _bmad-output/implementation-artifacts/evidence/story-13-8/9-8-live-proof-7a573763.tar.gz -C "$comparison_dir"
  (cd "$comparison_dir/epic-9-final-7a573763" && sha256sum --check --quiet checksums.sha256)
  git show ff93f53d25b3032fc2a0676b531258a2855949cc:tests/e2e/scripts/validate-epic9-artifacts.mjs \
    > "$comparison_dir/validate-epic9-artifacts.mjs"
  node "$comparison_dir/validate-epic9-artifacts.mjs" \
    "$comparison_dir/epic-9-final-7a573763" \
    --candidate 7a5737630611b4d54b0180a3fa4c9c4ccd23a28c
)
```

## Spec Change Log

- 2026-10-20: Recorded the authorized Product acceptance, preserved the original bundle and candidate-era verifier, and reconciled approval tracking. Added the exact-path byte-preservation attribute required for portable historical replay.
- 2026-10-20: Hardened replay failure/cleanup behavior, stated tool prerequisites, clarified retained synthetic/demo data and all three historical deferred items, and pinned the separate current-verifier comparison to the baseline revision. Archive/verifier bytes and frozen intent remain unchanged.

## Review Triage Log

All three independent review layers completed on 2026-10-20. Edge-case review returned no findings; verification-gap review independently checked archive/verifier hashes, safe 39-file extraction, all 38 checksums, exact candidate verifier bytes and successful replay, and found no gaps. The blind review's ten findings are individually dispositioned below. No findings were deferred. Receipt corrections leave frozen intent, original evidence, archive bytes, historical verifier and runtime unchanged.

| Finding | Verdict | Route | Evidence and disposition |
| --- | --- | --- | --- |
| B1 — raw antiforgery retention | low | receipt correction | Both trace ZIP copies contain only the local ASP.NET antiforgery cookie among cookie names; no authentication cookie or Authorization header was found. The receipt explicitly discloses retained local-demo antiforgery fields while preserving the authorized original archive. No credential value is reproduced in the receipt or diagnostics. |
| B2 — raw synthetic inputs | low | receipt correction | Candidate test fills synthetic values 41, 1, 2 and 8; only its JSON command attachment replaces values with REDACTED. The receipt now distinguishes redacted JSON from original trace/screenshot demonstration data; the historical record remains untouched. |
| B3 — complete fallback residuals | low | receipt correction | Historical spec frontmatter contains three deferred obligations. The receipt links it and explicitly preserves complete dependency rebuild, structured invocation/results and serialized-log redaction policy; none is closed. |
| B4 — replay can report success after failure | medium | patch | The previous command block could proceed after an outer checksum failure. A fail-fast Bash subshell now rejects an altered archive before invoking verification; positive replay returns 0 and the negative copy returns 1. |
| B5 — prerequisites | low | receipt correction | The recipe uses Bash, gzip-capable tar, mktemp, GNU-compatible sha256sum and Node; these and the baseline comparison's Git-object prerequisite are now explicit. |
| B6 — no viewer command | low | rejected | The retained standard Playwright tree includes playwright-report/index.html, PNG and trace ZIP, and the archive can be extracted directly. Adding report-serving/browser tooling is an optional viewing workflow rather than a defect in proof retention or exact replay. |
| B7 — moving current verifier | low | receipt correction | The comparison now extracts the exact verifier at full baseline ff93f53d25b3032fc2a0676b531258a2855949cc and records its complete invocation and expected exit-1 message separately from historical success. |
| B8 — additional verification packet | low | rejected | The receipt already retains hashes, exact reproducible commands, tool version and results, plus the original packet and verifier. A second packet of temporary inspection output adds a new evidence mechanism without a missing acceptance boundary. |
| B9 — temporary extraction retained | low | patch | Replay and comparison now use EXIT cleanup traps for success and failure; focused checks confirmed cleanup. Parent-created raw extraction scratch directories are removed after inspection. |
| B10 — authority lacks scope | false | rejected | The receipt already quotes the owner's apply-recommended instruction and states the selected recommendation: scoped historical acceptance with unchanged raw archive retention. The immutable candidate, exact record, limitations and gate consequences are explicit; no separate approving identity is invented. |

## Verification

Investigation on 2026-10-20:

- Historical record: browser 1/1; exact-key create/update counts 41 → 44 → 52; first-wins localized polite status and materialization dismissal. Preserve its default-lane restore blocker and fallback residuals as historical limitations.
- `sha256sum --check --quiet checksums.sha256` from the bundle — exit 0, 38/38 entries. Manifest SHA-256: `d8133a775549eff4c4f450877cf6707062987c42bb59dd899f56ddebeba11ad0`.
- Candidate-era verifier extracted unchanged with `git show`, invoked with Node, absolute bundle path and recorded candidate — exit 0. Script: `/tmp/fc138-historical-adfsbfdk/validate-epic9-artifacts.mjs`.
- Current verifier against the same bundle/candidate — exit 1: `runtime-metadata.json.commands does not equal the exact expected command sequence.` Historical replay does not establish a current-validator pass or new live run.

Before completion, run `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-13-8-accept-the-completed-fresh-row-live-proof.md --candidate HEAD` with an actual File List, and `git diff --check`. Compare the receipt with the owner's answer and Epic 9 statuses/evidence with baseline. No runtime build or new tests are needed.

Implementation verification on 2026-10-20:

- Archive extraction: the complete 39-file set (38 manifest entries plus the manifest) matched the original bundle byte-for-byte; `sha256sum --check --quiet checksums.sha256` from the extracted bundle exited 0.
- The retained verifier matched `git show 7a5737630611b4d54b0180a3fa4c9c4ccd23a28c:tests/e2e/scripts/validate-epic9-artifacts.mjs` exactly. Its absolute-path replay against the extracted archive and recorded candidate exited 0; the documented portable replay also exited 0 using Node `v26.4.0`.
- The current verifier replay remained exit 1 with `runtime-metadata.json.commands does not equal the exact expected command sequence.` This is the preserved historical limitation, not a failed new live proof.
- `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-13-8-accept-the-completed-fresh-row-live-proof.md --candidate HEAD` — exit 0, `Story artifact validation passed.` The actual six-file File List reconciled at baseline/candidate `ff93f53d25b3032fc2a0676b531258a2855949cc`.
- `git diff --check` — exit 0. YAML parsed successfully. Comparison with entry snapshots and the baseline confirmed unchanged frozen intent, Epic 9 history/statuses, other gates/open items/decisions, approval flags, runtime source, current validators, dependencies, and gitlinks. Git's clean filter preserved the historical verifier bytes.
- Root independently confirmed archive/verifier hashes, all 39 original file bytes, 38 checksums and historical replay in `/tmp/fc138-root-replay-_uv4tdun`. YAML comparisons confirm unchanged Epic 9 statuses, unrelated approval action items, Story 13.9, runtime/current validators/dependencies/gitlinks, original live record and deferred ledger. Story status was normalized to `in-review` before independent review; the completed three-layer review and focused receipt corrections permit this spec to transition to `done`. The sprint development row remains `review`, as the build workflow presentation step requires; the Product approval itself is accepted and closed. No runtime build or new tests were required; test-count delta is zero.

Review-fix verification on 2026-10-20: the exact documented historical replay exited 0; one altered archive copy failed its checksum with exit 1 before verification continued; both EXIT traps removed all temporary evidence. The documented baseline-pinned current-verifier comparison exited 1 with the exact recorded command-sequence message and cleaned its temporary directory. Archive/verifier hashes and frozen intent remained unchanged. These focused recipe checks were the only tests rerun for the receipt edits.

| Lane | Required command | Local result | Blocker timing | Fallback evidence | CI authority |
| --- | --- | --- | --- | --- | --- |
| Preserved historical proof | Documented archive hash/extract/checksum/Node replay above | Passed: 39 files identical, 38 checksums, historical verifier exit 0 | None | None | Local only; CI not run |
| Current verifier comparison | Current verifier, extracted bundle, exact accepted candidate | Failed: historical command sequence differs | Artifact comparison; no new tests executed | Historical verifier pass is separate evidence | Local only; no current live-run claim |
| Strict story artifacts | Exact `--story` / `--candidate HEAD` command above | Passed: six paths reconciled at the full baseline/candidate SHA above | None | None | Local only; CI not run |
| Whitespace/history | `git diff --check` and exact baseline comparisons | Passed | None | None | Local only |

Documentation sweep: PRD and sprint tracking are the current approval authorities; the decision receipt links both historical proof and portable archive. Runtime/source/API/UX contracts, adopter documentation, and current validators are unchanged, so no product documentation update is required. Dated planning/review snapshots retain their historical open-gate statements; the canonical PRD records the new acceptance.

Completion on 2026-10-20: all tasks and three independent review layers are complete, with no new deferrals. Final focused replay succeeds; the altered-copy recipe rejects corruption; the baseline-pinned comparison retains its documented exit-1 limitation. The strict artifact command passed before the done transition and must pass again through the local transition commit. The original baseline, frozen approved intent, historical record, archive/verifier hashes, Epic 9 delivery state and other approvals are preserved.

## File List

- `.gitattributes`
- `_bmad-output/implementation-artifacts/spec-13-8-accept-the-completed-fresh-row-live-proof.md`
- `_bmad-output/planning-artifacts/prd.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/evidence/story-13-8/9-8-live-proof-7a573763.tar.gz`
- `_bmad-output/implementation-artifacts/evidence/story-13-8/validate-epic9-artifacts.mjs`
