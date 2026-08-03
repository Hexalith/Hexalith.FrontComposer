#!/usr/bin/env python3
"""Synthetic graph, semantic-policy, AD-8 diff, and materialization tests."""

from __future__ import annotations

import copy
import hashlib
import json
import os
import pathlib
import subprocess
import sys
import tempfile
import unittest
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

    def test_known_profile_keys_load(self) -> None:
        policy = self._load({
            "owner_checks": {},
            "selected_catalog_required_properties": {"SomeVersion": "1.0.0"},
            "selected_catalog_required_packages": {},
        })
        self.assertEqual(
            policy["profiles"]["some-profile"]["selected_catalog_required_properties"],
            {"SomeVersion": "1.0.0"},
        )

    def test_misspelled_required_properties_key_fails_closed(self) -> None:
        with self.assertRaises(dg.GraphError) as ctx:
            self._load({"selected_catalog_required_propertys": {"SomeVersion": "1.0.0"}})
        self.assertIn("unknown keys", str(ctx.exception))

    def test_non_dict_required_properties_fails_closed(self) -> None:
        with self.assertRaises(dg.GraphError) as ctx:
            self._load({"selected_catalog_required_properties": ["SomeVersion"]})
        self.assertIn("must be an object", str(ctx.exception))

    def test_non_string_expected_value_fails_closed(self) -> None:
        with self.assertRaises(dg.GraphError) as ctx:
            self._load({"selected_catalog_required_properties": {"SomeVersion": 1.0}})
        self.assertIn("must be a string", str(ctx.exception))

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

    def test_landed_dependency_build_targets_are_standalone_owned_surfaces(self) -> None:
        policy = dg.load_policy(ROOT / "eng" / "dependency-graph-policy.json")
        expected = {
            "github.com/hexalith/hexalith.eventstore": "Hexalith.EventStore.slnx",
            "github.com/hexalith/hexalith.tenants": "src/Hexalith.Tenants/Hexalith.Tenants.csproj",
            "github.com/hexalith/hexalith.commons": "src/libraries/Hexalith.Commons/Hexalith.Commons.csproj",
            "github.com/hexalith/hexalith.builds": "Hexalith.Builds.slnx",
            "github.com/hexalith/hexalith.polymorphicserializations": "Hexalith.PolymorphicSerializations.slnx",
            "github.com/hexalith/hexalith.memories": "Hexalith.Memories.slnx",
            "github.com/hexalith/hexalith.parties": "src/Hexalith.Parties/Hexalith.Parties.csproj",
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


if __name__ == "__main__":
    unittest.main()
