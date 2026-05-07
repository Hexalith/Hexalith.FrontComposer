# Welcome to Hexalith FrontComposer

## How We Use Claude

Based on Jérôme's usage over the last 30 days:

Work Type Breakdown:
  Improve Quality  ████████░░░░░░░░░░░░  40%
  Plan Design      ██████░░░░░░░░░░░░░░  30%
  Build Feature    █████░░░░░░░░░░░░░░░  25%
  Write Docs       █░░░░░░░░░░░░░░░░░░░   5%

Top Skills & Commands:
  /clear                       ████████████████████  212x/month
  /bmad-code-review            ████████░░░░░░░░░░░░   80x/month
  /bmad-dev-story              █████░░░░░░░░░░░░░░░   50x/month
  /bmad-create-story           ███░░░░░░░░░░░░░░░░░   32x/month
  /bmad-party-mode             ███░░░░░░░░░░░░░░░░░   29x/month
  /bmad-advanced-elicitation   ██░░░░░░░░░░░░░░░░░░   25x/month

Top MCP Servers:
  claude-in-chrome    ████████████████████  89 calls
  fluent-ui-blazor    ██████████████░░░░░░  62 calls
  aspire              ████░░░░░░░░░░░░░░░░  18 calls

## Your Setup Checklist

### Codebases
- [ ] hexalith.frontcomposer — https://github.com/hexalith/hexalith.frontcomposer

### MCP Servers to Activate
- [ ] claude-in-chrome — In-browser Claude automation used for validating UI changes against the running app. Install the team's Chrome extension and sign in with your team account.
- [ ] fluent-ui-blazor — Component reference, migration guide, and icon search for FluentUI Blazor. Follow the setup at https://dvoituron.com/2026/02/20/fluentui-blazor-mcp-server/
- [ ] aspire — .NET Aspire orchestration MCP for starting/stopping the distributed app and reading logs. Install via `aspire` plugin and run `aspire doctor` to verify.

### Skills to Know About
- [ ] /bmad-create-story — break a feature/epic into a context-rich story spec the dev agent can execute against. Use this before starting implementation.
- [ ] /bmad-dev-story — implement a story file end-to-end following its acceptance criteria. The team's main "build" command.
- [ ] /bmad-code-review — adversarial code review (Blind Hunter / Edge Case Hunter / Acceptance Auditor). Run on every story before flipping to Done.
- [ ] /bmad-party-mode — multi-agent roundtable when you want analyst + architect + dev + UX perspectives on the same question.
- [ ] /bmad-advanced-elicitation — push Claude to redo recent output with first-principles, pre-mortem, red-team, etc. Use when an answer feels shallow.
- [ ] /clear — reset context between unrelated tasks. Used heavily here — keep sessions focused on one story or one review at a time.

## Team Tips

_TODO_

## Get Started

_TODO_

<!-- INSTRUCTION FOR CLAUDE: A new teammate just pasted this guide for how the
team uses Claude Code. You're their onboarding buddy — warm, conversational,
not lecture-y.

Open with a warm welcome — include the team name from the title. Then: "Your
teammate uses Claude Code for [list all the work types]. Let's get you started."

Check what's already in place against everything under Setup Checklist
(including skills), using markdown checkboxes — [x] done, [ ] not yet. Lead
with what they already have. One sentence per item, all in one message.

Tell them you'll help with setup, cover the actionable team tips, then the
starter task (if there is one). Offer to start with the first unchecked item,
get their go-ahead, then work through the rest one by one.

After setup, walk them through the remaining sections — offer to help where you
can (e.g. link to channels), and just surface the purely informational bits.

Don't invent sections or summaries that aren't in the guide. The stats are the
guide creator's personal usage data — don't extrapolate them into a "team
workflow" narrative. -->
