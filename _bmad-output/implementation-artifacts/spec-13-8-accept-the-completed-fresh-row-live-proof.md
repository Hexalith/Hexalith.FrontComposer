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
- [x] Rejection correction is not applicable: the authorized decision is acceptance. In the resumed edit, preserve all existing deferred-work entries and residuals and append only the authorized lifecycle-fault investigation as DW-1984; the full original-baseline story diff also includes the retained fallback-reconciliation deferral.
- [x] This spec and `_bmad-output/implementation-artifacts/sprint-status.yaml` — transition Story 13.8 spec and sprint row together to `done` after independent review/validation, and record that the dated E9-APP-1 Product decision preceded story acceptance; preserve Epic 9 done.
- [x] `_bmad-output/planning-artifacts/epics.md` — append the authorized dated G-5/E9-APP-1 acceptance annotation and Story 13.8 disposition while preserving the original Epic 9 delivery record.

**Acceptance Criteria:**
- Given the record/candidate, when Product decides, then a named, dated receipt says accept or reject and cites both identities.
- Given acceptance, when readiness is reconciled, then E9-APP-1/G-5/OI-1 close without expanding scope or changing Epic 9 completion.
- Given rejection requiring correction, when recorded, then a new residual identifies the deficiency and owner; preserve historical evidence/delivery.
- Given no decision, when status is checked, then G-5 remains open.

## Implementation Notes

- Footprint: this spec, PRD, dated annotations in `epics.md`, sprint tracking, the retained fallback-reconciliation deferral and resumed bounded DW-1984 lifecycle-fault investigation in `deferred-work.md`, the archive plus unchanged historical verifier, and one exact-path `.gitattributes` rule preserving that verifier's original LF bytes across checkouts; no irreversible or external action. The owner selected acceptance, so no rejection residual is needed. The archive preserves existing evidence rather than adding a new proof mechanism. The current verifier remains unchanged.

## Product Decision Receipt

- **Approver:** the requesting Product Owner, identified in this session by the workspace label **Administrator**; git-author identity **Jérôme Piquot**, recorded here under the 2026-10-20 review disposition and the Story 12.2 precedent. No additional approving role is inferred.
- **Decision date:** 2026-10-20.
- **Decision:** **Accept** the completed historical Story 9.8 composed/live fresh-row proof within its recorded scope and limitations.
- **Authority:** the approved decision in this spec records the Product Owner's instruction **“apply recommended”** after the accept/reject/defer comparison. The selected recommendation was acceptance with the unchanged raw bundle preserved in a checksum-bound repository archive. This receipt records that explicit choice; execution of this story and passing checks do not supply approval.
- **Immutable record:** [`tests/9-8-live-acceptance.md`](tests/9-8-live-acceptance.md), finalized at revision `4e9b2423c228d0f940884389ecf0bd90f0d4264b` ([exact historical record](https://github.com/Hexalith/Hexalith.FrontComposer/blob/4e9b2423c228d0f940884389ecf0bd90f0d4264b/_bmad-output/implementation-artifacts/tests/9-8-live-acceptance.md)). The recorded live run was `2026-08-27T22:35:38Z`.
- **Accepted candidate:** `7a5737630611b4d54b0180a3fa4c9c4ccd23a28c`. Acceptance applies to that historical candidate; it is not projected onto current HEAD.

**Rationale:** the historical Chromium browser proof demonstrates an already-rendered generated Counter grid receiving an exact-key create and three updates, with values **41 → 44 → 52**, retention of one indicator DOM element across overlapping updates, and indicator dismissal on materialization. It observes English text with `html[lang="en"]`, `role="status"`, `aria-live="polite"` and the expected accessible label; it does not observe heard speech or establish other locale/browser coverage. Internal first-wins provenance (retaining `MessageId` and `CreatedAt`) belongs to the separate historical bUnit `AlreadyRenderedGrid_FirstWinsAndEveryInvalidation_RerendersAutomatically` proof, not the browser DOM observation. The retained browser, runtime, report, trace, screenshot, and manifest evidence correlates at the metadata level to the same clean candidate and discovered endpoint. The serialized fallback launched with `--no-build` over existing outputs beyond its EventStore Aspire prebuild; this packet does not independently prove that every executing dependency was rebuilt from the candidate. Preserving those original bytes provides a durable repository packet for the decision without rewriting the record or recapturing a new live run.

**Limitations accepted:** this is the historical exact-key Counter sample lane. Its live framework composition uses `CounterSampleCommandService` over the sample lifecycle service and `CounterCommandProjectionCatchUpChannel` to drive demo catch-up; it proves neither production EventStore dispatch nor persisted projection state. It establishes no server-allocated-key, unknown-target, multi-target, or other excluded coverage. The exact solution default lane's historical `NU1109` restore blocker (`FsCheck.Xunit.v3 3.3.4` requiring `FsCheck 3.3.4` while the catalog selected `3.3.3`) remains part of the record. All three deferred items in the [historical Story 9.8 spec](spec-9-8-prove-composed-and-live-epic-9-acceptance.md) remain open:

1. Rebuild the complete candidate dependency graph in the serialized AppHost fallback, rather than relying on existing outputs beyond EventStore Aspire.
2. Retain structured fallback build invocations and per-command results rather than reconstructed commands and a non-empty log.
3. Apply an explicit redaction policy to the retained serialized-build log.

**Accepted lifecycle-log limitation:** the archived `counter-web-logs.redacted.json` contains **four Error-level EventId 5740 `LifecycleSubscriberFaulted` entries**, one for each create/update dispatch, reporting `NewState=Confirmed` and `FailureCategory=InvalidOperationException`. The browser success does not establish fault-free lifecycle delivery or identify the failing subscriber. The 2026-10-20 Product disposition retains acceptance with this limitation disclosed and opens [DW-1984](deferred-work.md#dw-1984-investigate-the-four-lifecycle-subscriber-faults-in-the-accepted-story-98-proof) for the FrontComposer Shell maintainer to investigate; this receipt does not diagnose or fix the runtime.

The unchanged candidate-era verifier passes the archived bundle; the baseline-pinned current verifier comparison below rejects its historical command sequence. Commit `d08d1dec3243a30e2541d914c3d2474910eabe1d` (2026-08-30) added required source routing and a Counter.Web prebuild after an Aspire healthy report preceded failure of a missing no-build executable. The old packet lacks those later command requirements, so neither a current-validator pass nor a new live run is claimed.

**Supplementary later proof:** [the CI/release repair spec](spec-repair-tests-ci-and-release-publication.md) records hosted Quality run [36849274110](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/36849274110) on 2026-10-01 at candidate `6a4fb3986c6b207022e85616c931c88f531a3fce`, passing live Epic 9 acceptance with the complete `isolated-no-build-after-source-graph-build` mode under the then-current verifier. This is separate contextual evidence; the accepted candidate and record above remain the sole scope of this decision, and the three historical fallback residuals remain open.

**Evidence form:** the archive retains the original browser/runtime files: command-evidence JSON, JUnit/HTML report, PNG/screenshot and trace ZIP, runtime metadata, AppHost/Counter diagnostics and checksums. The composed xUnit/bUnit results, typecheck and 87 tooling cases are historical Markdown summaries in the immutable Story 9.8 record; their raw test output is not part of this archive.

**Raw demo-data retention in this public repository:** JSON command evidence is redacted. Original traces/screenshots retain synthetic Counter inputs, and trace network captures retain local ASP.NET antiforgery fields. The archive also retains local absolute `/home/administrator/…` paths: 145 occurrences across the failed/start/describe/runtime-log JSON and `apphost-serialized-build.log`, plus paths in the trace ZIPs. The serialized-build log is unredacted, as covered by historical deferred item 3 above. The bounded inspection below found zero matches for its exact email, credential-label and JWT-shaped patterns in those five diagnostic files. In each trace ZIP's `0-trace.network`, parsed request/response header names contained zero `Authorization` entries and cookie names contained zero matches for the stated authentication-cookie-name pattern; all 28 cookie-name occurrences matched the ASP.NET antiforgery prefix. This does not establish absence of secrets elsewhere, encoded values or cookies with unrecognized names, and does not replace an explicit log-redaction policy. The archive and verifier bytes are preserved unchanged; this acceptance does not rewrite or further redact the historical evidence.

**Consequences:** close **E9-APP-1, G-5, and OI-1** as accepted on 2026-10-20 and record D-4's Product acceptance as fulfilled. Epic 9, Stories 9.1–9.8, E9-AI-1…E9-AI-6, and their retrospective retain their completed delivery state and unchanged evidence. Acceptance creates no rejection-correction residual; DW-1984 tracks the separately accepted lifecycle-log limitation. It closes neither DW-679 nor any of the three fallback residuals. G-4, Fluent approval, accessibility approval, and overall Product re-approval remain separately open.

### Preserved proof and portable replay

The archive contains the original `artifacts/epic-9-final-7a573763/` tree under the single directory `epic-9-final-7a573763/`: **39 regular files**, comprising all **38 manifest-listed entries** plus the unchanged manifest. Every extracted file, including the manifest, was compared byte-for-byte with the original local bundle. Archive mtime is normalized to epoch 0 and owner/group to 0/0; original mixed file modes 0644/0664 are retained. The original local files' mtimes span `2026-08-27T22:35:10.997734Z`–`2026-08-27T22:35:50.874953Z`, bracketing the recorded live run at `22:35:38Z`; these local mtimes are observations, not preserved archive timestamps. Original evidence file bytes are unchanged.

| Preserved file | SHA-256 |
| --- | --- |
| [`9-8-live-proof-7a573763.tar.gz`](evidence/story-13-8/9-8-live-proof-7a573763.tar.gz) (6,051,480 bytes) | `43abe6796dcca77ba929ca33777b0816d249f840718f37c716268ebbfdca02bd` |
| [`validate-epic9-artifacts.mjs`](evidence/story-13-8/validate-epic9-artifacts.mjs), unchanged from `tests/e2e/scripts/validate-epic9-artifacts.mjs` at the accepted candidate | `62591e7d674008dcb436f041481f3adfedd663638170f47708dccb0957efadf9` |
| Extracted `epic-9-final-7a573763/checksums.sha256` | `d8133a775549eff4c4f450877cf6707062987c42bb59dd899f56ddebeba11ad0` |

Prerequisites: Bash, `tar` with gzip support, `mktemp`, GNU-compatible `sha256sum` supporting `--check`/`--quiet`, and Node (verified using `v26.4.0`). The baseline comparison also requires Git and the baseline commit's local objects. Run the following block in Bash from the repository root. It stops on checksum, extraction, or verification failure and removes temporary raw evidence on every exit. After extraction, the seven hashes copied verbatim from the immutable record at `4e9b2423c228d0f940884389ecf0bd90f0d4264b` independently anchor selected files to that record before the full manifest and verifier checks; its three browser basenames map to the retained `playwright-results/epic-9-fresh-row-acceptanc-b89a4-to-an-already-rendered-grid-chromium/` files. The absolute bundle path is required because the historical verifier is relocated unchanged:

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
  (
    cd "$replay_dir/epic-9-final-7a573763"
    sha256sum --check <<'RECORD_SHA256'
12e309c4fee1d0c7e5f33f357bb43c6246a38d4a7622f71c1811b8719d17b1a5  runtime-metadata.json
f8afa3421b1885fb7065ab4356783ea81c52da36f32e68d1843f63edfe174f95  counter-web-describe.json
ce7fd5e91b3e519f7bf670771fae72a3b6439cfcaaaab05ea443907436d74463  counter-web-logs.redacted.json
f40b0752611fe2d7eca20b924924b3e7a77e4fde4e711744529aa9c1bcc1cc71  junit.xml
8b145321e07fefb28131514ff73d21b8d03dfd62b93d0e8733c9f770ed2b4722  playwright-results/epic-9-fresh-row-acceptanc-b89a4-to-an-already-rendered-grid-chromium/epic-9-command-evidence.json
85bc1a7262b59fe414cfbb86196b392e09975f9d57aaf1af782ebc5163865b8c  playwright-results/epic-9-fresh-row-acceptanc-b89a4-to-an-already-rendered-grid-chromium/epic-9-live-acceptance.png
276b6a3c30eb1d5571a4277495a22a49b0e791ee7c0c1bb20a257cec618634d4  playwright-results/epic-9-fresh-row-acceptanc-b89a4-to-an-already-rendered-grid-chromium/trace.zip
RECORD_SHA256
    sha256sum --check --quiet checksums.sha256
  )
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

### Bounded retained-data inspection

Run this Python 3 program from the repository root with `python3 -` reading the program on standard input. It reads the retained archive in memory, prints only fixed file/member names and pattern/count labels, and never prints matched values. The scope is exactly the five named diagnostics and `test.trace`, `0-trace.trace` and `0-trace.network` in each of the two named ZIP copies; header/cookie counts concern only parsed request/response arrays in `0-trace.network`. Other ZIP resources, source attachments, image pixels and encoded/encrypted values are outside this inspection. Pattern hits are not classified secrets: for example, each `0-trace.trace` has 99 email-pattern matches from snapshot-resource names. Zero counts are bounded observations, not a general credential-free assertion.

```python
import io
import json
import re
import tarfile
import zipfile

patterns = {
    "email": r"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b",
    "credential_label": r"(?i)\b(?:authorization|bearer|token|password|client[_-]?secret|access[_-]?token|refresh[_-]?token|api[_-]?key)\b",
    "jwt_shape": r"\beyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\b",
    "local_path": r"/home/administrator/",
}
auth_cookie = re.compile(r"(?i)(?:^|[._-])(?:auth|authentication|identity|jwt|access[_-]?token|refresh[_-]?token)(?:[._-]|$)")
diagnostics = [
    "apphost-start.failed.json", "apphost-serialized-build.log",
    "apphost-start.json", "counter-web-describe.json", "counter-web-logs.redacted.json",
]
zip_paths = [
    "playwright-report/data/d87d0898c5f43e6621f29e2e1c75d387458beba9.zip",
    "playwright-results/epic-9-fresh-row-acceptanc-b89a4-to-an-already-rendered-grid-chromium/trace.zip",
]

def report(name, text):
    print(name, {label: len(re.findall(pattern, text)) for label, pattern in patterns.items()})

with tarfile.open("_bmad-output/implementation-artifacts/evidence/story-13-8/9-8-live-proof-7a573763.tar.gz") as archive:
    prefix = "epic-9-final-7a573763/"
    for name in diagnostics:
        report(name, archive.extractfile(prefix + name).read().decode("utf-8"))
    for name in zip_paths:
        with zipfile.ZipFile(io.BytesIO(archive.extractfile(prefix + name).read())) as trace:
            for member in ("test.trace", "0-trace.trace", "0-trace.network"):
                report(name + "!" + member, trace.read(member).decode("utf-8"))
            snapshots = [json.loads(line)["snapshot"] for line in trace.read("0-trace.network").decode("utf-8").splitlines()]
            parts = [snapshot[side] for snapshot in snapshots for side in ("request", "response")]
            cookies = [cookie["name"] for part in parts for cookie in part["cookies"]]
            print(name, {
                "authorization_header_count": sum(header["name"].lower() == "authorization" for part in parts for header in part["headers"]),
                "cookie_name_count": len(cookies),
                "auth_cookie_name_pattern_count": sum(bool(auth_cookie.search(cookie)) for cookie in cookies),
                "antiforgery_cookie_name_count": sum(cookie.startswith(".AspNetCore.Antiforgery.") for cookie in cookies),
            })
```

## Spec Change Log

- 2026-10-20: Recorded the authorized Product acceptance, preserved the original bundle and candidate-era verifier, and reconciled approval tracking. Added the exact-path byte-preservation attribute required for portable historical replay.
- 2026-10-20: Hardened replay failure/cleanup behavior, stated tool prerequisites, clarified retained synthetic/demo data and all three historical deferred items, and pinned the separate current-verifier comparison to the baseline revision. Archive/verifier bytes and frozen intent remain unchanged.
- 2026-10-20: Applied the thirteen round-2 receipt/tracking patches, including the owner-selected lifecycle limitation and DW-1984, supplementary hosted context, named git author, dated epics annotations and seven immutable-record checksum anchors. Preserved archive/verifier/frozen intent; aligned the story at in-review / sprint review pending review closure.

- 2026-10-20: Applied resumed-review clarifications for the sample backend, DOM versus internal provenance, English Chromium markup observations, raw versus summarized evidence, bounded value-free inspection, full-story ledger scope and canonical IDs. Updated planning metadata and made Product-decision timing independent of story completion; frozen intent and acceptance criteria remain unchanged.

## Review Triage Log

Initial implementation review (before round 2): all three independent review layers completed on 2026-10-20. Edge-case review returned no findings; verification-gap review independently checked archive/verifier hashes, safe 39-file extraction, all 38 checksums, exact candidate verifier bytes and successful replay, and found no gaps. The blind review's ten findings are individually dispositioned below. No findings were deferred. Receipt corrections leave frozen intent, original evidence, archive bytes, historical verifier and runtime unchanged.

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

### Review Findings

Code review round 2 (`bmad-code-review`, 2026-10-20) of `ff93f53d..172a6066`: Blind (R2-B), Edge Case (R2-E), Verification Gap (R2-V) and Acceptance (R2-A) layers, all completed. The receipt, replay recipe and Verification sections are this story's deliverable (Task 1), so findings that correct them are routed normally. Only the frozen intent and acceptance criteria are treated as the spec under review.

- [x] [Review][Patch] Accepted run's logs carry four undisclosed Error-level lifecycle faults [spec-13-8-accept-the-completed-fresh-row-live-proof.md:72] — R2-A1; decided 2026-10-20: disclose as an accepted limitation and open a bounded investigation residual; acceptance stands. Archived `counter-web-logs.redacted.json` has one `fail: …LifecycleStateService[5740]` "Lifecycle subscriber callback faulted … NewState=Confirmed FailureCategory=InvalidOperationException" per create/update dispatch (EventId 5740, `LogLevel.Error`, `FrontComposerHotPathLog.cs:784`). Neither the 9.8 record, this receipt, nor any `_bmad-output` document mentions them, and the Rationale says the runtime evidence "correlates". The Product Owner must choose: disclose as an accepted limitation, possibly with an investigation residual, or reconsider acceptance with it in view. **Resolved: the receipt discloses all four EventId 5740 errors as an accepted limitation and appends bounded, owned DW-1984; acceptance stands.**
- [x] [Review][Patch] Later hosted live Epic 9 acceptance is not recorded [spec-13-8-accept-the-completed-fresh-row-live-proof.md:72] — R2-B2; decided 2026-10-20: cite as supplementary context only, outside the accepted scope. `spec-repair-tests-ci-and-release-publication.md` records Quality run 36849274110 at `6a4fb398` (2026-10-01) passing live Epic 9 acceptance in the complete `isolated-no-build-after-source-graph-build` mode under the then-current verifier. The receipt's accept/reject/defer basis omits it. Choose whether to cite it as supplementary context without expanding the accepted scope, or leave it out. **Resolved: the receipt links the 2026-10-01 hosted run as supplementary context only and preserves the exact historical acceptance scope.**
- [x] [Review][Patch] `epics.md` still states G-5/E9-APP-1 is open [epics.md:1784] — R2-B4, R2-E6; decided 2026-10-20: annotate `epics.md` (dated note plus Story 13.8 disposition, 12.2 precedent) and add it to the File List. `epics.md:1784` ("G-5/E9-APP-1 remains an open Product approval task") now contradicts the PRD. The spec footprint excludes `epics.md`. The precedents differ: Story 12.2 added a dated disposition to `epics.md`, and MCP-APP-1 did not. Choose whether to widen the footprint with a dated annotation plus a Story 13.8 disposition, or leave `epics.md` for a later planning sync. **Resolved: dated annotations preserve the original Epic 9 statement and record the later G-5/E9-APP-1 closure plus Story 13.8 disposition; epics.md is in the File List.**
- [x] [Review][Patch] Approver is recorded only by workspace label "Administrator" [spec-13-8-accept-the-completed-fresh-row-live-proof.md:61] — R2-B6, R2-A6; decided 2026-10-20: keep the label and add the git-author identity (12.2 precedent). AC1 asks for a "named, dated receipt". The frozen intent uses the session label. The Story 12.2 precedent named the decision owner as the git author. Choose whether the label suffices or the receipt should add the git-author identity. The original accept/reject/defer comparison text cannot be reconstructed after the fact; only the selected recommendation is recorded. **Resolved: the receipt records git-author identity Jérôme Piquot beside the Administrator session label under the authorized disposition.**
- [x] [Review][Patch] Replay never anchors the archive to the immutable record's seven pinned hashes [spec-13-8-accept-the-completed-fresh-row-live-proof.md:94] — R2-V1, R2-B7. Add a `sha256sum --check` step after extraction for the seven "Selected final-bundle SHA-256 values" in `tests/9-8-live-acceptance.md:51-61` at `4e9b2423`. All seven currently match their archive paths. **Resolved: portable replay checks the exact seven immutable-record hashes after extraction, before checking all 38 manifest entries and running the historical verifier.**
- [x] [Review][Patch] Raw-data disclosure omits local absolute paths and the unredacted serialized-build log [spec-13-8-accept-the-completed-fresh-row-live-proof.md:80] — R2-B9, R2-A2. 145 `/home/administrator/…` paths appear across `apphost-start.failed.json` (93), `apphost-serialized-build.log` (44, unredacted, deferred item 3), `apphost-start.json`, `counter-web-describe.json` and `counter-web-logs.redacted.json`, plus the trace ZIPs. No email, token or credential pattern was found. The repository is public. **Resolved: public-repository retention disclosure names the local absolute paths and unredacted serialized-build log without reproducing any credential value.**
- [x] [Review][Patch] Lifecycle statuses disagree: the spec is `done`, the sprint row is `review`, and E9-APP-1 closed before story acceptance [spec-13-8-accept-the-completed-fresh-row-live-proof.md:5] — R2-B5, R2-E4, R2-E5, R2-V2, R2-A3. The Story 13.7 precedent kept the spec `in-review` while its row was `review`. The validator checks this pairing only for `11.N` stories. Keep the spec status aligned with `sprint-status.yaml:208`. Record on the E9-APP-1 entry (`sprint-status.yaml:727`) that the dated Product decision preceded story acceptance, and transition everything together when the review closes. **Resolved: spec and sprint were aligned at review, then transitioned together to done after independent review/validation. E9-APP-1 records that its dated Product decision preceded story acceptance.**
- [x] [Review][Patch] PRD Table A drops "Dated approval records exist" for UX-contract approvals [prd.md:154] — R2-A4. The rewording removes an unrelated G-4 condition, which violates "preserve unrelated gates". **Resolved: Table A again requires dated UX-contract approval records; G-4 remains open.**
- [x] [Review][Patch] PRD approval-gate header lost its 2026-09-22 date and says "dated updates below" [prd.md:42] — R2-A5, R2-B13a. G-4's "Open" state is now undated, and the G-5 update is inline, not below. **Resolved: the approval-gate header restores the 2026-09-22 default date and permits newer inline dated updates.**
- [x] [Review][Patch] Rationale over-reads candidate correlation and leaves the current-verifier rejection cause unnamed [spec-13-8-accept-the-completed-fresh-row-live-proof.md:70] — R2-B1. The correlation is metadata-level; the fallback launched with `--no-build` over existing outputs (deferred item 1). The baseline verifier rejects because `d08d1dec` (2026-08-30) added source routing and a Counter.Web prebuild after `counter-web` reported healthy before its missing no-build executable failed. **Resolved: rationale limits correlation to metadata, discloses no-build output reuse, and identifies the d08d1dec source-routing/Counter.Web prebuild change behind current-verifier rejection.**
- [x] [Review][Patch] Archive-metadata statement is imprecise and the original capture mtimes are unrecorded [spec-13-8-accept-the-completed-fresh-row-live-proof.md:84] — R2-B8. The archive normalizes mtime to epoch 0 and owner to 0/0 but keeps mixed 644/664 modes. The local bundle's original mtimes (2026-08-27T22:35:10Z–22:35:50Z) bracket the recorded run but appear nowhere. **Resolved: the receipt states normalized mtime and ownership, retained mixed modes, and original local capture-time observations.**
- [x] [Review][Patch] Verification cites deleted temporary directories as evidence [spec-13-8-accept-the-completed-fresh-row-live-proof.md:162] — R2-B11, R2-A7. `/tmp/fc138-historical-adfsbfdk/…` and `/tmp/fc138-root-replay-_uv4tdun` were removed (B9). They cannot be checked from the repository. **Resolved: verification now points to retained repository artifacts and exact replay commands; deleted scratch directories are not cited as evidence.**
- [x] [Review][Patch] PRD D-4 cell names one residual while the receipt keeps three open [prd.md:685] — R2-A8. The edited row still says "Residual: 9.8 deferred AppHost-fallback rebuild", while the receipt and §12.1 carry all three 9.8 deferred items. **Resolved: D-4 now names complete dependency rebuild, structured build results and serialized-log redaction policy as all three open residuals.**
- [x] [Review][Defer] Story 9.8 fallback residuals have no ledger ID or owner, and may be superseded by the source-graph mode [spec-13-8-accept-the-completed-fresh-row-live-proof.md:72] — R2-B3. Deferred, pre-existing: the three items live only in the 9.8 spec frontmatter and PRD §12.1. `c6068f59` added `isolated-no-build-after-source-graph-build` for current tooling, and the frozen Never-list forbids closing them here.

**Rejected:**

- R2-B10, R2-E2 — the negative comparison accepts any failure as its expected exit 1 — `low`. A checksum, extraction or baseline-object failure prints its own diagnostic before exiting, which differs visibly from the recorded verifier message. Fixing it would add an output-capture assertion branch to a manual comparison.
- R2-E1 — a symlinked TMPDIR silently skips the comparison verifier — `low`. The `isMain` guard (`validate-epic9-artifacts.mjs:426`) does exit 0 when the absolute path crosses a symlink. This affects only the negative comparison, needs macOS-style TMPDIR plus GNU coreutils, and visibly contradicts the recorded exit 1. The fix would add a guard.
- R2-E3 — replay through a symlink skips verification — `false`. The relative invocation from a symlinked working directory still ran the verifier (tested: ENOENT, exit 1), because `process.cwd()` is physical.
- R2-B12 — the hash-bound tar.gz lacks an explicit `-text` rule — `false`. `git ls-files --eol` reports `i/-text w/-text`, so binary detection prevents any conversion, and the spec deliberately declares one exact-path rule.
- R2-B13b — SM-7 and FR-13 do not cite the acceptance — `false`. Both are delivery and runtime-evidence statements. Approval state lives in G-5, OI-1, D-4 and §8.2, which are all reconciled.
- R2-B13c — the G-5 "Must cite" cell points at the record, not the archive — `false`. The cell names the pass condition's required evidence and now also links the decision receipt that binds the archive.

### Resumed Build review (2026-10-20)

All three required independent layers completed against the original-baseline diff: edge-case review returned no findings and verification-gap review found no gaps. The blind review returned ten findings. As authorized in the round-2 clarification above, the Product receipt and replay documentation are Task 1 deliverables; the frozen intent and acceptance criteria remain the specification and are unchanged by these receipt corrections.

| Finding | Verdict | Route | Evidence and disposition |
| --- | --- | --- | --- |
| R3-B1 — sample backend boundary | low | receipt correction | Candidate CounterSampleCommandService decorates the sample lifecycle and publishes the demo catch-up channel. State explicitly that browser/framework composition does not prove production EventStore dispatch or persisted projection state. Resolved: the receipt names the sample lifecycle/catch-up boundary and excludes production dispatch/persisted projection proof. |
| R3-B2 — first-wins provenance basis | low | receipt correction | The candidate Playwright source explicitly reserves composite-key provenance for bUnit; browser evidence checks retained DOM identity. Separate those evidence bases in the rationale. Resolved: the receipt separates retained browser DOM identity from the historical bUnit MessageId/CreatedAt provenance proof. |
| R3-B3 — announcement observation scope | low | receipt correction | Candidate assertions check English text, role and aria-live in Chromium. Describe observed markup without implying heard speech, other locales or other browsers. Resolved: observations are limited to English DOM/ARIA markup under Chromium, without heard-speech or broader locale/browser claims. |
| R3-B4 — raw versus summarized evidence | low | receipt correction | The 39-file archive includes browser/runtime evidence but no composed xUnit, typecheck or tooling-case raw results. Identify those checks as historical Markdown summaries. Resolved: the receipt distinguishes retained raw browser/runtime files from historical Markdown xUnit/typecheck/87-case summaries. |
| R3-B5 — reproducible retained-data inspection | low | receipt correction | The receipt records negative diagnostic/trace inspection conclusions without recording member scope or patterns. Bound the claims and provide a value-free reproducible inspection method; preserve archive bytes. Resolved: exact scoped diagnostic/ZIP patterns and parsed header/cookie-name counts are reproducible without printing values. |
| R3-B6 — expected comparison failure assertion | low | rejected | carried: R2-B10/R2-E2 already rejected this same claim. The manual comparison prints the exact expected diagnostic separately from checksum/extraction/Git failures; an assertion branch would add complexity to a comparison whose expected exit and text are explicit. |
| R3-B7 — symlinked comparison directory | low | rejected | carried: R2-E1 already records the baseline verifier isMain guard and rejects adding a guard for the unusual symlinked TMPDIR case. The unexpected exit 0 visibly differs from the required recorded exit 1. |
| R3-B8 — ledger scope description | low | receipt correction | The original-baseline diff includes the already-committed fallback-reconciliation deferral and the resumed DW-1984 addition. Qualify resumed-edit versus complete-story ledger claims so both additions are accounted for. Resolved: ledger descriptions distinguish the sole resumed DW-1984 append from both additions in the complete original-baseline diff. |
| R3-B9 — stale epics updated date | low | patch | epics.md adds dated 2026-10-20 dispositions while retaining updated: 2026-09-27. Correct the metadata to the actual planning-edit date. Resolved: epics updated metadata is 2026-10-20. |
| R3-B10 — status mirrors at completion | low | patch | The new epics disposition and E9-APP-1 decision_timing embed present-tense review states. Make those timing notes historical or independent of delivery state, so the final synchronized lifecycle transition cannot leave stale mirrors. Resolved: epics disposition and E9-APP-1 timing notes are historical/state-independent; spec and sprint were aligned at review and transitioned together to done on completion. |

## Verification

Investigation on 2026-10-20:

- Historical record: Chromium browser 1/1; exact-key create/update counts 41 → 44 → 52; retained indicator DOM identity, English status text/ARIA markup and materialization dismissal. The separate summarized bUnit proof covers internal first-wins provenance. Preserve its default-lane restore blocker and fallback residuals as historical limitations.
- `sha256sum --check --quiet checksums.sha256` from the bundle — exit 0, 38/38 entries. Manifest SHA-256: `d8133a775549eff4c4f450877cf6707062987c42bb59dd899f56ddebeba11ad0`.
- Candidate-era verifier retained unchanged at [`evidence/story-13-8/validate-epic9-artifacts.mjs`](evidence/story-13-8/validate-epic9-artifacts.mjs), invoked with Node, the extracted absolute bundle path and recorded candidate — exit 0. The portable recipe above reproduces this check from repository artifacts.
- Current verifier against the same bundle/candidate — exit 1: `runtime-metadata.json.commands does not equal the exact expected command sequence.` Historical replay does not establish a current-validator pass or new live run.

Before completion, run `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-13-8-accept-the-completed-fresh-row-live-proof.md --candidate HEAD` with an actual File List, and `git diff --check`. Compare the receipt with the owner's answer and Epic 9 statuses/evidence with baseline. No runtime build or new tests are needed.

Implementation verification on 2026-10-20:

- Archive extraction: the complete 39-file set (38 manifest entries plus the manifest) matched the original bundle byte-for-byte; `sha256sum --check --quiet checksums.sha256` from the extracted bundle exited 0.
- The retained verifier matched `git show 7a5737630611b4d54b0180a3fa4c9c4ccd23a28c:tests/e2e/scripts/validate-epic9-artifacts.mjs` exactly. Its absolute-path replay against the extracted archive and recorded candidate exited 0; the documented portable replay also exited 0 using Node `v26.4.0`.
- The current verifier replay remained exit 1 with `runtime-metadata.json.commands does not equal the exact expected command sequence.` This is the preserved historical limitation, not a failed new live proof.
- `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-13-8-accept-the-completed-fresh-row-live-proof.md --candidate HEAD` — exit 0, `Story artifact validation passed.` The initial six-file implementation inventory passed; the round-2 eight-path inventory and unchanged original baseline are verified separately below.
- `git diff --check` — exit 0. YAML parsed successfully. Comparison with entry snapshots and the baseline confirmed unchanged frozen intent, Epic 9 history/statuses, other gates/open items/decisions, approval flags, runtime source, current validators, dependencies, and gitlinks. Git's clean filter preserved the historical verifier bytes.
- Root independently confirmed the retained archive/verifier hashes, all 39 original file bytes, 38 checksums and historical replay using the repository artifacts linked above. YAML comparisons confirmed unchanged Epic 9 statuses, unrelated approval action items and Story 13.9; runtime/current validators/dependencies/gitlinks and the original live record also remained unchanged. The resumed round-2 edit appends only DW-1984 to the then-existing deferred ledger; the complete original-baseline story diff contains both the previously committed fallback-reconciliation deferral and DW-1984. At the review handoff, this spec was `in-review` with sprint `review`; the separate dated Product approval was already accepted and closed. The joint completion transition is recorded below. No runtime build or new tests were required; test-count delta is zero.

Review-fix verification on 2026-10-20: the exact documented historical replay exited 0; one altered archive copy failed its checksum with exit 1 before verification continued; both EXIT traps removed all temporary evidence. The documented baseline-pinned current-verifier comparison exited 1 with the exact recorded command-sequence message and cleaned its temporary directory. Archive/verifier hashes and frozen intent remained unchanged. These focused recipe checks were the only tests rerun for the receipt edits.

Round-2 verification on 2026-10-20:

- Executed the exact documented historical Bash block using Node `v26.4.0`: archive/verifier hashes, all seven immutable-record anchors, all 38 manifest checksums and the candidate-era verifier passed, exit 0. The record's three browser basenames resolve to their retained nested Playwright result paths; an initial recipe check caught and corrected a root-path assumption before the passing replay.
- Executed the exact baseline-pinned comparison block: exit 1 with `runtime-metadata.json.commands does not equal the exact expected command sequence.` An altered archive copy failed the outer hash check with exit 1 before verification continued. All success/failure EXIT traps removed their temporary evidence.
- Rechecked all 39 archived file bytes against the original local bundle, archive/verifier/manifest hashes, original file mtimes, normalized tar metadata and exact candidate-era verifier bytes. Frozen approved intent and the original baseline remain unchanged. The resumed edit's sole ledger append is open DW-1984; the full original-baseline story diff also retains the previously committed fallback-reconciliation deferral.
- Strict artifact validation with the exact `--story` / `--candidate HEAD` command above passed at candidate `913c4132c71cd7f77fcdf6befa9e396d284dd4bc` over original baseline `ff93f53d25b3032fc2a0676b531258a2855949cc`, reconciling all eight File List paths and the five pending documentation/tracking edits. YAML/status and preservation comparisons passed; `git diff --check` passed. Local checks only; no CI or new live run was performed.

| Lane | Required command | Local result | Blocker timing | Fallback evidence | CI authority |
| --- | --- | --- | --- | --- | --- |
| Preserved historical proof | Documented archive hash/extract/checksum/Node replay above | Passed: 39 files identical, 38 checksums, historical verifier exit 0 | None | None | Local only; CI not run |
| Current verifier comparison | Current verifier, extracted bundle, exact accepted candidate | Failed: historical command sequence differs | Artifact comparison; no new tests executed | Historical verifier pass is separate evidence | Local only; no current live-run claim |
| Strict story artifacts | Exact `--story` / `--candidate HEAD` command above | Passed: all eight paths reconciled at candidate `913c4132c71cd7f77fcdf6befa9e396d284dd4bc`; original baseline retained | None | None | Local only; CI not run |
| Whitespace/history | `git diff --check` and exact baseline comparisons | Passed | None | None | Local only |

Documentation sweep: PRD and sprint tracking are the current approval authorities; the decision receipt links both historical proof and portable archive. The authorized dated annotations in `epics.md` preserve its historical open-gate statement and record the subsequent acceptance. Runtime/source/API/UX contracts, adopter documentation, and current validators are unchanged, so no product documentation update is required. Other dated planning/review snapshots retain their historical statements.

Round-2 corrections on 2026-10-20: all thirteen patch findings are addressed; the separately accepted lifecycle-log limitation is tracked in open DW-1984 and the pre-existing fallback-reconciliation deferral stays open. Story 13.8 was handed off as `in-review` / sprint `review` pending independent review closure. The original baseline, frozen approved intent, historical record, archive/verifier hashes, Epic 9 delivery state and other approvals are preserved. Independent review is now closed; the spec and sprint row transition together to done, with strict artifact validation repeated through the workflow-authorized local completion commit.

Final resumed-Build verification and completion (2026-10-20): all three required independent review layers completed; edge-case and verification-gap layers found no new gaps, and every blind finding was individually triaged. Eight bounded receipt/planning corrections are resolved; two already-dispositioned comparison limitations were carried without reopening. No new review deferrals were added. The separately authorized DW-1984 investigation and previously committed fallback-reconciliation deferral remain open.

Root executed both exact Bash recipes: historical replay exited 0 after the seven immutable-record pins, 38 manifest checksums and unchanged candidate verifier; the baseline comparison exited 1 with exactly the documented command-sequence diagnostic. An altered archive was rejected before verification, and all extraction cleanup traps passed. The embedded Python inspection exited 0 and reproduced its 13 names/count-only output lines. Preservation checks passed for frozen intent/AC, baseline, immutable record Git blob, archive/verifier hashes, all 39 original bytes, Epic 9 delivery, unrelated approval/action records, G-4/overall Product flags, existing ledger entries and CRLF. The full story footprint remains eight paths. No runtime build, CI or new live run was performed.

The pinned @commitlint/cli@21.2.2 validated the exact local completion message `docs(13.8): resolve proof acceptance review and close tracking` with zero problems and warnings before use. Successful validation output is retained in the session and its local validation log. The spec and sprint Story 13.8 row are now both done, preserving the approved synchronized-transition correction; E9-APP-1 remains closed by the earlier Product decision. Strict story-artifact validation and whitespace checks are repeated immediately before and after the local commit.

## File List

- `.gitattributes`
- `_bmad-output/implementation-artifacts/spec-13-8-accept-the-completed-fresh-row-live-proof.md`
- `_bmad-output/planning-artifacts/prd.md`
- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/evidence/story-13-8/9-8-live-proof-7a573763.tar.gz`
- `_bmad-output/implementation-artifacts/evidence/story-13-8/validate-epic9-artifacts.mjs`
- `_bmad-output/implementation-artifacts/deferred-work.md`
