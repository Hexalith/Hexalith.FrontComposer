# Read-only desktop local-dark focus probe

The current type-local foreground adaptation is already outside the40rem media rule (FrontComposerTypeSpecimen.razor.css183–184). No source change was necessary or made.

A standalone Playwright probe navigated to `/__frontcomposer/specimens/type?theme=dark` at1280×900 in Chromium, Firefox and WebKit, focused the actual `region[name="specimen status"]`, and used real Shift+Tab/Tab before collecting computed paint, `focusGeometry` and `expectSurfaceContrast(..., "focus")` from the existing helper.

All three engines reported focused/:focus-visible true, narrow media false,2px solid#f7f7f2 outline with-2px offset against local#15171a,16.71109645481964:1 contrast, and geometry failures[]. Fluent Focus2 remained#000000, demonstrating that the local all-width theme adaptation owns the visible contour.

Exact per-engine proof is `desktop-local-dark-probe.json`. This was a read-only browser measurement, not an added or rerun spec. No temporary executable spec or child-owned host was created; parent host remains untouched.
