"""Focused integrity and portable replay regressions for this evidence packet."""
import hashlib
import json
import os
import re
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

PACKET = Path(__file__).resolve().parent
REPO = next(path for path in PACKET.parents if (path / 'Hexalith.FrontComposer.slnx').exists())


class EvidenceToolsTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.input = self.root / 'input'
        self.input.mkdir()
        (self.input / 'receipt.txt').write_bytes(b'captured bytes\n')
        self.roots = self.root / 'roots.json'
        self.roots.write_text(json.dumps({'required': str(self.input)}))
        self.output = self.root / 'packet'

    def run_pack(self):
        return subprocess.run([sys.executable, str(PACKET / 'pack.py'), '--roots-json', str(self.roots), '--output-dir', str(self.output)], capture_output=True, text=True)

    def run_restore(self):
        return subprocess.run([sys.executable, str(PACKET / 'restore.py'), str(self.root / 'destination'), '--packet', str(self.output)], env={**os.environ, 'PYTHONOPTIMIZE': '1'}, capture_output=True, text=True)

    def test_missing_input_does_not_replace_existing_evidence(self):
        self.assertEqual(self.run_pack().returncode, 0)
        before = {p.name: p.read_bytes() for p in self.output.iterdir()}
        self.roots.write_text(json.dumps({'required': str(self.root / 'missing')}))
        failure = self.run_pack()
        self.assertNotEqual(failure.returncode, 0)
        self.assertIn('Required evidence root is missing', failure.stderr)
        self.assertEqual(before, {p.name: p.read_bytes() for p in self.output.iterdir()})

    def test_optimized_restore_preserves_bytes(self):
        self.assertEqual(self.run_pack().returncode, 0)
        self.assertEqual(self.run_restore().returncode, 0)
        self.assertEqual((self.root / 'destination/required/receipt.txt').read_bytes(), b'captured bytes\n')

    def test_optimized_restore_rejects_corrupted_part(self):
        self.assertEqual(self.run_pack().returncode, 0)
        manifest = json.loads((self.output / 'raw-artifacts.json').read_text())
        part = self.output / manifest['parts'][0]['file']
        part.write_bytes(b'x' + part.read_bytes()[1:])
        failure = self.run_restore()
        self.assertNotEqual(failure.returncode, 0)
        self.assertIn('size/hash mismatch', failure.stderr)
        self.assertFalse((self.root / 'destination').exists())

    def test_optimized_restore_rejects_entry_size_and_path_escape(self):
        self.assertEqual(self.run_pack().returncode, 0)
        path = self.output / 'raw-artifacts.json'
        original = json.loads(path.read_text())
        for mutation, expected in [('size', 'size/hash mismatch'), ('escape', 'escapes destination')]:
            manifest = json.loads(json.dumps(original))
            if mutation == 'size':
                manifest['entries'][0]['bytes'] += 1
            else:
                manifest['entries'][0]['tree'] = '../outside'
            path.write_text(json.dumps(manifest))
            failure = self.run_restore()
            self.assertNotEqual(failure.returncode, 0)
            self.assertIn(expected, failure.stderr)
        self.assertFalse((self.root / 'outside').exists())

    def test_portable_audit_projects_complete_report_and_returns_failure(self):
        restored = self.root / 'restored'
        results, html = restored / 'current-browser/results', restored / 'current-browser/report'
        results.mkdir(parents=True)
        (html / 'data').mkdir(parents=True)
        (html / 'index.html').write_text('<img src="data/attachment.png">')
        (html / 'data/attachment.png').write_bytes(b'retained raw asset')
        captured = json.loads((PACKET / 'raw-artifacts.json').read_text())['roots']['current-browser']
        report = {'config': {'metadata': {'a11yEvidenceRunId': 'portable-fixture'}}, 'stats': {'expected': 0, 'unexpected': 1, 'flaky': 0, 'skipped': 0}, 'errors': [], 'suites': [{'specs': [{'id': 'fixture', 'title': 'visual baseline existing failure', 'tests': [{'projectName': 'firefox', 'status': 'unexpected', 'expectedStatus': 'passed', 'results': [{'status': 'failed', 'retry': 0, 'duration': 1, 'attachments': [{'name': 'proof', 'contentType': 'application/json', 'path': captured + '/results/proof.json'}]}]}]}]}]}
        (results / 'playwright.json').write_text(json.dumps(report))
        (results / 'junit.xml').write_text('<testsuites/>')
        (results / 'proof.json').write_text('{}')
        before = hashlib.sha256((results / 'playwright.json').read_bytes()).hexdigest()
        receipt = self.root / 'audit'
        run = subprocess.run(['node', str(PACKET / 'audit.mjs'), '--repo-root', str(REPO), '--evidence-root', str(restored), '--output-root', str(receipt)], cwd=self.root, capture_output=True, text=True)
        self.assertEqual(run.returncode, 1, run.stderr)
        self.assertTrue((receipt / 'state-validation.json').exists(), run.stderr)
        gates = json.loads((receipt / 'gate-status.json').read_text())
        self.assertTrue(gates['failed'])
        self.assertEqual(gates['existingFailedGates'][0]['kind'], 'recorded visual comparison')
        replay = json.loads((receipt / 'raw-replay.json').read_text())
        self.assertEqual(replay['suites'][0]['specs'][0]['tests'][0]['results'][0]['attachments'][0]['path'], str(results / 'proof.json'))
        self.assertEqual(before, hashlib.sha256((results / 'playwright.json').read_bytes()).hexdigest())
        for engine in ['chromium', 'firefox', 'webkit']:
            index = restored / 'bounded-replay' / engine / 'report/index.html'
            projection = index.read_text()
            self.assertIn('Bounded evidence projection', projection)
            link = re.search(r'id="complete-raw-report" href="([^"]+)"', projection).group(1)
            self.assertEqual((index.parent / link).resolve(), (html / 'index.html').resolve())
            self.assertTrue((index.parent / link).is_file())
        self.assertTrue((html / 'data/attachment.png').is_file())


if __name__ == '__main__':
    unittest.main(verbosity=2)
