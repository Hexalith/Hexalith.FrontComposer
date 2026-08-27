# Story 9.8 composed and live acceptance evidence

## Acceptance state

Final live acceptance passed against clean committed candidate
`f4f43fdc3053e45ffb939b718c670afb4cfcecd0`. The browser, runtime metadata,
Aspire resource description, redacted logs, JUnit/HTML reports, screenshot, trace,
and checksums are correlated to that exact candidate and discovered endpoint.

The historical bundle in `artifacts/epic-9/` and the development bundles remain
preserved. The final local bundle is
`artifacts/epic-9-final-f4f43fdc/`; CI produces the equivalent
`epic-9-live-acceptance-artifacts` upload for 14 days.

## Final live result

- Command: `FC_EPIC9_ARTIFACT_ROOT=/home/administrator/projects/hexalith/frontcomposer/artifacts/epic-9-final-f4f43fdc FC_EPIC9_REQUIRE_CLEAN=true FC_EPIC9_EXPECTED_COMMIT=f4f43fdc3053e45ffb939b718c670afb4cfcecd0 ./eng/run-epic9-live-proof.sh`
- Recorded at: `2026-08-26T22:38:07Z`
- Candidate commit: `f4f43fdc3053e45ffb939b718c670afb4cfcecd0`
- Evidence mode: `final`
- Working tree dirty: `false`
- Aspire CLI: `13.4.6+87fe259e4fc244c599019a7b1304c85a1488f248`
- .NET SDK: `10.0.302`
- Node: `v26.4.0`
- Discovered resource: `counter-web-pxxzyuye`
- Discovered endpoint: `https://localhost:41107`
- Start mode: `isolated-no-build-after-serialized-build`
- Browser result: 1 passed in 11.9 seconds
- Evidence tooling: 40/40 artifact-validator and proof-runner cases passed
- Artifact validator: final bundle passed against the exact candidate
- Checksums: every retained file passed `sha256sum -c checksums.sha256`
- AppHost safety: preflight and postflight returned `[]`; the proof stopped only its exact isolated AppHost

The standard isolated start encountered the repository's known parallel
static-web-assets contention. The script retained that failure, completed the
serialized AppHost build, started the same isolated AppHost with `--no-build`, and
stopped it before artifact validation.

The generated create used fresh exact key `counter-e9-1787783889542`, which was
absent from the already-rendered grid before dispatch and matched all four recorded
create/update dispatches. The row materialized at 41, reached 44 after two
overlapping updates, and reached 52 after the later update. The browser observed one
first-wins announcement with localized copy, `role="status"`,
`aria-live="polite"`, the expected accessible label, tenant `counter-demo`, user
`demo-user`, and materialization dismissal after every phase. Command payloads are
`[REDACTED]` in retained browser evidence.

Selected final-bundle SHA-256 values:

```text
14ca9f0c7a0c5803c57e70ce33b8a1fd0af331d7468268b0b1136900cbed5ae0  runtime-metadata.json
3e845ad37ba8fcddfe951dc9599b573ed8273ac55fa177ad3a1a0856c9f4d5a1  counter-web-describe.json
bd0d678c691e2880b134384d48302d5e6b82c92578ae9cfde3017910ba260696  counter-web-logs.redacted.json
6a65d08c600f95d548e5ab1324fa51a65cf0cbbb3ade4c87d63be5719903cf12  junit.xml
e7f0c8652c2aa6eec9a8d1f1b280b6cea0be09bdf1619f56f17c63f0cb812d61  epic-9-command-evidence.json
b94036d3c1c1e8b954c80c8a7466e25b85c601cae6c2aff44488d85629d908fd  epic-9-live-acceptance.png
99e3ef04b350ea7a493bc541867f3e446d43d2708e3f4ef21ba41405d71a1392  trace.zip
```

## Composed and repository verification

- `npm --prefix tests/e2e run typecheck` -- passed.
- `npm --prefix tests/e2e run test:epic-9-evidence` -- 40/40 passed, including missing, contradictory, stale, sensitive, wrong-type, dirty-candidate, and unrelated-AppHost cases.
- Release build of `Hexalith.FrontComposer.Shell.Tests.csproj` with serialized build, NuGet audit disabled, and central transitive pinning disabled -- passed with 0 warnings and 0 errors.
- Built xUnit `Epic9CompositionTests` class filter -- 2/2 passed.
- Built xUnit `CounterPage_SeedState_RendersUnrelatedRow` filter -- 1/1 passed.
- Built xUnit `AnalyzerPolicy_IdentifierInventory_MatchesSeal` filter -- 1/1 passed after the intentional Story 9.8 test-source reseal.
- Exact solution default lane -- blocked during restore by pre-existing `NU1109`: `FsCheck.Xunit.v3 3.3.4` requires `FsCheck 3.3.4`, while the central catalog selects `FsCheck 3.3.3`.

## Candidate reconciliation

The final browser bundle proves the committed implementation candidate
`f4f43fdc3053e45ffb939b718c670afb4cfcecd0`. The subsequent evidence/spec commit
contains documentation and workflow bookkeeping only; Story 9.8's strict artifact
validator reconciles both commits and their exact changed paths from baseline
`1cc9c2774ca6368322b7aa7b2e89cee4a5f5fbf3`.
