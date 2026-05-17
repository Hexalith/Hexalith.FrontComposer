---
title: Mutation and Property Quality Gates
description: Run and interpret the Story 10-4 mutation and command idempotency property gates.
genre: how-to
audience: framework-contributor
ownerStory: 10-4-mutation-testing-and-property-based-testing
status: active
reviewed: 2026-05-10
uid: hfc.howto.mutation-property-quality-gates
---

# Mutation and Property Quality Gates

Story 10-4 adds two authoritative nightly gates and one local replay path for contributors.

## Local Commands

Restore pinned tools and packages:

```bash no-compile reason="developer shell command"
dotnet tool restore
dotnet restore Hexalith.FrontComposer.slnx
```

Run the deterministic command idempotency property suite:

```bash no-compile reason="developer shell command"
pwsh ./eng/run-lifecycle-property-suite.ps1 -MaxTest 1000 -Replay "15485863,32452843,0"
```

Omit `-Replay` for a random nightly-style seed. The script writes the actual seed and replay command to `artifacts/property/property-seed-summary.md`, plus structured max-size, sequence-count, operation-distribution, shrink-policy, and replay-command evidence to `artifacts/property/property-run-evidence.json`.

Run Stryker segments from the SourceTools test project:

```bash no-compile reason="mutation run is long-running and environment-specific"
dotnet tool run dotnet-stryker --config-file tests/Hexalith.FrontComposer.SourceTools.Tests/Mutation/stryker-happy-path.json --output artifacts/mutation/happy-path
dotnet tool run dotnet-stryker --config-file tests/Hexalith.FrontComposer.SourceTools.Tests/Mutation/stryker-error-handling.json --output artifacts/mutation/error-handling
pwsh ./eng/validate-stryker-reports.ps1
```

## Thresholds

The happy-path SourceTools Parse/Transform segment is blocking at 80 percent. The error-handling segment is blocking at 60 percent. These are separate gates; a blended score is not accepted as evidence.

## Survivor Triage

Every `Survived`, `NoCoverage`, `Timeout`, and `CompileError` mutant needs one action:

- `kill-test-added`: a focused test now kills the mutant.
- `equivalent-accepted`: the mutant is equivalent or non-actionable, with rationale.
- `deferred-with-owner`: the gap is real and has an owner/follow-up story.
- `blocking`: the gate cannot pass until the mutant is addressed.

Record accepted or deferred problem mutants in `tests/Hexalith.FrontComposer.SourceTools.Tests/Mutation/mutation-target-manifest.json` under `problemMutantTriage`; validation fails when report counts exceed reviewed triage entries.

## Property Counterexamples

FsCheck failures must retain the package version, seed, size or max size, sequence count, operation distribution, shrunk sequence, and replay command. Confirmed bug or protected-invariant counterexamples go in `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/Lifecycle/Fixtures/command-idempotency-counterexamples.json`.

Do not commit transient non-bug seeds. Fixtures must use synthetic correlation, tenant, user, and message values only.

## Artifact Allowlist

Allowed artifacts are bounded Stryker HTML/JSON reports, mutation validation summaries, FsCheck TRX files, seed summaries, and sanitized failure envelopes. Artifacts must not contain secrets, bearer tokens, environment dumps, full payload bodies, tenant/user identifiers, machine-local paths, or unbounded generated source.

## Oracle Contract

The lifecycle property oracle runs each generated sequence against fresh `LifecycleStateService`, deterministic time, a fresh transition subscriber set, and a fresh reference evaluation. It compares final lifecycle state, message ID, idempotency flag counts, transition trace, visible terminal notifications, warning counts, and invalid-transition counts between original and replay.

The authoritative gate status is nightly blocking. PR smoke may provide a fast advisory signal only; it cannot replace the nightly thresholds or suppress mutation/property evidence. Story 10-5 owns any quarantine or CI diet policy.
