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
record the complete release dependency graph.

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

Estimated FrontComposer effort is 3-5 engineer-days, including Governance, release-evidence, CI, and
focused verification. `BUILD-CAT-1` is an additional upstream 1-2 engineer-day change and is not a
blocker for the immediate removal of exact-SHA compatibility assertions.

## 3. Recommended Approach

Use **Option 1 - Direct Adjustment** with four coordinated controls.

### Control A: Validate Compatibility at the Actual Gitlinks

Enumerate the current root `Hexalith.Builds` gitlink and every reachable nested Builds gitlink by
walking committed Git trees from the root-declared modules. Key traversal by repository identity plus
commit and stop on visited nodes so cyclic module references cannot recurse forever. For each distinct
Builds commit:

- read `Props/Directory.Packages.props` from that exact commit, not from an unrelated working-tree
  checkout;
- validate XML structure, central-package ownership, required import/marker semantics, and the
  module-specific required package/version contract;
- fail with the owner path, actual commit, and semantic mismatch;
- contain no expected 40-hex commit literals in product Governance tests.

The catalog fingerprint is computed and reported for provenance. It is not compared to a hard-coded
allowlist as a substitute SHA.

### Control B: Make Pointer Changes Intentional and Reviewable

When a root or nested gitlink changes, CI emits a deterministic base-to-head dependency-graph diff.
For each affected module it runs the narrowest supported standalone restore/build using the exact
selected catalog. Targeted explicit checkouts may be used; recursive submodule initialization remains
forbidden.

Unchanged graph edges do not trigger redundant standalone builds. A pointer-only change is acceptable
when catalog-contract validation and the affected module gate both pass.

### Control C: Seal Exact Provenance in Release Evidence

Extend `sealed-manifest.json` with a deterministic `dependency_graph` containing:

- FrontComposer root commit;
- every root gitlink edge;
- every reachable nested gitlink edge discoverable from committed Git trees;
- normalized owner/path, commit SHA, and relationship depth;
- for Builds edges, catalog contract version when present and a SHA-256 catalog-content fingerprint.

Manifest verification fails on a missing edge, malformed SHA, duplicate normalized edge, graph drift,
or catalog fingerprint mismatch. The post-publication verifier preserves and validates the same sealed
graph. Graph collection must detect cycles and must not recurse through submodule initialization.

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
**When** Governance enumerates root and reachable nested Builds gitlinks,
**Then** it loads the catalog from every distinct actual commit and validates the applicable semantic
package/build contract without any expected 40-hex SHA literal.

**Given** a compatible catalog at a different commit,
**When** the focused Governance tests run,
**Then** compatibility passes and the changed commit appears only in the dependency-graph diff/evidence.

**Given** a catalog missing or changing a required package/import/marker contract,
**When** Governance runs,
**Then** it fails with the owning gitlink path, actual commit, and semantic mismatch.

**Given** a root or nested gitlink change,
**When** CI compares the merge base to the candidate head,
**Then** it emits the normalized graph diff and runs the affected module's supported standalone
restore/build gate without recursive submodule initialization.

**Given** release candidates are prepared,
**When** the manifest is sealed and verified,
**Then** the complete reachable dependency graph and Builds catalog provenance are inside the
seal and any missing, duplicate, malformed, or drifted edge fails closed.

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
  module restore/build evidence, not by hard-coded historical gitlink identities. Every root and direct
  nested gitlink identity used for a release is sealed as exact dependency provenance.
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
- The sealed release manifest binds the FrontComposer commit, every reachable root and nested gitlink
  edge, and each selected Builds catalog contract version/content fingerprint in addition to artifact
  paths and SHA-256 hashes.
- Graph discovery reads committed Git trees, detects cycles, and never recursively initializes nested
  submodules.
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
  "commit_sha": "<frontcomposer-sha>",
  "dependency_graph": {
    "schema": "hexalith.dependency-graph.v1",
    "edges": [
      {
        "owner": ".",
        "path": "references/Hexalith.Builds",
        "commit": "<40-hex-sha>",
        "depth": 1,
        "catalog_contract_version": "<semantic-version-or-migration-marker>",
        "catalog_sha256": "<64-hex-sha256>"
      }
    ]
  },
  "packages": []
}
```

Update manifest preparation, diagnostics, sealing, verification, fallback invalidation, fixtures,
governance pins, and post-publication verification as one atomic schema change. Do not store absolute
paths or working-tree-only identities.

### 4.6 UX Change

**N/A.** No UI component, route, content, interaction, accessibility, visual, or localization change.

## 5. Implementation Handoff

### Scope Classification

**Moderate.** Product behavior is unchanged, but backlog registration and coordinated test, CI,
architecture, release-evidence, and upstream-contract work are required.

### Recipients and Responsibilities

- **Product Owner:** approve `GOV-1`, register it in sprint status, and keep Story 11.17d's scope
  separate while prioritizing removal of its false-red blocker.
- **Architect:** approve the compatibility/provenance split, complete reachable graph boundary, cycle
  handling, and semantic-version migration posture.
- **Developer:** implement exact-gitlink catalog loading, semantic validators, graph collection/diff,
  pointer-change gates, and focused tests.
- **Release Owner:** approve the manifest schema change, require graph verification in pre- and
  post-publication paths, and route `BUILD-CAT-1` upstream.
- **Hexalith.Builds owner:** define and publish the semantic catalog-contract version/canonicalization
  in a separate upstream change. FrontComposer must not edit the submodule in `GOV-1`.

### Sequencing

1. Approve and register `GOV-1`; record the architecture decision.
2. Replace SHA assertions with exact-gitlink semantic catalog validation and make the focused
   Governance tests green.
3. Add dependency-graph collection/diff and pointer-change affected-module CI gates.
4. Extend and verify the sealed manifest and post-publication evidence.
5. Route `BUILD-CAT-1`; later make its version marker mandatory through a separate approval after
   supported gitlinks migrate.
6. Rerun Story 11.17d's complete promotion lane on the exact candidate revision.

### Success Criteria

- Product Governance contains no expected 40-hex submodule SHA constants.
- Every current root-module Builds gitlink is semantically validated from the exact referenced commit.
- A different compatible commit passes; an incompatible catalog fails with actionable diagnostics.
- Pointer changes produce a reviewable graph diff and affected-module restore/build evidence.
- The sealed manifest and post-publication verifier bind the complete defined gitlink graph.
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

- [x] 4.1 Direct adjustment is viable; medium effort, low-to-medium implementation risk.
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
- Backlog registration: `GOV-1` added as `backlog`; production implementation has not started.
- Handoff: Product Owner / Architect / Developer / Release Owner.
- Upstream boundary: BUILD-CAT-1 belongs to Hexalith.Builds; no submodule content change is authorized
  by this workflow.
