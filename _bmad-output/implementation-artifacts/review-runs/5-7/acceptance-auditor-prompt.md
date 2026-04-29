# Acceptance Auditor Prompt - Story 5-7

You are an Acceptance Auditor. Review this diff against the spec and context docs. Check for acceptance-criteria violations, deviations from spec intent, missing implementation of specified behavior, and contradictions between spec constraints and actual code.

Spec file:

- `_bmad-output/implementation-artifacts/5-7-signalr-fault-injection-test-harness.md`

No extra frontmatter context docs were loaded.

Construct the review diff with:

```powershell
git diff HEAD -- _bmad-output\implementation-artifacts\5-7-signalr-fault-injection-test-harness.md _bmad-output\implementation-artifacts\sprint-status.yaml
git diff --no-index -- NUL tests\Hexalith.FrontComposer.Shell.Tests\Infrastructure\EventStore\FaultInjection\FR24-29-trace.md
git diff --no-index -- NUL tests\Hexalith.FrontComposer.Shell.Tests\Infrastructure\EventStore\FaultInjection\FaultInjectingProjectionHubConnection.cs
git diff --no-index -- NUL tests\Hexalith.FrontComposer.Shell.Tests\Infrastructure\EventStore\FaultInjection\FaultInjectingProjectionHubConnectionFactory.cs
git diff --no-index -- NUL tests\Hexalith.FrontComposer.Shell.Tests\Infrastructure\EventStore\FaultInjection\FaultInjectingProjectionHubConnectionTests.cs
git diff --no-index -- NUL tests\Hexalith.FrontComposer.Shell.Tests\Infrastructure\EventStore\FaultInjection\HarnessCheckpoint.cs
git diff --no-index -- NUL tests\Hexalith.FrontComposer.Shell.Tests\Infrastructure\EventStore\FaultInjection\HarnessDisposalException.cs
git diff --no-index -- NUL tests\Hexalith.FrontComposer.Shell.Tests\Infrastructure\EventStore\FaultInjection\NudgeQueueToken.cs
git diff --no-index -- NUL tests\Hexalith.FrontComposer.Shell.Tests\Infrastructure\EventStore\FaultInjection\ProjectionHubFaultScenarioBuilder.cs
git diff --no-index -- NUL tests\Hexalith.FrontComposer.Shell.Tests\Infrastructure\EventStore\FaultInjection\ProjectionSubscriptionServiceFaultTests.cs
git diff --no-index -- NUL tests\Hexalith.FrontComposer.Shell.Tests\Infrastructure\EventStore\FaultInjection\README.md
```

Output findings as a Markdown list. Each finding must include: one-line title, which AC or story constraint it violates, and evidence from the diff.
