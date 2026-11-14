"""Temporary Linux mutation diagnostics; preserve the wrapped command's result."""
import hashlib
import json
import os
from pathlib import Path
import re
import selectors
import shutil
import signal
import subprocess
import sys
import tempfile
import threading
import time


TRACE_OPTIONS = ["-f", "--seccomp-bpf", "-ttt", "-qq", "-e",
                 "trace=kill,tkill,tgkill,rt_sigqueueinfo,rt_tgsigqueueinfo",
                 "-e", "signal=SIGTERM,SIGINT,SIGHUP", "--"]
RUNNER_NAMES = {"Runner.Listener", "Runner.Worker"}


def emit(event, **values):
    print("mutation-diagnostics " + json.dumps({"event": event, "unix_time": time.time(), **values}), flush=True)


def acquire_tracer(work):
    installed = shutil.which("strace")
    if installed:
        tracer = Path(installed)
    else:
        # Download/extract into owned temporary storage, without installing a
        # package or changing host privileges/settings. APT's existing signed
        # package index supplies the exact version and archive SHA256.
        policy = subprocess.check_output(["apt-cache", "policy", "strace"], text=True, timeout=15)
        candidate = re.search(r"Candidate: (\S+)", policy)
        if not candidate or candidate[1] == "(none)":
            raise RuntimeError("No strace candidate in the host package index.")
        version = candidate[1]
        metadata = subprocess.check_output(["apt-cache", "show", "strace=" + version], text=True, timeout=15)
        digest = re.search(r"^SHA256: ([0-9a-f]{64})$", metadata, re.MULTILINE)
        if not digest:
            raise RuntimeError("The strace package index has no SHA256.")
        subprocess.run(["apt-get", "download", "strace=" + version], cwd=work,
                       stdout=subprocess.PIPE, stderr=subprocess.PIPE, check=True, timeout=60)
        packages = list(work.glob("strace_*.deb"))
        if len(packages) != 1 or hashlib.sha256(packages[0].read_bytes()).hexdigest() != digest[1]:
            raise RuntimeError("The downloaded strace package differs from its index digest.")
        subprocess.run(["dpkg-deb", "--extract", str(packages[0]), str(work / "tool")], check=True, timeout=15)
        tracer = work / "tool/usr/bin/strace"
        emit("private-tool", package_version=version, archive_sha256=digest[1])
    version = subprocess.check_output([str(tracer), "--version"], text=True, timeout=10).splitlines()[0]
    emit("tracer", version=version, binary_sha256=hashlib.sha256(tracer.read_bytes()).hexdigest())
    return tracer


def verify_tracer(tracer):
    # Refuse unsupported tracing before starting Stryker. In particular, never
    # silently fall back to stopping every syscall if seccomp-bpf is unavailable.
    process = sender = None
    try:
        process = subprocess.Popen([str(tracer), *TRACE_OPTIONS, sys.executable, "-c",
                                    "import os,time;print(os.getpid(),flush=True);time.sleep(30)"],
                                   stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True, start_new_session=True)
        with selectors.DefaultSelector() as selector:
            selector.register(process.stdout, selectors.EVENT_READ)
            if not selector.select(5):
                raise RuntimeError("The strace control did not start within five seconds.")
            target_text = process.stdout.readline().strip()
            if not target_text.isdecimal():
                _, trace = process.communicate(timeout=5)
                raise RuntimeError("The strace control could not launch its tracee: " + trace)
            target = int(target_text)
        sender = subprocess.Popen([sys.executable, "-c",
                                   "import os,signal,sys;os.kill(int(sys.argv[1]),signal.SIGTERM)", str(target)])
        if sender.wait(timeout=5) != 0:
            raise RuntimeError("The known signal sender failed.")
        _, trace = process.communicate(timeout=5)
        if process.returncode != -signal.SIGTERM or "strace:" in trace or not re.search(
                r"SIGTERM.*si_pid=" + str(sender.pid) + r", si_uid=" + str(os.getuid()), trace):
            raise RuntimeError("The known-sender/seccomp control failed: " + trace)
        emit("known-sender-control", target_pid=target, sender_pid=sender.pid,
             sender_uid=os.getuid(), exit_code=process.returncode)
    finally:
        for child in (sender, process):
            if child is not None:
                if child.poll() is None:
                    child.kill()
                child.wait()
        if process is not None:
            # The synthetic probe alone has an owned group, so even failed
            # seccomp setup cannot leave its disposable tracee behind.
            try:
                os.killpg(process.pid, signal.SIGKILL)
            except ProcessLookupError:
                pass


def process_snapshot():
    rows = []
    for entry in Path("/proc").iterdir():
        if not entry.name.isdecimal():
            continue
        try:
            stat = (entry / "stat").read_text()
            name = stat[stat.index("(") + 1:stat.rindex(")")]
            if entry.stat().st_uid != os.getuid() and name not in RUNNER_NAMES:
                continue
            fields = stat[stat.rindex(")") + 2:].split()
            rows.append({"pid": int(entry.name), "comm": name, "ppid": int(fields[1]),
                         "pgrp": int(fields[2]), "sid": int(fields[3]), "threads": int(fields[17]),
                         "rss_kib": int(fields[21]) * os.sysconf("SC_PAGE_SIZE") // 1024})
        except (OSError, ValueError, IndexError):
            continue  # A process can exit between the proc reads.
    return rows


def resource_sample(root_pid):
    rows = process_snapshot()
    selected = {root_pid, os.getpid(), os.getppid()}
    for _ in range(len(rows)):
        additions = {row["pid"] for row in rows if row["ppid"] in selected}
        if additions <= selected:
            break
        selected.update(additions)
    # Bound console volume; highest RSS siblings can identify a sender/resource
    # contender without printing arguments, environments, usernames or paths.
    ordered = sorted(rows, key=lambda row: row["rss_kib"], reverse=True)
    selected.update(row["pid"] for row in ordered[:8])
    runners = [row for row in ordered if row["comm"] in RUNNER_NAMES]
    other = [row for row in ordered if row["pid"] in selected and row["comm"] not in RUNNER_NAMES]
    sample = {"root_pid": root_pid, "processes": (runners + other)[:32]}
    sample["memory_kib"] = {line.split(":")[0]: int(line.split()[1])
                            for line in Path("/proc/meminfo").read_text().splitlines()
                            if line.split(":")[0] in {"MemTotal", "MemAvailable", "SwapTotal", "SwapFree"}}
    for kind in ("memory", "cpu"):
        pressure = Path("/proc/pressure") / kind
        if pressure.exists():
            sample[kind + "_pressure"] = pressure.read_text().strip()
    membership = Path("/proc/self/cgroup").read_text().splitlines()
    unified = next((line[3:] for line in membership if line.startswith("0::")), None)
    if unified is not None and ".." not in Path(unified).parts:
        cgroup = Path("/sys/fs/cgroup") / unified.lstrip("/")
        sample["cgroup"] = {name: (cgroup / name).read_text().strip() for name in
                            ("memory.current", "memory.max", "memory.events", "cpu.stat") if (cgroup / name).exists()}
    emit("resources", **sample)


def run_command(tracer, arguments):
    stop = threading.Event()
    process = None
    received_signal = None

    def on_signal(number, _frame):
        nonlocal received_signal
        received_signal = number

    def sample():
        while not stop.is_set():
            try:
                resource_sample(process.pid)
            except (OSError, ValueError) as error:
                emit("sampling-error", error=type(error).__name__)
            stop.wait(5)

    previous = {number: signal.signal(number, on_signal) for number in (signal.SIGTERM, signal.SIGINT, signal.SIGHUP)}
    sampler = None
    try:
        # Keep the original process group/session: changing those would obscure
        # a process-group signal. strace streams signal sender PID/UID to stderr.
        process = subprocess.Popen([str(tracer), *TRACE_OPTIONS, *arguments])
        sampler = threading.Thread(target=sample, name="mutation-resource-sampler")
        sampler.start()
        while received_signal is None:
            try:
                code = process.wait(timeout=1)
                if received_signal is not None:
                    break
                emit("command-exit", exit_code=code)
                return code if code >= 0 else 128 - code
            except subprocess.TimeoutExpired:
                pass
        emit("wrapper-signal", signal=received_signal)
        # Forward cancellation; do not suppress it or turn it into success.
        try:
            process.send_signal(received_signal)
        except ProcessLookupError:
            pass
        try:
            process.wait(timeout=10)
        except subprocess.TimeoutExpired:
            process.kill()
            process.wait()
        return 128 + received_signal
    finally:
        if process is not None:
            if process.poll() is None:
                process.kill()
            process.wait()
        stop.set()
        if sampler is not None:
            sampler.join()
        for number, handler in previous.items():
            signal.signal(number, handler)
        # Also cover a signal delivered after wait's result was computed but
        # before the handlers were restored. Setup's restored handler raises
        # the same nonzero exit if a later signal arrives during function exit.
        if received_signal is not None:
            raise SystemExit(128 + received_signal)


def main():
    if len(sys.argv) < 3 or sys.argv[1] != "--":
        raise RuntimeError("Usage: python3 trace-mutation-run.py -- COMMAND [ARGUMENT ...]")
    def cancel_setup(number, _frame):
        raise SystemExit(128 + number)

    for number in (signal.SIGTERM, signal.SIGINT, signal.SIGHUP):
        signal.signal(number, cancel_setup)
    with tempfile.TemporaryDirectory(prefix="mutation-trace-", dir=os.environ.get("RUNNER_TEMP")) as name:
        tracer = acquire_tracer(Path(name))
        verify_tracer(tracer)
        # seccomp-bpf implies strace kill-on-exit: if the tracer itself dies,
        # its tracees are killed. This is diagnostic evidence, never a score.
        emit("diagnostic-scope", seccomp_bpf=True, tracer_kill_on_exit=True, sample_interval_seconds=5)
        return run_command(tracer, sys.argv[2:])


if __name__ == "__main__":
    sys.exit(main())
