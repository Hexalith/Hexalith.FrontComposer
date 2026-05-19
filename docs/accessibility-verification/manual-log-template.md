# Manual Screen Reader Verification Log

## Per-Status Required Fields

Use this matrix to identify which fields are required for the canonical status selected for a row. Field labels match the entries in the "Log Fields" section below. Core identity fields (Stable gate id, Task ids, Acceptance criteria ids, Release branch or tag, Commit or immutable artifact reference, Canonical status) are always required.

| Canonical status | Required fields beyond core identity |
| --- | --- |
| `completed` | Date; Tester (human only; see Tester field note); Operating system; Browser and version; Screen reader and version; Specimen route or flow; UX baseline or responsive tier; Result: Pass or Fail; Issue links (required when Result=Fail); Reviewer or sign-off owner; Approval references (one per required approver); Sanitization status; Evidence attachment paths or links |
| `not performed` | Owner; Release impact; Reason; Next action (Decision needed); Reopen event or revalidation trigger |
| `blocked` | Owner; Release impact; Blocker ref; Decision needed; Reopen event or revalidation trigger |
| `accepted v1 constraint` | Owner; Release impact; Downstream consumer impact; Adopter communication need; Evidence reference; Expiry or revalidation trigger; Reopen event; Approval references (one per canonical role: Product, Quality/Test, Accessibility/Stakeholder, Release Owner) |
| `post-v1 roadmap` | Owner; Story or roadmap reference; Target release or non-planning rationale; Release impact; Reopen event |

## Log Fields

- Stable gate id:
- Task ids:
- Acceptance criteria ids:
- Release branch or tag:
- Commit or immutable artifact reference:
- Date:
- Tester (human only for manual AT/device `completed` rows; AI-agent identifiers such as model names are valid as `Prepared by` on a pack, never as the tester of a manual `completed` row):
- Operating system:
- Browser and version:
- Screen reader and version:
- Specimen route or flow:
- UX baseline or responsive tier:
- Canonical status: `completed` / `not performed` / `blocked` / `accepted v1 constraint` / `post-v1 roadmap`
- Result if completed: Pass / Fail
- Issue links (required when Result=Fail):
- Resolution status:
- Owner:
- Release impact:
- Reason (required for `not performed`):
- Decision needed (required for `blocked`):
- Reopen event or revalidation trigger:
- Downstream consumer impact (required for `accepted v1 constraint`):
- Adopter communication need (required for `accepted v1 constraint`):
- Story or roadmap reference (required for `post-v1 roadmap`):
- Target release or non-planning rationale (required for `post-v1 roadmap`):
- Reviewer or sign-off owner:
- Approval references (list one per required approver; for `completed` the reviewer/sign-off owner; for `accepted v1 constraint` the four canonical roles Product, Quality/Test, Accessibility/Stakeholder, and Release Owner):
- Sanitization status:
- Evidence attachment paths or links:

## Delegation or Proxy Sign-off

If any approval recorded in this log is provided through a delegate or proxy, the following fields are required (otherwise the sign-off is treated as missing per AC34/D19):

- Delegating principal (the canonical role being represented):
- Delegate or proxy identity:
- Delegation authority (auditable reference proving the delegation was granted):
- Delegation scope (specific approvals or gates covered):
- Delegation expiration:
- Approving role:

## Notes

Record observed announcement quality, keyboard reachability, focus visibility, and unresolved issues. Do not mark a combination as passed unless it was manually exercised.

If the gate was not exercised, do not leave the row blank and do not mark pass. Classify it as `not performed`, `blocked`, `accepted v1 constraint`, or `post-v1 roadmap` with the per-status required fields filled in.

Do not include raw screen-reader transcripts, personal data, tenant/user values, secrets, local absolute paths, cookies, full DOM dumps, command payloads, or unbounded logs.
