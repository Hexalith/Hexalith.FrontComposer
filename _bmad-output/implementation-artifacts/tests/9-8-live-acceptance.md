# Story 9.8 composed and live acceptance evidence

## Acceptance state

Final live acceptance remains open until the hardened fallback is committed and rerun
in strict mode. A complete development-mode diagnostic passed against HEAD
`9d410d223f214f85695b13ede98dc8b63fbfc1c7` with the intended uncommitted Story 9.8
tooling changes present. The browser, runtime metadata, Aspire resource description,
redacted logs, JUnit/HTML reports, screenshot, trace, and checksums correlate to that
diagnostic candidate and discovered endpoint, but the dirty flag prevents final use.

The historical bundle in `artifacts/epic-9/`, the former final bundle in
`artifacts/epic-9-final-f4f43fdc/`, and all development bundles remain preserved.
The current validator rejects the former final bundle's non-canonical checksum paths,
so it is historical evidence only. The latest diagnostic bundle is
`artifacts/epic-9-development-fallback-fix/`; CI produces
`epic-9-live-acceptance-artifacts` uploads for 14 days.

## Latest development live result

- Command: `FC_EPIC9_ARTIFACT_ROOT=/home/administrator/projects/hexalith/frontcomposer/artifacts/epic-9-development-fallback-fix FC_EPIC9_REQUIRE_CLEAN=false FC_EPIC9_EXPECTED_COMMIT=9d410d223f214f85695b13ede98dc8b63fbfc1c7 ./eng/run-epic9-live-proof.sh`
- Recorded at: `2026-08-27T22:14:19Z`
- Candidate commit: `9d410d223f214f85695b13ede98dc8b63fbfc1c7`
- Evidence mode: `development`
- Working tree dirty: `true`
- Aspire CLI: `13.4.6+87fe259e4fc244c599019a7b1304c85a1488f248`
- .NET SDK: `10.0.302`
- Node: `v26.4.0`
- Discovered resource: `counter-web-cszteqad`
- Discovered endpoint: `https://localhost:33145`
- Start mode: `isolated-no-build-after-serialized-build`
- Browser result: 1 passed in 10.8 seconds
- Evidence tooling: 85/85 artifact-validator and proof-runner cases passed
- Artifact validator: development bundle passed against the exact candidate with `--allow-dirty`
- Checksums: every retained file passed `sha256sum -c checksums.sha256`
- AppHost safety: preflight and postflight returned `[]`; the proof stopped only its exact isolated AppHost

The standard isolated start encountered the repository's known parallel
static-web-assets contention. The script retained that failure, built the missing
EventStore Aspire dependency serially, built the AppHost without re-entering the
collision-prone dependency graph, started the same isolated AppHost with `--no-build`,
and stopped it before artifact validation.

The generated create used fresh exact key `counter-e9-1787868861939`, which was
absent from the already-rendered grid before dispatch and matched all four recorded
create/update dispatches. The row materialized at 41, reached 44 after two
overlapping updates, and reached 52 after the later update. The browser observed one
first-wins announcement with localized copy, `role="status"`,
`aria-live="polite"`, the expected accessible label, tenant `counter-demo`, user
`demo-user`, and materialization dismissal after every phase. Command payloads are
`[REDACTED]` in retained browser evidence.

Selected development-bundle SHA-256 values:

```text
e554df67fb59f8d1178976edea2e7a4796303a2da160f3863c707cb1672c06a1  runtime-metadata.json
03a839bdc4628b96bd8c15d9da7d12bfb0b64e4c2429567a4af760f955109d1f  counter-web-describe.json
2daf808edcfc53d344477f527ef44078415f9322e5c4f186d7bee74130d8a346  counter-web-logs.redacted.json
de86fd6f27270cd664137c4bebd6024b7531f6679e8e271e277cae9907e8fdc2  junit.xml
922b9621a8807be17855d11c9bdd0b129c0aad58c114d0ca6e2d28d703801876  epic-9-command-evidence.json
d3f46e439cf198e84cb1d0db9e07f6d09e6ad9d6ea833ecf964a7e7310bbd4f4  epic-9-live-acceptance.png
a65eff86360ef275905691d662465f2b33d997ab2c9e79c8e231fa93c611e16e  trace.zip
```

## Composed and repository verification

- `npm --prefix tests/e2e run typecheck` -- passed.
- `npm --prefix tests/e2e run test:epic-9-evidence` -- 85/85 passed, including missing, contradictory, stale, sensitive, wrong-type, dirty-candidate, fallback-dependency, and unrelated-AppHost cases.
- Release build of `Hexalith.FrontComposer.Shell.Tests.csproj` with serialized build, NuGet audit disabled, and central transitive pinning disabled -- passed with 0 warnings and 0 errors.
- Built xUnit `Epic9CompositionTests` class filter -- 2/2 passed.
- Built xUnit `CounterPage_SeedState_RendersUnrelatedRow` filter -- 1/1 passed.
- Built xUnit `AnalyzerPolicy_IdentifierInventory_MatchesSeal` filter -- 1/1 passed after the intentional Story 9.8 test-source reseal.
- Exact solution default lane -- blocked during restore by pre-existing `NU1109`: `FsCheck.Xunit.v3 3.3.4` requires `FsCheck 3.3.4`, while the central catalog selects `FsCheck 3.3.3`.

## Candidate reconciliation

The latest browser bundle proves the live behavior and repaired fallback in explicit
development mode only. Final acceptance requires committing the current Story 9.8
tooling and evidence-document changes, running the same proof with
`FC_EPIC9_REQUIRE_CLEAN=true` and the exact new commit as
`FC_EPIC9_EXPECTED_COMMIT`, then replacing this section with the strict bundle's
candidate, endpoint, results, and checksums. Commit-scope dispositions must also
account for later commits that touched Story 9.8-listed paths without a `9.8`
identifier; sprint tracking remains read-only.
