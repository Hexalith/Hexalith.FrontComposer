import { readFileSync, writeFileSync, readdirSync, mkdirSync, copyFileSync, statSync } from 'node:fs';
import { resolve, join, relative, dirname, basename, isAbsolute } from 'node:path';
import { existsSync } from 'node:fs';
import { parseArgs } from 'node:util';
import { pathToFileURL, fileURLToPath } from 'node:url';
import { createHash } from 'node:crypto';
import { spawnSync } from 'node:child_process';
const packet = dirname(fileURLToPath(import.meta.url));
const { values } = parseArgs({ options: { 'repo-root': { type: 'string' }, 'evidence-root': { type: 'string' }, 'output-root': { type: 'string' }, 'run-id': { type: 'string' } } });
let repo = resolve(values['repo-root'] ?? process.env.FC_REPO_ROOT ?? packet);
while (!existsSync(join(repo, 'Hexalith.FrontComposer.slnx'))) {
 const next = dirname(repo);
 if (next === repo) throw new Error('Repository root unavailable; supply --repo-root');
 repo = next;
}
const evidenceRoot = resolve(values['evidence-root'] ?? process.env.FC_A11Y_EVIDENCE_ROOT ?? join(packet, 'restored'));
const output = join(evidenceRoot, 'current-browser', 'results'), html = join(evidenceRoot, 'current-browser', 'report');
const base = resolve(values['output-root'] ?? join(packet, 'replay-audit'));
mkdirSync(base, { recursive: true });
const captured = JSON.parse(readFileSync(join(packet, 'raw-artifacts.json'), 'utf8'));
const restoredPath = path => {
 for (const [label, original] of Object.entries(captured.roots).sort((a, b) => b[1].length - a[1].length)) {
  const suffix = relative(original, path);
  if (suffix === '' || suffix !== '..' && !suffix.startsWith('../') && !isAbsolute(suffix)) return join(evidenceRoot, label, suffix);
 }
 return path;
};
const { validateStateEvidence, requiredStates }=await import(pathToFileURL(join(repo,'tests/e2e/scripts/a11y-state-evidence.mjs')));
const walk=root=>readdirSync(root,{withFileTypes:true}).flatMap(entry=>entry.isDirectory()?walk(join(root,entry.name)):[join(root,entry.name)]);
const legacyManifest=JSON.parse(readFileSync(join(repo,'tests/e2e/specimens/frontcomposer-specimen-manifest.json'),'utf8'));
const legacyNames=new Set(legacyManifest.routes.flatMap(route=>route.expectedArtifacts).filter(name=>name.startsWith('axe-')||name.startsWith('focus-')));
const files=walk(output), report=JSON.parse(readFileSync(join(output,'playwright.json'),'utf8'));
const run=values['run-id'] ?? report.config.metadata.a11yEvidenceRunId;
const rebase=suite=>{for(const spec of suite.specs??[])for(const test of spec.tests??[])for(const result of test.results??[])for(const attachment of result.attachments??[])if(attachment.path)attachment.path=restoredPath(attachment.path);for(const child of suite.suites??[])rebase(child);};
for(const suite of report.suites??[])rebase(suite);
const replayReport=join(base,'raw-replay.json');
writeFileSync(replayReport,JSON.stringify(report));
const tests=[];
const visit=suite=>{for(const spec of suite.specs??[])for(const test of spec.tests??[])tests.push({id:spec.id,title:spec.title,...test});for(const child of suite.suites??[])visit(child);};
visit(report);
const sha=file=>createHash('sha256').update(readFileSync(file)).digest('hex');
const json=(file,value)=>writeFileSync(join(base,file),JSON.stringify(value,null,2)+'\n');
const audit=[];
for(const engine of ['chromium','firefox','webkit']) {
 const ownedTests=tests.filter(test=>test.projectName===engine);
 const rawValidation=spawnSync('node',['scripts/validate-a11y-artifacts.mjs'],{cwd:join(repo,'tests/e2e'),env:{...process.env,FC_E2E_OUTPUT_DIR:output,FC_E2E_HTML_REPORT_DIR:html,FC_E2E_JSON_PATH:replayReport,FC_A11Y_EVIDENCE_RUN_ID:run,FC_A11Y_EVIDENCE_ENGINE:engine},encoding:'utf8'});
 writeFileSync(join(base,'artifact-raw-'+engine+'.log.txt'),rawValidation.stdout+rawValidation.stderr);
 const owned=files.filter(file=>!/^ae-state-.+\.json$/u.test(basename(file))||JSON.parse(readFileSync(file,'utf8')).engine===engine);
 const attachments=ownedTests.filter(test=>test.results.at(-1)?.status==='passed'&&['expected','flaky'].includes(test.status)).flatMap(test=>test.results.at(-1).attachments??[]).filter(attachment=>attachment.name==='forced-color-capability');
 const capability=attachments.length===1?JSON.parse(readFileSync(attachments[0].path,'utf8')):null;
 const strict=validateStateEvidence(owned,{expectedEngine:engine,expectedRun:run,report});
 const attributed=validateStateEvidence(owned,{expectedEngine:engine,expectedRun:run,report,unsupportedForcedColorsCapability:capability});
 const stateFiles=owned.filter(file=>/^ae-state-.+\.json$/u.test(basename(file)));
 const rows=stateFiles.map(file=>({file,evidence:JSON.parse(readFileSync(file,'utf8'))})).filter(({evidence})=>ownedTests.some(test=>test.id===evidence.testId&&test.results.at(-1)?.retry===evidence.attempt&&test.results.at(-1)?.status==='passed'));
 const states=[...new Set(rows.map(row=>row.evidence.state))].sort();
 const view=join(evidenceRoot,'bounded-replay',engine), artifacts=join(view,'test-results'), viewHtml=join(view,'report');
 mkdirSync(artifacts,{recursive:true});mkdirSync(viewHtml,{recursive:true});
 const directories=new Set(ownedTests.flatMap(test=>test.results.flatMap(result=>result.attachments??[])).filter(attachment=>attachment.path?.startsWith(output+'/')).map(attachment=>relative(output,attachment.path).split('/')[0]));
 const relocated=new Map();
 for(const file of files)if((directories.has(relative(output,file).split('/')[0])&&/\.(json|png)$/u.test(file))||legacyNames.has(basename(file))) {const target=join(artifacts,relative(output,file));mkdirSync(dirname(target),{recursive:true});copyFileSync(file,target);relocated.set(file,target);}
 const rebound=structuredClone(report);
 const relocate=suite=>{for(const spec of suite.specs??[])for(const test of spec.tests??[])for(const result of test.results??[])for(const attachment of result.attachments??[])if(relocated.has(attachment.path))attachment.path=relocated.get(attachment.path);for(const child of suite.suites??[])relocate(child);};
 relocate(rebound);
 writeFileSync(join(artifacts,'playwright.json'),JSON.stringify(rebound));copyFileSync(join(output,'junit.xml'),join(artifacts,'junit.xml'));const completeReport=join(html,'index.html');
 if(!existsSync(completeReport))throw new Error('Complete retained raw HTML report is missing');
 const reportLink=relative(viewHtml,completeReport).split('/').map(encodeURIComponent).join('/');
 writeFileSync(join(viewHtml,'index.html'),`<!doctype html><meta charset="utf-8"><title>Bounded evidence projection</title><h1>Bounded evidence projection: ${engine}</h1><p>This projection retains validator JSON and images. Trace, video and other attachments remain in the complete retained report.</p><a id="complete-raw-report" href="${reportLink}">Open complete raw Playwright report with all attachments</a>`);
 const validation=spawnSync('node',['scripts/validate-a11y-artifacts.mjs'],{cwd:join(repo,'tests/e2e'),env:{...process.env,FC_E2E_OUTPUT_DIR:artifacts,FC_E2E_HTML_REPORT_DIR:viewHtml,FC_E2E_JSON_PATH:join(artifacts,'playwright.json'),FC_A11Y_EVIDENCE_RUN_ID:run,FC_A11Y_EVIDENCE_ENGINE:engine},encoding:'utf8'});
 writeFileSync(join(base,'artifact-'+engine+'.log.txt'),validation.stdout+validation.stderr);
 const size=root=>walk(root).reduce((sum,file)=>sum+statSync(file).size,0);
 audit.push({engine,counts:{passed:ownedTests.filter(test=>test.status==='expected').length,failed:ownedTests.filter(test=>test.status==='unexpected').length,flaky:ownedTests.filter(test=>test.status==='flaky').length,skipped:ownedTests.filter(test=>test.status==='skipped').length},stateCount:states.length,missingStates:requiredStates.filter(state=>!states.includes(state)),strictErrors:strict,capabilityAttributedErrors:attributed,capability,rawArtifactValidation:{exitCode:rawValidation.status},artifactValidation:{exitCode:validation.status,outputBytes:size(artifacts),htmlBytes:size(viewHtml),reportSha256:sha(join(artifacts,'playwright.json')),relocatedAttachmentCount:relocated.size},rows:rows.map(({file,evidence})=>({state:evidence.state,file:relative(output,file),sha256:sha(file),testId:evidence.testId,attempt:evidence.attempt}))});
}
json('browser-outcomes.json',{run,stats:report.stats,errors:report.errors,tests:tests.map(test=>({id:test.id,engine:test.projectName,title:test.title,status:test.status,expectedStatus:test.expectedStatus,attempts:test.results.map(result=>({retry:result.retry,status:result.status,duration:result.duration,error:result.error?.message,attachments:result.attachments}))}))});
json('state-validation.json',{run,source:JSON.parse(readFileSync(join(packet,'source.json'),'utf8')).sourceIdentitySha256,rawReportSha256:sha(join(output,'playwright.json')),replayReportSha256:sha(replayReport),evidenceRoot,results:audit});
console.log(JSON.stringify(audit.map(({engine,counts,stateCount,missingStates,strictErrors,capabilityAttributedErrors,artifactValidation})=>({engine,counts,stateCount,missingStates,strictErrorCount:strictErrors.length,attributedErrorCount:capabilityAttributedErrors.length,artifactValidation})),null,2));

const failed=audit.some(row=>row.rawArtifactValidation.exitCode!==0||row.artifactValidation.exitCode!==0||row.strictErrors.length||row.capabilityAttributedErrors.length||row.missingStates.length);
const existingFailedGates=tests.filter(test=>test.status==='unexpected').map(test=>({engine:test.projectName,title:test.title,kind:test.title.startsWith('visual baseline')?'recorded visual comparison':'recorded test failure'}));
json('gate-status.json',{failed,existingFailedGates,unsupportedForcedPalette:audit.filter(row=>row.capability&&row.strictErrors.length>row.capabilityAttributedErrors.length).map(row=>row.engine),note:'Existing failed gates remain failures. Replay and bounded projection never imply overall readiness.'});
if(failed){console.error('Evidence validation failed; retained diagnostics distinguish existing failed gates in gate-status.json.');process.exitCode=1;}
