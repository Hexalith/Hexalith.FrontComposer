# Story 6-5 Hot Reload and Rebuild Matrix

| Change type | Expected behavior |
| --- | --- |
| Projection annotation argument tweak | Source generator hot reload can refresh generated metadata. |
| New Level 2/3/4 override registration | Full restart required because DI and descriptor registries are startup metadata. |
| Component-tree contract version change | Full rebuild/restart required; starter output references HFC1049 drift guidance. |
| Dev-mode shortcut rebinding | Reload required for the scoped shortcut registrar to register the new binding. |
| Overlay Razor/CSS edit | Blazor hot reload applies while the overlay is active; no persisted overlay state is required. |
| `IHostEnvironment.IsDevelopment()` flips to false at runtime | Dev-mode services remain registered for the current process; restart required to honor the new environment. The shortcut handler short-circuits silently and the overlay renders zero DOM via the runtime gate, so no user-visible "dev-mode" error reaches Production callers. |

Story 6-6 owns the dedicated user-facing "Full restart required for this change type" diagnostic channel. Story 6-5 surfaces stale metadata in the drawer and suppresses current starter copying.
