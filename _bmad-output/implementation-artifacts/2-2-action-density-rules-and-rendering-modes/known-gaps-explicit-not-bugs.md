# Known Gaps (Explicit, Not Bugs)

These are cross-story deferrals intentionally out of scope for Story 2-2. QA should NOT file these as defects. If shipped behavior is observed to be missing, link to the owning story.

| Gap | Owning Story | Reason |
|---|---|---|
| Danger appearance + destructive confirmation dialog | Story 2-5 | Confirmation UX is a cross-cutting concern tied to form abandonment; single-story focus |
| 30-second form abandonment warning (UX-DR38) | Story 2-5 | Same family as destructive confirmation |
| HFC1015 analyzer-emitted diagnostic for RenderMode/density mismatch | Epic 9 | Analyzer emission is Story 9.4's domain; 2-2 ships runtime `ILogger` warning only |
| DataGrid capture-side Fluxor wiring (scroll, filter, sort, expansion event producers) | Story 4.3 | DataGrid component surface is Epic 4 |
| `DataGridNavigationState` effects (persistence, hydration, beforeunload flush) | Story 4.3 | Effects land with capture producers; 2-2 ships reducers only (Decision D30) |
| Shell header breadcrumb integration | Story 3.1 | Shell layout is Epic 3; 2-2 ships embedded `FluentBreadcrumb` fallback (opt-out via `FcShellOptions.EmbeddedBreadcrumb`) |
| Shell-level `ProjectionContext` cascading from DataGrid rows | Story 4.1 | Epic 4 DataGrid infrastructure; 2-2 ships null-tolerant renderer + Counter-sample manual cascade |
| Real DataGrid state preservation end-to-end demo | Story 4.3 | 2-2 only proves `RestoreGridStateAction` dispatch contract with an empty state map (Decision D30) |
| `[PrefillStrategy(SkipLastUsed=true)]` attribute (bypass LastUsed entirely) | Future (post-v0.1) | Current `[DefaultValue]` hard-floor rule (Decision D24) handles 80% of cases; wider attribute added on adopter feedback |
| FluentMessageBar form abandonment UX (UX-DR38 full treatment) **AND** any interim draft-protection | Story 2-5 | 2-2 evaluated a minimal native `beforeunload` guard and rejected it during elicitation round 2 (matrix score 2.25) — shipping half-UX creates prompt-fatigue debt before 2-5's real treatment lands. Adopters who need interim protection wire `beforeunload` themselves. |
| Popover ↔ FluentDialog coordination (z-index / force-close before dialog opens) | Story 2-5 | 2-2 exposes `ClosePopoverAsync()` on popover components; 2-5 owns the coordination contract when it adds destructive confirmation dialogs (Pre-mortem PM-6). Building the contract speculatively was rejected at matrix score 2.45. |
| Static Fluent UI icon-catalog validator (parse-time HFC1010) | Epic 9 | 2-2 ships runtime try/catch fallback + warning (Decision D34); parse-time validation needs Fluent UI version-aware analyzer and is redundant given the runtime safety net. |
| Command-type-name character validation (parse-time HFC1009) | Roslyn native | Roslyn's identifier rules already reject invalid C# identifiers — this validation was redundant and cut in round 2. |
| LastUsed storage LRU cap | Future (adopter signal) | DoS via 1M LastUsed keys is theoretical in v0.1. Add when Epic 8 MCP broadens command surface or adopter quota pressure is reported. Decision D33 is DataGridNav-only. |
| DataGrid state orphan-detection telemetry (stale FQN-keyed snapshots after projection rename) | Story 4.3 | 2-2's 24-hour TTL prunes silently; telemetry on orphan pickup lands with capture-side wiring. |
| MCP command manifest emission (expose density + provider chain per command for Epic 8 agent introspection) | Epic 8 | Renderer currently emits no MCP-facing manifest. Adding `{CommandTypeName}McpManifest.g.json` per command would let MCP tool server introspect without reflection. Defer to Epic 8 where the MCP tool server design lands. |
| Custom `CommandRenderMode` resolver delegate (e.g., "always FullPage on mobile", "Inline for power users") | Backlog | Currently `RenderMode?` is a static per-instance parameter. A `Func<CommandDensity, DeviceContext, CommandRenderMode>` resolver would enable runtime mode decisions. No adopter demand evidence yet; revisit after adopter feedback. |
| `LastUsedValueProvider` audit log (compliance-ready "who wrote what" trail without values) | Epic 7 (compliance/multi-tenancy) | 2-2 logs nothing on `Record<TCommand>`; compliance adopters will want a structured audit trail (command type + property names + tenant + user + timestamp, NO values). Defer to Epic 7 where compliance requirements land. |

---
