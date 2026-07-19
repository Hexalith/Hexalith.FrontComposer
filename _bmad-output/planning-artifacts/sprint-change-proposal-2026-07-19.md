---
project: frontcomposer
date: 2026-07-19
workflow: bmad-correct-course
mode: Incremental
trigger: "Replace brittle exact-SHA assertions with shared-catalog contract validation while preserving exact dependency provenance in release evidence."
status: approved
approved: 2026-07-19
approvedBy: Administrator
planningChangesApplied: 2026-07-19
scope: Moderate
recommendedApproach: Direct Adjustment
handoffStatus: completed
architectureRatified: 2026-07-19
architectureSpine: _bmad-output/planning-artifacts/architecture/architecture-gov-1-2026-07-19/ARCHITECTURE-SPINE.md
handoff:
  - Product Owner
  - Architect
  - Developer
  - Release Owner
---

# Sprint Change Proposal: Contract-Based Submodule Governance and Dependency Provenance

Approval: approved by Administrator on 2026-07-19.

## 1. Issue Summary

FrontComposer's shared-catalog Governance tests currently treat selected Git commit identities as
compatibility requirements. `InfrastructureGovernanceTests` hard-codes the expected root and nested
`Hexalith.Builds` commits and fails whenever an intentional pointer advance is not accompanied by a
mechanical constant edit, even when the referenced catalogs satisfy every package and build contract.

The actual invariants are different:

1. Every affected module restores and builds against a compatible shared package catalog.
2. Root and nested gitlink changes are intentional, visible, and reviewable.
3. A release records the exact dependency graph used to produce its artifacts.

A Git SHA is necessary provenance, but it is not itself a compatibility contract.

### Trigger and Evidence

- Triggering context: Story 11.17d's evidence/status review was blocked by unrelated shared-catalog
  gitlink assertions while its owned mechanical Shell split remained valid.
- Direct evidence: the focused Governance run on 2026-07-19 passed `0/2`:
  - root `references/Hexalith.Builds` is `f8981e8...`, while the test requires `deb76e9...`;
  - EventStore's nested `references/Hexalith.Builds` is `786955b...`, while the test requires
    `c177c66...`.
- Repetition evidence: recent history repeatedly edits the same constants or reconciles pointers,
  including `96840a29`, `a56c6f62`, `6a4350ec`, `aaefba44`, and `064b886d`.
- Compatibility evidence: the Builds catalogs at the currently referenced `f8981e8`, `786955b`, and
  `c177c66` commits all expose the required Tenants, TimeProvider, MCP, and System.Reactive values.
  The failure is therefore identity drift, not a demonstrated catalog-contract failure.
- Coverage gap: the repository has eight root gitlinks and a cyclic nested gitlink graph, but
  product Governance pins only a small historical subset.
- Release-evidence gap: `prepare_manifest` seals the FrontComposer commit and package artifacts but
  does not record the root and nested gitlink graph that supplied source/build dependencies.
- Contract-version gap: `Hexalith.Builds/Props/Directory.Packages.props` exposes
  `HexalithVersionsLoaded` but no semantic shared-catalog contract version.

### Problem Classification

This is a failed governance approach discovered during implementation and review. The current checks
conflate compatibility enforcement with provenance recording, create false-red CI, and still fail to
record the complete defined depth-1/2 v1 release dependency graph.

## 2. Impact Analysis

### Epic and Story Impact

No product epic is invalidated, removed, or resequenced. Completed Epics 1-10 remain closed, and no
runtime feature is reopened.

Add one cross-cutting governance story, `GOV-1`, rather than attaching unrelated dependency work to
Story 11.17d or reopening REL-3. `GOV-1` spans the Epic 11 maintainability/enforcement outcome and
FR-24 release provenance, while retaining separate ownership from both delivery streams.

Story 11.17d remains `in-progress` until the corrected Governance lane passes on its promotion
revision. Its scope and acceptance criteria do not absorb `GOV-1` implementation.

### Artifact Conflicts

| Artifact | Conflict | Required change after approval |
| --- | --- | --- |
| `prd.md` | Repository policy says how submodules are initialized but does not separate compatibility from provenance. | Add the dependency-governance invariant and release-graph requirement. |
| `epics.md` | No story owns contract-based shared-catalog validation and complete gitlink provenance. | Register `GOV-1` as cross-cutting governance work and trace it to FR-24/NFR dependency governance. |
| `architecture.md` | The release architecture seals artifact paths/hashes but omits the dependency graph; external-dependency text records only initialization policy. | Add shared-catalog compatibility and dependency-provenance architecture. |
| UX artifacts | No user interface, journey, accessibility, route, or visual behavior changes. | No change. |
| `InfrastructureGovernanceTests.cs` | Exact historical SHAs are executable compatibility assertions; catalog bytes may also come from a checkout different from the indexed gitlink. | Load catalog content from each actual gitlink commit and validate semantic contract requirements. |
| Release evidence | The sealed manifest has the root commit but no root/nested gitlink edges or catalog provenance. | Seal and verify a deterministic dependency graph. |
| CI | Pointer advances are reviewed only as raw Git diffs; standalone module compatibility is not a pointer-change gate. | Emit a dependency-graph diff and run targeted restore/build validation for affected modules. |
| Hexalith.Builds | No semantic catalog-contract version exists. | Route `BUILD-CAT-1` upstream; do not edit the submodule as part of FrontComposer `GOV-1`. |

### Technical and Release Impact

- No FrontComposer runtime, public API, schema, generated output, package inventory, or UX change.
- Governance becomes stricter about behavior: it validates every selected catalog, not a few SHAs.
- Release reproducibility improves because the exact graph becomes sealed evidence instead of living
  only in Git trees.
- The release manifest schema changes intentionally and requires producer, verifier, fixture, and
  post-publication verification updates together.
- No recursive submodule initialization is permitted. Exact commits are inspected from Git objects
  or targeted explicit checkouts without moving submodule HEADs.

### MVP and Timeline Impact

MVP and v1.0 product scope are unchanged. `GOV-1` is a release-quality correction that should execute
before Story 11.17d is promoted and before the next governed release manifest is accepted.

Estimated FrontComposer effort is 9-14 engineer-days, including the closed policy/bootstrap path,
committed-object graph and bounded Builds-tree materialization, Governance and fixtures, authenticated
CI and Release verification handoffs, static transitive evaluator closure, release-evidence v2, and
focused/broad verification.
Implementation risk is high because these controls jointly guard publication. `BUILD-CAT-1` is an
additional upstream 1-2 engineer-day change and is not a blocker for removing historical exact-SHA
compatibility assertions.

## 3. Recommended Approach

Use **Option 1 - Direct Adjustment** with four coordinated controls.

### Control A: Validate Compatibility at the Actual Gitlinks

Enumerate every gitlink at the explicit FrontComposer commit as depth 1 and every direct gitlink in each
exact root-selected commit as depth 2. Record self/back-reference edges before deduplicating owner object
reads; edges below depth 2 require a separately approved schema. For each Builds selector, caching bytes
by distinct selected commit:

- read `Props/Directory.Packages.props` from that exact commit, not from an unrelated working-tree
  checkout;
- validate XML structure, central-package ownership, required import/marker semantics, and the
  module-specific required package/version contract;
- fail with the owner path, actual commit, and semantic mismatch;
- contain no historical Builds-commit or catalog-fingerprint compatibility allowlist in product
  Governance tests. Approved workflow/action provenance intentionally remains 40-hex pinned.

The catalog fingerprint is computed and reported for provenance. It is not compared to a hard-coded
allowlist as a substitute SHA.

### Control B: Make Pointer Changes Intentional and Reviewable

When an in-boundary gitlink changes, CI emits a deterministic dependency-graph diff using the ratified
PR/push revision model. Each affected target resolves through the immutable base/before policy to an
exact static Release/NuGet build or evidence-only disposition. Exact selected Builds contract trees are
bounded-materialized without nested initialization; root changes subsume descendant churn and every
affected module runs at most once.

That same active policy is the independent trust root for CI, Release, and post-release evaluators. It
pre-authorizes local caller blob hashes plus literal-40-hex reusable/action coordinates and raw metadata
hashes; recording or sealing an unapproved closure cannot authorize it. A standard-library static
closure follows every conditional `uses:` and composite descendant under fixed cycle/depth/source/blob
limits, independent of which runtime branch executed.

Unchanged graph edges do not trigger redundant standalone builds. A pointer-only change is acceptable
when catalog-contract validation and the affected module gate both pass.

### Control C: Seal Exact Provenance in Release Evidence

Extend `sealed-manifest.json` with a deterministic `dependency_graph` containing:

- FrontComposer root commit;
- every root gitlink edge;
- every direct gitlink edge from each exact root-selected commit (depth 2);
- normalized owner/path, commit SHA, and relationship depth;
- for Builds edges, catalog contract version when present and a SHA-256 catalog-content fingerprint.

Manifest v2 additionally seals the active dependency-policy coordinates, authenticated successful-CI
handoff, and immutable CI/release workflow definitions. Verification fails on missing/unknown/duplicate/
out-of-order/over-limit evidence, malformed hashes, graph/catalog drift, policy mismatch, mutable
workflow provenance, or handoff mismatch. Legacy manifests are audit-only and non-publishable. The
post-publication verifier preserves and validates the same sealed evidence.

Every Release attempt also emits an authenticated `if: always()` verification handoff carrying the
original CI candidate, Release run identity/conclusion, version/tag/release/manifest/assets, and
authorized Release evaluator. The post-release verifier uses that handoff—not its second-hop
`workflow_run.head_sha` or default-branch SHA—and cannot green-no-op failed or partial attempts.

### Control D: Introduce an Upstream Semantic Catalog Contract

Route `BUILD-CAT-1` to the Hexalith.Builds owner to add an explicit semantic catalog-contract version
and canonicalization rules. Use a major-compatible policy for consumers. During migration,
FrontComposer validates semantic catalog contents directly and records the computed fingerprint;
after all supported Builds gitlinks expose the marker, a separately approved change makes the marker
mandatory.

### Alternatives Considered

- **Keep exact SHA constants:** rejected. It is high-maintenance, fails on compatible pointer changes,
  covers only selected edges, and duplicates Git provenance poorly.
- **Remove submodule governance:** rejected. It permits incompatible catalogs and unreviewed graph
  drift.
- **Fingerprint allowlist as the compatibility gate:** rejected. An exact fingerprint allowlist merely
  replaces commit identity with content identity and retains the same mechanical-update failure mode.
- **Rollback current pointers:** rejected. It restores historical identities without proving that the
  older catalogs are the correct compatibility target.
- **MVP/PRD reduction:** not applicable. Product capability and user scope do not change.

## 4. Detailed Change Proposals

### 4.1 New Governance Story

**OLD**

```markdown
No story owns contract-based shared-catalog validation plus complete dependency-graph provenance.
```

**NEW**

```markdown
### GOV-1: Validate shared-catalog compatibility and seal dependency provenance

As a framework maintainer and Release Owner,
I want compatibility validated from the catalogs selected by actual gitlinks while exact identities
are sealed as provenance,
So that legitimate pointer advances remain reviewable and reproducible without false-red SHA pins.

**Given** the FrontComposer root and its root-declared modules,
**When** Governance collects the complete defined depth-1/2 v1 graph from exact committed objects,
**Then** it evaluates every Builds selector under its explicit semantic profile, caches exact bytes by
distinct commit, and contains no historical Builds-commit or catalog-fingerprint compatibility
allowlist. Approved workflow/action provenance intentionally remains 40-hex pinned.

**Given** a compatible catalog at a different commit,
**When** the focused Governance tests run,
**Then** compatibility passes and the changed commit appears only in the dependency-graph diff/evidence.

**Given** a catalog missing or changing a required package/import/marker contract,
**When** Governance runs,
**Then** it fails with the owning gitlink path, actual commit, and semantic mismatch.

**Given** a root or nested gitlink change,
**When** CI applies the ratified PR/push revision model and immutable base/before policy,
**Then** it emits the normalized graph diff and runs each affected module once through its exact static
Release/NuGet build or evidence-only disposition without recursive submodule initialization.

**Given** release candidates are prepared,
**When** the manifest is sealed and verified,
**Then** manifest v2 seals the complete defined depth-1/2 graph, Builds catalog provenance, active
policy, authenticated CI handoff, and immutable workflow definitions; invalid or drifted evidence fails
closed and legacy manifests remain non-publishable.

**Given** Hexalith.Builds has no catalog contract version,
**When** GOV-1 is handed off,
**Then** BUILD-CAT-1 is routed upstream; FrontComposer uses semantic content validation during the
migration and does not block on an exact fingerprint allowlist.
```

### 4.2 Governance Test Change

**OLD**

```csharp
const string eventStoreBuildsCommit = "c177c66af5d3f509328c2f568dc0737fe9f89e4e";
ReadGitlinkCommit(eventStoreRoot, "references/Hexalith.Builds")
    .ShouldBe(eventStoreBuildsCommit);
```

**NEW**

```csharp
GitlinkCatalog catalog = ReadCatalogAtGitlink(eventStoreRoot, "references/Hexalith.Builds");
AssertCompatibleSharedCatalog(catalog, EventStoreCatalogRequirements);
```

The implementation must read catalog bytes from `catalog.Commit`, include the commit in diagnostics,
deduplicate identical Builds commits, and include every current root-module Builds edge. Existing
package-ownership assertions remain semantic requirements; only historical commit equality is removed.

### 4.3 PRD Change

**OLD - §7 repository policy**

```markdown
- Repository policy: root-declared submodules under `references/` only; never recursive submodule
  initialization; never modify submodule files without explicit approval.
```

**NEW**

```markdown
- Repository policy: root-declared submodules under `references/` only; never recursive submodule
  initialization; never modify submodule files without explicit approval.
- Dependency governance: compatibility is established by the semantic catalog contract and affected
  module restore/build evidence, not by hard-coded historical gitlink identities. Every depth-1 root
  gitlink and every depth-2 gitlink in each exact root-selected commit is sealed as exact dependency
  provenance; deeper edges require a separately approved schema.
```

Add a corresponding NFR requirement: dependency pointer changes must be graph-diffed, contract-tested,
and release-manifest-bound without recursive initialization.

### 4.4 Architecture Change

**OLD**

```markdown
- External dependencies: root-declared `references/Hexalith.*` submodules only. Nested submodules are
  not initialized.
...
The sealed manifest identifies every immutable release candidate by normalized path and SHA-256 hash.
```

**NEW**

```markdown
- External dependency compatibility is evaluated from the catalog content selected by each actual
  gitlink. Commit identities are provenance, not compatibility allowlists.
- Pointer changes produce a normalized dependency-graph diff and affected-module restore/build proof.
- The sealed release manifest binds the FrontComposer commit, every edge in the complete defined
  depth-1/2 v1 graph, each selected Builds catalog contract version/content fingerprint, the active
  policy, authenticated CI handoff, and immutable workflow definitions in addition to artifact paths
  and SHA-256 hashes.
- Graph discovery reads exact committed Git objects, records in-boundary self/back-reference edges, and
  never recursively initializes nested submodules.
```

### 4.5 Release Manifest Change

**OLD**

```json
{
  "commit_sha": "<frontcomposer-sha>",
  "packages": []
}
```

**NEW**

```json
{
  "manifest_schema": "hexalith.release-evidence.v2",
  "commit_sha": "<frontcomposer-sha>",
  "dependency_graph": {
    "schema": "hexalith.dependency-graph.v1",
    "root": {
      "repository": "github.com/hexalith/hexalith.frontcomposer",
      "commit": "<40-hex-sha>"
    },
    "edge_count": 1,
    "edges": [
      {
        "owner_repository": "github.com/hexalith/hexalith.frontcomposer",
        "owner_commit": "<40-hex-sha>",
        "path": "references/Hexalith.Builds",
        "repository": "github.com/hexalith/hexalith.builds",
        "commit": "<40-hex-sha>",
        "depth": 1,
        "catalog_contract_version": null,
        "catalog_sha256": "<64-hex-sha256>"
      }
    ],
    "graph_digest": "<64-hex-sha256>"
  },
  "dependency_policy": {
    "schema": "hexalith.dependency-graph-policy.v1",
    "repository": "github.com/hexalith/hexalith.frontcomposer",
    "commit": "<40-hex-sha>",
    "sha256": "<64-hex-sha256>"
  },
  "workflow_provenance": {
    "ci": {
      "run": {
        "repository": "github.com/hexalith/hexalith.frontcomposer",
        "workflow_path": ".github/workflows/ci.yml",
        "run_id": 123,
        "head_sha": "<40-hex-sha>"
      },
      "evidence_sha256": "<64-hex-sha256>",
      "caller": {
        "repository": "github.com/hexalith/hexalith.frontcomposer",
        "workflow_path": ".github/workflows/ci.yml",
        "commit": "<40-hex-sha>",
        "blob_sha256": "<64-hex-sha256>"
      },
      "reusable": {
        "repository": "github.com/hexalith/hexalith.builds",
        "workflow_path": ".github/workflows/domain-ci.yml",
        "commit": "<40-hex-sha>",
        "blob_sha256": "<64-hex-sha256>"
      },
      "actions": []
    },
    "release": {
      "caller": {
        "repository": "github.com/hexalith/hexalith.frontcomposer",
        "workflow_path": ".github/workflows/release.yml",
        "commit": "<40-hex-sha>",
        "blob_sha256": "<64-hex-sha256>"
      },
      "reusable": {
        "repository": "github.com/hexalith/hexalith.builds",
        "workflow_path": ".github/workflows/domain-release.yml",
        "commit": "<40-hex-sha>",
        "blob_sha256": "<64-hex-sha256>"
      },
      "actions": []
    },
    "definition_digest": "<64-hex-sha256>"
  },
  "packages": []
}
```

Update manifest preparation, diagnostics, sealing, verification, fallback invalidation, fixtures,
governance pins, and post-publication verification as one atomic schema change. The fallback digest
binds the graph digest, active policy SHA-256, and canonical combined CI/release workflow-definition
digest. The example shows the closed member structure; production arrays enumerate every executed
action source. Do not store absolute paths or working-tree-only identities.

### 4.6 UX Change

**N/A.** No UI component, route, content, interaction, accessibility, visual, or localization change.

## 5. Implementation Handoff

### Scope Classification

**Moderate.** Product behavior is unchanged, but backlog registration and coordinated test, CI,
architecture, release-evidence, and upstream-contract work are required.

### Recipients and Responsibilities

- **Product Owner:** approve `GOV-1`, register it in sprint status, and keep Story 11.17d's scope
  separate while prioritizing removal of its false-red blocker.
- **Architect:** maintain the ratified bounded committed-object graph, canonical schemas, policy
  activation model, and semantic-version migration posture.
- **Developer:** implement exact-gitlink catalog loading, semantic validators, graph collection/diff,
  pointer-change gates, and focused tests.
- **Release Owner:** enforce manifest v2, both exact-candidate handoffs, active-policy evaluator trust,
  the release freeze while current seams are non-conforming, and route BUILD-CAT-1 plus the issue-17
  BUILD-REL-1 amendment upstream.
- **Hexalith.Builds owner:** define and publish the semantic catalog-contract version/canonicalization
  in BUILD-CAT-1 and deliver the issue-17 / BUILD-REL-1 exact-candidate, evaluator-closure, and handoff
  amendment at an owner-accepted immutable revision. FrontComposer must not edit the submodule in GOV-1.

### Sequencing

1. **Complete:** approve/register `GOV-1` and ratify the focused architecture/FC-DEP-1 decision.
2. Replace SHA assertions with exact-gitlink semantic catalog validation and make the focused
   Governance tests green.
3. Add dependency-graph collection/diff, policy-authorized static evaluator closure, and pointer-change gates.
4. Obtain and record the owner-accepted immutable BUILD-REL-1 issue-17 revision; Tasks 4/5, completion,
   release eligibility, and unfreeze remain blocked while it is pending.
5. Extend and verify manifest v2 plus the CI-to-Release and Release-to-verifier handoffs.
6. Route `BUILD-CAT-1`; later make its version marker mandatory through a separate approval after
   supported gitlinks migrate.
7. Rerun Story 11.17d's complete promotion lane on the exact candidate revision.

### Success Criteria

- Product Governance contains no expected 40-hex submodule SHA constants.
- Every current root-module Builds gitlink is semantically validated from the exact referenced commit.
- A different compatible commit passes; an incompatible catalog fails with actionable diagnostics.
- Pointer changes produce a reviewable graph diff and affected-module restore/build evidence.
- The sealed manifest and post-publication verifier bind the complete defined depth-1/2 v1 graph,
  active policy, authenticated CI handoff, independently authorized static workflow definitions, and
  the original-candidate Release verification handoff.
- No nested recursive submodule command is introduced.
- Story 11.17d's full Governance promotion lane passes on its exact revision.

## 6. Change Navigation Checklist

### 1. Understand Trigger and Context

- [x] 1.1 Trigger identified: Story 11.17d evidence/status review and the catalog-centralization CI
  repairs exposed recurring false-red SHA pins.
- [x] 1.2 Core problem defined: failed governance approach; compatibility and provenance are conflated.
- [x] 1.3 Evidence collected: focused `0/2` result, current gitlinks, compatible referenced catalogs,
  repeated constant edits, and release-manifest graph omission.

### 2. Epic Impact Assessment

- [x] 2.1 Existing epics remain completable.
- [x] 2.2 Add cross-cutting `GOV-1`; do not reopen completed product stories.
- [x] 2.3 No future epic is invalidated.
- [x] 2.4 No new product epic is required.
- [x] 2.5 Prioritize `GOV-1` before Story 11.17d promotion and the next governed release manifest.

### 3. Artifact Conflict and Impact Analysis

- [x] 3.1 PRD impact identified; MVP unchanged.
- [x] 3.2 Architecture and release-manifest changes identified.
- [N/A] 3.3 UX is unaffected.
- [x] 3.4 Governance tests, CI, release evidence, sprint status, and upstream Builds contract assessed.

### 4. Path Forward Evaluation

- [x] 4.1 Direct adjustment is viable; 9-14 engineer-days of FrontComposer work with high implementation risk at the publication authorization seam.
- [x] 4.2 Rollback is not viable; it restores identities without proving compatibility.
- [N/A] 4.3 MVP review is unnecessary.
- [x] 4.4 Recommended path selected: Direct Adjustment.

### 5. Proposal Components

- [x] 5.1 Issue summary complete.
- [x] 5.2 Epic/story and artifact impacts complete.
- [x] 5.3 Recommended path and alternatives complete.
- [x] 5.4 MVP impact and action sequence complete.
- [x] 5.5 Handoff roles and responsibilities defined.

### 6. Final Review and Handoff

- [x] 6.1 Applicable checklist sections addressed.
- [x] 6.2 Proposal checked for consistency with repository evidence and safeguards.
- [x] 6.3 Administrator explicitly approved the proposal on 2026-07-19.
- [x] 6.4 PRD, architecture, epics, decision contract, and sprint status were updated.
- [x] 6.5 Moderate-scope handoff is routed to Product Owner, Architect, Developer, and Release Owner.

## 7. Workflow Execution Log

- Approval received: 2026-07-19, Administrator (`yes`).
- Change scope: Moderate.
- Canonical artifacts updated: PRD, architecture, epics, sprint status, and decision contract.
- Story registration: `GOV-1` is `ready-for-dev`; its architecture entry gate was ratified and
  production implementation has not started.
- Handoff: Product Owner / Architect / Developer / Release Owner.
- Upstream boundary: BUILD-CAT-1 belongs to Hexalith.Builds; no submodule content change is authorized
  by this workflow.
