from pathlib import Path
import json,os,shutil,subprocess
root=Path('/home/administrator/projects/hexalith/frontcomposer');review=root/'_bmad-output/implementation-artifacts/evidence/story-13-6/2026-10-16-completion/gate-reconciliation'
m=json.loads((review/'command.json').read_text());source=Path(m['output']);dest=review/'browser/chromium';report=review/'playwright-report'
if report.exists():
 assert (report/'index.html').read_bytes()==(Path(m['html'])/'index.html').read_bytes()
else:shutil.copytree(m['html'],report);shutil.copy2(source/'junit.xml',dest/'junit.xml')
copied=[]
for p in source.rglob('*'):
 if p.is_file() and p.parent.name.endswith('-chromium') and (p.name.startswith('axe-') and p.suffix=='.json' or p.name.startswith('focus-') and p.suffix=='.png'):
  producer=dest/'specimen-captures'/p.parent.name;producer.mkdir(parents=True,exist_ok=True)
  shutil.copy2(p,producer/p.name);copied.append(str((producer/p.name).relative_to(review)))
cmd=['npm','run','validate:a11y-artifacts'];env={**os.environ,'FC_E2E_OUTPUT_DIR':str(dest),'FC_E2E_HTML_REPORT_DIR':str(report)}
p=subprocess.run(cmd,cwd=root/'tests/e2e',env=env,text=True,stdout=subprocess.PIPE,stderr=subprocess.STDOUT)
(review/'artifact-validation.log').write_text(p.stdout)
result={'candidateDigest':m['candidateDigest'],'command':cmd,'cwd':'tests/e2e','exitCode':p.returncode,'environment':{k:env[k] for k in ['FC_E2E_OUTPUT_DIR','FC_E2E_HTML_REPORT_DIR']},'copiedSpecimenArtifacts':copied,'curation':'Strict existing Windows inventory validator executed on Linux against current-run Chromium raw state/axe/focus artifacts and the actual shared three-engine JUnit/HTML report. Other-engine state rows are retained separately. This supplies no Windows execution claim.'}
(review/'artifact-validation.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result));print(p.stdout)
raise SystemExit(p.returncode)
