import original from '../../../../../../../tests/e2e/playwright.config.ts';
export default { ...original, testDir: '.', projects: original.projects.filter(project => project.name === 'firefox') };
