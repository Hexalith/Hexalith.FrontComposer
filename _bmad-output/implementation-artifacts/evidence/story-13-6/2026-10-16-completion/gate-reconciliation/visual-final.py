from pathlib import Path
import json,os,subprocess,time,uuid,shutil,hashlib,xml.etree.ElementTree as E
root=Path('/home/administrator/projects/hexalith/frontcomposer');r=root/'_bmad-output/implementation-artifacts/evidence/story-13-6/2026-10-16-completion/gate-reconciliation';c=json.loads((r/'candidate.json').read_text());token=uuid.uuid4().hex[:12];output=Path('/tmp')/f'story13-6-visual-{token}';report=Path('/tmp')/f'story13-6-visual-report-{token}'
env={**os.environ,'BASE_URL':'http://127.0.0.1:5070','PLAYWRIGHT_SKIP_WEBSERVER':'1','FC_E2E_BUILD_CONFIGURATION':'Debug','FC_E2E_OUTPUT_DIR':str(output),'FC_E2E_HTML_REPORT_DIR':str(report),'FC_A11Y_EVIDENCE_RUN_ID':f'story-13-6-visual-{token}'}
cmd=['npm','test','--','specs/specimen-accessibility.spec.ts','--project=chromium','--project=firefox','--project=webkit','--grep','visual baseline','--workers=3'];start=time.monotonic()
with (r/'visual.log').open('w') as log:p=subprocess.run(cmd,cwd=root/'tests/e2e',env=env,stdout=log,stderr=subprocess.STDOUT)
shutil.copy2(output/'junit.xml',r/'visual-junit.xml');j=E.parse(output/'junit.xml').getroot();counts={k:int(j.get(k,0)) for k in ['tests','failures','errors','skipped']};counts['passed']=counts['tests']-counts['failures']-counts['errors']-counts['skipped'];images=[]
for path in sorted(output.rglob('*.png')):
 if not any(path.name.endswith('-'+kind+'.png') for kind in ['expected','actual','diff']):continue
 engine=next((engine for engine in ['chromium','firefox','webkit'] if path.parent.name.endswith('-'+engine)),None)
 assert engine,path
 dest=r/'visual-comparisons'/engine;dest.mkdir(parents=True,exist_ok=True);shutil.copy2(path,dest/path.name)
 images.append({'path':str((dest/path.name).relative_to(r)),'sha256':hashlib.sha256(path.read_bytes()).hexdigest()})
(r/'visual-comparisons').mkdir(exist_ok=True)
(r/'visual-comparisons/inventory.json').write_text(json.dumps({'candidateDigest':c['candidateDigest'],'files':images},indent=2)+'\n')
meta={'candidateDigest':c['candidateDigest'],'command':cmd,'cwd':'tests/e2e','environment':{k:env[k] for k in ['BASE_URL','PLAYWRIGHT_SKIP_WEBSERVER','FC_E2E_BUILD_CONFIGURATION','FC_E2E_OUTPUT_DIR','FC_E2E_HTML_REPORT_DIR']},'exitCode':p.returncode,'durationSeconds':round(time.monotonic()-start,3),'counts':counts,'retainedImages':len(images),'classification':'Actual ordinary visual comparisons; no baseline changes.'}
(r/'visual-verification.json').write_text(json.dumps(meta,indent=2)+'\n');print(json.dumps(meta));raise SystemExit(p.returncode)
