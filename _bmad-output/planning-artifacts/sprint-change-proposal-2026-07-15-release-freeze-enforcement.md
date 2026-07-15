---
title: Sprint Change Proposal - Technically Enforce the Temporary Release Freeze
project: frontcomposer
date: 2026-07-15
workflow: bmad-correct-course
mode: Batch
trigger: "The 2026-07-15 REL-3 correction declared publish-capable releases frozen, but release.yml still invokes semantic-release automatically after every successful push-CI on main. The freeze is administrative only; any fix:/feat: merge can publish another unsigned release."
intent: Add a fail-closed, Release Owner-controlled technical publish gate so ordinary main-branch activity cannot publish while REL-3 is being built, with the mechanism standardized as a common control for all Hexalith modules.
relationship: hardens-the-freeze-declared-by
prior_proposals:
  - _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-15-rel-ai-1-prepublish-enforcement.md
  - _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-13-rel-ai-1-fr24-rehome-into-rel-2.md
status: approved
approval: approved-by-administrator-2026-07-15
scope: Moderate
implementation_risk: Low-Medium
implementation_effort: Low
owner: Release Owner
urgency: stop-the-line — precedes all other REL-3 work
---

# Sprint Change Proposal — Technically Enforce the Temporary Release Freeze

## Section 1 — Issue Summary

The approved 2026-07-15 correction (`sprint-change-proposal-2026-07-15-rel-ai-1-prepublish-enforcement.md`)
froze publish-capable FrontComposer releases until REL-3's exact-artifact pre-publication gate is
operational (`release_control: "frozen — … automated package publication is prohibited until REL-3
real-release evidence passes"`). That freeze exists only in planning artifacts. Nothing in the
executable pipeline enforces it:

- `.github/workflows/release.yml` triggers on `workflow_run` after every successful push-CI on `main`
  and delegates unconditionally to the shared reusable
  `Hexalith/Hexalith.Builds/.github/workflows/domain-release.yml@main`.
- `domain-release.yml`'s final step runs `npx semantic-release` with `NUGET_API_KEY` in scope.
- `.releaserc.json`'s `publishCmd` pushes `.nupkg`/`.snupkg` directly to nuget.org and
  `@semantic-release/github` creates the GitHub Release.
- Product development explicitly continues during the freeze (Epic 11 and REL-3 itself), so
  release-triggering `fix:`/`feat:` commits are expected on `main`.

Therefore the next ordinary `fix:` or `feat:` merge to `main` would publish v3.2.3 with exactly the
v3.2.2 defects: unsigned packages, invalid manifest, `classification=blocked`,
`publish_authorized=false`. The v3.2.2 evidence (GitHub Release with 16 unsigned package assets,
`NU3004` on direct verification, green evidence workflow over blocked readiness) is the concrete
proof that this path is live and harmful.

**Steering input from the Administrator (2026-07-15, during this correction):** the freeze mechanism
must be **common to all Hexalith modules**, not a FrontComposer-only construct. Every Hexalith module
publishes through the same shared `domain-release.yml`, so every module has the same unguarded
auto-publish exposure.

**Problem statement:** The declared REL-3 release freeze is not technically enforced. The release
pipeline auto-publishes on ordinary main-branch activity, so the freeze depends on contributors
avoiding release-triggering commit types — the opposite of fail-closed. A repository control must
make publication impossible by default, enableable only by an explicit Release Owner condition,
immune to missing/malformed configuration, proven by governance tests, and removable only when the
permanent REL-3 gate is operational. The mechanism must be a common Hexalith control.

## Section 2 — Impact Analysis

### Change Navigation Checklist

| Item | Status | Finding |
| --- | --- | --- |
| 1.1 Triggering story | [x] | REL-3 (`ready-for-dev`) AC1 declares the freeze; the trigger is the gap between AC1's declaration and the executable pipeline. |
| 1.2 Core problem | [x] | Technical limitation + new requirement: the approved freeze has no enforcement point, and the Administrator requires the control to be Hexalith-common. |
| 1.3 Evidence | [x] | `release.yml` workflow_run trigger → `domain-release.yml` `npx semantic-release` → `.releaserc.json` direct `dotnet nuget push`; live v3.2.1/v3.2.2 unsigned publications; freeze recorded only in YAML/markdown planning artifacts. |
| 2.1 Current epic impact | [x] | RG-1/REL-AI-1 unchanged. REL-3 remains the owner of the permanent gate; its AC1 gains a concrete enforcement reference. |
| 2.2 Epic-level changes | [!] | Add one small release-governance story, REL-4, ordered before REL-3 implementation. No product epic changes. |
| 2.3 Future epic impact | [x] | Epic 11 and all product work continue unblocked — that is the point: merges stay safe during the freeze. |
| 2.4 New/remove epics | [x] | No new epic. One story under RG-1. |
| 2.5 Priority/order | [!] | REL-4 is stop-the-line and precedes REL-3 development; it is small enough to land immediately. |
| 3.1 PRD conflicts | [!] | FR-24 already prohibits publication without authorization but does not name the interim enforcement; add one consequence. D-6 gains a follow-up note. |
| 3.2 Architecture conflicts | [!] | The FR24 Release Evidence Architecture assigns "the release freeze" to the Release Owner without a mechanism; add the fail-closed gate and its Hexalith-common contract. |
| 3.3 UX conflicts | [N/A] | No user-interface impact. |
| 3.4 Other artifacts | [!] | `release.yml`, `CiGovernanceTests`, REL-3 story AC1, sprint status, G2 upstream request, deployment guide. |
| 4.1 Direct adjustment | Viable | Add a guard to the caller workflow + governance tests. Effort Low; risk Low-Medium. **Selected.** |
| 4.2 Rollback | Not viable | Reverting REL-2's reusable-workflow adoption would resurrect the bespoke pipeline G1 already superseded and still would not add a freeze. |
| 4.3 MVP review | Not viable | No scope problem exists; this is release-safety hardening. |
| 4.4 Recommended path | [x] | Direct Adjustment in two layers: immediate FrontComposer caller-side gate + required Hexalith.Builds common gate. |
| 5.1–5.5 Proposal components | [x] | Sections 3–5 below. |
| 6.1–6.2 Final review | [x] | Proposal is specific, testable, and does not reduce any REL-3 requirement. |
| 6.3 Final approval | [!] | Pending Administrator approval of this document. |
| 6.4 Sprint status update | [!] | Applied on approval (Proposal E). |
| 6.5 Handoff | [!] | Section 5. |

### Why the enforcement point is the caller workflow

Alternatives considered for the immediate layer:

- **`domain-release.yml` (shared submodule):** the correct *permanent, common* home, but it must not
  be edited from this repository; it changes on the Hexalith.Builds owner's timeline. Urgency rules
  it out as the *first* enforcement point.
- **`.releaserc.json` `prepareCmd` guard:** enforceable, but it runs after checkout/build/restore
  compute, semantic-release has already resolved a version, and a failing prepare turns every frozen
  push into a red Release run — noise that trains people to ignore red. It also does not generalize
  to other modules (each has its own `.releaserc.json`).
- **Branch protection / disabling the workflow in the UI:** pure settings, invisible to git history,
  not testable by governance tests, silently reversible.
- **`release.yml` caller job gate (selected):** repository-owned, effective the moment it merges,
  visible in git history, pinned by the existing `CiGovernanceTests` machinery, and it skips (green)
  rather than fails when frozen.

### Fail-closed analysis of the selected control

The gate is a dedicated `freeze-guard` job that evaluates the repository/organization variable
`HEXALITH_RELEASE_PUBLISH_ENABLED` with an **exact POSIX string comparison in bash**
(`[ "$VALUE" = "true" ]`), publishing a controlled `publish-enabled` job output. The `release` job
adds `needs: freeze-guard` and requires `needs.freeze-guard.outputs.publish-enabled == 'true'` in
addition to its existing CI-success + push-event conjuncts.

- **Missing variable** → empty string → not `true` → frozen.
- **Malformed value** (`True`, `TRUE`, `1`, `yes`, `enabled`, padded whitespace) → frozen. The exact
  match deliberately happens in bash because GitHub Actions expression `==` compares strings
  case-insensitively; a raw `vars.X == 'true'` job condition would accept `True`.
- **Guard job fails or is skipped** → `needs` dependency leaves the release job skipped → frozen.
- **Alternate publish path** → governance tests pin that only `release.yml` references
  `domain-release.yml` and that no repository-owned workflow executes `npx semantic-release` or
  `dotnet nuget push` (currently true: those strings appear elsewhere only in comments).
- **Who can enable:** GitHub Actions variables are writable only through repository/organization
  settings — Release Owner-controlled. Documented hazard: repository-level variables shadow
  organization-level ones, so an org-level `HEXALITH_RELEASE_PUBLISH_ENABLED=true` would leak into
  any repo that has no repo-level value. The runbook therefore requires FrontComposer to carry an
  explicit repo-level value whenever an org-level value exists.
- **Out of scope (documented residual):** a human with `NUGET_API_KEY` custody running
  semantic-release locally bypasses any workflow control; API-key custody remains a Release Owner
  responsibility under FR24 and is unchanged by this proposal.

### The common-to-all-Hexalith-modules requirement

Every Hexalith module that publishes through `domain-release.yml` shares the unguarded
`npx semantic-release` exposure. The durable, common enforcement point is therefore the reusable
workflow itself. Because called reusable workflows resolve the `vars` context from the **caller's**
repository/organization, `domain-release.yml` can host one standard gate that every module controls
through its own `HEXALITH_RELEASE_PUBLISH_ENABLED` variable — same name, same exact-match semantics,
same default-frozen posture. This is an upstream Hexalith.Builds change and follows the same
owner-approved request channel as the G2 signing contract (Proposal F). Rollout is deliberately
breaking-by-design: when the shared gate lands, every module is frozen until its owner sets the
variable — which is exactly the fail-closed default the Administrator required. FrontComposer's
caller-side gate remains as local defense-in-depth until REL-3 supersedes the temporary posture.

## Section 3 — Recommended Approach

**Direct Adjustment, two layers, one new story (REL-4).**

1. **Layer 1 — immediate, repository-owned (REL-4, this repo):** add the fail-closed `freeze-guard`
   publish gate to `.github/workflows/release.yml`, pin it with governance tests, document the
   freeze/unfreeze runbook, and reference it from REL-3 AC1. Effective on merge; no upstream
   dependency; no red-run noise (frozen runs conclude green with an explicit freeze notice).
2. **Layer 2 — durable, common (upstream request):** extend the existing mandatory Hexalith.Builds
   request with the standard release-freeze gate contract for `domain-release.yml`, so all Hexalith
   modules gain the identical Release Owner-controlled, default-frozen switch. Filing the request
   does not gate FrontComposer's own freeze (Layer 1 already enforces it).

Lifecycle: the control is **temporary in posture, permanent in mechanism**. While REL-3 is
incomplete, the variable stays unset/non-`true` and publication is impossible. When REL-3's
exact-artifact gate is operational and a real release passes every FR24 criterion, the Release Owner
sets the variable to `true` to re-enable publication; the gate itself is then re-scoped as the
standing Release Owner freeze switch already assigned by the FR24 architecture ("the Release Owner
owns … the release freeze") rather than deleted. Any removal or replacement of the gate requires the
REL-3 completion evidence — never a routine cleanup.

Scope **Moderate** (one new story, backlog reordering, cross-repo request extension, six artifact
edits); implementation effort **Low** (a guard job, test amendments, documentation); implementation
risk **Low-Medium** (workflow-only change; the failure mode of a mis-wired gate is "release stays
frozen", which is the safe direction; governance tests and the run notice make the state visible).

## Section 4 — Detailed Change Proposals

### Proposal A — Add the fail-closed publish gate to `release.yml`

Artifact: `.github/workflows/release.yml` (executable; implemented by REL-4, shown here as the
approved target shape).

OLD (trigger/jobs excerpt):

```yaml
jobs:
  release:
    if: >-
      github.event.workflow_run.conclusion == 'success' &&
      github.event.workflow_run.event == 'push'
    permissions:
      contents: write
      issues: write
      pull-requests: write
    uses: Hexalith/Hexalith.Builds/.github/workflows/domain-release.yml@main
```

NEW:

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

The workflow header comment is extended to describe the freeze. `with:`/`secrets:` are unchanged.
No `workflow_dispatch`, dry-run, or approval-environment mechanics are introduced.

Rationale: enforces the freeze at the only repository-owned point upstream of
`npx semantic-release`; skip-not-fail keeps `main` green during the freeze; the guard's controlled
output makes the release job's expression comparison bypass-safe.

### Proposal B — Governance tests proving the freeze

Artifact: `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` (implemented by
REL-4).

1. **New** `ReleaseWorkflow_PublishFreezeGate_IsFailClosedByDefault`: reads `release.yml` and pins —
   the `freeze-guard` job exists with the CI-success + push-event condition; the exact bash
   comparison `[ "${PUBLISH_ENABLED}" = "true" ]` (not a GitHub-expression `vars.… == 'true'`
   enablement); `PUBLISH_ENABLED: ${{ vars.HEXALITH_RELEASE_PUBLISH_ENABLED }}`; the `release` job
   declares `needs: freeze-guard` and its `if` requires
   `needs.freeze-guard.outputs.publish-enabled == 'true'` alongside the retained CI-success/push
   conjuncts; the frozen branch emits the freeze notice and step-summary line; the REL-3
   removal-condition comment marker is present (so deleting the gate without REL-3 evidence fails
   the Governance lane, and so does silently dropping the removal contract).
2. **New** `Workflows_HaveNoPublishPathOutsideGatedReleaseWorkflow`: scans every
   `.github/workflows/*.yml` and pins — only `release.yml` contains a `uses:` reference to
   `domain-release.yml`; no repository-owned workflow executes `npx semantic-release`; no
   repository-owned workflow contains `dotnet nuget push` (currently satisfied; occurrences
   elsewhere are comments only, so the assertions target executable `uses:`/`run:` content).
3. **Amend** `ReleaseWorkflow_RunsViaWorkflowRunAfterCiSuccess`: update the stale comment ("No
   manual dispatch / approval / dry-run gating is reintroduced") to record the REL-4 freeze gate as
   a deliberate, separately-pinned exception; keep all existing positive and negative pins (the
   chosen tokens `freeze-guard`, `publish-enabled`, `HEXALITH_RELEASE_PUBLISH_ENABLED` do not
   collide with the banned `RELEASE_OWNER_APPROVED`/`RELEASE_DRY_RUN`/`workflow_dispatch` family,
   which continues to guard against reintroducing the pre-REL-2 approval mechanics).

Rationale: the freeze must be provable and tamper-evident. The Governance lane is CI-blocking, so a
PR that removes or weakens the gate fails CI unless it also visibly rewrites the governance pins.

### Proposal C — Add story REL-4 and amend REL-3 AC1

New artifact: `_bmad-output/implementation-artifacts/rel-4-enforce-temporary-release-freeze.md`
(`ready-for-dev`).

> As the Release Owner, I want automated package publication technically disabled by default while
> REL-3 is being built, so ordinary `fix:`/`feat:` merges to `main` cannot publish another
> unauthorized release.

Acceptance criteria:

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
5. Governance tests prove the freeze per Proposal B, including the single-publish-path scan and the
   REL-3 removal-condition marker.
6. The deployment guide documents the freeze runbook: variable name, exact enabling value,
   Release Owner-only custody, the org-vs-repo variable shadowing hazard (a repo-level value must
   exist whenever an org-level value exists), and the local-`NUGET_API_KEY` residual.
7. The gate is removed or replaced only when the REL-3 exact-artifact gate is operational and
   REL-AI-1 records passing real-release evidence; re-enabling publication is a Release Owner
   variable change, not a workflow edit.
8. The gate contract (variable name, exact-match semantics, default-frozen, caller-vars resolution)
   is recorded in the Hexalith.Builds upstream request as the common mechanism for all Hexalith
   modules (Proposal F); FrontComposer's caller-side gate stays as defense-in-depth until REL-3
   supersedes the temporary posture.

Amend `_bmad-output/implementation-artifacts/rel-3-enforce-fr24-pre-publish-and-reconcile-releases.md`
AC1:

OLD:

> 1. Given REL-3 is not yet operational, when a publish-capable release would start, then publication
>    remains frozen and no NuGet or GitHub package release is authorized.

NEW:

> 1. Given REL-3 is not yet operational, when a publish-capable release would start, then publication
>    remains frozen and no NuGet or GitHub package release is authorized. The freeze is technically
>    enforced by REL-4's fail-closed publish gate (`HEXALITH_RELEASE_PUBLISH_ENABLED` must be exactly
>    `true`; disabled by default). REL-3 may re-enable publication, and may re-scope or replace the
>    gate, only after the exact-artifact gate is operational and the Release Owner authorizes it.

Rationale: REL-3 AC1 currently *asserts* the freeze without an enforcement mechanism; REL-4 supplies
the mechanism as a small, immediately implementable story so the large high-risk REL-3 story is not
blocked on, or bloated by, the urgent hardening.

### Proposal D — PRD and epics traceability

Artifact: `_bmad-output/planning-artifacts/prd.md` — add one FR-24 consequence after the REL-2/REL-3
bullet:

> - While the REL-3 gate is not yet operational, automated publication is technically frozen by a
>   fail-closed publish control: disabled by default, enabled only by an explicit Release
>   Owner-controlled repository condition, with missing or malformed configuration keeping the
>   freeze. The control's mechanism is standardized for all Hexalith modules, and it may be removed
>   or replaced only when the permanent REL-3 gate is operational.

Amend D-6 (append to current state): "Follow-up 2026-07-15: the declared freeze is technically
enforced by the fail-closed `HEXALITH_RELEASE_PUBLISH_ENABLED` gate (REL-4); the same gate contract
is a required common Hexalith.Builds control for all modules."

Artifact: `_bmad-output/planning-artifacts/epics.md` — append to the 2026-07-15 update block in the
FR Coverage / RG-1 section:

> **Update (correct-course 2026-07-15, freeze enforcement):** the REL-3 freeze is now technically
> enforced. `REL-4: Technically enforce the temporary release freeze` adds a fail-closed publish
> gate to `release.yml` (publication disabled unless the Release Owner-controlled
> `HEXALITH_RELEASE_PUBLISH_ENABLED` variable is exactly `true`; governance tests pin the gate and
> the absence of alternate publish paths). The identical gate contract is a required Hexalith.Builds
> upstream item so all Hexalith modules share the same default-frozen control. REL-4 precedes REL-3
> implementation; the gate is removed/re-scoped only on REL-3 real-release evidence. See
> `sprint-change-proposal-2026-07-15-release-freeze-enforcement.md`.

### Proposal E — Sprint status

Artifact: `_bmad-output/implementation-artifacts/sprint-status.yaml`

- Add `rel-4-enforce-temporary-release-freeze: ready-for-dev` with an entry comment recording the
  trigger (unenforced freeze + auto-publishing pipeline), the two-layer design, the
  common-to-all-modules directive, and the ordering (REL-4 executes before REL-3 development
  starts).
- Prepend to the REL-AI-1 `progress` field: "2026-07-15 (freeze enforcement): the frozen
  release_control is now technically enforced by REL-4's fail-closed
  HEXALITH_RELEASE_PUBLISH_ENABLED gate in release.yml; publication is disabled by default and only
  the Release Owner can enable it. The gate contract is a required common Hexalith.Builds control
  for all Hexalith modules. Removal/re-scope only on REL-3 real-release evidence."
- `release_control` wording gains "(technically enforced by REL-4)".

### Proposal F — Extend the mandatory Hexalith.Builds request with the common freeze gate

Artifact: `_bmad-output/planning-artifacts/g2-hexalith-builds-inline-pre-publish-gate-request.md`

Add a clearly separated second required item, "Common release-freeze gate (all Hexalith modules)":

- `domain-release.yml` must refuse to run its Semantic Release step unless the calling repository's
  `HEXALITH_RELEASE_PUBLISH_ENABLED` configuration variable is exactly the string `true`, evaluated
  with an exact (case-sensitive, untrimmed) comparison in a shell step; missing or malformed values
  freeze publication with an explicit notice, and the run concludes green (skip-not-fail).
- Called reusable workflows resolve `vars` from the caller's repository/organization, which is what
  makes one shared gate per-module controllable; the Hexalith.Builds owner must verify this
  resolution behavior on the current GitHub Actions platform as part of implementation. Fallback
  shape if verification fails: a required `publish-enabled` boolean input defaulting to `false`,
  computed by each caller from its own variable.
- Rollout is deliberately fail-closed for the whole ecosystem: when the gate lands, every consuming
  module is frozen until its owner sets the variable. The owner sets `true` on modules that should
  keep publishing at rollout time; FrontComposer's stays non-`true` until REL-3 completes.
- Documented hazard: repo-level variables shadow org-level ones; an org-level `true` leaks into
  repos with no repo-level value, so frozen repos must carry an explicit repo-level value whenever
  an org-level value exists.
- This item does not gate FrontComposer's own freeze (the REL-4 caller-side gate enforces it
  immediately) and is independent of, though filed alongside, the signing-contract item. The
  signing-contract item remains blocking for REL-3; the freeze-gate item is required for ecosystem
  coverage per the Administrator's 2026-07-15 directive.

As with the signing item: this repository does not edit or commit the shared submodule; filing,
approval, implementation, and revision tracking are Release Owner + Hexalith.Builds-owner actions.

### Proposal G — Deployment-guide runbook

Artifact: `_bmad-output/project-docs/deployment-guide.md`

Add a "Release freeze control" subsection: the gate's location (release.yml `freeze-guard`),
variable name and exact enabling value, default-frozen semantics, Release Owner-only custody of
repository/organization variables, the org-vs-repo shadowing hazard and the rule that a repo-level
value must exist whenever an org-level value exists, what a frozen run looks like (green with the
freeze notice; release job skipped), the local-`NUGET_API_KEY` residual risk, and the REL-3-gated
removal/re-scope contract.

## Section 5 — Implementation Handoff

### Change-scope classification

**Moderate course correction; Low implementation effort; Low-Medium implementation risk.** One new
stop-the-line story ordered ahead of REL-3, coordinated planning/documentation edits, and a
cross-repository request extension. No product scope, runtime, or UX change. The mis-implementation
failure mode is "stays frozen" — the safe direction.

### Owners

- **Release Owner:** approves this proposal; owns `HEXALITH_RELEASE_PUBLISH_ENABLED` custody
  (never sets it to `true` before REL-3 evidence passes); files the extended Hexalith.Builds
  request; owns the org-vs-repo variable posture at rollout.
- **Developer:** implements REL-4 (Proposal A workflow change, Proposal B governance tests,
  Proposal G runbook) immediately after approval; commit type `ci:`/`test:`/`docs:` so REL-4 itself
  cannot trigger a release even before the gate merges.
- **QA/Test Architect:** reviews the governance pins for bypass coverage (alternate publish paths,
  exact-match semantics, removal marker).
- **Hexalith.Builds owner:** implements the common freeze gate in `domain-release.yml` per
  Proposal F on the upstream timeline; verifies caller-vars resolution.
- **Product Owner:** applies Proposals C (planning parts), D, and E on approval.

### Sequence

1. Administrator approves this proposal.
2. Apply Proposals C–E (planning artifacts, REL-4 story, sprint status) and G's runbook stub.
3. Developer implements REL-4 (Proposals A + B + G) on a `ci/`-prefixed branch, runs the Governance
   lane, and lands it through the normal PR + `/bmad-code-review` path. From this merge on, the
   freeze is technically enforced.
4. Release Owner extends and files the Hexalith.Builds request (Proposal F).
5. When the upstream common gate lands, FrontComposer keeps its caller-side gate as
   defense-in-depth; REL-3 later re-scopes both per its completion evidence.

### Success criteria

- A `fix:`/`feat:` merge to `main` after REL-4 lands produces a green Release run whose release job
  is skipped with the freeze notice, and no NuGet/GitHub side effect.
- Governance lane fails on any PR that removes the gate, weakens the exact-match comparison, or
  introduces a second publish path.
- REL-3 AC1, sprint status, PRD FR-24, epics, the G2 request, and the deployment guide all describe
  the same enforced control.
- All Hexalith modules gain the identical control when the upstream item lands.

## Section 6 — Approval State

**Approved by Administrator on 2026-07-15.** Compiled and approved in Batch mode ("Approve
(Recommended)": apply planning edits and hand REL-4 to the Developer for immediate implementation).

Applied on approval:

- `_bmad-output/implementation-artifacts/rel-4-enforce-temporary-release-freeze.md` — created
  `ready-for-dev` with the approved acceptance contract and implementation notes.
- `_bmad-output/implementation-artifacts/rel-3-enforce-fr24-pre-publish-and-reconcile-releases.md` —
  AC1 now references the REL-4 technical enforcement and its removal contract.
- `_bmad-output/planning-artifacts/prd.md` — FR-24 gained the interim technical-freeze consequence;
  D-6 records the enforcement follow-up and the common Hexalith.Builds contract.
- `_bmad-output/planning-artifacts/epics.md` — freeze-enforcement update block added to the RG-1
  section.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` — REL-4 added `ready-for-dev` ordered
  before REL-3 development; REL-AI-1 `release_control`/`progress` record the technical enforcement.
- `_bmad-output/planning-artifacts/g2-hexalith-builds-inline-pre-publish-gate-request.md` — second
  required item added: the common release-freeze gate for all Hexalith modules.
- `_bmad-output/project-docs/deployment-guide.md` — "Release freeze control" runbook subsection
  added (documents the approved control; REL-4 implements it).

Executable changes (Proposal A `release.yml` gate, Proposal B governance tests, the deployment-guide
current-state table updates) are routed to the Developer via REL-4 and were deliberately NOT applied
by this Correct Course pass. This application did not modify workflows, semantic-release
configuration, product source, or the Hexalith.Builds submodule.
