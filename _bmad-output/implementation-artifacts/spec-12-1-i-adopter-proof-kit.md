---
title: 'Story 12.1: Deterministic Three-Call Adopter Proof Kit'
type: 'feature'
created: '2026-09-23'
status: 'draft'
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
- [ ] `samples/AdopterProofKit/v1/AdopterProofKit.Domain/{AdopterProofKit.Domain.csproj,ProofDomain.cs,ProofProjection.cs,CreateProofCommand.cs}` -- define a minimal annotated domain whose projection and command are generated from candidate packages, with one C# type per file.
- [ ] `samples/AdopterProofKit/v1/AdopterProofKit.Web/{AdopterProofKit.Web.csproj,Program.cs,Components/App.razor,Components/Routes.razor,Components/Layout/MainLayout.razor}` -- wire a standalone package-based host with Quickstart → domain → EventStore, framework Shell, and generated routes; accept package version/source and EventStore endpoint as inputs.
- [ ] `eng/adopter_proof.py` -- check full candidate SHA against package provenance, start and probe the fixture, assert generated projection and command surfaces, cover startup-negative and empty-registry cases, and emit only allowlisted result fields.
- [ ] `docs/how-to/adopter-bootstrap-proof.md` and `docs/how-to/index.md` -- publish copyable package-source, candidate, runtime, execution, and result instructions; name the selected external maintainer as the owner of independent proof.
- [ ] `tests/eng/test_adopter_proof.py` -- verify candidate mismatch, missing render assertion, bootstrap failure, empty-registry handling, and result redaction through the runner; cite existing focused regressions rather than recreating them.
- [ ] `_bmad-output/implementation-artifacts/sprint-status.yaml` -- update the story state as the workflow advances; leave EXT-ADOPTER-1 external until dated adopter evidence exists.

**Acceptance Criteria:**
- Given the clean kit and exact candidate inputs, when an adopter follows the documented procedure, then the host starts through the supported calls and the result independently identifies a rendered generated projection and command.
- Given an absent or misordered required stage, when the host starts, then the named startup diagnostic appears before first render and the kit records failure; given no domain, the existing empty-shell state remains valid.
- Given a proof run, when its result is saved, then it contains the candidate SHA, verified package/runtime identity, date, assertions, and pass/fail, with no sensitive or machine-specific fields.
- Given existing focused generator, bootstrap, Shell, and authentication tests, when the kit is validated, then their current results are cited where applicable without duplicating their behavior or claiming external acceptance.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- `python3 -m unittest tests/eng/test_adopter_proof.py` -- runner edge cases and redaction pass.
- `python3 eng/adopter_proof.py --help` -- documented candidate inputs and result location are available.
- `dotnet build Hexalith.FrontComposer.slnx --configuration Debug` -- existing solution compiles; the standalone fixture is built by its proof procedure.
- `git diff --check` -- no whitespace errors.
