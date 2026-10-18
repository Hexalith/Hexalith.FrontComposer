# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: responsive-accessibility.spec.ts >> owned overlay return releases watchers when no destination appears after dismissal
- Location: specs/responsive-accessibility.spec.ts:285:1

# Error details

```
Error: page.evaluate: expected both return watchers, got 1
@debugger eval code line 311 > eval:40:35
async*evaluate@debugger eval code:313:16
@debugger eval code:1:44
@debugger eval code:1:62

```

# Page snapshot

```yaml
- generic [ref=e1]:
  - generic [ref=e2]:
    - link "Skip to content" [ref=e3] [cursor=pointer]:
      - /url: /counter#fc-main-content
    - link "Skip to navigation" [ref=e4] [cursor=pointer]:
      - /url: /counter#fc-nav
    - generic [ref=e6]:
      - banner [ref=e7]:
        - generic [ref=e8]:
          - generic [ref=e9]:
            - button "Toggle navigation" [ref=e12] [cursor=pointer]
            - generic [ref=e16]: Hexalith FrontComposer
          - generic [ref=e17]:
            - button "Change theme" [ref=e19] [cursor=pointer]:
              - generic [ref=e20]:
                - generic:
                  - generic [ref=e21]: "Change theme: Light"
                  - generic [aria-hidden] [ref=e22]: Light
            - button "Open settings" [ref=e26] [cursor=pointer]
            - button "FU" [ref=e31] [cursor=pointer]
      - navigation "Primary navigation" [ref=e35]:
        - generic [ref=e36]:
          - button "Counter" [ref=e37] [cursor=pointer]
          - button "More Counter pages" [ref=e43] [cursor=pointer]
          - button "Default" [ref=e50] [cursor=pointer]
          - button "Specimens" [ref=e55] [cursor=pointer]
          - button "More Specimens pages" [ref=e61] [cursor=pointer]
      - generic [ref=e68]:
        - status [ref=e69]: Density set to Compact.
        - main [ref=e70]:
          - status [ref=e71]
          - status [ref=e72]
          - generic [ref=e73]:
            - generic "Counter pages" [ref=e74]:
              - tablist [ref=e75]:
                - generic:
                  - tab "Overview" [selected] [ref=e76] [cursor=pointer]
                  - tab "Projection" [ref=e78] [cursor=pointer]
              - tabpanel "Overview" [ref=e80]:
                - generic [ref=e82]:
                  - generic:
                    - generic [ref=e83]:
                      - heading [level=2] [ref=e84]:
                        - button "Create with an exact target" [expanded] [ref=e85] [cursor=pointer]
                      - region "Create with an exact target" [ref=e88]:
                        - group [ref=e90]:
                          - generic "Create Counter command form" [ref=e91]:
                            - generic [ref=e92]:
                              - generic [ref=e93]:
                                - generic [ref=e95]:
                                  - generic: Counter key
                                  - generic "Counter key" [ref=e97]:
                                    - textbox "Counter key, Required" [ref=e99]
                                - generic [ref=e101]:
                                  - generic: Initial Value
                                  - generic "Initial Value" [ref=e103]:
                                    - textbox "Initial Value, Required" [ref=e105]: "0"
                                - button "Create Counter" [ref=e106] [cursor=pointer]
                              - status [ref=e108]
                    - generic [ref=e109]:
                      - heading [level=2] [ref=e110]:
                        - button "Update with an exact target" [expanded] [ref=e111] [cursor=pointer]
                      - region "Update with an exact target" [ref=e114]:
                        - group [ref=e116]:
                          - generic "Update Counter command form" [ref=e117]:
                            - generic [ref=e118]:
                              - generic [ref=e119]:
                                - generic [ref=e121]:
                                  - generic: Counter key
                                  - generic "Counter key" [ref=e123]:
                                    - textbox "Counter key, Required" [ref=e125]
                                - generic [ref=e127]:
                                  - generic: Amount
                                  - generic "Amount" [ref=e129]:
                                    - textbox "Amount, Required" [ref=e131]: "1"
                                - button "Update Counter" [ref=e132] [cursor=pointer]
                              - status [ref=e134]
                    - generic [ref=e135]:
                      - heading [level=2] [ref=e136]:
                        - button "Compact inline (3 fields)" [expanded] [ref=e137] [cursor=pointer]
                      - region "Compact inline (3 fields)" [ref=e140]:
                        - group [ref=e142]:
                          - generic "Batch Increment command form" [ref=e143]:
                            - generic [ref=e144]:
                              - generic [ref=e145]:
                                - generic [ref=e147]:
                                  - generic: Amount
                                  - generic "Amount" [ref=e149]:
                                    - textbox "Amount, Required" [ref=e151]: "1"
                                - generic [ref=e153]:
                                  - generic: Note
                                  - generic "Note" [ref=e155]:
                                    - textbox "Note, Required" [ref=e157]
                                - generic [ref=e159]:
                                  - generic: Effective Date
                                  - generic "Effective Date" [ref=e161]:
                                    - generic [ref=e162]:
                                      - textbox "Effective Date" [ref=e163]:
                                        - /placeholder: M/d/yyyy
                                        - text: 10/18/2026
                                      - button "Show date picker" [ref=e165]
                                - button "Batch Increment" [ref=e167] [cursor=pointer]
                              - status [ref=e169]
                    - generic [ref=e170]:
                      - heading [level=2] [ref=e171]:
                        - button "Inline + popover (1 field)" [expanded] [ref=e172] [cursor=pointer]
                      - region "Inline + popover (1 field)" [ref=e175]:
                        - button "Increment" [ref=e176] [cursor=pointer]
                    - generic [ref=e180]:
                      - heading [level=2] [ref=e181]:
                        - button "Full-page command (5 fields)" [expanded] [ref=e182] [cursor=pointer]
                      - region "Full-page command (5 fields)" [ref=e185]:
                        - generic:
                          - note [ref=e186]: Navigation state persistence (scroll, filter, sort across FullPage round-trip) lands in Story 4.3. The current demo proves routing + breadcrumb return only.
                          - link [ref=e187] [cursor=pointer]:
                            - /url: /commands/Counter/ConfigureCounterCommand?returnPath=%2Fcounter&projectionTypeFqn=Counter.Domain.CounterProjection
                            - generic [ref=e188]:
                              - generic:
                                - link [ref=e189]:
                                  - /url: /commands/Counter/ConfigureCounterCommand?returnPath=%2Fcounter&projectionTypeFqn=Counter.Domain.CounterProjection
                                - text: Configure Counter
            - status [ref=e190]
      - generic [ref=e191]: Hexalith FrontComposer © 2026
  - dialog [ref=e194]:
    - button "Action" [active] [ref=e195]
```