---
title: 'Verify release package inventory and metadata'
type: 'bugfix'
created: '2026-11-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_commit: 'bbf46a04530eaef5bb3742f36c329c0ea28a0da5'
context: []
---

<frozen-after-approval reason="Five open findings in the release package verification path">

## Intent

Close DW-1861, DW-1862, DW-1870, DW-1876, and DW-1877. Reject incomplete or extra package and symbol outputs from the shared CI packer. Require the exact primary assembly paths in release candidates, verify the paired symbol archive identity and portable symbols, and prove that the verifier rejects each binary version mismatch independently. Reject duplicate JSON members in the compatibility suppression ledger before its values can affect packing. Exercise failures through the existing packer and verifier entry points.

</frozen-after-approval>

## Implementation Notes

The shared packer compares its output directory against the package and symbol names derived from the release inventory. It reports missing and unexpected paths after successful pack commands. The compatibility ledger parser rejects repeated object members at every nesting level.

The candidate verifier pins ten primary assemblies across eight packages. It checks the tool, analyzer, and library layouts emitted by the pinned SDK. Symbol archives must declare the expected package ID, version, and symbol package type. Their exact portable PDB paths include the Contracts companion in the analyzer package. Each PDB must match the CodeView identity and checksum recorded in its paired assembly.

## Review Triage Log

- Medium, fixed symbol content verification to catch corruption that preserves the PDB header identity.
- Medium, fixed standalone verification to reject unexpected root-level symbol archives.
- Medium, fixed linked directories that could hide unexpected archives from the packer inventory scan.
- Medium, fixed missing or duplicate symbol ZIP control parts and unexpected symbol payloads.
- Medium, fixed duplicate package-type containers and required the analyzer package's companion symbol.
- Medium, fixed each binary version fixture to mutate only the second target assembly and assert the target path in the diagnostic.
- Medium, retained DW-1849 for the existing SemVer build-metadata mismatch across release artifact naming and publication.

## Verification

- The .NET SDK 11.0.100 Release solution build completed with zero warnings and errors.
- The shared CI pack command for `0.0.0-ci-test` produced eight packages and eight symbol archives under the current compatibility policy.
- The production-style pack command for `6.1.0-review.verify` and the standalone verifier passed for all eight package pairs and ten primary assemblies. Both package runs were local.
- An inventory probe rejected a linked directory that would conceal an extra symbol archive.
- Both package-tooling Python suites passed all 72 tests. The standalone Shell package-inventory governance test also passed.
- The broader .NET default lane was interrupted after the CLI tool smoke test timed out during its sample-project build.
