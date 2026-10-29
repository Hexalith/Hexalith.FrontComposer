---
title: 'Story 15.5: Record Later-Base Policy Activation'
type: 'chore'
created: '2026-10-29'
status: 'done'
route: 'oneshot'
story_id: '15.5'
baseline_commit: '0b469e03c5ae81cf6756f7cd8f2f78df7074514c'
review_loop_iteration: 0
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-15-5-reconcile-the-published-compatibility-baseline.md'
---

<frozen-after-approval reason="user-authorized bounded activation under the existing contract">

## Intent

Record and verify the existing AD-12 later-base activation of Story 15.5's
published 4.7.1 compatibility baseline. Make this documentation-only successor
of the already-landed implementation; evaluate it under that actual prior
policy. Preserve the original refused transition and all historical evidence.
Keep dependencies, policy, validator guards, runtime follow-ups and independent
approval states unchanged. Do not push.

</frozen-after-approval>

## Implementation Notes

### Investigation and supported path — 2026-10-29

Original implementation and current HEAD:
`0b469e03c5ae81cf6756f7cd8f2f78df7074514c`.
Original event base: `612d23485d7e2845d22102621a38d6c904e1ea90`.
The [original story](spec-15-5-reconcile-the-published-compatibility-baseline.md)
retains its original baseline, accepted history and verification limits.

AD-12 loads one immutable event-base policy for both graphs. AD-12a seals the
defined committed MSBuild, SDK and NuGet configuration inputs. The original
push therefore refuses
with `consumer Compile/ProjectReference configuration changed from its sealed
source projection`. Standalone candidate validation passes, but cannot authorize
that earlier event. A scalar-only policy precursor also fails its next event's
base validation, as confirmed with the existing temporary Git fixture.

The supported path is a later change governed by the already-landed policy.
This is covered by
`ConsumerProjectionTests.test_candidate_cannot_authorize_its_own_projection_or_expectations`
and the earlier Parties activation record. A local descendant alone does not
change the remote event base.

At investigation start, local `origin/main` was the original event base. During
this task it advanced externally: its reflog records `update by push` at
`2026-10-29T10:54:56+01:00`. Read-only
`git ls-remote --exit-code origin refs/heads/main` then confirmed remote main
at `0b469e03c5ae81cf6756f7cd8f2f78df7074514c` (exit 0). This task performed no
push. Consequently that commit can now govern a later event under the existing
contract. The investigated successor-hash contract extension is unnecessary and
has not been implemented or approved.

### Preserved scope

The prior active policy for the new activation is exactly
`eng/dependency-graph-policy.json` at
`0b469e03c5ae81cf6756f7cd8f2f78df7074514c`, raw SHA-256
`49dfbe45fd1da400b8ee17815cb8272b63bf8370d14161c53e3a18381ef7391d`.
Its configuration seal remains
`f5186d1b483c33e065ac542374a4b90c0da8257782ebc871d29aaf0ef3613762`.

The implementation changed only two of 39 configuration files:
`Directory.Build.targets` and Contracts.UI's project. Exact committed-blob
comparison confirms only the approved `4.7.0` to `4.7.1` substitutions. Every
other policy value and all root dependency selections match the original base.
This continuation changes only this record. No source, policy, selector,
catalog, trust, suppression, runtime or validator behavior changes.

The original `612d2348... -> 0b469e03...` refusal remains valid history. A green
later event does not repair or reclassify it, grant release/publication approval,
or establish hosted CI/runtime success. Story 15.5 and REL-BASE-1 stay in review;
all inherited runtime follow-ups remain with their existing owners.

### Verification before finalization

`python3 eng/dependency_graph.py --root . diff --event push --event-base 612d23485d7e2845d22102621a38d6c904e1ea90 --candidate 0b469e03c5ae81cf6756f7cd8f2f78df7074514c`
— exit 1, original sealed-configuration refusal retained.

`python3 eng/dependency_graph.py --root . validate --commit 0b469e03c5ae81cf6756f7cd8f2f78df7074514c`
— exit 0, 69 raw edges.

`python3 -m unittest tests.eng.test_dependency_graph tests.eng.test_dependency_handoff tests.eng.test_release_evidence_v2`
— exit 0, 156 tests in 16.454s. Existing projection, drift, forgery, policy
activation, affected-build, handoff and release-consumer regressions all pass.
Log: `/tmp/fc-15-5-policy-transition-regression.log`.

Exact commands/results, scope comparison and the scalar-precursor probe are
local scratch in `/tmp/fc-15-5-policy-transition-8faxxmqv/`.
The previously recorded 379 focused tests and seven ApiCompat packs remain
attributed to the implementation; no package or runtime rerun is claimed.
Finalization must additionally verify an actual committed descendant against
`0b469e03c5ae81cf6756f7cd8f2f78df7074514c`, retaining that prior-policy coordinate,
zero selector changes and zero affected modules. No pass is inferred from
standalone validation or a self-to-self comparison.

### Committed-object activation proof

After successful pinned commitlint 21.2.2 validation, an isolated temporary index
produced scratch commit `f123554dfbb0b45f1aec19500798c0e84db80d26`, a direct child
of `0b469e03c5ae81cf6756f7cd8f2f78df7074514c` changing only this record.
No branch, HEAD or real index was moved by this probe.

`python3 eng/dependency_graph.py --root . diff --event push --event-base 0b469e03c5ae81cf6756f7cd8f2f78df7074514c --candidate f123554dfbb0b45f1aec19500798c0e84db80d26`
— exit 0, actual prior-policy commit/hash match the coordinates above; zero
affected modules. Standalone validation of that scratch commit exits 0 with 69
edges. The same scratch candidate against the original `612d2348...` base still
exits 1 with the original seal refusal. Exact commands/results:
`/tmp/fc-15-5-policy-transition-8faxxmqv/scratch-command-results.json`.

The branch activation commit will retain these identical configuration/policy
bytes and receive the same actual transition checks after creation. Those final
results are recorded in `branch-final-results.json` in the external scratch
directory; the final commit cannot contain its own hash. This record and the original story confer no hosted CI,
runtime, publication or maintainer approval.

## Review Triage Log

- Medium / patch: the bounded numbered continuation lacked explicit story and
  commit-scope metadata. Added `story_id: '15.5'` and this continuation's actual
  starting commit as `baseline_commit`; the original story's baseline is unchanged.
- Medium / patch: the record lacked a File List, preventing owned-path checks.
  Added its sole changed path and required focused/strict artifact validation.
- Low / patch: "every committed configuration byte" overstated the seal's scope.
  Named its bounded MSBuild/SDK/NuGet inputs; workflow/application settings are
  not implied to be sealed by the consumer projection.

All three independent Blind Hunter findings are patched; none deferred.
Global artifact validation and the focused command with this record's `--story`
and `--base 0b469e03c5ae81cf6756f7cd8f2f78df7074514c` pass. The strict command
with `--candidate f123554dfbb0b45f1aec19500798c0e84db80d26` also passes:
`story-id=match`, `disposition=owned`, sole path owned. Its exact output is
`/tmp/fc-15-5-policy-transition-8faxxmqv/scratch-story-scope.log`.
These use `python3 eng/validate-story-artifacts.py` with this record's path.
Sprint and REL-BASE-1 were already at review and remain unchanged.

## File List

- `_bmad-output/implementation-artifacts/spec-15-5-governed-policy-transition.md`
  — Record existing-contract activation, exact verification and review disposition.
