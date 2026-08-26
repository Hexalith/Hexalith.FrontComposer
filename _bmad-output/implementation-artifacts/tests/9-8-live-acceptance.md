# Story 9.8 composed and live acceptance evidence

## Acceptance state

Final acceptance remains open until the strict proof is rerun against the clean,
committed Story 9.8 candidate. The refreshed run below is intentionally labeled
`development`; it proves the isolated runtime path and the new assertions without
misrepresenting dirty baseline commit
`1cc9c2774ca6368322b7aa7b2e89cee4a5f5fbf3` as final evidence.

The historical bundle in `artifacts/epic-9/` remains preserved. The successful
development bundle is isolated in `artifacts/epic-9-refresh-2/`; it has not been
promoted over the historical final-evidence location.

## Development live result

- Command: `FC_EPIC9_ARTIFACT_ROOT=/home/administrator/projects/hexalith/frontcomposer/artifacts/epic-9-refresh-2 FC_EPIC9_REQUIRE_CLEAN=false ./eng/run-epic9-live-proof.sh`
- Recorded at: `2026-08-26T22:25:55Z`
- Candidate commit: `1cc9c2774ca6368322b7aa7b2e89cee4a5f5fbf3`
- Evidence mode: `development`
- Working tree dirty: `true`
- Aspire CLI: `13.4.6+87fe259e4fc244c599019a7b1304c85a1488f248`
- .NET SDK: `10.0.302`
- Node: `v26.4.0`
- Discovered resource: `counter-web-zpmhfgeh`
- Discovered endpoint: `https://localhost:40277`
- Start mode: `isolated-no-build-after-serialized-build`
- Browser result: 1 passed in 11.3 seconds
- Evidence tooling: 40/40 artifact-validator and proof-runner cases passed, then the live bundle passed
- Checksums: every retained file passed `sha256sum -c checksums.sha256`
- AppHost safety: preflight returned `[]`; post-run `aspire ps --format Json --non-interactive --nologo` returned `[]`

The standard isolated start encountered the repository's known parallel
static-web-assets contention. The script retained that failure, completed the
serialized AppHost build, started the same isolated AppHost with `--no-build`, and
stopped it on exit.

The generated create used fresh exact key `counter-e9-1787783158013`, which was
absent from the already-rendered grid before dispatch and matched all four recorded
create/update dispatches. The row materialized at 41, reached 44 after the two
overlapping updates, and reached 52 after the later update. The browser observed one
first-wins announcement with localized copy, `role="status"`,
`aria-live="polite"`, the expected accessible label, tenant `counter-demo`, user
`demo-user`, and materialization dismissal after every phase. Command payload values
are `[REDACTED]` in the retained browser evidence.

CI uploads the corresponding final bundle under artifact name
`epic-9-live-acceptance-artifacts` for 14 days.

Selected development-bundle SHA-256 values:

```text
4ee52fdbf6f31ec1a1f7bb5d09dad25eaed57020ed1e09589f13ef45f7fec7b0  runtime-metadata.json
a4d30a1071540399801db21f492bd5b8a9635b1f23345aaf90df992aeb7f0190  counter-web-describe.json
14c7c56072ad3cac8c43b732fd91269461bce8282a342837b06900c21df93633  counter-web-logs.redacted.json
411965c877d7f71e17f8adc79684b83d37376e3987af2341fd8390d5e08aea4f  junit.xml
0ed65ca3925711e6928002b3702c7cda40d056df95661a59ca577f47602977c1  epic-9-command-evidence.json
4622174051b8b88b5a069eb12e81577f7301ca9c9baba6e9beaefa111991cc3a  epic-9-live-acceptance.png
d5c0c0515561fa693e75e6a2e16c9fa711e8cdde066335c268f7c3efba5b87d4  trace.zip
```

## Composed and repository verification

- `npm --prefix tests/e2e run typecheck` -- passed.
- `npm --prefix tests/e2e run test:epic-9-evidence` -- 40/40 passed, including missing, contradictory, stale, sensitive, wrong-type, dirty-candidate, and unrelated-AppHost fixtures.
- Release build of `Hexalith.FrontComposer.Shell.Tests.csproj` with serialized build, NuGet audit disabled, and central transitive pinning disabled -- passed with 0 warnings and 0 errors.
- Built xUnit `Epic9CompositionTests` class filter -- 2/2 passed.
- Built xUnit `CounterPage_SeedState_RendersUnrelatedRow` filter -- 1/1 passed.
- Built xUnit `AnalyzerPolicy_IdentifierInventory_MatchesSeal` filter -- 1/1 passed after the intentional Story 9.8 test-source reseal.
- Exact solution default lane -- blocked during restore by pre-existing `NU1109`: `FsCheck.Xunit.v3 3.3.4` requires `FsCheck 3.3.4`, while the central catalog selects `FsCheck 3.3.3`.

## Final gate still required

Strict preflight was exercised while the implementation was uncommitted. It exited
2 before starting Aspire with:

```text
Epic 9 candidate preflight failed: HEAD=1cc9c2774ca6368322b7aa7b2e89cee4a5f5fbf3 expected=1cc9c2774ca6368322b7aa7b2e89cee4a5f5fbf3 dirty=true mode=final
```

After the implementation candidate is committed, run the spec's exact strict proof
against a new empty artifact root, verify the bundle and checksums, promote the fresh
bundle intentionally, replace the development metadata above with final metadata,
and only then close Story 9.8, Epic 9, FR-13, and FR-26.
