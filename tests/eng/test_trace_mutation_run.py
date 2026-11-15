import ctypes
import json
import os
from pathlib import Path
import signal
import subprocess
import sys
import tempfile
import time
import unittest

ROOT = Path(__file__).resolve().parents[2]
WRAPPER = ROOT / "eng/trace-mutation-run.py"


@unittest.skipUnless(sys.platform == "linux", "Linux resource/process wrapper")
class ProcessControls(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        # Own failed controls too: orphaned grandchildren must not outlive tests.
        if ctypes.CDLL(None, use_errno=True).prctl(36, 1, 0, 0, 0) != 0:
            raise OSError(ctypes.get_errno(), "Cannot own test grandchildren")

    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.directory = Path(self.temp.name)
        self.process = None
        self.addCleanup(self.finish)

    def finish(self):
        if self.process is not None:
            # The test alone creates a private session. Actual mutation commands
            # retain their caller's session and never use this group cleanup.
            if self.process.poll() is None:
                os.killpg(self.process.pid, signal.SIGKILL)
            self.process.communicate(timeout=5)
        deadline = time.monotonic() + 5
        while True:
            try:
                pid, _ = os.waitpid(-1, os.WNOHANG)
            except ChildProcessError:
                return
            if pid == 0:
                children = Path(f"/proc/self/task/{os.getpid()}/children").read_text().split()
                for child in children:
                    # Unreaped direct children remain ours; their PIDs cannot
                    # be reused before waitpid reaps them.
                    try:
                        os.kill(int(child), signal.SIGKILL)
                    except ProcessLookupError:
                        pass
                if time.monotonic() > deadline:
                    self.fail("Owned test children remained after cleanup")
                time.sleep(0.02)

    def launch(self, body):
        script = self.directory / "control.py"
        script.write_text("import json, os, signal, subprocess, sys, time\nfrom pathlib import Path\n" + body)
        self.process = subprocess.Popen([sys.executable, str(WRAPPER), "--", sys.executable, str(script)],
                                        cwd=self.directory, start_new_session=True, stdin=subprocess.DEVNULL,
                                        stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)
        return self.process

    def wait_file(self, name):
        path = self.directory / name
        deadline = time.monotonic() + 5
        while not path.exists():
            if self.process.poll() is not None:
                output, error = self.process.communicate()
                self.fail(f"Control exited before {name}: {output} {error}")
            if time.monotonic() >= deadline:
                self.fail(f"Control did not create {name}")
            time.sleep(0.01)
        return json.loads(path.read_text())

    def result(self, expected):
        output, error = self.process.communicate(timeout=20)
        self.assertEqual(self.process.returncode, expected, (output, error))
        self.assertNotIn("strace", output + error)
        return output, error

    def testExitZeroPreservesOutputAndGroup(self):
        self.launch('Path("identity.json").write_text(json.dumps([os.getpid(), os.getpgrp(), os.getsid(0)]))\nprint("actual-output", flush=True)\n')
        output, _ = self.result(0)
        pid, group, session = json.loads((self.directory / "identity.json").read_text())
        self.assertNotEqual(pid, self.process.pid)
        self.assertEqual([group, session], [self.process.pid, self.process.pid])
        self.assertIn("actual-output", output)
        self.assertIn('"exit_code": 0', output)

    def testActualFailureExitIsNotSwallowed(self):
        self.launch('print("actual-refusal", flush=True)\nsys.exit(7)\n')
        output, _ = self.result(7)
        self.assertIn("actual-refusal", output)
        self.assertIn('"exit_code": 7', output)

    def cancel(self, number, group=False):
        self.launch('def cancel(number, _):\n    Path("forwarded.json").write_text(json.dumps(number))\n    sys.exit(128 + number)\n' +
                    'signal.signal(signal.SIGTERM, cancel)\nsignal.signal(signal.SIGINT, cancel)\n' +
                    'Path("ready.json").write_text(json.dumps(os.getpid()))\nwhile True: time.sleep(1)\n')
        self.wait_file("ready.json")
        if group:
            os.killpg(self.process.pid, number)
        else:
            self.process.send_signal(number)
        self.result(128 + number)
        self.assertEqual(json.loads((self.directory / "forwarded.json").read_text()), number)

    def testTermForwarded(self):
        self.cancel(signal.SIGTERM)

    def testInterruptForwarded(self):
        self.cancel(signal.SIGINT)

    def testGroupTermRemainsCancellation(self):
        self.cancel(signal.SIGTERM, group=True)

    def testGroupInterruptRemainsCancellation(self):
        self.cancel(signal.SIGINT, group=True)

    def testCancellationReapsSignalIgnoringChild(self):
        worker = ('import signal,time; from pathlib import Path; '
                  'signal.signal(signal.SIGTERM,signal.SIG_IGN); '
                  'signal.signal(signal.SIGINT,signal.SIG_IGN); '
                  'Path("child-ready.json").write_text("true"); time.sleep(30)')
        self.launch('worker = ' + repr(worker) + '\n' +
                    'child = subprocess.Popen([sys.executable, "-c", worker])\n' +
                    'def cancel(number, _):\n    sys.exit(128 + number)\n' +
                    'signal.signal(signal.SIGTERM, cancel)\n' +
                    'while not Path("child-ready.json").exists(): time.sleep(0.01)\n' +
                    'Path("ready.json").write_text(json.dumps(child.pid))\nwhile True: time.sleep(1)\n')
        child = self.wait_file("ready.json")
        self.process.send_signal(signal.SIGTERM)
        output, _ = self.result(143)
        self.assertFalse(Path(f"/proc/{child}").exists(), child)
        self.assertIn('"owned-children-cleaned"', output)

    def testStoppedThreadedChildCanBeKilledAndReplaced(self):
        worker = ('import threading,time; '
                  '[threading.Thread(target=time.sleep,args=(30,)).start() for _ in range(4)]; '
                  'print("ready",flush=True); time.sleep(30)')
        self.launch('worker = ' + repr(worker) + '\n' +
                    'for _ in range(3):\n    child = subprocess.Popen([sys.executable, "-c", worker], stdout=subprocess.PIPE, text=True)\n    assert child.stdout.readline().strip() == "ready"\n    os.kill(child.pid, signal.SIGSTOP)\n    os.kill(child.pid, signal.SIGKILL)\n    assert child.wait(timeout=5) == -signal.SIGKILL\n    child.stdout.close()\n' +
                    'print("three-owned-restarts", flush=True)\nsys.exit(7)\n')
        output, _ = self.result(7)
        self.assertIn("three-owned-restarts", output)

    def testOrphanedChildAndGrandchildAreReaped(self):
        worker = ('import subprocess,sys,time; from pathlib import Path; '
                  'child=subprocess.Popen([sys.executable,"-c","import time; time.sleep(30)"]); '
                  'Path("grandchild.json").write_text(str(child.pid)); time.sleep(30)')
        self.launch('worker = ' + repr(worker) + '\n' +
                    'child = subprocess.Popen([sys.executable, "-c", worker])\n' +
                    'while not Path("grandchild.json").exists(): time.sleep(0.01)\n' +
                    'Path("children.json").write_text(json.dumps([child.pid, int(Path("grandchild.json").read_text())]))\n')
        output, _ = self.result(0)
        for pid in json.loads((self.directory / "children.json").read_text()):
            self.assertFalse(Path(f"/proc/{pid}").exists(), pid)
        self.assertIn('"owned-children-cleaned"', output)


if __name__ == "__main__":
    unittest.main()
