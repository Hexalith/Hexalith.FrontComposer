export default {
  extends: ['@commitlint/config-conventional'],
  // Conventional-commit subjects are written in sentence-case (e.g. "feat: Add ...").
  // Header, body, and footer max line lengths are configured to 200 characters.
  rules: {
    'subject-case': [0],
    'header-max-length': [2, 'always', 200],
    'body-max-line-length': [2, 'always', 200],
    'footer-max-line-length': [2, 'always', 200],
  },
};

