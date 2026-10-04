---
title: 'Fix browser failures in Actions run 37211983182'
type: 'bugfix'
created: '2026-10-04'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The browser regression job in Actions run 37211983182 fails because dedicated Counter hosts return HTTP 500 on `/counter`. An empty specimen enablement configuration value reaches a Boolean binder in the sample command service and throws during rendering. The Epic 9 job also fails to compile the then-pinned Parties and EventStore source graph; the current branch has newer submodule pins that need verification.

**Approach:** Use the shared specimen route enablement check in the sample command service so blank configuration fails closed. Re-run the focused browser cases and verify the current source graph against the Epic 9 build failure.

</frozen-after-approval>

## Implementation Notes

- Counter sample now uses `FrontComposerSpecimenRoutes.IsEnabled` and treats blank outcome flags as disabled. The focused Chromium browser regressions for the custom Counter hosts and the mapped rejection passed, including the non-development debug host. The submission fixture now supplies a blank value for its disabled outcome flag; the mapped-rejection recovery case passed with this configuration.
- The Epic 9 source-graph failure in run 37211983182 was resolved by the newer EventStore and Parties gitlinks already present on `main` at `c7ad78a`; the subsequent Quality run 37221895782 passed `epic9-live-acceptance`.
- The subsequent Quality run exposed stale EventStore runtime identity assertions. The workflow, evidence script, governance test, and pact reference now agree with the current EventStore and Builds revisions; the focused governance test, 208 evidence-script tests, and `actionlint` passed.
- The first repaired push reached Tier 1 tests and exposed a random invalid NuGet test version when an eight-character GUID prefix began with `0`. The package-boundary test now prefixes the random identifier with a letter; its five focused cases passed locally.
- The same push's Quality run passed the browser, accessibility, and Epic 9 jobs, then its authenticated AppHost smoke build stopped at Memories' check for nested `Hexalith.McpCli`. Memories now skips its two unused nested checkout guards when hosted by FrontComposer. The root AppHost source build passed locally with the same source-reference properties and without initializing nested submodules.
- The CI dependency-governance job also exposed Parties and Tenants UI references to FrontComposer APIs absent from the published 4.5.0 packages. Both modules now build with those packages while retaining the newer route configuration and aliases in source mode. Parties package and source builds passed; Tenants standalone package build and source-mode UI build passed. Tenants' generated-surface tests passed in both modes. Its full source-mode UI suite had one timing failure in 3,948 cases; the affected test class passed all 108 cases on rerun. The full 4.5.0 package-mode suite fails 151 cases because the package lacks the `FcPageTabs.ModuleRoute` behavior used by current Tenants pages. The package-mode fallback omits host route reservations and Tenants canonical aliases until consumers use a FrontComposer package containing those APIs.

## Review Triage Log

- Blank outcome flags could reproduce the Counter 500: fixed with fail-closed string comparison.
- A direct service-level blank-setting test was suggested; the existing focused Chromium hosts exercise blank enablement through HTTP, and the mapped-rejection case now exercises a blank disabled outcome flag. Both passed after the change.
- The Epic 9 and module-build evidence is recorded above. The submodule gitlinks must be updated after their owning repositories are committed.
- Findings about the concurrently edited Story 13.3 spec and deferred-work ledger belong to that separate work and are not part of this CI repair.
