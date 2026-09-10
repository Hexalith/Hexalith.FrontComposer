# Reviewer Gate — Consolidated Validation Report — GOV-1 Architecture Spine

| Field | Value |
| --- | --- |
| Spine | `ARCHITECTURE-SPINE.md` — GOV-1 "dependency provenance" (16 ADs, `status: final`, `created`/`updated: 2026-07-19`) |
| Altitude | EPIC — GOV-1 |
| Validated | 2026-09-08 (bmad-architecture VALIDATE run) |
| Gate composition | deterministic lint (`lint_spine.py`, 0 findings) + rubric walker + 2 configured reviewers (current-fit, adversarial) + 2 ad-hoc lenses (implementation drift, security/trust boundary) |
| Review 1 | `reviews/review-rubric-validate-2026-09-08.md` |
| Review 2 | `reviews/review-current-fit-validate-2026-09-08.md` |
| Review 3 | `reviews/review-adversarial-validate-2026-09-08.md` |
| Review 4 | `reviews/review-code-drift-validate-2026-09-08.md` |
| Review 5 | `reviews/review-security-validate-2026-09-08.md` |

## Gate verdict

**FAIL** — the spine is no longer convergent for the release-integration seam (AD-11–AD-16) and for AD-9's fallback-authorization guarantee; the graph core (AD-1–AD-8, AD-10) holds and is ratified by the shipped code.

### Lens scoreboard

| Lens | File | Verdict | C | H | M | L |
| --- | --- | --- | --- | --- | --- | --- |
| Deterministic lint | `lint_spine.py` | PASS | 0 | 0 | 0 | 0 |
| Rubric walker (good-spine checklist) | `reviews/review-rubric-validate-2026-09-08.md` | FAIL | 0 | 4 | 7 | 5 |
| Current-fit (currency / reality check) | `reviews/review-current-fit-validate-2026-09-08.md` | PASS WITH FINDINGS | 0 | 2 | 1 | 5 |
| Adversarial divergence | `reviews/review-adversarial-validate-2026-09-08.md` | FAIL | 1 | 5 | 8 | 4 |
| Implementation drift (ad-hoc) | `reviews/review-code-drift-validate-2026-09-08.md` | FAIL | 0 | 5 | 7* | 6* |
| Security / trust boundary (ad-hoc) | `reviews/review-security-validate-2026-09-08.md` | PASS WITH FINDINGS | 0 | 1 | 6 | 4 |
| **Raw total** | | | **1** | **17** | **29** | **24** |
| **Merged Critical/High rows** (below) | | | **1** | **10** | | |

\* The code-drift review's header states 7 Medium / 6 Low; its findings table contains 6 Medium (D-6–D-11) and 7 Low (D-12–D-18). The header counts are kept here; the table is authoritative for IDs.

## Critical and High findings (merged across lenses)

| # | ID(s) | Lens(es) | Severity | AD(s) | Finding | Proposed disposition |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | A-1 | adversarial | **Critical** | AD-9 | Fallback approval digest: the spine pins the formula but not who records `approved_against_fingerprints_sha256`, when, or that the release run may not write it. Shipped code rebinds the digest live before comparing, so graph/policy/workflow drift never invalidates a fallback approval — `publish_authorized=true` after drift. | **autofix** spine (immutable Release-Owner-recorded input; no release/prepublish/verification step may write, default, or rebind it; fixture proving drift invalidates) **+ escalate** implementation nonconformance; **owner decision** on which authority (spine intent vs. code comment) stands. |
| 2 | A-5 | adversarial | High | AD-9, AD-14 | `workflow_definition_digest` is used in the fallback formula but defined nowhere (AD-14 offers two candidates: combined ci+release vs CI-only); `fallback_invalidation_fingerprints` value type unpinned (shipped adds `helper_version` pseudo-key). | **autofix** — pin `workflow_definition` = `workflow_provenance.definition_digest` (combined) and `definition` = `FALLBACK_INVALIDATION_FILES` map `{path: sha256}` with no other keys. |
| 3 | R-2, C-2, D-1 (+S-2 M) | rubric, current-fit, code-drift, security | High | AD-13 (AD-14, AD-16) | Release model drift (T1): spine binds a `workflow_run` caller passing CI run ID + event head, `dependency-release-handoff.v1`, and a REL-4 freeze; implementation is operator `workflow_dispatch` from the exact `main` SHA, CI run selected via Actions API (`release_contract.py select-ci`), no freeze guard; parent `architecture.md` (2026-08-04+) already describes the new model and says it "deliberately replaces the pending AD-16 evaluator handoff". Neither document says which is authoritative. | **discuss** — Architect + Release Owner choose the authoritative model; then amend AD-13 (dispatch SHA == live main == authenticated push-CI head == handoff candidate), drop `workflow_run`/REL-4 wording, add Inherited Invariants row to the parent section. S-2's actor/environment binding rides on the same amendment. |
| 4 | R-4, C-1, D-2 | rubric, current-fit, code-drift | High | AD-16, AD-13, Deferred | AD-16 / external gate stale (T2): Rule text is a dated narrative ("issue 17 closed 2026-07-20 … Release Owner reopens it", "current `@main` caller non-conforming") that is now false — `ci.yml`/`release.yml` pin Builds `4eb33928…`, `evaluator_authorizations` populated, v4.1.0/v4.1.1 published 2026-08-14. But the formal "owner-accepted immutable revision" record the spine demands is not evidenced (see inconsistency note below). | **discuss** — confirm whether BUILD-REL-1 at Builds `4eb33928…` is the owner-accepted revision and where that acceptance is recorded (or record the dated contingency decision); then **autofix** AD-16 to the timeless invariant, move chronology to memlog, delete "current @main" sentences from AD-13, collapse Deferred bullet 3. |
| 5 | R-3, D-3 | rubric, code-drift | High | AD-14, AD-9 | Manifest v3 shipped; spine still binds v2 (T3): AD-14 names `hexalith.release-evidence.v2` as *the* schema; production emits v3 with a reduced `workflow_provenance` shape (`ci.run{…}+evidence_sha256`, `release{caller,reusable,builds_execution_sha}`); v2 is verify-only. Whether v2 remains publishable is undecided; AD-9 defines only the "V2 fallback approval" digest. | **discuss → autofix** — add AD-17 "manifest v3" (closed member set, v2 disposition = audit/verify-only, v3 fallback-digest recipe, transitive action-closure binding via `ci.evidence_sha256`); retitle AD-14 as the v1→v2 one-way rule. |
| 6 | R-1 | rubric | High | Frontmatter, AD-11, AD-12, AD-13, `.memlog.md` | Spine metadata integrity (T7): materially edited 2026-08-03 in commit `8a6a6cb3` (a test commit) — AD-11/12/13 Rules rewritten — while `status: final`, `updated: 2026-07-19` unchanged, memlog ends at "spine finalized", no reviewer gate re-ran. | **autofix** — memlog event entry for the 2026-08-03 edit, bump `updated`, record this gate; rule: post-`final` edits require memlog + re-gate in the same change. |
| 7 | A-2 | adversarial | High | AD-8, AD-12 | "added/changed/removed" is used but "changed" is undefined for a logical edge keyed `(owner_repository, path, repository)`; "graph is unchanged" (bootstrap) has no equality relation. Shipped code compares all members except depth-1 `owner_commit` — useful, but not required by the spine. | **autofix** — define the unchanged relation (commit, depth, catalog members equal; depth-1 `owner_commit` excluded) and reuse it in AD-12. |
| 8 | A-3 | adversarial | High | AD-13 | Closure resolution of a "local" `uses: ./.hexalith/builds-execution/…` path materialised by a prior `actions/checkout` of Builds is undefined: spine-literal reading fails closed (path not a caller blob); shipped closure regex-parses checkout `with.repository/ref/path` — a runtime step argument the spine says the static closure must not trace. Two conforming closures, two `definition_digest`s. | **discuss → autofix** — Architect decides: explicit `checkout_injections` in the policy row, or forbid checkout-injected local actions and require `uses: Hexalith/Hexalith.Builds/...@<40-hex>`. |
| 9 | A-4, D-5 | adversarial, code-drift | High | AD-12, AD-13, AD-15 | Post-release stage (T5): `reusable` is a mandatory non-null object but the `post_release` caller has no reusable `uses:`; shipped policy names an unreachable Builds workflow as a second closure root. Independently, `release-evidence.yml` never drafts/validates a `post_release` evaluator (`verify-release` without `--root`) and the HEAD blob matches none of the five stale `post_release` rows — nothing consumes them, so nothing fails. | **autofix** spine (nullable `reusable`; closures contain only caller-reachable sources) **+ autofix code/policy** (`draft-evaluator --stage post_release` + authorization check; re-authorize current blob) or **discuss** dropping the stage. |
| 10 | A-6 | adversarial | High | AD-15 | AD-15 `evaluator` ("the actual … closure plus its digest") admits three conforming shapes (AD-13 `{caller,reusable,actions,definition_digest}`, AD-14 `release` shape + sibling digest, AD-12 registry row with `closure_digest`); three raw SHA-256 values. | **autofix** — pin to the AD-13 evaluator shape. |
| 11 | D-4, S-1 | code-drift, security | High | AD-13, AD-12, AD-8 | Runtime identity / evaluator trust (T6): no workflow records or validates `github.workflow_sha`/`job.workflow_sha`; the evaluator is projected from the policy row by caller blob hash, and `workflow_source_closure.py` is never executed outside unit tests. Security adds that the evaluator *code* (`eng/*.py`, Governance) runs from the candidate checkout, so delayed activation does not cover the enforcement code — one merged change can alter `.gitmodules` and the loader. | **discuss** — wire the closure into `ci.yml`/`release.yml` and record `job.workflow_sha`, or downgrade the clause with a Deferred entry; decide whether evaluator code is executed from the active policy commit (S-1 option a) or the acceptance is written down with S-5 as compensating control (option b). |

**A-1 parent verification (verbatim):** Parent verification 2026-09-08: confirmed in eng/release_evidence.py — the production `classify` call (line 3834, `bind_live_fallback_digest=True`) causes lines 2727-2743 to overwrite `fallback['approved_against_fingerprints_sha256']` with the live-computed digest immediately before `fallback_complete` compares it. The code comment documents this as deliberate (protect an approver+expiry contingency approval from invalidation by every release-definition edit). It contradicts AD-9's stated intent. This is a spine-vs-code conflict requiring an owner decision, not a reviewer misread.

Note: the code-drift lens's per-AD scorecard marks AD-9 "conforms" (fallback digest material binds graph/policy/workflow), which is true of the *formula* but not of the *write path* A-1 identifies. The parent verification settles the fact in A-1's favour.

## Cross-lens themes

**T1 — Release model drift (R-2, C-2, D-1, D-2, S-2).** The spine binds a `workflow_run` caller, the `dependency-release-handoff.v1` hop, and a REL-4 publication freeze. The implementation moved to operator `workflow_dispatch` from an exact `main` SHA authenticated against a completed push-CI run (`release_contract.py select-ci`), the freeze guard is gone, and v4.1.0/v4.1.1 were published 2026-08-14. The protective intent (exact CI-tested revision, no later default-branch head) survives, but AD-13's mechanism text is false and the parent architecture already records the new model without the spine saying which document wins. Security notes the new model raises an unanswered question — who may dispatch, under which protected environment.

**T2 — AD-16 / external gate stale (R-4, C-1, D-2).** The Builds revision landed and is pinned to `4eb33928…` in `ci.yml` and `release.yml`; Builds' own `domain-ci.yml` pins its actions; no `@main` remains in the GOV-1 path; `evaluator_authorizations` is populated. The spine still narrates the gate as open. What is *not* evidenced is the formal "owner-accepted immutable revision" record: the lenses disagree on which deferred-work item is open (see inconsistencies) and no successor issue number appears anywhere.

**T3 — Manifest v3 shipped; spine binds v2 (R-3, D-3).** `CURRENT_MANIFEST_SCHEMA = hexalith.release-evidence.v3` since commit `df689935` (2026-08-30); v2 is accepted for verification only; Governance pins v3. AD-14's exact member sets and digest material and AD-9's "V2 fallback approval" are stale; the v2 publishability disposition is undecided.

**T4 — Bootstrap clause is dead code / never existed (R-9, S-4, D-7, A-12).** `HEXALITH_DEPENDENCY_POLICY_BOOTSTRAP_SHA256` appears nowhere in `eng/`, `.github/`, or tests; a base policy has existed on `main` since 2026-07-19, so by the AD's own words bootstrap is permanently unavailable. Four lenses converge: the 12-line procedure misstates enforcement (R-9), is a latent TOFU instruction (S-4), hides the actual zero-`before` behaviour (candidate policy for diagnostics only, never release-eligible — D-7), and leaves "base policy exists" undefined for a malformed blob (A-12).

**T5 — Post-release authorization not enforced; `post_release` `reusable` shape undefined (D-5, A-4).** The spine's mandatory `reusable` object has no rule for a caller without a reusable workflow; the shipped policy fills it with an unreachable second root, and the verifier never checks any `post_release` authorization, so the five stale rows went unnoticed.

**T6 — Runtime workflow identity not recorded; closure never executed outside tests (D-4, S-1).** The AD-13 clause "Governance constructs the same static transitive closure and requires it to equal one authorization" has no production execution; the evaluator is projected from the policy row by blob hash. Because the evaluator code itself runs from the candidate checkout, delayed activation protects the declarative policy and workflow blobs but not the enforcement program. Note: the security lens's "Confirmed sound" §4 asserts runtime `workflow_sha` *is* checked; D-4 finds it is not — see inconsistencies.

**T7 — Spine metadata integrity (R-1).** Commit `8a6a6cb3` (2026-08-03, "feat(tests): …") rewrote AD-11/12/13 Rules while `status: final` and `updated: 2026-07-19` stayed unchanged; the memlog's last entry is "spine finalized"; no gate re-ran. The 2026-08-08 issue-17 reopening, 2026-08-16 catalog-gitlink/execution-SHA split, and 2026-08-30 v3 also have no memlog entries.

**T8 — Fallback digest seam (A-1 Critical + A-5 High).** The one finding that is a live publication-authority risk: the shipped release path rebinds the approval digest to live inputs before comparison (parent-verified), and the formula's `workflow_definition_digest` identifier is unbound, so a second implementer could legitimately pick the CI-only digest and let release-workflow drift survive fallback. Both must be folded into AD-9 before the spine can be called convergent for the release/fallback seam.

## Medium / Low tail (per lens)

- **Rubric:** 7 medium, 5 low — see `reviews/review-rubric-validate-2026-09-08.md`.
  - R-6 — AD-6 carries the seed profile roster inside an invariant while L400 says the spine does not mirror volatile policy values (autofix: reduce to coverage invariant).
  - R-7 — Builds catalog gitlink vs Builds CI/CD execution commit split (commit `dfbe9978`) is silent in the spine (discuss: one Consistency row naming both identities).
  - R-10 — bloat: AD-12/13/14/15 embed member lists, caps, and status prose; caps stated three times (autofix: move to contract/code constants, state each cap once).
- **Current-fit:** 1 medium, 5 low — see `reviews/review-current-fit-validate-2026-09-08.md`.
  - C-3 — Stack row says .NET SDK `10.0.302`; `global.json` pins `10.0.400` (pure wording pin; autofix + memlog `version` entry).
  - C-6 — `job.workflow_*` contexts are GitHub.com-only (not GHES); add a memlog `version` entry and one clause to AD-13.
  - C-7 — Git 3.0 will default new repositories to SHA-256; add a reopen trigger to the Deferred entry.
- **Adversarial:** 8 medium, 4 low — see `reviews/review-adversarial-validate-2026-09-08.md`.
  - Pure wording pins (autofix): A-7 (Builds-edge constant for offline verification), A-8 (artifact `<base>-<run_id>-<run_attempt>` + run-coordinate equality), A-9 (`assets` unique/sorted by `name`, GitHub assets only), A-10 (ordered AD-3 normalisation grammar; paths byte-exact), A-13 (seal = AD-5 canonical bytes minus `seal`).
  - A-11 — is the gitlink *path* trusted policy data or acquisition data? Needs an Architect decision (discuss).
  - A-12, A-14 — "base policy exists" = blob at canonical path regardless of validity; name the handoff-contract files in the definition lists (autofix).
- **Implementation drift:** 7 medium, 6 low (header; table shows 6/7) — see `reviews/review-code-drift-validate-2026-09-08.md`.
  - D-6 — `ci.yml` treats `create-ci` exit 2 as success (green push run with no AD-13 artifact); spine says it "fails before CI handoff" (DW-1795; discuss hard-fail vs ratify soft-defer).
  - D-8 / D-9 — run-suffixed artifact names and directory upload; `.deferred.json` sentinel instead of a null-filled AD-15 handoff (autofix spine / discuss).
  - D-10 — second CI→Release authority path (`hexalith.dependency-release-source.v1`, `--source-proof`) contradicts "sole release-candidate authority" (discuss: remove or name as non-eligible diagnostic).
- **Security:** 6 medium, 4 low — see `reviews/review-security-validate-2026-09-08.md`.
  - S-2 — publication actor/trigger authorization unbound (who may dispatch; `production` environment with Release Owner review; actor sealed in evidence) — autofix alongside T1.
  - S-3 — least-privilege stage tokens: spine says "read-only token" yet publication holds `contents: write`; forbid `actions: write` and `pull_request_target` (autofix, new AD).
  - S-6 — "ledger" undefined (writer, location, append-only, retention); failure dispositions should open an incident, not only fail a run (discuss).

## Per-AD heatmap

| AD | Title | Worst severity | Finding IDs | Status |
| --- | --- | --- | --- | --- |
| AD-1 | v1 graph boundary | — | — | holds (conforms; census is evidence) |
| AD-2 | committed objects are authoritative | Low | A-16 | holds (null-OID wording pin) |
| AD-3 | repository resolution is closed-world | Medium | A-10, A-11, D-16 | holds; normalisation order + path-trust decision pending |
| AD-4 | v1 edge identity and deterministic order | — | — | holds |
| AD-5 | v1 canonical material and digest | Medium | A-7, A-13, D-11, A-15, C-7, S-9 | holds; Builds-edge constant, object-format probe, golden literal missing |
| AD-6 | compatibility and provenance are separate | Medium | R-6, D-17 | holds; seed roster inside invariant; sibling presence-only drift (DW-1803) |
| AD-7 | bounded traversal fails closed | Low | A-18 | holds (ceilings match policy) |
| AD-8 | CI uses one explicit revision model | High | A-2, S-1, R-7, R-8, A-16 | holds in code; "changed" undefined; catalog/execution identity split silent |
| AD-9 | offline structure and live identity are distinct modes | **Critical** | A-1, A-5, R-3, A-7, A-14, D-10, S-8 | **conflicts with code** (live-rebound fallback digest, parent-verified); unbound identifiers |
| AD-10 | acquisition is isolated from collection | Low | S-9 | holds |
| AD-11 | GOV-1 is governance-only | High (via R-1) | R-1, R-5 | stale/unlogged edit; sprint fact inside Rule |
| AD-12 | one versioned trust and semantic policy | High | A-2, A-4, D-5, S-1, R-1, R-6, R-9, A-11, A-12, A-14, D-6, D-7, S-4, S-5, D-12 | stale (dead bootstrap) + silent dimensions (evaluator-code activation, review strength) + `post_release` shape |
| AD-13 | release consumes the exact CI-tested revision | High | R-2, R-4, R-1, C-1, C-2, A-3, A-4, D-1, D-4, S-1, R-10, A-8, D-6, D-8, D-10, S-2, S-3, S-7, C-5, C-6, A-16, D-14, D-15, S-10 | stale (trigger model, `@main` prose) + conflicts with code (runtime identity, checkout-injected actions) |
| AD-14 | manifest migration is one-way and fail-closed | High | R-3, D-3, R-2, A-5, A-13, S-6, A-17 | stale (v3 shipped; v2 verify-only) |
| AD-15 | release-to-verifier handoff preserves the original candidate | High | A-4, A-6, D-5, A-8, A-9, D-8, D-9, S-3, S-6, S-7, C-5 | conflicts with code (`post_release` authorization unenforced; deferred sentinel) + shape ambiguity |
| AD-16 | Hexalith.Builds workflow revision is an external completion gate | High | R-4, C-1, D-2, R-2, R-15 | stale (gate narrated as open; releases published; acceptance record not evidenced) |
| Stack | observed toolchain | Medium | C-3, R-12, C-4, D-18 | stale (.NET 10.0.302 → 10.0.400; no CI version diagnostics) |
| Structural Seed | file tree / diagrams | Low | R-14, C-8, D-13 | stale (omits `dependency_handoff.py`, `release_contract.py`, `release_prepublish.py`, tests; no dependency-direction diagram) |
| Deferred | | High (via T2) | R-4, C-1, D-2, R-15, C-7, S-8 | stale (restates AD-16); silent dimensions: catalog/execution split (R-7), diagnostics envelopes (R-8), revocation (S-8) |
| Frontmatter | | High | R-1, R-11 | stale (`updated`, `status`); `binds` lacks parent AD ids; no Inherited Invariants section |

## Disposition menu

| Item | Path | Action |
| --- | --- | --- |
| A-1 (Critical, AD-9) / T8 | **discuss + escalate** | Owner decision: does AD-9's invalidation guarantee stand (then code must stop rebinding the digest; add fixture) or is the approver+expiry contingency the accepted model (then AD-9 is rewritten and the weaker guarantee is stated)? Spine wording autofix follows the decision. |
| A-5 (AD-9/AD-14) | autofix | Pin `workflow_definition` and `definition` identifiers and value types. |
| T1 — R-2, C-2, D-1, S-2 | **discuss** | Owner decision: `workflow_dispatch` exact-SHA model vs return to `workflow_run`. Then amend AD-13, drop REL-4/`workflow_run` text, bind dispatch actor + `production` environment (S-2), add Inherited Invariants row to parent. |
| T2 — R-4, C-1, D-2 | **discuss** | Owner decision: is Builds `4eb33928…` the owner-accepted immutable revision, and where is acceptance recorded (reopened issue 17 / successor / dated contingency)? Then autofix AD-16 to the invariant; move chronology to memlog; close or re-scope the open DW item. |
| T3 — R-3, D-3 | discuss → autofix | Decide v2 disposition (verify-only), then add AD-17 "manifest v3" and retitle AD-14. |
| T4 — R-9, S-4, D-7, A-12 | autofix | Replace bootstrap procedure with one historical sentence; state zero-`before` rule as implemented; define "exists" as blob presence; route recovery through AD-16 decision. |
| T5 — A-4, D-5 | autofix (spine + code/policy) or discuss | Nullable `reusable`, caller-reachable closures; wire `post_release` authorization into the verifier and re-authorize the current blob — or drop the stage. |
| T6 — D-4, S-1 | **discuss** | Wire `workflow_source_closure.py` into production and record `job.workflow_sha`, or downgrade the clause with a Deferred entry; decide evaluator-code activation (S-1 a/b). |
| T7 — R-1 | autofix | Memlog event for the 2026-08-03 edit, `updated` bump, gate record; post-`final` edit rule. |
| A-2 (AD-8/AD-12) | autofix | Define the "unchanged" relation. |
| A-3 (AD-13) | **discuss → autofix** | Owner decision: `checkout_injections` policy member vs forbid checkout-injected local actions. |
| A-6 (AD-15) | autofix | Pin `evaluator` to the AD-13 shape. |
| A-11 (AD-3/AD-12) | **discuss** | Owner decision: gitlink path as trusted policy data (fail closed on move) or acquisition data. |
| Wording pins (Medium) | autofix | A-7, A-8, A-9, A-10, A-12, A-13, A-14, C-3, D-8, D-12, D-15, D-16, R-5, R-6, R-10, S-4, S-7, S-10. |
| Silent dimensions | defer (Deferred entries) | R-7 (catalog vs execution identity — or a Consistency row), R-8 (evidence envelopes), C-7 (Git 3.0 trigger), S-8 (revocation/idempotency), S-9 (transport/SHA-1 acceptance), D-17 (DW-1803), R-16 (story AC5 wording). |
| Implementation nonconformance | escalate | A-1 (digest rebinding), D-4 (closure not executed), D-5 (`post_release` unenforced, stale rows), D-6 (exit-2 soft pass), D-9 (deferred-attempt shape), D-11 (object-format probe, golden literal). |

**Items needing an explicit owner decision before UPDATE can close them:** A-1 / AD-9 fallback authority; A-3 checkout-injected local actions; A-11 gitlink-path trust; T1 release trigger model; T2 owner-accepted Builds revision record. (Also flagged as decisions by the lenses: T3 v2 disposition, T6 evaluator-code activation, D-6 exit-2 semantics, S-5 branch-protection binding, S-6 ledger definition.)

## Confirmed sound

- Closed-world identity (AD-3, AD-10): candidate `.gitmodules` URLs are validated but never used as fetch endpoints; remotes reconstructed from policy identity.
- Exact-object provenance (AD-2, AD-4, AD-5): immutable-object reads, strict 40/64-hex, closed JSON schema with duplicate-member rejection, canonicalization pinned by fixtures; envelope/edge member sets and sort tuple match the spine exactly.
- No candidate-controlled execution (AD-8): argv from active policy only; `merge_base == event_base` fail-closed; nested submodules never initialized; contract-tree materialization rejects special modes and enforces ceilings before extraction.
- Delayed activation of declarative policy and workflow definitions (AD-12, AD-13): policy read from `event_base`/`before`; caller blob, reusable commit+blob, and every action commit+blob must match an active-policy authorization; HEAD `ci.yml`/`release.yml` blobs each authorized exactly once for Builds `4eb33928…`.
- Static closure over the release path (AD-13): Docker actions, mutable refs, `uses:` expressions, anchors, cycles, and limits fail closed.
- Exact-candidate propagation (AD-13, AD-15): authenticated handoff candidate is the sole authority; `main` revalidated before protected credentials; live re-collection at release (`verify-ci --live`).
- Fail-closed evidence (AD-9, AD-15): `publish_authorized=false` before side effects; `if: always()` emit; legacy manifests audit-only, never resealed.
- Zero-base handling (AD-12) and resource ceilings (AD-7) exactly as specified in policy (4096 / 64 MiB / 1 MiB / 4 MiB / 16384 / 16 MiB / 256 MiB; closure 16 / 256 / 1 MiB / 16 MiB).
- Technology currency: Git plumbing unchanged through 2.55.0; Python 3.14 `json.dumps`/`hashlib.sha256` semantics unchanged; all repos `sha1`; RFC 8785 correctly contrasted (project canonicalization is not JCS); GitHub Actions contexts (`github.sha` merge commit, `event.before`, `workflow_run.*`, `github/job.workflow_ref/sha`) behave as assumed; 40-hex pins remain the immutable reference form; artifacts immutable since upload-artifact v4.
- AD-6 / BUILD-CAT-1 marker still nullable provenance only; AD-11 / Story 11.17d completion record accurate.

## Inconsistencies between reviews (noted, not resolved)

1. Security "Confirmed sound" §4 states runtime `github.workflow_sha`/`job.workflow_sha` are checked against the sealed and allowlisted commit; code-drift D-4 finds no workflow records or validates either. D-4 cites `rg` evidence; treat §4 as describing spine intent.
2. Open deferred-work item for the Builds acceptance record: current-fit C-1 says DW-1688 open and DW-1783 done 2026-08-27; code-drift D-2 says DW-1783 still open.
3. Rubric R-4 states issue 17 was "reopened and owner-accepted 2026-08-08" (G2 request); current-fit C-1 finds no acceptance record and no successor issue number anywhere.
4. Code-drift scorecard marks AD-9 "conforms" while adversarial A-1 (Critical) is grounded in the same file; parent verification confirms A-1.
5. Code-drift header counts (7 M / 6 L) differ from its table (6 M / 7 L).

## Recommended next step

Run **bmad-architecture UPDATE** against this spine, resuming from `.memlog.md`, keeping AD IDs AD-1…AD-16 stable (new decisions as AD-17+). Before any AD edit, the memlog must first receive an event entry recording the unlogged 2026-08-03 edit (commit `8a6a6cb3`, AD-11/12/13 Rules), followed by entries for the 2026-08-08 issue-17 reopening, the 2026-08-16 catalog-gitlink/execution-SHA split, the 2026-08-30 manifest v3, and this 2026-09-08 gate. Collect the five owner decisions listed above first; apply the wording-pin autofixes in the same UPDATE; re-run this five-lens gate before re-stamping `final`.
