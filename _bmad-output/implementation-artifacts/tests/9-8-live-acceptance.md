# Story 9.8 composed and live acceptance evidence

Recorded at 2026-08-26T15:38:44Z against full repository HEAD
`6891baef28a35d4dcfc72842e454103beca54d8f`. The working tree was intentionally
dirty because the Story 9.8 candidate had not been committed; no commit was created
by the implementation session.

## Live result

- Command: `./eng/run-epic9-live-proof.sh`
- Aspire CLI: `13.4.6+87fe259e4fc244c599019a7b1304c85a1488f248`
- .NET SDK: `10.0.302`
- Node: `v26.4.0`
- Discovered resource: `counter-web-qavbxgws`
- Discovered endpoint: `https://localhost:37287`
- Start mode: `isolated-no-build-after-serialized-build`
- Browser result: 1 passed in 8.6 seconds
- Artifact validator: passed
- AppHost safety: preflight found no FrontComposer AppHost; the script stopped only
  the exact isolated AppHost it started; post-run `aspire ps --format Json` returned
  `[]`.

The standard isolated start first encountered the repository's existing parallel
static-web-assets contention. The script retained that failure, ran the documented
serialized AppHost build, and then started the same AppHost with `--no-build`. This
was an orchestration fallback, not a focused-test substitution.

The generated create carried fresh exact key
`counter-e9-1787758725360`; the key was absent from the already-rendered grid before
dispatch. The provider returned that same pre-dispatch key. The live row materialized
with count 41, reached 44 after two overlapping updates, then reached 52 after a later
update. Browser evidence records one `role="status"`, `aria-live="polite"`
announcement, first-wins visible count 1, tenant `counter-demo`, user `demo-user`, and
materialization dismissal. Command payload values are redacted. Structured logs
contain only command type and terminal result at event 9801; target keys, scopes, and
payload values are not logged.

## Retained artifacts

The local bundle is `artifacts/epic-9/`; CI uploads the same tree under the
`epic-9-live-acceptance` artifact. The bundle includes:

- `runtime-metadata.json`
- `apphost-preflight.json`, credential-redacted `apphost-start.json`, retained
  `apphost-start.failed.json`, and `apphost-serialized-build.log`
- credential-redacted `counter-web-describe.json` and
  `counter-web-logs.redacted.json`
- `junit.xml` and `playwright-report/index.html`
- `epic-9-command-evidence.json`, `epic-9-live-acceptance.png`, and `trace.zip`
- `checksums.sha256` covering every retained file

Selected SHA-256 values:

```text
13fef2e0e64870f0328731ab8e8e5be6b0ee647cd93971210d9bc70516c8d75b  runtime-metadata.json
7961fa9a1292f6e177c5b00b576ff0d7e7207538a697990c813d9642d09bdb5a  counter-web-describe.json
1b5ca7fbd3159a2b89b4efd0e7638c0d0a6d09b1918de1a3e4b4338e92452646  counter-web-logs.redacted.json
ec5668ebb32ccb9b8b66d5285ac3537f0eac543669c928cd2dc519364e1faff6  junit.xml
57c6305dad146f976bab80a00943a680a5610acf42247f72ad1418aeb24cabbe  epic-9-command-evidence.json
cac6564137ebb65193935903de1ac216d1a8e8c109e07af8b5e4965006c930ea  epic-9-live-acceptance.png
37e3eeb911c789a1e6c2a9a2c8e49e695366c72bc61c723e67a76ee12622ba69  trace.zip
```

`(cd artifacts/epic-9 && sha256sum -c checksums.sha256)` passed for the complete
bundle. `npm --prefix tests/e2e run validate:epic-9-artifacts -- artifacts/epic-9`
also passed.

## Composed and repository verification

- `npm --prefix tests/e2e run typecheck` -- passed.
- `dotnet restore tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj -m:1 -p:NuGetAudit=false -p:CentralPackageTransitivePinningEnabled=false` followed by the Release `FcNipRowIdentityProducerContractTests` filter -- 5/5 passed.
- Release `Epic9CompositionTests`, seeded CounterPage contract, and identifier seal
  filters with serialized build and central transitive pinning disabled -- 4/4 passed.
- Full Release `Hexalith.FrontComposer.Shell.Tests` fallback with serialized build,
  `-p:NuGetAudit=false`, and
  `-p:CentralPackageTransitivePinningEnabled=false` -- 2,656/2,657 passed in 2m37s.
  The only failure is the unrelated
  `CiGovernanceTests.ReleaseWorkflow_DelegatesToReusableDomainReleaseAfterCiGate`:
  root release workflows contain Builds SHA
  `4eb33928a1d8c7775f97221cf9edc171db0cb5f8`, which does not equal the approved
  current Builds submodule SHA.
- Exact default command
  `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --configuration Release --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"`
  -- blocked during restore by existing `NU1109`: `FsCheck.Xunit.v3 3.3.4`
  requires `FsCheck 3.3.4`, while the root central catalog selects `FsCheck 3.3.3`.
  Story 9.8 did not edit dependencies or submodules.

## Candidate reconciliation

The story-artifact validator is run separately against `--candidate HEAD`. Because
repository policy forbids this implementation session from creating a commit, the
live metadata records the full current HEAD and `workingTreeDirty: true`. A final
post-commit live refresh is recommended if the integrator requires the browser
bundle's `candidateCommit` to identify a clean Story 9.8 commit rather than the
baseline HEAD plus the reconciled workspace snapshot.
