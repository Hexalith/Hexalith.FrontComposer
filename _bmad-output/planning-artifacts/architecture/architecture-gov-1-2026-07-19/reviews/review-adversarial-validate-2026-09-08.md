# Adversarial Divergence Review — VALIDATE pass (2026-09-08)

**Target:** `ARCHITECTURE-SPINE.md` (status `final`, updated 2026-07-19)
**Lens:** two units one level down that each obey every AD to the letter yet build incompatibly
**Grounding:** `eng/dependency_graph.py`, `eng/dependency-graph-policy.json`, `eng/workflow_source_closure.py`,
`eng/dependency_handoff.py`, `eng/release_evidence.py`, `.github/workflows/{ci,release,release-evidence}.yml`
(read only; nothing but this file was written).
**Prior passes consulted:** `review-adversarial.md`, `-pass2`, `-pass3`, `-pass4`, `-final`. Holes already
closed there (policy coordinate, PR base algorithm, closed graph schema, argv/materialization, compound
removal collapse, handoff transport, manifest member placement, fallback formula key names, legacy manifest
disposition, action sort tuple, run-ID integer typing, policy reload at release) are **not** repeated below.

**Verdict: FAIL** — 1 Critical, 5 High, 8 Medium, 4 Low. The Critical and two of the High findings are
grounded in the current one-level-down code, i.e. the divergence is not hypothetical: the shipped units
already took one of the two conforming readings, and in the Critical case the reading neutralises AD-9's
stated invalidation guarantee.

## Findings table

| ID | Severity | AD(s) | Hole (one line) | Unit A vs Unit B | Proposed tightening | Disposition |
| --- | --- | --- | --- | --- | --- | --- |
| A-1 | Critical | AD-9 | Fallback approval digest: spine pins the formula but not *who records it, when, and that the release run may not write it*; a live-bound approval satisfies "uses exactly canonical_sha256(...)" while never being invalidated by drift. | A: Release Owner records digest at approval time, verifier recomputes live and requires equality. B: verifier recomputes live, **writes it into the approval**, then compares (tautology). | Add to AD-9: "`approved_against_fingerprints_sha256` is an immutable Release Owner-recorded input captured outside the release run; every classification recomputes the formula live and fails closed on inequality. No release, prepublish, or verification step may write, default, or rebind that value." | autofix (spine) + escalate (implementation) |
| A-2 | High | AD-8, AD-12 | "added/changed/removed" is used but "changed" is never defined for a logical edge keyed by `(owner_repository, path, repository)`; "graph is unchanged" (bootstrap) has no equality relation. | A: full-edge equality → every depth-1 edge "changes" on every commit (owner_commit = new SHA) → all modules build always; bootstrap unreachable. B: key-only → commit bumps are "unchanged" → no standalone build. C (shipped): equality on all members except depth-1 `owner_commit`. | Add to AD-8: "Two logical edges are *unchanged* iff `commit` and, for Builds edges, `catalog_sha256`/`catalog_contract_version` are equal; depth-1 `owner_commit` is excluded from change detection; any other member difference is *changed*." Reuse this relation in AD-12 for "dependency graph is unchanged". | autofix |
| A-3 | High | AD-13 | Closure resolution of a "local" `uses: ./…` path that exists only because a prior `actions/checkout` step materialised another repository (`./.hexalith/builds-execution/...`) is undefined. | A (spine-literal): local refs resolve inside the caller commit → path absent → fail closed. B (shipped): regex-parse `checkout` steps `with.repository/ref/path` and resolve into Builds at that `ref`. | Add to AD-13: "A local `uses:` whose path is not a blob of the caller commit fails closed unless the policy authorization row declares an explicit `checkout_injections: [{repository, commit, path_prefix}]`; the closure resolves such paths only against the declared exact commit and never parses runtime step arguments." | discuss → autofix |
| A-4 | High | AD-12, AD-13, AD-15 | `reusable` is a mandatory non-null object for every stage, but the `post_release` caller has no reusable workflow `uses:`; the spine gives no rule for that case. | A: `reusable = null`/omitted (violates shape). B (shipped): policy names an unreachable Builds workflow as `reusable`, and the collector visits it as a second root and merges its actions into the closure. C: `reusable := caller`. | Add to AD-12: "A caller with no reusable workflow in its static closure records `reusable` as JSON `null`; the closure contains only sources reachable from the caller. Adding an unreachable root to a closure is forbidden." Adjust AD-13/AD-14/AD-15 shapes to nullable `reusable`. | autofix |
| A-5 | High | AD-9, AD-14 | Fallback formula names `workflow_definition_digest`, an identifier defined nowhere; AD-14 defines two candidates (`workflow_provenance.definition_digest` = ci+release, `evaluator.definition_digest` = CI only). `fallback_invalidation_fingerprints` value type is unpinned. | A: combined digest, map `path → sha256`. B: CI-only digest (release-workflow drift no longer invalidates fallback). C: adds non-file keys (`helper_version`) into the map. | Rewrite AD-9: "`workflow_definition` is exactly `workflow_provenance.definition_digest` (AD-14 combined digest); `definition` is exactly the `FALLBACK_INVALIDATION_FILES` map `{relative POSIX path: lowercase 64-hex sha256}` and contains no other keys." | autofix |
| A-6 | High | AD-15 | `evaluator` is "the actual … closure plus its digest" — three conforming shapes exist. | A: AD-13 shape `{caller,reusable,actions,definition_digest}`. B: AD-14 `release` shape `{caller,reusable,actions}` + sibling digest. C: AD-12 registry shape with `stage`+`closure_digest`. | Replace with: "`evaluator` is exactly the AD-13 evaluator shape `{caller, reusable, actions, definition_digest}`." | autofix |
| A-7 | Medium | AD-5, AD-9 | Offline verifier must enforce "Builds edge has 8 members, non-Builds has 6" but has no policy in scope and the spine never defines how Builds-ness is decided. | A: policy `builds_identity` (offline needs policy). B (shipped): name suffix ∈ {`builds`,`hexalith.builds`}. C: presence of catalog members defines Builds-ness (accepts anything). | Add to AD-5: "An edge is a Builds edge iff `repository == 'github.com/hexalith/hexalith.builds'` (v1 constant, also asserted equal to policy `builds_identity`); offline verification uses the constant." | autofix |
| A-8 | Medium | AD-13, AD-15 | Artifact names are pinned literally (`dependency-release-handoff`, `release-verification-handoff`) but upload-artifact v4 forbids duplicate names across re-run attempts, so producers suffix; consumer name derivation, and equality of recorded `run.run_id/run_attempt` with the authenticated run, are unstated. | A: literal name; picks first match. B (shipped): `<name>-<run_id>-<run_attempt>`; requires recorded run/attempt equality. | Pin: "artifact name is `<base>-<run_id>-<run_attempt>`; exactly one match; `run.run_id`/`run_attempt` (and `release_run.*`) must equal the authenticated coordinates." | autofix |
| A-9 | Medium | AD-15 | `assets`: "ordinally sorted unique" without a named tuple or uniqueness key; scope (GitHub release assets only vs NuGet packages too) and `manifest.path` frame (checkout path vs asset name) unstated. | A: sort by `name`, unique by `name`, GitHub assets only, `path` relative to candidate root. B (shipped): sort/unique by full `(name,sha256,size)` tuple, allows duplicate names. | Add: "`assets` are the GitHub Release assets; unique by `name`; sorted by `name`; `manifest.path` is the asset name of the sealed manifest." | autofix |
| A-10 | Medium | AD-3 | Normalisation is listed as operations, not an ordered pipeline: `.git` strip vs lowercase order (`.GIT`), trailing `/`, explicit `:22`, scheme/host case. "lowercase ASCII" sits in the same sentence as path rules. | A: lowercase first, strip `.git`, strip trailing `/`, accept `:22`, lowercase paths. B (shipped): case-sensitive match, strip `.git` then `/`, reject `:22`, paths case-preserved. | Add ordered grammar: "match scheme/host case-sensitively; reject any port; strip exactly one trailing `/` then one terminal `.git` (case-sensitive); lowercase owner/repository only. Gitlink paths are byte-exact and never case-folded." | autofix |
| A-11 | Medium | AD-3, AD-12 | Policy holds "identity/**path** mappings" and AD-3 validates ".gitmodules mappings against policy", but whether the gitlink *path* is trusted data (must equal policy path) or evidence is unstated. | A: path bound → moving a submodule requires delayed-activation policy change; fails closed otherwise. B (shipped): only identity is checked; `local_path` is an object-store location. | Decide and state: "Policy `trusted_identities[*].path` is the required gitlink path for that identity in the FrontComposer root; a mismatch fails closed. Object-store locations are acquisition data, not policy." (or explicitly the opposite). | discuss |
| A-12 | Medium | AD-12 | "no base policy exists" / "base-policy existence makes bootstrap permanently unavailable" — existence of a *blob* or of a *schema-valid policy*? A malformed base blob either deadlocks every future run or re-opens bootstrap. | A: blob present ⇒ exists ⇒ fail closed forever (no recovery rule). B: malformed ⇒ "no policy" ⇒ bootstrap re-eligible. | Add: "Existence means a blob at the canonical path regardless of validity. A malformed active base policy fails closed; recovery requires the dated Release Owner + Architect decision of AD-16, never bootstrap. The PR gate must also reject a candidate policy that fails the closed schema so a malformed policy cannot land." | autofix |
| A-13 | Medium | AD-14, AD-5 | Seal recipe ("Python-equivalent compact, sorted-key JSON") is weaker than AD-5 (no `ensure_ascii`, `allow_nan`, BOM/newline pin) while the manifest carries free text (`tag`, `workflow_ref`, `symbol_artifact` exception text). | A: Python defaults (ASCII-escaped). B: `jq -cS`/UTF-8 emitter (raw non-ASCII, and 2^53 integer rounding). | Replace with: "The seal is SHA-256 over AD-5 canonical bytes of every top-level member except `seal`." | autofix |
| A-14 | Medium | AD-12, AD-9 | "Bind … the versioned handoff contract into the release-definition surface" names no file; ownership of `RELEASE_DEFINITION_FILES`/`FALLBACK_INVALIDATION_FILES` membership is unassigned. | A: adds `eng/dependency_handoff.py`. B: adds a schema doc. C: nothing (contract = string constant). | Name the files: "`eng/dependency_handoff.py` and `eng/workflow_source_closure.py` are members of both lists; the lists are owned by `eng/release_evidence.py` and pinned by a Governance fixture." | autofix |
| A-15 | Low | AD-5 | "Python-equivalent ensure_ascii" does not spell out the escape set for printable ASCII (`/`, `+ < > & '`) or DEL; non-Python re-implementers (C#, jq, Java) differ. | A: Python set (`"` `\` and non-`[ -~]` only). B: .NET default encoder escapes `+<>&'`. | Add: "Only `"`, `\`, and code points outside U+0020–U+007E are escaped; `/` and all other printable ASCII are emitted literally." | autofix |
| A-16 | Low | AD-2, AD-8, AD-13 | The null OID `0000…0` is a strict lowercase 40-hex value and therefore satisfies every "40-hex commit" rule in evidence shapes. | A: accepts it in `revisions.base`; B: rejects. | Add to Consistency Conventions: "The all-zero OID is never a valid commit in any evidence member." | autofix |
| A-17 | Low | AD-14 | `ci.run.head_sha` has no source member in the handoff (`run.candidate` vs `workflow_run.head_sha`), and `run_attempt` is dropped from the manifest projection. | A: `head_sha := run.candidate`. B: `:= workflow_run.head_sha`. (Equal only when AD-13 checks pass.) | Pin: "`ci.run.head_sha` is `handoff.run.candidate`; `ci.run` also carries `run_attempt`." | autofix |
| A-18 | Low | AD-7 | Edge ceiling during streaming: per owner commit or cumulative across the graph. | A: 4,096 per owner. B (shipped): 4,096 total after collection. | Add "cumulative across the whole v1 graph, checked as each record is read". | autofix |

## Detail

### A-1 — Fallback approval digest has no owner, capture time, or write prohibition (Critical, AD-9)

Spine text (`ARCHITECTURE-SPINE.md:170-174`): "V2 fallback approval uses exactly
`canonical_sha256({...})`; graph, active policy, and trusted CI/release workflow definitions therefore
invalidate fallback."

- **Unit A** (intended reading): the Release Owner computes the digest when approving, records it in the
  approval record, and every later classification recomputes it from live inputs and fails on inequality.
- **Unit B** (also conforming to the letter): classification recomputes the digest from live inputs, stores
  it into the approval record, and then compares — equality is guaranteed. The approval "uses exactly" the
  formula, yet no drift ever invalidates it.

The shipped production path is Unit B: `eng/release_evidence.py:3830-3834` calls
`classify_release_payload(..., bind_live_fallback_digest=True)`, and `:2727-2743` overwrites
`fallback["approved_against_fingerprints_sha256"]` with `fallback_v2_digest(<live values>)` immediately before
`fallback_complete` compares it against the same live values (`:2407-2422`). The inline comment states the
purpose: a contingency approval should "not be invalidated by every release-definition file edit". The only
remaining controls on a fallback-approved publication are approver/expiry/365-day/evidence-file checks.

Consequence: a `fallback-approved` classification yields `publish_authorized=true` (`:2881`) after graph,
policy, or workflow-definition drift — exactly what AD-9 says must be impossible. This is a
wrong-publication path.

Proposed AD-9 text: "The approval's `approved_against_fingerprints_sha256` is an immutable Release
Owner-recorded input captured outside any release run; classification recomputes the formula from live
inputs and fails closed on inequality. No prepublish, release, or verification step may write, default,
rebind, or 'refresh' that member. A fixture proves that a graph, policy, or workflow-definition change
invalidates an otherwise-complete approval."

Disposition: autofix the spine; escalate the implementation nonconformance to the parent (out of this
reviewer's lane to change).

### A-2 — "changed" is undefined; "graph unchanged" has no relation (High, AD-8 / AD-12)

AD-8 (`:150-151`) keys logical edges by `(owner_repository, path, repository)` and speaks of
"added/changed/removed" edges. Nothing says which member differences constitute a change. Every depth-1
edge's `owner_commit` equals the root commit, so it differs on every PR/push.

- **Unit A**: equality over all six/eight members → all depth-1 edges "changed" every run → every governed
  module builds every time; "an unchanged graph builds no module" and the AD-12 bootstrap condition
  "dependency graph is unchanged" are unreachable.
- **Unit B**: the key *is* the edge; a commit bump is "unchanged" → no standalone build for a pointer bump.
  The GOV-1 proof is silently lost while the run is release-eligible.
- **Unit C** (shipped, `eng/dependency_graph.py:577-581`): compare every member except depth-1
  `owner_commit`.

Only C is useful, and the spine does not require it. Proposed: "Two logical edges with the same key are
unchanged iff `commit`, `depth`, `catalog_sha256`, and `catalog_contract_version` are equal; depth-1
`owner_commit` is excluded from change detection (it is the root commit by construction). Bootstrap's
'dependency graph is unchanged' means every logical edge is unchanged under this relation."

### A-3 — Checkout-injected "local" actions (High, AD-13)

AD-13 (`:295-296`): "Local references resolve inside the same exact repository commit." The FrontComposer
callers run `actions/checkout` of `Hexalith/Hexalith.Builds` at `BUILDS_EXECUTION_SHA` into
`.hexalith/builds-execution` and then `uses: ./.hexalith/builds-execution/Github/initialize-build`
(`.github/workflows/release-evidence.yml:327`, `release.yml:17`).

- **Unit A** (spine-literal): the path is not a blob of the caller commit → "ambiguous/missing metadata"
  → fail closed. No FrontComposer release closure can ever be authorised.
- **Unit B** (shipped, `eng/workflow_source_closure.py:42-48, 418-499`): regex-scan the caller for a
  `checkout` step with `repository: Hexalith/Hexalith.Builds`, `ref: <40-hex>`, `path: .hexalith/builds-execution`,
  and resolve the local prefix into Builds at that ref.

Both conform; their closures and `definition_digest` differ, so the C# Governance closure ("constructs the
same static transitive source closure", `:286-287`) and the Python closure cannot both match one policy row.
Unit B also reads a runtime step argument (`with.ref`) although AD-13 says the closure "is static, not a
runtime-path trace"; a mutable `ref:` value is not covered by "mutable refs … fail closed", which is scoped to
`uses:`. Proposed text in table; alternatively forbid checkout-injected local actions outright and require
`uses: Hexalith/Hexalith.Builds/Github/initialize-build@<40-hex>`.

### A-4 — Mandatory `reusable` for a caller that has none (High, AD-12 / AD-13 / AD-15)

`reusable` is a required object in the registry row, the handoff evaluator, the manifest, and the AD-15
evaluator. `release-evidence.yml` calls no reusable workflow. The shipped closure
(`eng/workflow_source_closure.py:820-823, 843-850`) therefore accepts `require_reusable_edge=False`, visits
the policy-named Builds `domain-release.yml@99d5a46…` as a **second root**, and merges its actions into the
`post_release` closure even though nothing in the caller reaches it. The `post_release` row in
`eng/dependency-graph-policy.json` names that workflow as `reusable`. A spine-literal collector that only
follows `uses:` from the caller produces a different action set and digest. Proposed: nullable `reusable`;
closures contain only caller-reachable sources.

### A-5 — `workflow_definition_digest` and `fallback_invalidation_fingerprints` are unbound names (High, AD-9 / AD-14)

AD-9 (`:171-173`) uses `workflow_definition_digest`, which no AD defines. AD-14 defines
`workflow_provenance.definition_digest` (ci+release) and the handoff's `evaluator.definition_digest` (CI only).
Unit B, using the CI-only value, satisfies the formula and lets a release-workflow change survive fallback
invalidation. The shipped code uses the combined value (`eng/release_evidence.py:2718-2722`) — correct, but
by choice. `fallback_invalidation_fingerprints` is likewise typeless; the shipped producer and consumer add a
`helper_version` pseudo-file key (`:2704`) that a Governance pin reading the spine would not include. Pin both
(table).

### A-6 — AD-15 `evaluator` shape (High, AD-15)

`:348-349`: "`evaluator` is the actual active-policy-authorized Release caller/reusable/static-action closure
plus its digest." AD-14 `release` is `{caller,reusable,actions}` with no per-stage digest; AD-13's evaluator
has `definition_digest`; AD-12's row has `closure_digest` over five members. Three conforming raw-handoff
layouts, three raw SHA-256 values, and a verifier written against a different one rejects the artifact. Pin to
the AD-13 shape.

### A-7 — Builds-edge determination for offline verification (Medium, AD-5 / AD-9)

AD-5 requires member-set validation conditioned on "Builds edge", AD-9 makes offline verification
policy-free. The shipped verifier (`eng/dependency_graph.py:469-471`) uses a repository-name suffix heuristic
(`builds` | `hexalith.builds`), the collector uses policy `builds_identity`, and a third reading infers
Builds-ness from member presence. A graph with catalog members on a non-Builds edge passes verifier C and
fails A/B. Pin the v1 constant.

### A-8 — Artifact naming and run-coordinate equality (Medium, AD-13 / AD-15)

Spine pins literal names (`:249`, `:337`); the shipped producers use `<name>-<run_id>-<run_attempt>`
(`ci.yml:215`, `release.yml:89,574`, `release-evidence.yml:125`) because upload-artifact v4 rejects duplicate
names within a run. AD-13 also requires only "recorded candidate equals run head" — not that recorded
`run.run_id/run_attempt` equal the authenticated coordinates (shipped consumer does check, `release.yml:144`).
Pin both.

### A-9 — `assets` ordering, identity, scope; `manifest.path` frame (Medium, AD-15)

`:347`: "ordinally sorted unique array of `{name,sha256,size}`" — sort tuple and uniqueness key unnamed (the
same hole pass-3 H-2 closed for `actions`). Shipped: `(name,sha256,size)` sort and full-tuple uniqueness
(`eng/dependency_handoff.py:323-325`), so two entries with one `name` are accepted, making "asset
names/sizes/hashes … must agree" ambiguous. Scope (GitHub assets vs NuGet packages) and whether
`manifest.path` is a checkout path or an asset name are also open. Pin per table.

### A-10 — AD-3 normalisation order and case scope (Medium, AD-3)

`:73-74` lists operations without order. `Hexalith.Builds.GIT`: lowercase-then-strip yields
`…/hexalith.builds`, strip-then-lowercase yields `…/hexalith.builds.git` (unknown identity → fail closed).
Trailing `/`, explicit `:22` ("default port"), and `HTTPS://GitHub.com` are each accepted by one reading and
rejected by the other. Because the sentence also lists path rules, "lowercase ASCII" can be read as applying to
gitlink paths, which would change edge bytes, order, and `graph_digest` (`references/Hexalith.Builds` vs
`references/hexalith.builds`). Pin the ordered grammar and state that paths are byte-exact.

### A-11 — Is the gitlink path trusted policy data? (Medium, AD-3 / AD-12)

AD-12 says the policy holds "trusted repository identity/path mappings" and AD-3 says to "validate both
base/candidate mappings against the active … policy revision". Unit A binds identity→path and fails closed on a
submodule move until a later policy activates it; Unit B (shipped, `eng/dependency_graph.py:309-313, 347-348`)
checks identity only and treats `local_path` as an object-store location. Different accept sets for the same
PR. Needs a decision.

### A-12 — "Base policy exists" with a malformed blob (Medium, AD-12)

`:219`, `:225`. If a malformed policy ever lands on `main`, Unit A (blob-exists) fails every later run with no
recovery path in the spine; Unit B (schema-valid-exists) silently re-opens bootstrap. Pin blob-existence, route
recovery through AD-16's dated decision, and require the PR gate to reject a schema-invalid candidate policy so
the state cannot arise.

### A-13 — Seal recipe weaker than AD-5 (Medium, AD-14)

`:326-327` pins only "compact, sorted-key". The manifest carries free-text members (`tag`, `workflow_ref`,
`symbol_artifact` exception strings, `eng/release_evidence.py:3402-3425`). A UTF-8 emitter or `jq -cS`
re-computation diverges on any non-ASCII byte. Define the seal by reference to AD-5 canonical bytes.

### A-14 — "Versioned handoff contract" is not a path (Medium, AD-12 / AD-9)

`:226-228`. The shipped lists include `eng/dependency_handoff.py` and `eng/workflow_source_closure.py`
(`eng/release_evidence.py:123-168`); the spine does not, so a Governance pin derived from the spine would
accept a list without them, and a handoff-contract change would not invalidate fallback. Name the files and
the owning module.

### A-15 … A-18 — Low

- **A-15 (AD-5)**: spell out the escape set; today "Python-equivalent" is the only guard against emitters that
  escape `/` or `+<>&'`.
- **A-16 (AD-2/8/13)**: `0000…0` passes `^[0-9a-f]{40}$`; state that the null OID is never a valid commit in
  evidence (shipped code relies on downstream object-lookup failure only).
- **A-17 (AD-14)**: `ci.run.head_sha` source and dropped `run_attempt`; harmless only while AD-13 equality
  holds.
- **A-18 (AD-7)**: per-owner vs cumulative edge ceiling while streaming; acceptance sets differ only for
  pathological graphs.

## Coverage of requested attack surfaces

| Surface | Result |
| --- | --- |
| AD-5 canonicalisation | Tight for the ASCII graph domain; Low A-15. Seal (AD-14) is the weak sibling: A-13. |
| AD-4 edge identity/dedup | No new hole: per-owner identity uniqueness (AD-3) makes the logical key unique; depth is per edge. |
| AD-3 identity grammar | A-10 (order/case), A-11 (path trust). |
| AD-8 diff classification | A-2 (High). |
| AD-12 activation/bootstrap | A-2 (unchanged relation), A-12 (malformed base). Two trust owners (file + repo variable) are consistently sequenced; no new hole. |
| AD-13/14/15 shapes | A-4, A-6 (High); A-8, A-9, A-17. |
| AD-9 fallback digest | A-1 (Critical), A-5 (High). |
| AD-7 ceilings | A-18 (Low); inclusive wording is consistent with shipped `>` checks. |
| Ownership / state mutation | A-1 (approval record), A-14 (definition-file lists). `publish_authorized` has one writer (`classify_release_payload`) — no hole found. |
| Deferred section | No new incompatibility beyond those above. |

## Gate recommendation

Do not treat the spine as convergent for the release/fallback seam until A-1, A-5, and A-6 are folded in;
A-2, A-3, and A-4 must be closed before a second closure or diff implementer (C# Governance) can be trusted
to agree with the Python engine. All proposed tightenings are deterministic wording fixes except A-3 and A-11,
which need an Architect decision.
