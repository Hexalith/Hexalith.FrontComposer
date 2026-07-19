#!/usr/bin/env python3
"""Committed-object dependency-graph engine (GOV-1 / FC-DEP-1).

Collects the bounded depth-1/depth-2 `hexalith.dependency-graph.v1` gitlink graph from
an explicit FrontComposer root commit, computes its AD-5 canonical digest, and evaluates
every Builds-selector edge under its AD-6 semantic profile. This is the single canonical
semantic-policy implementation; C# Governance invokes its machine-readable result rather
than reimplementing catalog policy.

Scope note: this module implements Task 2/3 of GOV-1 (local graph collection + semantic
validation). CI graph diffing, affected-module build gates, and release-manifest binding
(Task 4/5, AD-8/AD-12 evaluator trust, AD-13/AD-14/AD-15) remain blocked pending an
owner-accepted Hexalith.Builds issue 17 / BUILD-REL-1 revision (AD-16) and are not
implemented here.
"""

from __future__ import annotations

import argparse
import fnmatch
import hashlib
import json
import re
import subprocess
import sys
import xml.etree.ElementTree as ET
from pathlib import Path
from typing import Any

__version__ = "1.0.0"

SCHEMA = "hexalith.dependency-graph.v1"
POLICY_SCHEMA = "hexalith.dependency-graph-policy.v1"


class GraphError(Exception):
    """Raised for any fail-closed condition during collection or semantic evaluation."""


# ---------------------------------------------------------------------------
# Git plumbing — argv-only subprocess calls, never shell interpolation.
# `.gitmodules` and nested repository content are untrusted candidate data.
# ---------------------------------------------------------------------------


def _run_git(args: list[str], cwd: Path) -> subprocess.CompletedProcess[bytes]:
    return subprocess.run(["git", *args], cwd=str(cwd), capture_output=True, check=False)


def _git_ok(args: list[str], cwd: Path) -> bytes:
    proc = _run_git(args, cwd)
    if proc.returncode != 0:
        raise GraphError(
            f"git {' '.join(args)} failed in {cwd}: {proc.stderr.decode('utf-8', 'replace').strip()}"
        )
    return proc.stdout


_COMMIT_RE = re.compile(r"^[0-9a-f]{40}$")


def require_commit(value: str, context: str) -> str:
    if not isinstance(value, str) or not _COMMIT_RE.match(value):
        raise GraphError(f"{context}: expected a strict lowercase 40-hex commit, got {value!r}")
    return value


def _tree_entry(local_path: Path, commit: str, path: str) -> tuple[str, str, str] | None:
    """Return (mode, type, sha) for an exact path at commit, or None if absent."""
    out = _git_ok(["ls-tree", commit, "--", path], local_path)
    line = out.decode("utf-8").strip()
    if not line:
        return None
    meta, _, entry_path = line.partition("\t")
    mode, obj_type, sha = meta.split()
    if entry_path != path:
        return None
    return mode, obj_type, sha


def _blob_size(local_path: Path, blob_sha: str) -> int:
    return int(_git_ok(["cat-file", "-s", blob_sha], local_path).decode("ascii").strip())


def read_blob(local_path: Path, commit: str, path: str, max_bytes: int, context: str) -> bytes | None:
    entry = _tree_entry(local_path, commit, path)
    if entry is None:
        return None
    _mode, obj_type, sha = entry
    if obj_type != "blob":
        raise GraphError(f"{context}: {path} at {commit} is a {obj_type}, not a blob")
    size = _blob_size(local_path, sha)
    if size > max_bytes:
        raise GraphError(f"{context}: {path} at {commit} is {size} bytes, exceeds the {max_bytes}-byte ceiling")
    return _git_ok(["cat-file", "blob", sha], local_path)


def list_gitlinks(local_path: Path, commit: str, max_ls_tree_bytes: int) -> dict[str, str]:
    """Every mode-160000 (gitlink) tree entry at commit: path -> commit sha."""
    out = _git_ok(["ls-tree", "-r", "-z", "--full-tree", commit], local_path)
    if len(out) > max_ls_tree_bytes:
        raise GraphError(
            f"ls-tree output for {commit} in {local_path} is {len(out)} bytes, "
            f"exceeds the {max_ls_tree_bytes}-byte ceiling"
        )
    gitlinks: dict[str, str] = {}
    for record in out.split(b"\x00"):
        if not record:
            continue
        meta, _, path_bytes = record.partition(b"\t")
        _mode, obj_type, sha = meta.split()
        if obj_type == b"commit":
            gitlinks[path_bytes.decode("utf-8")] = sha.decode("ascii")
    return gitlinks


_GITMODULES_KEY_RE = re.compile(r"^submodule\.(?P<name>.+)\.(?P<field>path|url)$")


def read_gitmodules(local_path: Path, commit: str, max_bytes: int) -> dict[str, dict[str, str]]:
    """Parse committed `.gitmodules` at commit via `git config --blob`. name -> {path, url}."""
    entry = _tree_entry(local_path, commit, ".gitmodules")
    if entry is None:
        return {}
    _mode, obj_type, sha = entry
    if obj_type != "blob":
        raise GraphError(f".gitmodules at {commit} in {local_path} is a {obj_type}, not a blob")
    size = _blob_size(local_path, sha)
    if size > max_bytes:
        raise GraphError(f".gitmodules at {commit} in {local_path} is {size} bytes, exceeds the {max_bytes}-byte ceiling")
    out = _git_ok(["config", "--blob", f"{commit}:.gitmodules", "--list"], local_path)
    entries: dict[str, dict[str, str]] = {}
    for line in out.decode("utf-8").splitlines():
        if not line:
            continue
        key, _, value = line.partition("=")
        m = _GITMODULES_KEY_RE.match(key)
        if not m:
            continue
        entries.setdefault(m.group("name"), {})[m.group("field")] = value
    return entries


# ---------------------------------------------------------------------------
# AD-3 identity/path normalization — closed-world, no clones, no candidate URLs.
# ---------------------------------------------------------------------------

_HTTPS_RE = re.compile(r"^https://github\.com/(?P<owner>[A-Za-z0-9._-]+)/(?P<repo>[A-Za-z0-9._-]+?)(?:\.git)?/?$")
_SSH_SCP_RE = re.compile(r"^git@github\.com:(?P<owner>[A-Za-z0-9._-]+)/(?P<repo>[A-Za-z0-9._-]+?)(?:\.git)?/?$")
_SSH_URL_RE = re.compile(r"^ssh://git@github\.com/(?P<owner>[A-Za-z0-9._-]+)/(?P<repo>[A-Za-z0-9._-]+?)(?:\.git)?/?$")
_CONTROL_RE = re.compile(r"[\x00-\x1f\x7f]")


def normalize_identity(url: str, context: str) -> str:
    if not isinstance(url, str) or not url:
        raise GraphError(f"{context}: empty repository URL")
    if _CONTROL_RE.search(url):
        raise GraphError(f"{context}: control characters in repository URL {url!r}")
    if "%" in url:
        raise GraphError(f"{context}: percent-escapes are not permitted in repository URL {url!r}")

    match = _HTTPS_RE.match(url) or _SSH_SCP_RE.match(url) or _SSH_URL_RE.match(url)
    if not match:
        raise GraphError(f"{context}: unrecognized or unsafe repository URL {url!r}")

    owner, repo = match.group("owner"), match.group("repo")
    for segment, label in ((owner, "owner"), (repo, "repository")):
        if segment in (".", ".."):
            raise GraphError(f"{context}: unsafe {label} segment in repository URL {url!r}")

    return f"github.com/{owner.lower()}/{repo.lower()}"


def normalize_path(path: str, context: str) -> str:
    if not isinstance(path, str) or not path:
        raise GraphError(f"{context}: empty path")
    if _CONTROL_RE.search(path) or "\\" in path:
        raise GraphError(f"{context}: unsafe path {path!r}")
    if path.startswith("/"):
        raise GraphError(f"{context}: absolute path not permitted: {path!r}")
    for segment in path.split("/"):
        if segment in ("", ".", ".."):
            raise GraphError(f"{context}: unsafe path segment in {path!r}")
    return path


def resolve_local_path(policy: dict[str, Any], identity: str, root_dir: Path) -> Path:
    for entry in policy["trusted_identities"]:
        if entry["identity"] == identity:
            return (root_dir / entry["local_path"]).resolve()
    raise GraphError(f"unknown/untrusted repository identity {identity!r}")


# ---------------------------------------------------------------------------
# AD-4/AD-5 — edge collection, canonical envelope, and digest.
# ---------------------------------------------------------------------------


def canonical_bytes(obj: Any) -> bytes:
    return json.dumps(obj, ensure_ascii=True, allow_nan=False, sort_keys=True, separators=(",", ":")).encode("utf-8")


def canonical_digest(obj: Any) -> str:
    return hashlib.sha256(canonical_bytes(obj)).hexdigest()


def _owner_edges(
    local_path: Path,
    commit: str,
    owner_identity: str,
    depth: int,
    policy: dict[str, Any],
    root_dir: Path,
) -> list[dict[str, Any]]:
    limits = policy["resource_limits"]
    modules = read_gitmodules(local_path, commit, limits["max_gitmodules_blob_bytes"])
    gitlinks = list_gitlinks(local_path, commit, limits["max_ls_tree_bytes_per_owner_commit"])

    path_to_identity: dict[str, str] = {}
    for name, fields in modules.items():
        if "path" not in fields or "url" not in fields:
            raise GraphError(f"{owner_identity}@{commit}: .gitmodules entry {name!r} missing path/url")
        path = normalize_path(fields["path"], f"{owner_identity}@{commit} .gitmodules[{name}]")
        identity = normalize_identity(fields["url"], f"{owner_identity}@{commit} .gitmodules[{name}]")
        # fail closed on any identity the policy does not already trust (AD-3/AD-12)
        resolve_local_path(policy, identity, root_dir)
        path_to_identity[path] = identity

    declared_paths = {fields["path"] for fields in modules.values() if "path" in fields}
    missing = declared_paths - set(gitlinks.keys())
    if missing:
        raise GraphError(f"{owner_identity}@{commit}: declared .gitmodules paths missing a gitlink: {sorted(missing)}")

    edges: list[dict[str, Any]] = []
    for path, target_commit in sorted(gitlinks.items()):
        identity = path_to_identity.get(path)
        if identity is None:
            raise GraphError(f"{owner_identity}@{commit}: gitlink at {path!r} has no matching .gitmodules mapping")
        edges.append(
            {
                "owner_repository": owner_identity,
                "owner_commit": commit,
                "path": path,
                "repository": identity,
                "commit": target_commit,
                "depth": depth,
            }
        )
    return edges


def collect_graph(root_dir: Path, root_identity: str, root_commit: str, policy: dict[str, Any]) -> dict[str, Any]:
    require_commit(root_commit, "root commit")
    limits = policy["resource_limits"]
    builds_identity = policy["builds_identity"]

    edges = _owner_edges(root_dir, root_commit, root_identity, 1, policy, root_dir)
    for depth1_edge in list(edges):
        owner_local = resolve_local_path(policy, depth1_edge["repository"], root_dir)
        edges.extend(
            _owner_edges(owner_local, depth1_edge["commit"], depth1_edge["repository"], 2, policy, root_dir)
        )

    if len(edges) > limits["max_edges"]:
        raise GraphError(f"graph has {len(edges)} edges, exceeds the {limits['max_edges']}-edge ceiling")

    catalog_cache: dict[str, str] = {}
    for edge in edges:
        if edge["repository"] != builds_identity:
            continue
        if edge["commit"] not in catalog_cache:
            builds_local = resolve_local_path(policy, builds_identity, root_dir)
            blob = read_blob(
                builds_local,
                edge["commit"],
                "Props/Directory.Packages.props",
                limits["max_catalog_blob_bytes"],
                f"{builds_identity}@{edge['commit']}",
            )
            if blob is None:
                raise GraphError(f"{builds_identity}@{edge['commit']}: missing Props/Directory.Packages.props")
            catalog_cache[edge["commit"]] = hashlib.sha256(blob).hexdigest()
        edge["catalog_sha256"] = catalog_cache[edge["commit"]]
        # nullable until Hexalith.Builds supplies a marker (BUILD-CAT-1); see AD-6
        edge["catalog_contract_version"] = None

    edges.sort(key=lambda e: (e["depth"], e["owner_repository"], e["owner_commit"], e["path"], e["repository"], e["commit"]))

    envelope: dict[str, Any] = {
        "schema": SCHEMA,
        "root": {"repository": root_identity, "commit": root_commit},
        "edge_count": len(edges),
        "edges": edges,
    }
    envelope["graph_digest"] = canonical_digest(
        {"schema": envelope["schema"], "root": envelope["root"], "edge_count": envelope["edge_count"], "edges": envelope["edges"]}
    )
    return envelope


# ---------------------------------------------------------------------------
# AD-6 — semantic catalog evaluation (MSBuild XML introspection, not evaluation).
# ---------------------------------------------------------------------------


def parse_project_xml(blob: bytes, context: str) -> ET.Element:
    try:
        return ET.fromstring(blob)
    except ET.ParseError as exc:
        raise GraphError(f"{context}: malformed XML ({exc})") from exc


def _simple_match(item_spec: str | None, package_id: str) -> bool:
    if not item_spec:
        return False
    for spec in item_spec.split(";"):
        spec = spec.strip()
        if spec and fnmatch.fnmatch(spec.lower(), package_id.lower()):
            return True
    return False


def find_package_version_ops(root: ET.Element, package_id: str) -> list[ET.Element]:
    ops = []
    for el in root.iter("PackageVersion"):
        if any(_simple_match(el.get(attr), package_id) for attr in ("Include", "Update", "Remove")):
            ops.append(el)
    return ops


def _parent_map(root: ET.Element) -> dict[ET.Element, ET.Element]:
    return {child: parent for parent in root.iter() for child in parent}


def _ancestors(el: ET.Element, parents: dict[ET.Element, ET.Element]) -> list[ET.Element]:
    chain = []
    cur = el
    while cur in parents:
        cur = parents[cur]
        chain.append(cur)
    return chain


def assert_authoritative_package_version(root: ET.Element, package_id: str, expected_version: str, context: str) -> None:
    ops = find_package_version_ops(root, package_id)
    if len(ops) != 1:
        raise GraphError(f"{context}: {package_id} must have exactly one unmasked shared operation (found {len(ops)})")
    el = ops[0]
    include = el.get("Include")
    if not include or include.strip().lower() != package_id.lower():
        raise GraphError(f"{context}: {package_id} must be an authoritative Include item, not Update")
    if el.get("Update") is not None:
        raise GraphError(f"{context}: {package_id} must not use Update in the shared catalog")
    if el.get("Exclude") is not None:
        raise GraphError(f"{context}: {package_id} must not use Exclude in the shared catalog")
    parents = _parent_map(root)
    ancestors = _ancestors(el, parents)
    if el.get("Condition") is not None or any(node.get("Condition") is not None for node in ancestors):
        raise GraphError(f"{context}: {package_id} must be unconditional in the shared catalog")
    if any(node.tag in ("Choose", "When", "Otherwise") for node in ancestors):
        raise GraphError(f"{context}: {package_id} must not be selected through an MSBuild Choose branch")
    version = el.get("Version")
    if version != expected_version:
        raise GraphError(f"{context}: {package_id} expected version {expected_version!r}, found {version!r}")


_UTF8_BOM = b"\xef\xbb\xbf"


def assert_utf8_bom_and_crlf(data: bytes, context: str) -> None:
    if not data.startswith(_UTF8_BOM):
        raise GraphError(f"{context}: must start with a UTF-8 BOM")
    body = data[len(_UTF8_BOM):]
    try:
        body.decode("utf-8", errors="strict")
    except UnicodeDecodeError as exc:
        raise GraphError(f"{context}: invalid UTF-8 after BOM ({exc})") from exc
    for i, byte in enumerate(body):
        if byte == 0x0A and (i == 0 or body[i - 1] != 0x0D):
            raise GraphError(f"{context}: bare LF at byte offset {i + len(_UTF8_BOM)}")
        if byte == 0x0D and (i + 1 >= len(body) or body[i + 1] != 0x0A):
            raise GraphError(f"{context}: bare CR at byte offset {i + len(_UTF8_BOM)}")


def _check_attr(local_path: Path, relative_path: str, attribute: str) -> str:
    out = _git_ok(["check-attr", attribute, "--", relative_path], local_path).decode("utf-8")
    prefix = f"{relative_path}: {attribute}: "
    if not out.startswith(prefix):
        raise GraphError(f"git check-attr produced unexpected output for {relative_path}: {out!r}")
    return out[len(prefix):].strip()


def assert_builds_checkout_format_policy(builds_local: Path, context: str) -> None:
    """Root-only BOM/CRLF + gitattributes format policy, ported as-is from the pre-GOV-1
    test. Deliberately reads the checked-out working tree rather than a committed-object
    blob: eol=crlf only rewrites bytes on checkout, so the raw commit object for a
    catalog can legitimately carry bare LF (a known, separately tracked upstream
    Hexalith.Builds formatting issue) while every local checkout still renders CRLF.
    Dev Notes call this out as remaining "a local format policy unless separately
    generalized" — it stays scoped to the local checkout, not graph provenance.
    """
    catalog_relative = "Props/Directory.Packages.props"
    if _check_attr(builds_local, catalog_relative, "text") != "set":
        raise GraphError(f"{context}: {catalog_relative} must declare text=set")
    if _check_attr(builds_local, catalog_relative, "eol") != "crlf":
        raise GraphError(f"{context}: {catalog_relative} must declare eol=crlf")
    if _check_attr(builds_local, "Directory.Build.props", "eol") != "unspecified":
        raise GraphError(f"{context}: Directory.Build.props eol must remain unspecified (the catalog-only checkout policy must not broaden to unrelated Builds files)")
    catalog_path = builds_local / "Props" / "Directory.Packages.props"
    if not catalog_path.is_file():
        raise GraphError(f"{context}: missing checked-out {catalog_relative}")
    assert_utf8_bom_and_crlf(catalog_path.read_bytes(), context)


def assert_override_not_enabled(root: ET.Element, context: str) -> None:
    for el in root.iter("CentralPackageVersionOverrideEnabled"):
        if (el.text or "").strip().lower() == "true":
            raise GraphError(f"{context}: CentralPackageVersionOverrideEnabled must not be enabled")


def assert_no_minver(root: ET.Element, context: str) -> None:
    for el in root.iter("PackageReference"):
        if _simple_match(el.get("Include"), "MinVer"):
            raise GraphError(f"{context}: release versioning is owned by semantic-release, not MinVer")
    for el in root.iter():
        if el.tag.startswith("MinVer"):
            raise GraphError(f"{context}: must not retain MinVer configuration after semantic-release ownership")


def assert_guarded_imports(root: ET.Element, spec: dict[str, Any], context: str) -> None:
    imports = list(root.iter("Import"))
    expected_projects = spec["import_projects"]
    expected_conditions = spec["import_conditions"]
    if len(imports) != len(expected_projects):
        raise GraphError(f"{context}: must preserve exactly {len(expected_projects)} guarded shared-catalog import paths (found {len(imports)})")
    for idx, el in enumerate(imports):
        if el.get("Project") != expected_projects[idx]:
            raise GraphError(f"{context}: import[{idx}] Project mismatch (found {el.get('Project')!r})")
        if el.get("Condition") != expected_conditions[idx]:
            raise GraphError(f"{context}: import[{idx}] Condition mismatch (found {el.get('Condition')!r})")
    for prop_name, expected_value in spec["required_properties"].items():
        matches = list(root.iter(prop_name))
        if len(matches) != 1:
            raise GraphError(f"{context}: expected exactly one {prop_name} property (found {len(matches)})")
        if (matches[0].text or "") != expected_value:
            raise GraphError(f"{context}: {prop_name} expected {expected_value!r}, found {matches[0].text!r}")


def list_tracked_files(local_path: Path, commit: str, extensions: list[str], max_ls_tree_bytes: int) -> list[str]:
    out = _git_ok(["ls-tree", "-r", "-z", "--full-tree", commit], local_path)
    if len(out) > max_ls_tree_bytes:
        raise GraphError(f"ls-tree output for {commit} in {local_path} exceeds the {max_ls_tree_bytes}-byte ceiling")
    files = []
    for record in out.split(b"\x00"):
        if not record:
            continue
        meta, _, path_bytes = record.partition(b"\t")
        _mode, obj_type, _sha = meta.split()
        if obj_type != b"blob":
            continue
        path = path_bytes.decode("utf-8")
        if any(path.endswith(ext) for ext in extensions):
            files.append(path)
    return sorted(files)


def assert_no_inline_versions(local_path: Path, commit: str, extensions: list[str], limits: dict[str, int], context: str) -> None:
    for rel_path in list_tracked_files(local_path, commit, extensions, limits["max_ls_tree_bytes_per_owner_commit"]):
        blob = read_blob(local_path, commit, rel_path, limits["max_catalog_blob_bytes"], f"{context}:{rel_path}")
        if blob is None:
            continue
        root = parse_project_xml(blob, f"{context}:{rel_path}")
        parents = _parent_map(root)
        for el in root.iter("PackageVersion"):
            parent = parents.get(el)
            if parent is not None and parent.tag == "ItemGroup":
                raise GraphError(f"{context}:{rel_path} must inherit every package version from the pinned Builds catalog")
        for el in root.iter():
            if el.tag in ("PackageReference", "GlobalPackageReference"):
                if el.get("Version") is not None:
                    raise GraphError(f"{context}:{rel_path} must not declare an inline Version")
                if el.get("VersionOverride") is not None:
                    raise GraphError(f"{context}:{rel_path} must not declare VersionOverride")
                for child in el:
                    if child.tag in ("Version", "VersionOverride"):
                        raise GraphError(f"{context}:{rel_path} must not declare inline package-version metadata")


def evaluate_semantics(root_dir: Path, policy: dict[str, Any], envelope: dict[str, Any]) -> dict[str, Any]:
    """Evaluate every Builds-selector edge under its owner's explicit semantic profile (AD-6)."""
    limits = policy["resource_limits"]
    profiles = policy["profiles"]
    semantic_profiles = policy["semantic_profiles"]
    builds_identity = policy["builds_identity"]

    by_owner: dict[str, list[dict[str, Any]]] = {}
    for edge in envelope["edges"]:
        if edge["repository"] == builds_identity:
            by_owner.setdefault(edge["owner_repository"], []).append(edge)

    catalog_cache: dict[str, tuple[ET.Element, bytes]] = {}

    def load_catalog(commit: str) -> tuple[ET.Element, bytes]:
        if commit not in catalog_cache:
            builds_local = resolve_local_path(policy, builds_identity, root_dir)
            blob = read_blob(
                builds_local, commit, "Props/Directory.Packages.props", limits["max_catalog_blob_bytes"], f"{builds_identity}@{commit}"
            )
            if blob is None:
                raise GraphError(f"{builds_identity}@{commit}: missing Props/Directory.Packages.props")
            catalog_cache[commit] = (parse_project_xml(blob, f"{builds_identity}@{commit}"), blob)
        return catalog_cache[commit]

    diagnostics: list[str] = []
    for owner_identity, owner_edges in sorted(by_owner.items()):
        profile_name = semantic_profiles.get(owner_identity)
        if profile_name is None:
            raise GraphError(f"{owner_identity}: no semantic profile mapping in policy (fails closed)")
        profile = profiles.get(profile_name)
        if profile is None:
            raise GraphError(f"{owner_identity}: unknown semantic profile {profile_name!r}")

        owner_local = resolve_local_path(policy, owner_identity, root_dir)
        owner_commit = owner_edges[0]["owner_commit"]
        owner_checks = profile.get("owner_checks", {})

        own_blob = read_blob(owner_local, owner_commit, "Directory.Packages.props", limits["max_catalog_blob_bytes"], f"{owner_identity}@{owner_commit}")
        own_xml = parse_project_xml(own_blob, f"{owner_identity}@{owner_commit} Directory.Packages.props") if own_blob is not None else None

        if owner_checks.get("no_package_version_rows"):
            if own_xml is None:
                raise GraphError(f"{owner_identity}@{owner_commit}: missing Directory.Packages.props")
            if list(own_xml.iter("PackageVersion")):
                raise GraphError(f"{owner_identity}@{owner_commit}: root Directory.Packages.props must be an import shim owning no PackageVersion rows")

        if owner_checks.get("well_formed_project_root"):
            if own_xml is None:
                raise GraphError(f"{owner_identity}@{owner_commit}: missing Directory.Packages.props")
            if own_xml.tag != "Project":
                raise GraphError(f"{owner_identity}@{owner_commit}: Directory.Packages.props must have a Project root")

        if owner_checks.get("override_not_enabled") and own_xml is not None:
            assert_override_not_enabled(own_xml, f"{owner_identity}@{owner_commit}")

        if owner_checks.get("no_minver"):
            build_props_blob = read_blob(owner_local, owner_commit, "Directory.Build.props", limits["max_catalog_blob_bytes"], f"{owner_identity}@{owner_commit}")
            if build_props_blob is not None:
                build_props_xml = parse_project_xml(build_props_blob, f"{owner_identity}@{owner_commit} Directory.Build.props")
                assert_no_minver(build_props_xml, f"{owner_identity}@{owner_commit} Directory.Build.props")

        guarded = owner_checks.get("guarded_imports")
        if guarded:
            if own_xml is None:
                raise GraphError(f"{owner_identity}@{owner_commit}: missing Directory.Packages.props")
            assert_guarded_imports(own_xml, guarded, f"{owner_identity}@{owner_commit} Directory.Packages.props")

        inline = owner_checks.get("no_inline_versions_in_tracked_files")
        if inline:
            assert_no_inline_versions(owner_local, owner_commit, inline["extensions"], limits, f"{owner_identity}@{owner_commit}")

        required_props = profile.get("selected_catalog_required_properties", {})
        required_packages = profile.get("selected_catalog_required_packages", {})
        for edge in owner_edges:
            catalog_xml, catalog_blob = load_catalog(edge["commit"])
            edge_context = f"{owner_identity}@{owner_commit} -> {edge['path']} -> {builds_identity}@{edge['commit']}"

            if owner_checks.get("bom_crlf_on_selected_catalog"):
                builds_local = resolve_local_path(policy, builds_identity, root_dir)
                assert_builds_checkout_format_policy(builds_local, edge_context)

            for prop_name, expected_value in required_props.items():
                matches = list(catalog_xml.iter(prop_name))
                if len(matches) != 1:
                    raise GraphError(f"{edge_context}: expected exactly one {prop_name} property (found {len(matches)})")
                if (matches[0].text or "") != expected_value:
                    raise GraphError(f"{edge_context}: {prop_name} expected {expected_value!r}, found {matches[0].text!r}")

            for package_id, expected_version in required_packages.items():
                assert_authoritative_package_version(catalog_xml, package_id, expected_version, edge_context)
                if owner_checks.get("no_local_override_for_selected_catalog_packages") and own_xml is not None:
                    if find_package_version_ops(own_xml, package_id):
                        raise GraphError(
                            f"{owner_identity}@{owner_commit}: must inherit {package_id} {expected_version} from the shared catalog without local override"
                        )

            diagnostics.append(f"validated {edge_context} under profile {profile_name}")

    return {"selectors_validated": sum(len(v) for v in by_owner.values()), "diagnostics": diagnostics}


# ---------------------------------------------------------------------------
# CLI
# ---------------------------------------------------------------------------


def load_policy(path: Path) -> dict[str, Any]:
    with open(path, "rb") as handle:
        policy = json.loads(handle.read().decode("utf-8"))
    if policy.get("schema") != POLICY_SCHEMA:
        raise GraphError(f"policy schema mismatch: expected {POLICY_SCHEMA!r}, found {policy.get('schema')!r}")
    return policy


def build_arg_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", default=".", help="FrontComposer root working directory")
    parser.add_argument("--policy", default=None, help="Path to dependency-graph-policy.json (default: <root>/eng/dependency-graph-policy.json)")
    sub = parser.add_subparsers(dest="command", required=True)

    graph_cmd = sub.add_parser("graph", help="Collect and emit the canonical v1 graph envelope")
    graph_cmd.add_argument("--commit", required=True)
    graph_cmd.add_argument("--root-identity", default="github.com/hexalith/hexalith.frontcomposer")

    validate_cmd = sub.add_parser("validate", help="Collect the graph and evaluate every selector's semantic profile")
    validate_cmd.add_argument("--commit", required=True)
    validate_cmd.add_argument("--root-identity", default="github.com/hexalith/hexalith.frontcomposer")

    return parser


def main(argv: list[str] | None = None) -> int:
    parser = build_arg_parser()
    args = parser.parse_args(argv)
    root_dir = Path(args.root).resolve()
    policy_path = Path(args.policy).resolve() if args.policy else root_dir / "eng" / "dependency-graph-policy.json"

    try:
        policy = load_policy(policy_path)
        commit = require_commit(args.commit, "--commit")
        envelope = collect_graph(root_dir, args.root_identity, commit, policy)
        if args.command == "graph":
            print(json.dumps({"ok": True, "envelope": envelope}, indent=2, sort_keys=True))
            return 0
        if args.command == "validate":
            semantics = evaluate_semantics(root_dir, policy, envelope)
            print(json.dumps({"ok": True, "envelope": envelope, "semantics": semantics}, indent=2, sort_keys=True))
            return 0
        raise GraphError(f"unknown command {args.command!r}")
    except GraphError as exc:
        print(json.dumps({"ok": False, "error": str(exc)}, indent=2, sort_keys=True))
        return 1


if __name__ == "__main__":
    sys.exit(main())
