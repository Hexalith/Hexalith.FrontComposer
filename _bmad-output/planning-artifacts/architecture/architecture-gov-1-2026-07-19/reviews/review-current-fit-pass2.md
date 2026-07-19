# Current-Fit Review, Pass 2 — GOV-1 Dependency Provenance

**Target:** `ARCHITECTURE-SPINE.md` and its two named companions  
**Lens:** exact GitHub revision handoff, workflow provenance, immutable policy activation, closed JSON,
PR revision semantics, and runtime-version treatment  
**Review date:** 2026-07-19  
**Verdict:** **CHANGES REQUIRED — no new Critical finding.** The revision, policy, graph-schema, PR,
and version corrections materially close the first-pass blockers. Two High seams remain: immutable
workflow provenance does not cover transitive actions or GitHub's actual workflow-definition
coordinates, and the v2 manifest does not fix where or in what exact shape the closed graph/policy
objects are embedded.

The AD-1 requirements conflict is intentionally excluded from this verdict. It remains the explicit
Architect + Release Owner ratification gate, exactly as requested.

## Critical

None beyond the intentionally open AD-1 ratification gate.

## High

### H-1 — Immutable reusable-workflow provenance stops at the workflow file

**What pass 2 fixed:** AD-13 now requires the caller to pass
`github.event.workflow_run.head_sha`, requires the reusable release workflow to check out and propagate
that exact commit, pins the reusable workflow reference to an approved 40-hex Builds commit, passes the
triggering CI run ID, and binds the CI artifact's candidate to `workflow_run.head_sha`
(`ARCHITECTURE-SPINE.md:203-216`). The canonical architecture and FC-DEP-1 repeat the same fail-closed
rule and explicitly retain the REL-4 freeze while current `@main` plumbing is non-conforming. This
correctly resolves the first-pass wrong-checkout finding.

**Remaining current-fit gap:** An immutable reusable workflow file can still execute mutable action
code. The currently selected `domain-release.yml` contains:

- `Hexalith/Hexalith.Builds/Github/initialize-build@main` on the FrontComposer non-container release
  path (`references/Hexalith.Builds/.github/workflows/domain-release.yml:137-139`);
- `Hexalith/Hexalith.Builds/Github/dapr-init@main` when tests are enabled (`:167-170`).

FrontComposer's independent verifier likewise calls `initialize-build@main` and
`initialize-dotnet@main`. Pinning only `domain-release.yml@<sha>` does not make those transitive action
implementations immutable, so sealing “the workflow commit” is not complete execution provenance.
GitHub recommends full commit SHAs as the strongest stability/security reference for actions. See
[GitHub action metadata guidance](https://docs.github.com/en/actions/reference/workflows-and-actions/metadata-syntax#runsstepsuses).

The phrase “caller workflow hash” is also under-specified. On `workflow_run`, the executing caller
workflow definition can come from a different default-branch commit than the CI-tested source commit.
Hashing `.github/workflows/release.yml` from the source checkout would not prove which workflow
definition executed. GitHub exposes the actual caller coordinate as `github.workflow_ref` /
`github.workflow_sha` and the actual reusable job coordinate as `job.workflow_repository` /
`job.workflow_ref` / `job.workflow_sha`. See the
[GitHub contexts reference](https://docs.github.com/en/actions/reference/workflows-and-actions/contexts#job-context).

**Disposition:** **Autofix.** Extend AD-13 so every release-affecting transitive `uses:` reference is
either a full 40-hex SHA or a local action checked out from the already approved Builds workflow SHA.
Seal and compare the GitHub-provided caller and reusable coordinates, plus raw-byte hashes of the two
workflow files read from those exact commits. Require `job.workflow_repository ==
Hexalith/Hexalith.Builds` and `job.workflow_sha ==` the literal approved reusable-workflow SHA. Apply the
same immutable-action or explicitly non-authorizing identity rule to the post-publication verifier.

### H-2 — The graph schema is closed, but its v2 manifest embedding is not

**What pass 2 fixed:** AD-5 now closes the graph envelope, root, Builds/non-Builds edge member sets,
duplicate-member behavior, JSON integer/boolean distinction, depth range, marker grammar, normalized
values, canonical bytes, and digest material (`:85-103`). AD-9 applies duplicate-member rejection and
the closed schema to live verification (`:151-165`). FC-DEP-1 and the canonical architecture project
the same requirements. Independent graph producers and verifiers now have a convergent v1 graph
contract.

**Remaining divergence:** AD-14 says v2 contains “the complete AD-5 graph, and the AD-12 policy
coordinates” (`:218-226`) but does not fix:

- the graph's exact member path/name in the existing manifest;
- the policy-coordinate object's exact member path and closed shape;
- the exact member path/shape for caller/reusable workflow provenance from AD-13;
- whether these new objects are mandatory before the outer seal is computed and how unknown v2
  members are handled.

One implementation can emit top-level `dependency_graph`; another can emit
`provenance.dependency_graph`. Policy coordinates can be four scalar top-level fields or a nested
object. Both satisfy the prose but produce incompatible manifests, seals, fallback digests, fixtures,
and post-publication verification. The brownfield `release_evidence.py` currently reads ordinary JSON
and seals all top-level members after parsing, so duplicate-name rejection must start at the outer
manifest decoder, not only inside a graph validator.

**Disposition:** **Autofix.** Name exact required top-level v2 members, for example
`dependency_graph`, `dependency_policy`, and `workflow_provenance`; close each object and define exact
types. Require `dependency_policy` to contain exactly `{repository, commit, sha256, schema}` and define
the exact workflow-coordinate fields. State that duplicate member names at every manifest nesting level
fail during decode, all required v2 members are present before sealing, and the unchanged outer-seal
formula covers them. Add a complete golden v2 manifest plus hostile fixtures for alternate nesting,
unknown members, duplicate names, and missing coordinates.

## Medium

### M-1 — The CI-to-release policy handoff is safe in principle but not yet a complete contract

**What pass 2 fixed:** AD-12 now selects policy only from immutable `event_base` / non-zero `before`,
uses that same revision for both graphs, records repository/commit/raw SHA-256/schema, delays candidate
policy activation until a later change, and makes zero/unavailable `before` non-release-eligible
(`:185-201`). AD-13 binds the CI run ID and candidate SHA before accepting the graph/policy artifact
(`:211-214`). This closes candidate self-authorization and the zero-before ambiguity.

**Remaining detail:** “CI supplies” does not name the artifact contract, retention/failure behavior, or
which exact fields bind the artifact to the triggering run. GitHub supports downloading an artifact by
`github.event.workflow_run.id`; its artifact metadata includes a digest and workflow-run `head_sha`.
See [workflow_run artifact access](https://docs.github.com/en/actions/reference/workflows-and-actions/events-that-trigger-workflows#using-data-from-the-triggering-workflow)
and the [Actions artifacts API](https://docs.github.com/en/rest/actions/artifacts).

**Disposition:** **Autofix or make structural seed explicit.** Define one versioned CI handoff envelope
containing CI run ID/attempt, event type, candidate SHA, event base/before, merge-base when applicable,
graph digest, policy coordinates, and artifact SHA-256. Release downloads it only from the triggering
run, validates metadata/content, then live-recomputes the graph/policy before authorization. Missing,
expired, duplicated, or mismatched artifacts fail closed.

### M-2 — The one-time bootstrap approval is human-readable but not machine-addressable

AD-12 correctly restricts bootstrap to no base policy, unchanged graph, frozen publication, and
Architect + Release Owner approval of the exact candidate digest (`:198-200`). The architecture does
not name a durable approval record, signer/role evidence, or the exact digest field the bootstrap gate
consumes. A candidate-controlled markdown statement cannot itself establish approval.

**Disposition:** **Discuss, then autofix.** Name the approval evidence coordinate and validation rule
(for example, a protected-environment approval plus a checked digest, or an immutable owner-approved
GitHub record). Bootstrap remains diagnostic/non-release-eligible, but CI still needs a deterministic
way to distinguish approval from candidate-authored text.

### M-3 — The Capability Map omits the revised release and migration surfaces

AD-13 and AD-14 are load-bearing, but the Capability Map still maps sealed provenance only to
`eng/release_evidence.py` and does not list `.github/workflows/release.yml`,
`eng/release_prepublish.py`, `.github/workflows/release-evidence.yml`, the upstream
`domain-release.yml` contract, or the CI handoff artifact.

**Disposition:** **Autofix.** Add rows for exact CI-to-release revision handoff, immutable workflow
provenance, manifest-v2 migration, and independent post-publication verification so implementation does
not miss the upstream/caller plumbing that AD-13 makes mandatory.

## Low

### L-1 — Official GitHub/.NET sources remain absent from spine frontmatter

The behavior is now correctly researched and expressed, but the `sources` list still contains only Git
plumbing and Python JSON/hash documentation. Add GitHub `pull_request`/`workflow_run` semantics,
reusable-workflow contexts, checkout behavior, action pinning, and .NET `global.json` documentation for
future revalidation.

## Verification of Requested Corrections

| Requested area | Pass-2 result | Evidence |
| --- | --- | --- |
| Exact `workflow_run.head_sha` seam | **Pass as target architecture** | AD-13 requires explicit input, exact checkout/propagation, CI run binding, and freeze until implemented. Canonical architecture and FC-DEP-1 agree. |
| Immutable reusable workflow provenance | **Partial / High finding** | Top-level workflow ref is immutable, but transitive `@main` actions and actual caller/reusable GitHub coordinates are not fully bound. |
| Immutable policy coordinates | **Pass** | AD-12 records repository, commit, raw SHA-256, and schema from immutable base/before policy. |
| Delayed activation | **Pass, bootstrap detail pending** | Candidate policy cannot self-authorize; later-base activation and zero-before non-release eligibility are explicit. Bootstrap approval needs a machine-addressable record. |
| Closed JSON verifier schema | **Pass for graph; partial for manifest v2** | AD-5 closes the entire graph object and AD-9 binds verifier behavior. AD-14 does not close graph/policy/workflow embedding in the outer manifest. |
| PR revision rule | **Pass** | `event_base`, candidate merge SHA, computed merge-base equality, exact diff boundary, and fail-closed mismatch are explicit in AD-8 and both companions. |
| Version treatment | **Pass** | Git/Python/.NET are explicitly observed rather than trust inputs; CI capability checks, diagnostics, `global.json`, and golden fixtures own behavior. |

## Confirmed Strengths to Preserve

- GitHub's documented `pull_request` semantics support using `github.sha` as the merge revision built by
  current CI; AD-8 now states one unambiguous base algorithm.
- GitHub's documented `workflow_run` behavior supports AD-13's explicit use of
  `github.event.workflow_run.head_sha` and run-ID artifact lookup; the architecture no longer relies on
  the event's misleading default `GITHUB_SHA`.
- Policy authority is no longer candidate-selected. The same immutable policy evaluates base and
  candidate, and intentional expansion is a two-change operation.
- The graph canonicalization is correctly project-specific rather than mislabeled RFC 8785, and now
  rejects the Python last-key-wins ambiguity before digest acceptance.
- Version drift is handled at the right layer: runtime capabilities and golden behavior are checked,
  while `.NET` selection remains repository-owned through `global.json`.
- The current `@main` release seam is honestly labeled non-conforming and protected by the mandatory
  publication freeze rather than treated as already solved.

## Gate Recommendation

After H-1 and H-2 are incorporated, this current-fit lens has no independent blocker beyond the
intentional AD-1 ratification gate. M-1 through M-3 are low-cost convergence fixes that should land in
the same redistillation because they determine how developers wire the already-adopted rules into the
brownfield CI/release system.
