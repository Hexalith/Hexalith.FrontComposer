#!/usr/bin/env python3
"""Synthetic committed-object graph and semantic-policy tests for eng/dependency_graph.py.

Scope note: this covers GOV-1 Task 2/3 (local collection + semantic evaluation). CI graph
diffing and affected-module contract-tree extraction (Task 4, blocked pending Hexalith.Builds
issue 17 / BUILD-REL-1 per AD-16) are not implemented yet and are out of scope here.
"""

from __future__ import annotations

import copy
import hashlib
import pathlib
import subprocess
import sys
import tempfile
import unittest

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


class CanonicalDigestTests(unittest.TestCase):
    def test_canonical_bytes_are_compact_sorted_ascii(self) -> None:
        payload = {"b": 1, "a": [3, 2, 1], "c": "café"}
        raw = dg.canonical_bytes(payload)
        self.assertEqual(raw, b'{"a":[3,2,1],"b":1,"c":"caf\\u00e9"}')
        self.assertFalse(raw.endswith(b"\n"))

    def test_digest_is_sha256_of_canonical_bytes(self) -> None:
        payload = {"x": 1}
        self.assertEqual(dg.canonical_digest(payload), hashlib.sha256(dg.canonical_bytes(payload)).hexdigest())


if __name__ == "__main__":
    unittest.main()
