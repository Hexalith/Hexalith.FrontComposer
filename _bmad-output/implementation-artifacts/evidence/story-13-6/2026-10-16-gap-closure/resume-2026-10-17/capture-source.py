from pathlib import Path
import json,subprocess,hashlib,re
root=Path.cwd()
prior=json.loads((root/'_bmad-output/implementation-artifacts/evidence/story-13-6/2026-10-16-gap-closure/root-review/verified/source.json').read_text())
def sha(path):return hashlib.sha256((root/path).read_bytes()).hexdigest()
source={p:sha(p) for p in prior['sourceFiles']}
modules=[]
for old in prior['submodules']:
 p=old['path']
 modules.append({'path':p,'head':subprocess.check_output(['git','-C',p,'rev-parse','HEAD'],text=True).strip(),'trackedDiffSha256':hashlib.sha256(subprocess.check_output(['git','-C',p,'diff','--binary'])).hexdigest(),'status':subprocess.check_output(['git','-C',p,'status','--short'],text=True)})
images={p:sha(p) for p in prior['approvedImageSha256']}
assert images==prior['approvedImageSha256']
spec=root/'_bmad-output/implementation-artifacts/spec-13-6-close-automated-verification-gaps.md'
frozen=re.search(r'<frozen-after-approval\b[^>]*>.*?</frozen-after-approval>',spec.read_text(),re.S).group()
assets={}
for project in ['samples/Counter/Counter.Web','tests/Hexalith.FrontComposer.Shell.Tests']:
 path=project+'/obj/project.assets.json'
 restored=json.loads((root/path).read_text())
 assets[path]={'sha256':sha(path),'fluentPackages':[name for name in restored['libraries'] if name.startswith('Microsoft.FluentUI.')]}
binaries={}
for path in ['samples/Counter/Counter.Web/bin/Debug/net10.0/Counter.Web.dll','src/Hexalith.FrontComposer.Shell/bin/Debug/net10.0/Hexalith.FrontComposer.Shell.dll','tests/Hexalith.FrontComposer.Shell.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Shell.Tests.dll','tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.SourceTools.Tests.dll']:
 if (root/path).exists():binaries[path]=sha(path)
identity={'sourceFiles':source,'submodules':modules,'approvedImages':images,'frozenIntentSha256':hashlib.sha256(frozen.encode()).hexdigest(),'existingRestoredAssets':assets,'executedDebugBinaries':binaries}

digest=hashlib.sha256(json.dumps(identity,sort_keys=True,separators=(',',':')).encode()).hexdigest()
packet={'schema':'story-13-6-resume-source-v1','rootCommit':subprocess.check_output(['git','rev-parse','HEAD'],text=True).strip(),'specBaselineCommit':prior['baselineCommit'],'branch':subprocess.check_output(['git','branch','--show-current'],text=True).strip(),'sourceIdentitySha256':digest,'identity':identity,'changedFromPreviousSourceFiles':[p for p,h in source.items() if h!=prior['sourceFiles'][p]],'approvedImageBytesUnchanged':True,'dependencyLimitation':'Debug no-restore checks use existing Fluent 5.0.0 assets. The current Builds catalog asks for 5.0.1 while the conformance gate requires 5.0.0. No clean-restore or upgrade validation is claimed.', 'note':'Current identity replaces the historical packet for this continuation. It includes current root-declared submodule heads; no prior whole-candidate pass is reused.'}
output=root/'_bmad-output/implementation-artifacts/evidence/story-13-6/2026-10-16-gap-closure/resume-2026-10-17/source.json'
output.write_text(json.dumps(packet,indent=2)+'\n')
print(digest)
print('Changed source inputs:',packet['changedFromPreviousSourceFiles'])
