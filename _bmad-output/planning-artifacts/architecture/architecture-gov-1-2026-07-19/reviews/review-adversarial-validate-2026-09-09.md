# Adversarial Divergence Review — VALIDATE (2026-09-09)

**Target:** `ARCHITECTURE-SPINE.md` (`status: final`, updated 2026-09-08)

**Lens:** construct two independently built units one level down that obey every architecture decision
literally yet still disagree at a shared-data, ownership, mutation, or workflow/provenance seam.

**Method:** read-only pressure test of the spine, its memlog, the FrontComposer release helpers/workflows,
and the exact `Hexalith.Builds` `domain-release.yml` blob at the currently pinned
`4eb33928a1d8c7775f97221cf9edc171db0cb5f8`. Existing code is used only as evidence that a reading is
realistic; implementation drift is not itself counted as a spine finding. No source or spine file was
changed.

**Verdict: FAIL** — **0 Critical, 3 High, 3 Medium, 0 Low**. The graph/catalog half is strongly
convergent. The gate fails because the release half still permits incompatible implementations of (1)
which Release run is a governed publication attempt, (2) canonical JSON escaping used by every digest and
seal, and (3) the semantic and mutation contract of the durable ledger record.

## Findings

| Stable ID | Severity | AD refs | Divergence / exploit pair | Evidence | Required disposition |
| --- | --- | --- | --- | --- | --- |
| ADV-2026-09-09-01 | High | AD-15, AD-16, AD-18 | **The post-release meaning of “governed attempt” is undefined.** Unit A treats an attempt as governed when the caller-side reusable job or any `release / *` inner job was non-skipped. Unit B checks only the legacy inner job `release / release`; when the external reusable selects `release / governed-release`, B routes the same run through the non-publication path and can omit the mandatory publication/partial-publication inspection. Both obey AD-15 because its aggregate `needs.release.result` rule binds only the handoff emitter; the later phrase “for a governed attempt” has no classifier or fail-closed comparison rule. | Spine lines 428-456 define emitter `publication_started`/`published`, then switch to the undefined “For a governed attempt.” The pinned Builds reusable has mutually selected jobs `release` (line 251) and `governed-release` (line 490). The existing classifier recognizes both names but chooses only `release / release` at `eng/release_disposition.py:18-27,86-102`, demonstrating the exploitable reading. | **Discuss, then autofix.** Pin the authenticated job-topology vocabulary and define a governed attempt independently of one inner-job name. Require a `missing-artifact`/`partial-publish-incident` whenever any publication-capable caller or inner job started but the verified handoff does not report the same `publication_started` state. |
| ADV-2026-09-09-02 | High | AD-5, AD-9, AD-12, AD-14, AD-17 | **The canonical JSON recipe does not uniquely encode escapes.** A valid gitlink path may contain `"`. Unit A emits that character as `\"`; Unit B emits it as `\u0022`. Both use compact JSON, sorted keys, UTF-8, no BOM/newline, and escape the quote as AD-5 requires, yet calculate different graph, closure, workflow, fallback, and manifest-seal digests. The single golden digest need not exercise this accepted character. | Spine lines 117-133 allow ASCII normalized paths and say which characters/code-point ranges are escaped, but not the exact short-vs-`\uXXXX` form, hex case, or surrogate convention. The Consistency Convention at line 569 applies this encoder to every digest and seal. Python currently chooses one encoding in `eng/release_evidence.py:385-393`; another-language verifier can literally satisfy the prose and choose the other. | **Autofix.** Either define canonicalization as the byte-for-byte result of a named encoder invocation (`json.dumps(..., ensure_ascii=True, allow_nan=False, sort_keys=True, separators=(",", ":"))`) plus accepted scalar types, or pin every escape form and add golden vectors for quote, backslash, C0 controls, DEL, BMP, and supplementary code points. The narrower alternative is to forbid every string character that has more than one legal JSON escape representation. |
| ADV-2026-09-09-03 | High | AD-15, AD-18 | **The “closed” ledger has member names but no closed semantic domains or immutable record identity.** Unit A emits `disposition: "compliant"`, `incident: null | {kind,detail}`, and keys the durable append by `(release_run.repository, workflow_path, run_id, run_attempt)`. Unit B emits `disposition: "compliant-candidate"`, `incident: boolean`, and keys by tag, allowing a second verification attempt to append a conflicting disposition for the same Release attempt. Both populate exactly the listed members and leave unavailable values null. A consumer written by the other unit cannot decide which record is valid or whether an incident was impermissibly relabelled green. | Spine lines 458-473 fix the member list and three incident labels but never define the complete `disposition` vocabulary, `incident` shape, boolean/null domains, stable record key, duplicate policy, or mapping into the Release-Owner-maintained REL-AI-1 ledger. The brownfield workflow already emits mutually incompatible `frontcomposer.release-ledger-record.v2` shapes/vocabularies at `.github/workflows/release-evidence.yml:209-218,585-601`; that is implementation nonconformance, but it demonstrates that the unbound choice is active rather than theoretical. The durable ledger itself says a later record cannot relabel an earlier disposition (`rel-ai-1-release-evidence-ledger.md:15-21`). | **Autofix.** Pin every field’s type and closed vocabulary; make the Release attempt coordinate the immutable primary key; define whether verification reruns create linked append-only observations or are rejected; and state that no later record may replace or weaken an incident disposition. Pin the mechanical projection from the machine record into the Release Owner ledger. |
| ADV-2026-09-09-04 | Medium | AD-13, AD-15, AD-17 | **`quality_run` has no authoritative transport from selection to the post-release ledger.** Unit A re-queries Actions by candidate when composing the ledger; a rerun created after publication can make the query ambiguous or select different coordinates. Unit B writes JSON `null` because the field is unavailable in every authenticated handoff/manifest it receives. Both satisfy “JSON null where unavailable,” but their ledger records disagree. Adding it ad hoc to either handoff would violate that handoff’s closed schema. | AD-13 lines 319-329 require `select-ci` to record `quality_run`. The CI handoff shape at lines 339-349, Release handoff shape at lines 414-438, and manifest v3 shape at lines 502-516 contain no `quality_run`; the ledger nevertheless requires it at lines 458-465. | **Autofix.** Carry the exact selected coordinate in one authenticated closed envelope (prefer the Release handoff, since quality is deliberately deny-only and outside workflow trust), then require the post-release ledger to copy it byte-for-byte. If it is intentionally non-durable, remove it from the mandatory ledger shape rather than allowing re-selection. |
| ADV-2026-09-09-05 | Medium | AD-15 | **`release_run.conclusion` is a vocabulary without a projection function.** For needs `{verify-source: success, plan-release: success, prepare-candidate: success, release: success, verify-publication: failure}`, Unit A projects `failure` because any required need failed; Unit B projects `success` because publication completed and the field is evidence of publication. Both return one of `{success,failure,cancelled}` and are projections over `needs`, but downstream incident comparisons differ. | Spine lines 421-425 define the three tokens but no truth table. The existing shell makes one particular choice at `.github/workflows/release.yml:442-454`; an independent emitter is not required by the spine to make that choice. | **Autofix.** Define the total truth table over every named `needs` result, including `skipped` and cancellation, and pin how a missing/unknown job fails. Keep the separate API conclusion in `release_run.conclusion` and the projection in `reported_conclusion`, as the ledger text already intends. |
| ADV-2026-09-09-06 | Medium | AD-12, AD-13, AD-15, AD-16 | **The Builds execution-SHA equality set has no time/scope rule for recovery of an older release.** Unit A requires the current `release-evidence.yml` checkout ref to equal the old release manifest’s sealed `builds_execution_sha`; after a legitimate lineage advance it cannot verify the older release. Unit B validates the verifier’s current Builds action commit only through the matched `post_release` authorization and does not compare it with the historical Release pin. Both can cite AD-13 literally: it includes `release-evidence.yml` in the equality set, while AD-12/AD-15 independently authorize the post-release caller and AD-16 permits CI and Release pins to differ over time. | Spine lines 351-390 define the equality set and defer runtime workflow identity; lines 440-452 require the verifier’s own `post_release` authorization; lines 475-496 permit lineage advancement. The current verifier checkout is a literal independent pin at `.github/workflows/release-evidence.yml:317-327`. | **Discuss, then autofix.** Scope the equality set to a named repository revision and phase. State explicitly whether historical/recovery verification must use the release-time Builds pin or may use a later active-policy-authorized post-release pin; in the latter case, record that verifier pin independently in the ledger evaluator and prohibit treating it as release provenance. |

## Detail on gate-level findings

### ADV-2026-09-09-01 — publication happened, but one conforming verifier calls it non-governed

AD-15 closes the producer side of `publication_started`: the Release workflow observes the caller’s
aggregate `needs.release.result`. It does not close the consumer side. The verifier is told to perform the
strong checks “for a governed attempt” without a normative definition of that condition.

That omission crosses an external-owner seam. The accepted Builds reusable contains two mutually exclusive
publication-capable jobs. Their API names are `release / release` and `release / governed-release`; the
selected one depends on the reusable input. A verifier that keys only on the first can classify a successful
governed-mode publication as `no-releasable-commits` or `rejected-before-publication`. A verifier that keys
on either inner job (or the caller aggregate) performs the required artifact inspection. Neither violates a
literal classifier rule because none exists.

This is High rather than Critical because AD-15 still states that detected external publication must become
an incident. The hole is that one conforming unit can avoid reaching the detection branch.

### ADV-2026-09-09-02 — accepted JSON, different bytes

The divergence can be exercised without Unicode or malformed input. Git and the spine allow a path such as
`references/Build"Mirror`. Both of these encodings represent the same accepted string:

```json
{"path":"references/Build\"Mirror"}
{"path":"references/Build\u0022Mirror"}
```

AD-5 says the quote is escaped, but does not select one representation. Hashing those byte strings yields
different results. Because the recipe is reused for policy authorization rows, handoff evaluator digests,
fallback approval, workflow provenance, and the outer manifest seal, the incompatibility is system-wide.
One ordinary golden vector only proves the encoder on the characters present in that vector.

### ADV-2026-09-09-03 — one schema name, incompatible state machines

The member-set rule prevents structural extension but does not define the state machine carried by those
members. In particular, `disposition` and `incident` decide whether a failed/partial attempt is permanently
incident-bearing, while the durable ledger is a separately shaped, manually appended document. Without a
primary key and an append/reverification rule, two Release Owner tools can both preserve history yet append
contradictory terminal labels for one run attempt. This defeats AD-15’s “recorded and never retried to green”
invariant without deleting an earlier row.

## Confirmed-sound adversarial checks

| Surface | Result |
| --- | --- |
| Graph boundary and ownership | **Sound.** AD-1–AD-4 make depth, owner, edge identity, deduplication, and ordering converge, including self/back-reference edges. |
| Graph shape and Builds-edge discriminator | **Sound except ADV-2026-09-09-02.** AD-5 closes envelope/edge member sets and uses the exact Builds repository identity rather than a suffix heuristic. |
| Semantic compatibility vs provenance | **Sound.** AD-6 gives semantic policy to one Python engine and keeps commit/catalog hashes evidential. |
| Bounded failure and acquisition | **Sound.** AD-7/AD-10 assign cumulative/per-object ceilings, isolate acquisition, and fail closed on partial object graphs. |
| Edge diff and affected-build ownership | **Sound.** AD-8 defines unchanged equality, depth-1 subsumption, removal collapse, candidate materialization, and no implicit build command. |
| Fallback mutation path | **Sound.** AD-9 fixes the digest inputs, external Release Owner write point, immutable approval value, and live recomputation; a workflow cannot self-refresh approval. |
| Policy authority and delayed activation | **Sound.** AD-12 assigns one declarative owner, a closed registry, exact base/before activation, two-phase caller authorization, and fail-closed bootstrap recovery. |
| CI candidate and handoff authenticity | **Sound.** AD-13 fixes the candidate authority, run/attempt naming, raw member identity, independent Actions-API authentication, and candidate/policy equality. |
| Manifest migration and v3 projection | **Sound except the shared encoder.** AD-14/AD-17 close the one-way schema lineage, seal coverage, and CI/Release provenance member sets. |
| Builds owner boundary | **Sound.** AD-16 distinguishes graph/catalog and execution identities and makes active policy authorization stricter than historical lineage admission. |
| Partial-publication evidence | **Sound once ADV-2026-09-09-01 is closed.** AD-15 requires inspection for every started attempt, independently downloads the CI handoff, and forbids post-publication authorization. |

## Gate disposition

The spine should remain **failed under the adversarial lens** until ADV-2026-09-09-01 through -03 are
resolved. Findings -04 through -06 may be fixed in the same update; deferring any of them should add an
explicit owner and revisit trigger because each crosses an independently implemented workflow/evidence
boundary. This VALIDATE pass makes no architecture changes.
