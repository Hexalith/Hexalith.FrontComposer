import config from "/home/administrator/projects/hexalith/frontcomposer/tests/e2e/playwright.config.ts";
export default {
 ...config,
 testDir: "/home/administrator/projects/hexalith/frontcomposer/tests/e2e/specs",
 outputDir: "/tmp/fc-story-13-3-review-zpidrz_f" + "/browser-final-results",
 reporter: [["list"], ["json", {outputFile: "/tmp/fc-story-13-3-review-zpidrz_f" + "/browser-final.json"}], ["junit", {outputFile: "/tmp/fc-story-13-3-review-zpidrz_f" + "/browser-final.xml"}]],
 workers: 2,
 use: {...config.use, baseURL: "http://127.0.0.1:49097"},
 projects: config.projects.map(p => ({...p, use: {...p.use, launchOptions: {args: ["--disable-gpu", "--disable-software-rasterizer"]}}})),
 webServer: {...config.webServer,
  cwd: "/home/administrator/projects/hexalith/frontcomposer/tests/e2e",
  command: config.webServer.command.replace("--configuration Release", "--configuration Debug").replace("5070", String(49097)),
  url: "http://127.0.0.1:49097", reuseExistingServer: false
 }
};
