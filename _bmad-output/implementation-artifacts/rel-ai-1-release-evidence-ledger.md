---
title: REL-AI-1 Release Evidence Compliance Ledger
project: frontcomposer
created: 2026-07-15
updated: 2026-08-03
owner: Release Owner
decisionContract: frontcomposer.release-compliance-ledger.v1
sourceProposal: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-15-rel-ai-1-prepublish-enforcement.md
correctionProposal: _bmad-output/planning-artifacts/sprint-change-proposal-2026-08-03.md
status: active
---

# REL-AI-1 Release Evidence Compliance Ledger

This controlled ledger records whether released FrontComposer package bytes satisfy FR24. Workflow
success is not a compliance disposition. A release is compliant only when it was authorized before
publication and independently verified afterward against the same sealed manifest.

Historical records are not REL-AI-1 closure evidence. They document affected releases and the reason
the next publish-capable release is frozen until REL-3 is operational.

Status note (2026-07-18): REL-4's fail-closed freeze gate and REL-3's exact-artifact pre-publication
enforcement (pack-once orchestration in `eng/release_prepublish.py`, authorized-bytes publish,
independent downloaded-byte verification in `release-evidence.yml`) are implemented in the
repository. This changes no disposition in this ledger: REL-AI-1 closes only when a real release
passes the full chain with durable evidence and downloaded NuGet/GitHub bytes matching the
authorized manifest (REL-5 owner enablement).

Status note (2026-08-03): reconciliation now includes `v4.0.0` and `v4.0.1`. Both releases expose
the expected package/symbol asset count but no durable FR24 evidence assets, and a downloaded
Contracts package from each returns `NU3004` unsigned. Remaining run, registry, consumer, and
published-byte reconciliation stays open. Historical non-compliance is preserved even if a later
release corrects the process.

Status note (2026-08-03 REL-5 T0): the Release Owner restored
`HEXALITH_RELEASE_PUBLISH_ENABLED` from exact `true` to exact lowercase `false` at
`2026-08-03T06:24:13Z`. The complete enabled interval was audited across Release runs, GitHub
Releases/tags, and all eight nuget.org package IDs. No partial external publication was observed.
This containment result does not authorize publication or close REL-AI-1.

## Required Fields

Each release record carries:

- release tag/URL and CI, Release, and Release Evidence run URLs;
- expected/observed package inventory;
- NuGet and GitHub asset identity/hashes;
- package signing and timestamp verification;
- manifest verification, readiness classification, and `publish_authorized`;
- package-consumer validation;
- durable evidence paths;
- compliance disposition, owner, remediation, and verification date.

## Summary

| Release | Inventory | Published signing | Manifest | Readiness | Consumer validation | Durable evidence | Disposition |
| --- | --- | --- | --- | --- | --- | --- | --- |
| v3.2.1 | 8 `.nupkg` + 8 `.snupkg`; expected set | unsigned (`NU3004`) | invalid (40 diagnostics) | blocked; `publish_authorized=false` | passed in CI | none on GitHub Release; 30-day Actions artifact only | non-compliant / affected G1 release |
| v3.2.2 | 8 `.nupkg` + 8 `.snupkg`; expected set | unsigned (`NU3004`) | invalid (40 diagnostics) | blocked; `publish_authorized=false` | passed in CI | none on GitHub Release; 30-day Actions artifact only | non-compliant / affected G1 release |
| v4.0.0 | 8 `.nupkg` + 8 `.snupkg`; expected asset count | sampled Contracts package unsigned (`NU3004`) | no durable manifest evidence | not established | not reconciled | no FR24 assets on GitHub Release | non-compliant / affected pre-REL-4 release |
| v4.0.1 | 8 `.nupkg` + 8 `.snupkg`; expected asset count | sampled Contracts package unsigned (`NU3004`) | no durable manifest evidence | not established | not reconciled | no FR24 assets on GitHub Release | non-compliant / affected pre-REL-4 release |

## v3.2.1

| Field | Recorded evidence |
| --- | --- |
| Release | <https://github.com/Hexalith/Hexalith.FrontComposer/releases/tag/v3.2.1> |
| CI | success: <https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/29368280737> |
| Release workflow | success: <https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/29368461177> |
| Release Evidence workflow | success: <https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/29368682294> |
| Expected inventory | eight packable IDs (`Cli`, `Contracts`, `Contracts.UI`, `Mcp`, `Schema`, `Shell`, `SourceTools`, `Testing`) and two explicit non-packable projects (`AppHost`, combined `UI`) |
| Observed release assets | 16 assets: eight `.nupkg` plus eight `.snupkg`; no release-evidence assets |
| Consumer validation | CI step `Validate package consumer references`: success |
| Tests | `test-results.json`: valid, 4,122 tests, zero failures |
| Published signing | direct `dotnet nuget verify --all` on `Hexalith.FrontComposer.Contracts.3.2.1.nupkg`: `NU3004`, package is not signed |
| Evidence signing | `signing-readiness.json`: `signed=false`, `verified=false`, `blocking=true`; signing certificate secret not provisioned |
| Manifest | `manifest-verification.json`: invalid, 40 diagnostics; every package lacks signed-artifact checksum/signature/timestamp/sealed-artifact proof |
| Readiness | `classification=blocked`, `publish_authorized=false`; blocking reasons include missing release verification, invalid checksums/helper paths/semantic-release state, missing signing/timestamp, and invalid manifest |
| NuGet/GitHub byte comparison | not performed; G1 checksums cover reconstructed packages and do not establish identity with published assets |
| Durable evidence | absent from the immutable GitHub Release; workflow artifact `release-evidence-29368682294-1`, retention 30 days |
| Disposition | **non-compliant / affected G1 release** |
| Owner and remediation | Release Owner; retain this disclosure, do not use the release as FR24 closure, and supersede its release process with REL-3 exact-artifact enforcement |
| Verified | 2026-07-15 by direct release/run inspection, downloaded Actions evidence, and direct published-package signature verification |

## v3.2.2

| Field | Recorded evidence |
| --- | --- |
| Release | <https://github.com/Hexalith/Hexalith.FrontComposer/releases/tag/v3.2.2> |
| CI | success: <https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/29375165477> |
| Release workflow | success: <https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/29375310946> |
| Release Evidence workflow | success: <https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/29375505915> |
| Expected inventory | eight packable IDs (`Cli`, `Contracts`, `Contracts.UI`, `Mcp`, `Schema`, `Shell`, `SourceTools`, `Testing`) and two explicit non-packable projects (`AppHost`, combined `UI`) |
| Observed release assets | 16 assets: eight `.nupkg` plus eight `.snupkg`; no release-evidence assets |
| Consumer validation | CI step `Validate package consumer references`: success |
| Tests | `test-results.json`: valid, 4,122 tests, zero failures |
| Published signing | direct `dotnet nuget verify --all` on `Hexalith.FrontComposer.Contracts.3.2.2.nupkg`: `NU3004`, package is not signed |
| Evidence signing | `signing-readiness.json`: `signed=false`, `verified=false`, `blocking=true`; signing certificate secret not provisioned |
| Manifest | `manifest-verification.json`: invalid, 40 diagnostics; every package lacks signed-artifact checksum/signature/timestamp/sealed-artifact proof |
| Readiness | `classification=blocked`, `publish_authorized=false`; blocking reasons include missing release verification, invalid checksums/helper paths/semantic-release state, missing signing/timestamp, and invalid manifest |
| NuGet/GitHub byte comparison | not performed; G1 checksums cover reconstructed packages and do not establish identity with published assets |
| Durable evidence | absent from the immutable GitHub Release; workflow artifact `release-evidence-29375505915-1`, retention 30 days |
| Disposition | **non-compliant / affected G1 release** |
| Owner and remediation | Release Owner; retain this disclosure, do not use the release as FR24 closure, and supersede its release process with REL-3 exact-artifact enforcement |
| Verified | 2026-07-15 by direct release/run inspection, downloaded Actions evidence, and direct published-package signature verification |

## v4.0.0

| Field | Recorded evidence |
| --- | --- |
| Release | <https://github.com/Hexalith/Hexalith.FrontComposer/releases/tag/v4.0.0>; published 2026-07-15T23:40:57Z |
| CI and Release workflows | not yet mapped to the release during this correction; open historical reconciliation |
| Release Evidence workflow | no durable workflow evidence is attached to the GitHub Release; run mapping remains open |
| Expected inventory | eight packable IDs (`Cli`, `Contracts`, `Contracts.UI`, `Mcp`, `Schema`, `Shell`, `SourceTools`, `Testing`) and two explicit non-packable projects (`AppHost`, combined `UI`) |
| Observed release assets | 16 assets: eight `.nupkg` plus eight `.snupkg`; no SBOM, checksum, package-inventory, consumer-validation, readiness, manifest, or release-evidence assets |
| Consumer validation and tests | not reconciled to the exact published candidates |
| Published signing | direct `dotnet nuget verify --all` on `Hexalith.FrontComposer.Contracts.4.0.0.nupkg`: `NU3004`, package is not signed |
| Manifest and readiness | no durable sealed manifest or readiness evidence on the release; `classification=ready` and `publish_authorized=true` are not established |
| NuGet/GitHub byte comparison | not performed; published-byte identity remains open reconciliation |
| Durable evidence | absent from the GitHub Release |
| Disposition | **non-compliant / affected pre-REL-4 release** |
| Owner and remediation | Release Owner; retain this disclosure, reconcile the remaining workflow/registry facts, and do not use the release as FR24 closure evidence |
| Verified | 2026-08-03 by direct GitHub Release asset inspection and direct downloaded-package signature verification |

## v4.0.1

| Field | Recorded evidence |
| --- | --- |
| Release | <https://github.com/Hexalith/Hexalith.FrontComposer/releases/tag/v4.0.1>; published 2026-07-16T02:00:00Z |
| CI and Release workflows | not yet mapped to the release during this correction; open historical reconciliation |
| Release Evidence workflow | no durable workflow evidence is attached to the GitHub Release; run mapping remains open |
| Expected inventory | eight packable IDs (`Cli`, `Contracts`, `Contracts.UI`, `Mcp`, `Schema`, `Shell`, `SourceTools`, `Testing`) and two explicit non-packable projects (`AppHost`, combined `UI`) |
| Observed release assets | 16 assets: eight `.nupkg` plus eight `.snupkg`; no SBOM, checksum, package-inventory, consumer-validation, readiness, manifest, or release-evidence assets |
| Consumer validation and tests | not reconciled to the exact published candidates |
| Published signing | direct `dotnet nuget verify --all` on `Hexalith.FrontComposer.Contracts.4.0.1.nupkg`: `NU3004`, package is not signed |
| Manifest and readiness | no durable sealed manifest or readiness evidence on the release; `classification=ready` and `publish_authorized=true` are not established |
| NuGet/GitHub byte comparison | not performed; published-byte identity remains open reconciliation |
| Durable evidence | absent from the GitHub Release |
| Disposition | **non-compliant / affected pre-REL-4 release** |
| Owner and remediation | Release Owner; retain this disclosure, reconcile the remaining workflow/registry facts, and do not use the release as FR24 closure evidence |
| Verified | 2026-08-03 by direct GitHub Release asset inspection and direct downloaded-package signature verification |

## REL-5 T0 Enabled-Window Containment Audit

### Control evidence

| Field | Recorded evidence |
| --- | --- |
| Audit interval | `2026-08-02T08:27:15Z` through `2026-08-03T06:24:13Z` |
| Before state | repository variable `HEXALITH_RELEASE_PUBLISH_ENABLED=true`; `created_at` and `updated_at` both `2026-08-02T08:27:15Z` |
| After state | repository variable set to exact lowercase `false`; API `updated_at=2026-08-03T06:24:13Z` |
| Mutation side effect | changing the variable did not trigger a workflow or authorize a release |
| Publication status | unauthorized; retain non-`true` until the governed candidate/post-evidence authorization seam is approved |

### Release workflow audit

| Release run | Created (UTC) | Head | Execution result |
| --- | --- | --- | --- |
| [30743463963](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/30743463963) | 2026-08-02T10:17:53Z | `22c130d9` | Entered reusable job; runner-local `4.1.0` prepare failed closed at package-inventory validation before publication |
| [30757806987](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/30757806987) | 2026-08-02T16:57:48Z | `6521550a` | `freeze-guard` and release path skipped |
| [30757835682](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/30757835682) | 2026-08-02T16:58:34Z | `d9f0d526` | `freeze-guard` and release path skipped |
| [30757956331](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/30757956331) | 2026-08-02T17:01:39Z | `d9f0d526` | Entered reusable job; runner-local `4.1.0` prepare failed closed at package-inventory validation before publication |
| [30758637451](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/30758637451) | 2026-08-02T17:20:06Z | `4302301a` | Entered reusable job; runner-local `4.1.0` prepare failed closed at package-inventory validation before publication |
| [30760188983](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/30760188983) | 2026-08-02T18:01:57Z | `52f4327c` | Entered reusable job; runner-local `4.1.0` prepare failed closed at package-inventory validation before publication |
| [30785942090](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/30785942090) | 2026-08-03T05:00:32Z | `8a6a6cb3` | `freeze-guard` and release path skipped |

For all four entered jobs, `Semantic Release` stopped in its `prepare` phase with
`release_prepublish.py prepare --version 4.1.0` failing at the inventory command. The complete
release-evidence upload step was skipped. Locally generated runner candidates are not published
artifacts.

### GitHub publication surface

- No GitHub Release was created in the audit interval.
- No remote release tag is newer than `v4.0.1`.
- The latest release remains
  [v4.0.1](https://github.com/Hexalith/Hexalith.FrontComposer/releases/tag/v4.0.1), published
  `2026-07-16T02:00:00Z` with 16 package/symbol assets.

### NuGet publication surface

The nuget.org registration records contained no publication in the audit interval:

| Package registration | Latest version | Published (UTC) |
| --- | --- | --- |
| [Cli](https://api.nuget.org/v3/registration5-gz-semver2/hexalith.frontcomposer.cli/index.json) | `4.0.1` | 2026-07-16T01:59:39.51Z |
| [Contracts](https://api.nuget.org/v3/registration5-gz-semver2/hexalith.frontcomposer.contracts/index.json) | `4.0.1` | 2026-07-16T01:59:39.92Z |
| [Contracts.UI](https://api.nuget.org/v3/registration5-gz-semver2/hexalith.frontcomposer.contracts.ui/index.json) | `4.0.1` | 2026-07-16T01:59:40.26Z |
| [Mcp](https://api.nuget.org/v3/registration5-gz-semver2/hexalith.frontcomposer.mcp/index.json) | `4.0.1` | 2026-07-16T01:59:40.647Z |
| [Schema](https://api.nuget.org/v3/registration5-gz-semver2/hexalith.frontcomposer.schema/index.json) | `4.0.1` | 2026-07-16T01:59:41.013Z |
| [Shell](https://api.nuget.org/v3/registration5-gz-semver2/hexalith.frontcomposer.shell/index.json) | `4.0.1` | 2026-07-16T01:59:41.43Z |
| [SourceTools](https://api.nuget.org/v3/registration5-gz-semver2/hexalith.frontcomposer.sourcetools/index.json) | `4.0.1` | 2026-07-16T01:59:41.877Z |
| [Testing](https://api.nuget.org/v3/registration5-gz-semver2/hexalith.frontcomposer.testing/index.json) | `4.0.1` | 2026-07-16T01:59:42.237Z |

### Audit disposition

**No partial publication observed.** Neither GitHub Releases/tags nor any configured nuget.org
package ID gained a version during the enabled interval. REL-5 T0 is complete; REL-AI-1 remains
open and a future release still requires the complete FR24 prepublication and published-byte chain.

## Next Compliant Release Record

Do not populate a passing disposition from a dry run or reconstructed evidence. The next record may be
marked compliant only after all of the following are durable:

- valid expected inventory, tests, and package-consumer validation against the release candidates;
- verified author signatures and RFC 3161 timestamps on every published `.nupkg`;
- required symbols and SBOM bound by complete checksums;
- valid sealed manifest over the exact candidate paths;
- `classify-release --require-publishable` with `classification=ready` and
  `publish_authorized=true` before publication;
- initial GitHub Release evidence assets;
- downloaded NuGet and GitHub bytes matching the authorized hashes;
- no unreconciled partial-publication incident.

REL-AI-1 remains open until the Release Owner records and signs off that real-release evidence.
