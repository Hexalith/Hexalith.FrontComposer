---
title: 'Keep CLI diagnostic reports complete and actionable'
type: 'bugfix'
created: '2026-11-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_commit: 'b0ab2ee7efeb4de19dc01b277805370e2df38fa5'
context: []
closes_deferred: [DW-1624, DW-1625, DW-1626, DW-1628, 'migration diagnostics for unavailable sources']
---

<frozen-after-approval reason="Five open FrontComposer CLI deferred findings">

## Intent

Make CLI diagnostic reports reliable when inspecting or migrating a project. Given a malformed inspect sidecar, report one `HFCM0002` warning and keep readable diagnostics. If the generated directory cannot be enumerated, return exit code 3 with retry guidance. Given an inspect diagnostic without a related type, retain it as a global diagnostic under `--type`. Given a path-like configuration, return exit code 2 before resolving generated output or invoking the SDK. Given a valid manual migration diagnostic for an unavailable project source, retain it as a manual finding with guidance to regenerate the sidecar.

</frozen-after-approval>

## Implementation Notes

Inspect distinguishes an unreadable diagnostics directory from an empty set. It reports malformed sidecars with a path and keeps readable entries in the same file. Missing and null related types remain visible under `--type`. Configuration validation runs before framework discovery or SDK invocation. Migration reports valid manual sidecar diagnostics whose source could not be scanned and includes regeneration guidance in both output formats.

## Review Triage Log

- Medium, fixed. Rooted invalid paths now pass through the same guarded normalization as relative paths. An escaped NUL is redacted and sibling diagnostics are retained.
- Medium, fixed. Non-string, non-null related types yield a malformed-sidecar warning in place of the invalid finding.
- Medium, fixed. Bare `--configuration` is rejected without treating it as the literal name `true`. Explicit `true` remains a valid custom name.
- Medium, fixed. Framework discovery shares the generated-output error handling. An inaccessible configuration directory returns exit code 3.
- Medium, fixed. The default migration text includes the fix for manual-only findings, including regeneration guidance for unavailable source files.
- Low, fixed. The text renderer includes the affected sidecar path with an `HFCM0002` warning.

## Verification

- `dotnet build tests/Hexalith.FrontComposer.Cli.Tests/Hexalith.FrontComposer.Cli.Tests.csproj -c Debug -m:1` passed on SDK 11.0.100 with zero warnings and errors.
- `dotnet test --project tests/Hexalith.FrontComposer.Cli.Tests/Hexalith.FrontComposer.Cli.Tests.csproj -c Debug --no-build` passed all 190 cases with zero skips.
- `python3 -m unittest tests/eng/test_validate_contract_artifacts.py` passed all four tests.
- `aspire run` could not start because the Aspire CLI is absent. The CLI build and tests ran independently of the AppHost.
