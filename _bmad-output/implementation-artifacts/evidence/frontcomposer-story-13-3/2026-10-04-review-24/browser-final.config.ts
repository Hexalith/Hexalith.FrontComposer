import config from "tests/e2e/playwright.config.ts";
export default {
 ...config,
 testDir: "tests/e2e/specs",
 outputDir: "<temporary-verification-directory>/browser-final-results",
 reporter: [["list"], ["json", {outputFile: "<temporary-verification-directory>/browser-final.json"}], ["junit", {outputFile: "<temporary-verification-directory>/browser-final.xml"}]],
 workers: 2,
 use: {...config.use, baseURL: "http://127.0.0.1:36871"},
 projects: config.projects.map(p => ({...p, use: {...p.use, launchOptions: {args: ["--disable-gpu", "--disable-software-rasterizer"]}}})),
 webServer: {...config.webServer,
  cwd: "tests/e2e",
  command: config.webServer.command.replace("--configuration Release", "--configuration Debug").replace("5070", String(36871)),
  url: "http://127.0.0.1:36871", reuseExistingServer: false
 }
};
