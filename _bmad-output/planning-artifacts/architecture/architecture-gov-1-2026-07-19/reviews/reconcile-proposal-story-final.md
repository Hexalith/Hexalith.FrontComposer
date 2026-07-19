# GOV-1 Proposal and Story Reconciliation

Date: 2026-07-19
Authority: final `ARCHITECTURE-SPINE.md`
Disposition: both inputs require reconciliation before development starts.

## Count reconciliation

| Revision | Defined v1 edges | Builds selectors | Distinct Builds commits | Meaning |
|---|---:|---:|---:|---|
| Creation baseline `e3e3dcf592fd7fa962c559e6e9fee034427cbe32` | 40 (8 depth-1 + 32 depth-2) | 7 | 5 | Historical creation evidence only |
| Implementation-start `600f4c738bd28b1efe0e69940ccec8b03faba7c4` | 40 (8 depth-1 + 32 depth-2) | 7 | **6** | Census to freeze in Task 1 and use for implementation fixtures |

Counts are evidence, never acceptance allowlists. The normative v1 boundary is every root gitlink at
depth 1 plus every gitlink in each exact root-selected repository commit at depth 2. No edge below
depth 2 belongs to v1. Record every defined edge before deduplicating object/catalog work.

## Sprint change proposal

Required amendments to
`_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-19.md`:

1. **Approval history (lines 7-9, 23):** preserve Administrator approval of the original proposal.
   Add a dated architecture-ratification/amendment record naming Architect + Release Owner and state
   that the final spine supersedes every “complete reachable” traversal statement.
2. **Boundary wording (lines 64, 119-122, 146-156, 199-200, 218, 287-291, 346, 372):** replace
   “complete/reachable/root and nested” traversal language with “complete defined v1 graph: every
   depth-1 root gitlink and every depth-2 gitlink in each exact root-selected commit; deeper edges are
   excluded.” Cycle-safe historical recursion is future-schema work, not v1 behavior.
3. **Catalog selection (lines 119-132, 244-246, 368-372):** require all 7 Builds selector edges in
   the defined graph, validate each distinct Builds commit once, and retain every selector in evidence.
   State the implementation-start count of 6 distinct commits separately from the creation count of 5.
4. **CI revisions (lines 136-139, 211-214):** replace generic “base-to-head”/“merge base to candidate
   head” with: `event_base = github.event.pull_request.base.sha`, candidate = `github.sha` (the exact PR
   merge revision), compute `merge_base = git merge-base event_base github.sha`, require
   `merge_base == event_base`, and diff `event_base` against `github.sha`.
5. **Affected-module gate (lines 136-142):** make `eng/dependency-graph-policy.json` the active closed
   policy selected from the exact base/before revision. Require the final static matrix, exact argv for
   standalone Release restore/build with `UseNuGetDeps=true`, depth-1 cascade collapse, and no nested
   initialization or candidate-owned commands.
6. **Build materialization:** require the bounded complete regular-file tree of the exact selected Builds
   commit, not only its catalog. Enforce 16,384 files, 16 MiB per blob, 256 MiB total raw bytes, safe
   regular-file modes/paths, then rehash the materialized catalog before building.
7. **Policy activation:** add delayed candidate-policy activation and the release-frozen bootstrap escape
   controlled by `HEXALITH_DEPENDENCY_POLICY_BOOTSTRAP_SHA256` and Architect + Release Owner evidence.
8. **Manifest example (lines 305-325):** replace the stale sample with manifest v2. It needs top-level
   `manifest_schema: hexalith.release-evidence.v2`, the complete graph envelope (`schema`, exact `root`,
   `edge_count`, sorted `edges`, `graph_digest`), exact edge names (`owner_repository`, `owner_commit`,
   `path`, `repository`, `commit`, `depth`), plus top-level `dependency_policy` and
   `workflow_provenance`. Represent an absent marker as `null`; do not invent a placeholder version.
9. **Seal/fallback:** bind dependency graph, dependency policy, and workflow-definition digest into
   fallback invalidation, not only the graph digest.
10. **CI-to-release provenance:** add the authenticated `dependency-release-handoff.json` contract
    (`hexalith.dependency-release-handoff.v1`). Release must use `workflow_run.head_sha`, authenticate
    the exact run/artifact, reload and hash policy at the recorded candidate, and compare evaluator and
    graph projections. Pin primary CI, release reusable workflows, and their transitive action closure
    to approved literal 40-hex commits; current `@main` references are nonconforming.
11. **Sequencing (lines 355-364):** make ratification/source reconciliation step 1 and complete the
    exact-ref/handoff seam before any release candidate is eligible.
12. **Risk/estimate (lines 109-110, 403, 422):** change low-to-medium implementation risk to **high**
    and re-estimate the old 3-5 engineer-day forecast after adding policy, immutable evaluator closure,
    bounded Builds-tree materialization, and authenticated handoff. Keep product scope “moderate” if
    desired; it is not the implementation-risk rating.

The PRD proposal at lines 263-264 is directionally compatible, but should use the exact “defined v1,
depth 1-2” wording to avoid reopening the resolved ambiguity.

## GOV-1 story

Required amendments to
`_bmad-output/implementation-artifacts/gov-1-validate-shared-catalog-compatibility-and-seal-dependency-provenance.md`:

1. **Frontmatter/gate (line 14; lines 41-58):** mark the boundary gate satisfied and reference the final
   spine. Replace the unresolved-gate section with the exact text below. Task 1 remains open for the
   implementation-start evidence freeze; only its approval subtask (line 77) may be checked complete.
2. **Counts (lines 48, 78, 150-154, 304):** keep 40/7/5 explicitly labelled as creation evidence and
   add implementation-start `600f4c738bd28b1efe0e69940ccec8b03faba7c4` = 40/7/**6**. Remove
   “currently” from creation-baseline prose and reconcile fixtures to the implementation-start census.
3. **AC4 and Task 4 (line 68; lines 99-103):** use exact `event_base`/`github.sha` semantics and require
   merge-base equality. Add closed base/before policy selection, depth-1 cascade collapse, immutable
   reusable/action pins, full bounded Builds regular-file-tree materialization, catalog rehash, exact
   Release/NuGet argv, and the authenticated release-handoff artifact.
4. **Task 2 / new files (line 83; lines 142-146):** add nonoptional
   `eng/dependency-graph-policy.json` as the single declarative policy. Remove the optional competing
   `eng/dependency-module-gates.json`, or make it non-authoritative and fully subsumed by the one policy.
   Add base/before selection, delayed activation/bootstrap, closed profiles/matrix, limits, trusted
   evaluator identities, and the handoff contract.
5. **Graph safety (line 162):** v1 does not recurse below depth 2. State that all defined edges are
   recorded before object-read/catalog deduplication; a visited set may deduplicate reads but must not
   expand or alter the bounded graph.
6. **Task 5 / AC5 (lines 70, 105-111):** require manifest v2 and exact top-level graph, policy, and
   workflow-provenance objects. Require the fallback formula to bind all three. Release must receive
   `workflow_run.head_sha` and run ID, authenticate the exact-run handoff, reload/hash policy, and pin
   release reusable workflow/actions to literal 40-hex commits.
7. **Workflow map (lines 131, 139):** identify the current mutable `domain-ci@main` as nonconforming and
   require an approved 40-hex pin plus CI handoff. Replace “preserve release.yml unless minimal plumbing”
   with the required exact SHA/run-ID, immutable reusable release pin, handoff authentication, and policy
   verification changes. Preserve the REL-4 freeze and publication ownership.
8. **CI notes (lines 179-183):** replace “select and document one candidate model” with the already
   ratified model. Include exact policy matrix, full Builds tree, and cascade-collapse requirements.
9. **Release notes (lines 185-192):** add manifest v2, policy/workflow provenance, authenticated handoff,
   exact release candidate, and fallback workflow-definition digest requirements.
10. **Never-list (line 197):** keep “no package/action version upgrade,” but explicitly require replacing
    mutable CI/release `@main` refs with approved 40-hex pins. This is provenance closure, not a version
    upgrade. The current claim that mutable `initialize-build@main` is irrelevant contradicts the spine.
11. **Completion note (line 310):** state that the architecture gate is satisfied and development may
    proceed only after Task 1 freezes implementation-start evidence and records BUILD-CAT-1.

## Exact replacement gate wording

```markdown
## Implementation Entry Gate — Satisfied on 2026-07-19

The Architect and Release Owner ratified FC-DEP-1 through the final GOV-1 architecture spine:

1. `hexalith.dependency-graph.v1` contains every FrontComposer root gitlink at depth 1 and every
   gitlink in each exact root-selected repository commit at depth 2.
2. Edges below depth 2 are outside v1. Historical transitive traversal requires a separately approved
   schema.
3. Every defined edge is recorded before object-read or catalog-validation deduplication. Exact closed
   schemas, policy selection, resource limits, CI revision model, affected-build matrix, and release
   handoff/provenance follow the final architecture spine.

The creation baseline `e3e3dcf592fd7fa962c559e6e9fee034427cbe32` remains 40 edges
(8 depth-1 + 32 depth-2), 7 Builds selectors, and 5 distinct Builds commits. At implementation-start
`600f4c738bd28b1efe0e69940ccec8b03faba7c4`, the boundary remains 40 edges and 7 selectors but
resolves to 6 distinct Builds commits. Counts are evidence, never acceptance allowlists.

The architecture boundary decision gate is satisfied. Task 2 remains gated only by completing Task 1's
implementation-start census/evidence freeze and recording BUILD-CAT-1.
```

## Final consistency gate

After amendment, no proposal/story sentence may require “complete reachable” recursion, leave CI revision
selection discretionary, permit candidate policy or commands to authorize themselves, treat mutable
workflow refs as trusted provenance, or describe release evidence as graph-only. The development entry
state is: architecture ratified, story ready for Task 1 evidence freeze, production edits not yet started.
