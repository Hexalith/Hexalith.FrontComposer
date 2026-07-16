---
created: 2026-07-15
updated: 2026-07-16
owner: Release Owner + Developer + QA/Test Architect
sourceProposal: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-15-release-freeze-enforcement.md
status: ready-for-dev
scope: minor
implementationRisk: low-medium
ordering: stop-the-line — executes before REL-3 development starts
releaseControl: this story IS the technical freeze; REL-3 owns its later removal/re-scope
---

# REL-4: Technically Enforce the Temporary Release Freeze

Status: ready-for-dev.

Approval: approved by Administrator on 2026-07-15 (Batch-mode correct-course).

## Story

As the Release Owner,
I want automated package publication technically disabled by default while REL-3 is being built,
so that ordinary `fix:`/`feat:` merges to `main` cannot publish another unauthorized release.

## Why This Story Exists

The 2026-07-15 REL-3 correction froze publish-capable releases, but only in planning artifacts.
The executable pipeline still auto-publishes: `release.yml` triggers via `workflow_run` after every
successful push-CI on `main` and delegates unconditionally to the shared reusable
`Hexalith/Hexalith.Builds/.github/workflows/domain-release.yml@main`, whose final step runs
`npx semantic-release`; `.releaserc.json`'s `publishCmd` pushes unsigned `.nupkg`/`.snupkg` straight
to nuget.org and `@semantic-release/github` creates the GitHub Release. Product development
explicitly continues during the freeze, so release-triggering commits are expected on `main`. The
next such merge would publish v3.2.3 with exactly the v3.2.2 defects (unsigned, invalid manifest,
`classification=blocked`, `publish_authorized=false`).

Per the Administrator's directive, the freeze mechanism must be **common to all Hexalith modules**:
the identical gate contract is a required Hexalith.Builds upstream item (owner-filed; see the G2
request document). This story implements the immediate FrontComposer-side enforcement, which does
not wait for upstream and remains as defense-in-depth afterwards.

## Acceptance Criteria

1. Given no `HEXALITH_RELEASE_PUBLISH_ENABLED` variable is configured, when CI succeeds for a push
   to `main`, then the `release` job is skipped, `npx semantic-release` never runs, no NuGet or
   GitHub Release side effect occurs, and the Release run concludes green with an explicit freeze
   notice.

2. Given the variable is set to any value other than the exact string `true` (including `True`,
   `TRUE`, `1`, `yes`, padded whitespace, or empty), when the guard evaluates it, then publication
   remains frozen — the enabling comparison is an exact POSIX string match in bash, not a
   case-insensitive GitHub expression.

3. Given the Release Owner sets the repository variable to exactly `true`, when CI succeeds for a
   push to `main`, then the release job proceeds under the existing CI-success + push-event guards.

4. Given the guard job fails or is skipped, when the release job is evaluated, then it is skipped
   (fail-closed via `needs`).

5. Governance tests prove the freeze:
   - a new `ReleaseWorkflow_PublishFreezeGate_IsFailClosedByDefault` pins the `freeze-guard` job,
     the `PUBLISH_ENABLED: ${{ vars.HEXALITH_RELEASE_PUBLISH_ENABLED }}` env binding, the exact
     bash comparison `[ "${PUBLISH_ENABLED}" = "true" ]`, the `release` job's
     `needs: freeze-guard` + `needs.freeze-guard.outputs.publish-enabled == 'true'` condition
     alongside the retained CI-success/push conjuncts, the frozen-branch notice/step-summary, and
     the REL-3 removal-condition comment marker;
   - a new `Workflows_HaveNoPublishPathOutsideGatedReleaseWorkflow` scans every
     `.github/workflows/*.yml` and pins that only `release.yml` contains a `uses:` reference to
     `domain-release.yml`, and no repository-owned workflow executes `npx semantic-release` or
     `dotnet nuget push` (assertions must target executable `uses:`/`run:` content — those strings
     appear elsewhere in comments today).

6. Before implementation, the deployment guide labels the freeze guard as an approved target that is not yet operational. After REL-4 lands, the Developer updates it to active-state wording and records governance-test results plus the first CI-authoritative frozen Release run URL showing `freeze-guard` success, release-job skip, and no publication side effect. The active runbook must retain: variable name, exact enabling value, Release Owner-only custody, org-vs-repo shadowing hazard, frozen-run shape, and local `NUGET_API_KEY` residual.

7. The gate is removed or replaced only when the REL-3 exact-artifact gate is operational and
   REL-AI-1 records passing real-release evidence; re-enabling publication is a Release Owner
   variable change, not a workflow edit. The workflow carries a comment marker recording this
   contract, and the governance test pins the marker.

8. The gate contract (variable name, exact-match semantics, default-frozen, caller-vars resolution)
   is recorded in the Hexalith.Builds upstream request as the common mechanism for all Hexalith
   modules (done at approval; keep consistent if the implemented shape drifts).

## Approved Target Shape (`.github/workflows/release.yml`)

```yaml
jobs:
  # REL-4 (2026-07-15): temporary technical release freeze (REL-3 / REL-AI-1).
  # Publication is DISABLED BY DEFAULT. It is enabled only when the Release
  # Owner-controlled repository variable HEXALITH_RELEASE_PUBLISH_ENABLED is
  # exactly the string 'true'. The comparison is an exact POSIX match in bash
  # because GitHub expression '==' is case-insensitive; missing or malformed
  # values keep the freeze. Frozen runs conclude green with a freeze notice.
  # REMOVAL/REPLACEMENT is permitted only when the permanent REL-3
  # exact-artifact gate is operational and REL-AI-1 records passing
  # real-release evidence; the variable then becomes the standing Release
  # Owner freeze switch (see architecture.md, FR24 Release Evidence
  # Architecture). Pinned by CiGovernanceTests.
  freeze-guard:
    if: >-
      github.event.workflow_run.conclusion == 'success' &&
      github.event.workflow_run.event == 'push'
    runs-on: ubuntu-latest
    outputs:
      publish-enabled: ${{ steps.evaluate.outputs.publish-enabled }}
    steps:
      - name: Evaluate release publication freeze
        id: evaluate
        env:
          PUBLISH_ENABLED: ${{ vars.HEXALITH_RELEASE_PUBLISH_ENABLED }}
        run: |
          set -euo pipefail
          if [ "${PUBLISH_ENABLED}" = "true" ]; then
            echo "publish-enabled=true" >> "$GITHUB_OUTPUT"
            echo "::notice::Release publication ENABLED: HEXALITH_RELEASE_PUBLISH_ENABLED is exactly 'true' (Release Owner-controlled)."
          else
            echo "publish-enabled=false" >> "$GITHUB_OUTPUT"
            echo "::notice::Release publication FROZEN (REL-3 / REL-AI-1): HEXALITH_RELEASE_PUBLISH_ENABLED is not exactly 'true'. No package will be published."
            echo "Release publication is frozen until the REL-3 exact-artifact gate is operational." >> "$GITHUB_STEP_SUMMARY"
          fi

  release:
    needs: freeze-guard
    if: >-
      github.event.workflow_run.conclusion == 'success' &&
      github.event.workflow_run.event == 'push' &&
      needs.freeze-guard.outputs.publish-enabled == 'true'
    permissions:
      contents: write
      issues: write
      pull-requests: write
    uses: Hexalith/Hexalith.Builds/.github/workflows/domain-release.yml@main
```

Extend the existing workflow header comment to describe the freeze. Keep `with:`/`secrets:`
unchanged. Do NOT introduce `workflow_dispatch`, dry-run inputs, or approval environments.

## Dev Notes

- **Existing governance-test collision:** `CiGovernanceTests.ReleaseWorkflow_RunsViaWorkflowRunAfterCiSuccess`
  (`tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs`) asserts
  `ShouldNotContain` for `workflow_dispatch:`, `release_owner_approved`, `release_approver`,
  `RELEASE_OWNER_APPROVED`, `RELEASE_APPROVER`, `RELEASE_DRY_RUN`, `RELEASE_CONCURRENT_SAME_VERSION`.
  The approved token names (`freeze-guard`, `publish-enabled`, `HEXALITH_RELEASE_PUBLISH_ENABLED`)
  do not collide — keep it that way. Amend that test's stale comment ("No manual dispatch / approval
  / dry-run gating is reintroduced") to record the REL-4 freeze gate as a deliberate,
  separately-pinned exception; keep all existing pins.
- **Why bash, not expression `==`:** GitHub Actions expression string comparison is
  case-insensitive (`'True' == 'true'` is true), which would let a malformed value enable
  publication — violating AC2. The exact match must live in the shell step; the release job's
  expression then compares the guard's *controlled* output only.
- **Skip-not-fail:** frozen runs must conclude green. A red Release run on every merge trains
  people to ignore red; the freeze is expected behavior, not a failure.
- **Commit type:** use a `ci/`-prefixed branch and `ci:`/`test:`/`docs:` commit types so REL-4
  itself cannot trigger a release bump even before the gate merges. Never `feat:`/`fix:` here.
- **Test run model:** solution-level `dotnet test` with trait filters and
  `DiffEngine_Disabled=true`; if local VSTest sockets are blocked, run the built
  `Hexalith.FrontComposer.Shell.Tests` executable directly with `-class` filters for the
  Governance suite.
- **Verification of the live behavior** is CI-authoritative: after merge, the next push-CI success
  on `main` must show Release green with `release` skipped and the freeze notice (record the run
  URL in this story on completion).
- This story deliberately does NOT touch `.releaserc.json`, `release-evidence.yml`,
  `eng/release_evidence.py`, or the Hexalith.Builds submodule. The upstream common gate is
  owner-filed via the G2 request document, not implemented here.

## Implementation Boundary

- FrontComposer owns: `release.yml` gate, governance tests, deployment-guide accuracy.
- Release Owner owns: `HEXALITH_RELEASE_PUBLISH_ENABLED` custody (never `true` before REL-3
  evidence), org-vs-repo variable posture, filing the extended Hexalith.Builds request.
- Hexalith.Builds owner owns: the common gate in `domain-release.yml` (upstream timeline).
- REL-3 owns: later removal/re-scope of the gate on real-release evidence.

## References

- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-15-release-freeze-enforcement.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-15-rel-ai-1-prepublish-enforcement.md`
- `_bmad-output/implementation-artifacts/rel-3-enforce-fr24-pre-publish-and-reconcile-releases.md` (AC1)
- `_bmad-output/planning-artifacts/g2-hexalith-builds-inline-pre-publish-gate-request.md` (second required item)
- `_bmad-output/project-docs/deployment-guide.md` ("Release freeze control" subsection)
