---
title: "IDE parity"
description: "Reference wrapper for FrontComposer IDE parity matrix and evidence manifests."
genre: reference
audience: adopter
ownerStory: 9-5-diataxis-documentation-site
status: published
reviewed: 2026-05-10
uid: frontcomposer.reference.ide-parity
slug: reference/ide-parity/
---

# IDE parity

<!-- hfc:reference:start -->
The authoritative matrix is `docs/ide-parity-matrix.md`, with JSON evidence in `artifacts/ide-parity/evidence`.

Must-tier parity covers generated type completion, XML-doc hover content, go-to-definition to generated source, HFC diagnostic squiggles, solution-wide symbol search, generator performance, remote path limits, and version revalidation.

Matrix and evidence JSON are strict release-gate artifacts: duplicate keys, unknown properties,
trailing commas, traversal paths, rooted paths, URI-shaped paths, and unsupported evidence artifact
locations fail closed in tests. Evidence paths must remain under `artifacts/ide-parity/` and use
repository-relative forward slashes.

`jobs/ide-parity-version-revalidation.ps1` writes dry-run issue artifacts atomically inside the
repository. `$OutPath` is repository-bounded, drive-relative and URI-shaped paths are rejected, and
parallel script invocation is not a supported release-gate mode.

Production source must not contain unconditional `Debugger.Launch()` calls. Contributor debugging
may use `Debugger.Launch()` only in local investigation branches, guarded narrowly and removed
before review; the IDE parity test suite enforces this against `src/**/*.cs`.
<!-- hfc:reference:end -->

Onboarding, troubleshooting, CLI output, and generated-output references link here so IDE claims do not drift from the matrix.
