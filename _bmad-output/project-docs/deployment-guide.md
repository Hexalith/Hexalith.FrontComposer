# Hexalith.FrontComposer — Deployment / Release Guide

> Updated 2026-08-16 for the operator-controlled, exact-source production release model.
> FrontComposer ships NuGet packages, not a deployed service or container image.
> Approved Builds execution identity is `3f0e3595be693fce56a37648c0bd0f89390f5fd3` and is independent
> of the catalog gitlink (`7867d8fc7bcc3c906b16f0867f6555d8bec5432d`, EventStore 3.95.0 /
> Memories 2.21.3 / Tenants 5.4.1).

## Published package set

The approved release boundary is declared by `tools/release-packages.json` and contains exactly eight
lockstep-versioned packages:

1. `Hexalith.FrontComposer.Cli`
2. `Hexalith.FrontComposer.Contracts`
3. `Hexalith.FrontComposer.Contracts.UI`
4. `Hexalith.FrontComposer.Mcp`
5. `Hexalith.FrontComposer.Schema`
6. `Hexalith.FrontComposer.Shell`
7. `Hexalith.FrontComposer.SourceTools`
8. `Hexalith.FrontComposer.Testing`

Every package has a symbol package. `AppHost` and the combined `UI` host are explicit non-package
projects. FrontComposer declares no container projects and passes `publish-containers: false` to the
shared release workflow.

## CI and release architecture

| Layer | Trigger | Responsibility |
|---|---|---|
| CI | push and pull request to `main` | build/test plus fail-closed dependency graph/catalog governance |
| Quality | push and pull request to `main` | FrontComposer-specific governance, contracts, docs, telemetry, a11y, and visual checks |
| Release | operator `workflow_dispatch` from `refs/heads/main` | exact-source authentication, semantic release planning, protected preparation, and the pinned Builds publisher |
| Release Evidence | completion of every Release run | explicit no-release/rejected disposition or independent verification of immutable GitHub/NuGet publication |

Normal pushes and pull requests never publish. An operator starts Release only from the current
`refs/heads/main` SHA. Before any production job can start, the workflow requires exactly one completed,
successful push CI run for that SHA and authenticates its run/attempt-bound dependency source proof.
Malformed, truncated, paginated, missing, failed, or ambiguous API evidence fails closed. If `main`
advances, the operator must wait for successful push CI on the new tip and dispatch again.

Release concurrency is the repository-wide `release-production` group with cancellation disabled. The
protected jobs use the `production` environment. The reusable publisher is selected at the exact
Hexalith.Builds commit `3f0e3595be693fce56a37648c0bd0f89390f5fd3`; the identical value is passed as
`builds-execution-sha`. The candidate's `references/Hexalith.Builds` gitlink is an independent catalog
identity: it must be a real `160000` lowercase 40-hex SHA and need not equal the execution pin.
Mutable workflow references are not accepted at the release boundary. The working-tree catalog gitlink
is `7867d8fc7bcc3c906b16f0867f6555d8bec5432d`; catalog companions there are EventStore `3.95.0`,
Memories `2.21.3`, and Tenants `5.4.1`.

## Operator procedure

1. Confirm the current `main` SHA and that its `CI` workflow completed successfully from a `push`.
2. Open Actions → Release, select `main`, and choose **Run workflow**. Do not dispatch from another ref.
3. Review the exact SHA and planned version in the unprotected job summary.
4. If a release is required, approve the pending `production` environment job according to the
   repository's environment protection policy.
5. Wait for the protected reusable release and `verify-publication` jobs to finish.
6. Wait for Release Evidence to verify the immutable release, exact GitHub checksums, NuGet.org
   repository signatures and package content, symbols, SBOM, and sealed manifest. Retain any incident
   artifact for reconciliation.

If Semantic Release finds no releasable commits, Release reports that fact explicitly, skips preparation
and publication, and does not claim a release was published.

## Pack-once and publication safety

The protected preparation job runs `eng/release_prepublish.py prepare` once. It validates the declared
eight-package boundary, builds, packs, inventories, tests, validates consumers, produces symbols and an
SBOM, records benchmark and checksum evidence over the exact unsigned candidates, seals/verifies the
manifest, and requires a publish-authorized readiness result.

The resulting bytes and evidence are sealed into a run/attempt-bound prepared-candidate artifact. Inside
the pinned reusable publisher, Semantic Release restores and authenticates that exact artifact, verifies
it again, and publishes only manifest-authorized `.nupkg` and matching `.snupkg` bytes. It does not
repack. `--skip-duplicate` is prohibited because it can hide partial publication.

The release configuration intentionally has no changelog/git commit plugin. Therefore the Semantic
Release tag is created on the dispatched source itself. A successful release must produce a non-draft
GitHub Release whose tag resolves, through any annotated tag objects, to the exact dispatched SHA.

The independent Release Evidence workflow is read-only. For governed attempts it requires an immutable,
non-draft GitHub Release, downloads its durable assets, verifies the sealed manifest against the exact
checked-out source/dependency graph, downloads each of the eight packages from nuget.org, exact-hashes
the GitHub candidate against the seal, compares every non-signature ZIP member with the NuGet.org copy,
verifies the NuGet.org repository signature with `dotnet nuget verify --all`, and verifies
symbols/SBOM/checksum evidence. Missing or inconsistent publication writes a typed
`partial-publish-incident.json` and fails. Post-publication evidence can never authorize a release
retroactively.

## Dependency source proof

Push CI emits `hexalith.dependency-release-source.v1`, binding the CI run/attempt, push base policy, and
exact candidate dependency graph. This replaces the unrealized AD-16 handoff without pretending that the
shared mutable CI implementation has an immutable evaluator closure. Release authenticates the artifact
against the exact successful CI run and recomputes the graph before sealing
`hexalith.release-evidence.v3`. The v3 workflow provenance binds the exact Release caller bytes, the exact
Builds reusable workflow bytes, and the Builds execution SHA.

## Required external configuration

Implementation does not change GitHub settings. Repository administrators must confirm these existing
controls before a real dispatch:

| Name | Kind | Requirement |
|---|---|---|
| `production` | GitHub environment | required reviewers/protection policy; secrets are unavailable before approval |
| `HEXALITH_RELEASE_PUBLISH_ENABLED` | repository variable | Release Owner custody. On operator `workflow_dispatch` of Release, the pinned Builds `domain-release.yml` publisher evaluates this variable with an exact, case-sensitive, untrimmed bash match to `true`. Any other value (including absent) freezes publication: Semantic Release (and governed build/candidate/attestation on the governed path) is skipped, the run concludes successfully, and a freeze notice is emitted. Set a repository-level value even when an organization-level value exists — repository shadows organization. Leave unset or non-`true` while publication is unauthorized. `NUGET_API_KEY` alone cannot unfreeze publication. |
| `NUGET_API_KEY` | production/repository secret | forwarded only to the protected shared publisher |
| `RELEASE_ATTESTATION_STATUS` | production/repository variable | `attested` or the approved `approved-unsupported` contingency |
| `RELEASE_ATTESTATION_FALLBACK_APPROVER` | production/repository variable | required for the bounded unsupported-attestation contingency |
| `RELEASE_ATTESTATION_FALLBACK_APPROVED_AT` | production/repository variable | UTC approval timestamp |
| `RELEASE_ATTESTATION_FALLBACK_EXPIRES_AT` | production/repository variable | UTC expiry timestamp |

Immutable GitHub Releases must be enabled for the repository. FrontComposer does not require or consume
an author-signing certificate, certificate password, or timestamp service. NuGet.org adds its repository
signature after upload; Release Evidence verifies downloaded packages with the stock public trust bundle.

No assistant or implementation task should alter these settings or perform a real dispatch. A real
release requires a separate, explicit operator decision and production approval.

## Local verification (non-publishing)

Use the repository's normal Release build/test commands plus:

```bash
python3 eng/release_contract.py manifest --root . --manifest tools/release-packages.json --expected-count 8
python3 -m unittest tests/eng/test_dependency_graph.py tests/eng/test_dependency_handoff.py tests/eng/test_release_contract.py
actionlint .github/workflows/*.yml
git diff --check
```

`release_prepublish.py prepare --non-publishing` exercises the full candidate chain with a local,
non-authorizing readiness context. It cannot publish and is not a substitute for production approval.
