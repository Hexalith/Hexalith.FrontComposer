#!/usr/bin/env python3
"""Offline AD-7/AD-19 publication-candidate data boundary.

Callers must independently authenticate Actions run/artifact coordinates, the original
CI handoff and active policy before supplying them here. This module neither grants
publication authority nor performs network requests, attestation, or publication.
Protected use additionally requires bundling these bytes in an accepted, literal-commit
owner action and activating its complete closure through AD-12/AD-16.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import shutil
import stat
import struct
import sys
import tempfile
import xml.etree.ElementTree as ET
import zipfile
import zlib
from dataclasses import dataclass
from pathlib import Path
from typing import Any, BinaryIO

import dependency_graph as dg
import dependency_handoff as dh
import release_contract as rc

SCHEMA = "hexalith.publication-candidate.v1"
DESCRIPTOR = "publication-candidate.json"
MAX_ARCHIVE = 512 * 1024 * 1024
MAX_MEMBERS = 256
MAX_MEMBER = 128 * 1024 * 1024
MAX_TOTAL = 1024 * 1024 * 1024
MAX_RATIO = 100
MAX_PATH_DEPTH = 8
MAX_PATH_BYTES = 240
MAX_INTEGER = 9007199254740991
CHUNK = 64 * 1024
ROLES = frozenset({"package", "symbol", "evidence", "release-metadata"})
ASSET_RE = re.compile(r"[A-Za-z0-9](?:[A-Za-z0-9._-]{0,126}[A-Za-z0-9])?")
VERSION_RE = re.compile(r"(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(?:-([0-9a-z-]+(?:\.[0-9a-z-]+)*))?")


class CandidateError(ValueError):
    """Raised before publication on any unauthenticated or malformed candidate."""


def _closed(value: Any, keys: set[str], context: str) -> dict:
    if not isinstance(value, dict) or set(value) != keys:
        raise CandidateError(f"{context}: members must be exactly {sorted(keys)}")
    return value


def _integer(value: Any, context: str, minimum: int = 0) -> int:
    if type(value) is not int or not minimum <= value <= MAX_INTEGER:
        raise CandidateError(f"{context}: expected safe JSON integer >= {minimum}")
    return value


def _hash(value: Any, context: str) -> str:
    if not isinstance(value, str) or re.fullmatch(r"[0-9a-f]{64}", value) is None:
        raise CandidateError(f"{context}: expected lowercase SHA-256")
    return value


def _json(raw: bytes, context: str) -> Any:
    def pairs(rows):
        result = {}
        for name, value in rows:
            if name in result:
                raise CandidateError(f"{context}: duplicate JSON member {name!r}")
            result[name] = value
        return result

    def invalid(_):
        raise CandidateError(f"{context}: noninteger JSON number")

    try:
        result = json.loads(raw.decode("utf-8"), object_pairs_hook=pairs,
                            parse_float=invalid, parse_constant=invalid)
        # Apply AD-5's number domain even inside imported policy/evaluator projections.
        pending = [result]
        while pending:
            value = pending.pop()
            if type(value) is int:
                _integer(value, context)
            elif isinstance(value, dict):
                pending.extend(value.values())
            elif isinstance(value, list):
                pending.extend(value)
        return result
    except (UnicodeError, json.JSONDecodeError, RecursionError) as exc:
        raise CandidateError(f"{context}: malformed UTF-8 JSON") from exc


def _ad5(value: Any) -> None:
    """Enforce number/commit domains also for direct Python API inputs."""
    pending = [(None, value)]
    commit_fields = {"commit", "candidate", "base", "merge_base", "owner_commit", "head_sha", "builds_execution_sha"}
    while pending:
        key, item = pending.pop()
        if key in commit_fields and item is not None:
            if not isinstance(item, str) or re.fullmatch(r"[0-9a-f]{40}", item) is None or item == "0" * 40:
                raise CandidateError(f"{key}: expected nonzero lowercase 40-hex commit")
        if type(item) is int:
            _integer(item, "AD-5 number")
        elif isinstance(item, dict):
            pending.extend(item.items())
        elif isinstance(item, list):
            pending.extend((None, child) for child in item)
        elif item is not None and type(item) not in {str, bool}:
            raise CandidateError("unsupported AD-5 JSON value")


def _path(value: Any) -> str:
    if (not isinstance(value, str) or not value or not value.isascii()
            or len(value.encode("utf-8")) > MAX_PATH_BYTES
            or len(value.split("/")) > MAX_PATH_DEPTH
            or "\\" in value or ":" in value
            or any(ord(char) < 32 or ord(char) == 127 for char in value)
            or any(part in {"", ".", ".."} for part in value.split("/"))):
        raise CandidateError(f"unsafe or over-limit archive path: {value!r}")
    return value


def validate_version(value: Any) -> str:
    """Require the exact AD-19 canonical NuGet/SemVer subset."""
    if not isinstance(value, str) or len(value) > 128 or not value.isascii():
        raise CandidateError("release version must be 1..128 ASCII bytes")
    match = VERSION_RE.fullmatch(value)
    if match is None or any(int(part) > 2147483647 for part in match.groups()[:3]):
        raise CandidateError("release version is not the AD-19 canonical subset")
    if match[4] and any(part.isdigit() and len(part) > 1 and part[0] == "0"
                        for part in match[4].split(".")):
        raise CandidateError("numeric prerelease identifiers cannot have leading zeroes")
    return value


def validate_release_run(value: Any) -> dict:
    run = _closed(value, {"repository", "workflow_path", "run_id", "run_attempt"}, "release_run")
    if run["repository"] != dh.ROOT_REPOSITORY or run["workflow_path"] != dh.RELEASE_WORKFLOW_PATH:
        raise CandidateError("release_run repository/workflow mismatch")
    _integer(run["run_id"], "release run ID", 1)
    _integer(run["run_attempt"], "release run attempt", 1)
    return run


def durable_asset_names(release_run: dict) -> set[str]:
    """Reserve publisher-produced names, including either authorization branch."""
    validate_release_run(release_run)
    return {DESCRIPTOR, f"publication-candidate-{release_run['run_id']}-{release_run['run_attempt']}.zip",
            "dependency-release-handoff.json", "release-evidence.json", "provenance-attestation.json",
            "attestation-fallback-authorization.json"}


def validate_asset_names(rows: list[dict], additional_names: set[str]) -> None:
    """Reject collisions by destination name, independently of digest or size."""
    names = list(additional_names)
    if len(names) != len(set(names)):
        raise CandidateError("duplicate publisher destination asset_name")
    seen = set(names)
    for row in rows:
        name = row["asset_name"]
        if name is None:
            continue
        if not isinstance(name, str) or ASSET_RE.fullmatch(name) is None:
            raise CandidateError(f"invalid destination asset_name: {name!r}")
        if name in seen:
            raise CandidateError(f"duplicate or reserved destination asset_name: {name!r}")
        seen.add(name)


def validate_descriptor(value: Any, *, ci_handoff_raw: bytes, release_run: dict,
                        evaluator: dict, policy: dict, required_evidence: set[str]) -> dict:
    """Bind the closed descriptor to independently authenticated authority inputs.

    ``required_evidence`` is the owner-controlled FR-24 evidence inventory. It is
    never taken from the descriptor. This is a structural/data check, not final
    manifest classification or authorization.
    """
    _ad5(value)
    _ad5(release_run)
    _ad5(evaluator)
    _ad5(policy)
    descriptor = _closed(value, {"schema", "release_run", "candidate", "ci_handoff_sha256",
                                "dependency_policy", "evaluator", "release_plan", "files"}, "descriptor")
    if descriptor["schema"] != SCHEMA:
        raise CandidateError(f"descriptor schema must be {SCHEMA}")
    validate_release_run(descriptor["release_run"])
    if descriptor["release_run"] != validate_release_run(release_run):
        raise CandidateError("descriptor release run/attempt differs from authenticated run")
    if len(ci_handoff_raw) > MAX_MEMBER:
        raise CandidateError("CI handoff exceeds member bound")
    ci = dh.validate_ci_handoff(_json(ci_handoff_raw, "CI handoff"))
    _ad5(ci)
    dh.require_evaluator_authorized(policy, "ci", ci["evaluator"])
    dh.require_evaluator_authorized(policy, "release", evaluator)
    if descriptor["candidate"] != ci["run"]["candidate"]:
        raise CandidateError("descriptor candidate differs from original CI candidate")
    if descriptor["ci_handoff_sha256"] != hashlib.sha256(ci_handoff_raw).hexdigest():
        raise CandidateError("descriptor CI handoff raw-byte digest mismatch")
    if descriptor["dependency_policy"] != ci["dependency_policy"]:
        raise CandidateError("descriptor active policy differs from CI handoff")
    if descriptor["evaluator"] != evaluator:
        raise CandidateError("descriptor release evaluator differs from authenticated evaluator")
    caller, reusable = evaluator["caller"], evaluator["reusable"]
    if (caller["repository"] != dh.ROOT_REPOSITORY or caller["workflow_path"] != dh.RELEASE_WORKFLOW_PATH
            or caller["commit"] != descriptor["candidate"]
            or reusable["repository"] != "github.com/hexalith/hexalith.builds"
            or reusable["workflow_path"] != ".github/workflows/domain-release.yml"):
        raise CandidateError("release evaluator does not name the exact candidate/Builds reusable")
    rows = descriptor["files"]
    if not isinstance(rows, list) or len(rows) >= MAX_MEMBERS:
        raise CandidateError("descriptor files exceed archive member limit or are not an array")
    paths = []
    total = 0
    for row in rows:
        _closed(row, {"path", "asset_name", "sha256", "size", "role"}, "descriptor file")
        paths.append(_path(row["path"]))
        _hash(row["sha256"], row["path"])
        total += _integer(row["size"], row["path"])
        if row["size"] > MAX_MEMBER or total > MAX_TOTAL:
            raise CandidateError("descriptor file sizes exceed AD-7 limits")
        if not isinstance(row["role"], str) or row["role"] not in ROLES:
            raise CandidateError("unknown descriptor file role")
        if (row["role"] == "release-metadata") != (row["asset_name"] is None):
            raise CandidateError("only release-metadata files use null asset_name")
    if paths != sorted(set(paths)) or any(path == DESCRIPTOR or path.startswith(DESCRIPTOR + "/") for path in paths):
        raise CandidateError("descriptor paths must be sorted, unique and exclude the descriptor")
    # No file may also be a parent directory of another declared file.
    for path in paths:
        if any("/".join(path.split("/")[:i]) in paths for i in range(1, len(path.split("/")))):
            raise CandidateError("archive file/directory path collision")
    validate_asset_names(rows, durable_asset_names(release_run))
    plan = descriptor["release_plan"]
    if plan is not None:
        _closed(plan, {"version", "tag", "release_name", "release_notes_path"}, "release plan")
        version = validate_version(plan["version"])
        if plan["tag"] != "v" + version or not isinstance(plan["release_name"], str) or not plan["release_name"].strip():
            raise CandidateError("release tag/name is invalid")
        notes = _path(plan["release_notes_path"])
        if not any(row["path"] == notes and row["role"] == "release-metadata" and row["size"] <= 1024 * 1024 for row in rows):
            raise CandidateError("release notes must be declared bounded release-metadata")
        if not required_evidence:
            raise CandidateError("release requires an independent FR-24 evidence inventory")
    elif any(row["role"] in {"package", "symbol"} for row in rows):
        raise CandidateError("no-release descriptor cannot carry publication packages")
    for path in required_evidence:
        _path(path)
        if not any(row["path"] == path and row["role"] == "evidence" for row in rows):
            raise CandidateError(f"required owner-controlled evidence missing: {path}")
    return descriptor


@dataclass(frozen=True)
class _Member:
    name: str
    flags: int
    method: int
    crc: int
    compressed: int
    size: int
    offset: int
    zip64: bool


def _read(source: BinaryIO, size: int) -> bytes:
    value = source.read(size)
    if len(value) != size:
        raise CandidateError("truncated ZIP structure")
    return value


def _extra(raw: bytes, fields: list[int]) -> list[int]:
    values = {}
    cursor = 0
    while cursor < len(raw):
        if cursor + 4 > len(raw):
            raise CandidateError("truncated ZIP extra field")
        kind, size = struct.unpack_from("<HH", raw, cursor)
        cursor += 4
        if kind in values or cursor + size > len(raw):
            raise CandidateError("duplicate or truncated ZIP extra field")
        values[kind] = raw[cursor:cursor + size]
        cursor += size
    required = [i for i, value in enumerate(fields) if value == (0xFFFF if i == 3 else 0xFFFFFFFF)]
    payload = values.get(1, b"")
    if len(payload) != sum(4 if i == 3 else 8 for i in required):
        raise CandidateError("inconsistent ZIP64 extra fields")
    cursor = 0
    result = list(fields)
    for i in required:
        width = 4 if i == 3 else 8
        result[i] = int.from_bytes(payload[cursor:cursor + width], "little")
        cursor += width
    return result


def _index(source: BinaryIO, archive_size: int) -> tuple[list[_Member], int]:
    """Stream the bounded central directory before opening any member."""
    source.seek(max(0, archive_size - 65557))
    tail = source.read()
    position = tail.rfind(b"PK\x05\x06")
    if position < 0 or position + 22 > len(tail):
        raise CandidateError("missing ZIP end-of-central-directory")
    end = archive_size - len(tail) + position
    _, disk, central_disk, disk_count, count, size, offset, comment = struct.unpack_from("<4s4H2LH", tail, position)
    if position + 22 + comment != len(tail) or disk or central_disk or disk_count != count:
        raise CandidateError("inconsistent or multidisk ZIP end record")
    central_end = end
    if end >= 20:
        source.seek(end - 20)
        locator = _read(source, 20)
    else:
        locator = b""
    has_zip64 = locator[:4] == b"PK\x06\x07"
    if has_zip64:
        _, zip_disk, zip_offset, disk_total = struct.unpack("<4sLQL", locator)
        if zip_disk or disk_total != 1 or zip_offset + 56 != end - 20:
            raise CandidateError("inconsistent ZIP64 locator")
        source.seek(zip_offset)
        record = struct.unpack("<4sQ2H2L4Q", _read(source, 56))
        signature, record_size, _, _, zip_disk, zip_central_disk, zip_disk_count, zip_count, zip_size, zip_start = record
        if signature != b"PK\x06\x06" or record_size != 44 or zip_disk or zip_central_disk or zip_disk_count != zip_count:
            raise CandidateError("inconsistent ZIP64 end record")
        for original, actual, sentinel in ((count, zip_count, 0xFFFF), (size, zip_size, 0xFFFFFFFF), (offset, zip_start, 0xFFFFFFFF)):
            if original != sentinel and original != actual:
                raise CandidateError("ZIP64/end record disagreement")
        count, size, offset = zip_count, zip_size, zip_start
        central_end = zip_offset
    elif count == 0xFFFF or size == 0xFFFFFFFF or offset == 0xFFFFFFFF:
        raise CandidateError("missing ZIP64 end record")
    if not 1 <= count <= MAX_MEMBERS or offset + size != central_end or offset < 0:
        raise CandidateError("ZIP central directory exceeds bounds or overlaps records")
    source.seek(offset)
    members = []
    names = set()
    compressed_total = total = 0
    for _ in range(count):
        if source.tell() + 46 > central_end:
            raise CandidateError("truncated central directory")
        row = struct.unpack("<4s6H3L5H2L", _read(source, 46))
        signature, made, needed, flags, method, _, _, crc, compressed, expanded, name_len, extra_len, comment_len, member_disk, _, attributes, local = row
        if signature != b"PK\x01\x02" or needed > 45 or flags & ~0x80E or method not in {0, 8} or (method == 0 and flags & 6):
            raise CandidateError("encrypted or unsupported ZIP member")
        if not 1 <= name_len <= MAX_PATH_BYTES or source.tell() + name_len + extra_len + comment_len > central_end:
            raise CandidateError("ZIP member name/metadata exceeds bounds")
        try:
            name = _path(_read(source, name_len).decode("ascii"))
        except UnicodeError as exc:
            raise CandidateError("ZIP member path must be ASCII") from exc
        raw_extra = _read(source, extra_len)
        source.seek(comment_len, 1)
        zip64 = expanded == 0xFFFFFFFF or compressed == 0xFFFFFFFF
        expanded, compressed, local, member_disk = _extra(raw_extra, [expanded, compressed, local, member_disk])
        mode = attributes >> 16
        if member_disk or attributes & 0x10 or stat.S_IFMT(mode) not in {0, stat.S_IFREG}:
            raise CandidateError(f"non-regular ZIP member: {name}")
        if name in names:
            raise CandidateError(f"duplicate ZIP member: {name}")
        names.add(name)
        total += expanded
        compressed_total += compressed
        if (expanded > MAX_MEMBER or total > MAX_TOTAL or expanded > MAX_RATIO * max(compressed, 1)
                or local >= offset or compressed > archive_size):
            raise CandidateError(f"ZIP member exceeds AD-7 size/ratio/offset limits: {name}")
        members.append(_Member(name, flags, method, crc, compressed, expanded, local, zip64))
    if source.tell() != central_end or total > MAX_RATIO * max(compressed_total, 1):
        raise CandidateError("ZIP central-directory size or aggregate ratio mismatch")
    return sorted(members, key=lambda item: item.offset), offset


def _stream_member(source: BinaryIO, member: _Member, boundary: int, sink: BinaryIO | None) -> tuple[str, int]:
    source.seek(member.offset)
    row = struct.unpack("<4s5H3L2H", _read(source, 30))
    signature, needed, flags, method, _, _, crc, compressed, expanded, name_len, extra_len = row
    if signature != b"PK\x03\x04" or needed > 45 or flags != member.flags or method != member.method:
        raise CandidateError(f"local/central ZIP header disagreement: {member.name}")
    if name_len != len(member.name) or source.tell() + name_len + extra_len > boundary:
        raise CandidateError("local ZIP header overlaps another record")
    if _read(source, name_len) != member.name.encode("ascii"):
        raise CandidateError("local/central ZIP member name disagreement")
    local_zip64 = expanded == 0xFFFFFFFF or compressed == 0xFFFFFFFF
    expanded, compressed = _extra(_read(source, extra_len), [expanded, compressed])
    if flags & 8:
        if crc not in {0, member.crc} or expanded not in {0, member.size} or compressed not in {0, member.compressed}:
            raise CandidateError("data-descriptor local ZIP sizes disagree")
    elif (crc, compressed, expanded) != (member.crc, member.compressed, member.size):
        raise CandidateError("local/central ZIP CRC or size disagreement")
    if source.tell() + member.compressed > boundary:
        raise CandidateError("ZIP compressed member overlaps another record")
    digest, actual_crc, actual_size = hashlib.sha256(), 0, 0
    inflater = zlib.decompressobj(-15) if method == 8 else None

    def accept(raw):
        nonlocal actual_crc, actual_size
        actual_size += len(raw)
        if actual_size > member.size or actual_size > MAX_MEMBER:
            raise CandidateError("streamed ZIP data exceeds declared size")
        digest.update(raw)
        actual_crc = zlib.crc32(raw, actual_crc)
        if sink is not None:
            sink.write(raw)

    remaining = member.compressed
    while remaining:
        chunk = _read(source, min(CHUNK, remaining))
        remaining -= len(chunk)
        if inflater is None:
            accept(chunk)
        else:
            while chunk:
                try:
                    accept(inflater.decompress(chunk, CHUNK))
                except zlib.error as exc:
                    raise CandidateError("invalid ZIP deflate stream") from exc
                chunk = inflater.unconsumed_tail
                if inflater.unused_data:
                    raise CandidateError("ZIP deflate stream contains undeclared trailing data")
    if inflater is not None and not inflater.eof:
        raise CandidateError("truncated ZIP deflate stream")
    if (actual_size, actual_crc) != (member.size, member.crc):
        raise CandidateError(f"streamed ZIP CRC/size mismatch: {member.name}")
    if flags & 8:
        first = _read(source, 4)
        if first == b"PK\x07\x08":
            first = _read(source, 4)
        descriptor_crc = int.from_bytes(first, "little")
        width = 8 if member.zip64 or local_zip64 else 4
        descriptor_compressed = int.from_bytes(_read(source, width), "little")
        descriptor_size = int.from_bytes(_read(source, width), "little")
        if (descriptor_crc, descriptor_compressed, descriptor_size) != (member.crc, member.compressed, member.size):
            raise CandidateError("ZIP data descriptor disagrees with streamed data")
    if source.tell() != boundary:
        raise CandidateError("overlapping records, hidden ZIP data or inconsistent member size")
    return digest.hexdigest(), actual_size


def _snapshot(path: Path, target: BinaryIO) -> tuple[str, int]:
    digest, size = hashlib.sha256(), 0
    with _open_regular(path.parent, path.name) as source:
        if os.fstat(source.fileno()).st_size > MAX_ARCHIVE:
            raise CandidateError("raw archive exceeds 512 MiB")
        for chunk in iter(lambda: source.read(min(CHUNK, MAX_ARCHIVE - size + 1)), b""):
            size += len(chunk)
            if size > MAX_ARCHIVE:
                raise CandidateError("raw archive exceeds 512 MiB")
            digest.update(chunk)
            target.write(chunk)
    target.seek(0)
    return digest.hexdigest(), size


class _NuspecBuilder(ET.TreeBuilder):
    """Reject DTDs in the XML parser, after encoding detection and before expansion."""

    def doctype(self, name, pubid, system):
        raise CandidateError("nuspec declarations are forbidden")


def _package_identity(path: Path) -> tuple[str, str]:
    # Reuse the existing bounded unsigned-package/member verifier. Candidate files
    # remain data throughout; neither NuGet nor MSBuild nor lifecycle hooks run.
    rc.validate_unsigned_candidate(path)
    with zipfile.ZipFile(path) as archive:
        entries = archive.infolist()
        for item in entries:
            mode = stat.S_IFMT(item.external_attr >> 16)
            if (mode not in ({0, stat.S_IFDIR} if item.is_dir() else {0, stat.S_IFREG})
                    or (item.external_attr & 0x10 and not item.is_dir())):
                raise CandidateError(f"non-regular NuGet member: {item.filename}")
        matches = [item for item in entries if "/" not in item.filename and item.filename.endswith(".nuspec")]
        if len(matches) != 1 or matches[0].file_size > 1024 * 1024:
            raise CandidateError(f"{path.name}: expected one bounded root nuspec")
        raw = archive.read(matches[0])
    try:
        root = ET.fromstring(raw, parser=ET.XMLParser(target=_NuspecBuilder()))
        if root.tag.rsplit("}", 1)[-1] != "package":
            raise CandidateError("nuspec root must be package")
        metadata = [item for item in root if item.tag.rsplit("}", 1)[-1] == "metadata"]
        if len(metadata) != 1:
            raise CandidateError("nuspec must have exactly one metadata element")
        def field(name):
            rows = [item for item in metadata[0] if item.tag.rsplit("}", 1)[-1] == name]
            if len(rows) != 1 or not rows[0].text or len(rows[0]):
                raise CandidateError(f"nuspec {name} must be one nonempty scalar element")
            return rows[0].text
        return field("id"), validate_version(field("version"))
    except (ET.ParseError, LookupError) as exc:
        raise CandidateError("malformed nuspec XML or unsupported encoding") from exc


def _verify_package_set(directory: Path, descriptor: dict, expected_package_ids: set[str]) -> None:
    plan = descriptor["release_plan"]
    for row in descriptor["files"]:
        named_package = any(isinstance(value, str) and value.lower().endswith((".nupkg", ".snupkg"))
                            for value in (row["path"], row["asset_name"]))
        if plan is None and (row["role"] in {"package", "symbol"} or named_package):
            raise CandidateError("no-release candidate cannot contain package/symbol data")
        if row["role"] not in {"package", "symbol"} and named_package:
            raise CandidateError("package bytes cannot be relabelled as evidence/metadata")
    if plan is None:
        return
    if not expected_package_ids:
        raise CandidateError("release requires an independent package inventory")
    packages, symbols = {}, {}
    for row in descriptor["files"]:
        if row["role"] not in {"package", "symbol"}:
            continue
        package_id, version = _package_identity(directory / row["path"])
        suffix = ".nupkg" if row["role"] == "package" else ".snupkg"
        expected_name = f"{package_id}.{plan['version']}{suffix}"
        if (package_id not in expected_package_ids or version != plan["version"]
                or row["asset_name"] != expected_name or Path(row["path"]).name != expected_name):
            raise CandidateError(f"package inventory/version/name mismatch: {row['path']}")
        target = packages if row["role"] == "package" else symbols
        if package_id in target:
            raise CandidateError(f"duplicate package identity: {package_id}")
        target[package_id] = row
    if set(packages) != expected_package_ids or set(symbols) != expected_package_ids:
        raise CandidateError("package/symbol set differs from independent package inventory")
    notes = directory / plan["release_notes_path"]
    try:
        notes.read_bytes().decode("utf-8")
    except UnicodeError as exc:
        raise CandidateError("release notes are not valid UTF-8") from exc


def verify_archive(archive: Path, *, coordinate: dict, ci_handoff_raw: bytes,
                   release_run: dict, evaluator: dict, policy: dict,
                   required_evidence: set[str], expected_package_ids: set[str],
                   extract_to: Path | None = None) -> dict:
    """Authenticate a raw ZIP completely before materializing declared regular data.

    The API coordinate must come from the authenticated Actions artifact listing;
    its descriptor digest must come from independent authenticated handoff evidence.
    No success result contains a seal, classification or publication authorization.
    """
    _closed(coordinate, {"artifact_name", "artifact_id", "run_id", "run_attempt", "archive_sha256", "descriptor_sha256"}, "artifact coordinate")
    validate_release_run(release_run)
    _integer(coordinate["artifact_id"], "artifact ID", 1)
    for key in ("run_id", "run_attempt"):
        _integer(coordinate[key], f"artifact {key}", 1)
        if coordinate[key] != release_run[key]:
            raise CandidateError("artifact run/attempt does not match authenticated Release")
    if coordinate["artifact_name"] != f"publication-candidate-{release_run['run_id']}-{release_run['run_attempt']}":
        raise CandidateError("artifact name is not run-bound")
    _hash(coordinate["archive_sha256"], "archive digest")
    _hash(coordinate["descriptor_sha256"], "descriptor digest")
    with tempfile.TemporaryFile() as snapshot, tempfile.TemporaryDirectory(prefix="publication-candidate-") as temporary:
        archive_digest, archive_size = _snapshot(archive, snapshot)
        if archive_digest != coordinate["archive_sha256"]:
            raise CandidateError("authenticated raw archive digest mismatch")
        members, central = _index(snapshot, archive_size)
        if members[0].offset != 0:
            raise CandidateError("undeclared prefix before ZIP members")
        if sum(member.name == DESCRIPTOR for member in members) != 1:
            raise CandidateError("archive must contain exactly one descriptor")
        bounds = [item.offset for item in members[1:]] + [central]
        identities = {}
        descriptor_raw = b""
        # First pass: authenticate every compressed stream with no archive-path writes.
        for member, boundary in zip(members, bounds):
            if member.name == DESCRIPTOR:
                with tempfile.TemporaryFile() as content:
                    identities[member.name] = _stream_member(snapshot, member, boundary, content)
                    content.seek(0)
                    descriptor_raw = content.read()
            else:
                identities[member.name] = _stream_member(snapshot, member, boundary, None)
        if hashlib.sha256(descriptor_raw).hexdigest() != coordinate["descriptor_sha256"]:
            raise CandidateError("authenticated descriptor digest mismatch")
        descriptor = validate_descriptor(_json(descriptor_raw, "descriptor"), ci_handoff_raw=ci_handoff_raw,
                                         release_run=release_run, evaluator=evaluator, policy=policy,
                                         required_evidence=required_evidence)
        declared = {row["path"]: (row["sha256"], row["size"]) for row in descriptor["files"]}
        if {name: identity for name, identity in identities.items() if name != DESCRIPTOR} != declared:
            raise CandidateError("archive members/digests/sizes differ from exact descriptor inventory")
        directory = Path(temporary)
        # Only after the full archive, descriptor and authority checks pass do we
        # create fresh private regular files. Never extractall or follow links.
        for member, boundary in zip(members, bounds):
            destination = directory / member.name
            destination.parent.mkdir(parents=True, exist_ok=True)
            with destination.open("xb") as target:
                os.chmod(destination, 0o600)
                _stream_member(snapshot, member, boundary, target)
        _verify_package_set(directory, descriptor, expected_package_ids)
        if extract_to is not None:
            # copytree's exclusive destination prevents overwriting an existing tree.
            # Clean only the new tree this call created if an I/O error interrupts it.
            extract_to.mkdir(parents=False, exist_ok=False)
            try:
                shutil.copytree(directory, extract_to, dirs_exist_ok=True)
            except BaseException:
                shutil.rmtree(extract_to)
                raise
        return {"archive_sha256": archive_digest,
                "descriptor_sha256": hashlib.sha256(descriptor_raw).hexdigest(), "descriptor": descriptor}


def _open_regular(root: Path, path: str) -> BinaryIO:
    """Open a confined regular file without following any path-component symlink."""
    parts = _path(path).split("/")
    descriptor = os.open(root, os.O_RDONLY | os.O_DIRECTORY | os.O_NOFOLLOW)
    try:
        for part in parts[:-1]:
            child = os.open(part, os.O_RDONLY | os.O_DIRECTORY | os.O_NOFOLLOW, dir_fd=descriptor)
            os.close(descriptor)
            descriptor = child
        leaf = os.open(parts[-1], os.O_RDONLY | os.O_NOFOLLOW | os.O_NONBLOCK, dir_fd=descriptor)
        if not stat.S_ISREG(os.fstat(leaf).st_mode):
            os.close(leaf)
            raise CandidateError(f"not a regular candidate file: {path}")
        return os.fdopen(leaf, "rb")
    finally:
        os.close(descriptor)


def create_archive(archive: Path, *, source_root: Path, files: list[dict], release_plan: dict | None,
                   ci_handoff_raw: bytes, release_run: dict, evaluator: dict, policy: dict,
                   required_evidence: set[str], expected_package_ids: set[str]) -> dict:
    """Produce one pack-once transfer ZIP; never produce final publisher evidence.

    ``files`` is the builder's explicit list of {path, asset_name, role}; hashes and
    sizes are computed from private snapshots of those exact regular-file bytes.
    """
    if len(ci_handoff_raw) > MAX_MEMBER:
        raise CandidateError("CI handoff exceeds member bound")
    ci = dh.validate_ci_handoff(_json(ci_handoff_raw, "CI handoff"))
    _ad5(ci)
    validate_release_run(release_run)
    if not isinstance(files, list) or len(files) >= MAX_MEMBERS:
        raise CandidateError("candidate file count exceeds AD-7 member limit")
    with tempfile.TemporaryDirectory(prefix="publication-candidate-build-") as temporary:
        directory = Path(temporary) / "files"
        directory.mkdir()
        rows, seen, total = [], set(), 0
        for item in files:
            _closed(item, {"path", "asset_name", "role"}, "builder file")
            name = _path(item["path"])
            if name in seen or name == DESCRIPTOR:
                raise CandidateError("duplicate or reserved builder file path")
            seen.add(name)
            destination = directory / name
            destination.parent.mkdir(parents=True, exist_ok=True)
            digest, size = hashlib.sha256(), 0
            with _open_regular(source_root, name) as source, destination.open("xb") as target:
                for chunk in iter(lambda: source.read(CHUNK), b""):
                    size += len(chunk)
                    total += len(chunk)
                    if size > MAX_MEMBER or total > MAX_TOTAL:
                        raise CandidateError("builder file bytes exceed AD-7 limits")
                    digest.update(chunk)
                    target.write(chunk)
            rows.append({**item, "sha256": digest.hexdigest(), "size": size})
        descriptor = {"schema": SCHEMA, "release_run": release_run, "candidate": ci["run"]["candidate"],
                      "ci_handoff_sha256": hashlib.sha256(ci_handoff_raw).hexdigest(),
                      "dependency_policy": ci["dependency_policy"], "evaluator": evaluator,
                      "release_plan": release_plan, "files": sorted(rows, key=lambda item: item["path"])}
        validate_descriptor(descriptor, ci_handoff_raw=ci_handoff_raw, release_run=release_run,
                            evaluator=evaluator, policy=policy, required_evidence=required_evidence)
        _verify_package_set(directory, descriptor, expected_package_ids)
        raw = dg.canonical_bytes(descriptor)
        if len(raw) > MAX_MEMBER:
            raise CandidateError("descriptor exceeds AD-7 member limit")
        (directory / DESCRIPTOR).write_bytes(raw)
        # Stage the complete archive before the exclusive destination write.
        staged = Path(temporary) / "transfer.zip"
        with zipfile.ZipFile(staged, "w", compression=zipfile.ZIP_STORED, allowZip64=True) as output:
            for name in sorted([DESCRIPTOR, *seen]):
                info = zipfile.ZipInfo(name, date_time=(1980, 1, 1, 0, 0, 0))
                info.create_system = 3
                info.external_attr = (stat.S_IFREG | 0o600) << 16
                with (directory / name).open("rb") as source, output.open(info, "w") as target:
                    shutil.copyfileobj(source, target, CHUNK)
                if staged.stat().st_size > MAX_ARCHIVE:
                    raise CandidateError("produced raw archive exceeds 512 MiB")
        if staged.stat().st_size > MAX_ARCHIVE:
            raise CandidateError("produced raw archive exceeds 512 MiB")
        digest = hashlib.sha256()
        with staged.open("rb") as source:
            target = archive.open("xb")
            try:
                with target:
                    for chunk in iter(lambda: source.read(CHUNK), b""):
                        target.write(chunk)
                        digest.update(chunk)
            except BaseException:
                archive.unlink()
                raise
        return {"artifact_name": f"publication-candidate-{release_run['run_id']}-{release_run['run_attempt']}",
                "run_id": release_run["run_id"], "run_attempt": release_run["run_attempt"],
                "archive_sha256": digest.hexdigest(), "descriptor_sha256": hashlib.sha256(raw).hexdigest()}


def _load(path: str) -> Any:
    with Path(path).open("rb") as source:
        raw = source.read(MAX_MEMBER + 1)
    if len(raw) > MAX_MEMBER:
        raise CandidateError(f"{path}: JSON input exceeds member bound")
    return _json(raw, path)


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest="command", required=True)
    for command in ("create", "verify"):
        child = commands.add_parser(command)
        child.add_argument("--archive", required=True)
        child.add_argument("--ci-handoff", required=True)
        child.add_argument("--release-run", required=True)
        child.add_argument("--release-evaluator", required=True)
        child.add_argument("--root", required=True, help="Exact-object store root for active-policy/CI live verification")
        child.add_argument("--required-evidence", required=True, help="Owner-controlled JSON array of required FR-24 evidence paths")
        child.add_argument("--output", required=True)
        if command == "create":
            child.add_argument("--source-root", required=True)
            child.add_argument("--files", required=True)
            child.add_argument("--release-plan", required=True, help="JSON release plan or null")
        else:
            child.add_argument("--coordinate", required=True, help="Independently authenticated Actions/handoff artifact projection")
            child.add_argument("--extract-to")
    args = parser.parse_args(argv)
    output = None
    try:
        result_path = Path(args.output)
        archive_path = Path(args.archive)
        destinations = [result_path, archive_path]
        if args.command == "verify" and args.extract_to:
            destinations.append(Path(args.extract_to))
        resolved = [path.resolve() for path in destinations]
        for index, first in enumerate(resolved):
            if any(first == other or first.is_relative_to(other) or other.is_relative_to(first)
                   for other in resolved[index + 1:]):
                raise CandidateError("CLI archive, result and extraction destinations must not overlap")
        # Reserve the result exclusively before archive creation or extraction.
        # A failed reservation cannot strand data that makes a retry impossible.
        output = result_path.open("xb")
        with output:
            with Path(args.ci_handoff).open("rb") as source:
                ci_raw = source.read(MAX_MEMBER + 1)
            if len(ci_raw) > MAX_MEMBER:
                raise CandidateError("CI handoff exceeds member bound")
            ci = dh.validate_ci_handoff(_json(ci_raw, "CI handoff"), root=Path(args.root))
            policy, _, projection = dg.load_policy_at_commit(Path(args.root), ci["dependency_policy"]["commit"])
            if projection != ci["dependency_policy"]:
                raise CandidateError("active policy differs from authenticated CI policy")
            evidence = _load(args.required_evidence)
            if not isinstance(evidence, list) or any(not isinstance(item, str) for item in evidence) or evidence != sorted(set(evidence)):
                raise CandidateError("required-evidence must be an owner-controlled sorted unique path array")
            common = {"ci_handoff_raw": ci_raw, "release_run": _load(args.release_run),
                      "evaluator": _load(args.release_evaluator), "policy": policy,
                      "required_evidence": set(evidence), "expected_package_ids": {item[0] for item in rc.EXPECTED_PACKAGES}}
            if args.command == "create":
                result = create_archive(Path(args.archive), source_root=Path(args.source_root), files=_load(args.files),
                                        release_plan=_load(args.release_plan), **common)
            else:
                result = verify_archive(Path(args.archive), coordinate=_load(args.coordinate),
                                        extract_to=Path(args.extract_to) if args.extract_to else None, **common)
            output.write(dg.canonical_bytes(result))
        print(json.dumps({"ok": True, "schema": SCHEMA, "archive_sha256": result["archive_sha256"]}, sort_keys=True))
        return 0
    except (CandidateError, dg.GraphError, rc.ContractError, OSError, zipfile.BadZipFile, ValueError, TypeError, KeyError, struct.error) as exc:
        if output is not None:
            result_path.unlink(missing_ok=True)
        print(json.dumps({"ok": False, "error": str(exc)}, sort_keys=True), file=sys.stderr)
        return 1


if __name__ == "__main__":
    sys.exit(main())
