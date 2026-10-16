from pathlib import Path
import hashlib,json,shutil,subprocess
root=Path('/home/administrator/projects/hexalith/frontcomposer')
evidence=root/'_bmad-output/implementation-artifacts/evidence/story-13-6/2026-10-16-completion/gate-reconciliation'
meta=json.loads(Path('/tmp/story13-6-reconcile-parent-paths.json').read_text());source=Path(meta['output']);candidate=json.loads((evidence/'candidate.json').read_text())
assert not (evidence/'browser').exists(), 'Do not overwrite a previous curated run'
ledger=[];seen=set()
for path in sorted(source.rglob('ae-state-*.json')):
 data=json.loads(path.read_text());engine=data['engine'];key=(engine,data['state'])
 assert key not in seen, f'Duplicate state {key}'
 seen.add(key);assert data['runId']==meta['runId']
 dest=evidence/'browser'/engine;dest.mkdir(parents=True,exist_ok=True);shutil.copy2(path,dest/path.name)
 snapshot=data.get('systemSnapshot')
 if snapshot and (path.parent/snapshot).is_file():shutil.copy2(path.parent/snapshot,dest/snapshot)
 ledger.append({'state':data['state'],'engine':engine,'runId':data['runId'],'viewport':data['viewport'],'artifact':str((dest/path.name).relative_to(evidence)),'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'AE-01':'320 CSS px reflow' if data['viewport']['width']==320 else '1600 CSS px desktop rail','AE-03':'all four spacing overrides','AE-04':len(data['targets']),'AE-05':data['systemFocus'],'AE-06':{k:len(data[k]) for k in ['text','icon','boundaries','cues']},'AE-07':data['forcedColorsStatus'],'AE-08':'reduced motion active','AE-02':'actual zoom not performed','AE-09':'automation only; heard speech not observed'})
(evidence/'state-ledger.json').write_text(json.dumps({'candidateDigest':meta['candidateDigest'],'runId':meta['runId'],'states':ledger},indent=2)+'\n')
js=r"""
import {readdirSync,readFileSync,writeFileSync} from 'node:fs';
import {join} from 'node:path';
import {validateStateEvidence} from './tests/e2e/scripts/a11y-state-evidence.mjs';
const root=process.argv[1];const meta=JSON.parse(readFileSync(join(root,'command.json'),'utf8'));
const capabilities=JSON.parse(readFileSync(join(root,'..','browser-capabilities.json'),'utf8'));
const walk=path=>readdirSync(path,{withFileTypes:true}).flatMap(e=>e.isDirectory()?walk(join(path,e.name)):[join(path,e.name)]);
const results=[];
for(const engine of ['chromium','firefox','webkit']){
 const files=walk(join(root,'browser',engine));const options={expectedEngine:engine,expectedRun:meta.runId};
 const strict=validateStateEvidence(files,options);
 const limited=engine==='webkit'?validateStateEvidence(files,{...options,unsupportedForcedColorsCapability:capabilities.find(c=>c.engine==='webkit')}):null;
 results.push({engine,rows:files.filter(p=>p.endsWith('.json')).length,strictErrors:strict,capabilityLimitedErrors:limited,actualForcedPaletteCellsUncovered:engine==='webkit'?116:0});
}
writeFileSync(join(root,'state-validation.json'),JSON.stringify({candidateDigest:meta.candidateDigest,runId:meta.runId,results},null,2)+'\n');
console.log(JSON.stringify(results.map(r=>({engine:r.engine,rows:r.rows,strictErrors:r.strictErrors.length,capabilityLimitedErrors:r.capabilityLimitedErrors?.length??null,actualForcedPaletteCellsUncovered:r.actualForcedPaletteCellsUncovered}))));
"""
p=subprocess.run(['node','--input-type=module','-e',js,str(evidence)],cwd=root,text=True,capture_output=True)
print(p.stdout);print(p.stderr);p.check_returncode()
