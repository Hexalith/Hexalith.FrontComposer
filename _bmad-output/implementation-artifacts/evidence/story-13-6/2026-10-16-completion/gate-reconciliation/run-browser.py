from pathlib import Path
import hashlib,json,os,subprocess,time,uuid,xml.etree.ElementTree as ET
root=Path('/home/administrator/projects/hexalith/frontcomposer')
evidence=root/'_bmad-output/implementation-artifacts/evidence/story-13-6/2026-10-16-completion/gate-reconciliation'
candidate=json.loads((evidence/'candidate.json').read_text())
digest=candidate['candidateDigest']
files=candidate['files']
token=uuid.uuid4().hex[:12]
output=Path('/tmp')/f'story13-6-reconcile-final-{token}'
html=Path('/tmp')/f'story13-6-reconcile-final-report-{token}'
runid=f'story-13-6-reconciliation-{token}'
env={**os.environ,'BASE_URL':'http://127.0.0.1:5070','PLAYWRIGHT_SKIP_WEBSERVER':'1','FC_E2E_BUILD_CONFIGURATION':'Debug','FC_E2E_OUTPUT_DIR':str(output),'FC_E2E_HTML_REPORT_DIR':str(html),'FC_A11Y_EVIDENCE_RUN_ID':runid}
cmd=['npm','test','--','specs/specimen-accessibility.spec.ts','specs/responsive-accessibility.spec.ts','specs/accessibility-state-matrix.spec.ts','--project=chromium','--project=firefox','--project=webkit','--workers=3']
meta={'candidateDigest':digest,'cwd':'tests/e2e','command':cmd,'environment':{k:env[k] for k in ['BASE_URL','PLAYWRIGHT_SKIP_WEBSERVER','FC_E2E_BUILD_CONFIGURATION','FC_E2E_OUTPUT_DIR','FC_E2E_HTML_REPORT_DIR','FC_A11Y_EVIDENCE_RUN_ID']},'runId':runid,'output':str(output),'html':str(html)}
(evidence/'command.json').write_text(json.dumps(meta,indent=2)+'\n')
Path('/tmp/story13-6-reconcile-parent-paths.json').write_text(json.dumps(meta))
print(json.dumps({'candidate':digest,'files':len(files),'runId':runid,'output':str(output)}),flush=True)
start=time.monotonic()
with (evidence/'browser.log').open('w') as log:
    proc=subprocess.run(cmd,cwd=root/'tests/e2e',env=env,stdout=log,stderr=subprocess.STDOUT)
meta.update(exitCode=proc.returncode,durationSeconds=round(time.monotonic()-start,3))
if (output/'junit.xml').exists():
    import shutil
    shutil.copy2(output/'junit.xml',evidence/'browser-junit.xml')
    doc=ET.parse(output/'junit.xml').getroot()
    suites=[]
    failures=[]
    for s in doc.findall('testsuite'):
        cases=s.findall('testcase');fail=[c for c in cases if c.find('failure') is not None or c.find('error') is not None];skip=[c for c in cases if c.find('skipped') is not None]
        suites.append({'project':s.get('hostname'),'spec':s.get('name'),'total':len(cases),'passed':len(cases)-len(fail)-len(skip),'failed':len(fail),'skipped':len(skip)})
        failures.extend({'project':s.get('hostname'),'name':c.get('name'),'message':''.join((c.find('failure') if c.find('failure') is not None else c.find('error')).itertext())[:2000]} for c in fail)
    meta.update(suites=suites,failures=failures)
(evidence/'browser-verification.json').write_text(json.dumps(meta,indent=2)+'\n')
print(json.dumps({k:v for k,v in meta.items() if k not in ['failures','environment']}),flush=True)
raise SystemExit(proc.returncode)
