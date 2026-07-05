# Senior Developer Review - Validation Checklist

- [ ] Story file loaded from `{{story_path}}`
- [ ] Story Status verified as reviewable (review)
- [ ] Epic and Story IDs resolved ({{epic_num}}.{{story_num}})
- [ ] Story Context located or warning recorded
- [ ] Epic Tech Spec located or warning recorded
- [ ] Architecture/standards docs loaded (as available)
- [ ] Tech stack detected and documented
- [ ] MCP doc search performed (or web fallback) and references captured
- [ ] Acceptance Criteria cross-checked against implementation
- [ ] File List reviewed and validated for completeness
- [ ] `python3 eng/validate-story-artifacts.py --story {{story_path}}` passed. If the command cannot execute, record the exact environmental blocker.
- [ ] Mechanical reconciliation gate enforced before `done`: a non-zero validator exit is `artifact_validation_failed` and keeps the story `in-progress` regardless of CRITICAL count.
- [ ] Any suspected validator false positive is tracked as a validator fix with regression evidence; it is not a manual bypass path.
- [ ] Tests identified and mapped to ACs; gaps noted
- [ ] Test evidence language verified: exact local commands/results, blocker timing, VSTest/socket or network blocker text, xUnit in-process fallback evidence, Playwright/browser CI-gate handoff, and CI-authoritative lanes are clearly separated
- [ ] Code quality review performed on changed files
- [ ] Security review performed on changed files and dependencies
- [ ] Outcome decided (Approve/Changes Requested/Blocked)
- [ ] Review notes appended under "Senior Developer Review (AI)"
- [ ] Change Log updated with review entry
- [ ] Status updated according to settings (if enabled)
- [ ] Sprint status synced (if sprint tracking enabled)
- [ ] Story saved successfully

_Reviewer: {{user_name}} on {{date}}_
