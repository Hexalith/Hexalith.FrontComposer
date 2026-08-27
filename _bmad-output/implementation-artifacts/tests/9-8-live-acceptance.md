# Story 9.8 composed and live acceptance evidence

## Acceptance state

Final live acceptance passed in strict mode against clean committed candidate
`cd67e933b7e381f8da170c7fa6843ff3aae75802`. The browser, runtime metadata,
Aspire resource description, redacted logs, JUnit/HTML reports, screenshot, trace,
and checksums correlate to that exact candidate and discovered endpoint.

The historical bundle in `artifacts/epic-9/`, the former final bundle in
`artifacts/epic-9-final-f4f43fdc/`, and all development bundles remain preserved.
The current validator rejects the former final bundle's non-canonical checksum paths,
so it is historical evidence only. The accepted final bundle is
`artifacts/epic-9-final-cd67e933/`; CI produces
`epic-9-live-acceptance-artifacts` uploads for 14 days.

## Final live result

- Command: `FC_EPIC9_ARTIFACT_ROOT=/home/administrator/projects/hexalith/frontcomposer/artifacts/epic-9-final-cd67e933 FC_EPIC9_REQUIRE_CLEAN=true FC_EPIC9_EXPECTED_COMMIT=cd67e933b7e381f8da170c7fa6843ff3aae75802 ./eng/run-epic9-live-proof.sh`
- Recorded at: `2026-08-27T22:20:25Z`
- Candidate commit: `cd67e933b7e381f8da170c7fa6843ff3aae75802`
- Evidence mode: `final`
- Working tree dirty: `false`
- Aspire CLI: `13.4.6+87fe259e4fc244c599019a7b1304c85a1488f248`
- .NET SDK: `10.0.302`
- Node: `v26.4.0`
- Discovered resource: `counter-web-ggqnqffh`
- Discovered endpoint: `https://localhost:41819`
- Start mode: `isolated-no-build-after-serialized-build`
- Browser result: 1 passed in 13.6 seconds
- Evidence tooling: 87/87 artifact-validator and proof-runner cases passed
- Artifact validator: final bundle passed against the exact clean candidate
- Checksums: every retained file passed `sha256sum -c checksums.sha256`
- AppHost safety: preflight and postflight returned `[]`; the proof stopped only its exact isolated AppHost

The standard isolated start encountered the repository's known parallel
static-web-assets contention. The script retained that failure, built the missing
EventStore Aspire dependency serially, built the AppHost without re-entering the
collision-prone dependency graph, started the same isolated AppHost with `--no-build`,
and stopped it before artifact validation.

The generated create used fresh exact key `counter-e9-1787869229160`, which was
absent from the already-rendered grid before dispatch and matched all four recorded
create/update dispatches. The row materialized at 41, reached 44 after two
overlapping updates, and reached 52 after the later update. The browser observed one
first-wins announcement with localized copy, `role="status"`,
`aria-live="polite"`, the expected accessible label, tenant `counter-demo`, user
`demo-user`, and materialization dismissal after every phase. Command payloads are
`[REDACTED]` in retained browser evidence.

Selected final-bundle SHA-256 values:

```text
62460841184a7c1dd4736d350bafd03886a262f5638df99e5c87c24c190a09a2  runtime-metadata.json
9050a30885a92d25bbc52cad8aefd733a7fe8f24226403a57d950f2f70089d48  counter-web-describe.json
a2d8995cc4dbb63917255df4bd644c855467552b8dd88e1f5ce17c088cfff7fa  counter-web-logs.redacted.json
ee0b4a884ac5b6746ca6d63953deef17358b3da1fa766141cd143850a5b4ec1f  junit.xml
213a31a2bf333c32d459f67440c455b96a85baa9fb99d524c09f2cbca32f55e5  epic-9-command-evidence.json
9873b97765bca71d837261056a284c669bbcb6c9afcfc68864f942f76f6ec7f1  epic-9-live-acceptance.png
0bddeaad56474cbea1c964d6a11b9ea34bc0dff2044b207202aac5e171aab90a  trace.zip
```

## Composed and repository verification

- `npm --prefix tests/e2e run typecheck` -- passed.
- `npm --prefix tests/e2e run test:epic-9-evidence` -- 87/87 passed, including missing, contradictory, stale, sensitive, wrong-type, dirty-candidate, fallback-dependency, build-failure, and unrelated-AppHost cases.
- Release build of `Hexalith.FrontComposer.Shell.Tests.csproj` with serialized build, NuGet audit disabled, and central transitive pinning disabled -- passed with 0 warnings and 0 errors.
- Built xUnit `Epic9CompositionTests` class filter -- 2/2 passed.
- Built xUnit `CounterPage_SeedState_RendersUnrelatedRow` filter -- 1/1 passed.
- Built xUnit `AnalyzerPolicy_IdentifierInventory_MatchesSeal` filter -- 1/1 passed after the intentional Story 9.8 test-source reseal.
- Exact solution default lane -- blocked during restore by pre-existing `NU1109`: `FsCheck.Xunit.v3 3.3.4` requires `FsCheck 3.3.4`, while the central catalog selects `FsCheck 3.3.3`.

## Candidate reconciliation

The accepted browser bundle proves the live behavior and repaired serialized fallback
against the exact clean implementation commit. Commit-scope dispositions account for
later commits that touched Story 9.8-listed paths without a `9.8` identifier; sprint
tracking remains read-only.
