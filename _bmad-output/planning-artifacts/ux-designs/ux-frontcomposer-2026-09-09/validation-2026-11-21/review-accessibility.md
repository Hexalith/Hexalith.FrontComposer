# Accessibility review of the FrontComposer UX references

November 21, 2026. Two high and two medium findings affect the responsive navigation mockup. The command lifecycle preview displayed the expected message at each keyboard selection. The findings below concern the two offline HTML references. Product implementation and release acceptance require their own evidence.

## Findings

### High · Keyboard focus moves behind the open drawer

At 320 and 1024 CSS px, open the module drawer with Enter, then press Tab five times. Focus moves through the two drawer links, Account and then the Tenants and Users workspace links. The drawer fully covers both workspace links. At 320 px the Users link occupied x=110–175 and y=340–378, inside the drawer at x=11–273 and y=282–602. Browser hit tests at all four inset corners and the center reached the drawer. Escape leaves it open. This violates the reference’s AE-05 focus criterion and WCAG 2.2 SC 2.4.11 in this interaction. [Drawer styles and controls](../mockups/key-responsive-navigation.html) · [Canonical UX-AE-1](../../../ux-design.md#ux-ae-1---responsive-and-accessibility-evidence-matrix)

**Fix.** Keep the drawer in document flow if it acts as a disclosure, or use keyboard-operable modal behavior with background controls inert while it overlays content. Provide a dismissal path that returns focus to the opener. Repeat the Tab sequence forward and backward at phone and compact desktop widths and check that each focused control and its ring remain visible.

### High · Current navigation items lose visible keyboard focus in forced colors

At 1440 CSS px with forced colors active, focusing the current rail link or current workspace link left every pixel in and around the control unchanged. Each retained the same 1 px solid outline with a −1 px offset when focused and unfocused. The more specific current-item style at lines 126–129 overrides the focus style at line 25. The equivalent comparison on the inactive Users link showed a distinct ring. Keyboard users cannot locate focus on either current item. This fails VC-04 and WCAG SC 2.4.7. [Navigation style rules](../mockups/key-responsive-navigation.html) · [Visual focus contract](../../../ux-design-detailed-2026-07-05.md#ux-vc-1---contrast-and-forced-colors-matrix)

**Fix.** Give current-item `:focus-visible` its own forced-colors outline and offset and retain a separate current-item marker. Compare current, focused and current-plus-focused states on the rail and tabs.

### Medium · Module navigation focuses the outer reference frame

Use Enter on the Parties module link. The URL changes and the workspace appears, but the active element becomes `section#parties/overview`, an outer frame containing the address bar and repeated shell controls. The destination heading inside the frame receives no focus. FM-01 calls for focus on the route heading when activation succeeds. The static reference has an overall page heading and a separate workspace heading. [Module links and destinations, lines 136–202](../mockups/key-responsive-navigation.html) · [FM-01](../../../ux-design.md#ux-fm-1---focus-matrix)

**Fix.** On module activation, focus the destination workspace heading. If the reference remains a CSS-only illustration, identify the fragment links as a preview mechanism and document the destination distinction next to the example.

### Medium · The workspace page selector behaves as ordinary links

On Tenants, Right Arrow on the active workspace link leaves focus and the displayed panel in place. Tab then Enter on Users changes the fragment and moves focus to its outer section. The selector lacks tablist/tab/tabpanel roles and their selection relationships. FM-02 calls for keyboard selection to retain focus on the chosen tab while its associated panel changes. The links remain keyboard-operable as links, but copying this example would lose the product’s tab interaction. [Workspace selector, lines 151–153 and 177–179](../mockups/key-responsive-navigation.html) · [FM-02](../../../ux-design.md#ux-fm-1---focus-matrix)

**Fix.** Demonstrate the documented tab model with selected state, arrow-key movement and a labelled panel, or label this explicitly as a route-link illustration and direct implementers to the behavior supplement for the keyboard contract.

## Checks and evidence limits

- Used Playwright with Chromium 156.0.8078.79 to load both checked-in HTML files offline. At 320, 768, 1024 and 1366 CSS px both had document width equal to viewport width. With reduced motion active, neither page ran an animation. These checks cover reflow at the listed widths. Actual 400% browser zoom and physical devices require separate verification.
- In forced colors and reduced motion, Home followed by nine Down Arrow presses exercised all ten lifecycle snapshots. Each retained focus on **Preview state** and exposed one visible canonical message inside the existing polite, atomic status region. This included the terminal Warning and both Degraded outcomes. The exhausted snapshot’s accessibility-tree status contained the canonical final message. The preview cannot establish heard speech, 250 ms coalescing, duplicate suppression or runtime polling. [Lifecycle selector, status region and scope note](../mockups/key-command-lifecycle.html) · [Canonical UX-AM-1](../../../ux-design.md#ux-am-1---announcement-matrix)
- The five documents agree on the route and tab destinations, visible focus, non-color state cues and supplemental announcements. The dated accessibility story records the candidate-specific native, zoom and WebKit decisions. OI-16 and G-4 remain open. This review exercised the HTML references. It did not run the deployed Fluent shell or native assistive technology. [Accessibility story receipt](../../../../implementation-artifacts/spec-13-6-verify-responsive-and-assistive-accessibility.md#owner-acceptance-of-the-h6-review-patches-and-code-review-completion--2026-11-15)
- The coordinating reviewer separately ran axe-core 4.14.0 with WCAG A/AA tags on 20 lifecycle and eight navigation state/width combinations. The run reported zero automated violations and incomplete checks in some combinations. The keyboard and paint findings above required interaction outside that scan.

Reviewed HTML SHA-256 values are `28de35ce7b83610791b483204861aab745e32e65588b82135f32df4cd9b5cf24` for responsive navigation and `c7a081da3c48d2580cfc49b61358415aeb8633e2c8714683b9ba8da4e9da885c` for command lifecycle.
