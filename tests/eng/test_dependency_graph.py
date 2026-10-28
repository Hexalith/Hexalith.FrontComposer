#!/usr/bin/env python3
"""Synthetic graph, semantic-policy, AD-8 diff, and materialization tests."""

from __future__ import annotations

import copy
import hashlib
import json
import os
import pathlib
import re
import subprocess
import sys
import tempfile
import unittest
import xml.etree.ElementTree as ET
from unittest import mock

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "eng"))

import dependency_graph as dg  # noqa: E402


def _run(args: list[str], cwd: pathlib.Path) -> bytes:
    proc = subprocess.run(["git", *args], cwd=str(cwd), capture_output=True)
    if proc.returncode != 0:
        raise RuntimeError(f"git {args} failed in {cwd}: {proc.stderr.decode('utf-8', 'replace')}")
    return proc.stdout


class TempGitRepo:
    def __init__(self, root: pathlib.Path) -> None:
        self.root = root
        root.mkdir(parents=True, exist_ok=True)
        _run(["init", "--quiet", "-b", "main"], root)
        _run(["config", "user.email", "test@example.com"], root)
        _run(["config", "user.name", "Test"], root)
        self._gitmodules_entries: list[str] = []

    def write_text(self, relative: str, content: str) -> pathlib.Path:
        return self.write_bytes(relative, content.encode("utf-8"))

    def write_bytes(self, relative: str, data: bytes) -> pathlib.Path:
        path = self.root / relative
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(data)
        _run(["add", "--", relative], self.root)
        return path

    def link_gitlink(self, path: str, commit_sha: str) -> None:
        _run(["update-index", "--add", "--cacheinfo", f"160000,{commit_sha},{path}"], self.root)

    def add_submodule(self, name: str, path: str, url: str, commit_sha: str) -> None:
        self._gitmodules_entries.append(f'[submodule "{name}"]\n\tpath = {path}\n\turl = {url}\n')
        self.write_text(".gitmodules", "".join(self._gitmodules_entries))
        self.link_gitlink(path, commit_sha)

    def commit(self, message: str = "commit") -> str:
        _run(["-c", "commit.gpgsign=false", "commit", "--quiet", "-m", message], self.root)
        return self.head()

    def head(self) -> str:
        return _run(["rev-parse", "HEAD"], self.root).decode("ascii").strip()


BASELINE_CATALOG = (
    b"\xef\xbb\xbf<Project>\r\n"
    b"  <ItemGroup>\r\n"
    b'    <PackageVersion Include="Some.Package" Version="1.0.0" />\r\n'
    b"  </ItemGroup>\r\n"
    b"</Project>\r\n"
)

OWNER_SHIM = (
    "<Project>\n"
    "  <PropertyGroup>\n"
    "    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>\n"
    "  </PropertyGroup>\n"
    "</Project>\n"
)


class GraphFixture:
    """Builds a synthetic closed-world graph across temp git repositories."""

    def __init__(self, tmp_path: pathlib.Path) -> None:
        self.tmp_path = tmp_path
        self.repos: dict[str, TempGitRepo] = {}
        self.policy: dict = {
            "schema": dg.POLICY_SCHEMA,
            "builds_identity": self.identity("Builds"),
            "trusted_identities": [],
            "semantic_profiles": {},
            "profiles": {},
            "module_build_registry": {},
            "resource_limits": {
                "max_edges": 4096,
                "max_ls_tree_bytes_per_owner_commit": 67108864,
                "max_gitmodules_blob_bytes": 1048576,
                "max_catalog_blob_bytes": 4194304,
                "max_contract_tree_files": 16384,
                "max_contract_tree_blob_bytes": 16777216,
                "max_contract_tree_total_bytes": 268435456,
                "max_workflow_closure_depth": 16,
                "max_workflow_closure_sources": 256,
                "max_workflow_source_blob_bytes": 1048576,
                "max_workflow_source_total_bytes": 16777216,
            },
            "evaluator_authorizations": {"ci": [], "release": [], "post_release": []},
        }

    def add_repo(self, name: str) -> TempGitRepo:
        repo = TempGitRepo(self.tmp_path / name)
        self.repos[name] = repo
        self.policy["trusted_identities"].append({"identity": self.identity(name), "local_path": str(repo.root)})
        return repo

    @staticmethod
    def identity(name: str) -> str:
        return f"github.com/test/{name.lower()}"

    @staticmethod
    def url(name: str) -> str:
        return f"https://github.com/test/{name}.git"

    def link(self, owner: str, path: str, target: str, target_commit: str) -> None:
        self.repos[owner].add_submodule(target, path, self.url(target), target_commit)

    def register_module(self, name: str, *, evidence_only: bool = False) -> None:
        identity = self.identity(name)
        if evidence_only:
            self.policy["module_build_registry"][identity] = {
                "disposition": "evidence-only",
                "solution": None,
                "builds_contract_source": "none",
                "restore_argv": None,
                "build_argv": None,
            }
            return
        solution = f"{name}.slnx"
        self.policy["module_build_registry"][identity] = {
            "disposition": "build",
            "solution": solution,
            "builds_contract_source": "self" if name == "Builds" else "edge-tree:references/Builds",
            "restore_argv": [
                "dotnet", "restore", solution, "-p:Configuration=Release", "-p:UseNuGetDeps=true",
            ],
            "build_argv": [
                "dotnet", "build", solution, "--configuration", "Release", "--no-restore",
                "-p:UseNuGetDeps=true",
            ],
        }


class CollectionTests(unittest.TestCase):
    def setUp(self) -> None:
        self._tmp = tempfile.TemporaryDirectory()
        self.tmp_path = pathlib.Path(self._tmp.name)
        self.addCleanup(self._tmp.cleanup)

    def test_depth1_depth2_collection_and_digest_determinism(self) -> None:
        fx = GraphFixture(self.tmp_path)
        builds = fx.add_repo("Builds")
        builds.write_bytes("Props/Directory.Packages.props", BASELINE_CATALOG)
        builds_commit = builds.commit()

        mid = fx.add_repo("Mid")
        mid.write_text("Directory.Packages.props", OWNER_SHIM)
        fx.link("Mid", "references/Builds", "Builds", builds_commit)
        mid_commit = mid.commit()

        root = fx.add_repo("Root")
        root.write_text("Directory.Packages.props", OWNER_SHIM)
        fx.link("Root", "references/Mid", "Mid", mid_commit)
        root_commit = root.commit()

        envelope1 = dg.collect_graph(root.root, fx.identity("Root"), root_commit, fx.policy)
        envelope2 = dg.collect_graph(root.root, fx.identity("Root"), root_commit, fx.policy)
        self.assertEqual(envelope1["graph_digest"], envelope2["graph_digest"])
        self.assertEqual(envelope1["edge_count"], 2)
        depth1 = [e for e in envelope1["edges"] if e["depth"] == 1]
        depth2 = [e for e in envelope1["edges"] if e["depth"] == 2]
        self.assertEqual(len(depth1), 1)
        self.assertEqual(depth1[0]["repository"], fx.identity("Mid"))
        self.assertEqual(len(depth2), 1)
        self.assertEqual(depth2[0]["repository"], fx.identity("Builds"))
        self.assertEqual(depth2[0]["catalog_sha256"], hashlib.sha256(BASELINE_CATALOG).hexdigest())

    def test_approved_depth2_targets_need_no_root_gitlinks_or_local_stores(self) -> None:
        fx = GraphFixture(self.tmp_path)
        target_commits = {}
        for name in ("Platform", "McpCli"):
            target = fx.add_repo(name)
            target.add_submodule(
                "Untrusted", "references/Untrusted",
                "https://github.com/other/untrusted.git", "1" * 40,
            )
            target_commits[name] = target.commit()
            entry = next(
                row for row in fx.policy["trusted_identities"]
                if row["identity"] == fx.identity(name)
            )
            entry["local_path"] = f"references/{name}"

        mid = fx.add_repo("Mid")
        for name, commit in target_commits.items():
            fx.link("Mid", f"references/{name}", name, commit)
        mid_commit = mid.commit()
        root = fx.add_repo("Root")
        fx.link("Root", "references/Mid", "Mid", mid_commit)
        root_commit = root.commit()

        envelope = dg.collect_graph(root.root, fx.identity("Root"), root_commit, fx.policy)
        self.assertEqual(envelope["edge_count"], 3)
        self.assertEqual(
            {(edge["repository"], edge["commit"]) for edge in envelope["edges"] if edge["depth"] == 2},
            {(fx.identity(name), commit) for name, commit in target_commits.items()},
        )
        self.assertEqual(
            [edge["repository"] for edge in envelope["edges"] if edge["depth"] == 1],
            [fx.identity("Mid")],
        )
        for name in target_commits:
            self.assertFalse((root.root / f"references/{name}").exists())
            with self.subTest(unapproved=name):
                policy = copy.deepcopy(fx.policy)
                policy["trusted_identities"] = [
                    row for row in policy["trusted_identities"]
                    if row["identity"] != fx.identity(name)
                ]
                with self.assertRaisesRegex(dg.GraphError, "unknown/untrusted repository identity"):
                    dg.collect_graph(root.root, fx.identity("Root"), root_commit, policy)

    def test_depth2_boundary_excludes_depth3(self) -> None:
        fx = GraphFixture(self.tmp_path)
        deepest = fx.add_repo("Deepest")
        deepest.write_text("marker.txt", "x")
        deepest_commit = deepest.commit()

        builds = fx.add_repo("Builds")
        builds.write_bytes("Props/Directory.Packages.props", BASELINE_CATALOG)
        # Builds' own gitlink to Deepest would only surface as depth 3 (Builds' own edges are
        # never collected, since it is itself a depth-2 node here); it must be excluded.
        fx.link("Builds", "references/Deepest", "Deepest", deepest_commit)
        builds_commit = builds.commit()

        mid = fx.add_repo("Mid")
        mid.write_text("Directory.Packages.props", OWNER_SHIM)
        fx.link("Mid", "references/Builds", "Builds", builds_commit)
        mid_commit = mid.commit()

        root = fx.add_repo("Root")
        root.write_text("Directory.Packages.props", OWNER_SHIM)
        fx.link("Root", "references/Mid", "Mid", mid_commit)
        root_commit = root.commit()

        envelope = dg.collect_graph(root.root, fx.identity("Root"), root_commit, fx.policy)
        self.assertEqual(envelope["edge_count"], 2)
        self.assertEqual({e["depth"] for e in envelope["edges"]}, {1, 2})
        self.assertFalse(any(e["owner_repository"] == fx.identity("Builds") for e in envelope["edges"]))

    def test_self_back_reference_edge_is_recorded(self) -> None:
        fx = GraphFixture(self.tmp_path)
        root = fx.add_repo("Root")
        root.write_text("Directory.Packages.props", OWNER_SHIM)
        root_commit_stub = root.commit("stub")

        mid = fx.add_repo("Mid")
        mid.write_text("Directory.Packages.props", OWNER_SHIM)
        fx.link("Mid", "references/Root", "Root", root_commit_stub)  # back edge to root
        mid_commit = mid.commit()

        fx.link("Root", "references/Mid", "Mid", mid_commit)
        root_commit = root.commit("add mid")

        envelope = dg.collect_graph(root.root, fx.identity("Root"), root_commit, fx.policy)
        depth2 = [e for e in envelope["edges"] if e["depth"] == 2]
        self.assertEqual(len(depth2), 1)
        self.assertEqual(depth2[0]["repository"], fx.identity("Root"))

    def test_multiple_owners_selecting_same_builds_commit(self) -> None:
        fx = GraphFixture(self.tmp_path)
        builds = fx.add_repo("Builds")
        builds.write_bytes("Props/Directory.Packages.props", BASELINE_CATALOG)
        builds_commit = builds.commit()

        for name in ("Alpha", "Beta"):
            owner = fx.add_repo(name)
            owner.write_text("Directory.Packages.props", OWNER_SHIM)
            fx.link(name, "references/Builds", "Builds", builds_commit)
            owner.commit()

        root = fx.add_repo("Root")
        root.write_text("Directory.Packages.props", OWNER_SHIM)
        fx.link("Root", "references/Alpha", "Alpha", fx.repos["Alpha"].head())
        fx.link("Root", "references/Beta", "Beta", fx.repos["Beta"].head())
        root_commit = root.commit()

        envelope = dg.collect_graph(root.root, fx.identity("Root"), root_commit, fx.policy)
        builds_edges = [e for e in envelope["edges"] if e["repository"] == fx.identity("Builds")]
        self.assertEqual(len(builds_edges), 2)
        self.assertEqual(builds_edges[0]["catalog_sha256"], builds_edges[1]["catalog_sha256"])
        self.assertEqual(builds_edges[0]["catalog_sha256"], hashlib.sha256(BASELINE_CATALOG).hexdigest())
        self.assertIsNone(builds_edges[0]["catalog_contract_version"])

    def test_deterministic_edge_ordering(self) -> None:
        fx = GraphFixture(self.tmp_path)
        root = fx.add_repo("Root")
        root.write_text("Directory.Packages.props", OWNER_SHIM)
        for name in ("Zeta", "Alpha", "Mu"):
            child = fx.add_repo(name)
            child.write_text("marker.txt", name)
            child_commit = child.commit()
            fx.link("Root", f"references/{name}", name, child_commit)
        root_commit = root.commit()

        envelope = dg.collect_graph(root.root, fx.identity("Root"), root_commit, fx.policy)
        paths = [e["path"] for e in envelope["edges"]]
        self.assertEqual(paths, sorted(paths))

    def test_missing_gitmodules_mapping_fails_closed(self) -> None:
        fx = GraphFixture(self.tmp_path)
        root = fx.add_repo("Root")
        root.write_text("Directory.Packages.props", OWNER_SHIM)
        # a gitlink with no corresponding .gitmodules entry
        root.link_gitlink("references/Ghost", "f" * 40)
        root_commit = root.commit()

        with self.assertRaises(dg.GraphError):
            dg.collect_graph(root.root, fx.identity("Root"), root_commit, fx.policy)

    def test_unknown_identity_fails_closed(self) -> None:
        fx = GraphFixture(self.tmp_path)
        root = fx.add_repo("Root")
        root.write_text("Directory.Packages.props", OWNER_SHIM)
        root.add_submodule("Untrusted", "references/Untrusted", "https://github.com/other/untrusted.git", "1" * 40)
        root_commit = root.commit()

        with self.assertRaises(dg.GraphError):
            dg.collect_graph(root.root, fx.identity("Root"), root_commit, fx.policy)

    def test_missing_catalog_fails_closed(self) -> None:
        fx = GraphFixture(self.tmp_path)
        builds = fx.add_repo("Builds")
        builds.write_text("README.md", "no catalog here")
        builds_commit = builds.commit()

        root = fx.add_repo("Root")
        root.write_text("Directory.Packages.props", OWNER_SHIM)
        fx.link("Root", "references/Builds", "Builds", builds_commit)
        root_commit = root.commit()

        with self.assertRaises(dg.GraphError):
            dg.collect_graph(root.root, fx.identity("Root"), root_commit, fx.policy)

    def test_malformed_gitmodules_url_fails_closed(self) -> None:
        fx = GraphFixture(self.tmp_path)
        root = fx.add_repo("Root")
        root.write_text("Directory.Packages.props", OWNER_SHIM)
        bad_urls = [
            "https://user:pass@github.com/test/evil.git",  # userinfo
            "https://github.com:8443/test/evil.git",  # port
            "https://github.com/test/evil?x=1",  # query
            "https://not-github.com/test/evil.git",  # wrong host
            "https://github.com/test/%2e%2e",  # percent-escape
            "not-a-url",
        ]
        for index, url in enumerate(bad_urls):
            with self.subTest(url=url):
                with self.assertRaises(dg.GraphError):
                    dg.normalize_identity(url, f"case[{index}]")

    def test_unsafe_path_rejected(self) -> None:
        for path in ("/absolute", "a/../b", "a/./b", "a\\b", "", "a//b".replace("//", "/") + "/.."):
            with self.subTest(path=path):
                with self.assertRaises(dg.GraphError):
                    dg.normalize_path(path, "case")

    def test_edge_count_ceiling_fails_closed(self) -> None:
        fx = GraphFixture(self.tmp_path)
        fx.policy["resource_limits"]["max_edges"] = 0
        root = fx.add_repo("Root")
        root.write_text("Directory.Packages.props", OWNER_SHIM)
        child = fx.add_repo("Child")
        child.write_text("marker.txt", "x")
        child_commit = child.commit()
        fx.link("Root", "references/Child", "Child", child_commit)
        root_commit = root.commit()

        with self.assertRaises(dg.GraphError):
            dg.collect_graph(root.root, fx.identity("Root"), root_commit, fx.policy)

    def test_catalog_blob_ceiling_fails_closed(self) -> None:
        fx = GraphFixture(self.tmp_path)
        fx.policy["resource_limits"]["max_catalog_blob_bytes"] = 4
        builds = fx.add_repo("Builds")
        builds.write_bytes("Props/Directory.Packages.props", BASELINE_CATALOG)
        builds_commit = builds.commit()

        root = fx.add_repo("Root")
        root.write_text("Directory.Packages.props", OWNER_SHIM)
        fx.link("Root", "references/Builds", "Builds", builds_commit)
        root_commit = root.commit()

        with self.assertRaises(dg.GraphError):
            dg.collect_graph(root.root, fx.identity("Root"), root_commit, fx.policy)

    def test_gitmodules_blob_ceiling_fails_closed(self) -> None:
        fx = GraphFixture(self.tmp_path)
        fx.policy["resource_limits"]["max_gitmodules_blob_bytes"] = 4
        builds = fx.add_repo("Builds")
        builds.write_bytes("Props/Directory.Packages.props", BASELINE_CATALOG)
        builds_commit = builds.commit()

        root = fx.add_repo("Root")
        root.write_text("Directory.Packages.props", OWNER_SHIM)
        fx.link("Root", "references/Builds", "Builds", builds_commit)
        root_commit = root.commit()

        with self.assertRaises(dg.GraphError):
            dg.collect_graph(root.root, fx.identity("Root"), root_commit, fx.policy)

    def test_ls_tree_ceiling_fails_closed(self) -> None:
        fx = GraphFixture(self.tmp_path)
        fx.policy["resource_limits"]["max_ls_tree_bytes_per_owner_commit"] = 1
        root = fx.add_repo("Root")
        root.write_text("Directory.Packages.props", OWNER_SHIM)
        root_commit = root.commit()

        with self.assertRaises(dg.GraphError):
            dg.collect_graph(root.root, fx.identity("Root"), root_commit, fx.policy)


BASELINE_PROFILE = {
    "owner_checks": {"well_formed_project_root": True, "override_not_enabled": True},
    "selected_catalog_required_properties": {},
    "selected_catalog_required_packages": {},
}

REQUIRED_PACKAGE_PROFILE = {
    "owner_checks": {"no_local_override_for_selected_catalog_packages": True},
    "selected_catalog_required_properties": {},
    "selected_catalog_required_packages": {"Some.Package": "1.0.0"},
}

REQUIRED_PACKAGE_NAMES_PROFILE = {
    "owner_checks": {"no_local_override_for_selected_catalog_packages": True},
    "selected_catalog_required_properties": {},
    "selected_catalog_required_package_names": ["Some.Package"],
    "selected_catalog_required_packages": {},
}

REQUIRED_PROPERTY_PROFILE = {
    "owner_checks": {},
    "selected_catalog_required_properties": {"SomeVersion": "1.0.0"},
    "selected_catalog_required_packages": {},
}


class SemanticEvaluationTests(unittest.TestCase):
    def setUp(self) -> None:
        self._tmp = tempfile.TemporaryDirectory()
        self.tmp_path = pathlib.Path(self._tmp.name)
        self.addCleanup(self._tmp.cleanup)

    def _catalog_at(self, version: str) -> bytes:
        return (
            b"\xef\xbb\xbf<Project>\r\n"
            b"  <ItemGroup>\r\n"
            b'    <PackageVersion Include="Some.Package" Version="' + version.encode("ascii") + b'" />\r\n'
            b"  </ItemGroup>\r\n"
            b"</Project>\r\n"
        )

    def _catalog_with_properties(self, values: list[str]) -> bytes:
        properties = b"".join(
            b"    <SomeVersion>" + value.encode("ascii") + b"</SomeVersion>\r\n"
            for value in values
        )
        return (
            b"\xef\xbb\xbf<Project>\r\n  <PropertyGroup>\r\n"
            + properties
            + b"  </PropertyGroup>\r\n</Project>\r\n"
        )

    def _catalog_with_property_body(self, body: bytes) -> bytes:
        """Build a catalog whose <Project> children are supplied verbatim.

        Lets a case express the conditional, ancestor-conditional, and Choose shapes the
        real shared catalog can take, which _catalog_with_properties cannot.
        """
        return b"\xef\xbb\xbf<Project>\r\n" + body + b"</Project>\r\n"

    def _evaluate_required_property_catalog(self, catalog: bytes) -> dict:
        fx = GraphFixture(self.tmp_path)
        root = fx.add_repo("Root")
        root.write_text("Directory.Packages.props", OWNER_SHIM)
        _owner, owner_commit = self._build(
            fx,
            "Owner",
            catalog,
            "required-property-profile",
            REQUIRED_PROPERTY_PROFILE,
        )
        fx.link("Root", "references/Owner", "Owner", owner_commit)
        root_commit = root.commit()
        envelope = dg.collect_graph(root.root, fx.identity("Root"), root_commit, fx.policy)
        return dg.evaluate_semantics(root.root, fx.policy, envelope)

    def _evaluate_required_property(self, values: list[str]) -> dict:
        fx = GraphFixture(self.tmp_path)
        root = fx.add_repo("Root")
        root.write_text("Directory.Packages.props", OWNER_SHIM)
        _owner, owner_commit = self._build(
            fx,
            "Owner",
            self._catalog_with_properties(values),
            "required-property-profile",
            REQUIRED_PROPERTY_PROFILE,
        )
        fx.link("Root", "references/Owner", "Owner", owner_commit)
        root_commit = root.commit()
        envelope = dg.collect_graph(root.root, fx.identity("Root"), root_commit, fx.policy)
        return dg.evaluate_semantics(root.root, fx.policy, envelope)

    def _build(self, fx: GraphFixture, owner_name: str, catalog_bytes: bytes, profile_name: str, profile: dict) -> tuple[TempGitRepo, str]:
        # one shared Builds repository per fixture (matching policy["builds_identity"] /
        # its single local_path); each call commits a new catalog state, mirroring how
        # different owners in reality pin different commits of the SAME Builds repo.
        builds = fx.repos.get("Builds") or fx.add_repo("Builds")
        builds.write_bytes("Props/Directory.Packages.props", catalog_bytes)
        builds_commit = builds.commit(f"catalog for {owner_name}")

        owner = fx.add_repo(owner_name)
        owner.write_text("Directory.Packages.props", OWNER_SHIM)
        fx.link(owner_name, "references/Builds", "Builds", builds_commit)
        owner_commit = owner.commit()

        fx.policy["semantic_profiles"][fx.identity(owner_name)] = profile_name
        fx.policy["profiles"][profile_name] = profile
        return owner, owner_commit

    def test_canonical_source_digest_rejects_changed_or_missing_platform_helper(self) -> None:
        for content in ("approved canonical source\n", "changed source\n", None):
            with self.subTest(content=content), tempfile.TemporaryDirectory() as temp:
                fx = GraphFixture(pathlib.Path(temp))
                root = fx.add_repo("Root")
                root.write_text("Directory.Packages.props", OWNER_SHIM)
                profile = copy.deepcopy(BASELINE_PROFILE)
                profile["owner_checks"]["canonical_source_files"] = {"DaprSelfHostedMtls.cs": hashlib.sha256(b"approved canonical source\n").hexdigest()}
                owner, _ = self._build(fx, "Owner", self._catalog_at("1.0.0"), "canonical-source-profile", profile)
                if content is not None:
                    owner.write_text("DaprSelfHostedMtls.cs", content)
                    owner.commit()
                fx.link("Root", "references/Owner", "Owner", owner.head())
                commit = root.commit()
                envelope = dg.collect_graph(root.root, fx.identity("Root"), commit, fx.policy)
                if content == "approved canonical source\n":
                    self.assertEqual(dg.evaluate_semantics(root.root, fx.policy, envelope)["selectors_validated"], 1)
                else:
                    with self.assertRaisesRegex(dg.GraphError, "canonical source.*missing or differs"):
                        dg.evaluate_semantics(root.root, fx.policy, envelope)

    def test_compatible_pointer_advance_passes_under_same_profile(self) -> None:
        fx = GraphFixture(self.tmp_path)
        root = fx.add_repo("Root")
        root.write_text("Directory.Packages.props", OWNER_SHIM)

        ownerA, commitA = self._build(fx, "OwnerA", self._catalog_at("1.0.0"), "shared-profile", BASELINE_PROFILE)
        ownerB, commitB = self._build(fx, "OwnerB", self._catalog_at("2.0.0"), "shared-profile", BASELINE_PROFILE)
        self.assertNotEqual(commitA, commitB)

        fx.link("Root", "references/OwnerA", "OwnerA", commitA)
        fx.link("Root", "references/OwnerB", "OwnerB", commitB)
        root_commit = root.commit()

        envelope = dg.collect_graph(root.root, fx.identity("Root"), root_commit, fx.policy)
        semantics = dg.evaluate_semantics(root.root, fx.policy, envelope)
        self.assertEqual(semantics["selectors_validated"], 2)

    def test_semantic_mismatch_fails_with_actionable_diagnostic(self) -> None:
        fx = GraphFixture(self.tmp_path)
        root = fx.add_repo("Root")
        root.write_text("Directory.Packages.props", OWNER_SHIM)
        wrong_catalog = self._catalog_at("9.9.9")
        _owner, owner_commit = self._build(fx, "Owner", wrong_catalog, "required-profile", REQUIRED_PACKAGE_PROFILE)
        fx.link("Root", "references/Owner", "Owner", owner_commit)
        root_commit = root.commit()

        envelope = dg.collect_graph(root.root, fx.identity("Root"), root_commit, fx.policy)
        with self.assertRaises(dg.GraphError) as ctx:
            dg.evaluate_semantics(root.root, fx.policy, envelope)
        self.assertIn("Some.Package", str(ctx.exception))

    def test_required_property_match_passes(self) -> None:
        semantics = self._evaluate_required_property(["1.0.0"])
        self.assertEqual(semantics["selectors_validated"], 1)

    def test_required_property_missing_fails_with_actionable_diagnostic(self) -> None:
        with self.assertRaises(dg.GraphError) as ctx:
            self._evaluate_required_property([])
        message = str(ctx.exception)
        self.assertIn("references/Builds", message)
        self.assertIn("SomeVersion expected exactly one value '1.0.0'", message)
        self.assertIn("found 0 values <missing>", message)

    def test_required_property_duplicate_fails_with_actionable_diagnostic(self) -> None:
        with self.assertRaises(dg.GraphError) as ctx:
            self._evaluate_required_property(["1.0.0", "2.0.0"])
        message = str(ctx.exception)
        self.assertIn("references/Builds", message)
        self.assertIn("SomeVersion expected exactly one value '1.0.0'", message)
        self.assertIn("found 2 values ['1.0.0', '2.0.0']", message)

    def test_required_property_mismatch_fails_with_actionable_diagnostic(self) -> None:
        with self.assertRaises(dg.GraphError) as ctx:
            self._evaluate_required_property(["2.0.0"])
        message = str(ctx.exception)
        self.assertIn("references/Builds", message)
        self.assertIn("SomeVersion expected '1.0.0', found '2.0.0'", message)

    # The real shared catalog declares every Hexalith version property as
    # Condition="'$(Name)' == ''". That canonical self-default form must pass; any other
    # condition, a conditional ancestor, or a Choose branch must be rejected, because the
    # effective value would then depend on state this validator cannot observe.
    def test_required_property_canonical_self_default_condition_passes(self) -> None:
        catalog = self._catalog_with_property_body(
            b"  <PropertyGroup>\r\n"
            b"    <SomeVersion Condition=\"'$(SomeVersion)' == ''\">1.0.0</SomeVersion>\r\n"
            b"  </PropertyGroup>\r\n"
        )
        semantics = self._evaluate_required_property_catalog(catalog)
        self.assertEqual(semantics["selectors_validated"], 1)

    def test_required_property_foreign_condition_is_rejected(self) -> None:
        catalog = self._catalog_with_property_body(
            b"  <PropertyGroup>\r\n"
            b"    <SomeVersion Condition=\"'$(NeverSet)' == 'yes'\">1.0.0</SomeVersion>\r\n"
            b"  </PropertyGroup>\r\n"
        )
        with self.assertRaises(dg.GraphError) as ctx:
            self._evaluate_required_property_catalog(catalog)
        self.assertIn("canonical self-default", str(ctx.exception))

    def test_required_property_condition_naming_another_property_is_rejected(self) -> None:
        catalog = self._catalog_with_property_body(
            b"  <PropertyGroup>\r\n"
            b"    <SomeVersion Condition=\"'$(OtherVersion)' == ''\">1.0.0</SomeVersion>\r\n"
            b"  </PropertyGroup>\r\n"
        )
        with self.assertRaises(dg.GraphError) as ctx:
            self._evaluate_required_property_catalog(catalog)
        self.assertIn("canonical self-default", str(ctx.exception))

    def test_required_property_under_conditional_group_is_rejected(self) -> None:
        catalog = self._catalog_with_property_body(
            b"  <PropertyGroup Condition=\"'$(NeverSet)' == 'yes'\">\r\n"
            b"    <SomeVersion>1.0.0</SomeVersion>\r\n"
            b"  </PropertyGroup>\r\n"
        )
        with self.assertRaises(dg.GraphError) as ctx:
            self._evaluate_required_property_catalog(catalog)
        self.assertIn("conditional group", str(ctx.exception))

    def test_required_property_selected_through_choose_is_rejected(self) -> None:
        catalog = self._catalog_with_property_body(
            b"  <Choose>\r\n"
            b"    <When Condition=\"'$(NeverSet)' == 'yes'\">\r\n"
            b"      <PropertyGroup>\r\n"
            b"        <SomeVersion>1.0.0</SomeVersion>\r\n"
            b"      </PropertyGroup>\r\n"
            b"    </When>\r\n"
            b"  </Choose>\r\n"
        )
        with self.assertRaises(dg.GraphError) as ctx:
            self._evaluate_required_property_catalog(catalog)
        self.assertIn("Choose branch", str(ctx.exception))

    def test_required_property_empty_element_reports_empty_string_not_none(self) -> None:
        catalog = self._catalog_with_property_body(
            b"  <PropertyGroup>\r\n    <SomeVersion />\r\n  </PropertyGroup>\r\n"
        )
        with self.assertRaises(dg.GraphError) as ctx:
            self._evaluate_required_property_catalog(catalog)
        message = str(ctx.exception)
        self.assertIn("SomeVersion expected '1.0.0', found ''", message)
        self.assertNotIn("None", message)

    def test_no_semantic_profile_mapping_fails_closed(self) -> None:
        fx = GraphFixture(self.tmp_path)
        root = fx.add_repo("Root")
        root.write_text("Directory.Packages.props", OWNER_SHIM)
        _owner, owner_commit = self._build(fx, "Owner", self._catalog_at("1.0.0"), "unused-profile", BASELINE_PROFILE)
        fx.link("Root", "references/Owner", "Owner", owner_commit)
        root_commit = root.commit()
        del fx.policy["semantic_profiles"][fx.identity("Owner")]

        envelope = dg.collect_graph(root.root, fx.identity("Root"), root_commit, fx.policy)
        with self.assertRaises(dg.GraphError):
            dg.evaluate_semantics(root.root, fx.policy, envelope)

    def test_root_import_shim_violation_fails(self) -> None:
        fx = GraphFixture(self.tmp_path)
        root = fx.add_repo("Root")
        root.write_text(
            "Directory.Packages.props",
            '<Project><ItemGroup><PackageVersion Include="Own.Package" Version="1.0.0" /></ItemGroup></Project>',
        )
        builds = fx.add_repo("Builds")
        builds.write_bytes("Props/Directory.Packages.props", self._catalog_at("1.0.0"))
        builds_commit = builds.commit()
        fx.link("Root", "references/Builds", "Builds", builds_commit)
        root_commit = root.commit()

        profile = copy.deepcopy(BASELINE_PROFILE)
        profile["owner_checks"]["no_package_version_rows"] = True
        fx.policy["semantic_profiles"][fx.identity("Root")] = "root-shim-profile"
        fx.policy["profiles"]["root-shim-profile"] = profile

        envelope = dg.collect_graph(root.root, fx.identity("Root"), root_commit, fx.policy)
        with self.assertRaises(dg.GraphError) as ctx:
            dg.evaluate_semantics(root.root, fx.policy, envelope)
        self.assertIn("import shim", str(ctx.exception))

    def test_sibling_presence_required_package_version_may_move(self) -> None:
        fx = GraphFixture(self.tmp_path)
        root = fx.add_repo("Root")
        root.write_text("Directory.Packages.props", OWNER_SHIM)
        _owner, owner_commit = self._build(
            fx,
            "Owner",
            self._catalog_at("2.0.11"),
            "presence-profile",
            REQUIRED_PACKAGE_NAMES_PROFILE,
        )
        fx.link("Root", "references/Owner", "Owner", owner_commit)
        root_commit = root.commit()

        envelope = dg.collect_graph(root.root, fx.identity("Root"), root_commit, fx.policy)
        semantics = dg.evaluate_semantics(root.root, fx.policy, envelope)
        self.assertEqual(semantics["selectors_validated"], 1)

    def test_sibling_presence_required_package_missing_fails_with_coordinates(self) -> None:
        fx = GraphFixture(self.tmp_path)
        root = fx.add_repo("Root")
        root.write_text("Directory.Packages.props", OWNER_SHIM)
        empty_catalog = b"\xef\xbb\xbf<Project>\r\n  <ItemGroup>\r\n  </ItemGroup>\r\n</Project>\r\n"
        _owner, owner_commit = self._build(
            fx,
            "Owner",
            empty_catalog,
            "presence-profile",
            REQUIRED_PACKAGE_NAMES_PROFILE,
        )
        fx.link("Root", "references/Owner", "Owner", owner_commit)
        root_commit = root.commit()

        envelope = dg.collect_graph(root.root, fx.identity("Root"), root_commit, fx.policy)
        with self.assertRaises(dg.GraphError) as ctx:
            dg.evaluate_semantics(root.root, fx.policy, envelope)
        message = str(ctx.exception)
        self.assertIn("github.com/test/owner@", message)
        self.assertIn("references/Builds", message)
        self.assertIn("github.com/test/builds@", message)
        self.assertIn("Some.Package", message)
        self.assertIn("exactly one unmasked shared operation", message)

    def test_sibling_presence_required_package_nonliteral_version_fails_with_coordinates(self) -> None:
        rows = {
            "missing": b'    <PackageVersion Include="Some.Package" />\r\n',
            "empty": b'    <PackageVersion Include="Some.Package" Version="" />\r\n',
            "non-literal": b'    <PackageVersion Include="Some.Package" Version="$(SomeVersion)" />\r\n',
        }
        for name, row in rows.items():
            with self.subTest(name=name):
                catalog = (
                    b"\xef\xbb\xbf<Project>\r\n"
                    b"  <ItemGroup>\r\n"
                    + row
                    + b"  </ItemGroup>\r\n"
                    b"</Project>\r\n"
                )
                fx = GraphFixture(self.tmp_path / name)
                root = fx.add_repo("Root")
                root.write_text("Directory.Packages.props", OWNER_SHIM)
                _owner, owner_commit = self._build(
                    fx,
                    "Owner",
                    catalog,
                    "presence-profile",
                    REQUIRED_PACKAGE_NAMES_PROFILE,
                )
                fx.link("Root", "references/Owner", "Owner", owner_commit)
                root_commit = root.commit()

                envelope = dg.collect_graph(root.root, fx.identity("Root"), root_commit, fx.policy)
                with self.assertRaises(dg.GraphError) as ctx:
                    dg.evaluate_semantics(root.root, fx.policy, envelope)
                message = str(ctx.exception)
                self.assertIn("github.com/test/owner@", message)
                self.assertIn("references/Builds", message)
                self.assertIn("github.com/test/builds@", message)
                self.assertIn("Some.Package", message)
                self.assertIn("literal NuGet version", message)

    def test_sibling_presence_required_package_local_override_fails_with_coordinates(self) -> None:
        fx = GraphFixture(self.tmp_path)
        root = fx.add_repo("Root")
        root.write_text("Directory.Packages.props", OWNER_SHIM)
        builds = fx.add_repo("Builds")
        builds.write_bytes("Props/Directory.Packages.props", self._catalog_at("2.0.11"))
        builds_commit = builds.commit()

        owner = fx.add_repo("Owner")
        owner.write_text(
            "Directory.Packages.props",
            '<Project><ItemGroup><PackageVersion Update="Some.Package" Version="2.0.11" /></ItemGroup></Project>',
        )
        fx.link("Owner", "references/Builds", "Builds", builds_commit)
        owner_commit = owner.commit()

        fx.policy["semantic_profiles"][fx.identity("Owner")] = "presence-profile"
        fx.policy["profiles"]["presence-profile"] = REQUIRED_PACKAGE_NAMES_PROFILE
        fx.link("Root", "references/Owner", "Owner", owner_commit)
        root_commit = root.commit()

        envelope = dg.collect_graph(root.root, fx.identity("Root"), root_commit, fx.policy)
        with self.assertRaises(dg.GraphError) as ctx:
            dg.evaluate_semantics(root.root, fx.policy, envelope)
        message = str(ctx.exception)
        self.assertIn("github.com/test/owner@", message)
        self.assertIn("references/Builds", message)
        self.assertIn("github.com/test/builds@", message)
        self.assertIn("Some.Package", message)
        self.assertIn("without local override", message)

    def test_no_local_override_violation_fails(self) -> None:
        fx = GraphFixture(self.tmp_path)
        root = fx.add_repo("Root")
        root.write_text("Directory.Packages.props", OWNER_SHIM)
        builds = fx.add_repo("Builds")
        builds.write_bytes("Props/Directory.Packages.props", self._catalog_at("1.0.0"))
        builds_commit = builds.commit()

        owner = fx.add_repo("Owner")
        owner.write_text(
            "Directory.Packages.props",
            '<Project><ItemGroup><PackageVersion Update="Some.Package" Version="1.0.0" /></ItemGroup></Project>',
        )
        fx.link("Owner", "references/Builds", "Builds", builds_commit)
        owner_commit = owner.commit()

        fx.policy["semantic_profiles"][fx.identity("Owner")] = "no-override-profile"
        fx.policy["profiles"]["no-override-profile"] = REQUIRED_PACKAGE_PROFILE
        fx.link("Root", "references/Owner", "Owner", owner_commit)
        root_commit = root.commit()

        envelope = dg.collect_graph(root.root, fx.identity("Root"), root_commit, fx.policy)
        with self.assertRaises(dg.GraphError) as ctx:
            dg.evaluate_semantics(root.root, fx.policy, envelope)
        self.assertIn("without local override", str(ctx.exception))

    def test_no_minver_violation_fails(self) -> None:
        root_xml = dg.parse_project_xml(
            b'<Project><ItemGroup><PackageReference Include="MinVer" /></ItemGroup></Project>', "test"
        )
        with self.assertRaises(dg.GraphError):
            dg.assert_no_minver(root_xml, "test")

    def test_no_minver_missing_directory_build_props_fails_closed(self) -> None:
        fx = GraphFixture(self.tmp_path)
        root = fx.add_repo("Root")
        root.write_text("Directory.Packages.props", OWNER_SHIM)
        owner, owner_commit = self._build(fx, "Owner", self._catalog_at("1.0.0"), "no-minver-profile", {
            "owner_checks": {"no_minver": True},
            "selected_catalog_required_properties": {},
            "selected_catalog_required_packages": {},
        })
        fx.link("Root", "references/Owner", "Owner", owner_commit)
        root_commit = root.commit()
        envelope = dg.collect_graph(root.root, fx.identity("Root"), root_commit, fx.policy)
        with self.assertRaises(dg.GraphError) as ctx:
            dg.evaluate_semantics(root.root, fx.policy, envelope)
        self.assertIn("missing Directory.Build.props while no_minver is required", str(ctx.exception))

    def test_no_inline_versions_in_tracked_files_fails(self) -> None:
        fx = GraphFixture(self.tmp_path)
        root = fx.add_repo("Root")
        root.write_text("Directory.Packages.props", OWNER_SHIM)
        builds = fx.add_repo("Builds")
        builds.write_bytes("Props/Directory.Packages.props", self._catalog_at("1.0.0"))
        builds_commit = builds.commit()

        owner = fx.add_repo("Owner")
        owner.write_text("Directory.Packages.props", OWNER_SHIM)
        owner.write_text(
            "src/Widget.csproj",
            '<Project><ItemGroup><PackageReference Include="Some.Package" Version="1.0.0" /></ItemGroup></Project>\n',
        )
        fx.link("Owner", "references/Builds", "Builds", builds_commit)
        owner_commit = owner.commit()

        fx.policy["semantic_profiles"][fx.identity("Owner")] = "no-inline-profile"
        fx.policy["profiles"]["no-inline-profile"] = {
            "owner_checks": {
                "no_inline_versions_in_tracked_files": {"extensions": [".csproj", ".props", ".targets"]},
            },
            "selected_catalog_required_properties": {},
            "selected_catalog_required_packages": {},
        }
        fx.link("Root", "references/Owner", "Owner", owner_commit)
        root_commit = root.commit()

        envelope = dg.collect_graph(root.root, fx.identity("Root"), root_commit, fx.policy)
        with self.assertRaises(dg.GraphError) as ctx:
            dg.evaluate_semantics(root.root, fx.policy, envelope)
        self.assertIn("must not declare an inline Version", str(ctx.exception))

    def test_validate_with_zero_builds_selectors_fails_closed(self) -> None:
        fx = GraphFixture(self.tmp_path)
        root = fx.add_repo("Root")
        root.write_text("Directory.Packages.props", OWNER_SHIM)
        root_commit = root.commit()
        envelope = dg.collect_graph(root.root, fx.identity("Root"), root_commit, fx.policy)
        with self.assertRaises(dg.GraphError) as ctx:
            dg.evaluate_semantics(root.root, fx.policy, envelope)
        self.assertIn("no Builds-selector edges", str(ctx.exception))

    def test_duplicate_gitmodules_path_fails_closed(self) -> None:
        fx = GraphFixture(self.tmp_path)
        root = fx.add_repo("Root")
        root.write_text("Directory.Packages.props", OWNER_SHIM)
        builds = fx.add_repo("Builds")
        builds.write_bytes("Props/Directory.Packages.props", self._catalog_at("1.0.0"))
        builds_commit = builds.commit()
        other = fx.add_repo("Other")
        other.write_text("README.md", "x\n")
        other_commit = other.commit()

        # Two .gitmodules entries sharing one path — last-wins must not be silent.
        root._gitmodules_entries.append(
            f'[submodule "builds"]\n\tpath = references/Shared\n\turl = {fx.url("Builds")}\n'
        )
        root._gitmodules_entries.append(
            f'[submodule "other"]\n\tpath = references/Shared\n\turl = {fx.url("Other")}\n'
        )
        root.write_text(".gitmodules", "".join(root._gitmodules_entries))
        root.link_gitlink("references/Shared", builds_commit)
        root_commit = root.commit()

        with self.assertRaises(dg.GraphError) as ctx:
            dg.collect_graph(root.root, fx.identity("Root"), root_commit, fx.policy)
        self.assertIn("duplicate .gitmodules path", str(ctx.exception))

    def test_multi_owner_commit_pins_are_each_owner_checked(self) -> None:
        """Two Builds-selector edges from the same identity at different owner commits
        must run owner_checks against each commit, not only the first.
        """
        fx = GraphFixture(self.tmp_path)
        root = fx.add_repo("Root")
        root.write_text("Directory.Packages.props", OWNER_SHIM)
        builds = fx.add_repo("Builds")
        builds.write_bytes("Props/Directory.Packages.props", self._catalog_at("1.0.0"))
        builds_commit = builds.commit()

        # Fabricate an envelope: same owner identity, two owner_commits. The later pin
        # introduces a PackageVersion row that no_package_version_rows must catch.
        # Use a real owner repo so read_blob works for both commits.
        owner = fx.add_repo("Owner")
        owner.write_text("Directory.Packages.props", OWNER_SHIM)
        fx.link("Owner", "references/Builds", "Builds", builds_commit)
        commit_clean = owner.commit("clean shim")
        owner.write_text(
            "Directory.Packages.props",
            '<Project><ItemGroup><PackageVersion Include="Own.Package" Version="1.0.0" /></ItemGroup></Project>\n',
        )
        commit_dirty = owner.commit("dirty shim")

        fx.policy["semantic_profiles"][fx.identity("Owner")] = "shim-profile"
        fx.policy["profiles"]["shim-profile"] = {
            "owner_checks": {"no_package_version_rows": True},
            "selected_catalog_required_properties": {},
            "selected_catalog_required_packages": {},
        }

        envelope = {
            "schema": dg.SCHEMA,
            "root": {"repository": fx.identity("Root"), "commit": "cccccccccccccccccccccccccccccccccccccccc"},
            "edges": [
                {
                    "owner_repository": fx.identity("Owner"),
                    "owner_commit": commit_clean,
                    "path": "references/Builds",
                    "repository": fx.identity("Builds"),
                    "commit": builds_commit,
                    "depth": 1,
                },
                {
                    "owner_repository": fx.identity("Owner"),
                    "owner_commit": commit_dirty,
                    "path": "references/Builds",
                    "repository": fx.identity("Builds"),
                    "commit": builds_commit,
                    "depth": 1,
                },
            ],
            "graph_digest": "0" * 64,
        }
        with self.assertRaises(dg.GraphError) as ctx:
            dg.evaluate_semantics(root.root, fx.policy, envelope)
        self.assertIn("import shim", str(ctx.exception))
        self.assertIn(commit_dirty, str(ctx.exception))

    def test_guarded_imports_mismatch_fails(self) -> None:
        spec = {
            "import_projects": ["$(A)"],
            "import_conditions": ["true"],
            "required_properties": {},
        }
        xml = dg.parse_project_xml(b"<Project></Project>", "test")
        with self.assertRaises(dg.GraphError):
            dg.assert_guarded_imports(xml, spec, "test")

    def test_bom_crlf_violations_fail(self) -> None:
        with self.assertRaises(dg.GraphError):
            dg.assert_utf8_bom_and_crlf(b"<Project></Project>", "no-bom")
        with self.assertRaises(dg.GraphError):
            dg.assert_utf8_bom_and_crlf(b"\xef\xbb\xbf<Project>\n</Project>\n", "bare-lf")
        # correct BOM + CRLF must pass without raising
        dg.assert_utf8_bom_and_crlf(b"\xef\xbb\xbf<Project>\r\n</Project>\r\n", "ok")


class DiffAndMaterializationTests(unittest.TestCase):
    def setUp(self) -> None:
        self._tmp = tempfile.TemporaryDirectory()
        self.tmp_path = pathlib.Path(self._tmp.name)
        self.addCleanup(self._tmp.cleanup)

    def _pointer_advance(self) -> tuple[GraphFixture, dict, dict]:
        fx = GraphFixture(self.tmp_path)
        builds = fx.add_repo("Builds")
        builds.write_bytes("Props/Directory.Packages.props", BASELINE_CATALOG)
        builds_base = builds.commit("base catalog")

        owner = fx.add_repo("Owner")
        owner.write_text("Directory.Packages.props", OWNER_SHIM)
        fx.link("Owner", "references/Builds", "Builds", builds_base)
        owner_base = owner.commit("base owner")

        root = fx.add_repo("Root")
        root.write_text("Directory.Packages.props", OWNER_SHIM)
        fx.link("Root", "references/Owner", "Owner", owner_base)
        root_base = root.commit("base root")

        builds.write_bytes("Props/Directory.Packages.props", BASELINE_CATALOG + b"<!-- compatible advance -->\r\n")
        builds_candidate = builds.commit("compatible catalog advance")
        owner.link_gitlink("references/Builds", builds_candidate)
        owner_candidate = owner.commit("advance catalog")
        root.link_gitlink("references/Owner", owner_candidate)
        root_candidate = root.commit("advance owner")

        for name in ("Root", "Owner", "Builds"):
            fx.register_module(name)
        base_graph = dg.collect_graph(root.root, fx.identity("Root"), root_base, fx.policy)
        candidate_graph = dg.collect_graph(root.root, fx.identity("Root"), root_candidate, fx.policy)
        return fx, base_graph, candidate_graph

    def test_depth1_pointer_advance_subsumes_descendant_churn_and_builds_once(self) -> None:
        fx, base_graph, candidate_graph = self._pointer_advance()
        evidence = dg.diff_graphs(
            base_graph,
            candidate_graph,
            fx.policy,
            event="push",
            event_base=base_graph["root"]["commit"],
            candidate=candidate_graph["root"]["commit"],
            merge_base=None,
            release_eligible=True,
        )
        self.assertEqual([row["repository"] for row in evidence["affected_modules"]], [fx.identity("Owner")])
        self.assertEqual(len(evidence["changes"]), 2)
        descendant = next(change for change in evidence["changes"] if (change["after"] or change["before"])["depth"] == 2)
        self.assertEqual(descendant["subsumed_by"], fx.identity("Owner"))
        self.assertEqual(
            evidence["affected_modules"][0]["restore_argv"],
            ["dotnet", "restore", "Owner.slnx", "-p:Configuration=Release", "-p:UseNuGetDeps=true"],
        )

    def test_unchanged_graph_builds_nothing(self) -> None:
        fx, base_graph, _candidate_graph = self._pointer_advance()
        evidence = dg.diff_graphs(
            base_graph,
            base_graph,
            fx.policy,
            event="push",
            event_base=base_graph["root"]["commit"],
            candidate=base_graph["root"]["commit"],
            merge_base=None,
            release_eligible=True,
        )
        self.assertEqual(evidence["changes"], [])
        self.assertEqual(evidence["affected_modules"], [])

    def test_root_owner_commit_only_churn_builds_nothing_and_retains_provenance(self) -> None:
        fx = GraphFixture(self.tmp_path)
        owner = fx.add_repo("Owner")
        owner.write_text("Directory.Packages.props", OWNER_SHIM)
        owner_commit = owner.commit("owner")

        root = fx.add_repo("Root")
        root.write_text("Directory.Packages.props", OWNER_SHIM)
        fx.link("Root", "references/Owner", "Owner", owner_commit)
        root_base = root.commit("base root")
        root.write_text("docs/unrelated.md", "root-only change")
        root_candidate = root.commit("candidate root")

        for name in ("Root", "Owner"):
            fx.register_module(name)
        base_graph = dg.collect_graph(root.root, fx.identity("Root"), root_base, fx.policy)
        candidate_graph = dg.collect_graph(root.root, fx.identity("Root"), root_candidate, fx.policy)

        evidence = dg.diff_graphs(
            base_graph,
            candidate_graph,
            fx.policy,
            event="push",
            event_base=root_base,
            candidate=root_candidate,
            merge_base=None,
            release_eligible=True,
        )

        self.assertNotEqual(root_base, root_candidate)
        self.assertEqual(base_graph["edges"][0]["owner_commit"], root_base)
        self.assertEqual(candidate_graph["edges"][0]["owner_commit"], root_candidate)
        self.assertEqual(evidence["changes"], [])
        self.assertEqual(evidence["affected_modules"], [])

    def test_removed_depth1_edge_collapses_to_frontcomposer_root(self) -> None:
        fx, base_graph, candidate_graph = self._pointer_advance()
        removed_candidate = copy.deepcopy(candidate_graph)
        removed_candidate["edges"] = []
        removed_candidate["edge_count"] = 0
        removed_candidate["graph_digest"] = dg.canonical_digest({
            "schema": removed_candidate["schema"],
            "root": removed_candidate["root"],
            "edge_count": 0,
            "edges": [],
        })
        evidence = dg.diff_graphs(
            base_graph,
            removed_candidate,
            fx.policy,
            event="push",
            event_base=base_graph["root"]["commit"],
            candidate=removed_candidate["root"]["commit"],
            merge_base=None,
            release_eligible=True,
        )
        self.assertEqual([row["repository"] for row in evidence["affected_modules"]], [fx.identity("Root")])

    def test_contract_tree_materializes_exact_regular_bytes(self) -> None:
        fx = GraphFixture(self.tmp_path)
        builds = fx.add_repo("Builds")
        builds.write_bytes("Props/Directory.Packages.props", BASELINE_CATALOG)
        builds.write_bytes("eng/config.bin", b"\x00\x01\xff")
        commit = builds.commit()
        destination = self.tmp_path / "materialized"
        result = dg.materialize_contract_tree(builds.root, commit, destination, fx.policy)
        self.assertEqual(result["file_count"], 2)
        self.assertEqual((destination / "eng/config.bin").read_bytes(), b"\x00\x01\xff")
        self.assertEqual((destination / "Props/Directory.Packages.props").read_bytes(), BASELINE_CATALOG)

    def test_contract_tree_omits_exact_nested_gitlink_and_materializes_regular_files(self) -> None:
        fx = GraphFixture(self.tmp_path)
        ai_tools = fx.add_repo("AI.Tools")
        ai_tools.write_text("README.md", "nested repository content")
        ai_tools_commit = ai_tools.commit("nested repository")

        builds = fx.add_repo("Builds")
        builds.write_bytes("Props/Directory.Packages.props", BASELINE_CATALOG)
        builds.write_bytes("eng/config.bin", b"\x00\x01\xff")
        executable = builds.write_text("eng/build.sh", "#!/bin/sh\nexit 0\n")
        executable.chmod(0o755)
        _run(["add", "--", "eng/build.sh"], builds.root)
        builds.add_submodule(
            "Hexalith.AI.Tools",
            "references/Hexalith.AI.Tools",
            fx.url("AI.Tools"),
            ai_tools_commit,
        )
        commit = builds.commit("real-shaped Builds tree")

        destination = self.tmp_path / "materialized-with-gitlink"
        result = dg.materialize_contract_tree(builds.root, commit, destination, fx.policy)

        self.assertEqual(result["file_count"], 4)
        self.assertEqual((destination / "eng/config.bin").read_bytes(), b"\x00\x01\xff")
        self.assertEqual((destination / "Props/Directory.Packages.props").read_bytes(), BASELINE_CATALOG)
        self.assertEqual((destination / "eng/build.sh").stat().st_mode & 0o777, 0o755)
        self.assertTrue((destination / ".gitmodules").is_file())
        self.assertFalse((destination / "references/Hexalith.AI.Tools").exists())

    def test_contract_tree_validates_gitlink_path_before_omitting_it(self) -> None:
        fx = GraphFixture(self.tmp_path)
        builds = fx.add_repo("Builds")
        builds.link_gitlink(r"references\Nested", "f" * 40)
        commit = builds.commit("unsafe gitlink path")
        destination = self.tmp_path / "unsafe-gitlink"

        with self.assertRaises(dg.GraphError) as ctx:
            dg.materialize_contract_tree(builds.root, commit, destination, fx.policy)

        self.assertIn("unsafe path", str(ctx.exception))
        self.assertFalse(destination.exists())

    def test_contract_tree_rejects_symlink_and_limits_before_extraction(self) -> None:
        fx = GraphFixture(self.tmp_path)
        builds = fx.add_repo("Builds")
        builds.write_bytes("Props/Directory.Packages.props", BASELINE_CATALOG)
        os.symlink("Props/Directory.Packages.props", builds.root / "catalog-link")
        _run(["add", "catalog-link"], builds.root)
        commit = builds.commit()
        destination = self.tmp_path / "rejected"
        with self.assertRaises(dg.GraphError) as ctx:
            dg.materialize_contract_tree(builds.root, commit, destination, fx.policy)
        self.assertIn("catalog-link", str(ctx.exception))
        self.assertIn("unsupported mode/type", str(ctx.exception))
        self.assertFalse(destination.exists())

        fx.policy["resource_limits"]["max_contract_tree_files"] = 1
        clean = fx.add_repo("CleanBuilds")
        clean.write_bytes("Props/Directory.Packages.props", BASELINE_CATALOG)
        clean.write_text("second.txt", "x")
        clean_commit = clean.commit()
        with self.assertRaises(dg.GraphError):
            dg.materialize_contract_tree(clean.root, clean_commit, destination, fx.policy)
        self.assertFalse(destination.exists())

    def test_module_materialization_is_detached_exact_and_does_not_initialize_gitlinks(self) -> None:
        owner = TempGitRepo(self.tmp_path / "Owner")
        owner.write_text("value.txt", "base")
        owner.link_gitlink("references/Nested", "f" * 40)
        base = owner.commit("base")
        owner.write_text("value.txt", "candidate")
        owner.commit("candidate")

        destination = self.tmp_path / "isolated-owner"
        dg.materialize_repository_tree(owner.root, base, destination)

        self.assertEqual((destination / "value.txt").read_text(), "base")
        self.assertTrue((destination / ".git").exists())
        self.assertFalse((destination / "references/Nested/.git").exists())
        observed = _run(["rev-parse", "HEAD"], destination).decode("ascii").strip()
        self.assertEqual(observed, base)

    def test_run_affected_replaces_empty_gitlink_mount_before_contract_materialization(self) -> None:
        fx = GraphFixture(self.tmp_path)
        builds = fx.add_repo("Builds")
        owner = fx.add_repo("Owner")
        fx.register_module("Owner")
        owner_commit = "a" * 40
        builds_commit = "b" * 40
        catalog_sha256 = hashlib.sha256(BASELINE_CATALOG).hexdigest()
        selector = {
            "owner_repository": fx.identity("Owner"),
            "owner_commit": owner_commit,
            "path": "references/Builds",
            "repository": fx.identity("Builds"),
            "commit": builds_commit,
            "depth": 2,
            "catalog_sha256": catalog_sha256,
            "catalog_contract_version": None,
        }
        module = dg._module_proof(fx.identity("Owner"), owner_commit, ["test pointer advance"], fx.policy)
        evidence = {"candidate_graph": {"edges": [selector]}, "affected_modules": [module]}
        output_root = self.tmp_path / "affected-output"

        def materialize_module(_local: pathlib.Path, _commit: str, destination: pathlib.Path) -> None:
            (destination / "references/Builds").mkdir(parents=True)

        def materialize_contract(
            _local: pathlib.Path,
            _commit: str,
            destination: pathlib.Path,
            _policy: dict,
        ) -> dict:
            self.assertFalse(destination.exists())
            catalog = destination / "Props/Directory.Packages.props"
            catalog.parent.mkdir(parents=True)
            catalog.write_bytes(BASELINE_CATALOG)
            return {"file_count": 1, "total_bytes": len(BASELINE_CATALOG), "commit": builds_commit}

        completed = subprocess.CompletedProcess([], 0, stdout="", stderr="")
        with (
            mock.patch.object(dg, "validate_diff_evidence", return_value=(fx.policy, evidence)),
            mock.patch.object(dg, "materialize_repository_tree", side_effect=materialize_module),
            mock.patch.object(dg, "materialize_contract_tree", side_effect=materialize_contract),
            mock.patch.object(dg.subprocess, "run", return_value=completed) as run,
        ):
            result = dg.run_affected_builds(owner.root, evidence, output_root)

        self.assertEqual(result["modules"][0]["disposition"], "build")
        self.assertEqual(run.call_count, 2)

    def test_run_affected_build_command_failure_remains_blocking(self) -> None:
        fx = GraphFixture(self.tmp_path)
        builds = fx.add_repo("Builds")
        owner = fx.add_repo("Owner")
        fx.register_module("Owner")
        owner_commit = "a" * 40
        builds_commit = "b" * 40
        catalog_sha256 = hashlib.sha256(BASELINE_CATALOG).hexdigest()
        selector = {
            "owner_repository": fx.identity("Owner"),
            "owner_commit": owner_commit,
            "path": "references/Builds",
            "repository": fx.identity("Builds"),
            "commit": builds_commit,
            "depth": 2,
            "catalog_sha256": catalog_sha256,
            "catalog_contract_version": None,
        }
        module = dg._module_proof(
            fx.identity("Owner"),
            owner_commit,
            ["test pointer advance"],
            fx.policy,
        )
        evidence = {"candidate_graph": {"edges": [selector]}, "affected_modules": [module]}
        output_root = self.tmp_path / "affected-failure-output"

        def materialize_module(_local: pathlib.Path, _commit: str, destination: pathlib.Path) -> None:
            (destination / "references/Builds").mkdir(parents=True)

        def materialize_contract(
            _local: pathlib.Path,
            _commit: str,
            destination: pathlib.Path,
            _policy: dict,
        ) -> dict:
            catalog = destination / "Props/Directory.Packages.props"
            catalog.parent.mkdir(parents=True)
            catalog.write_bytes(BASELINE_CATALOG)
            return {"file_count": 1, "total_bytes": len(BASELINE_CATALOG), "commit": builds_commit}

        restore_succeeded = subprocess.CompletedProcess([], 0, stdout="restored", stderr="")
        build_failed = subprocess.CompletedProcess([], 23, stdout="", stderr="synthetic build failure")
        with (
            mock.patch.object(dg, "validate_diff_evidence", return_value=(fx.policy, evidence)),
            mock.patch.object(dg, "materialize_repository_tree", side_effect=materialize_module),
            mock.patch.object(dg, "materialize_contract_tree", side_effect=materialize_contract),
            mock.patch.object(
                dg.subprocess,
                "run",
                side_effect=(restore_succeeded, build_failed),
            ) as run,
            self.assertRaises(dg.GraphError) as ctx,
        ):
            dg.run_affected_builds(owner.root, evidence, output_root)

        message = str(ctx.exception)
        self.assertIn(f"affected module {fx.identity('Owner')}@{owner_commit}", message)
        self.assertIn("build_argv failed with exit 23", message)
        self.assertIn("synthetic build failure", message)
        self.assertEqual(run.call_count, 2)

    def test_offline_graph_validation_rejects_drift_and_out_of_order_edges(self) -> None:
        _fx, _base_graph, candidate_graph = self._pointer_advance()
        drifted = copy.deepcopy(candidate_graph)
        drifted["edge_count"] += 1
        with self.assertRaises(dg.GraphError):
            dg.validate_graph_envelope(drifted)

        out_of_order = copy.deepcopy(candidate_graph)
        out_of_order["edges"].reverse()
        out_of_order["graph_digest"] = dg.canonical_digest({
            "schema": out_of_order["schema"],
            "root": out_of_order["root"],
            "edge_count": out_of_order["edge_count"],
            "edges": out_of_order["edges"],
        })
        with self.assertRaises(dg.GraphError):
            dg.validate_graph_envelope(out_of_order)

    def _reseal(self, graph: dict) -> None:
        graph["edges"].sort(key=lambda edge: (
            edge["depth"], edge["owner_repository"], edge["owner_commit"],
            edge["path"], edge["repository"], edge["commit"],
        ))
        graph["edge_count"] = len(graph["edges"])
        graph["graph_digest"] = dg.canonical_digest({
            "schema": graph["schema"],
            "root": graph["root"],
            "edge_count": graph["edge_count"],
            "edges": graph["edges"],
        })

    @staticmethod
    def _reseal_evidence(evidence: dict) -> None:
        evidence["evidence_digest"] = dg.canonical_digest({
            name: value for name, value in evidence.items() if name != "evidence_digest"
        })

    def _live_evidence(self) -> tuple[GraphFixture, dict, dict]:
        fx, base_graph, candidate_graph = self._pointer_advance()
        root_identity = fx.identity("Root")
        next(entry for entry in fx.policy["trusted_identities"] if entry["identity"] == root_identity)["local_path"] = "."
        evidence = dg.diff_graphs(
            base_graph,
            candidate_graph,
            fx.policy,
            event="push",
            event_base=base_graph["root"]["commit"],
            candidate=candidate_graph["root"]["commit"],
            merge_base=None,
            release_eligible=True,
        )
        coordinate = {
            "schema": dg.POLICY_SCHEMA,
            "repository": root_identity,
            "path": dg.POLICY_PATH,
            "commit": base_graph["root"]["commit"],
            "sha256": "a" * 64,
        }
        evidence["dependency_policy"] = coordinate
        self._reseal_evidence(evidence)
        return fx, evidence, coordinate

    def test_affected_evidence_rejects_self_declared_untrusted_root_identity(self) -> None:
        fx, evidence, coordinate = self._live_evidence()
        hostile = copy.deepcopy(evidence)
        for graph_name in ("base_graph", "candidate_graph"):
            graph = hostile[graph_name]
            graph["root"]["repository"] = "github.com/test/forged"
            for edge in graph["edges"]:
                if edge["depth"] == 1:
                    edge["owner_repository"] = "github.com/test/forged"
            self._reseal(graph)
        self._reseal_evidence(hostile)

        with (
            mock.patch.object(dg, "load_policy_at_commit", return_value=(fx.policy, b"", coordinate)),
            self.assertRaises(dg.GraphError) as ctx,
        ):
            dg.validate_diff_evidence(self.tmp_path, hostile)

        self.assertIn("trusted root identity", str(ctx.exception))

    def test_affected_evidence_rejects_base_graph_revision_mismatch(self) -> None:
        fx, evidence, coordinate = self._live_evidence()
        hostile = copy.deepcopy(evidence)
        hostile["base_graph"]["root"]["commit"] = "d" * 40
        for edge in hostile["base_graph"]["edges"]:
            if edge["depth"] == 1:
                edge["owner_commit"] = "d" * 40
        self._reseal(hostile["base_graph"])
        self._reseal_evidence(hostile)

        with (
            mock.patch.object(dg, "load_policy_at_commit", return_value=(fx.policy, b"", coordinate)),
            self.assertRaises(dg.GraphError) as ctx,
        ):
            dg.validate_diff_evidence(self.tmp_path, hostile)

        self.assertIn("base graph root commit", str(ctx.exception))

    def test_affected_evidence_applies_active_policy_edge_ceiling(self) -> None:
        fx, evidence, coordinate = self._live_evidence()
        fx.policy["resource_limits"]["max_edges"] = 1

        with (
            mock.patch.object(dg, "load_policy_at_commit", return_value=(fx.policy, b"", coordinate)),
            self.assertRaises(dg.GraphError) as ctx,
        ):
            dg.validate_diff_evidence(self.tmp_path, evidence)

        self.assertIn("active-policy 1-edge ceiling", str(ctx.exception))

    def test_offline_graph_validation_rejects_forged_depth1_root_ownership(self) -> None:
        _fx, _base_graph, candidate_graph = self._pointer_advance()
        for field, value, diagnostic in (
            ("owner_repository", "github.com/test/forged", "root repository"),
            ("owner_commit", "d" * 40, "root commit"),
        ):
            with self.subTest(field=field):
                hostile = copy.deepcopy(candidate_graph)
                depth1 = next(edge for edge in hostile["edges"] if edge["depth"] == 1)
                depth1[field] = value
                self._reseal(hostile)
                with self.assertRaises(dg.GraphError) as ctx:
                    dg.validate_graph_envelope(hostile)
                self.assertIn(diagnostic, str(ctx.exception))

    def test_offline_graph_validation_rejects_orphan_depth2_ancestry(self) -> None:
        _fx, _base_graph, candidate_graph = self._pointer_advance()
        hostile = copy.deepcopy(candidate_graph)
        depth2 = next(edge for edge in hostile["edges"] if edge["depth"] == 2)
        depth2["owner_commit"] = "d" * 40
        self._reseal(hostile)

        with self.assertRaises(dg.GraphError) as ctx:
            dg.validate_graph_envelope(hostile)

        self.assertIn("depth-1 target", str(ctx.exception))

    def test_offline_graph_validation_rejects_duplicate_logical_edges(self) -> None:
        _fx, _base_graph, candidate_graph = self._pointer_advance()
        hostile = copy.deepcopy(candidate_graph)
        depth1 = next(edge for edge in hostile["edges"] if edge["depth"] == 1)
        depth2 = next(edge for edge in hostile["edges"] if edge["depth"] == 2)
        alternate_owner_commit = "d" * 40

        alternate_depth1 = copy.deepcopy(depth1)
        alternate_depth1["path"] = "references/OwnerAlternate"
        alternate_depth1["commit"] = alternate_owner_commit
        alternate_depth2 = copy.deepcopy(depth2)
        alternate_depth2["owner_commit"] = alternate_owner_commit
        hostile["edges"].extend((alternate_depth1, alternate_depth2))
        self._reseal(hostile)

        with self.assertRaises(dg.GraphError) as ctx:
            dg.validate_graph_envelope(hostile)

        self.assertIn("duplicate logical edge", str(ctx.exception))

    def test_zero_push_base_forces_full_affected_and_is_never_release_eligible(self) -> None:
        # Reproduces, at the function level, the CLI `diff` zero/unavailable-push-base
        # branch (main(), "diff" command, lines ~1724-1751): the candidate graph stands
        # in for both base and candidate, release_eligible is forced False, and
        # full_affected marks every module rather than computing a real (unavailable)
        # diff. This does not invoke main()/argv parsing itself.
        fx, _base_graph, candidate_graph = self._pointer_advance()
        root_identity = fx.identity("Root")
        next(entry for entry in fx.policy["trusted_identities"] if entry["identity"] == root_identity)["local_path"] = "."
        zero_base = "0" * 40
        candidate = candidate_graph["root"]["commit"]

        evidence = dg.diff_graphs(
            candidate_graph,
            candidate_graph,
            fx.policy,
            event="push",
            event_base=candidate,
            candidate=candidate,
            merge_base=None,
            release_eligible=False,
            full_affected=True,
        )
        evidence["revisions"]["event_base"] = zero_base
        evidence["diagnostic"] = "zero/unavailable push base: full-affected diagnostics only; not release-eligible"

        self.assertFalse(evidence["revisions"]["release_eligible"])
        self.assertEqual(evidence["changes"], [])
        self.assertEqual(
            sorted(row["repository"] for row in evidence["affected_modules"]),
            sorted(fx.identity(name) for name in ("Root", "Owner")),
        )
        self.assertTrue(
            all("fail-closed full-affected diagnostic" in row["reasons"] for row in evidence["affected_modules"])
        )

        coordinate = {
            "schema": dg.POLICY_SCHEMA,
            "repository": root_identity,
            "path": dg.POLICY_PATH,
            "commit": candidate,
            "sha256": "a" * 64,
        }
        evidence["dependency_policy"] = coordinate
        self._reseal_evidence(evidence)

        with mock.patch.object(dg, "load_policy_at_commit", return_value=(fx.policy, b"", coordinate)):
            dg.validate_diff_evidence(self.tmp_path, evidence)


class NetworkAcquisitionRetryTests(unittest.TestCase):
    """Transient remote failures must retry; authoritative answers must still fail closed.

    A hosted-runner TLS blip cloning an approved remote (observed 2026-08-11) aborted the CI
    step before the graph gate ran. Retrying is safe because the requested commit is still
    verified after every fetch, but retrying a governance answer -- unknown ref, missing
    repository -- would weaken the gate, so only infrastructure-shaped stderr may retry.
    """

    def _completed(self, returncode: int, stderr: str) -> subprocess.CompletedProcess:
        return subprocess.CompletedProcess([], returncode, stdout=b"", stderr=stderr.encode("utf-8"))

    def test_transient_clone_failure_retries_and_succeeds(self) -> None:
        flake = self._completed(
            128,
            "fatal: unable to access 'https://github.com/hexalith/hexalith.tenants.git/': "
            "server certificate verification failed. CAfile: none CRLfile: none",
        )
        with (
            mock.patch.object(dg.subprocess, "run", side_effect=(flake, self._completed(0, ""))) as run,
            mock.patch.object(dg.time, "sleep") as sleep,
        ):
            proc, attempts = dg._run_git_network(["clone", "--quiet", "https://example.invalid/x.git", "dest"])

        self.assertEqual(proc.returncode, 0)
        self.assertEqual(attempts, 2)
        self.assertEqual(run.call_count, 2)
        sleep.assert_called_once_with(dg.NETWORK_RETRY_BACKOFF_SECONDS)

    def test_transient_failure_stops_at_the_attempt_ceiling(self) -> None:
        flake = self._completed(128, "fatal: unable to access: Could not resolve host: github.com")
        with (
            mock.patch.object(dg.subprocess, "run", return_value=flake) as run,
            mock.patch.object(dg.time, "sleep"),
        ):
            proc, attempts = dg._run_git_network(["fetch", "--no-tags", "origin", "a" * 40])

        self.assertEqual(proc.returncode, 128)
        self.assertEqual(attempts, dg.NETWORK_RETRY_ATTEMPTS)
        self.assertEqual(run.call_count, dg.NETWORK_RETRY_ATTEMPTS)

    def test_authoritative_failure_fails_closed_without_retry(self) -> None:
        missing_ref = self._completed(128, "fatal: couldn't find remote ref " + "a" * 40)
        with (
            mock.patch.object(dg.subprocess, "run", return_value=missing_ref) as run,
            mock.patch.object(dg.time, "sleep") as sleep,
        ):
            proc, attempts = dg._run_git_network(["fetch", "--no-tags", "origin", "a" * 40])

        self.assertEqual(proc.returncode, 128)
        self.assertEqual(attempts, 1)
        self.assertEqual(run.call_count, 1)
        sleep.assert_not_called()

    def test_retry_clears_a_partial_clone_destination(self) -> None:
        destination = pathlib.Path(tempfile.mkdtemp()) / "partial-store"
        destination.mkdir(parents=True)
        (destination / "objects").mkdir()
        flake = self._completed(128, "fatal: the remote end hung up unexpectedly")

        def run_side_effect(*_args: object, **_kwargs: object) -> subprocess.CompletedProcess:
            if run.call_count == 1:
                return flake
            self.assertFalse(destination.exists())
            return self._completed(0, "")

        with (
            mock.patch.object(dg.subprocess, "run", side_effect=run_side_effect) as run,
            mock.patch.object(dg.time, "sleep"),
        ):
            proc, attempts = dg._run_git_network(
                ["clone", "--quiet", "https://example.invalid/x.git", str(destination)],
                reset_before_retry=destination,
            )

        self.assertEqual(proc.returncode, 0)
        self.assertEqual(attempts, 2)

    def test_exhausted_retries_report_the_attempt_count(self) -> None:
        flake = self._completed(128, "fatal: unable to access: Could not resolve host: github.com")
        with (
            mock.patch.object(dg.subprocess, "run", return_value=flake),
            mock.patch.object(dg.time, "sleep"),
            self.assertRaises(dg.GraphError) as ctx,
        ):
            dg._ensure_commit_available(pathlib.Path("/nonexistent-store"), "a" * 40)

        message = str(ctx.exception)
        self.assertIn(f"after {dg.NETWORK_RETRY_ATTEMPTS} attempts", message)
        self.assertIn("Could not resolve host", message)


class CanonicalDigestTests(unittest.TestCase):
    def test_canonical_bytes_are_compact_sorted_ascii(self) -> None:
        payload = {"b": 1, "a": [3, 2, 1], "c": "café"}
        raw = dg.canonical_bytes(payload)
        self.assertEqual(raw, b'{"a":[3,2,1],"b":1,"c":"caf\\u00e9"}')
        self.assertFalse(raw.endswith(b"\n"))

    def test_digest_is_sha256_of_canonical_bytes(self) -> None:
        payload = {"x": 1}
        self.assertEqual(dg.canonical_digest(payload), hashlib.sha256(dg.canonical_bytes(payload)).hexdigest())


class PolicyShapeTests(unittest.TestCase):
    """A profile whose shape would make a required check silently vacuous must fail closed.

    Before these pins, `profile.get("selected_catalog_required_properties", {})` meant a
    renamed or misspelled key validated nothing while `validate` still reported ok: true.
    """

    def setUp(self) -> None:
        self._tmp = tempfile.TemporaryDirectory()
        self.tmp_path = pathlib.Path(self._tmp.name)
        self.addCleanup(self._tmp.cleanup)

    def _load(self, profile: object) -> dict:
        path = self.tmp_path / "policy.json"
        policy = json.loads((ROOT / "eng" / "dependency-graph-policy.json").read_text(encoding="utf-8-sig"))
        policy["profiles"]["some-profile"] = profile
        path.write_text(json.dumps(policy), encoding="utf-8")
        return dg.load_policy(path)

    @staticmethod
    def _frontcomposer_profile() -> dict:
        policy = json.loads(
            (ROOT / "eng" / "dependency-graph-policy.json").read_text(encoding="utf-8-sig")
        )
        return copy.deepcopy(policy["profiles"]["frontcomposer-catalog-v1"])

    @staticmethod
    def _minimal_validate_policy(root_identity: str, builds_identity: str) -> dict:
        evidence_only = {
            "disposition": "evidence-only",
            "solution": None,
            "builds_contract_source": "none",
            "restore_argv": None,
            "build_argv": None,
        }
        return {
            "schema": dg.POLICY_SCHEMA,
            "builds_identity": builds_identity,
            "trusted_identities": [
                {"identity": root_identity, "local_path": "."},
                {"identity": builds_identity, "local_path": "references/Builds"},
            ],
            "semantic_profiles": {
                root_identity: "root-policy-a",
                builds_identity: "builds-policy",
            },
            "profiles": {
                "root-policy-a": {
                    "selected_catalog_required_packages": {"Some.Package": "1.0.0"},
                },
                "builds-policy": {},
            },
            "module_build_registry": {
                root_identity: copy.deepcopy(evidence_only),
                builds_identity: copy.deepcopy(evidence_only),
            },
            "resource_limits": {
                "max_edges": 4096,
                "max_ls_tree_bytes_per_owner_commit": 67108864,
                "max_gitmodules_blob_bytes": 1048576,
                "max_catalog_blob_bytes": 4194304,
                "max_contract_tree_files": 16384,
                "max_contract_tree_blob_bytes": 16777216,
                "max_contract_tree_total_bytes": 268435456,
                "max_workflow_closure_depth": 16,
                "max_workflow_closure_sources": 256,
                "max_workflow_source_blob_bytes": 1048576,
                "max_workflow_source_total_bytes": 16777216,
            },
            "evaluator_authorizations": {"ci": [], "release": [], "post_release": []},
        }

    def test_validate_uses_policy_from_selected_commit_by_default(self) -> None:
        root_identity = GraphFixture.identity("Root")
        builds_identity = GraphFixture.identity("Builds")
        root = TempGitRepo(self.tmp_path / "Root")
        root.write_text("Directory.Packages.props", OWNER_SHIM)
        builds = TempGitRepo(root.root / "references" / "Builds")
        builds.write_bytes("Props/Directory.Packages.props", BASELINE_CATALOG)
        builds_commit = builds.commit()
        root.add_submodule("Builds", "references/Builds", GraphFixture.url("Builds"), builds_commit)

        policy_a = self._minimal_validate_policy(root_identity, builds_identity)
        root.write_text(dg.POLICY_PATH, json.dumps(policy_a))
        policy_a_commit = root.commit()

        policy_b = copy.deepcopy(policy_a)
        policy_b["semantic_profiles"][root_identity] = "root-policy-b"
        policy_b["profiles"]["root-policy-b"] = {
            "selected_catalog_required_packages": {"Some.Package": "2.0.0"},
        }
        (root.root / dg.POLICY_PATH).write_text(json.dumps(policy_b), encoding="utf-8")

        completed = subprocess.run(
            [
                sys.executable,
                str(ROOT / "eng" / "dependency_graph.py"),
                "--root",
                str(root.root),
                "validate",
                "--commit",
                policy_a_commit,
                "--root-identity",
                root_identity,
            ],
            cwd=str(root.root),
            capture_output=True,
            text=True,
        )

        self.assertEqual(completed.returncode, 0, completed.stdout + completed.stderr)
        payload = json.loads(completed.stdout)
        self.assertTrue(payload["ok"])
        self.assertEqual(payload["semantics"]["selectors_validated"], 1)
        self.assertIn("under profile root-policy-a", payload["semantics"]["diagnostics"][0])
        self.assertNotIn("root-policy-b", payload["semantics"]["diagnostics"][0])

    def test_validate_rejects_legacy_committed_policy_before_graph_collection(self) -> None:
        root_identity = GraphFixture.identity("Root")
        builds_identity = GraphFixture.identity("Builds")
        root = TempGitRepo(self.tmp_path / "Root")
        legacy_policy = self._minimal_validate_policy(root_identity, builds_identity)
        for row in legacy_policy["module_build_registry"].values():
            del row["restore_argv"]
            del row["build_argv"]
        for name in (
            "max_workflow_closure_depth",
            "max_workflow_closure_sources",
            "max_workflow_source_blob_bytes",
            "max_workflow_source_total_bytes",
        ):
            del legacy_policy["resource_limits"][name]
        root.write_text(dg.POLICY_PATH, json.dumps(legacy_policy))
        legacy_commit = root.commit()

        completed = subprocess.run(
            [
                sys.executable,
                str(ROOT / "eng" / "dependency_graph.py"),
                "--root",
                str(root.root),
                "validate",
                "--commit",
                legacy_commit,
                "--root-identity",
                root_identity,
            ],
            cwd=str(root.root),
            capture_output=True,
            text=True,
        )

        self.assertEqual(completed.returncode, 1, completed.stdout + completed.stderr)
        payload = json.loads(completed.stdout)
        self.assertFalse(payload["ok"])
        self.assertEqual(
            payload["error"],
            "validate requires a strict committed policy; legacy policy migration is non-executable",
        )

    def test_known_profile_keys_load(self) -> None:
        policy = self._load({
            "owner_checks": {},
            "selected_catalog_required_property_names": ["SomeVersion"],
            "selected_catalog_required_properties": {},
            "selected_catalog_required_package_names": ["Some.Package"],
            "selected_catalog_required_packages": {},
        })
        self.assertEqual(
            policy["profiles"]["some-profile"]["selected_catalog_required_property_names"],
            ["SomeVersion"],
        )
        self.assertEqual(
            policy["profiles"]["some-profile"]["selected_catalog_required_package_names"],
            ["Some.Package"],
        )

    def test_misspelled_required_properties_key_fails_closed(self) -> None:
        with self.assertRaises(dg.GraphError) as ctx:
            self._load({"selected_catalog_required_propertys": {"SomeVersion": "1.0.0"}})
        self.assertIn("unknown keys", str(ctx.exception))

    def test_required_property_names_must_be_non_empty_sorted_unique_names(self) -> None:
        for value in (
            [],
            "SomeVersion",
            ["SomeVersion", "SomeVersion"],
            ["ZVersion", "AVersion"],
            ["not-a-name"],
            [1],
        ):
            with self.subTest(value=value):
                with self.assertRaises(dg.GraphError):
                    self._load({"selected_catalog_required_property_names": value})

    def test_required_property_names_cannot_duplicate_literal_requirements(self) -> None:
        with self.assertRaises(dg.GraphError) as ctx:
            self._load({
                "selected_catalog_required_property_names": ["SomeVersion"],
                "selected_catalog_required_properties": {"SomeVersion": "1.0.0"},
            })
        self.assertIn("cannot also carry literal requirements", str(ctx.exception))

    def test_required_package_names_must_be_non_empty_sorted_unique_ids(self) -> None:
        for value in (
            [],
            "Some.Package",
            ["Some.Package", "Some.Package"],
            ["Z.Package", "A.Package"],
            ["not a package"],
            [1],
        ):
            with self.subTest(value=value):
                with self.assertRaises(dg.GraphError):
                    self._load({"selected_catalog_required_package_names": value})

    def test_required_package_names_cannot_duplicate_literal_requirements(self) -> None:
        with self.assertRaises(dg.GraphError) as ctx:
            self._load({
                "selected_catalog_required_package_names": ["Some.Package"],
                "selected_catalog_required_packages": {"Some.Package": "1.0.0"},
            })
        self.assertIn("cannot also carry literal requirements", str(ctx.exception))

    def test_non_dict_required_properties_fails_closed(self) -> None:
        with self.assertRaises(dg.GraphError) as ctx:
            self._load({"selected_catalog_required_properties": ["SomeVersion"]})
        self.assertIn("must be an object", str(ctx.exception))

    def test_explicit_null_required_catalog_controls_fail_closed(self) -> None:
        for key in (
            "selected_catalog_required_property_names",
            "selected_catalog_required_properties",
            "selected_catalog_required_package_names",
            "selected_catalog_required_packages",
        ):
            with self.subTest(key=key):
                with self.assertRaises(dg.GraphError) as ctx:
                    self._load({key: None})
                self.assertIn(key, str(ctx.exception))

    def test_non_string_expected_value_fails_closed(self) -> None:
        with self.assertRaises(dg.GraphError) as ctx:
            self._load({"selected_catalog_required_properties": {"SomeVersion": 1.0}})
        self.assertIn("must be a string", str(ctx.exception))

    def test_canonical_source_rule_rejects_unbounded_or_malformed_enrollment(self) -> None:
        for files in ({}, {"../escape.cs": "a" * 64}, {"DaprSelfHostedMtls.cs": "not-a-digest"}, True):
            with self.subTest(files=files), self.assertRaises(dg.GraphError):
                self._load({"owner_checks": {"canonical_source_files": files}})

    def test_unknown_owner_checks_key_fails_closed(self) -> None:
        with self.assertRaises(dg.GraphError) as ctx:
            self._load({
                "owner_checks": {"no_package_version_row": True},
                "selected_catalog_required_properties": {},
                "selected_catalog_required_packages": {},
            })
        self.assertIn("owner_checks has unknown keys", str(ctx.exception))

    def test_non_object_owner_checks_fails_closed(self) -> None:
        with self.assertRaises(dg.GraphError) as ctx:
            self._load({
                "owner_checks": ["no_minver"],
                "selected_catalog_required_properties": {},
                "selected_catalog_required_packages": {},
            })
        self.assertIn("owner_checks must be an object", str(ctx.exception))

    def test_owner_checks_boolean_value_must_be_bool(self) -> None:
        with self.assertRaises(dg.GraphError) as ctx:
            self._load({
                "owner_checks": {"no_minver": "yes"},
                "selected_catalog_required_properties": {},
                "selected_catalog_required_packages": {},
            })
        self.assertIn("must be a boolean", str(ctx.exception))

    def test_owner_checks_inline_versions_must_be_object_with_extensions(self) -> None:
        with self.assertRaises(dg.GraphError) as ctx:
            self._load({
                "owner_checks": {"no_inline_versions_in_tracked_files": True},
                "selected_catalog_required_properties": {},
                "selected_catalog_required_packages": {},
            })
        self.assertIn("must be an object", str(ctx.exception))

        with self.assertRaises(dg.GraphError) as ctx:
            self._load({
                "owner_checks": {"no_inline_versions_in_tracked_files": {}},
                "selected_catalog_required_properties": {},
                "selected_catalog_required_packages": {},
            })
        self.assertIn("extensions", str(ctx.exception))

    def test_owner_checks_guarded_imports_must_be_non_empty_object(self) -> None:
        with self.assertRaises(dg.GraphError) as ctx:
            self._load({
                "owner_checks": {"guarded_imports": {}},
                "selected_catalog_required_properties": {},
                "selected_catalog_required_packages": {},
            })
        self.assertIn("must be a non-empty object", str(ctx.exception))

    def test_landed_task2_seed_is_non_executable_delayed_activation_only(self) -> None:
        policy = json.loads((ROOT / "eng" / "dependency-graph-policy.json").read_text(encoding="utf-8-sig"))
        for row in policy["module_build_registry"].values():
            del row["restore_argv"]
            del row["build_argv"]
        for name in (
            "max_workflow_closure_depth",
            "max_workflow_closure_sources",
            "max_workflow_source_blob_bytes",
            "max_workflow_source_total_bytes",
        ):
            del policy["resource_limits"][name]
        raw = json.dumps(policy).encode("utf-8")

        with self.assertRaises(dg.GraphError):
            dg.load_policy_bytes(raw, "strict candidate")

        migrated = dg.load_policy_bytes(raw, "immutable base", allow_legacy_registry=True)
        self.assertIs(migrated["__legacy_registry_migration__"], True)
        self.assertEqual(
            migrated["module_build_registry"]["github.com/hexalith/hexalith.frontcomposer"]["restore_argv"][:2],
            ["dotnet", "restore"],
        )

    def test_landed_dependency_build_targets_are_complete_owned_surfaces(self) -> None:
        policy = dg.load_policy(ROOT / "eng" / "dependency-graph-policy.json")
        expected = {
            "github.com/hexalith/hexalith.frontcomposer": "Hexalith.FrontComposer.slnx",
            "github.com/hexalith/hexalith.eventstore": "Hexalith.EventStore.slnx",
            "github.com/hexalith/hexalith.tenants": "Hexalith.Tenants.Standalone.slnx",
            "github.com/hexalith/hexalith.commons": "Hexalith.Commons.Standalone.slnx",
            "github.com/hexalith/hexalith.builds": "Hexalith.Builds.slnx",
            "github.com/hexalith/hexalith.polymorphicserializations": "Hexalith.PolymorphicSerializations.slnx",
            "github.com/hexalith/hexalith.memories": "Hexalith.Memories.slnx",
            "github.com/hexalith/hexalith.parties": "Hexalith.Parties.Standalone.slnx",
        }

        for identity, target in expected.items():
            row = policy["module_build_registry"][identity]
            self.assertEqual(row["solution"], target)
            self.assertEqual(row["restore_argv"], [
                "dotnet", "restore", target, "-p:Configuration=Release", "-p:UseNuGetDeps=true",
            ])
            self.assertEqual(row["build_argv"], [
                "dotnet", "build", target, "--configuration", "Release", "--no-restore",
                "-p:UseNuGetDeps=true",
            ])

    @staticmethod
    def _replace_catalog_property_element(catalog: bytes, property_name: str, replacement: bytes) -> bytes:
        start = catalog.index(f"<{property_name}".encode("ascii"))
        closing = f"</{property_name}>".encode("ascii")
        end = catalog.index(closing, start) + len(closing)
        return catalog[:start] + replacement + catalog[end:]

    @classmethod
    def _replace_catalog_property_value(cls, catalog: bytes, property_name: str, value: str) -> bytes:
        start = catalog.index(f"<{property_name}".encode("ascii"))
        opening_end = catalog.index(b">", start) + 1
        closing_start = catalog.index(f"</{property_name}>".encode("ascii"), opening_end)
        return catalog[:opening_end] + value.encode("ascii") + catalog[closing_start:]

    @classmethod
    def _frontcomposer_catalog(cls) -> bytes:
        # These synthetic tests isolate the unchanged owner policy from external
        # catalog version drift before mutating each intended check. They do not
        # assert that the actually selected catalog passes owner governance.
        catalog = (ROOT / "references/Hexalith.Builds/Props/Directory.Packages.props").read_bytes()
        for package_id, expected in cls._frontcomposer_profile()["selected_catalog_required_packages"].items():
            pattern = rb'(<PackageVersion Include="' + re.escape(package_id.encode()) + rb'" Version=")[^"]+(" />)'
            catalog, count = re.subn(pattern, lambda match: match[1] + expected.encode() + match[2], catalog)
            if count != 1:
                raise AssertionError(f"Fixture requires one unconditional package row for {package_id}")
        return catalog

    def _evaluate_frontcomposer_profile(self, catalog: bytes, profile_name: str = "frontcomposer-catalog-v1") -> dict:
        with tempfile.TemporaryDirectory(dir=self.tmp_path) as fixture_directory:
            fx = GraphFixture(pathlib.Path(fixture_directory))

            frontcomposer = fx.add_repo("FrontComposer")
            frontcomposer.write_text("Directory.Packages.props", OWNER_SHIM)

            builds = fx.add_repo("Builds")
            builds.write_text(".gitattributes", "Props/Directory.Packages.props text eol=crlf\n")
            builds.write_bytes("Props/Directory.Packages.props", catalog)
            builds_commit = builds.commit()

            fx.link("FrontComposer", "references/Hexalith.Builds", "Builds", builds_commit)
            frontcomposer_commit = frontcomposer.commit()
            frontcomposer_identity = fx.identity("FrontComposer")
            fx.policy["semantic_profiles"][frontcomposer_identity] = profile_name
            fx.policy["profiles"][profile_name] = self._frontcomposer_profile()

            envelope = dg.collect_graph(
                frontcomposer.root,
                frontcomposer_identity,
                frontcomposer_commit,
                fx.policy,
            )
            return dg.evaluate_semantics(frontcomposer.root, fx.policy, envelope)

    def _assert_frontcomposer_profile_failure(self, catalog: bytes, expected_diagnostic: str) -> str:
        with self.assertRaises(dg.GraphError) as ctx:
            self._evaluate_frontcomposer_profile(catalog)

        message = str(ctx.exception)
        self.assertIn("github.com/test/frontcomposer@", message)
        self.assertIn("references/Hexalith.Builds", message)
        self.assertIn("github.com/test/builds@", message)
        self.assertIn(expected_diagnostic, message)
        return message

    def test_frontcomposer_profile_shape_contract_is_closed_and_value_independent(self) -> None:
        profile = self._frontcomposer_profile()
        property_names = [
            "HexalithCommonsVersion",
            "HexalithEventStoreVersion",
            "HexalithMemoriesVersion",
            "HexalithPartiesVersion",
            "HexalithPolymorphicSerializationsVersion",
            "HexalithTenantsVersion",
        ]
        self.assertEqual(
            set(profile),
            {
                "owner_checks",
                "selected_catalog_required_property_names",
                "selected_catalog_required_properties",
                "selected_catalog_required_packages",
            },
        )
        self.assertEqual(profile["selected_catalog_required_property_names"], property_names)
        self.assertEqual(profile["selected_catalog_required_properties"], {})
        dg.assert_profiles_well_formed({"profiles": {"frontcomposer-catalog-v1": profile}})

    @staticmethod
    def _catalog_xml_bytes(root: ET.Element) -> bytes:
        return b"\xef\xbb\xbf" + ET.tostring(root, encoding="utf-8").replace(b"\n", b"\r\n")

    def _eventstore_qualification_catalog(self) -> bytes:
        # Preserve the temporary upstream shape as a regression fixture after its removal.
        profile = self._frontcomposer_profile()
        root = ET.Element("Project")
        properties = ET.SubElement(root, "PropertyGroup")
        for name in profile["selected_catalog_required_property_names"]:
            ET.SubElement(properties, name, {"Condition": f"'$({name})' == ''"}).text = "1.2.3"
        condition = (
            "'$(CatalogCandidateTenantsVersion)' != '' and '$(UseHexalithProjectReferences)' != 'true' and ("
            "'$(MSBuildProjectName)' == 'Hexalith.EventStore.CatalogConstruction' or "
            "'$(MSBuildProjectName)' == 'Hexalith.EventStore.Server.Tests' or "
            "'$(MSBuildProjectName)' == 'Hexalith.EventStore.QueryRouting.Tests' or "
            "'$(MSBuildProjectName)' == 'Hexalith.EventStore.Server.LiveSidecar.Tests' or "
            "'$(MSBuildProjectName)' == 'routing-catalog.cs')"
        )
        ET.SubElement(properties, "HexalithTenantsVersion", {"Condition": condition}).text = "$(CatalogCandidateTenantsVersion)"
        packages = ET.SubElement(root, "ItemGroup")
        for name, version in profile["selected_catalog_required_packages"].items():
            ET.SubElement(packages, "PackageVersion", {"Include": name, "Version": version})
        ET.indent(root)
        return self._catalog_xml_bytes(root)

    def test_frontcomposer_profile_accepts_exact_inactive_eventstore_qualification(self) -> None:
        catalog = self._eventstore_qualification_catalog()
        self.assertEqual(self._evaluate_frontcomposer_profile(catalog)["selectors_validated"], 1)
        root = ET.fromstring(catalog)
        default, qualification = list(root.iter("HexalithTenantsVersion"))
        next(group for group in root if qualification in list(group)).remove(qualification)
        self.assertEqual(self._evaluate_frontcomposer_profile(self._catalog_xml_bytes(root))["selectors_validated"], 1)

    def test_eventstore_qualification_remains_strict_for_other_profiles(self) -> None:
        catalog = self._eventstore_qualification_catalog()
        with self.assertRaisesRegex(dg.GraphError, "must define exactly one version property"):
            self._evaluate_frontcomposer_profile(catalog, profile_name="strict-catalog-v1")
        with self.assertRaisesRegex(dg.GraphError, "must define exactly one version property"):
            dg.assert_selected_catalog_property_shape(ET.fromstring(catalog), "HexalithTenantsVersion", "strict helper")

    def test_frontcomposer_profile_rejects_changed_qualification_scope_or_value(self) -> None:
        catalog = self._eventstore_qualification_catalog()
        mutations = [
            (b"'$(UseHexalithProjectReferences)' != 'true' and ", b""),
            (b"'$(CatalogCandidateTenantsVersion)' != ''", b"'$(OtherCandidate)' != ''"),
            (b"'routing-catalog.cs')", b"'routing-catalog.cs' or '$(MSBuildProjectName)' == 'Hexalith.FrontComposer.Shell')"),
            (b">$(CatalogCandidateTenantsVersion)</HexalithTenantsVersion>", b">5.10.2</HexalithTenantsVersion>"),
            (b">$(CatalogCandidateTenantsVersion)</HexalithTenantsVersion>", b">$(OtherVersion)</HexalithTenantsVersion>"),
            (b">$(CatalogCandidateTenantsVersion)</HexalithTenantsVersion>", b">$(CatalogCandidateTenantsVersion)<Unexpected /></HexalithTenantsVersion>"),
        ]
        for original, replacement in mutations:
            with self.subTest(mutation=replacement):
                self.assertEqual(catalog.count(original), 1)
                self._assert_frontcomposer_profile_failure(
                    catalog.replace(original, replacement, 1),
                    "HexalithTenantsVersion must define exactly one version property",
                )

    def test_frontcomposer_profile_rejects_scoped_or_duplicate_qualification(self) -> None:
        catalog = self._eventstore_qualification_catalog()
        for placement in ("conditional", "target", "choose", "duplicate"):
            with self.subTest(placement=placement):
                root = ET.fromstring(catalog)
                default, qualification = list(root.iter("HexalithTenantsVersion"))
                parent = next(group for group in root if qualification in list(group))
                if placement == "duplicate":
                    parent.append(copy.deepcopy(qualification))
                else:
                    parent.remove(qualification)
                    if placement == "conditional":
                        group = ET.SubElement(root, "PropertyGroup", {"Condition": "'$(Other)' == 'true'"})
                    elif placement == "target":
                        group = ET.SubElement(ET.SubElement(root, "Target", {"Name": "Later"}), "PropertyGroup")
                    else:
                        group = ET.SubElement(ET.SubElement(ET.SubElement(root, "Choose"), "When", {"Condition": "'$(Other)' == 'true'"}), "PropertyGroup")
                    group.append(qualification)
                self._assert_frontcomposer_profile_failure(
                    self._catalog_xml_bytes(root),
                    "HexalithTenantsVersion must define exactly one version property",
                )

    def test_frontcomposer_profile_qualification_retains_literal_default_validation(self) -> None:
        catalog = self._eventstore_qualification_catalog()
        self._assert_frontcomposer_profile_failure(
            self._replace_catalog_property_value(catalog, "HexalithTenantsVersion", "$(OtherVersion)"),
            "HexalithTenantsVersion must contain a literal NuGet version",
        )
        for duplicate in (True, False):
            with self.subTest(duplicate=duplicate):
                root = ET.fromstring(catalog)
                default, qualification = list(root.iter("HexalithTenantsVersion"))
                parent = next(group for group in root if default in list(group))
                if duplicate:
                    parent.append(copy.deepcopy(default))
                else:
                    parent.remove(default)
                self._assert_frontcomposer_profile_failure(
                    self._catalog_xml_bytes(root),
                    "HexalithTenantsVersion must define exactly one version property",
                )

    def test_frontcomposer_profile_cross_module_versions_change_semantic_validation_passes(self) -> None:
        property_names = (
            "HexalithCommonsVersion",
            "HexalithEventStoreVersion",
            "HexalithMemoriesVersion",
            "HexalithPartiesVersion",
            "HexalithPolymorphicSerializationsVersion",
            "HexalithTenantsVersion",
        )
        catalog_path = ROOT / "references/Hexalith.Builds/Props/Directory.Packages.props"
        catalog = self._frontcomposer_catalog()
        for index, name in enumerate(property_names, start=1):
            with self.subTest(property_name=name):
                mutated_catalog = self._replace_catalog_property_value(
                    catalog,
                    name,
                    f"999.0.{index}",
                )
                semantics = self._evaluate_frontcomposer_profile(mutated_catalog)
                self.assertEqual(semantics["selectors_validated"], 1)

    def test_frontcomposer_profile_missing_module_property_fails_with_owner_and_catalog_coordinates(self) -> None:
        catalog = self._frontcomposer_catalog()
        property_names = self._frontcomposer_profile()["selected_catalog_required_property_names"]
        self.assertEqual(len(property_names), 6)
        for property_name in property_names:
            with self.subTest(property_name=property_name):
                mutated_catalog = self._replace_catalog_property_element(catalog, property_name, b"")
                self._assert_frontcomposer_profile_failure(
                    mutated_catalog,
                    f"{property_name} must define exactly one version property",
                )

    def test_frontcomposer_profile_duplicate_module_property_fails_with_owner_and_catalog_coordinates(self) -> None:
        catalog = self._frontcomposer_catalog()
        property_name = "HexalithMemoriesVersion"
        start = catalog.index(f"<{property_name}".encode("ascii"))
        closing = f"</{property_name}>".encode("ascii")
        end = catalog.index(closing, start) + len(closing)
        element = catalog[start:end]
        mutated_catalog = self._replace_catalog_property_element(
            catalog,
            property_name,
            element + b"\r\n    " + element,
        )
        message = self._assert_frontcomposer_profile_failure(
            mutated_catalog,
            f"{property_name} must define exactly one version property",
        )
        self.assertIn("found 2 values", message)

    def test_frontcomposer_profile_empty_module_property_fails_with_owner_and_catalog_coordinates(self) -> None:
        catalog = self._frontcomposer_catalog()
        property_name = "HexalithMemoriesVersion"
        mutated_catalog = self._replace_catalog_property_value(catalog, property_name, "")
        self._assert_frontcomposer_profile_failure(
            mutated_catalog,
            f"{property_name} must contain a literal NuGet version, found ''",
        )

    def test_frontcomposer_profile_malformed_module_property_fails_with_owner_and_catalog_coordinates(self) -> None:
        catalog = self._frontcomposer_catalog()
        property_name = "HexalithMemoriesVersion"
        mutated_catalog = self._replace_catalog_property_value(catalog, property_name, "$(OtherVersion)")
        self._assert_frontcomposer_profile_failure(
            mutated_catalog,
            f"{property_name} must contain a literal NuGet version",
        )

    def test_frontcomposer_profile_nested_module_property_value_fails_with_owner_and_catalog_coordinates(self) -> None:
        catalog = self._frontcomposer_catalog()
        property_name = "HexalithMemoriesVersion"
        start = catalog.index(f"<{property_name}".encode("ascii"))
        opening_end = catalog.index(b">", start) + 1
        closing = f"</{property_name}>".encode("ascii")
        replacement = catalog[start:opening_end] + b"1.2.3<Unexpected />" + closing
        mutated_catalog = self._replace_catalog_property_element(catalog, property_name, replacement)
        self._assert_frontcomposer_profile_failure(
            mutated_catalog,
            f"{property_name} must contain a literal NuGet version",
        )

    def test_frontcomposer_profile_noncanonical_condition_fails_with_owner_and_catalog_coordinates(self) -> None:
        catalog = self._frontcomposer_catalog()
        property_name = "HexalithMemoriesVersion"
        canonical = f"Condition=\"'$({property_name})' == ''\"".encode("ascii")
        noncanonical = b"Condition=\"'$(OtherVersion)' == ''\""
        self.assertEqual(catalog.count(canonical), 1)
        mutated_catalog = catalog.replace(canonical, noncanonical, 1)
        self._assert_frontcomposer_profile_failure(mutated_catalog, "canonical self-default condition")

    def test_frontcomposer_profile_unconditional_module_property_passes(self) -> None:
        catalog = self._frontcomposer_catalog()
        property_name = "HexalithMemoriesVersion"
        canonical = f" Condition=\"'$({property_name})' == ''\"".encode("ascii")
        self.assertEqual(catalog.count(canonical), 1)
        mutated_catalog = catalog.replace(canonical, b"", 1)
        semantics = self._evaluate_frontcomposer_profile(mutated_catalog)
        self.assertEqual(semantics["selectors_validated"], 1)

    def test_frontcomposer_profile_conditional_ancestor_fails_with_owner_and_catalog_coordinates(self) -> None:
        catalog = self._frontcomposer_catalog()
        property_name = "HexalithMemoriesVersion"
        start = catalog.index(f"<{property_name}".encode("ascii"))
        closing = f"</{property_name}>".encode("ascii")
        end = catalog.index(closing, start) + len(closing)
        element = catalog[start:end]
        without_property = self._replace_catalog_property_element(catalog, property_name, b"")
        conditional_group = (
            b"  <PropertyGroup Condition=\"'$(Configuration)' == 'Release'\">\r\n"
            b"    " + element + b"\r\n"
            b"  </PropertyGroup>\r\n"
        )
        mutated_catalog = without_property.replace(b"</Project>", conditional_group + b"</Project>", 1)
        self._assert_frontcomposer_profile_failure(mutated_catalog, "conditional group")

    def test_frontcomposer_profile_choose_selected_property_fails_with_owner_and_catalog_coordinates(self) -> None:
        catalog = self._frontcomposer_catalog()
        property_name = "HexalithMemoriesVersion"
        start = catalog.index(f"<{property_name}".encode("ascii"))
        closing = f"</{property_name}>".encode("ascii")
        end = catalog.index(closing, start) + len(closing)
        element = catalog[start:end]
        without_property = self._replace_catalog_property_element(catalog, property_name, b"")
        choose = (
            b"  <Choose>\r\n"
            b"    <When Condition=\"'$(Configuration)' == 'Release'\">\r\n"
            b"      <PropertyGroup>\r\n"
            b"        " + element + b"\r\n"
            b"      </PropertyGroup>\r\n"
            b"    </When>\r\n"
            b"  </Choose>\r\n"
        )
        mutated_catalog = without_property.replace(b"</Project>", choose + b"</Project>", 1)
        self._assert_frontcomposer_profile_failure(mutated_catalog, "Choose branch")

    def test_frontcomposer_profile_target_scoped_property_fails_as_nonglobal(self) -> None:
        catalog = self._frontcomposer_catalog()
        property_name = "HexalithMemoriesVersion"
        start = catalog.index(f"<{property_name}".encode("ascii"))
        closing = f"</{property_name}>".encode("ascii")
        end = catalog.index(closing, start) + len(closing)
        element = catalog[start:end]
        without_property = self._replace_catalog_property_element(catalog, property_name, b"")
        target = (
            b'  <Target Name="SetCatalogVersion">\r\n'
            b"    <PropertyGroup>\r\n"
            b"      " + element + b"\r\n"
            b"    </PropertyGroup>\r\n"
            b"  </Target>\r\n"
        )
        mutated_catalog = without_property.replace(b"</Project>", target + b"</Project>", 1)
        self._assert_frontcomposer_profile_failure(
            mutated_catalog,
            "must be a global property declared directly in a top-level PropertyGroup",
        )

    def test_frontcomposer_profile_required_package_changes_semantic_validation_fails_closed(self) -> None:
        profile = self._frontcomposer_profile()
        package_id = "ModelContextProtocol.AspNetCore"
        expected_version = profile["selected_catalog_required_packages"][package_id]
        catalog_path = ROOT / "references/Hexalith.Builds/Props/Directory.Packages.props"
        catalog = self._frontcomposer_catalog()
        expected_row = f'<PackageVersion Include="{package_id}" Version="{expected_version}" />'.encode("utf-8")
        changed_row = f'<PackageVersion Include="{package_id}" Version="999.0.0" />'.encode("utf-8")
        self.assertEqual(catalog.count(expected_row), 1)

        with self.assertRaises(dg.GraphError) as ctx:
            self._evaluate_frontcomposer_profile(catalog.replace(expected_row, changed_row, 1))

        message = str(ctx.exception)
        self.assertIn(package_id, message)
        self.assertIn(f"expected version {expected_version!r}", message)
        self.assertIn("found '999.0.0'", message)

    @staticmethod
    def _sibling_profile(profile_name: str) -> dict:
        policy = json.loads(
            (ROOT / "eng" / "dependency-graph-policy.json").read_text(encoding="utf-8-sig")
        )
        return copy.deepcopy(policy["profiles"][profile_name])

    def test_sibling_profiles_are_presence_only_for_required_packages(self) -> None:
        expected = {
            "eventstore-catalog-v1": [
                "Microsoft.Extensions.TimeProvider.Testing",
                "ModelContextProtocol",
                "System.CommandLine",
            ],
            "memories-catalog-v1": [
                "Microsoft.Extensions.TimeProvider.Testing",
                "ModelContextProtocol.AspNetCore",
            ],
            "parties-catalog-v1": [
                "Microsoft.AspNetCore.Components.CustomElements",
                "ModelContextProtocol",
                "ModelContextProtocol.AspNetCore",
            ],
        }
        for profile_name, package_names in expected.items():
            with self.subTest(profile=profile_name):
                profile = self._sibling_profile(profile_name)
                self.assertIn("selected_catalog_required_package_names", profile)
                self.assertEqual(profile["selected_catalog_required_package_names"], package_names)
                self.assertEqual(profile["selected_catalog_required_packages"], {})
                dg.assert_profiles_well_formed({"profiles": {profile_name: profile}})

    def _evaluate_eventstore_profile(self, catalog: bytes) -> dict:
        profile_name = "eventstore-catalog-v1"
        with tempfile.TemporaryDirectory(dir=self.tmp_path) as fixture_directory:
            fx = GraphFixture(pathlib.Path(fixture_directory))

            eventstore = fx.add_repo("EventStore")
            eventstore.write_text("Directory.Packages.props", OWNER_SHIM)

            builds = fx.add_repo("Builds")
            builds.write_bytes("Props/Directory.Packages.props", catalog)
            builds_commit = builds.commit()

            fx.link("EventStore", "references/Hexalith.Builds", "Builds", builds_commit)
            eventstore_commit = eventstore.commit()
            eventstore_identity = fx.identity("EventStore")
            fx.policy["semantic_profiles"][eventstore_identity] = profile_name
            fx.policy["profiles"][profile_name] = self._sibling_profile(profile_name)

            envelope = dg.collect_graph(
                eventstore.root,
                eventstore_identity,
                eventstore_commit,
                fx.policy,
            )
            return dg.evaluate_semantics(eventstore.root, fx.policy, envelope)

    def test_eventstore_profile_system_commandline_version_may_move(self) -> None:
        catalog_path = ROOT / "references/Hexalith.Builds/Props/Directory.Packages.props"
        catalog = self._frontcomposer_catalog()
        current_version = self._catalog_package_version(catalog, "System.CommandLine")
        current_row = f'<PackageVersion Include="System.CommandLine" Version="{current_version}" />'.encode("ascii")
        moved_row = b'<PackageVersion Include="System.CommandLine" Version="999.0.0" />'
        self.assertEqual(catalog.count(current_row), 1)
        semantics = self._evaluate_eventstore_profile(catalog.replace(current_row, moved_row, 1))
        self.assertEqual(semantics["selectors_validated"], 1)

    def test_eventstore_profile_missing_system_commandline_fails_with_coordinates(self) -> None:
        catalog_path = ROOT / "references/Hexalith.Builds/Props/Directory.Packages.props"
        catalog = self._frontcomposer_catalog()
        current_version = self._catalog_package_version(catalog, "System.CommandLine")
        current_row = f'<PackageVersion Include="System.CommandLine" Version="{current_version}" />'.encode("ascii")
        self.assertEqual(catalog.count(current_row), 1)
        catalog = catalog.replace(current_row, b"", 1)

        with self.assertRaises(dg.GraphError) as ctx:
            self._evaluate_eventstore_profile(catalog)
        message = str(ctx.exception)
        self.assertIn("github.com/test/eventstore@", message)
        self.assertIn("references/Hexalith.Builds", message)
        self.assertIn("github.com/test/builds@", message)
        self.assertIn("System.CommandLine", message)

    def _catalog_package_version(self, catalog: bytes, package_id: str) -> str:
        document = ET.fromstring(catalog.decode("utf-8-sig"))
        versions = [
            element.attrib["Version"]
            for element in document.iter("PackageVersion")
            if element.attrib.get("Include") == package_id
        ]
        self.assertEqual(len(versions), 1, f"{package_id} PackageVersion row missing or duplicated in selected catalog fixture")
        return versions[0]


class ConsumerProjectionTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.fixture = GraphFixture(pathlib.Path(self.temp.name))
        fx = self.fixture
        identities = {"Root": "github.com/hexalith/hexalith.frontcomposer",
                      "Builds": "github.com/hexalith/hexalith.builds",
                      "Source": "github.com/hexalith/hexalith.platform"}
        fx.identity = lambda name: identities[name]
        fx.url = lambda name: "https://" + identities[name] + ".git"
        fx.policy["builds_identity"] = identities["Builds"]
        self.root = fx.add_repo("Root")
        fx.policy["trusted_identities"][0]["local_path"] = "."
        self.root.write_text(".gitignore", "references/\n")
        for name in ("Builds", "Source"):
            repo = TempGitRepo(self.root.root / "references" / name)
            fx.repos[name] = repo
            fx.policy["trusted_identities"].append({"identity": fx.identity(name), "local_path": "references/" + name})
        self.builds = fx.repos["Builds"]
        self.builds.write_bytes("Props/Directory.Packages.props", BASELINE_CATALOG)
        self.builds.write_text("src/Tooling.csproj", "<Project />\n")
        self.nested_builds = self.builds.commit()
        self.builds.write_bytes("Props/Directory.Packages.props", BASELINE_CATALOG.replace(b'1.0.0', b'2.0.0'))
        self.root_builds = self.builds.commit()
        self.source = fx.repos["Source"]
        self.source.write_text("Directory.Packages.props", OWNER_SHIM)
        for name in ("One.cs", "Two.cs"):
            self.source.write_text(name, "// unchanged canonical " + name + "\n")
        fx.link("Source", "references/Builds", "Builds", self.nested_builds)
        self.untrusted = []
        for number in range(7):
            identity = f"github.com/untrusted/inventory{number}"
            self.untrusted.append(identity)
            self.source.add_submodule("Inventory" + str(number), "references/Inventory" + str(number),
                                      "https://" + identity + ".git", f"{number + 1:040x}")
        self.owner = self.source.commit()
        self.root.write_text("Directory.Packages.props", OWNER_SHIM)
        self.root.write_text("Root.slnx", "<Solution />\n")
        self.root.write_text("global.json", '{"sdk":{"version":"10.0.402"}}\n')
        self.root.write_text("nuget.config", "<configuration />\n")
        self.project = "src/AppHost.csproj"
        self.project_xml = ('<Project><ItemGroup>'
            '<Compile Include="$(CanonicalSourceRoot)/One.cs" Link="Canonical/One.cs" />'
            '<Compile Include="$(CanonicalSourceRoot)/Two.cs" Link="Canonical/Two.cs" />'
            '<ProjectReference Include="$(HexalithBuildsRoot)/src/Tooling.csproj" IsAspireProjectResource="false" />'
            '</ItemGroup></Project>\n')
        self.root.write_text(self.project, self.project_xml)
        fx.link("Root", "references/Source", "Source", self.owner)
        fx.link("Root", "references/Builds", "Builds", self.root_builds)
        self.commit = self.root.commit()
        for name in ("Root", "Builds", "Source"):
            fx.policy["semantic_profiles"][fx.identity(name)] = "fixture"
            fx.register_module(name, evidence_only=name == "Source")
        fx.policy["profiles"]["fixture"] = {"owner_checks": {}, "selected_catalog_required_properties": {},
                                               "selected_catalog_required_packages": {}}
        raw = dg.read_gitmodules(self.source.root, self.owner, 1048576)
        links = dg.list_gitlinks(self.source.root, self.owner, 67108864)
        by_path = {entry["path"]: dg.normalize_identity(entry["url"], "fixture") for entry in raw.values()}
        inventory = [{"owner_repository": fx.identity("Source"), "owner_commit": self.owner,
                      "path": path, "repository": by_path[path], "commit": sha, "depth": 2}
                     for path, sha in sorted(links.items())]
        direct = dg._owner_edges(self.root.root, self.commit, fx.identity("Root"), 1, fx.policy, self.root.root)
        row = {"owner_commit": self.owner,
               "gitmodules_sha256": hashlib.sha256(dg.read_blob(self.source.root, self.owner, ".gitmodules", 1048576, "fixture")).hexdigest(),
               "inventory_sha256": dg.canonical_digest(inventory),
               "inventory_only_identities": self.untrusted,
               "source_files": {name: hashlib.sha256(dg.read_blob(self.source.root, self.owner, name, 1048576, "fixture")).hexdigest()
                                for name in ("One.cs", "Two.cs")},
               "consumer_repository": fx.identity("Root"), "consumer_project": self.project,
               "source_root_property": "CanonicalSourceRoot",
               "compile_links": {name: "Canonical/" + name for name in ("One.cs", "Two.cs")},
               "builds_commit": self.root_builds, "builds_project": "src/Tooling.csproj",
               "root_selectors_sha256": dg.canonical_digest([{key: edge[key] for key in ("path", "repository", "commit")} for edge in direct]),
               "consumer_configuration_sha256": dg.canonical_digest(dg.consumer_configuration(self.root.root, self.commit, fx.policy))}
        fx.policy["consumer_source_projections"] = {fx.identity("Source"): row}
        self.row = row
        dg.assert_policy_well_formed(fx.policy)

    def graph(self, commit: str | None = None) -> dict:
        return dg.collect_graph(self.root.root, self.fixture.identity("Root"), commit or self.commit, self.fixture.policy)

    @staticmethod
    def resign(graph: dict) -> None:
        graph["edge_count"] = len(graph["edges"])
        graph["edges"].sort(key=lambda edge: tuple(edge[key] for key in ("depth", "owner_repository", "owner_commit", "path", "repository", "commit")))
        graph["graph_digest"] = dg.canonical_digest({key: graph[key] for key in ("schema", "root", "edge_count", "edges")})

    def test_raw_inventory_and_historical_catalog_stay_visible_without_global_trust(self) -> None:
        graph = self.graph()
        self.assertEqual(graph["edge_count"], 10)
        self.assertEqual(dg.validate_graph_envelope(graph), graph)
        self.assertEqual(sum(edge["repository"] in self.untrusted for edge in graph["edges"]), 7)
        nested = next(edge for edge in graph["edges"] if edge["depth"] == 2 and edge["repository"] == self.fixture.identity("Builds"))
        self.assertEqual(nested["commit"], self.nested_builds)
        self.assertEqual(nested["catalog_sha256"], hashlib.sha256(BASELINE_CATALOG).hexdigest())
        report = dg.evaluate_semantics(self.root.root, self.fixture.policy, graph)["consumer_source_projections"][self.fixture.identity("Source")]
        self.assertEqual(report["root_builds_commit"], self.root_builds)
        self.assertNotEqual(report["root_builds_commit"], nested["commit"])
        for identity in self.untrusted:
            with self.assertRaisesRegex(dg.GraphError, "unknown/untrusted"):
                dg.resolve_local_path(self.fixture.policy, identity, self.root.root)

    def test_projection_policy_has_closed_shapes_and_cannot_promote_global_trust(self) -> None:
        for field, value in (("inventory_sha256", "bad"), ("inventory_only_identities", [True]),
                             ("inventory_only_identities", [self.fixture.identity("Builds")]),
                             ("source_files", {"../outside.cs": "0" * 64, "Two.cs": "0" * 64}),
                             ("unexpected", True)):
            policy = copy.deepcopy(self.fixture.policy)
            policy["consumer_source_projections"][self.fixture.identity("Source")][field] = value
            with self.subTest(field=field, value=value), self.assertRaises(dg.GraphError):
                dg.assert_policy_well_formed(policy)

    def test_seal_changes_and_unknown_root_identity_refuse(self) -> None:
        for field in ("gitmodules_sha256", "inventory_sha256"):
            with self.subTest(field=field), mock.patch.dict(self.row, {field: "0" * 64}):
                with self.assertRaises(dg.GraphError): self.graph()
        with mock.patch.dict(self.row["source_files"], {"One.cs": "0" * 64}):
            with self.assertRaisesRegex(dg.GraphError, "canonical source"):
                self.graph()
        self.root.add_submodule("Untrusted", "references/Outside", "https://" + self.untrusted[0] + ".git", "1" * 40)
        changed = self.root.commit()
        with self.assertRaisesRegex(dg.GraphError, "unknown/untrusted"):
            self.graph(changed)

    def test_projection_owner_cannot_become_a_buildable_umbrella(self) -> None:
        registry = self.fixture.policy["module_build_registry"][self.fixture.identity("Source")]
        with mock.patch.dict(registry, {"disposition": "build"}), self.assertRaisesRegex(dg.GraphError, "whole-owner execution"):
            dg.assert_policy_well_formed(self.fixture.policy)
        with mock.patch.dict(registry, {"disposition": "build"}), self.assertRaisesRegex(dg.GraphError, "whole-owner execution"):
            self.graph()

    def test_materialization_contains_exactly_the_two_canonical_compile_files(self) -> None:
        destination = pathlib.Path(self.temp.name) / "consumer"
        destination.mkdir()
        files = dg.materialize_consumer_sources(self.root.root, self.fixture.policy, self.graph(), destination,
                                               self.fixture.identity("Root"), self.commit)
        self.assertEqual(files, ["references/Source/One.cs", "references/Source/Two.cs"])
        self.assertEqual(sorted(str(path.relative_to(destination)) for path in destination.rglob("*") if path.is_file()), files)
        self.assertFalse((destination / "references/Source/references").exists())

    def test_changed_owner_pin_refuses_before_source_consumption(self) -> None:
        self.source.write_text("README.md", "different owner tree\n")
        changed_owner = self.source.commit()
        self.root.link_gitlink("references/Source", changed_owner)
        changed = self.root.commit()
        with self.assertRaisesRegex(dg.GraphError, "owner pin changed"):
            self.graph(changed)

    def test_added_or_indirect_compile_and_project_reference_consumption_refuse(self) -> None:
        for item in ('<Compile Include="$(CanonicalSourceRoot)/Other.cs" />',
                     '<ProjectReference Include="$(CanonicalSourceRoot)/Other.csproj" />'):
            with self.subTest(item=item):
                self.root.write_text(self.project, self.project_xml.replace('</ItemGroup>', item + '</ItemGroup>'))
                changed = self.root.commit()
                with self.assertRaisesRegex(dg.GraphError, "Compile/ProjectReference configuration changed"):
                    dg.evaluate_consumer_projections(self.root.root, self.fixture.policy, self.graph(changed))
        self.root.write_text(self.project, self.project_xml)
        restored = self.root.commit()
        dg.evaluate_consumer_projections(self.root.root, self.fixture.policy, self.graph(restored))
        self.root.write_text("another.csproj", '<Project><ItemGroup><Compile Include="$(Alias)/**/*.cs" /></ItemGroup></Project>')
        changed = self.root.commit()
        with self.assertRaisesRegex(dg.GraphError, "Compile/ProjectReference configuration changed"):
            dg.evaluate_consumer_projections(self.root.root, self.fixture.policy, self.graph(changed))

    def test_automatic_user_imports_and_response_inputs_refuse_before_execution(self) -> None:
        # Microsoft.Common.CurrentVersion.targets imports $(MSBuildProjectFullPath).user;
        # MSBuild also consumes automatic directory/tool response files. None of these
        # requires a change to the already sealed consumer project.
        inputs = {
            self.project + ".user": '<Project><ItemGroup><Compile Include="$(CanonicalSourceRoot)/Other.cs" /></ItemGroup></Project>\n',
            "Directory.Build.rsp": '-p:CustomAfterMicrosoftCommonTargets=references/Source/Other.targets\n',
            "MSBuild.rsp": '-p:CustomAfterMicrosoftCommonTargets=references/Source/Other.targets\n',
            "src/Directory.Build.rsp": '-p:CustomAfterMicrosoftCommonTargets=references/Source/Other.targets\n',
        }
        original_project = dg.read_blob(self.root.root, self.commit, self.project, 1048576, "fixture")
        for path, content in inputs.items():
            with self.subTest(path=path):
                self.root.write_text(path, content)
                changed = self.root.commit()
                self.assertEqual(dg.read_blob(self.root.root, changed, self.project, 1048576, "fixture"), original_project)
                self.assertIn(path, dg.consumer_configuration(self.root.root, changed, self.fixture.policy))
                with mock.patch.object(dg.subprocess, "run", wraps=dg.subprocess.run) as invoked:
                    with self.assertRaisesRegex(dg.GraphError, "Compile/ProjectReference configuration changed"):
                        self.graph(changed)
                    self.assertTrue(all(call.args[0][0] == "git" for call in invoked.call_args_list))
                _run(["rm", "--", path], self.root.root)
                restored = self.root.commit()
                self.graph(restored)

    def test_committed_sdk_and_restore_configuration_changes_refuse(self) -> None:
        cases = {"global.json": '{"sdk":{"version":"10.0.999"}}\n',
                 "nuget.config": '<configuration><packageSources><clear /></packageSources></configuration>\n',
                 "NuGet.Config": '<configuration />\n', "src/NuGet.config": '<configuration />\n'}
        for path, content in cases.items():
            with self.subTest(path=path):
                original = dg.read_blob(self.root.root, self.commit, path, 1048576, "fixture")
                self.root.write_text(path, content)
                changed = self.root.commit()
                with self.assertRaisesRegex(dg.GraphError, "configuration changed"):
                    self.graph(changed)
                if original is None: _run(["rm", "--", path], self.root.root)
                else: self.root.write_bytes(path, original)
                restored = self.root.commit()
                self.graph(restored)
        for path in ("global.json", "nuget.config"):
            with self.subTest(removed=path):
                original = dg.read_blob(self.root.root, self.commit, path, 1048576, "fixture")
                _run(["rm", "--", path], self.root.root)
                removed = self.root.commit()
                with self.assertRaisesRegex(dg.GraphError, "configuration changed"):
                    self.graph(removed)
                self.root.write_bytes(path, original)
                self.graph(self.root.commit())

    def test_affected_build_commands_require_both_materialized_canonical_sources(self) -> None:
        # Execute the actual orchestration/materialization path; only the final
        # statically authorized command is simulated, after inspecting its files.
        self.fixture.register_module("Builds", evidence_only=True)
        commit = self.write_policy()
        graph = self.graph(commit)
        semantics = dg.evaluate_semantics(self.root.root, self.fixture.policy, graph)
        evidence = dg.diff_graphs(graph, graph, self.fixture.policy, event="push", event_base=commit,
                                  candidate=commit, merge_base=None, release_eligible=False, full_affected=True)
        _, _, coordinate = dg.load_policy_at_commit(self.root.root, commit)
        evidence.update({"dependency_policy": coordinate, "base_semantics": semantics, "candidate_semantics": semantics,
                         "diagnostic": "owned synthetic full-affected materialization regression"})
        evidence["evidence_digest"] = dg.canonical_digest({key: value for key, value in evidence.items() if key != "evidence_digest"})
        expected = {path: dg.read_blob(self.source.root, self.owner, path, 1048576, "fixture") for path in self.row["source_files"]}
        actual_run = dg.subprocess.run
        commands = []
        def execute(argv, **kwargs):
            if argv[0] == "git": return actual_run(argv, **kwargs)
            self.assertEqual(argv[0], "dotnet")
            source = pathlib.Path(kwargs["cwd"]) / "references/Source"
            self.assertEqual(sorted(path.name for path in source.iterdir()), sorted(expected))
            for path, content in expected.items(): self.assertEqual((source / path).read_bytes(), content)
            commands.append(argv)
            return subprocess.CompletedProcess(argv, 0, stdout="fixture verified canonical sources", stderr="")
        with mock.patch.object(dg.subprocess, "run", side_effect=execute):
            result = dg.run_affected_builds(self.root.root, evidence, self.root.root / "positive-output")
        self.assertEqual(len(commands), 2)
        self.assertEqual([argv[1] for argv in commands], ["restore", "build"])
        self.assertTrue(any(module["repository"] == self.fixture.identity("Root") and module["disposition"] == "build" for module in result["modules"]))

    def test_worktree_changes_never_replace_committed_consumer_configuration(self) -> None:
        graph = self.graph()
        self.root.write_text(self.project, self.project_xml.replace('One.cs', 'Unapproved.cs'))
        self.source.write_text("One.cs", "uncommitted foreign content\n")
        dg.evaluate_consumer_projections(self.root.root, self.fixture.policy, graph)

    def test_rehashed_forged_depth2_inventory_and_catalog_cannot_authenticate(self) -> None:
        for kind in ("target", "omitted", "extra", "catalog"):
            graph = copy.deepcopy(self.graph())
            edge = next(edge for edge in graph["edges"] if edge["depth"] == 2 and
                        (edge["repository"] == self.fixture.identity("Builds") if kind == "catalog" else edge["repository"] in self.untrusted))
            if kind == "target": edge["commit"] = "f" * 40
            elif kind == "omitted": graph["edges"].remove(edge)
            elif kind == "extra": graph["edges"].append({**edge, "path": "references/Extra"})
            else: edge["catalog_sha256"] = "0" * 64
            self.resign(graph)
            dg.validate_graph_envelope(graph)
            with self.subTest(kind=kind), self.assertRaises(dg.GraphError):
                dg.evaluate_consumer_projections(self.root.root, self.fixture.policy, graph)

    def test_rehashed_root_inventory_and_catalog_forgery_refuse_before_materialization(self) -> None:
        base = self.write_policy()
        graph = self.graph(base)
        semantics = dg.evaluate_semantics(self.root.root, self.fixture.policy, graph)
        _, _, coordinate = dg.load_policy_at_commit(self.root.root, base)
        for kind in ("omitted-owner", "root-catalog", "actual-root-target", "actual-root-mapping"):
            with self.subTest(kind=kind):
                forged = copy.deepcopy(graph)
                candidate = base
                if kind == "omitted-owner":
                    forged["edges"] = [edge for edge in forged["edges"] if edge["repository"] != self.fixture.identity("Source")
                                       and edge["owner_repository"] != self.fixture.identity("Source")]
                elif kind == "root-catalog":
                    next(edge for edge in forged["edges"] if edge["depth"] == 1 and
                         edge["repository"] == self.fixture.identity("Builds"))["catalog_sha256"] = "0" * 64
                else:
                    if kind == "actual-root-target":
                        self.root.link_gitlink("references/Builds", self.nested_builds)
                    else:
                        self.root.link_gitlink("references/Builds", self.root_builds)
                        modules = dg.read_blob(self.root.root, base, ".gitmodules", 1048576, "fixture").decode()
                        self.root.write_text(".gitmodules", modules.replace(self.fixture.url("Builds"), self.fixture.url("Root")))
                    candidate = self.root.commit()
                    forged["root"]["commit"] = candidate
                    for edge in forged["edges"]:
                        if edge["depth"] == 1: edge["owner_commit"] = candidate
                self.resign(forged)
                dg.validate_graph_envelope(forged)
                evidence = dg.diff_graphs(graph, forged, self.fixture.policy, event="push", event_base=base,
                                          candidate=candidate, merge_base=None, release_eligible=True)
                claimed = copy.deepcopy(semantics)
                if kind == "omitted-owner":
                    claimed["consumer_source_projections"] = {}
                else:
                    projection = claimed["consumer_source_projections"][self.fixture.identity("Source")]
                    projection["consumer_commit"] = candidate
                    projection["projection_digest"] = dg.canonical_digest({key: value for key, value in projection.items() if key != "projection_digest"})
                evidence.update({"dependency_policy": coordinate, "base_semantics": semantics, "candidate_semantics": claimed})
                evidence["evidence_digest"] = dg.canonical_digest({key: value for key, value in evidence.items() if key != "evidence_digest"})
                non_git_calls = []
                actual_run = dg.subprocess.run
                def inspect_or_record(argv, **kwargs):
                    if argv[0] == "git": return actual_run(argv, **kwargs)
                    non_git_calls.append(argv)
                    return subprocess.CompletedProcess(argv, 0, stdout="", stderr="")
                with mock.patch.object(dg.subprocess, "run", side_effect=inspect_or_record), \
                        mock.patch.object(dg, "materialize_repository_tree") as materialize, \
                        mock.patch.object(dg, "materialize_contract_tree") as contract, \
                        mock.patch.object(dg, "materialize_consumer_sources") as sources, \
                        self.assertRaisesRegex(dg.GraphError, "committed root edges|root Builds catalog"):
                    dg.run_affected_builds(self.root.root, evidence, self.root.root / "output")
                materialize.assert_not_called()
                contract.assert_not_called()
                sources.assert_not_called()
                self.assertEqual(non_git_calls, [])

    def test_committed_older_root_without_projection_owner_receives_no_projection_authority(self) -> None:
        self.root.write_text(".gitmodules", '[submodule "Builds"]\n\tpath = references/Builds\n\turl = ' + self.fixture.url("Builds") + '\n')
        self.root.write_text(self.project, '<Project />\n')
        _run(["update-index", "--force-remove", "references/Source"], self.root.root)
        earlier = self.root.commit()
        graph = self.graph(earlier)
        self.assertEqual(dg.evaluate_consumer_projections(self.root.root, self.fixture.policy, graph), {})
        self.assertFalse(any(edge["repository"] == self.fixture.identity("Source") for edge in graph["edges"]))

    def test_missing_trusted_historical_catalog_is_not_a_projection_skip(self) -> None:
        original = dg.read_blob
        def read(local, commit, path, *args):
            if commit == self.nested_builds and path == "Props/Directory.Packages.props": return None
            return original(local, commit, path, *args)
        with mock.patch.object(dg, "read_blob", side_effect=read), self.assertRaisesRegex(dg.GraphError, "missing Props/Directory.Packages.props"):
            self.graph()

    def write_policy(self) -> str:
        self.root.write_text(dg.POLICY_PATH, json.dumps(self.fixture.policy, indent=2) + "\n")
        return self.root.commit()

    def test_candidate_cannot_authorize_its_own_projection_or_expectations(self) -> None:
        row = self.fixture.policy.pop("consumer_source_projections")
        base = self.write_policy()
        self.fixture.policy["consumer_source_projections"] = row
        active = self.write_policy()
        with mock.patch("sys.stdout"):
            self.assertEqual(dg.main(["--root", str(self.root.root), "diff", "--event", "push", "--event-base", base, "--candidate", active]), 1)
        self.root.write_text("README.md", "after delayed activation\n")
        candidate = self.root.commit()
        with mock.patch("sys.stdout"):
            self.assertEqual(dg.main(["--root", str(self.root.root), "diff", "--event", "push", "--event-base", active, "--candidate", candidate]), 0)

    def test_forged_affected_evidence_refuses_before_materialization_or_execution(self) -> None:
        commit = self.write_policy()
        graph = self.graph(commit)
        semantics = dg.evaluate_semantics(self.root.root, self.fixture.policy, graph)
        forged = copy.deepcopy(graph)
        next(edge for edge in forged["edges"] if edge["repository"] in self.untrusted)["commit"] = "f" * 40
        self.resign(forged)
        evidence = dg.diff_graphs(graph, forged, self.fixture.policy, event="push", event_base=commit,
                                  candidate=commit, merge_base=None, release_eligible=True)
        _, _, coordinate = dg.load_policy_at_commit(self.root.root, commit)
        evidence.update({"dependency_policy": coordinate, "base_semantics": semantics, "candidate_semantics": semantics})
        evidence["evidence_digest"] = dg.canonical_digest({key: value for key, value in evidence.items() if key != "evidence_digest"})
        with mock.patch.object(dg, "materialize_repository_tree") as materialize, \
                mock.patch.object(dg, "materialize_contract_tree") as contract, self.assertRaisesRegex(dg.GraphError, "raw inventory"):
            dg.run_affected_builds(self.root.root, evidence, self.root.root / "output")
        materialize.assert_not_called()
        contract.assert_not_called()

    def test_live_source_handoff_and_release_consumer_recheck_the_projection(self) -> None:
        import dependency_handoff as handoff
        import release_evidence as release
        base = self.write_policy()
        graph = self.graph(base)
        _, _, coordinate = dg.load_policy_at_commit(self.root.root, base)
        evidence = {"revisions": {"event": "push", "release_eligible": True, "event_base": base, "candidate": base},
                    "dependency_policy": coordinate, "candidate_graph": graph}
        proof = handoff.create_source_proof(run_id=7, run_attempt=1, evidence=evidence)
        handoff.validate_source_proof(proof, root=self.root.root)
        self.root.write_text(self.project, self.project_xml.replace('</ItemGroup>', '<Compile Include="$(CanonicalSourceRoot)/Other.cs" /></ItemGroup>'))
        changed = self.root.commit()
        raw = dg._collect_raw_graph(self.root.root, self.fixture.identity("Root"), changed, self.fixture.policy)
        evidence["revisions"]["candidate"] = changed
        evidence["candidate_graph"] = raw
        proof = handoff.create_source_proof(run_id=8, run_attempt=1, evidence=evidence)
        with self.assertRaisesRegex(dg.GraphError, "Compile/ProjectReference configuration changed"):
            handoff.validate_source_proof(proof, root=self.root.root)
        manifest = {"dependency_graph": raw, "dependency_policy": coordinate, "workflow_provenance": {}, "commit_sha": changed}
        with mock.patch.object(release, "_load_dependency_graph_engine", return_value=dg):
            diagnostics = release._live_manifest_v2_diagnostics(manifest, self.root.root)
        self.assertTrue(any("Compile/ProjectReference configuration changed" in value for value in diagnostics), diagnostics)

    def test_zero_base_projection_diagnostics_cannot_acquire_or_execute_sources(self) -> None:
        commit = self.write_policy()
        destination = pathlib.Path(self.temp.name) / "unavailable-base"
        with mock.patch.object(dg, "_run_git_network") as network, self.assertRaisesRegex(dg.GraphError, "active base"):
            dg.acquire_object_stores(self.root.root, destination, "0" * 40, commit)
        network.assert_not_called()
        self.assertFalse(destination.exists())

    def test_acquisition_uses_only_trusted_metadata_and_never_the_seven_inventory_sources(self) -> None:
        commit = self.write_policy()
        original = dg._ensure_commit_available
        calls = []
        def ensure(local, sha):
            calls.append(sha)
            return original(local, sha)
        by_identity = {self.fixture.identity(name): repo.root for name, repo in self.fixture.repos.items()}
        def clone(args, **kwargs):
            identity = dg.normalize_identity(args[-2], "fixture remote")
            self.assertNotIn(identity, self.untrusted)
            result = subprocess.run(["git", "clone", "--quiet", "--no-checkout", str(by_identity[identity]), args[-1]], capture_output=True)
            return result, 1
        destination = pathlib.Path(self.temp.name) / "acquired"
        with mock.patch.object(dg, "_run_git_network", side_effect=clone), mock.patch.object(dg, "_ensure_commit_available", side_effect=ensure):
            result = dg.acquire_object_stores(self.root.root, destination, commit, commit)
        self.assertEqual(result["direct_edges"], 16)
        self.assertTrue(all(f"{number + 1:040x}" not in calls for number in range(7)))
        self.assertIn(self.nested_builds, calls)
        self.assertTrue(all(not (destination / "references" / ("Inventory" + str(number))).exists() for number in range(7)))


class ApprovedProjectionExpectationTests(unittest.TestCase):
    def test_exact_two_policy_expectations_and_older_wrong_missing_catalog_negatives(self) -> None:
        policy = dg.load_policy(ROOT / "eng/dependency-graph-policy.json")
        requirements = policy["profiles"]["frontcomposer-catalog-v1"]["selected_catalog_required_packages"]
        for name in ("Microsoft.Extensions.Localization", "System.Collections.Immutable"):
            self.assertEqual(requirements[name], "10.0.14")
            for version in ("10.0.14", "10.0.13", "10.0.15", None):
                catalog = ET.fromstring('<Project><ItemGroup>' + (
                    f'<PackageVersion Include="{name}" Version="{version}" />' if version else '') + '</ItemGroup></Project>')
                with self.subTest(package=name, version=version):
                    if version == "10.0.14":
                        dg.assert_authoritative_package_version(catalog, name, requirements[name], "approved expectation")
                    else:
                        with self.assertRaises(dg.GraphError):
                            dg.assert_authoritative_package_version(catalog, name, requirements[name], "approved expectation")


if __name__ == "__main__":
    unittest.main()
