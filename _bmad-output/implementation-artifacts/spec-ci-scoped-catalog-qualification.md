---
title: 'Restore FrontComposer catalog governance with scoped qualification overrides'
type: 'bugfix'
created: '2026-10-28'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="user-authorized CI repair; parent delegated the owning FrontComposer change">

## Intent

**Problem:** The adopted Builds catalog retains one literal Tenants version and a
package-only override restricted to five EventStore qualification projects. The
FrontComposer semantic validator counts both as active definitions and rejects
CI and Quality even though the scoped override cannot apply to FrontComposer.

**Approach:** Recognize only the exact inactive EventStore qualification override
in the FrontComposer catalog profile. Preserve validation of the one literal,
global default and reject duplicate defaults, altered scopes, arbitrary conditions,
nonliteral defaults, and malformed overrides. Add positive and negative regression
coverage and validate the affected graph/governance checks. Preserve policy,
package selections, historical evidence, runtime behavior, and all root gitlinks.
Use only root flat dependencies; never initialize nested submodules. Publish the
owning repair after independent review and the repository-pinned commit validator.

</frozen-after-approval>

## Implementation Notes

- Baseline: ccf69d6842b0d420d9e8a512819067f2c6c3a5ec; upstream unchanged on fetch.
- CI 40612835071 and Quality 40612834711 report the same duplicate Tenants property.
- Keep the strict generic property validator; opt in to the exact qualification
  exclusion only for frontcomposer-catalog-v1. No policy or source package edits.
- The one-shot artifact is owned by FrontComposer because the root snapshot is frozen.

- Implemented only the exact qualification exclusion in the FrontComposer profile;
  the generic helper remains strict by default. The semantic policy is unchanged.
- Regression fixtures preserve the temporary override independently of future
  catalog cleanup and require acceptance both with and without that row.
- Full dependency-graph suite: 107 tests passed in an isolated plain-file fixture
  using selected Builds catalog bytes in their required BOM/CRLF checkout form.
  No nested repository was initialized or checked out.
- Actual committed policy qualification: 51 edges and seven Builds selectors pass.
  The policy bytes and all semantic checks remain unchanged; a read-only resolver
  adapter maps trusted identities to existing root-declared flat repositories.
- Shell test-project Debug build with -warnaserror: zero warnings and errors.
  Native focused InfrastructureGovernance tests: 19 passed, zero skips/errors.
- The exact central-policy C# wrapper cannot resolve its nested AI.Tools repository
  in this intentionally flat workspace (fatal: not a tree object); no guard changed.
  Its canonical Python engine and actual policy pass with the flat location adapter.
- Parent independent review accepted the production diff before commit/push.

## Review Triage Log

- medium, patch: temporary-row coupling in added tests would fail after upstream
  removes the qualification hook. Dedicated synthetic fixture now tests both states.
- medium, patch: generic helper strictness did not test caller opt-in. A distinct
  strict profile now exercises evaluate_semantics and must reject the override.
- medium, patch: qualification-placement guards lacked Target/Choose cases. Both
  now reject, alongside conditional placement, duplicate qualifiers/defaults, and
  changed scope/value; missing or nonliteral defaults also remain failures.
