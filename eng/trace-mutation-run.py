"""Linux mutation resource sampling; preserve the wrapped command's result."""
import ctypes
import json
import os
from pathlib import Path
import signal
import subprocess
import sys
import threading
import time


RUNNER_NAMES = {"Runner.Listener", "Runner.Worker"}


def emit(event, **values):
    print("mutation-diagnostics " + json.dumps({"event": event, "unix_time": time.time(), **values}), flush=True)


def enable_child_reaping():
    # Adopt only descendants of this wrapper when their parents exit. This is
    # local process state; the actual command keeps its original group/session.
    libc = ctypes.CDLL(None, use_errno=True)
    if libc.prctl(36, 1, 0, 0, 0) != 0:  # PR_SET_CHILD_SUBREAPER
        raise OSError(ctypes.get_errno(), "Cannot own orphaned mutation children")


def finish_children():
    deadline = time.monotonic() + 5
    cleaned = set()
    while True:
        while True:
            try:
                pid, _status = os.waitpid(-1, os.WNOHANG)
            except ChildProcessError:
                return cleaned
            if pid == 0:
                break
        children = set()
        for path in Path("/proc/self/task").glob("*/children"):
            try:
                children.update(int(value) for value in path.read_text().split())
            except FileNotFoundError:
                continue
        for pid in children:
            # These are unreaped direct children: their PIDs cannot be reused
            # before this wrapper waits for them. Never signal the shared group.
            number = signal.SIGTERM if time.monotonic() < deadline else signal.SIGKILL
            if pid not in cleaned or number == signal.SIGKILL:
                try:
                    os.kill(pid, number)
                except ProcessLookupError:
                    pass
                cleaned.add(pid)
        if time.monotonic() > deadline + 5:
            raise RuntimeError("Owned mutation children remained after cleanup")
        time.sleep(0.05)


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


def run_command(arguments):
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
        # Execute the actual command directly, without ptrace or another tool
        # between its exit status and this wrapper. Keep its group/session.
        process = subprocess.Popen(arguments)
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
        try:
            cleaned = finish_children()
            if cleaned:
                emit("owned-children-cleaned", count=len(cleaned))
        finally:
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
    enable_child_reaping()
    emit("diagnostic-scope", execution="direct", resource_sampling=True,
         ptrace=False, sample_interval_seconds=5)
    return run_command(sys.argv[2:])


if __name__ == "__main__":
    sys.exit(main())
