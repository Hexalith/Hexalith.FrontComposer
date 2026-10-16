from pathlib import Path
import json,hashlib,re,subprocess,xml.etree.ElementTree as E,tarfile
from urllib.parse import urlsplit,unquote
root=Path.cwd();art=root/'_bmad-output/implementation-artifacts';r=art/'evidence/story-13-6/2026-10-16-completion/gate-reconciliation';c=json.loads((r/'candidate.json').read_text());errors=[];h=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
for f in c['files']:
 if not (root/f['path']).is_file() or h(root/f['path'])!=f['sha256']:errors.append('Source mismatch '+f['path'])
rows=''.join(f"{f['sha256']}  {f['path']}\n" for f in c['files'])
if 'sha256:'+hashlib.sha256(rows.encode()).hexdigest()!=c['candidateDigest'] or (r/'candidate.sha256').read_bytes()!=rows.encode():errors.append('Aggregate mismatch')
for name,expected in c['frozenIntentSha256'].items():
 data=(art/name).read_bytes();block=re.search(rb'<frozen-after-approval[^>]*>.*?</frozen-after-approval>',data,re.S).group(0)
 if hashlib.sha256(block).hexdigest()!=expected:errors.append('Frozen intent mismatch '+name)
for f in json.loads((r/'baseline-preservation.json').read_text())['files']:
 if h(root/f['path'])!=f['sha256']:errors.append('Baseline mismatch '+f['path'])
b=json.loads((r/'browser-candidate.json').read_text());current={f['path']:f['sha256'] for f in c['files']};prior={f['path']:f['sha256'] for f in b['files']}
if any(current.get(p)!=v for p,v in prior.items()):errors.append('Browser input drift')
if set(current)-set(prior)!={'tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/ProjectionSubscriptionServiceTests.cs'}:errors.append('Unexpected final delta')
j=E.parse(r/'browser-junit.xml').getroot();browser={k:int(j.get(k,0)) for k in ['tests','failures','errors','skipped']};browser['passed']=browser['tests']-browser['failures']-browser['errors']-browser['skipped'];a=E.parse(r/'shell.xml').getroot().find('assembly');shell={k:int(a.get(k,0)) for k in ['total','passed','failed','errors','skipped']}
if shell!={'total':3681,'passed':3681,'failed':0,'errors':0,'skipped':0}:errors.append('Unexpected Shell counts')
if browser!={'tests':381,'failures':13,'errors':0,'skipped':2,'passed':366}:errors.append('Unexpected browser counts')
ledger=json.loads((r/'state-ledger.json').read_text());states={e:0 for e in ['chromium','firefox','webkit']}
for row in ledger['states']:
 p=r/row['artifact'];data=json.loads(p.read_text());states[row['engine']]+=1
 if h(p)!=row['sha256'] or data['state']!=row['state'] or data['engine']!=row['engine'] or data['runId']!=ledger['runId']:errors.append('State identity mismatch '+row['artifact'])
 if data.get('systemSnapshot') and not (p.parent/data['systemSnapshot']).is_file():errors.append('Missing adjacent snapshot '+row['artifact'])
if states!={'chromium':116,'firefox':116,'webkit':116}:errors.append('State count mismatch')
for name in ['visual-comparisons/inventory.json','pre-cleanup/visual-comparisons/inventory.json']:
 base=r if not name.startswith('pre-cleanup') else r/'pre-cleanup'
 for f in json.loads((r/name).read_text())['files']:
  if h(base/f['path'])!=f['sha256']:errors.append('Comparison image mismatch '+f['path'])
ret=json.loads((r/'report-retention.json').read_text())
if h(r/ret['diagnosticArchive'])!=ret['diagnosticArchiveSha256']:errors.append('Archive hash mismatch')
with tarfile.open(r/ret['diagnosticArchive'],'r:gz') as t:
 for f in ret['archivedFiles']:
  data=t.extractfile(f['path']).read()
  if len(data)!=f['bytes'] or hashlib.sha256(data).hexdigest()!=f['sha256']:errors.append('Archive member mismatch '+f['path'])
files=[art/'13-6-accessibility-result.md',art/'spec-13-6-complete-automated-accessibility-coverage.md',art/'spec-13-6-verify-responsive-and-assistive-accessibility.md',r/'visual-diagnosis.md',r/'review/triage.md']
for p in files:
 for link in re.findall(r'\[[^\]]+\]\(([^)]+)\)',p.read_text()):
  u=urlsplit(link)
  if u.scheme or not u.path:continue
  target=p.parent/unquote(u.path)
  if target.resolve()==(r/'parent-audit.json').resolve():continue
  if not target.exists():errors.append('Broken active link '+str(p.relative_to(root))+' -> '+link)
attribute=subprocess.check_output(['git','check-attr','text',str((r/'candidate.json').relative_to(root))],text=True).strip()
if not attribute.endswith(': unset'):errors.append('Evidence byte attributes missing')
if list((root/'tests/e2e/specs').glob('__13-6*')):errors.append('Temporary diagnostic spec remains')
result={'candidateDigest':c['candidateDigest'],'sourceFileCount':len(c['files']),'frozenIntentHashesMatch':not any('Frozen' in x for x in errors),'baselineImagesChecked':24,'browserExistingCandidateFilesIdentical':len(prior),'shell':shell,'browser':browser,'stateLedgerCounts':states,'activeDocumentsChecked':len(files),'evidenceTextAttribute':attribute,'result':'passed' if not errors else 'failed','errors':errors,'scope':'Raw-byte provenance/artifact/count/link audit only. Passing this audit certifies neither browser/visual gates nor AE coverage or human/owner approvals.'};(r/'parent-audit.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result,indent=2));raise SystemExit(bool(errors))
