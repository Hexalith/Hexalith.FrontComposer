import copy
import importlib.util
import json
import os
from pathlib import Path
import sys
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[2]
SOURCE_REPOSITORY = ROOT
spec = importlib.util.spec_from_file_location('compat', ROOT / 'eng/stryker_compat.py')
compat = importlib.util.module_from_spec(spec)
spec.loader.exec_module(compat)
DEFINITION = json.loads((ROOT / compat.DEFINITION).read_text())
# Synthetic contract metadata only. Real compiled PE/activation controls are
# separate prerequisites; these fixtures never represent a compiled tool.
METADATA = [{"file": name, "assemblyName": name[:-4], "assemblyVersion": "5.1.0.0", "values": {
    "System.Reflection.AssemblyInformationalVersionAttribute": compat.INFORMATIONAL_VERSION,
    "System.Runtime.Versioning.TargetFrameworkAttribute": ".NETCoreApp,Version=v10.0"}}
    for name in compat.ASSEMBLIES]


class ContractTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.directory = Path(self.temp.name)
        self.runtime = self.directory / 'runtime'
        self.runtime.mkdir()
        for name in compat.ASSEMBLIES:
            (self.runtime / name).write_bytes(b'Synthetic contract fixture, not a compiled tool: ' + name.encode())
        self.inputs = {compat.DEFINITION: 'a' * 64}
        self.receipt = {'schema': 'hexalith.stryker-private-build.v1', 'definition_sha256': 'a' * 64,
                        'authenticated_inputs': self.inputs, 'upstream_commit': compat.UPSTREAM,
                        'custom_version': compat.CUSTOM_VERSION, 'informational_version': compat.INFORMATIONAL_VERSION,
                        'sdk': '11.0.100', 'runtimes': [], 'source_lockfiles': {}, 'resolved_package_assets': {},
                        'html_inputs': {}, 'assembly_metadata': METADATA,
                        'runtime_files': compat.runtime_files(self.runtime)}
        self.save()

    def save(self):
        self.receipt.pop('build_digest', None)
        self.receipt['build_digest'] = compat.json_digest(self.receipt)
        compat.save_json(self.directory / 'build-provenance.json', self.receipt)

    def verify(self):
        return compat.verify_build(self.directory, DEFINITION, self.inputs)

    def testMatchingContract(self):
        self.assertEqual(self.verify(), self.receipt)

    def testChangedRuntimeBytes(self):
        (self.runtime / 'Stryker.CLI.dll').write_bytes(b'changed')
        with self.assertRaisesRegex(compat.CompatibilityError, 'checksum/inventory'):
            self.verify()

    def testExtraRuntimeFile(self):
        (self.runtime / 'unrecorded.dll').write_bytes(b'extra')
        with self.assertRaisesRegex(compat.CompatibilityError, 'checksum/inventory'):
            self.verify()

    def testSymlinkedRuntimeDirectory(self):
        external = self.directory / 'external'; external.mkdir()
        (self.runtime / 'external').symlink_to(external, target_is_directory=True)
        with self.assertRaisesRegex(compat.CompatibilityError, 'Symlinked'):
            self.verify()

    def testSymlinkedReceipt(self):
        original = self.directory / 'build-provenance.json'; outside = self.directory / 'outside.json'
        original.rename(outside); original.symlink_to(outside)
        with self.assertRaisesRegex(compat.CompatibilityError, 'symlinked'):
            self.verify()

    def testChangedDescriptorBinding(self):
        self.inputs[compat.DEFINITION] = 'b' * 64
        with self.assertRaisesRegex(compat.CompatibilityError, 'Stale'):
            self.verify()

    def testUnknownBuildSchema(self):
        self.receipt['schema'] += '.future'; self.save()
        with self.assertRaisesRegex(compat.CompatibilityError, 'digest mismatch'):
            self.verify()

    def testUnknownBuildMember(self):
        self.receipt['unreviewed'] = True; self.save()
        with self.assertRaisesRegex(compat.CompatibilityError, 'members'):
            self.verify()

    def testMatchingVersionPrefixIsInsufficient(self):
        self.receipt['custom_version'] += '.other'; self.save()
        with self.assertRaisesRegex(compat.CompatibilityError, 'identity differs'):
            self.verify()

    def testWrongActualInformationalIdentity(self):
        rows = copy.deepcopy(METADATA); rows[0]['values']['System.Reflection.AssemblyInformationalVersionAttribute'] = '5.1.0'
        self.receipt['assembly_metadata'] = rows; self.save()
        with self.assertRaisesRegex(compat.CompatibilityError, 'Actual compiled'):
            self.verify()

    def testDuplicateAssemblyCannotReplaceMissingMtp(self):
        self.receipt['assembly_metadata'] = [METADATA[0], METADATA[1], METADATA[1]]; self.save()
        with self.assertRaisesRegex(compat.CompatibilityError, 'identity set'):
            self.verify()

    def testDigestTampering(self):
        receipt = json.loads((self.directory / 'build-provenance.json').read_text()); receipt['sdk'] = 'future'
        compat.save_json(self.directory / 'build-provenance.json', receipt)
        with self.assertRaisesRegex(compat.CompatibilityError, 'digest mismatch'):
            self.verify()

    def testCanonicalPatchAcceptsCheckoutCrlf(self):
        patch = compat.canonical((ROOT / compat.PATCH).read_bytes())
        self.assertEqual(compat.digest(compat.canonical(patch.replace(b'\n', b'\r\n'))), compat.PATCH_SHA)
        self.assertNotEqual(compat.digest(compat.canonical(patch + b'changed')), compat.PATCH_SHA)

    def testNoGlobalToolRootFallback(self):
        from unittest.mock import patch
        with patch.dict(os.environ, {}, clear=True):
            with self.assertRaisesRegex(compat.CompatibilityError, 'private TMPDIR'):
                compat.tool_root(self.inputs)

    def testUnknownSegmentAndArbitraryArgumentsRefuse(self):
        from unittest.mock import patch
        with self.assertRaisesRegex(compat.CompatibilityError, 'Unsupported mutation segment'):
            compat.invocation(ROOT, self.directory, 'unknown', self.receipt)
        for arguments in (['run', 'happy-path', '--mutate', '**/*'], ['run', 'happy-path', '--tool-root', '/elsewhere'], ['verify', 'unknown']):
            process = __import__('subprocess').run([sys.executable, '-B', str(ROOT/'eng/stryker_compat.py'), *arguments], capture_output=True)
            self.assertEqual(process.returncode, 2)


    def cloneSource(self):
        import subprocess
        source = subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=SOURCE_REPOSITORY, text=True).strip()
        target = self.directory / 'source'
        subprocess.run(['git', 'clone', '--shared', '--no-checkout', '--no-recurse-submodules', str(SOURCE_REPOSITORY), str(target)], check=True, capture_output=True)
        subprocess.run(['git', '-c', 'submodule.recurse=false', 'checkout', '--detach', source], cwd=target, check=True, capture_output=True)
        return target

    def testWholeSourceBoundaryIncludesReferencedProjectAndSharedTargets(self):
        source = self.cloneSource()
        compat.verify_source(source, {})
        for relative, content in [
            ('src/Hexalith.FrontComposer.Contracts/UntrackedProvenanceInput.cs', 'namespace Hexalith.FrontComposer.Contracts; public sealed class UntrackedProvenanceInput {}'),
            ('src/Directory.Build.targets', '<Project><Target Name="UntrackedInput" BeforeTargets="CoreCompile" /></Project>')]:
            with self.subTest(relative=relative):
                path = source / relative
                self.assertFalse(path.exists())
                path.write_text(content)
                try:
                    with self.assertRaisesRegex(compat.CompatibilityError, 'Untracked source/test'):
                        compat.verify_source(source, {})
                finally:
                    path.unlink()
        compat.verify_source(source, {})

    def testInvocationBindsCurrentConfigAndConstructsOriginalOptions(self):
        source = self.cloneSource()
        for segment in compat.SEGMENTS:
            args, receipt = compat.invocation(source, self.directory, segment, self.receipt)
            self.assertEqual(args[:2], ['dotnet', str(self.runtime / 'Stryker.CLI.dll')])
            self.assertEqual(args[2:], ['--test-runner', 'mtp', '--verbosity', 'debug', '--break-on-initial-test-failure',
                '--project', 'Hexalith.FrontComposer.SourceTools.csproj', '--test-project', '../../' + compat.TEST_PROJECT,
                '--config-file', '../../' + receipt['config'], '--output', '../../artifacts/mutation/' + segment])
            self.assertEqual(receipt['segment'], segment)
            self.assertEqual(receipt['config_sha256'], compat.file_hash(source / receipt['config']))
            self.assertEqual(receipt['invoked_dll_sha256'], self.receipt['runtime_files']['Stryker.CLI.dll'])
            path = source / receipt['config']; original = path.read_bytes()
            try:
                path.write_bytes(original + b' ')
                with self.assertRaisesRegex(compat.CompatibilityError, 'Uncommitted segment config'):
                    compat.invocation(source, self.directory, segment, self.receipt)
            finally:
                path.write_bytes(original)

    def testDefinitionAndLicenseDriftRefuseBeforeBuild(self):
        import shutil
        fixture = self.directory / 'definition-fixture'
        for relative in [compat.DEFINITION, compat.PATCH, compat.INSPECTOR, compat.LICENSE, 'eng/stryker_compat.py']:
            target = fixture / relative; target.parent.mkdir(parents=True, exist_ok=True); shutil.copyfile(ROOT / relative, target)
        target = fixture / '.config/dotnet-tools.json'; target.parent.mkdir(); shutil.copyfile(SOURCE_REPOSITORY / '.config/dotnet-tools.json', target)
        compat.load_definition(fixture)
        path = fixture / compat.DEFINITION; original = path.read_bytes()
        for field, value in [('schema', 'future'), ('custom_version', compat.CUSTOM_VERSION + '.other'), ('archive_sha256', 'a' * 64), ('unexpected', True)]:
            with self.subTest(field=field):
                definition = json.loads(original); definition[field] = value; path.write_text(json.dumps(definition))
                with self.assertRaises(compat.CompatibilityError):
                    compat.load_definition(fixture)
        path.write_bytes(original)
        (fixture / compat.LICENSE).write_text('changed attribution')
        with self.assertRaisesRegex(compat.CompatibilityError, 'license checksum'):
            compat.load_definition(fixture)


    def testPreparationOwnsAndReapsChildrenAcrossExitTimeoutAndCancellation(self):
        import ctypes, signal, subprocess, time
        self.assertEqual(ctypes.CDLL(None).prctl(36, 1, 0, 0, 0), 0)
        scratch = self.directory
        helper = ROOT / 'eng/stryker_compat.py'
        worker = scratch / 'worker.py'
        worker.write_text('import pathlib,os,signal,subprocess,sys,time\nroot=pathlib.Path(sys.argv[1]); mode=sys.argv[2]\n(root/\'worker.pid\').write_text(str(os.getpid()))\ncode="import os,signal,time,pathlib,sys; p=pathlib.Path("+repr(str(root.parent/\'term-seen\'))+"); signal.signal(signal.SIGTERM, lambda *_:(p.write_text(\'yes\'),time.sleep(.2),sys.exit(0))); p.with_name(\'child-ready\').write_text(pathlib.Path(\'/proc/self/status\').read_text()); time.sleep(30)" if mode=="boundary" else "import time;time.sleep(30)"\nchild=subprocess.Popen([sys.executable,"-c",code],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)\n(root/\'child.pid\').write_text(str(child.pid));time.sleep(.1)\nif mode==\'boundary\':\n deadline=time.monotonic()+5\n while not (root.parent/\'child-ready\').exists() and time.monotonic()<deadline:time.sleep(.005)\n assert (root.parent/\'child-ready\').exists()\nif mode in (\'timeout\',\'cancel\'):time.sleep(30)\nsys.exit(7 if mode==\'exit7\' else 0)\n')
        driver = scratch / 'driver.py'
        driver.write_text("import pathlib,os,signal,sys,importlib.util,ctypes,subprocess,json\nhelper=pathlib.Path(sys.argv[1]);root=pathlib.Path(sys.argv[2]);mode=sys.argv[3]\nspec=importlib.util.spec_from_file_location('compat',helper);m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)\nassert ctypes.CDLL(None).prctl(36,1,0,0,0)==0\nfor number in (signal.SIGTERM,signal.SIGINT):signal.signal(number,m.preparation_signal)\ntry:\n m.command([sys.executable,str(root.parent/'worker.py'),str(root),mode],cwd=root,env=os.environ.copy(),timeout=.3 if mode=='timeout' else 30,capture=True)\nexcept m.PreparationCancelled as e:sys.exit(128+e.number)\nexcept subprocess.TimeoutExpired:sys.exit(124)\nexcept subprocess.CalledProcessError as e:sys.exit(e.returncode)\n")

        def identity(pid):
            try:
                return Path(f'/proc/{pid}/stat').read_text().rsplit(')', 1)[1].split()[19]
            except (OSError, IndexError):
                return None

        for mode, expected in [('exit0', 0), ('exit7', 7), ('timeout', 124), ('cancel', 143), ('boundary', 143)]:
            with self.subTest(mode=mode):
                control = scratch / mode
                control.mkdir()
                marker = scratch / 'term-seen'
                marker.unlink(missing_ok=True)
                (scratch / 'child-ready').unlink(missing_ok=True)
                owned = {}
                process = subprocess.Popen([sys.executable, '-B', str(driver), str(helper), str(control), mode],
                    stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)
                try:
                    deadline = time.monotonic() + 10
                    while not (control / 'child.pid').exists() and process.poll() is None and time.monotonic() < deadline:
                        time.sleep(.005)
                    self.assertTrue((control / 'child.pid').exists())
                    child = int((control / 'child.pid').read_text())
                    for name in ('worker.pid', 'child.pid'):
                        pid = int((control / name).read_text())
                        owned[pid] = identity(pid)
                    if mode == 'cancel':
                        process.send_signal(signal.SIGTERM)
                    if mode == 'boundary':
                        while not marker.exists() and process.poll() is None and time.monotonic() < deadline:
                            time.sleep(.001)
                        self.assertTrue(marker.exists(), 'The actual child cleanup boundary must be observed.')
                        process.send_signal(signal.SIGTERM)
                    stdout, stderr = process.communicate(timeout=15)
                    self.assertEqual(process.returncode, expected, stdout + stderr)
                    self.assertFalse(Path(f'/proc/{child}').exists(), 'The owned grandchild must have been reaped.')
                finally:
                    # Always stop/join this control, including assertion/deadline
                    # failures. Never signal a process group or the test session.
                    for name in ('worker.pid', 'child.pid'):
                        path = control / name
                        if path.exists():
                            pid = int(path.read_text())
                            owned.setdefault(pid, identity(pid))
                    if process.poll() is None:
                        process.terminate()
                    try:
                        process.communicate(timeout=15)
                    except subprocess.TimeoutExpired:
                        process.kill()
                        process.communicate()
                    for pid, started in owned.items():
                        if started is not None and identity(pid) == started:
                            try:
                                descriptor = os.pidfd_open(pid)
                                try:
                                    if identity(pid) == started:
                                        signal.pidfd_send_signal(descriptor, signal.SIGKILL)
                                finally:
                                    os.close(descriptor)
                            except ProcessLookupError:
                                pass
                    for pid in owned:
                        try:
                            os.waitpid(pid, 0)
                        except ChildProcessError:
                            pass


if __name__ == '__main__':
    unittest.main(verbosity=2)
