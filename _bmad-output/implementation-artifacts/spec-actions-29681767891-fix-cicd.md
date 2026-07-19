---
title: 'Fix shared catalog line endings and stale governance gitlinks'
type: 'bugfix'
created: '2026-07-19'
status: 'done'
review_loop_iteration: 0
baseline_commit: 'cbe3aee08d5d1a390202c46aefe67bb3fcf55c4b'
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-centralize-package-versions-in-hexalith-builds.md'
  - '{project-root}/references/Hexalith.Builds/AGENTS.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** GitHub Actions run 29681767891 and the subsequent current-main run fail Quality Gate 2b because the pinned Hexalith.Builds shared package catalog has 18 bare LF endings in an otherwise UTF-8-BOM/CRLF file. Repairing that first assertion exposes a stale Memories nested-Builds expectation, while advancing the root Builds gitlink would incorrectly force the independently pinned Parties gitlink to match it.

**Approach:** Normalize and Git-enforce the catalog format in its owning Hexalith.Builds repository, publish an isolated upstream fix, advance only FrontComposer's root Builds gitlink, and reconcile the governance assertions with the already-pinned independent nested commits. Preserve the encoding guard and package-version semantics.

## Boundaries & Constraints

**Always:** Keep the catalog XML, BOM, package versions, ordering, and comments semantically unchanged; use an exact-path Builds-owned Git attribute; make the upstream fix reachable before recording its FrontComposer gitlink; keep EventStore, Memories, and Parties nested submodules untouched; retain `AssertUtf8BomAndCrLf` as a fail-closed contract.

**Ask First:** Any package-version or catalog-content change, nested-submodule update, broader file renormalization, force push, or CI/test-policy change beyond reconciling the inspected gitlink constants.

**Never:** Mask the failure in `.github/workflows/quality.yml`, relax/remove the BOM/CRLF assertion, rely on FrontComposer's root `.gitattributes` across a submodule boundary, normalize only the first failing line, or initialize/update nested submodules.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Fresh CI checkout | FrontComposer pins the published Builds fix | Catalog checks out as UTF-8 BOM with CRLF only; Gate 2b continues past encoding | Fail if any bare LF/CR or missing BOM remains |
| Independent consumers | Root Builds advances; EventStore, Memories, and Parties retain compatible nested pins | Governance validates each inspected gitlink against its own expected commit | Do not update nested submodules merely to equal the root pin |
| Upstream publication unavailable | New Builds commit is not reachable from its remote | FrontComposer does not record an unfetchable gitlink | Halt and report the publication blocker |

</frozen-after-approval>

## Code Map

- `references/Hexalith.Builds/Props/Directory.Packages.props` -- authoritative catalog; baseline blob `c177c66a` has a BOM, 299 CRLF endings, and 18 bare LFs introduced by Builds commit `96c83fc2`.
- `references/Hexalith.Builds/.gitattributes` -- new exact-path checkout enforcement owned by the Builds repository.
- `references/Hexalith.Builds` -- FrontComposer root gitlink, advanced from baseline `c177c66a` to reachable repair `deb76e98`.
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/InfrastructureGovernanceTests.cs` -- encoding contract and independently pinned EventStore/Memories/Parties Builds assertions.
- `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json` -- fail-closed test identifier hash refreshed after review-added attribute coverage.

## Tasks & Acceptance

**Execution:**
- [x] `references/Hexalith.Builds/.gitattributes` -- add an exact `Props/Directory.Packages.props text eol=crlf` rule -- prevent future patch-added LF lines without renormalizing unrelated Builds files.
- [x] `references/Hexalith.Builds/Props/Directory.Packages.props` -- mechanically normalize the complete file to UTF-8 BOM/CRLF -- remove all 18 defects without changing XML content.
- [x] `references/Hexalith.Builds` -- validate, commit, and publish the isolated two-file upstream repair, then advance the FrontComposer root gitlink -- make the fix reproducible by CI.
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Governance/InfrastructureGovernanceTests.cs` -- reconcile named root/Memories/Parties pin expectations and assert that Git resolves `text/eol=crlf` only for the catalog -- expose independent compatibility and prevent a broadened checkout policy.
- [x] `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json` -- refresh the deterministic test-source hash after review-added governance coverage -- keep analyzer policy fail-closed.

**Acceptance Criteria:**
- Given a fresh checkout of the repaired Builds commit, when the shared catalog bytes are scanned, then they start with the UTF-8 BOM and every newline is CRLF.
- Given current root-declared submodules, when the focused central-catalog and Parties package governance tests run, then the root, EventStore, Memories, and Parties Builds gitlinks match their explicit inspected expectations without a nested update.
- Given the Release solution build, when the complete Governance lane runs with `DiffEngine_Disabled=true`, then Gate 2b passes with no catalog, package-authority, or gitlink failure.

## Spec Change Log

## Design Notes

Git attributes do not cross submodule boundaries. The root repository's CRLF rule therefore cannot repair a Builds-owned file; the preventive rule and normalization must land together in Hexalith.Builds. The attribute enforces checkout line endings only; the existing byte assertion remains responsible for the BOM and CRLF-only contract. The catalog's semantic XML must compare equal before and after normalization.

## Verification

**Commands:**
- `pwsh -NoProfile -File ./references/Hexalith.Builds/Tools/validate-central-package-versions.ps1`, `pwsh -NoProfile -File ./references/Hexalith.Builds/Tools/test-authoritative-package-catalog.ps1`, and `pwsh -NoProfile -File ./references/Hexalith.Builds/Tools/test-central-package-version-validator.ps1` -- expected: all shared-catalog validators pass.
- `git -C references/Hexalith.Builds ls-files --eol -- Props/Directory.Packages.props` plus a byte scan -- expected: checkout reports CRLF and zero bare newline bytes while retaining the BOM.
- `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --no-restore` -- expected: rebuild the focused test assembly with zero warnings/errors before direct invocation.
- `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -method Hexalith.FrontComposer.Shell.Tests.Governance.InfrastructureGovernanceTests.CentralPackageVersions_WhenCatalogIsMigrated_AreOwnedBySharedCatalog -method Hexalith.FrontComposer.Shell.Tests.Governance.InfrastructureGovernanceTests.PartiesPackageVersions_WhenCatalogIsCentralized_AreInheritedFromPinnedBuilds` -- expected: both changed pin/attribute paths pass.
- `git -C references/Hexalith.Builds merge-base --is-ancestor $(git ls-files --stage -- references/Hexalith.Builds | awk '{print $2}') origin/main` plus the same command with a known-unavailable SHA -- expected: accept the recorded reachable commit and fail closed for the unavailable commit.
- `dotnet build Hexalith.FrontComposer.slnx --configuration Release` then `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --configuration Release --no-build --filter "Category=Governance"` -- expected: Release build and the exact Gate 2b test command pass.

**Observed 2026-07-19:** Builds commit `deb76e983434335c990b0a1f676b8887d643a274` contains only the exact attribute and catalog normalization, is reachable from Builds `origin/main`, retains the BOM, and has 317 CRLF with no bare newline bytes. Normalizing CRLF/CR to LF before `sha256sum` produced `8ce9d0df7b80080c350dacd1bb0aa1ddfceac01be040ddc047bb6ad0a17438e9` for both parent and repaired catalog blobs. All three Builds validators passed. A temporary FrontComposer worktree materialized at every recorded root submodule gitlink built Release with zero warnings/errors; its Release Governance lane passed (Shell 188/188 plus all other participating suites). The reachability guard accepted the recorded Builds SHA and rejected an unavailable SHA. The live shared worktree's same lane passed 187/188 Shell tests and failed only because concurrent, unstaged EventStore work points at a different nested Builds gitlink; the isolated recorded-gitlink run is authoritative for this change. Review-added attribute coverage retained 6,195 underscore identifier tokens and refreshed the fail-closed SHA-256 to `98fe2ade9302f725a22d8263148f412b8539dd568cfa2101bd8d473f675eabce`.

**Known external deviation:** Another process created and pushed the intended isolated Builds commit while the implementation agent had its two files staged. Its subject, `Refactor code structure for improved readability and maintainability`, fails commitlint with `type-empty` and `subject-empty`. This workflow did not amend, rewrite, force-push, or otherwise mutate the published history.

## Suggested Review Order

**Catalog checkout boundary**

- Start here: one test binds exact Git attributes, bytes, versions, and consumer pins.
  [`InfrastructureGovernanceTests.cs:46`](../../tests/Hexalith.FrontComposer.Shell.Tests/Governance/InfrastructureGovernanceTests.cs#L46)

- Builds owns the checkout conversion at the submodule boundary.
  [`.gitattributes:1`](../../references/Hexalith.Builds/.gitattributes#L1)

- The catalog changes only in newline representation; semantic XML is preserved.
  [`Directory.Packages.props:1`](../../references/Hexalith.Builds/Props/Directory.Packages.props#L1)

**Independent pin model**

- EventStore and Memories retain independently compatible nested Builds commits.
  [`InfrastructureGovernanceTests.cs:121`](../../tests/Hexalith.FrontComposer.Shell.Tests/Governance/InfrastructureGovernanceTests.cs#L121)

- Root and Parties pins are explicit instead of falsely forced equal.
  [`InfrastructureGovernanceTests.cs:203`](../../tests/Hexalith.FrontComposer.Shell.Tests/Governance/InfrastructureGovernanceTests.cs#L203)

**Review safeguards**

- The helper observes Git's resolved attributes instead of parsing patterns heuristically.
  [`InfrastructureGovernanceTests.cs:668`](../../tests/Hexalith.FrontComposer.Shell.Tests/Governance/InfrastructureGovernanceTests.cs#L668)

- Fail-closed identifier evidence tracks the reviewed governance source.
  [`analyzer-policy-exception-ledger-v1.json:75`](../contracts/analyzer-policy-exception-ledger-v1.json#L75)

- External history and dirty-worktree identity concerns remain deliberately deferred.
  [`deferred-work.md:1811`](deferred-work.md#L1811)
