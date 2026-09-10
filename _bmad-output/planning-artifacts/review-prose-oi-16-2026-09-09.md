# Prose Review — OI-16 UX Authority Chain

This three-file chain exists to help human FrontComposer framework implementers, testers, and Product reviewers retrieve and apply precise UX behavior, evidence, and status language without misreading the visual and journey supplements as competing authority.

The intentional voice is terse, normative, and technical. This pass preserves sentence fragments in
matrix cells, stable IDs and defined terms, exact source/component identifiers, timing values, and the
distinction between delivered baselines and open implementation/evidence work. It skips frontmatter,
code, headings/structural markup, and every structure decision already accepted, rejected, or marked
PRESERVE.

| Pass | Original Text | Revised Text | Changes |
| --- | --- | --- | --- |
| prose | `ux-design.md`, UX-DR5: “Default evidence budgets are confirming-to-`Degraded` at `10_000` ms, polling every `1_000` ms for at most `120_000` ms, zero pre-accept retries, and one transient retry `250` ms after acknowledgement. Projection recovery uses unbounded jittered reconnect attempts capped at `30_000` ms, restart of a closed connection within `10` s…” | “Default timing budgets transition to `Degraded` at `10_000` ms, poll every `1_000` ms for at most `120_000` ms, allow zero pre-accept retries, and allow one transient retry `250` ms after acknowledgement. Projection recovery retries indefinitely with jittered delays capped at `30_000` ms, restarts a closed connection within `10` s…” | Replaces the opaque “confirming-to” noun stack and distinguishes an unlimited attempt count from the capped delay; preserves every value and state. |
| prose | `ux-experience-2026-07-05.md`, Foundation: “FrontComposer is a responsive web developer product and operations shell.” | “FrontComposer is both a responsive web product for developers and an operations shell.” | Clarifies that FrontComposer serves two roles; the original compound can be read as modifying “developer” with “responsive web.” |
| prose | `ux-experience-2026-07-05.md`, `FcPageToolbar` and Interaction Primitives: “Target `/` uses FM-12 only for one enabled active-route page search”; “The target `/` behavior focuses active page search only when enabled” | “The target `/` shortcut follows FM-12 only when the active route has exactly one enabled page search”; “The target `/` shortcut focuses the active route's page search only when exactly one is enabled” | Deduplicates one phrasing issue across both locations and makes the enabling/uniqueness condition explicit in the sentence. Exact shortcut, component, FM-12, and open-convergence terms remain intact. |
| prose | `ux-experience-2026-07-05.md`, Announcement Behavior: “Separate state-free coalescing groups are lifecycle operation, surface + connection epoch, surface + operator-initiated load/filter operation, and palette session.” | “The state-free coalescing group keys are the lifecycle operation, surface + connection epoch, surface + operator-initiated load/filter operation, and palette session.” | Makes clear that the list defines deduplication keys, not four kinds of UI groups. |
| prose | `ux-experience-2026-07-05.md`, Interaction Primitives: “every action has a visible, keyboard-operable alternative for browser, OS, and localized-layout conflicts.” | “every action has a visible, keyboard-operable alternative that works despite browser, OS, or localized-keyboard-layout conflicts.” | Clarifies the relationship between the alternative and the conflicts; retains the platform/layout requirement. |
| prose | `ux-experience-2026-07-05.md`, UJ-4 failure: “This visual supplement does not claim that G-7 security acceptance is complete.” | “This behavior supplement does not claim that G-7 security acceptance is complete.” | Corrects the supplement's self-reference; no gate or acceptance status changes. |

These are **6 deduplicated prose fixes**. No additional style-only rewrite is recommended; the terse
matrix voice, deliberate cross-file joins, stable terminology, and accepted journey pacing should
remain as written. This prose review makes no gate or approval claim.
