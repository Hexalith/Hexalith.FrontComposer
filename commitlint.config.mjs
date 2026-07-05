export default {
  extends: ['@commitlint/config-conventional'],
  // Sprint Change Proposal 2026-07-05 (CI Package Boundary / Fluent Pin): the team's
  // conventional-commit subjects are written in sentence-case (e.g. "feat: Add ...") and
  // change-proposal bodies routinely exceed the 100-character default line cap. Both rules are
  // relaxed so that the push-to-main `commitlint --last` check and PR validation no longer fail on
  // these house-style messages. Type/scope/format enforcement from config-conventional is retained.
  rules: {
    'subject-case': [0],
    'body-max-line-length': [0],
  },
};
