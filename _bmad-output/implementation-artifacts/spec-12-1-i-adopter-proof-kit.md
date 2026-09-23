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

## Implementation Notes

- Added a copyable v1 package consumer with generated Proof projection and command routes, a real three-call host, and missing/misordered/empty bootstrap modes. The proof keeps the EventStore service registration but only asserts rendering.
- The runner verifies nuspec candidate metadata, compares restored package SHA-512 values with the candidate archives, records exact archive hashes and .NET runtime identity, and emits allowlisted result fields.
- Verification on 2026-09-24: `python3 -m unittest tests/eng/test_adopter_proof.py` passed 7 tests; a real local proof against candidate `171bfce89ba042f3cdd03dd182808d7c95389df6` and package version `0.0.0-ci-test` passed all four assertions; independent comparison matched four archive hashes. The implementation agent also reported a Debug solution build with zero warnings/errors and focused bootstrap, service graph, empty-shell, packaged consumer, and authentication suites passing.
- This local candidate run validates the kit. The selected external adopter maintainer has not run it; `EXT-ADOPTER-1` remains external.
- Review patches added exact startup diagnostics, a real EventStore-registration guard, generated navigation and HTML checks, bounded commands, exact runtime pinning, robust package/HTTP failure handling, and the blocking PR test lane. Final verification: 16 runner tests, Debug solution build (zero warnings/errors), real local package proof (four assertions), and archive hash comparison passed. Review deferred no findings; the intentionally self-declared nuspec provenance limitation is recorded as a rejected low-risk finding in the triage log.

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
