---
title: REL-AI-1 Release Evidence Compliance Ledger
project: frontcomposer
created: 2026-07-15
updated: 2026-07-15
owner: Release Owner
decisionContract: frontcomposer.release-compliance-ledger.v1
sourceProposal: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-15-rel-ai-1-prepublish-enforcement.md
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
