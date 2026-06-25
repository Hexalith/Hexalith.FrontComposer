## Run: 2026-06-05T07:33:36Z

**Epic:** Hexalith.FrontComposer - Epic Breakdown
**Stories:** 1.0-7.5

### Patterns Observed
- The later epics were mostly brownfield confirm-and-pin stories: the fastest path was to name the v1 contract, add narrow pins, and avoid broad rewrites.
- File List reconciliation remained the most common review-side correction, especially when E2E specs, test summaries, or sample files changed.
- Local VSTest lanes repeatedly failed before execution because socket creation is blocked in this environment; direct xUnit v3 in-process lanes were the reliable local evidence path.
- Retrospectives were useful for carrying forward phase/disposition constraints, especially around diagnostic catalog behavior and synthetic evidence.

### Code Review Insights
- Common issues: stale evidence counts, File List drift, text/JSON CLI parity gaps, redaction edge cases, and over-claiming build-time diagnostic emission.
- Review cycles generally completed in a single cycle per story during the resumed run, with review agents applying focused fixes before marking sprint status done.

### Timing Estimates
- create-story: usually a few minutes, longer when cross-epic context was needed.
- dev-story: ranged from short confirm-and-pin passes to longer validation runs for drift and Testing package stories.
- code-review: typically a longer single pass with focused reruns and story/status reconciliation.

### Recommendations for Future Runs
- Add a mechanical changed-file vs story File List check before review promotion.
- Keep using direct xUnit v3 in-process runners in this sandbox and record blocked VSTest lanes by exact error.
- Treat text CLI output, documentation evidence counts, and redaction behavior as first-class contract surfaces.
- Keep diagnostic phase/disposition explicit; do not treat cataloged IDs as proof of build-time emission.

## Run: 2026-06-25T17:10:56Z

**Epic:** Hexalith.FrontComposer - Epic Breakdown
**Stories:** 8.1-8.7

### Patterns Observed
- Fluent UI Blazor v5 visual work needs rendered-DOM proof, not selector-string confidence.
- Most review fixes were not broad rewrites; they were precise corrections to dead CSS, stale evidence, icon parity, and environment-specific test assumptions.
- Direct xUnit v3 in-process lanes remained the reliable local validation path; solution VSTest and browser Playwright lanes were blocked by local socket restrictions.
- Documentation drift became visible only after the epic completed, especially around shell parameter count and the unified navigation rail.

### Code Review Insights
- Common issues: dead selectors against Fluent-rendered DOM, stale File List/task evidence, retained component references that needed explicit justification, and E2E assertions hidden by local browser blockers.
- Average cycles to clean: 1 review cycle per story, with Claude applying review fixes automatically for the final review task configuration.

### Timing Estimates
- create-story: several minutes per story.
- dev-story: ranged from short visual/documentation updates to longer generator and validation passes.
- code-review: usually one longer adversarial pass with focused reruns and sprint-status reconciliation.

### Recommendations for Future Runs
- Add a reusable visual-component checklist requiring rendered-DOM or computed-style evidence for Fluent layout/CSS changes.
- Keep mechanical changed-file vs story File List reconciliation before review promotion.
- Track browser/visual lanes as named CI evidence when the local sandbox cannot bind sockets.
- Sweep adopter-facing docs at epic close whenever public component surfaces or navigation behavior changed.
