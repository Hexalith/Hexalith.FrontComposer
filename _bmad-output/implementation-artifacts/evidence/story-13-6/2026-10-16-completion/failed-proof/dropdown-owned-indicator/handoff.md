# Owned dropdown decoration and narrow focus correction

Settled focused verification: **27 passed, 0 failed, 0 skipped (34.3s)** across Chromium, Firefox and WebKit. Both complete forward/reverse type-specimen keyboard cycles pass in normal layout and with all text-spacing overrides. No actual clipping or occlusion failure remains in these tested paths.

The original false positive was an actual ownership mismatch: the native combobox button is assigned to Fluent's `control` slot, while its hidden decorative SVG is fallback content in the sibling `indicator` slot. Their composed paths share the Fluent control shell. The helper now qualifies that passive, bounded indicator ownership and keeps an injected overlapping SVG rectangle in the same shadow control rejected. Same-level, nested-stack, opaque sibling and open-shadow obstruction fixtures remain passing. The helper also recognizes actual SVGGraphicsElement paint at an outside-target hit, retaining the rectangle negative rather than replacing it with a path.

Once that false positive was removed, the complete cycles exposed real tight-flow focus obstructions. Below40rem, specimen navigation items and nested lists now leave8px for their7px external focus ring. The grid region now paints a2px inset contour using its existing local foreground theme token. Firefox's forced Highlight measured2.9407:1 against Canvas, so the supported forced-color contour uses CanvasText. The new themed regression checks normal/forced contrast, full focus geometry,2px inset paint, and unchanged0px desktop navigation margin. WebKit's missing actual forced-palette replacement is explicitly attributed.

Only these source files changed in this handoff:

- `tests/e2e/helpers/responsive-accessibility.ts`
- `tests/e2e/specs/responsive-accessibility.spec.ts`
- `samples/Counter/Counter.Specimens/FrontComposerTypeSpecimen.razor.css`

Exact settled command, from repository root:

```sh
PLAYWRIGHT_SKIP_WEBSERVER=1 FC_E2E_BUILD_CONFIGURATION=Debug FC_E2E_OUTPUT_DIR=/tmp/story13-6-dropdown-responsive-settled FC_E2E_HTML_REPORT_DIR=/tmp/story13-6-dropdown-responsive-settled-report npm --prefix tests/e2e test -- specs/responsive-accessibility.spec.ts --grep 'real Fluent dropdown owns|occlud|G02|type: 320|narrow specimen navigation' --project=chromium --project=firefox --project=webkit --workers=3
npm --prefix tests/e2e run typecheck
git diff --check
```

The exact final log/JUnit/self-contained HTML report are beside this file. Earlier failed commands, screenshots and contexts remain retained; `attribution.json` distinguishes each intermediate result. `story13-6-dropdown-ownership.json` and `story13-6-responsive-focus-owners.json` contain the original composed-tree and computed-paint evidence. No earlier failed comparison or configured Shell result is relabeled passing.

Parent has frozen candidate `sha256:0579f7b3a97547960595fa6847e38bf50c0d4ef56d55b71d4e2ac194ebb0e01e` and owns the broader rerun/result refresh. No temporary executable spec or child-owned host remains. Parent Test host5070/session6306 remains running. Overall Story13.6 completion, visual comparisons and manual/platform gates remain open.
