# Reviewer Gate — Rubric Pass 3

**Target:** `ARCHITECTURE-SPINE.md`  
**Review date:** 2026-07-19  
**Scope:** latest semantic-profile, Release/NuGet argv, catalog materialization, authenticated handoff,
manifest/fallback, bootstrap, and workflow-provenance revisions. AD-1 remains the intentional
ratification gate and is not re-scored.

## Verdict

**CHANGES REQUIRED:** the previous semantic-profile and module-execution findings are resolved, but
the release authorization chain still omits immutable provenance for the primary CI reusable workflow.

## Unresolved Critical/High Findings

### High — Primary CI's reusable workflow remains mutable and absent from sealed workflow provenance

- **Evidence:** the brownfield primary workflow delegates its core `ci` job through
  `Hexalith/Hexalith.Builds/.github/workflows/domain-ci.yml@main`. AD-13 authenticates and seals the
  FrontComposer CI workflow path/head plus the release caller/reusable workflow, and closes transitive
  `uses:` only for the publishing/release path. AD-14 has one `reusable` object for the release workflow
  but no coordinate for the external reusable CI workflow. The fallback digest likewise binds local
  release-definition files, graph, and policy, but not this external CI definition.
- **Risk:** the same FrontComposer commit and `.github/workflows/ci.yml` blob can produce a successful
  release-authorizing CI conclusion under different `domain-ci.yml@main` content. The handoff proves
  the run/head and dependency evidence, but not the immutable test/build workflow whose successful
  conclusion release trusts. That contradicts AD-13's exact-CI-tested-revision/provenance claim and
  permits release behavior to change without graph, policy, manifest, or fallback invalidation.
- **Required fix:** pin the primary CI reusable workflow to an approved literal 40-hex commit; record
  and validate its actual repository/path/commit/blob coordinates (and required transitive action
  closure) in the CI handoff and sealed manifest. Model CI and release reusable workflows separately or
  use a closed sorted workflow-source array rather than the current singular `reusable` member. Because
  this CI definition participates in release authorization, include its immutable identity in release-
  definition/fallback invalidation or explicitly bind a sealed workflow-provenance digest into the
  fallback formula.

No other unresolved Critical/High finding was found in this pass. The profile tables now make minimum
semantic rules normative; module argv now use standalone Release/NuGet mode; selected catalog bytes are
materialized and re-hashed; policy bootstrap is machine-addressed and one-time; handoff metadata is
fail-closed; and manifest v2 plus graph/policy fallback invalidation are explicit.

