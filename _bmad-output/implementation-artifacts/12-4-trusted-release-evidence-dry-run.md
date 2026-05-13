# Story 12.4: Trusted Release Evidence Dry Run

Status: ready-for-dev

> **Epic 12** - Release Certification and Evidence Alignment. This story proves the release-evidence path in a trusted context before v1 publication. It applies lessons **L06**, **L07**, **L08**, and **L10**.

---

## Executive Summary

Story 10.6 added the release-evidence contract for package inventory, symbols, SBOM, checksums, signing, attestation fallback, sealed manifest, and release budget evidence. Story 11.7 then tightened CI/release ordering and credentialed side-effect risk. The remaining release-certification gap is not more release prose: it is a trusted-context dry run that proves the current `.github/workflows/release.yml`, `.releaserc.json`, and `eng/release_evidence.py` path cannot produce a false release-ready record.

Story 12.4 is the narrow release-owner proof pass. It must run or simulate the release path in a trusted main/release context without irreversible publish/tag/release mutation, capture bounded evidence, and record any unavailable signing, attestation, timestamp, NuGet, GitHub Release, or publication path as either a release blocker or an explicitly approved fallback.

---

## Story

As a maintainer,
I want release evidence proven in a trusted context,
so that signing, SBOM, checksums, symbols, attestations, package inventory, and publication ordering cannot produce a false release-ready record.

### Release-Readiness Job To Preserve

A release owner should be able to inspect Story 12.4 evidence and know whether the v1 release workflow is ready to publish, blocked by a missing trusted path, or ready only with named and approved fallbacks.

---

## Dev Agent Cheat Sheet

| Area | Required outcome |
| --- | --- |
| Primary release files | `.github/workflows/release.yml`, `.releaserc.json`, `eng/release_evidence.py`, and `eng/release-package-inventory.json`. |
| Current workflow state | Release runs on `main` push or `workflow_dispatch`, checks out root-level submodules only, runs release tests, generates inventory/SBOM/checksums/signing verification, seals a manifest, publishes through semantic-release, and records release-budget evidence. |
| Trusted-context proof | Dry run must happen from trusted `main`/protected release context or a deliberately equivalent local proof that cannot exercise credentialed writes. PR/fork/untrusted contexts are read-only candidate evidence. |
| No irreversible side effects | Do not create tags, push changelog commits, publish NuGet packages, create GitHub Releases, upload attestations, or mutate external state during the dry run unless the step is explicitly approved as part of a real release. |
| Blocking checks before publish | Package inventory, blocking tests, SBOM, symbol package inventory, checksums, signing verification, attestation/approved fallback, redaction scan, sealed manifest verification, and rerun/partial-publish policy must pass before any publish/tag/release mutation. |
| External paths | Missing certificate, NuGet key, timestamp authority, GitHub attestation support, GitHub Release permission, or NuGet publish path becomes a release blocker unless an approved fallback has owner, evidence, release-note impact, expiry/revalidation trigger, and reopen event. |
| Evidence safety | Evidence must be bounded and sanitized: no certificate material, passwords, API keys, OIDC tokens, NuGet keys, tenant/user values, command payloads, local absolute paths, raw response bodies, or unbounded workflow logs. |
| Current package inventory | Expected packable packages are `Hexalith.FrontComposer.Cli`, `Contracts`, `Mcp`, `Schema`, `Shell`, and `Testing`; `SourceTools` is intentionally non-packable with an exception. |
| Scope guardrail | Do not redesign semantic-release, package topology, CI lane strategy, MCP, EventStore provider behavior, accessibility evidence, docs-site generation, or submodule strategy unless the dry run proves a named release blocker. |
| Validation | Run release evidence helpers and static workflow checks; run broader tests only if source/workflow changes require them. |

Start here: T1 snapshot release workflow and package inventory -> T2 prove no side effects before checks -> T3 execute trusted dry-run or equivalent local candidate proof -> T4 verify manifest/signing/SBOM/checksum/evidence contracts -> T5 record blockers/fallbacks -> T6 update release-readiness notes and Dev Agent Record.

---

## Acceptance Criteria

| AC | Given | When | Then |
| --- | --- | --- | --- |
| AC1 | `.github/workflows/release.yml`, `.releaserc.json`, and `eng/release_evidence.py` define release behavior | Story 12.4 starts | The implementer records the current release graph, publish-capable steps, required secrets/vars, permissions, package inventory, and side-effect boundaries before edits. |
| AC2 | The release workflow can run on `main` or `workflow_dispatch` | A dry run is planned | The plan distinguishes trusted write-capable context from PR/fork/local candidate evidence and names which paths cannot be proven without a real release context. |
| AC3 | Release tests run before package publication | Workflow evidence is inspected | A release-ready result is invalid if tests are skipped, filtered to zero, missing TRX evidence, or allowed to pass after publish/sign/tag/release mutation. |
| AC4 | Package inventory is generated | The dry run executes | Inventory matches `eng/release-package-inventory.json`, unexpected packable projects fail closed, and per-project `Version` drift is rejected. |
| AC5 | Package output is built | Evidence is recorded | Every packable package has a lockstep version, expected `.nupkg`, required `.snupkg`, package id, project path, commit SHA, and symbol exception status. |
| AC6 | SBOM generation runs | Evidence is recorded | A concrete SBOM file and SHA-256 hash are present before manifest sealing; partial or missing SBOM evidence cannot be treated as complete. |
| AC7 | NuGet package signing runs | Evidence is recorded | Signed `.nupkg` artifacts are verified with timestamp evidence before publish; unsigned packages, missing timestamp, invalid certificate, or missing signing verification blocks release. |
| AC8 | Credential, certificate, timestamp, NuGet, or GitHub Release path is unavailable | The dry run completes | The missing path is recorded as a release blocker or approved fallback with owner, likelihood, impact, release risk, evidence, expiry/revalidation trigger, and reopen event. |
| AC9 | GitHub artifact attestations are supported | Release evidence is prepared | Attestations are generated and verified before release readiness is claimed, with the required least-privilege permissions documented. |
| AC10 | GitHub artifact attestations are unavailable or deliberately unsupported | Release evidence is prepared | The approved fallback explains why attestation is unavailable and keeps checksums, NuGet signatures, SBOM, commit SHA, tag, run id, and workflow ref as blocking provenance evidence. |
| AC11 | Checksums are generated | The manifest is prepared | Checksums cover signed `.nupkg`, required `.snupkg`, SBOM, signing verification, and fallback note artifacts without placeholder hashes. |
| AC12 | The sealed manifest is created | Verification runs | Manifest rows bind package id, version, commit SHA, artifact path, checksum, symbol artifact or exception, SBOM component, signing status, attestation status, publish status, tag, run id, workflow ref, and seal hash. |
| AC13 | A manifest field is missing, placeholder, stale, or points to unsigned artifacts | Verification runs | Release publication fails before any irreversible mutation and records bounded diagnostics. |
| AC14 | Release publication partially succeeds or a rerun is attempted | Release governance evaluates state | The workflow records a partial-publish incident or blocks rerun; it never silently rebuilds different artifacts under the same release evidence. |
| AC15 | Evidence paths are supplied by tool output or workflow metadata | Paths are normalized | Paths must stay under the approved evidence root, reject traversal and absolute paths, and publish logical artifact names rather than local filesystem paths. |
| AC16 | Release evidence includes tool output, package metadata, markdown, or diagnostics | Evidence is committed or attached | Output is escaped, bounded, and scanned for workflow-command injection, markdown control text, secrets, tokens, local paths, tenant/user values, raw response bodies, and unbounded logs. |
| AC17 | Workflow permissions are reviewed | The dry run is classified | Read-only jobs remain `contents: read`; write scopes are limited to release/tag, package, OIDC, and attestation steps that need them. |
| AC18 | The workflow checks out submodules | Release validation runs | Checkout remains root-level only (`submodules: true`) and no recursive nested submodule initialization/update is introduced. |
| AC19 | Release budget evidence is generated | The dry run or real release completes | Release minutes, tag-to-nuget latency, package count, run id, publish status, and package-count-collapse trigger state are recorded or explicitly marked unavailable. |
| AC20 | The dry run completes | Story evidence is updated | Story 12.4 records whether release evidence is ready, blocked, or ready with approved fallback, and names all residual release gates. |
| AC21 | Story 12.4 closes | Validation runs | Release evidence helper commands, manifest checks, status-artifact consistency, and `git diff --check` outcomes are recorded; source/workflow tests are run if touched. |

---

## Tasks / Subtasks

- [ ] T1. Capture current release graph and trusted-context assumptions (AC1, AC2, AC17, AC18)
  - [ ] Review `.github/workflows/release.yml` from checkout through release budget monitor.
  - [ ] Review `.releaserc.json` prepare and publish commands for build, pack, SBOM, signing, checksum, manifest, NuGet push, and GitHub Release asset ordering.
  - [ ] Review `eng/release_evidence.py` command contracts: `inventory`, `checksums`, `prepare-manifest`, `seal-manifest`, `verify-manifest`, `release-budget`, and `path-check`.
  - [ ] Record required secrets/vars: `GITHUB_TOKEN`, `NUGET_API_KEY`, `NUGET_SIGNING_CERTIFICATE_PATH`, `NUGET_SIGNING_CERTIFICATE_PASSWORD`, `NUGET_SIGNING_TIMESTAMPER`, and `ATTESTATION_UNSUPPORTED`.
  - [ ] Confirm root-level submodule checkout only; do not add recursive checkout or `git submodule update --init --recursive`.

- [ ] T2. Prove ordering before irreversible side effects (AC3, AC13, AC14)
  - [ ] Build a step-order table that names the first irreversible operation and every blocking check that must precede it.
  - [ ] Verify blocking tests, package inventory, SBOM, signing verification, checksums, manifest sealing, manifest verification, attestation/fallback state, and redaction checks complete before NuGet push, tag/changelog push, GitHub Release creation, or attestation upload.
  - [ ] Record partial-publish/rerun behavior and manual reconciliation requirements for package, tag, changelog, GitHub Release, and attestation drift.

- [ ] T3. Execute dry-run evidence generation without publish mutation (AC4-AC7, AC11, AC12, AC15)
  - [ ] Run package inventory:

```powershell
python eng\release_evidence.py inventory --root . --expected eng\release-package-inventory.json --output artifacts\release\story-12-4-package-inventory.json
```

  - [ ] If local pack/SBOM/signing cannot run safely because credentials are absent, record the unavailable path as candidate evidence and do not fake success.
  - [ ] Generate a candidate checksum/manifest fixture only from concrete local artifacts under an approved evidence root; never use placeholder hashes as passing evidence.
  - [ ] Run `verify-manifest` against the candidate manifest or record why no valid manifest can be produced outside a trusted release run.

- [ ] T4. Resolve attestation, signing, and external dependency states (AC8-AC10, AC16)
  - [ ] Determine whether GitHub artifact attestations are supported in the repository context; if supported, identify the required pre-publish attestation and verification step.
  - [ ] If `ATTESTATION_UNSUPPORTED=true` remains the path, record the owner, approval evidence, expiry/revalidation trigger, and whether release notes must mention checksum/signature/SBOM provenance without attestations.
  - [ ] Classify timestamp-authority, NuGet, GitHub Release, SBOM metadata, and signing-certificate unavailability as fail-closed blockers or approved fallbacks.
  - [ ] Run or document a redaction scan over generated release evidence.

- [ ] T5. Update release-readiness artifacts (AC19, AC20)
  - [ ] Update this story's Dev Agent Record with command outcomes, changed files, final release classification, blockers, approved fallbacks, and residual risks.
  - [ ] Update `deferred-work.md` only if Story 12.4 closes or routes current release-evidence rows.
  - [ ] Add release-readiness notes only under bounded repository artifacts; do not attach raw workflow logs.
  - [ ] Keep Story 12.5 accessibility/stakeholder evidence and Story 12.3 provider behavior outside this story.

- [ ] T6. Validate completion (AC16, AC21)
  - [ ] Run status-artifact consistency.
  - [ ] Run `git diff --check`.
  - [ ] Run focused release governance tests if workflow/helper source changed.
  - [ ] Run broader main-lane tests only if production code, package metadata, or workflow ordering changed beyond evidence docs.

---

## Critical Decisions

| ID | Decision | Rationale |
| --- | --- | --- |
| D1 | A successful story-status or release-workflow run is not release evidence unless required artifacts are concrete, bounded, and verified before publish. | Story 10.6 and Story 11.7 both identified false release-ready records as the main risk. |
| D2 | Trusted write-capable context and local/PR candidate evidence are different classes of proof. | Credentials, OIDC attestations, protected refs, and publication permissions cannot be honestly proven from untrusted branches. |
| D3 | Publish/tag/release mutation must be the last phase after blocking checks. | Package inventory, SBOM, signatures, checksums, manifest, tests, and redaction must gate irreversible external side effects. |
| D4 | Attestation unsupported is an approved fallback, not a silent pass. | GitHub attestation support depends on repository plan/visibility/permissions and must be documented when unavailable. |
| D5 | The sealed manifest is the release truth source. | It binds package ids, versions, checksums, SBOM, signatures, attestation state, commit, tag, run id, and workflow ref into one auditable record. |
| D6 | Evidence is hostile input. | Tool output, package metadata, markdown, and diagnostics can leak secrets or inject misleading workflow summaries unless escaped and bounded. |
| D7 | Missing signing, timestamp, NuGet, GitHub Release, or attestation paths are release blockers unless explicitly accepted. | A dry run that cannot prove the external path must not imply release readiness. |
| D8 | No recursive nested submodule commands are needed. | Release checkout must honor the root-level submodule policy already encoded in project rules. |

---

## Source Tree Components To Touch

| Path | Action | Notes |
| --- | --- | --- |
| `.github/workflows/release.yml` | Update possible | Only if ordering, permissions, redaction, attestation, submodule, or dry-run behavior is proven unsafe. |
| `.releaserc.json` | Update possible | Semantic-release prepare/publish command ordering, assets, or manifest verification. |
| `eng/release_evidence.py` | Update possible | Release evidence validation, manifest, checksum, path, budget, or redaction helper behavior. |
| `eng/release-package-inventory.json` | Update possible | Only if package inventory drift is deliberate and release-owner approved. |
| `_bmad-output/implementation-artifacts/deferred-work.md` | Update possible | Final release-evidence row state or accepted fallback metadata if Story 12.4 closes release rows. |
| `_bmad-output/implementation-artifacts/12-4-trusted-release-evidence-dry-run.md` | Update | Dev Agent Record, validation evidence, blockers/fallbacks, changed files, and completion notes. |
| `artifacts/release/**` | Create/update possible | Bounded dry-run evidence. Do not commit secrets, raw logs, or unbounded outputs. |

No unrelated MCP, EventStore provider, accessibility, stakeholder acceptance, docs-site generation, package topology redesign, or nested submodule changes should be made by default.

---

## Project Structure Notes

- Release workflow lives in `.github/workflows/release.yml`; release orchestration is split between workflow steps and `.releaserc.json`.
- Release evidence helpers live in `eng/release_evidence.py`; expected package inventory lives in `eng/release-package-inventory.json`.
- Current release evidence output root is `release-evidence` in workflow context; story dry-run evidence may use `artifacts/release` if committed or referenced.
- Use repository-relative paths in evidence and story records. Do not paste local absolute paths, raw workflow logs, certificate paths, secrets, tokens, tenant/user values, command payloads, or raw HTTP/API responses.
- Root-level submodules are `Hexalith.EventStore` and `Hexalith.Tenants`; do not initialize or update nested submodules.

---

## Testing Strategy

- Start with static release graph review and `eng/release_evidence.py inventory`.
- Exercise `checksums`, `prepare-manifest`, `seal-manifest`, `verify-manifest`, and `path-check` against controlled fixture artifacts if real package/sign/SBOM artifacts are unavailable.
- Add focused tests or helper fixtures only if a helper defect is found. Do not broaden into a release-system rewrite.
- Run workflow lint/static inspection when workflow edits occur.
- Run `git diff --check` and status-artifact consistency before review.

---

## Cross-Story Contract Table

| Producer | Consumer | Contract |
| --- | --- | --- |
| Story 10.6 | Story 12.4 | Release evidence contract: package inventory, SBOM, signatures, checksums, symbols, attestation/fallback, sealed manifest, release budget, and redaction. |
| Story 11.7 | Story 12.4 | CI/release ordering and irreversible side-effect governance must be proven, not inferred. |
| `.github/workflows/release.yml` | Release owner | Workflow permissions, test ordering, attestation fallback, semantic-release, and release-budget evidence define the trusted release path. |
| `.releaserc.json` | Release owner | Prepare/publish commands define exact artifact creation, signing, verification, NuGet push, and GitHub Release asset behavior. |
| `eng/release_evidence.py` | Release workflow and story dry run | Helper commands are the executable oracle for inventory, paths, checksums, manifest verification, and release budget evidence. |
| Story 12.4 | Story 12.5 | Trusted release evidence does not satisfy manual accessibility or stakeholder acceptance evidence. |

---

## Known Gaps / Follow-Ups

| Gap | Owner |
| --- | --- |
| Manual accessibility, localization/RTL/AT, real-device, and stakeholder acceptance evidence. | Story 12.5 |
| Provider-backed pending-command status readiness. | Story 12.3 |
| Package-count collapse evaluation if three consecutive release budget breaches occur. | Release owner after Story 12.4 evidence |
| Real attestation implementation if approved fallback expires or GitHub support becomes available. | Release owner / CI governance follow-up |
| Release dashboard that rolls Pact, mutation, accessibility, quarantine, and release evidence into one view. | Future release dashboard/process story |

---

## References

- [Source: `_bmad-output/planning-artifacts/epics/epic-12-release-certification-evidence-alignment.md#Story-12.4`] - story statement, acceptance criteria, and Epic 12 scope.
- [Source: `_bmad-output/implementation-artifacts/10-6-llm-benchmark-signed-releases-and-sbom.md`] - release evidence contract, sealed manifest, SBOM, signing, attestation fallback, budget, and redaction hardening.
- [Source: `_bmad-output/implementation-artifacts/11-7-eventstore-reliability-and-ci-governance-follow-ups.md`] - release ordering, trusted context, and irreversible side-effect governance.
- [Source: `.github/workflows/release.yml`] - current release workflow, permissions, tests, package inventory, attestation fallback, semantic-release, and budget evidence.
- [Source: `.releaserc.json`] - semantic-release prepare/publish commands and GitHub Release assets.
- [Source: `eng/release_evidence.py`] - executable release evidence validation helpers.
- [Source: `eng/release-package-inventory.json`] - expected package inventory and symbol requirements.
- [Source: `_bmad-output/planning-artifacts/prd/functional-requirements.md#FR74-FR75`] - semantic release, SBOM, signed package, and symbol package requirements.
- [Source: `_bmad-output/planning-artifacts/prd/non-functional-requirements.md#Release-Automation`] - release automation, SBOM, signing, and package compatibility requirements.
- [Source: `_bmad-output/project-context.md`] - project rules for release evidence, redaction, central package management, and submodules.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L06--Defense-in-depth-budget-per-story`] - scope and decision budget guidance.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L07--Test-count-inflation-is-a-cost`] - test-scope budget guidance.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L08--Party-review-vs-elicitation--different-roles`] - later party review and elicitation sequencing.
- [Source: `_bmad-output/process-notes/story-creation-lessons.md#L10--Deferrals-need-story-specificity-not-epic-specificity`] - named owner requirement.
- [Official: GitHub Docs, artifact attestations](https://docs.github.com/en/actions/concepts/security/artifact-attestations) - current attestation/provenance model.
- [Official: GitHub Docs, using artifact attestations](https://docs.github.com/actions/security-for-github-actions/using-artifact-attestations/using-artifact-attestations-to-establish-provenance-for-builds) - workflow permission and verification context.
- [Official: Microsoft Learn, NuGet signed packages](https://learn.microsoft.com/en-us/nuget/reference/signed-packages-reference) - NuGet signing and timestamp expectations.
- [Official: CycloneDX .NET](https://github.com/CycloneDX/cyclonedx-dotnet) - current .NET SBOM tool used by the workflow.

---

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-13: Story created via `/bmad-create-story 12-4-trusted-release-evidence-dry-run` during recurring pre-dev hardening job.
- 2026-05-13: Pre-creation audit parsed `sprint-status.yaml`, confirmed status-artifact consistency had no drift, found ready buffer count 3, and selected the first backlog story `12-4-trusted-release-evidence-dry-run`.
- 2026-05-13: Starting release evidence audit identified current release workflow files `.github/workflows/release.yml`, `.releaserc.json`, helper `eng/release_evidence.py`, inventory `eng/release-package-inventory.json`, and expected packable package set `Cli`, `Contracts`, `Mcp`, `Schema`, `Shell`, and `Testing`.

### Completion Notes List

- 2026-05-13: Created the Story 12.4 developer guide and marked it ready for development. Ready for party-mode review on a later recurring run.

### Change Log

- 2026-05-13: Created Story 12.4 and marked ready-for-dev.

### File List

- `_bmad-output/implementation-artifacts/12-4-trusted-release-evidence-dry-run.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
