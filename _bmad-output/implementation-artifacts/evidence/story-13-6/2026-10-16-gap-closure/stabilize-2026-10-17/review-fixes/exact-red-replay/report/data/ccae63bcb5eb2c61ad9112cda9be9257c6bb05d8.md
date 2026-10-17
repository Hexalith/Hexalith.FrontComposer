# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: clipping.spec.ts >> contrast clips a shadow background at its scrollport while rejecting 1.7px of visible overlap
- Location: ../../_bmad-output/implementation-artifacts/evidence/story-13-6/2026-10-16-gap-closure/stabilize-2026-10-17/exact-red/clipping.spec.ts:3:5

# Error details

```
Error: locator.evaluate: Overlapping sibling background needs pixel evidence: {"element":"P#label.","candidate":"DIV#.","boxes":[{"left":40,"right":118.16667175292969,"top":100,"bottom":120}],"candidateBox":{"x":40,"y":118.30000305175781,"width":222,"height":32,"top":118.30000305175781,"right":262,"bottom":150.3000030517578,"left":40},"candidateClip":{"kind":"none"},"candidatePosition":"relative","root":"document"}
sample@debugger eval code line 311 > eval:185:138
visit@debugger eval code line 311 > eval:275:19
@debugger eval code line 311 > eval:390:10
evaluate@debugger eval code:313:16
@debugger eval code:1:44
@debugger eval code:1:62

```

# Page snapshot

```yaml
- paragraph [ref=e4]: Footer label
```