"""Finite ledger/count/admission controls only, never a full native mutation qualification."""
import copy
import hashlib
import importlib.util
import json
import re
import os
from pathlib import Path
import shutil
import tempfile
import subprocess
import sys
import unittest

OWNER=Path(__file__).resolve().parents[2]
spec=importlib.util.spec_from_file_location('finite_triage',OWNER/'eng/check-mutation-triage.py')
check=importlib.util.module_from_spec(spec);spec.loader.exec_module(check)

class FiniteTriageTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.ledger,cls.manifest,cls.rows,cls.sources=check.load_metadata(OWNER)
        cls.history=json.loads((OWNER/check.HISTORICAL_LEDGER).read_text())
        cls.historical_rows={row['id']:row for row in cls.history['mutations']}

    def current_row_for_historical_id(self, ident):
        if 'currentIdentityLedger' not in self.manifest:
            return self.rows[ident]
        return next((row for row in self.rows.values() if ident in row['historicalIds']), None)

    def copy_inputs(self, owner):
        catalog_path=self.history['evidenceCatalog']['path']
        catalog=json.loads((OWNER/catalog_path).read_text())
        paths={*self.ledger['sourceFiles'], check.HISTORICAL_LEDGER, check.REL+'mutation-target-manifest.json',
               'eng/check-mutation-triage.py', catalog_path}
        if 'currentIdentityLedger' in self.manifest:
            paths.add(check.CURRENT_LEDGER)
            paths.update(path for row in self.rows.values() for path in row.get('control', {}).get('testSources', {}))
            inherited={row['historicalIds'][0] for row in self.rows.values()
                       if row['action']=='kill-test-added' and 'control' not in row}
        else:
            inherited=set(catalog['cases'])
        paths.update(item['ownerFile'] for ident in inherited for item in catalog['cases'][ident]['currentPreservation'])
        for path in paths:
            target=owner/path
            target.parent.mkdir(parents=True,exist_ok=True)
            shutil.copyfile(OWNER/path,target)

    def report(self,row,status,segment=None):
        segment=segment or next(iter(row['allowedRemaining']))
        files={}
        # Complete realistic inventory: removing a reviewed mutant still leaves every
        # approved source file and many live mutants, satisfying the old shallow checks.
        for root in self.manifest['approvedTargetRoots']:
            for path in (OWNER/root).rglob('*.cs'):
                relative=path.relative_to(OWNER).as_posix()
                files[relative]={'language':'cs','source':path.read_text(),'mutants':[]}
        for expected in self.rows.values():
            if segment in expected['allowedRemaining']:
                files[expected['path']]['mutants'].append({'id':expected['id'],'status':'Killed','mutatorName':expected['mutator'],
                    'location':check.location(expected['span']),'replacement':expected['replacement']})
        for index,file in enumerate(files.values()):
            if not file['mutants']:
                file['mutants'].append({'id':str(90000000+index),'status':'Killed','mutatorName':'synthetic filler','location':{},'replacement':''})
        target=next((mutant for mutant in files[row['path']]['mutants'] if mutant['id']==row['id']),None)
        if target is None:
            target={'id':row['id'],'mutatorName':row['mutator'],'location':check.location(row['span']),'replacement':row['replacement']}
            files[row['path']]['mutants'].append(target)
        target['status']=status
        return {'files':files}

    def selected_mutant(self,report,row):
        return next(mutant for mutant in report['files'][row['path']]['mutants'] if mutant['id']==row['id'])

    def result(self,row,status,segment=None):
        return check.inspect_report(self.report(row,status,segment),segment or next(iter(row['allowedRemaining'])),self.rows,self.sources)

    def testEveryOriginalIdentityAndObservationIsFiniteAndNoTechnicalGatesChanged(self):
        self.assertEqual(len(self.historical_rows),829)
        self.assertEqual(sum(len(row['observed']) for row in self.historical_rows.values()),1498)
        if 'currentIdentityLedger' not in self.manifest:
            self.assertEqual(len(self.manifest['problemMutantTriage']),14)
        current=self.manifest
        self.assertEqual({segment['name']:segment['threshold'] for segment in current['segments']},{'happy-path':80,'error-handling':60})
        self.assertTrue(all(row['action']!='blocking' for row in self.rows.values()))
        self.assertEqual(sum(row['action']=='kill-test-added' for row in self.historical_rows.values()),95)
        self.assertEqual(sum(row['action']=='deferred-with-owner' for row in self.historical_rows.values()),405)
        self.assertLess((OWNER/check.HISTORICAL_LEDGER).stat().st_size,1048576)

    def testEachFiniteAllowedStatusIncludingSupplementalIsAdmittedByIdentityOnly(self):
        cases=0
        for row in self.rows.values():
            for segment,statuses in row['allowedRemaining'].items():
                for status in statuses:
                    with self.subTest(id=row['id'],segment=segment,status=status):
                        result=self.result(row,status,segment)
                        self.assertTrue(result['acceptedByThisFiniteCheck'],result['failures'])
                        self.assertTrue(result['nativeScoresBothCohortsPrivateProvenanceStillRequired'])
                        cases+=1
        self.assertGreater(cases,0)
        if os.environ.get('TRIAGE_CHECK_EVIDENCE'):
            (Path(os.environ['TRIAGE_CHECK_EVIDENCE'])/'allowed-status-case-count.json').write_text(json.dumps({'finiteStatusCases':cases,'scope':'Identity/status admission only; not complete/native reports'},indent=2)+'\n')

    def testEveryTestAddedIdentityRefusesAnyFreshRemainingProblem(self):
        for row in self.rows.values():
            if row['action']=='kill-test-added':
                for segment in row['allowedRemaining']:
                    for status in ['Survived','NoCoverage','Timeout','CompileError']:
                        with self.subTest(id=row['id'],segment=segment,status=status):
                            self.assertFalse(self.result(row,status,segment)['acceptedByThisFiniteCheck'])

    def testUnknownIdentitySpanReplacementMutatorAndSourceChangesRefuse(self):
        row=next(row for row in self.rows.values() if row['action']=='deferred-with-owner' and 'Survived' in next(iter(row['allowedRemaining'].values())))
        segment=next(segment for segment,statuses in row['allowedRemaining'].items() if 'Survived' in statuses)
        for change in ['id','location','replacement','mutatorName','source','status']:
            report=self.report(row,'Survived');file=report['files'][row['path']];mutant=self.selected_mutant(report,row)
            if change=='id':mutant['id']='99999999'
            elif change=='location':mutant['location']['start']['column']+=1
            elif change=='replacement':mutant['replacement']+=' changed'
            elif change=='mutatorName':mutant['mutatorName']='other mutator'
            elif change=='source':file['source']+='\n// changed'
            else:mutant['status']='RuntimeError'
            with self.subTest(change=change):self.assertFalse(check.inspect_report(report,segment,self.rows,self.sources)['acceptedByThisFiniteCheck'])

    def testUnexpectedExecutionStatusAndCoverageRegressionRefuse(self):
        for row in self.rows.values():
            for segment,statuses in row['allowedRemaining'].items():
                if statuses==['Survived']:
                    self.assertFalse(self.result(row,'NoCoverage',segment)['acceptedByThisFiniteCheck'])
                    self.assertFalse(self.result(row,'Timeout',segment)['acceptedByThisFiniteCheck'])
                    return
        self.fail('Expected a finite reviewed survivor')

    def testHost12812KeepsOriginalUncertaintyAndOnlyItsExactSupplementalAdmission(self):
        uncertainty=self.history['hosted12812Uncertainty']
        self.assertEqual(uncertainty['localCompleteStatus'],'Killed')
        self.assertIsNone(uncertainty['hostedFinalJsonStatus'])
        self.assertFalse(uncertainty['admittedAsProblem'])
        historical=self.historical_rows['12812']
        self.assertEqual(historical['observed'],{})
        self.assertFalse(historical['supplementalReview']['nativeOutcomeClaimed'])
        row=self.current_row_for_historical_id('12812')
        if row is None:
            self.assertIn('12812', self.ledger['retirements'])
            return
        for segment in ['happy-path','error-handling']:
            for status in ['Survived','NoCoverage']:
                self.assertEqual(self.result(row,status,segment)['acceptedByThisFiniteCheck'],
                                 status in row['allowedRemaining'][segment])
            self.assertFalse(self.result(row,'Timeout',segment)['acceptedByThisFiniteCheck'])
        unknown=copy.deepcopy(row);unknown['id']='12812000'
        self.assertFalse(self.result(unknown,'Survived','happy-path')['acceptedByThisFiniteCheck'])

    def testDestructiveTimeoutIsRetainedAndNotBlanketStatusAdmission(self):
        self.assertEqual(self.historical_rows['14021']['kind'],'non-actionable-detected-destructive-nonprogress')
        row=self.current_row_for_historical_id('14021')
        if row is not None:
            for segment,statuses in row['allowedRemaining'].items():
                self.assertEqual(self.result(row,'Timeout',segment)['acceptedByThisFiniteCheck'], 'Timeout' in statuses)
                self.assertFalse(self.result(row,'NoCoverage',segment)['acceptedByThisFiniteCheck'])
                self.assertFalse(self.result(row,'Survived',segment)['acceptedByThisFiniteCheck'])
        self.assertEqual(self.historical_rows['13541']['action'],'deferred-with-owner')
        self.assertIsNotNone(self.historical_rows['13541']['followup'])

    def testCurrentSourceAndLedgerDigestDriftActuallyRefuse(self):
        with tempfile.TemporaryDirectory() as temporary:
            root=Path(temporary)
            self.copy_inputs(root)
            changed=root/next(iter(self.ledger['sourceFiles']));original=changed.read_bytes();changed.write_bytes(original+b' ')
            with self.assertRaisesRegex(check.AdmissionError,'Changed current production source'):check.load_metadata(root)
            changed.write_bytes(original)
            ledger=root/check.HISTORICAL_LEDGER;ledger.write_bytes(ledger.read_bytes()+b' ')
            with self.assertRaisesRegex(check.AdmissionError,'Stale/changed ledger-manifest binding|Changed historical ledger'):
                check.load_metadata(root)

    def testManifestSelectsSuccessorAndRejectsAChangedLedgerDigest(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            self.copy_inputs(root)
            _, manifest, rows, _ = check.load_metadata(root)
            self.assertEqual(manifest['currentIdentityLedger']['path'], check.CURRENT_LEDGER)
            self.assertEqual(set(rows), set(self.rows))
            current = root / check.CURRENT_LEDGER
            current.write_bytes(current.read_bytes() + b' ')
            with self.assertRaisesRegex(check.AdmissionError, 'Stale successor ledger-manifest binding'):
                check.load_metadata(root)

    def testNativeInvalidCompilerShapeIsFinite(self):
        row=next(row for row in self.rows.values() if row['action']=='deferred-with-owner')
        report=self.report(row,'CompileError');mutant=self.selected_mutant(report,row)
        mutant.update({'statusReason':'Mutant caused compile errors','coveredBy':[],'killedBy':[]})
        segment=next(iter(row['allowedRemaining']))
        self.assertFalse(check.inspect_report(report,segment,self.rows,self.sources)['acceptedByThisFiniteCheck'])
        mutant['coveredBy']=['executed-case']
        self.assertFalse(check.inspect_report(report,segment,self.rows,self.sources)['acceptedByThisFiniteCheck'])



    def cli_fixture(self):
        temporary=tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        owner=Path(temporary.name)
        self.copy_inputs(owner)
        row=next(row for row in self.rows.values() if row['action']=='equivalent-accepted'
                 and set(row['allowedRemaining'])=={'happy-path','error-handling'}
                 and all(row['allowedRemaining'].values()))
        for segment in ['happy-path','error-handling']:
            path=owner/f'artifacts/mutation/{segment}/reports/source-tools-{segment}.json'
            path.parent.mkdir(parents=True)
            path.write_text(json.dumps(self.report(row,row['allowedRemaining'][segment][0],segment)))
        return owner,row

    def run_cli(self,owner,label):
        process=subprocess.run([sys.executable,'-B',str(owner/'eng/check-mutation-triage.py')],cwd=owner,capture_output=True,text=True,timeout=30)
        audit=owner/'artifacts/mutation/mutation-triage-admission.json'
        self.assertTrue(audit.is_file(),process.stderr)
        result=json.loads(audit.read_text())
        if os.environ.get('TRIAGE_CHECK_EVIDENCE'):
            directory=Path(os.environ['TRIAGE_CHECK_EVIDENCE'])/label;directory.mkdir(parents=True,exist_ok=True)
            (directory/'console.log').write_text(process.stdout+process.stderr)
            (directory/'admission.json').write_bytes(audit.read_bytes())
            (directory/'process.json').write_text(json.dumps({'exitCode':process.returncode,'scope':'Actual CLI finite fixture only; no native qualification'},indent=2)+'\n')
        return process,result

    def testActualBothSegmentCommandAcceptsOnlyFiniteIdentityStatus(self):
        owner,row=self.cli_fixture()
        process,result=self.run_cli(owner,'cli-positive')
        self.assertEqual(process.returncode,0,process.stderr)
        self.assertTrue(result['acceptedByThisFiniteCheck'])
        self.assertEqual(len(result['segments']),2)
        self.assertTrue(result['nativeScoresBothCohortsPrivateProvenanceStillRequired'])

    def testActualBothSegmentCommandRefusesAnIgnoredIdentityOutsideItsRequiredSegment(self):
        owner, _ = self.cli_fixture()
        segment = 'happy-path'
        row = next(row for row in self.rows.values() if segment not in row['allowedRemaining'])
        path = owner / f'artifacts/mutation/{segment}/reports/source-tools-{segment}.json'
        path.write_text(json.dumps(self.report(row, 'Ignored', segment)))
        process, result = self.run_cli(owner, 'cli-out-of-segment-ignored')
        self.assertEqual(process.returncode, 1, process.stderr)
        happy = next(item for item in result['segments'] if item['segment'] == segment)
        self.assertEqual(happy['expectedReviewedIdentities'], happy['seenReviewedIdentities'])
        self.assertEqual(happy['failures'], [{
            'id': row['id'], 'reason': 'Reviewed live identity cannot become an unapproved exclusion/Ignored status',
        }])

    def testActualBothSegmentCommandRefusesMissingAndAmbiguousReports(self):
        for shape in ['missing','ambiguous']:
            owner,row=self.cli_fixture()
            path=owner/'artifacts/mutation/happy-path/reports/source-tools-happy-path.json'
            if shape=='missing':path.unlink()
            else:
                extra=path.parent.parent/'other/reports'/path.name;extra.parent.mkdir(parents=True);shutil.copyfile(path,extra)
            process,result=self.run_cli(owner,'cli-'+shape)
            self.assertNotEqual(process.returncode,0)
            self.assertFalse(result['acceptedByThisFiniteCheck'])
            self.assertEqual(len(result['segments']),2)
            self.assertIn('Missing or ambiguous',result['segments'][0]['error'])
            self.assertTrue(result['segments'][1]['acceptedByThisFiniteCheck'])

    def testActualBothSegmentCommandRefusesUnknownIdentityAndChangedSpan(self):
        for changed in ['id','location','replacement','status']:
            owner,row=self.cli_fixture()
            path=owner/'artifacts/mutation/error-handling/reports/source-tools-error-handling.json'
            report=json.loads(path.read_text());mutant=self.selected_mutant(report,row)
            if changed=='id':mutant['id']='99999999'
            elif changed=='location':mutant['location']['start']['column']+=1
            elif changed=='replacement':mutant['replacement']+=' changed'
            else:mutant['status']='RuntimeError'
            path.write_text(json.dumps(report))
            process,result=self.run_cli(owner,'cli-refuse-'+changed)
            self.assertNotEqual(process.returncode,0)
            self.assertFalse(result['acceptedByThisFiniteCheck'])
            self.assertTrue(result['segments'][0]['acceptedByThisFiniteCheck'])
            self.assertFalse(result['segments'][1]['acceptedByThisFiniteCheck'])

    def testReviewedLiveIdentityCannotBeIgnoredOrHiddenByCompilerCandidate(self):
        row=next(row for row in self.rows.values() if row['action']=='kill-test-added' and 'happy-path' in row['allowedRemaining'])
        for status in ['Ignored','CompileError']:
            report=self.report(row,status,'happy-path')
            if status=='CompileError':self.selected_mutant(report,row).update({'statusReason':'Mutant caused compile errors','coveredBy':[],'killedBy':[]})
            result=check.inspect_report(report,'happy-path',self.rows,self.sources)
            self.assertFalse(result['acceptedByThisFiniteCheck'])
            self.assertTrue(any(failure['id']==row['id'] for failure in result['failures']))

    def testTruncatedOtherwiseCompleteReportCannotHideReviewedIdentity(self):
        row=next(row for row in self.rows.values() if row['action']=='kill-test-added' and 'happy-path' in row['allowedRemaining'])
        report=self.report(row,'Killed','happy-path')
        before_files=set(report['files'])
        report['files'][row['path']]['mutants']=[mutant for mutant in report['files'][row['path']]['mutants'] if mutant['id']!=row['id']]
        self.assertEqual(set(report['files']),before_files)
        self.assertGreater(sum(len(file['mutants']) for file in report['files'].values()),self.manifest['segments'][0]['minimumMutantCount'])
        self.assertTrue(all(file['mutants'] for file in report['files'].values()))
        result=check.inspect_report(report,'happy-path',self.rows,self.sources)
        self.assertFalse(result['acceptedByThisFiniteCheck'])
        self.assertIn({'id':row['id'],'reason':'Expected reviewed identity missing from report'},result['failures'])

    def testEveryClaimedKillHasAnExactResolvableExecutedEvidenceCatalog(self):
        catalog=json.loads((OWNER/self.history['evidenceCatalog']['path']).read_text())
        self.assertEqual(len(catalog['cases']),95)
        self.assertIn('pending',catalog['rootEvidencePublication']['state'])
        for ident,case in catalog['cases'].items():
            self.assertEqual(case['identitySha256'],self.historical_rows[ident]['identitySha256'])
            self.assertTrue(case['executed']['commands'])
            self.assertTrue(case['executed']['testSources'])
            self.assertTrue(case['executed']['resultLocator'])
            self.assertEqual(len(case['executed']['resultSha256']),64)
            self.assertTrue(all(item['method'] and item['testCase'] for item in case['currentPreservation']))

    def testQualityRequiresBothExistingSuitesAndStopsOnFailure(self):
        workflow=(OWNER/'.github/workflows/quality.yml').read_text()
        block=workflow.split("- name: 'Gate 2b: Dependency graph semantic policy tests'",1)[1].split("- name:",1)[0]
        self.assertIn('set -euo pipefail',block)
        for suite in ['test_mutation_triage.py','test_mutation_artifact_publication.py']:
            self.assertIn('python3 -m unittest tests/eng/'+suite,block)

    def testDocumentedReplayPropagatesNativePreparationValidationAndMatcherFailures(self):
        text=(OWNER/'docs/how-to/mutation-and-property-quality-gates.md').read_text()
        script=re.search(r"run-with-temp bash -c '\n(.*?)\n'",text,re.S).group(1)
        self.assertIn('prepare-mutation-artifacts.ps1 -Segment happy-path',script)
        self.assertIn('prepare-mutation-artifacts.ps1 -VerifyPublishedInputs',script)
        self.assertIn('python3 eng/check-mutation-triage.py',script)
        stub="""python3() {
          case "$*" in
            'eng/stryker_compat.py prepare') return 0 ;;
            'eng/stryker_compat.py run happy-path') label=happy ;;
            'eng/stryker_compat.py run error-handling') label=error ;;
            'eng/check-mutation-triage.py') label=matcher ;;
            *) return 99 ;;
          esac
          [ "$FAIL_STAGE" != "$label" ] || return 7
        }
        pwsh() {
          case "$*" in
            *'-Segment happy-path') label=publish-happy ;;
            *'-Segment error-handling') label=publish-error ;;
            *'-VerifyPublishedInputs') label=lineage ;;
            *'-RequireCompatibilityProvenance') label=validation ;;
            *) return 99 ;;
          esac
          [ "$FAIL_STAGE" != "$label" ] || return 7
        }
        """
        for stage in ['none','happy','error','publish-happy','publish-error','lineage','validation','matcher']:
            process=subprocess.run(['bash','-c',stub+script],env={**os.environ,'FAIL_STAGE':stage},capture_output=True,text=True)
            self.assertEqual(process.returncode,0 if stage=='none' else 7,stage+process.stderr)

    def testAggregateRequiresMatcherAfterStrictBothAndGuardScansItsAudit(self):
        workflow=(OWNER/'.github/workflows/mutation-property-nightly.yml').read_text().split('  source-tools-mutation:\n',1)[1].split('  lifecycle-properties:\n',1)[0]
        self.assertLess(workflow.index('Validate mutation reports'),workflow.index('Validate finite mutation identities and statuses'))
        self.assertLess(workflow.index('Validate finite mutation identities and statuses'),workflow.index('Require both complete segment jobs to succeed'))
        self.assertIn('run: ./eng/validate-stryker-reports.ps1 -RequireCompatibilityProvenance',workflow)
        self.assertIn('Validate finite mutation identities and statuses\n        if: always()\n        run: python3 eng/check-mutation-triage.py',workflow)
        self.assertIn('run: test "$SEGMENT_RESULT" = success',workflow)
        self.assertIn('artifacts/mutation/mutation-triage-admission.json',workflow)
        self.assertIn('"mutation-triage-admission.json"',(OWNER/'eng/prepare-mutation-artifacts.ps1').read_text())
        self.assertIn("if: always() && steps.publication.outcome == 'success'",workflow)
        self.assertNotIn('continue-on-error',workflow)

class MutationInventoryTests(unittest.TestCase):
    """Exact enum inventory accounting without excluding executable mutation targets."""

    ENUM_ROOT='src/Hexalith.FrontComposer.SourceTools/Transforms/'
    ENUM_NAMES=('FormFieldTypeCategory','ProjectionRenderStrategy','TypeCategory')

    @staticmethod
    def implicit_enum(source):
        source=re.sub(r'//[^\r\n]*|/\*.*?\*/','',source,flags=re.S)
        return re.fullmatch(r'\s*namespace\s+[\w.]+;\s*public\s+enum\s+\w+\s*\{\s*(?:\w+\s*,\s*)+\}\s*',source) is not None

    def testOnlyTheThreeImplicitEnumFilesReceiveFiniteInventoryAccounting(self):
        manifest=json.loads((OWNER/(check.REL+'mutation-target-manifest.json')).read_text())
        expected={self.ENUM_ROOT+name+'.cs' for name in self.ENUM_NAMES}
        actual={entry['path'] for entry in manifest['explicitExclusions'] if entry['path'].startswith(self.ENUM_ROOT)}
        self.assertEqual(actual,expected)
        for entry in manifest['explicitExclusions']:
            if entry['path'] in expected:
                self.assertEqual(entry['owner'],'Story 10-4 SourceTools quality/release-automation')
                self.assertIn('implicit ordinals only',entry['reason'])
                self.assertTrue(self.implicit_enum((OWNER/entry['path']).read_text()),entry['path'])
                canonical=(OWNER/entry['path']).read_bytes().decode('utf-8-sig').replace('\r\n','\n').replace('\r','\n')
                self.assertEqual(entry['canonicalSourceSha256'],hashlib.sha256(canonical.encode()).hexdigest())
        self.assertFalse(self.implicit_enum('namespace Fixture; public enum E { One = 1, Two, }'))
        self.assertFalse(self.implicit_enum('namespace Fixture; public enum E { One, } class C { int M() => 1; }'))

    def fixture(self,root,remove_enums=False,extra_executable=False):
        for relative in ('eng/validate-stryker-reports.ps1','eng/mutation-artifact-policy.ps1','.config/dotnet-tools.json'):
            target=root/relative;target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(OWNER/relative,target)
        manifest=json.loads((OWNER/(check.REL+'mutation-target-manifest.json')).read_text())
        if remove_enums:
            manifest['explicitExclusions']=[entry for entry in manifest['explicitExclusions'] if entry['path'] not in {self.ENUM_ROOT+name+'.cs' for name in self.ENUM_NAMES}]
        for name in self.ENUM_NAMES:
            relative=self.ENUM_ROOT+name+'.cs';target=root/relative;target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(OWNER/relative,target)
        files={}
        for index,target_root in enumerate(manifest['approvedTargetRoots']):
            relative=target_root+'/LiveFixture.cs';target=root/relative;target.parent.mkdir(parents=True,exist_ok=True)
            source='namespace Fixture; class Live'+str(index)+' { int Value() => 1; }\n'
            target.write_text(source)
            files[relative]={'language':'cs','source':source,'mutants':[{'id':str(index),'status':'Killed','location':{}}]}
        for name in self.ENUM_NAMES:
            relative=self.ENUM_ROOT+name+'.cs'
            files[relative]={'language':'cs','source':(root/relative).read_text(),'mutants':[]}
        if extra_executable:
            (root/self.ENUM_ROOT/'FutureExecutable.cs').write_text('namespace Fixture; class FutureExecutable { int Value() => 2; }\n')
        reports=root/'reports';reports.mkdir()
        for segment in manifest['segments']:
            target=root/segment['config'];target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(OWNER/segment['config'],target)
            (reports/(segment['artifactPrefix']+'.json')).write_text(json.dumps({'files':files}))
        (root/'manifest.json').write_text(json.dumps(manifest))

    def validate(self,root):
        result=subprocess.run(['pwsh','-NoProfile','-File',str(root/'eng/validate-stryker-reports.ps1'),
            '-ManifestPath','manifest.json','-ReportRoot','reports','-OutputPath','reports/job-summary.md'],cwd=root,capture_output=True,text=True)
        refusal=root/'reports/mutation-validation-errors.txt'
        return result,refusal.read_text() if refusal.exists() else ''

    def testNativeValidatorRetainsTheOriginalThreeTargetDriftRefusals(self):
        with tempfile.TemporaryDirectory() as temporary:
            root=Path(temporary);self.fixture(root,remove_enums=True)
            result,refusal=self.validate(root)
            self.assertNotEqual(result.returncode,0,result.stdout+result.stderr)
            self.assertEqual(refusal.count('Target drift:'),3,refusal)
            for name in self.ENUM_NAMES:self.assertIn(self.ENUM_ROOT+name+'.cs',refusal)

    def testNativeValidatorAcceptsTheseExactNonMutatableInventoryOmissions(self):
        with tempfile.TemporaryDirectory() as temporary:
            root=Path(temporary);self.fixture(root)
            result,refusal=self.validate(root)
            self.assertEqual(result.returncode,0,result.stdout+result.stderr+refusal)

    def testNativeValidatorStillRefusesAnUnreportedExecutableTarget(self):
        with tempfile.TemporaryDirectory() as temporary:
            root=Path(temporary);self.fixture(root,extra_executable=True)
            result,refusal=self.validate(root)
            self.assertNotEqual(result.returncode,0,result.stdout+result.stderr)
            self.assertEqual(refusal.count('Target drift:'),1,refusal)
            self.assertIn(self.ENUM_ROOT+'FutureExecutable.cs',refusal)


    def testNativeValidatorRefusesSamePathEnumSourceChangesWithUnchangedBindings(self):
        for name in self.ENUM_NAMES:
            with self.subTest(enum=name), tempfile.TemporaryDirectory() as temporary:
                root=Path(temporary);self.fixture(root)
                manifest=(root/'manifest.json').read_bytes()
                source=root/self.ENUM_ROOT/(name+'.cs')
                changed=('namespace Fixture; public enum '+name+' { One = 1, Two, }\n').encode()
                source.write_bytes(changed)
                result,refusal=self.validate(root)
                self.assertNotEqual(result.returncode,0,result.stdout+result.stderr)
                self.assertIn("Canonical source hash mismatch for finite enum exclusion '"+self.ENUM_ROOT+name+'.cs',refusal)
                self.assertEqual((root/'manifest.json').read_bytes(),manifest)
                self.assertEqual(source.read_bytes(),changed)

    def testNativeValidatorCanonicalizesBomAndLineEndingsWithoutRewritingSources(self):
        for bom,eol in ((b'',b'\n'),(b'',b'\r\n'),(b'\xef\xbb\xbf',b'\n'),(b'\xef\xbb\xbf',b'\r\n')):
            with self.subTest(bom=bool(bom),eol=eol), tempfile.TemporaryDirectory() as temporary:
                root=Path(temporary);self.fixture(root)
                originals={}
                for name in self.ENUM_NAMES:
                    source=root/self.ENUM_ROOT/(name+'.cs')
                    canonical=source.read_bytes().decode('utf-8-sig').replace('\r\n','\n').replace('\r','\n').encode()
                    originals[source]=bom+canonical.replace(b'\n',eol)
                    source.write_bytes(originals[source])
                result,refusal=self.validate(root)
                self.assertEqual(result.returncode,0,result.stdout+result.stderr+refusal)
                for source,original in originals.items():self.assertEqual(source.read_bytes(),original)

    def testNativeValidatorRequiresEachFiniteExceptionToHaveAValidBinding(self):
        for value in (None,'invalid','0'*64):
            with self.subTest(hash=value), tempfile.TemporaryDirectory() as temporary:
                root=Path(temporary);self.fixture(root)
                manifest=json.loads((root/'manifest.json').read_text())
                row=next(row for row in manifest['explicitExclusions'] if row['path']==self.ENUM_ROOT+self.ENUM_NAMES[0]+'.cs')
                if value is None:row.pop('canonicalSourceSha256')
                else:row['canonicalSourceSha256']=value
                (root/'manifest.json').write_text(json.dumps(manifest))
                result,refusal=self.validate(root)
                self.assertNotEqual(result.returncode,0,result.stdout+result.stderr)
                self.assertIn('finite enum exclusion' if value=='0'*64 else 'requires its exact path and canonical source SHA256',refusal)

    def testActiveEnumResultsRemainInLiveAndAuditCountsAndUntriagedProblemsRefuse(self):
        relative=self.ENUM_ROOT+self.ENUM_NAMES[0]+'.cs'
        for status in ('Killed','Survived','NoCoverage'):
            with self.subTest(status=status), tempfile.TemporaryDirectory() as temporary:
                root=Path(temporary);self.fixture(root)
                manifest=json.loads((root/'manifest.json').read_text())
                # This synthetic entry-point case intentionally has no problem triage.
                # Its three reported results are data, not a native mutation score.
                manifest['problemMutantTriage']=[]
                (root/'manifest.json').write_text(json.dumps(manifest))
                for report in (root/'reports').glob('source-tools-*.json'):
                    document=json.loads(report.read_text())
                    document['files'][relative]['mutants']=[{'id':'90000001','status':status,'location':{}}]
                    report.write_text(json.dumps(document))
                result,refusal=self.validate(root)
                if status=='Killed':self.assertEqual(result.returncode,0,result.stdout+result.stderr+refusal)
                else:
                    self.assertNotEqual(result.returncode,0,result.stdout+result.stderr)
                    self.assertIn('untriaged '+status,refusal)
                audit=json.loads((root/'reports/mutation-report-audit.json').read_text())
                self.assertEqual(len(audit['Reports']),2)
                for entry in audit['Reports']:
                    self.assertEqual(entry['LiveMutantCount'],3)
                    self.assertEqual(len(entry['RawMutantStatuses']),3)
                    self.assertEqual([row for row in entry['RawMutantStatuses'] if row['Id']=='90000001'],
                                     [{'Path':relative,'Id':'90000001','Status':status}])
                    counts={row['Status']:row['Count'] for row in entry['StatusCounts']}
                    self.assertEqual(counts,{'Killed':3} if status=='Killed' else {'Killed':2,status:1})


    def testNativeValidatorRequiresCanonicalCaseForFiniteEnumPaths(self):
        with tempfile.TemporaryDirectory() as temporary:
            root=Path(temporary);self.fixture(root)
            source=root/self.ENUM_ROOT/(self.ENUM_NAMES[0]+'.cs')
            source.rename(source.with_name(source.name.lower()))
            result,refusal=self.validate(root)
            self.assertNotEqual(result.returncode,0,result.stdout+result.stderr)
            self.assertIn('requires its exact path and canonical source SHA256',refusal)


class SuccessorLedgerTests(unittest.TestCase):
    SOURCE = 'src/Hexalith.FrontComposer.SourceTools/Parsing/Example.cs'
    CONTROL_SOURCE = 'tests/ExampleTests.cs'
    SOURCE_TEXT = 'newValue newValue\r\n'
    CURRENT_SHA = 'a' * 64
    CATALOG_SHA = '9' * 64
    HISTORICAL_ID = '12812'
    CURRENT_ID = '13362'
    SEGMENT = 'error-handling'

    def setUp(self):
        directory = tempfile.TemporaryDirectory()
        self.addCleanup(directory.cleanup)
        self.owner = Path(directory.name)
        source = self.owner / self.SOURCE
        source.parent.mkdir(parents=True)
        source.write_bytes(self.SOURCE_TEXT.encode())
        control = self.owner / self.CONTROL_SOURCE
        control.parent.mkdir()
        control.write_bytes(b'current assertions')
        self.row = {
            'id': self.CURRENT_ID, 'path': self.SOURCE, 'span': [1, 1, 1, 9],
            'original': 'newValue', 'replacement': 'wrongValue', 'mutator': 'Example mutation',
            'historicalIds': [self.HISTORICAL_ID], 'action': 'deferred-with-owner',
            'allowedRemaining': {self.SEGMENT: ['NoCoverage']}, 'rationale': 'Reviewed current scope.',
            'followup': {'owner': check.ROLE, 'action': 'Add coverage for the current expression.'},
        }
        self.historical = {
            ident: {**copy.deepcopy(self.row), 'id': ident, 'original': literal,
                    'observed': {self.SEGMENT: 'NoCoverage'}}
            for ident, literal in [(self.HISTORICAL_ID, 'newValue'), ('77', 'retiredValue')]
        }
        self.historical_controls = {}
        self.ledger = {
            'schemaVersion': check.CURRENT_SCHEMA, 'owner': check.ROLE, 'sourceCommit': 'b' * 40,
            'historicalLedgerSha256': '8' * 64, 'historicalLedgerMetadataCommit': '7' * 40,
            'historicalControlCatalogSha256': self.CATALOG_SHA,
            'reviewedReports': {segment: {
                **{field: 'c' * 64 for field in check.REPORT_DIGEST_FIELDS},
                'locator': f'archive/{segment}/reports/source-tools-{segment}.json',
            }
                                for segment in check.SEGMENTS},
            'sourceFiles': {self.SOURCE: {'sha256': check.digest(self.SOURCE_TEXT.encode()),
                                          'canonicalLfSha256': check.digest(b'newValue newValue\n')}},
            'mutations': [self.row],
            'spanCorrections': {},
            'reviewedCrosswalk': {'locator': 'archive/crosswalk.json', 'sha256': '6' * 64},
        }
        for row in self.historical.values():
            self.seal_row(row)
        retired = self.historical['77']
        self.ledger['retirements'] = {'77': {
            'reason': 'Removed expression.', 'evidence': 'archive/crosswalk.json#/retirementEvidence/77',
            'crosswalkSha256': '6' * 64, 'historicalIdentitySha256': retired['identitySha256'],
            'currentSourceFileSha256': self.ledger['sourceFiles'][self.SOURCE]['sha256'],
            'bothCurrentReportSha256': {segment: report['sha256'] for segment, report in self.ledger['reviewedReports'].items()},
            'otherCurrentIdentityTried': check.expression(retired),
            'historicalObservation': {
                'historicalId': '77', 'path': self.SOURCE, 'segment': self.SEGMENT, 'status': 'NoCoverage',
                'ledgerSpan': retired['span'], 'ledgerOriginal': retired['original'],
                'ledgerReplacement': retired['replacement'], 'ledgerMutator': retired['mutator'],
            },
        }}
        self.seal_row()
        self.manifest = {
            'approvedTargetRoots': [str(Path(self.SOURCE).parent)],
            'problemMutantTriage': [{
                'segment': self.SEGMENT, 'status': 'NoCoverage', 'action': self.row['action'],
                'identityLedger': check.CURRENT_LEDGER, 'identityLedgerSha256': self.CURRENT_SHA,
                'mutationIds': [self.CURRENT_ID], 'count': 1, 'owner': check.ROLE,
                'rationale': 'One reviewed current mutation.',
            }],
        }

    def seal_row(self, row=None):
        row = self.row if row is None else row
        row['identitySha256'] = check.mutation_identity_digest(row, self.ledger['sourceFiles'])

    def load(self):
        return check.load_current_metadata(self.owner, self.ledger, self.historical, self.historical_controls,
                                           self.manifest, self.CURRENT_SHA, self.CATALOG_SHA)

    def control(self):
        return {
            'resultSha256': 'd' * 64, 'resultLocator': 'controls/result.trx',
            'reviewReceiptSha256': 'e' * 64, 'reviewReceiptLocator': 'controls/verification.json',
            'identitySha256': self.row['identitySha256'],
            'testSources': {self.CONTROL_SOURCE: check.digest((self.owner / self.CONTROL_SOURCE).read_bytes())},
        }

    def write_retained(self, locator, data):
        path = self.owner / locator
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(data)
        return check.digest(data)

    def report(self, ident, status):
        return {'files': {self.SOURCE: {'source': self.SOURCE_TEXT, 'mutants': [{
            'id': ident, 'status': status, 'location': check.location(self.row['span']),
            'replacement': self.row['replacement'], 'mutatorName': self.row['mutator'],
        }]}}}

    def testCurrentNativeIdIsIndependentOfHistoricalId(self):
        _, _, rows, sources = self.load()
        self.assertNotIn(self.HISTORICAL_ID, rows)
        current = check.inspect_report(self.report(self.CURRENT_ID, 'NoCoverage'), self.SEGMENT, rows, sources)
        self.assertTrue(current['acceptedByThisFiniteCheck'], current['failures'])
        for ident, status in [(self.HISTORICAL_ID, 'NoCoverage'), (self.CURRENT_ID, 'Survived'), (self.CURRENT_ID, 'Ignored')]:
            with self.subTest(id=ident, status=status):
                refused = check.inspect_report(self.report(ident, status), self.SEGMENT, rows, sources)
                self.assertFalse(refused['acceptedByThisFiniteCheck'])

    def testEveryHistoricalIdentityMustBeResolvedExactlyOnce(self):
        retirement = self.ledger['retirements'].pop('77')
        with self.assertRaisesRegex(check.AdmissionError, 'does not account for every historical identity'):
            self.load()
        self.ledger['retirements']['77'] = retirement
        self.row['historicalIds'].append('77')
        with self.assertRaisesRegex(check.AdmissionError, 'multiply resolved historical identity'):
            self.load()

    def testDuplicateNativeIdAndUnreviewedHistoricalStatusRefuse(self):
        self.ledger['mutations'].append(copy.deepcopy(self.row))
        with self.assertRaisesRegex(check.AdmissionError, 'duplicate current mutation ID'):
            self.load()
        self.ledger['mutations'].pop()
        self.row['allowedRemaining'][self.SEGMENT].append('Timeout')
        with self.assertRaisesRegex(check.AdmissionError, 'changed a historical action, segment or status'):
            self.load()

    def testCarriedAllowanceRequiresTheSameExpression(self):
        old = self.historical[self.HISTORICAL_ID]
        for field in check.EXPRESSION_FIELDS:
            with self.subTest(field=field):
                original = old[field]
                old[field] += ' changed'
                with self.assertRaisesRegex(check.AdmissionError, 'allowance changed a historical expression'):
                    self.load()
                old[field] = original

    def testIdenticalTextAtAnotherSpanRequiresAnExactCrosswalkMapping(self):
        old = self.historical[self.HISTORICAL_ID]
        self.row['span'] = [1, 10, 1, 18]
        self.seal_row()
        with self.assertRaisesRegex(check.AdmissionError, 'span correction requires an exact reviewed mapping'):
            self.load()
        mapping = {
            'historicalIdentitySha256': old['identitySha256'], 'historicalSpan': old['span'],
            'identitySha256': self.row['identitySha256'], 'currentSpan': self.row['span'],
            'crosswalkSha256': self.ledger['reviewedCrosswalk']['sha256'],
        }
        self.ledger['spanCorrections'][self.HISTORICAL_ID] = mapping
        self.load()
        mapping['currentSpan'] = old['span']
        with self.assertRaisesRegex(check.AdmissionError, 'span correction requires an exact reviewed mapping'):
            self.load()

    def testChangedKillRequiresCurrentReplay(self):
        old = self.historical[self.HISTORICAL_ID]
        for row in (old, self.row):
            row['action'] = 'kill-test-added'
            row['allowedRemaining'] = {self.SEGMENT: []}
        old['replacement'] = 'previousReplacement'
        self.seal_row(old)
        self.manifest['problemMutantTriage'].clear()
        with self.assertRaisesRegex(check.AdmissionError, 'Current control requires a retained result'):
            self.load()
        self.row['control'] = self.control()
        self.load()

    def testMergedHistoricalControlsRequireBoundCurrentControlEvidence(self):
        for row in (*self.historical.values(), self.row):
            row['action'] = 'kill-test-added'
            row['allowedRemaining'] = {self.SEGMENT: []}
        self.row['historicalIds'].append('77')
        self.ledger['retirements'].clear()
        self.manifest['problemMutantTriage'].clear()
        with self.assertRaisesRegex(check.AdmissionError, 'Current control requires a retained result'):
            self.load()
        test_source = self.owner / self.CONTROL_SOURCE
        self.row['control'] = self.control()
        _, _, rows, _ = self.load()
        self.assertEqual(rows[self.CURRENT_ID]['historicalIds'], [self.HISTORICAL_ID, '77'])
        test_source.write_bytes(b'changed assertions')
        with self.assertRaisesRegex(check.AdmissionError, 'Current control test source fingerprint changed'):
            self.load()

    def testSourceAndAggregateRemainBoundToReviewedInputs(self):
        source = self.owner / self.SOURCE
        source.write_bytes(b'changedValue\r\n')
        with self.assertRaisesRegex(check.AdmissionError, 'Changed current production source'):
            self.load()
        source.write_bytes(self.SOURCE_TEXT.encode())
        aggregate = self.manifest['problemMutantTriage'][0]
        aggregate['mutationIds'] = [self.HISTORICAL_ID]
        with self.assertRaisesRegex(check.AdmissionError, 'Current aggregate maxima do not exactly cover'):
            self.load()
        aggregate['identityLedgerSha256'] = '0' * 64
        with self.assertRaisesRegex(check.AdmissionError, 'Current aggregate is not bound'):
            self.load()

    def testSuppliedReplayIsVerifiedEvenWhenTheLiteralIsUnchanged(self):
        self.row['control'] = self.control()
        self.row['control']['testSources'][self.CONTROL_SOURCE] = '0' * 64
        with self.assertRaisesRegex(check.AdmissionError, 'Current control test source fingerprint changed'):
            self.load()
        self.row['control']['identitySha256'] = '0' * 64
        with self.assertRaisesRegex(check.AdmissionError, 'Current control does not bind the reviewed mutation'):
            self.load()

    def testReplayRequiresTheReceiptAndChecksBothLocatorsAndRetainedBytes(self):
        control = self.control()
        self.row['control'] = control
        del control['reviewReceiptSha256']
        with self.assertRaisesRegex(check.AdmissionError, 'requires a replay receipt and digest'):
            self.load()
        control.update(self.control())
        control['reviewReceiptLocator'] = '../verification.json'
        with self.assertRaisesRegex(check.AdmissionError, 'locators do not identify the same replay'):
            self.load()
        control.update(self.control())
        control['resultSha256'] = self.write_retained(control['resultLocator'], b'focused result')
        replay = {
            'sourceCommit': self.ledger['sourceCommit'],
            'source': {'path': self.SOURCE, 'sha256': self.ledger['sourceFiles'][self.SOURCE]['sha256']},
            'testFiles': [{'path': self.CONTROL_SOURCE, 'sha256': control['testSources'][self.CONTROL_SOURCE]}],
            'cases': {'result': {
                'literal': {field: self.row[field] for field in ('historicalIds', 'span', 'original', 'replacement')},
                'test': {'retainedTrx': 'result.trx', 'retainedTrxSha256': control['resultSha256'],
                         'exitCode': 2, 'failedTestNames': ['Fixture.FocusedAssertion']},
            }},
        }
        receipt = control['reviewReceiptLocator']
        control['reviewReceiptSha256'] = self.write_retained(receipt, json.dumps(replay).encode())
        self.load()
        self.write_retained(receipt, b'changed receipt')
        with self.assertRaisesRegex(check.AdmissionError, 'Retained evidence digest differs: controls/verification.json'):
            self.load()
        replay['cases']['result']['literal']['replacement'] = 'unrelatedReplacement'
        control['reviewReceiptSha256'] = self.write_retained(receipt, json.dumps(replay).encode())
        with self.assertRaisesRegex(check.AdmissionError, 'receipt does not bind the mutation'):
            self.load()
        self.write_retained(control['resultLocator'], b'changed result')
        with self.assertRaisesRegex(check.AdmissionError, 'Retained evidence digest differs: controls/result.trx'):
            self.load()

    def testRelocatedKillStillRequiresThePreservedAssertionSource(self):
        for row in (self.historical[self.HISTORICAL_ID], self.row):
            row['action'] = 'kill-test-added'
            row['allowedRemaining'] = {self.SEGMENT: []}
        self.manifest['problemMutantTriage'].clear()
        source = self.owner / self.CONTROL_SOURCE
        self.historical_controls[self.HISTORICAL_ID] = {'currentPreservation': [
            {'ownerFile': self.CONTROL_SOURCE, 'currentFileSha256': check.digest(source.read_bytes())},
        ]}
        self.load()
        source.write_bytes(b'changed assertions')
        with self.assertRaisesRegex(check.AdmissionError, 'Current preserved assertion source fingerprint changed'):
            self.load()

    def testBothReviewedReportsAreRequired(self):
        del self.ledger['reviewedReports']['happy-path']
        with self.assertRaisesRegex(check.AdmissionError, 'requires both reviewed reports'):
            self.load()

    def testSourceAndHistoricalProvenanceMustBindTheReview(self):
        for field, pattern in [('sourceCommit', 'reviewed source commit'),
                               ('historicalLedgerMetadataCommit', 'historical metadata commit'),
                               ('historicalControlCatalogSha256', 'authenticated historical control catalog')]:
            with self.subTest(field=field):
                original = self.ledger[field]
                self.ledger[field] = '0' * len(original)
                with self.assertRaisesRegex(check.AdmissionError, pattern):
                    self.load()
                self.ledger[field] = original

    def testEveryReviewedReportDigestIsRequiredAndPresentArtifactsAreChecked(self):
        for report in self.ledger['reviewedReports'].values():
            for field in check.REPORT_DIGEST_FIELDS:
                with self.subTest(report=report['locator'], field=field):
                    original = report.pop(field)
                    with self.assertRaisesRegex(check.AdmissionError, 'all original, published and provenance digests'):
                        self.load()
                    report[field] = original
        report = self.ledger['reviewedReports'][self.SEGMENT]
        self.write_retained(report['locator'], b'changed report')
        with self.assertRaisesRegex(check.AdmissionError, 'Retained evidence digest differs: .*source-tools-error-handling.json'):
            self.load()

    def testRetirementBindsExactReviewAndAvailableCrosswalk(self):
        retirement = self.ledger['retirements']['77']
        for field in ('historicalIdentitySha256', 'currentSourceFileSha256', 'bothCurrentReportSha256', 'crosswalkSha256'):
            with self.subTest(field=field):
                original = retirement.pop(field)
                with self.assertRaisesRegex(check.AdmissionError, 'Historical retirement .* (differs|historical row)'):
                    self.load()
                retirement[field] = original
        retirement['historicalObservation']['ledgerMutator'] = 'Unrelated mutation'
        with self.assertRaisesRegex(check.AdmissionError, 'exact historical row'):
            self.load()
        retirement['historicalObservation']['ledgerMutator'] = self.historical['77']['mutator']
        self.write_retained(self.ledger['reviewedCrosswalk']['locator'], b'changed crosswalk')
        with self.assertRaisesRegex(check.AdmissionError, 'Retained evidence digest differs: archive/crosswalk.json'):
            self.load()

    def testMetadataRejectsNestedDuplicateObjectKeys(self):
        path = 'duplicate.json'
        self.write_retained(path, b'{"reports":{"happy-path":1,"happy-path":2}}')
        with self.assertRaisesRegex(check.AdmissionError, 'Duplicate metadata key: happy-path'):
            check.read_metadata(self.owner / path)

    def testLegacyDispatchChecksTheLegacyAggregateWithoutSuccessorMetadata(self):
        originals = [str(ident) for ident in range(1, 829)]
        supplement = '12812'
        mutations = []
        for ident in [*originals, supplement]:
            row = {**check.expression(self.row), 'id': ident, 'followup': 'fixture', 'action': 'deferred-with-owner',
                   'observed': {self.SEGMENT: 'NoCoverage'}, 'allowedRemaining': {self.SEGMENT: ['NoCoverage']}}
            if ident == supplement:
                row.update(observed={}, allowedRemaining={segment: ['Survived', 'NoCoverage'] for segment in check.SEGMENTS},
                           supplementalReview={'literalResult': 'survived-current94-case-control', 'nativeOutcomeClaimed': False})
            self.seal_row(row)
            mutations.append(row)
        catalog_locator = check.REL + 'fixture-catalog.json'
        catalog_sha = self.write_retained(catalog_locator, b'{"schemaVersion":"finite-regression-control-evidence.v1","cases":{}}')
        legacy = {
            'schemaVersion': 'finite-source-tools-technical-followups.v1', 'owner': check.ROLE,
            'originalIdentityCount': len(originals), 'supplementalIdentityCount': 1, 'mutations': mutations,
            'sourceFiles': self.ledger['sourceFiles'],
            'followups': {'fixture': {'owner': check.ROLE, 'ids': [*originals, supplement]}},
            'hosted12812Uncertainty': {'hostedFinalJsonStatus': None, 'admittedAsProblem': False},
            'evidenceCatalog': {'path': catalog_locator, 'sha256': catalog_sha},
        }
        legacy_sha = self.write_retained(check.HISTORICAL_LEDGER, json.dumps(legacy).encode())
        aggregates = []
        for segment in sorted(check.SEGMENTS):
            for status in ('NoCoverage', 'Survived'):
                observed = originals if (segment, status) == (self.SEGMENT, 'NoCoverage') else []
                aggregates.append({
                    'identityLedger': check.HISTORICAL_LEDGER, 'identityLedgerSha256': legacy_sha,
                    'segment': segment, 'status': status, 'action': 'deferred-with-owner',
                    'mutationIds': [*observed, supplement], 'count': len(observed) + 1,
                    'owner': check.ROLE, 'rationale': 'Fixture review.',
                    'originalObservedCount': len(observed), 'explicitTransitionMaximum': 0, 'supplementalReviewedMaximum': 1,
                })
        manifest_locator = check.REL + 'mutation-target-manifest.json'
        self.write_retained(manifest_locator, json.dumps({'problemMutantTriage': aggregates}).encode())
        self.write_retained(check.CURRENT_LEDGER, b'Invalid successor ledger should be skipped.')
        ledger, _, rows, _ = check.load_metadata(self.owner)
        self.assertEqual(ledger['schemaVersion'], legacy['schemaVersion'])
        self.assertEqual(set(rows), {*originals, supplement})
        aggregates[0]['count'] += 1
        self.write_retained(manifest_locator, json.dumps({'problemMutantTriage': aggregates}).encode())
        with self.assertRaisesRegex(check.AdmissionError, 'Aggregate count is not its finite identity maximum'):
            check.load_metadata(self.owner)


if __name__=='__main__':unittest.main(verbosity=2)
