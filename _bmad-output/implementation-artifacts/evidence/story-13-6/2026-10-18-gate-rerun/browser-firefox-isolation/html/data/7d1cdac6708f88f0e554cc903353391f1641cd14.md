# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: accessibility-state-matrix.spec.ts >> shell chrome, theme menu and exact desktop navigation rails in dark
- Location: specs/accessibility-state-matrix.spec.ts:462:3

# Error details

```
Error: locator.evaluate: Competing border/shadow paint needs pixel evidence: {"element":"FLUENT-MENU-ITEM#f56e5473.","candidate":"DIV#.fluent-card","pseudo":null,"boxes":[{"left":15,"right":169,"top":199,"bottom":201},{"left":15,"right":169,"top":233,"bottom":235},{"left":15,"right":17,"top":201,"bottom":233},{"left":167,"right":169,"top":201,"bottom":233}],"paintBoxes":[{"left":32,"right":288,"top":235.8000030517578,"bottom":236.8000030517578},{"left":287,"right":288,"top":235.8000030517578,"bottom":455.8000030517578},{"left":32,"right":288,"top":454.8000030517578,"bottom":455.8000030517578},{"left":32,"right":33,"top":235.8000030517578,"bottom":455.8000030517578},{"left":29,"right":291,"top":232.8000030517578,"bottom":458.8000030517578},{"left":29,"right":291,"top":233.8000030517578,"bottom":459.8000030517578}],"candidateBox":{"x":32,"y":235.8000030517578,"width":256,"height":220,"top":235.8000030517578,"right":288,"bottom":455.8000030517578,"left":32},"shadow":"rgba(0, 0, 0, 0.24) 0px 0px 2px 0px, rgba(0, 0, 0, 0.28) 0px 1px 2px 0px"}
unsupported@debugger eval code line 311 > eval:304:39
sample@debugger eval code line 311 > eval:313:56
visit@debugger eval code line 311 > eval:643:19
@debugger eval code line 311 > eval:697:10
evaluate@debugger eval code:313:16
@debugger eval code:1:44
@debugger eval code:1:62

```

# Page snapshot

```yaml
- generic [ref=e2]:
  - link "Skip to content" [ref=e3] [cursor=pointer]:
    - /url: /counter#fc-main-content
  - link "Skip to navigation" [ref=e4] [cursor=pointer]:
    - /url: /counter#fc-nav
  - generic [ref=e6]:
    - banner [ref=e7]:
      - generic [ref=e8]:
        - generic [ref=e9]:
          - generic [ref=e10]:
            - button "Toggle navigation" [ref=e12] [cursor=pointer]
            - generic:
              - generic:
                - generic: Counter
                - generic: Default
                - generic: Specimens
          - generic [ref=e16]: Hexalith FrontComposer
        - generic [ref=e17]:
          - generic [ref=e18]:
            - button "Change theme" [expanded] [ref=e19] [cursor=pointer]:
              - generic [ref=e20]:
                - generic:
                  - generic [ref=e21]: "Change theme: Dark"
                  - generic [aria-hidden] [ref=e22]: Dark
            - menu [ref=e26]:
              - generic:
                - menuitem "Light" [ref=e27] [cursor=pointer]
                - menuitem "Dark" [active] [ref=e29] [cursor=pointer]
                - menuitem "System" [ref=e31] [cursor=pointer]
          - button "Open command palette" [ref=e33] [cursor=pointer]
          - button "Open settings" [ref=e37] [cursor=pointer]
          - button "FU" [ref=e42] [cursor=pointer]
    - generic [ref=e44]:
      - status [ref=e45]: Density set to Comfortable.
      - main [ref=e46]:
        - status [ref=e47]
        - status [ref=e48]
        - generic [ref=e49]:
          - heading "Counter" [level=1] [ref=e50]
          - generic "Counter pages" [ref=e51]:
            - tablist [ref=e52]:
              - generic:
                - tab "Overview" [selected] [ref=e53] [cursor=pointer]
                - tab "Projection" [ref=e55] [cursor=pointer]
            - tabpanel "Overview" [ref=e57]:
              - generic [ref=e59]:
                - generic:
                  - generic [ref=e60]:
                    - heading [level=2] [ref=e61]:
                      - button "Create with an exact target" [expanded] [ref=e62] [cursor=pointer]
                    - region "Create with an exact target" [ref=e65]:
                      - group [ref=e67]:
                        - generic "Create Counter command form" [ref=e68]:
                          - generic [ref=e69]:
                            - generic [ref=e70]:
                              - generic [ref=e72]:
                                - generic: Counter key
                                - generic "Counter key" [ref=e74]:
                                  - textbox "Counter key, Required" [ref=e76]
                              - generic [ref=e78]:
                                - generic: Initial Value
                                - generic "Initial Value" [ref=e80]:
                                  - textbox "Initial Value, Required" [ref=e82]: "0"
                              - button "Create Counter" [ref=e83] [cursor=pointer]
                            - status [ref=e85]
                  - generic [ref=e86]:
                    - heading [level=2] [ref=e87]:
                      - button "Update with an exact target" [expanded] [ref=e88] [cursor=pointer]
                    - region "Update with an exact target" [ref=e91]:
                      - group [ref=e93]:
                        - generic "Update Counter command form" [ref=e94]:
                          - generic [ref=e95]:
                            - generic [ref=e96]:
                              - generic [ref=e98]:
                                - generic: Counter key
                                - generic "Counter key" [ref=e100]:
                                  - textbox "Counter key, Required" [ref=e102]
                              - generic [ref=e104]:
                                - generic: Amount
                                - generic "Amount" [ref=e106]:
                                  - textbox "Amount, Required" [ref=e108]: "1"
                              - button "Update Counter" [ref=e109] [cursor=pointer]
                            - status [ref=e111]
                  - generic [ref=e112]:
                    - heading [level=2] [ref=e113]:
                      - button "Compact inline (3 fields)" [expanded] [ref=e114] [cursor=pointer]
                    - region "Compact inline (3 fields)" [ref=e117]:
                      - group [ref=e119]:
                        - generic "Batch Increment command form" [ref=e120]:
                          - generic [ref=e121]:
                            - generic [ref=e122]:
                              - generic [ref=e124]:
                                - generic: Amount
                                - generic "Amount" [ref=e126]:
                                  - textbox "Amount, Required" [ref=e128]: "1"
                              - generic [ref=e130]:
                                - generic: Note
                                - generic "Note" [ref=e132]:
                                  - textbox "Note, Required" [ref=e134]
                              - generic [ref=e136]:
                                - generic: Effective Date
                                - generic "Effective Date" [ref=e138]:
                                  - generic [ref=e139]:
                                    - textbox "Effective Date" [ref=e140]:
                                      - /placeholder: M/d/yyyy
                                      - text: 10/18/2026
                                    - button "Show date picker" [ref=e142]
                              - button "Batch Increment" [ref=e144] [cursor=pointer]
                            - status [ref=e146]
                  - generic [ref=e147]:
                    - heading [level=2] [ref=e148]:
                      - button "Inline + popover (1 field)" [expanded] [ref=e149] [cursor=pointer]
                    - region "Inline + popover (1 field)" [ref=e152]:
                      - button "Increment" [ref=e153] [cursor=pointer]
                  - generic [ref=e157]:
                    - heading [level=2] [ref=e158]:
                      - button "Full-page command (5 fields)" [expanded] [ref=e159] [cursor=pointer]
                    - region "Full-page command (5 fields)" [ref=e162]:
                      - generic:
                        - note [ref=e163]: Navigation state persistence (scroll, filter, sort across FullPage round-trip) lands in Story 4.3. The current demo proves routing + breadcrumb return only.
                        - link [ref=e164] [cursor=pointer]:
                          - /url: /commands/Counter/ConfigureCounterCommand?returnPath=%2Fcounter&projectionTypeFqn=Counter.Domain.CounterProjection
                          - generic [ref=e165]:
                            - generic:
                              - link [ref=e166]:
                                - /url: /commands/Counter/ConfigureCounterCommand?returnPath=%2Fcounter&projectionTypeFqn=Counter.Domain.CounterProjection
                              - text: Configure Counter
          - status [ref=e167]
    - generic [ref=e168]: Hexalith FrontComposer © 2026
```