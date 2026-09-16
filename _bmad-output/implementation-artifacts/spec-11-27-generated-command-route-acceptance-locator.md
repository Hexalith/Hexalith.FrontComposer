---
title: 'Story 11.27: Generated Command Route Acceptance Locator'
type: 'bugfix'
created: '2026-09-16'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-11-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The Story 11.7 Playwright route acceptance test uses page-wide `Counter` heading locators that collide with the shell banner in the AppHost Development environment, so the automated proof stops before command activation even though the canonical generated route works.

**Approach:** Scope both route-heading assertions to the shell's unique main landmark while preserving tenant setup, the module and projection journey, palette activation, the exact `/commands/Counter/ConfigureCounterCommand` URL assertion, and the labeled generated command-form assertion.

</frozen-after-approval>

## Implementation Notes

- Replaced both page-wide `Counter` heading assertions in `tests/e2e/specs/route-contract.spec.ts` with one reusable semantic locator scoped through the shell's unique `main` landmark and constrained to an exact level-one heading. This proves the route heading rather than selecting the shell banner by position.
- Preserved the tenant fixture, module-root and projection-flyout checks, palette search and activation, anchored `/commands/Counter/ConfigureCounterCommand` URL assertion, and labeled generated-form assertion. No application markup, route generation, palette behavior, dependencies, or submodules changed.
- Verification on 2026-09-16 against baseline revision `aba2929141a6178edace541f59ced20e1815d45a` plus this story's working-tree diff: `npm --prefix tests/e2e run typecheck` passed; `dotnet build src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --configuration Debug --no-restore -m:1 /nr:false -p:NuGetAudit=false` passed with 0 warnings and 0 errors; `aspire start --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --no-build --format Json --non-interactive`, `aspire wait counter-web --status healthy --timeout 120 --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --non-interactive`, and focused `aspire describe` confirmed a Healthy Development resource at the discovered `http://localhost:5201`; `PLAYWRIGHT_SKIP_WEBSERVER=1 BASE_URL=http://localhost:5201 npm --prefix tests/e2e run test:route-contract` passed 1/1 in Chromium and retained the canonical route/form proof.
- The first AppHost start encountered a transient concurrent-build lock on `Hexalith.FrontComposer.Shell.dll`; after the required `aspire stop` check, a serialized AppHost build and `aspire start --no-build` succeeded. The topology was stopped cleanly after browser verification.

## Review Triage Log

- **false — tenant context is not established:** Story 11.27 requires the existing tenant fixture setup to remain intact, not a new authentication/session contract. The unchanged fixture still supplies and asserts its tenant identity; adding `seedDemoSession` would expand this locator-only story without addressing an observed route failure.
- **false — locator does not prove `#fc-main-content`:** The story requires one route-content heading, not an ID-contract assertion. The route page is rendered beneath the shell's single semantic `main`, and Playwright's visibility assertion is strict, so zero or multiple matching route headings fail rather than being hidden by positional selection.
- **low — verification lacked revision and launch details:** Patched by recording the full baseline revision, exact serialized build, AppHost start/wait/discovery commands, discovered Development endpoint, endpoint-bound Playwright command, and pass count. The final Git commit co-locates that evidence with the tested locator change.
- **low — mixed line endings:** Patched by normalizing every changed text file to the repository's required CRLF format; `git diff --check` passes.
