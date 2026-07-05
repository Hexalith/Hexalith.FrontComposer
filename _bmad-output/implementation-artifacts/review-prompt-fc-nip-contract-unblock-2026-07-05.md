# Adversarial Review Prompt: FC-NIP Contract Unblock

Review the FC-NIP contract unblock change with the `bmad-review-adversarial-general` skill.

## Scope

The user approved resolving and pinning the FC new-item indicator producer / generated grid consumer row-identity contract before another Story 9.2 implementation pass.

Review only this scoped change:

- `_bmad-output/implementation-artifacts/9-2-wire-fcnewitemindicator-producer-and-generated-grid-consumer.md`
- `_bmad-output/implementation-artifacts/epic-9-context.md`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Docs/FcNipRowIdentityProducerContractTests.cs`
- `tests/e2e/specs/fc-nip-row-identity-contract.spec.ts`

Also read this source-of-truth contract even if it is not currently dirty in git:

- `_bmad-output/contracts/fc-nip-row-identity-producer-contract-2026-07-04.md`

## Intent

The active contract decision is:

- Approved source: FrontComposer-owned pending-command row metadata populated from generated grid/command runtime context.
- EventStore command status remains lifecycle/status-only by `MessageId`.
- EventStore `AggregateId` is not promoted to FC-NIP row identity.
- Story 9.2 becomes `ready-for-dev`, not `done`; production producer/grid consumer work remains open.
- Sprint status should be advanced by the future implementation workflow, not by this unblock step.

## Validation Already Run

- `dotnet build tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj -c Release --no-restore -m:1 /nr:false -p:UseSharedCompilation=false` passed.
- `DiffEngine_Disabled=true tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Release/net10.0/Hexalith.FrontComposer.SourceTools.Tests -noLogo -noColor -class Hexalith.FrontComposer.SourceTools.Tests.Docs.FcNipRowIdentityProducerContractTests` passed 2/2.
- `npm run typecheck` in `tests/e2e` passed.
- `npm run test:fc-nip` in `tests/e2e` passed 4/4.

## Review Focus

Find concrete issues only. Pay particular attention to:

- Whether Story 9.2 now overclaims readiness or falsely implies AC implementation.
- Whether the approved payload source is specific enough for the next implementation pass.
- Whether tests still pin the no-smuggling boundary for EventStore `AggregateId` and generated command metadata.
- Whether the unchanged sprint-status behavior is coherent with Story 9.2 moving to `ready-for-dev`.
- Whether old historical review text conflicts with the new active status.
