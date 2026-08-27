# Story 9.8 composed and live acceptance evidence

## Acceptance state

Final live acceptance passed in strict mode against clean committed candidate
`7a5737630611b4d54b0180a3fa4c9c4ccd23a28c`. The browser, runtime metadata,
Aspire resource description, redacted logs, JUnit/HTML reports, screenshot, trace,
and checksums correlate to that exact candidate and discovered endpoint.

The historical bundle in `artifacts/epic-9/`, the former final bundles in
`artifacts/epic-9-final-f4f43fdc/` and `artifacts/epic-9-final-cd67e933/`, and all development bundles remain preserved.
The current validator rejects the former final bundle's non-canonical checksum paths,
so it is historical evidence only. The accepted final bundle is
`artifacts/epic-9-final-7a573763/`; CI produces
`epic-9-live-acceptance-artifacts` uploads for 14 days.

## Final live result

- Command: `FC_EPIC9_ARTIFACT_ROOT=/home/administrator/projects/hexalith/frontcomposer/artifacts/epic-9-final-7a573763 FC_EPIC9_REQUIRE_CLEAN=true FC_EPIC9_EXPECTED_COMMIT=7a5737630611b4d54b0180a3fa4c9c4ccd23a28c ./eng/run-epic9-live-proof.sh`
- Recorded at: `2026-08-27T22:35:38Z`
- Candidate commit: `7a5737630611b4d54b0180a3fa4c9c4ccd23a28c`
- Evidence mode: `final`
- Working tree dirty: `false`
- Aspire CLI: `13.4.6+87fe259e4fc244c599019a7b1304c85a1488f248`
- .NET SDK: `10.0.302`
- Node: `v26.4.0`
- Discovered resource: `counter-web-qznzgcbv`
- Discovered endpoint: `https://localhost:39831`
- Start mode: `isolated-no-build-after-serialized-build`
- Browser result: 1 passed in 9.6 seconds
- Evidence tooling: 87/87 artifact-validator and proof-runner cases passed
- Artifact validator: final bundle passed against the exact clean candidate
- Checksums: every retained file passed `sha256sum -c checksums.sha256`
- AppHost safety: preflight and postflight returned `[]`; the proof stopped only its exact isolated AppHost

The standard isolated start encountered the repository's known parallel
static-web-assets contention. The script retained that failure, built the missing
EventStore Aspire dependency serially, built the AppHost without re-entering the
collision-prone dependency graph, started the same isolated AppHost with `--no-build`,
and stopped it before artifact validation.

The generated create used fresh exact key `counter-e9-1787870139947`, which was
absent from the already-rendered grid before dispatch and matched all four recorded
create/update dispatches. The row materialized at 41, reached 44 after two
overlapping updates, and reached 52 after the later update. The browser observed one
first-wins announcement with localized copy, `role="status"`,
`aria-live="polite"`, the expected accessible label, tenant `counter-demo`, user
`demo-user`, and materialization dismissal after every phase. Command payloads are
`[REDACTED]` in retained browser evidence.

Selected final-bundle SHA-256 values:

```text
12e309c4fee1d0c7e5f33f357bb43c6246a38d4a7622f71c1811b8719d17b1a5  runtime-metadata.json
f8afa3421b1885fb7065ab4356783ea81c52da36f32e68d1843f63edfe174f95  counter-web-describe.json
ce7fd5e91b3e519f7bf670771fae72a3b6439cfcaaaab05ea443907436d74463  counter-web-logs.redacted.json
f40b0752611fe2d7eca20b924924b3e7a77e4fde4e711744529aa9c1bcc1cc71  junit.xml
8b145321e07fefb28131514ff73d21b8d03dfd62b93d0e8733c9f770ed2b4722  epic-9-command-evidence.json
85bc1a7262b59fe414cfbb86196b392e09975f9d57aaf1af782ebc5163865b8c  epic-9-live-acceptance.png
276b6a3c30eb1d5571a4277495a22a49b0e791ee7c0c1bb20a257cec618634d4  trace.zip
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
