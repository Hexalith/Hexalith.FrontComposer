# Story 13.6: guided manual verification

Start with one Firefox/NVDA session on Windows. The assistant prepares the app and records your observations; you listen and report what actually happens. A short first session supplies partial evidence, not completion of the full manual matrix.

## Prepared application

Once started, the local Counter Test host serves [the accessibility scenario page](http://localhost:5070/__frontcomposer/specimens/type?generatedGrid=true&accessibilityScenarios=true&density=compact&theme=light). It uses fixed sample data and outcomes. This session tests the UI and its speech; it does not prove a live EventStore backend.

The first preparation matched product source `d1236f9e5f21c0a000e1081846a2ea6308a35fd3`. The assistant subsequently ran native NVDA/Firefox checks, found and corrected a palette announcement gap, and rebuilt the host used for those checks. The [current result](../../_bmad-output/implementation-artifacts/13-6-accessibility-result.md#agent-run-native-nvda-checks-and-palette-correction--2026-11-02) identifies the corrected source and exact automated results. That correction is committed as `9b386d4f0b305c06f2a7b00e949506970fda4ba2`. For any resumed human run, record the commit actually built and served, currently `9b386d4f0b305c06f2a7b00e949506970fda4ba2`. Agent-generated speech checks do not supply the human observations requested below.

The host is not left running between sessions. Ask the assistant to build and start it before you begin, and to check or restart it if the page stops responding. A tester on another computer, Mac, tablet or phone needs a separately prepared reachable test instance; `localhost` refers to that device itself.

## Start NVDA

1. If needed, download NVDA from [NV Access](https://www.nvaccess.org/download/) and install it in Windows.
2. Start NVDA from the Start menu; installed copies with its shortcut enabled also support **Ctrl+Alt+N**. Enable only this screen reader for the session.
3. Open the prepared page in Firefox. Wait for startup speech to finish.
4. **Tab** moves between controls; **Shift+Tab** moves back. **Enter** activates a button. In browse mode, **H** moves to the next heading. If NVDA is in focus mode, **NVDA+Space** switches modes. The NVDA key is usually **Insert**, or **Caps Lock** if configured.
5. **Ctrl** stops current speech. **NVDA+Q**, then **Enter**, exits NVDA.

These commands follow the [NVDA user guide](https://download.nvaccess.org/documentation/en/userGuide.html). Do not stop speech during the observation you are evaluating.

## First observation: a command with no input fields

1. In browse mode, press **H** until you hear **Fixed accessibility scenarios**.
2. Press **Tab** until you hear the **validation** button, then **Enter** to select that sample outcome.
3. Continue with **Tab** until you hear **Run accessibility specimen**. Press **Enter** once and let speech finish.
4. Look for feedback beside or below the trigger. Report whether you heard its outcome zero times, once, or more than once. Report the words or meaning in your own words.
5. Press **NVDA+Tab** once to hear the current focused control. Record whether it is still the trigger. This requested focus readback is separate from the automatic outcome announcement.
6. Wait without pressing anything. Report whether the outcome repeats by itself.

Reply in plain language, for example by answering: which control had focus, what outcome you heard, how many automatic announcements occurred, and whether waiting repeated it. Do not report Pass if you could not observe a step. The assistant can help distinguish an application failure from a navigation/setup problem.

## Continue with the assistant

After the first observation, work through the [owning human checklist](../../_bmad-output/implementation-artifacts/13-6-accessibility-result.md#human-evidence-checklist--leave-each-box-open-until-observed). The assistant can guide one sequence at a time using the existing visible controls:

| Sequence | Exercise and observe |
| --- | --- |
| Shell and dialogs | Home, navigation drawer, account/theme menus, Settings radios, palette and route/tab changes: meaningful names, keyboard operation, visible focus and correct return focus. |
| Projection and detail | **Load two rows**, expand `FC-AE-1`, then use the delayed missing-result control to test focused-detail loss. Test retained outside focus and **Repeat completed result** separately. Hear one neutral explanation without duplicate recovery speech. |
| Command outcomes | Repeat the first observation for each fixed failure outcome. Check feedback persistence, next-attempt clearing, navigation/scope clearing and the separate accepted-command lifecycle. |
| Other operation states | Loading, empty, failed and recovered queries; lifecycle states; toolbar search/filter/view/tabs; destructive confirmation and abandonment warning. Check one speech owner, silent restored messages and one new announcement for a new event. |
| Responsive and visual modes | Repeat the required operations at 320 CSS pixels and actual browser zoom 400% from a measured 1280 CSS-pixel content width, with combined text spacing, both themes, reduced motion and supported forced colors. The assistant helps measure geometry; you report visibility, usability and speech. |

Do not substitute responsive viewport emulation for browser zoom. A browser zoom session must record its measured reference width and resulting zoom/viewport, not just the menu's percentage. Do not resize the window to estimate a measurement.

Full manual acceptance also requires JAWS/Chrome on Windows, VoiceOver/Safari on macOS, physical tablet, physical phone fallback and the applicable zero-field/zoom sequences. The Windows session proves only what it actually exercises. Ask a tester with the missing setup to perform the remaining runs; do not buy tools just to mark a gate complete.

The accepted WebKit platform limitation retains existing WebKit media/semantic checks and actual palette proof on capable Chromium/Firefox environments. This guide does not request unavailable native WebKit palette painting.

## What to send back

Send the actual test date, your tester identity, OS, Firefox and NVDA versions, the exercised sequence and its result. NVDA's Help/About and Firefox's Help/About show their versions. The assistant records the candidate, build configuration, gate mapping and remaining scope from the prepared session, then asks only for missing facts.

For each sequence, report what you heard, where focus ended, whether anything repeated or was unreachable, and anything you did not test or could not determine. A failure report is useful evidence. Use only the fixed sample data; omit personal data, tokens, full transcripts and raw browser dumps.

The [manual log template](manual-log-template.md) defines the final record fields. This walkthrough contains instructions only; it supplies no new manual Pass. The seven historical Owner reports and their supporting-details waiver remain intact.
