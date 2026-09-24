---
title: 'Story 12.1: Deterministic Three-Call Adopter Proof Kit'
type: 'feature'
created: '2026-09-23'
status: 'done'
baseline_commit: 'eed4f8e73312146956905b119091271680d8e68f'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '_bmad-output/implementation-artifacts/epic-12-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The existing three-call spike proves service registration, and the Counter sample renders generated UI, but neither is a clean, candidate-bound consumer proof: Counter replaces its command service and omits EventStore registration. External adopters therefore lack a reproducible way to demonstrate the supported path and record its result.

**Approach:** Ship a versioned, package-based consumer fixture and a documented proof procedure that starts the actual three-call host, asserts generated projection and command rendering, and produces a small redacted result bound to the selected candidate. Reuse existing bootstrap, generator, Shell, and authentication regressions for behavior they already cover.

## Boundaries & Constraints

**Always:** Use an exact full candidate SHA, package/runtime identity, execution date, explicit projection and command assertions, and pass/fail in each result. Verify the supplied package identity belongs to the selected candidate; fail closed when provenance cannot be established. Keep the fixture copyable outside this repository, use FrontComposer and pinned Fluent UI V5 components, and preserve the valid empty-domain shell.

**Never:** Treat an internal fixture pass as external adopter acceptance; hand-author replacement projection or command UI; replace the EventStore command/query registration in the success path; embed tenant/user data, tokens, payloads, stack traces, or machine paths in evidence; add a new release evidence framework or greenfield starter template.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Valid proof | Clean consumer, exact candidate packages and runtime, reachable host | Host starts; generated projection and command render; dated redacted pass result records both assertions | A missing assertion fails the result |
| Bad provenance | Missing or mismatched SHA, package version, or runtime identity | No passing result is emitted | Named candidate/identity error |
| Bad bootstrap | Required Quickstart absent or stages misordered | Startup fails before first render and names the offending stage | Nonzero proof result; no false render pass |
| Empty registry | Quickstart with no domain | Shell starts and shows its existing no-modules state | No bogus missing-domain failure |

</frozen-after-approval>

## Code Map

- `src/Hexalith.FrontComposer.Shell/Extensions/{ServiceCollectionExtensions,EventStoreServiceExtensions,FrontComposerBootstrapValidator,FrontComposerBootstrapValidationGate}.cs` -- public registrations and existing hosted order guard; reuse, do not duplicate.
- `tests/Hexalith.FrontComposer.Shell.Tests/{Extensions/FrontComposerBootstrapGuardTests.cs,Extensions/FrontComposerServiceGraphTests.cs,Components/Layout/Story11BootstrapShellRenderTests.cs}` -- existing startup, lifetime, and empty-shell evidence.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/PackagedAnalyzerConsumerTests.cs` -- clean temporary package consumer and generated-source precedent; compilation alone is insufficient.
- `samples/Counter/Counter.Web/Program.cs` and `tests/e2e/specs/route-contract.spec.ts` -- generated render locators; Counter's sample command service must not enter the proof fixture.
- `docs/how-to/test-generated-components.md` and `docs/reference/components/front-composer-shell.md` -- adopter documentation conventions and Shell usage; bUnit fake-host tests are supporting evidence only.

## Tasks & Acceptance

**Execution:**
- [x] `samples/AdopterProofKit/v1/AdopterProofKit.Domain/{AdopterProofKit.Domain.csproj,ProofDomain.cs,ProofProjection.cs,CreateProofCommand.cs}` -- define a minimal annotated domain whose projection and command are generated from candidate packages, with one C# type per file.
- [x] `samples/AdopterProofKit/v1/AdopterProofKit.Web/{AdopterProofKit.Web.csproj,Program.cs,Components/App.razor,Components/Routes.razor,Components/Layout/MainLayout.razor}` -- wire a standalone package-based host with Quickstart → domain → EventStore, framework Shell, and generated routes; accept package version/source and EventStore endpoint as inputs.
- [x] `eng/adopter_proof.py` -- check full candidate SHA against package provenance, start and probe the fixture, assert generated projection and command surfaces, cover startup-negative and empty-registry cases, and emit only allowlisted result fields.
- [x] `docs/how-to/adopter-bootstrap-proof.md` and `docs/how-to/index.md` -- publish copyable package-source, candidate, runtime, execution, and result instructions; name the selected external maintainer as the owner of independent proof.
- [x] `tests/eng/test_adopter_proof.py` -- verify candidate mismatch, missing render assertion, bootstrap failure, empty-registry handling, and result redaction through the runner; cite existing focused regressions rather than recreating them.
- [x] `_bmad-output/implementation-artifacts/sprint-status.yaml` -- update the story state as the workflow advances; leave EXT-ADOPTER-1 external until dated adopter evidence exists.

**Acceptance Criteria:**
- Given the clean kit and exact candidate inputs, when an adopter follows the documented procedure, then the host starts through the supported calls and the result independently identifies a rendered generated projection and command.
- Given an absent or misordered required stage, when the host starts, then the named startup diagnostic appears before first render and the kit records failure; given no domain, the existing empty-shell state remains valid.
- Given a proof run, when its result is saved, then it contains the candidate SHA, verified package/runtime identity, date, assertions, and pass/fail, with no sensitive or machine-specific fields.
- Given existing focused generator, bootstrap, Shell, and authentication tests, when the kit is validated, then their current results are cited where applicable without duplicating their behavior or claiming external acceptance.

### Review Findings

Code review 2026-09-24, pass 2 (diff `eed4f8e7...d3f99995`). Layers: blind-hunter, edge-case-hunter, verification-gap, acceptance-auditor. None failed.

- [x] [Review][Decision→Patch] Fixture Fluent UI pin duplicates the Builds catalog — Both fixture csproj files hard-code `Microsoft.FluentUI.AspNetCore.Components(.Icons)` `5.0.0-rc.5-26219.1`, which currently matches `references/Hexalith.Builds/Props/Directory.Packages.props:242-243`. When the catalog moves forward, a candidate Shell nuspec will require a newer Fluent version. The fixture then fails restore with NU1605 (downgrade), which the runner reports only as the generic `fixture-build-failed`. The spec asks for "pinned Fluent UI V5", so there are three options: drop the direct references and inherit the Shell package's exact pin; have the runner read the pin from the candidate Shell nuspec and pass it as a property; or keep the hard-coded pin and rely on a future CI lane to catch drift. [medium; blind-hunter+verification-gap] — resolved: option 1, direct Fluent references removed; Fluent now flows from the candidate Shell nuspec pin.
- [x] [Review][Decision→Patch] No CI lane builds or runs the fixture end to end — The fixture is not in `Hexalith.FrontComposer.slnx`. The only new CI step (`.github/workflows/quality.yml:180-181`) runs runner tests that mock every dotnet and HTTP call and build their inputs from the module's own constants. Renaming `fc-home-empty-no-microservices` or the nav `data-testid`, rewording `FrontComposerBootstrapValidator` diagnostics, or making `StubCommandService` internal would leave CI green but break the next external run. Options: add a quality.yml lane now (pack candidate packages, then run `eng/adopter_proof.py`), or defer it to a follow-up story. The lane must stay out of the hash-pinned release workflows. [medium; verification-gap+blind-hunter] — resolved: option 1, blocking `Gate 4a: Adopter proof kit smoke` added after Gate 4, reusing `./nupkgs-validation`.
- [x] [Review][Patch] Copied fixture has no SDK pin, so a preview/RC or 11.x SDK is selected and rejected [eng/adopter_proof.py:318] — With no `global.json` in the temp root, `dotnet --version` returns the machine's newest SDK. An installed .NET 11 RC (current as of 2026-09) fails the `RUNTIME` regex with `runtime-identity-invalid`; a stable 11.x SDK builds with an SDK the guide doesn't allow. Fix: write a temp-root `global.json` pinning SDK major 10 with `rollForward: latestFeature` and reject any SDK that is not 10.x. [medium; edge-case-hunter+blind-hunter]
- [x] [Review][Patch] Guide documents a bare `AddHexalithFrontComposerQuickstart()`, but the host needs a Fluxor scan of the domain assembly [docs/how-to/adopter-bootstrap-proof.md:37] — `Program.cs` calls `AddHexalithFrontComposerQuickstart(o => o.ScanAssemblies(typeof(ProofDomain).Assembly))` (Counter does the same), and `AddHexalithDomain<T>()` does not scan Fluxor features. An adopter reproducing the documented sequence would drop the scan. [medium; acceptance-auditor]
- [x] [Review][Patch] Probe matrix arguments passed by `execute()` are not asserted [tests/eng/test_adopter_proof.py:250] — Only `calls[-1].args[4] == "empty"` is checked. Changing the negative loop to `("missing-quickstart", "missing-quickstart")` still records `bootstrap_rejected: true` without the misordered host ever starting. Assert the full ordered `(mode, path, expected)` five-case matrix on the passing run. [medium; verification-gap]
- [x] [Review][Patch] `bootstrap-unexpected-render` branch has no test [eng/adopter_proof.py:183] — Removing it makes a negative mode return `True` whenever the host serves `/`, so a Shell regression that stops rejecting bad bootstraps would still publish `bootstrap_rejected: true`. Add a `_start_and_probe` test where `_get` returns HTML for mode `missing-quickstart`/path `/` and assert `bootstrap-unexpected-render`. [medium; verification-gap]
- [x] [Review][Patch] Several failure paths in the spec's matrix have no runner test [tests/eng/test_adopter_proof.py:50-285] — Not tested:
  - `package-missing`
  - invalid SHA or version inputs, and that the result reports `"invalid"` rather than echoing them
  - nuspec id, version, or repository-url mismatch (only the commit case is tested)
  - `runtime-identity-mismatch` and `runtime-unavailable`
  - `eventstore-endpoint-invalid`
  - the command-not-rendered case (`[True, False]`)
  - the misordered-mode diagnostic

  Also, `test_result_is_allowlisted_and_redacted` mocks `execute()`, so the endpoint and package-source values it passes never reach the code that builds the result. Redaction is therefore not verified "through the runner" as the task requires. [medium; acceptance-auditor+blind-hunter]
- [x] [Review][Patch] `http.client.HTTPException` subclasses escape `_get` and `execute` [eng/adopter_proof.py:163] — `BadStatusLine`/`LineTooLong` are raised from `getresponse()` outside urllib's `OSError`→`URLError` wrapping. `execute` does not catch them, so `main` prints a traceback and writes no result JSON. That contradicts the documented named-failure contract. Catch `http.client.HTTPException` in `_get`. [low; edge-case-hunter+blind-hunter]
- [x] [Review][Patch] EventStore endpoint check disagrees with the host's `Uri.TryCreate` [eng/adopter_proof.py:246] — `startswith(("https://", "http://"))` accepts `http://` with no host, which the host then rejects, misreported as `host-start-or-render-failed`. It also rejects a valid `HTTPS://`. Use `urllib.parse.urlsplit` and require an http(s) scheme (any case) plus a netloc. [low; edge-case-hunter+blind-hunter]
- [x] [Review][Patch] Adopter guide omits prerequisites and locations [docs/how-to/adopter-bootstrap-proof.md:16-24] — Python 3 is not listed as a prerequisite. The guide does not say which repository holds the `tenants-bootstrap-acceptance.md` evidence path, or that the kit must be copied from the candidate SHA's checkout (a kit from another revision can disagree with the candidate's markup). [low; blind-hunter]
- [x] [Review][Decision→Patch] Kit fails against current main: Story 13.1 scope gate blocks all Shell content without an authenticated tenant scope — A real proof run on 2026-09-24 against HEAD `d3f99995` (four packages packed at `4.4.0-ci`, runtime 10.0.12, SDK 10.0.401) fails with `render-assertion-missing`: both probed pages render `fc-scope-blocked` ("Workspace unavailable") instead of the generated surfaces. The unpatched fixture fails the same way, so this predates the review patches. The cause is `fc680685` (Story 13.1, in review): `FrontComposerShell.razor` now renders `@ChildContent` only while `ScopeBoundary.IsCurrent`, which needs a non-synthetic tenant and user from `IUserContextAccessor`. The fixture has no authentication and sets `AllowDemoTenantContext = false`, so every page is blocked, including the empty-registry home the spec requires to stay valid. The kit was last proven against `171bfce8` (2026-08-29), before 13.1. Gate 4a will be red on main until this is resolved. [high; code-review pass-2 live verification] — resolved: option 1. Added `AdopterProofKit.Web/ProofUserContextAccessor.cs`, a fixed public, non-synthetic tenant `adopter-proof` and user `adopter-proof-operator`, registered through the `IUserContextAccessor` adopter seam in every mode. `AllowDemoTenantContext` stays false. The guide explains the stand-in. 13.1 follow-ups (the `FcScopeBlocked` API summary, and Quickstart docs saying a tenant/user source is required) are recorded in `deferred-work.md` against the 13.1 spec.

**Rejected (pass 2):**
- Ephemeral-port race in `_port()` (low): it can only cause a false failure, never a false pass. Fixing it would need host-output port discovery.
- Inherited `Kestrel__Endpoints__*`/`ASPNETCORE_HTTP_PORTS` env overriding the host URL (low): unlikely in an adopter shell, and fixing it adds an env-scrubbing guard.
- Production host from build output serves only prerendered HTML, with no static web assets and no interactive circuit (low): by design. The spec and guide assert rendered HTML surfaces, not interactivity.
- Leading-zero package versions (false): `package-missing` is accurate for a version string that matches no supplied archive, and the check fails closed.
- Build output elsewhere misreported as `runtime-config-invalid` (low): the fixture is built in an isolated temp dir outside any props, and fixing it adds a guard.
- `TemporaryDirectory` cleanup error turning a pass into a fail (maybe-false; low if true): would need evidence that build servers keep a handle on the temp fixture on Windows.
- In-place build of `samples/AdopterProofKit/v1` hitting NU1008 under repo CPM (low): the guide and runner always build a copy outside the repo, and fixing it adds stop-file props.
- Spec `status: done`, `review_loop_iteration: 0`, and doc `reviewed:` date disagree with the `review` sprint state (rejected): the fix edits the spec under review, and this review's status sync resolves it.
- Hand-mounted `/proof/proof-projection` route (false): if the generator's route convention changes, the manifest-specific nav `data-href` regex stops matching and the proof fails. Counter uses the same mounting pattern.
- Fixture EventStore guard checks concrete types rather than interface resolution (false): nothing in the fixture pre-registers `ICommandService`/`IQueryService`. Quickstart registers only the `StubCommandService` path, which `RemoveStubCommandService` strips and the guard checks.
- Projection fragments not unique to `ProofProjectionView` (low): in this fixture only the generated view can render `fc-projection-empty-placeholder`, and the nav tag is manifest-specific.
- Internal gate IDs and dated test counts in the guide (low): AC4 requires citing current results, and the readers are Hexalith maintainers.
- Duplicate "Gate 2b" step label and two Python test roots (false): no harm named, and the test roots predate this change.
- Nuspec-declared provenance can be forged (low): already rejected as B1. Independent attestation is a new evidence framework, which the spec's Never list forbids.
- Regression results cited from the implementation agent's report, not rerun (low): the fix would edit the spec's notes, and the review patches touched no framework code.
- `/` shows the projection instead of the shell home in success mode (false): the empty-registry branch renders the Shell's own `FcHomeDirectory` no-modules state, as the matrix requires.
- "Deterministic"/"rendered" claim versus port race and prerender-only probing (low): covered by the port-race and prerender rejections above.

## Implementation Notes

- Added a copyable v1 package consumer with generated Proof projection and command routes, a real three-call host, and missing/misordered/empty bootstrap modes. The proof keeps the EventStore service registration but only asserts rendering.
- The runner verifies nuspec candidate metadata, compares restored package SHA-512 values with the candidate archives, records exact archive hashes and .NET runtime identity, and emits allowlisted result fields.
- Verification on 2026-09-24: `python3 -m unittest tests/eng/test_adopter_proof.py` passed 7 tests; a real local proof against candidate `171bfce89ba042f3cdd03dd182808d7c95389df6` and package version `0.0.0-ci-test` passed all four assertions; independent comparison matched four archive hashes. The implementation agent also reported a Debug solution build with zero warnings/errors and focused bootstrap, service graph, empty-shell, packaged consumer, and authentication suites passing.
- This local candidate run validates the kit. The selected external adopter maintainer has not run it; `EXT-ADOPTER-1` remains external.
- Review patches added exact startup diagnostics, a real EventStore-registration guard, generated navigation and HTML checks, bounded commands, exact runtime pinning, robust package/HTTP failure handling, and the blocking PR test lane. Final verification: 16 runner tests, Debug solution build (zero warnings/errors), real local package proof (four assertions), and archive hash comparison passed. Review deferred no findings; the intentionally self-declared nuspec provenance limitation is recorded as a rejected low-risk finding in the triage log.
- Code review pass 2 (2026-09-24): applied 10 review fixes plus the Story 13.1 scope-gate resolution. Verification: `python3 -m unittest tests/eng/test_adopter_proof.py` passed 25 tests, and deliberate runner breakages were each caught by a test. Four packages packed from HEAD `d3f99995` at `4.4.0-ci` passed a real proof using the exact Gate 4a command (SDK 10.0.401 via the pinned band, runtime 10.0.12; all four assertions true; no identity, endpoint, or path in the result), and the documented independent archive-hash check matched. `git diff --check` is clean. `eng/validate-docs.ps1` fails only on the pre-existing Story 13.1 `FcScopeBlocked` summary (deferred).

## Spec Change Log

## Review Triage Log

- B1 | low, rejected: `verify_packages` checks candidate commit declared inside supplied nuspecs and records archive hashes, so a deliberately forged archive can claim a SHA; ordinary accidental mismatch is caught. Independent artifact attestation would add a new evidence framework beyond this kit, so this unlikely case does not justify that complexity here.
- B2 | medium, patch: `verify_runtime` invokes `dotnet --version` outside the copied fixture while restore/build run inside it; a differing `global.json` can make recorded SDK identity wrong. Check from the fixture working directory.
- B3 | medium, patch: installed shared runtimes do not prove the host loaded the selected patch under roll-forward. Disable roll-forward for the proof host.
- B4 | medium, patch: `_start_and_probe` accepts a still-running negative host after its deadline because `None != 0`; require an observed nonzero exit.
- B5 | medium, patch: negative checks search only method names, so the wrong startup message can satisfy them. Match the mode-specific bootstrap diagnostic.
- B6 | medium, patch: restore/build `subprocess.run` has no timeout; a stalled feed prevents any result. Bound both commands and use a named failure.
- B7 | medium, patch: `Home.razor` serves the generated projection directly, so proof can pass without a working manifest navigation entry. Verify a rendered navigation link to the projection route.
- B8 | medium, patch: the guide says to publish JSON without naming the adopter evidence location; reviewers cannot attribute an independent run from that instruction. Name the Tenants evidence path and conditional Parties equivalent.
- G1 | high, patch: Quickstart plus Domain is valid and can render with stub clients, so removing `AddEventStore()` at `Program.cs:31` can leave a passing three-call claim. Assert real EventStore command/query registrations in the fixture success path.
- G2 | medium, patch: runner tests mock `_start_and_probe`, leaving the HTTP fragment predicate untested. Add a direct controlled-response helper test for present and missing fragments.
- G3 | medium, patch: `.github/workflows/quality.yml` explicitly lists Python suites but omits `test_adopter_proof.py`. Add the new suite to the blocking PR lane.
- E1 | medium, patch: nuspec parsing and archive hashing read the same path separately; an archive swap between reads can bind unverified bytes. Parse and hash one byte snapshot.
- E2 | medium, patch: the misordered case can accept a missing-Quickstart message because it only checks method names. Use the specific misordered message, as B5.
- E3 | medium, patch: a negative host alive at the deadline can count as rejected; require exit, as B4.
- E4 | medium, patch: `dotnet --version` can select a different SDK outside the temporary fixture; inspect SDK inside it, as B2.
- E5 | medium, patch: a stalled NuGet command has no timeout; bound it, as B6.
- E6 | medium, patch: `ZipFile.read` can raise `RuntimeError` for unsupported/encrypted members, escaping the redacted result. Convert it to `package-identity-invalid`.
- E7 | medium, patch: `http.client.IncompleteRead` is not caught by `_get`, so a truncated response can escape without a redacted result. Treat it as an unavailable page.

## Verification

**Commands:**
- `python3 -m unittest tests/eng/test_adopter_proof.py` -- runner edge cases and redaction pass.
- `python3 eng/adopter_proof.py --help` -- documented candidate inputs and result location are available.
- `dotnet build Hexalith.FrontComposer.slnx --configuration Debug` -- existing solution compiles; the standalone fixture is built by its proof procedure.
- `git diff --check` -- no whitespace errors.
