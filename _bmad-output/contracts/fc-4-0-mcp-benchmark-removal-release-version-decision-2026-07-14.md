---
title: FrontComposer 4.0 MCP Benchmark Removal Release Version Decision
date: 2026-07-14
status: approved
owner: Release Owner
approvedBy: Administrator
approvedOn: 2026-07-15
story: 11.17c
supersedes: none
---

# FrontComposer 4.0 MCP Benchmark Removal Release Version Decision

## Proposed Decision

Approve `4.0.0` as the release target for removing the 26 public `SkillBenchmark*` types from
`Hexalith.FrontComposer.Mcp` while relocating the repository-maintainer benchmark harness to the
non-packable `Hexalith.FrontComposer.Shell.Tests.Bench` executable.

The configured package-validation baseline is `3.0.0`, and the latest stable release at the start
of Story 11.17c is `v3.1.1`. Removing already-shipped public types without runtime facades or type
forwarders is intentionally binary-breaking. The change must not be represented as a compatible
`3.x` release.

## Proposed Compatibility Posture

- The MCP runtime retains the 27 skill-corpus and generated-code-validator declarations required
  to serve and validate the published skill corpus.
- The MCP package exports no `SkillBenchmark*` type and embeds no benchmark prompt resource.
- No runtime NuGet replacement is provided for the removed benchmark types. Repository
  maintainers run the harness through `Hexalith.FrontComposer.Shell.Tests.Bench` and
  `eng/llm_benchmark.py`.
- The benchmark declarations retain their historical namespace only to preserve their source
  bodies during the assembly-ownership relocation; the Bench executable remains non-packable.
- Compatibility suppressions are limited to one exact `CP0001` removal per affected public type,
  mirrored one-to-one in the compatibility ledger as `intentional-major-break` entries with
  `currentRelease` and `targetRelease` `v4.0` and `expiresAfter` `v4.1`.
- The first release-lifecycle update after `4.0.0` must advance the MCP package-validation baseline
  to `4.0.0` and remove the 26 XML and JSON suppressions before any `v4.1` pack.

## Required Release Evidence

The authorized implementation commit must carry a valid Conventional Commit breaking-change
signal (`!` or a `BREAKING CHANGE:` footer). A post-commit semantic-release dry run must classify
the release range as major. `CHANGELOG.md` remains semantic-release-owned and is not edited by this
story.

Before approval, Story 11.17c may perform source, test, package-discovery, and documentation work,
but it must not advance the compatibility ledger to `v4.0`, run the release pack/dry-run gates, or
move the story to review.

## Approval Record

Approved by Administrator acting as Release Owner on 2026-07-15. The approval explicitly accepts
the `4.0.0` major-release posture, removal of all 26 public MCP benchmark types without a runtime
NuGet replacement, the bounded `v4.0` to `v4.1` suppression lifecycle, and creation of the
breaking implementation commit required for semantic-release evidence.
