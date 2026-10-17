from pathlib import Path
import hashlib,json,subprocess,re
root=Path.cwd()
p=root/'_bmad-output/implementation-artifacts/evidence/story-13-6/2026-10-16-gap-closure/review-current-2026-10-17'
s=json.loads((p/'source.json').read_text());i=s['identity']
def require(condition, message):
 if not condition:raise ValueError(message)
def sha(path):return hashlib.sha256((root/path).read_bytes()).hexdigest()
require(hashlib.sha256(json.dumps(i,sort_keys=True,separators=(',',':')).encode()).hexdigest()==s['sourceIdentitySha256'],'Source identity recipe')
for group in ['sourceFiles','approvedImages','executedDebugBinaries','executedScopedStyles','executedDependencyCopies']:
 for path,digest in i[group].items():require(sha(path)==digest,group+': '+path)
for copy,built in [('samples/Counter/Counter.Web/bin/Debug/net10.0/Hexalith.FrontComposer.Shell.dll','src/Hexalith.FrontComposer.Shell/bin/Debug/net10.0/Hexalith.FrontComposer.Shell.dll'),('tests/Hexalith.FrontComposer.Shell.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Shell.dll','src/Hexalith.FrontComposer.Shell/bin/Debug/net10.0/Hexalith.FrontComposer.Shell.dll'),('tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.SourceTools.dll','src/Hexalith.FrontComposer.SourceTools/bin/Debug/netstandard2.0/Hexalith.FrontComposer.SourceTools.dll')]:
 require(i['executedDependencyCopies'][copy]==i['executedDebugBinaries'][built],'Loaded copy differs '+copy)
for path,data in i['existingRestoredAssets'].items():require(sha(path)==data['sha256'],'Restore assets '+path)
for path,digest in i['frozenIntentsSha256'].items():
 block=re.search(r'<frozen-after-approval\b[^>]*>.*?</frozen-after-approval>',(root/path).read_text(),re.S).group()
 require(hashlib.sha256(block.encode()).hexdigest()==digest,'Frozen intent '+path)
for module in i['submodules']:
 require(subprocess.check_output(['git','-C',module['path'],'rev-parse','HEAD'],text=True).strip()==module['head'],'Submodule HEAD '+module['path'])
 require(subprocess.check_output(['git','-C',module['path'],'status','--short'],text=True)==module['status'],'Submodule status '+module['path'])
require(subprocess.check_output(['git','rev-parse','HEAD'],text=True).strip()==s['rootCommit'],'Root HEAD')
require(subprocess.check_output(['git','branch','--show-current'],text=True).strip()==s['branch'],'Root branch')
require(subprocess.run(['git','diff','--cached','--quiet']).returncode==0,'Index changed')
spec=root/'_bmad-output/implementation-artifacts/spec-13-6-verify-responsive-and-assistive-accessibility.md'
text=spec.read_text()
status_line=next(line for line in text.splitlines() if line.startswith('status:'))
require(status_line.split(':',1)[1].strip().strip("'\"")=='in-review','Story status')
require('3c8679628ebdb8995afc09f712a04fcde623695c' in text,'Original baseline')
sprint=(root/'_bmad-output/implementation-artifacts/sprint-status.yaml').read_text();require(re.search(r'^\s*13-6-i-ux-e-verify-responsive-and-assistive-accessibility: review\s*$',sprint,re.M) is not None,'Sprint status')
result=root/'_bmad-output/implementation-artifacts/13-6-accessibility-result.md';require(result.read_text().count('- [ ]')==18,'Manual checklist boxes')
files=[x for x in p.rglob('*') if x.is_file()]
ignored=subprocess.run(['git','check-ignore','--stdin'],input='\n'.join(str(x.relative_to(root)) for x in files)+'\n',text=True,capture_output=True);require(not ignored.stdout,'Ignored evidence files '+ignored.stdout)
documentTargets=[]
for document in [p/'report.md',result,spec]:
 for link in re.findall(r'\[[^\]]*\]\(([^)]+)\)',document.read_text()):
  if re.match(r'^[a-z]+:|^#',link):continue
  target=link.split('#',1)[0].strip('<>')
  if target:documentTargets.append((document.parent/target,str(document)+': '+link))
check=subprocess.run(['git','diff','--check'],text=True,capture_output=True);require(check.returncode==0,'Whitespace '+check.stdout+check.stderr)
cleanup=json.loads((p/'cleanup.json').read_text());require(cleanup['exitCode']==0 and not cleanup['apphostStillRunning'] and not cleanup['counterStillRunning'] and not cleanup['endpointReachable'],'AppHost cleanup')
archives=json.loads((p/'archive-integrity.json').read_text());require(all(x['verified'] for x in archives['results']),'Archive integrity')
audit={'sourceIdentitySha256':s['sourceIdentitySha256'],'sourceFilesVerified':len(i['sourceFiles']),'debugBinariesVerified':len(i['executedDebugBinaries']),'compiledScopedStylesVerified':len(i['executedScopedStyles']),'executedDependencyCopiesVerified':len(i['executedDependencyCopies']),'restoreAssetsVerified':len(i['existingRestoredAssets']),'approvedImageBytesPreserved':len(i['approvedImages']),'frozenIntentBlocksPreserved':len(i['frozenIntentsSha256']),'rootSubmodulesPreserved':len(i['submodules']),'rootHeadBranchIndexUnchanged':True,'manualBoxesStillOpen':18,'documentationLinksValid':True,'noIgnoredEvidenceFiles':True,'whitespaceExitCode':check.returncode,'apphostStopped':True,'storyStatus':'in-review','sprintStatus':'review','humanOrOwnerApprovalInferred':False,'dependencyUpgradePerformed':False,'cleanRestoreCompatibilityClaimed':False}
(p/'audit.json').write_text(json.dumps(audit,indent=2)+'\n')
hashes={str(x.relative_to(p)):hashlib.sha256(x.read_bytes()).hexdigest() for x in sorted(p.rglob('*')) if x.is_file() and x.name!='evidence-sha256.json'}
(p/'evidence-sha256.json').write_text(json.dumps(hashes,indent=2)+'\n')
for target,label in documentTargets:require(target.exists(),'Missing link '+label)
print(json.dumps(audit,indent=2));print('Sealed evidence files:',len(hashes))
