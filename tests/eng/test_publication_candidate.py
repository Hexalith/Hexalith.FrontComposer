#!/usr/bin/env python3
"""Executable hostile-data proofs for the inactive GOV-1 transfer boundary."""
from __future__ import annotations

import copy
import contextlib
import hashlib
import io
import json
import os
import pathlib
import stat
import struct
import subprocess
import sys
import tempfile
import unittest
import zipfile
from unittest import mock

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "eng"))
sys.path.insert(0, str(pathlib.Path(__file__).parent))

import dependency_graph as dg
import dependency_handoff as dh
import publication_candidate as pc
import test_dependency_handoff as handoff_fixtures
import test_dependency_graph as graph_fixtures


def _central_start(raw):
    end = raw.rfind(b"PK\x05\x06")
    return struct.unpack_from("<L", raw, end + 16)[0]


class PublicationCandidateTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.work = pathlib.Path(self.temporary.name)
        self.source = self.work / "source"
        self.source.mkdir()
        fixture = handoff_fixtures.HandoffTests()
        self.ci = fixture._ci_handoff()
        self.ci_raw = dg.canonical_bytes(self.ci)
        self.evaluator = fixture._evaluator("release")
        self.evaluator["caller"]["commit"] = fixture.candidate
        self.evaluator["reusable"]["workflow_path"] = ".github/workflows/domain-release.yml"
        self.evaluator["definition_digest"] = dg.canonical_digest({key: value for key, value in self.evaluator.items() if key != "definition_digest"})
        self.policy = {"evaluator_authorizations": {"ci": [fixture._authorization("ci", self.ci["evaluator"])],
                                                  "release": [fixture._authorization("release", self.evaluator)]}}
        self.run = {"repository": dh.ROOT_REPOSITORY, "workflow_path": dh.RELEASE_WORKFLOW_PATH,
                    "run_id": 42, "run_attempt": 2}
        self.evidence = {"evidence/inventory.json", "evidence/tests.trx", "evidence/sbom.json", "evidence/symbols.json", "evidence/checksums.txt"}
        self.package_ids = {"Hexalith.FrontComposer.One", "Hexalith.FrontComposer.Two"}
        self.plan = {"version": "1.2.3-rc.1", "tag": "v1.2.3-rc.1", "release_name": "A release",
                     "release_notes_path": "metadata/notes.md"}
        self.files = []
        for package_id in sorted(self.package_ids):
            for suffix, role in ((".nupkg", "package"), (".snupkg", "symbol")):
                name = package_id + "." + self.plan["version"] + suffix
                content = io.BytesIO()
                with zipfile.ZipFile(content, "w") as package:
                    package.writestr(package_id + ".nuspec", f"<package><metadata><id>{package_id}</id><version>{self.plan['version']}</version></metadata></package>")
                    package.writestr("lib/net10.0/value.dll", os.urandom(128))
                self._file("packages/" + name, content.getvalue(), role, name)
        for name in sorted(self.evidence):
            self._file(name, b'{"result":"pass"}', "evidence", pathlib.Path(name).name)
        self._file("metadata/notes.md", b"Bounded release notes; $(touch should-never-execute)", "release-metadata", None)
        self.archive = self.work / "candidate.zip"
        result = pc.create_archive(self.archive, source_root=self.source, files=self.files,
                                   release_plan=self.plan, **self._bindings())
        self.coordinate = {**result, "artifact_id": 91}
        with zipfile.ZipFile(self.archive) as archive:
            self.members = {name: archive.read(name) for name in archive.namelist()}
        self.descriptor = json.loads(self.members[pc.DESCRIPTOR])

    def _file(self, path, raw, role, asset):
        destination = self.source / path
        destination.parent.mkdir(parents=True, exist_ok=True)
        destination.write_bytes(raw)
        self.files.append({"path": path, "role": role, "asset_name": asset})

    def _bindings(self):
        return {"ci_handoff_raw": self.ci_raw, "release_run": self.run, "evaluator": self.evaluator,
                "policy": self.policy, "required_evidence": self.evidence, "expected_package_ids": self.package_ids}

    def _descriptor(self, value=None):
        return pc.validate_descriptor(value or self.descriptor, **{key: val for key, val in self._bindings().items() if key != "expected_package_ids"})

    def _verify(self, coordinate=None, **kwargs):
        return pc.verify_archive(self.archive, coordinate=coordinate or self.coordinate, **self._bindings(), **kwargs)

    def _rewrite(self, *, descriptor=None, members=None, compression=zipfile.ZIP_STORED, info=None):
        values = copy.deepcopy(self.members if members is None else members)
        if descriptor is not None:
            values[pc.DESCRIPTOR] = dg.canonical_bytes(descriptor)
        with zipfile.ZipFile(self.archive, "w", compression=compression) as output:
            for name, raw in values.items():
                output.writestr(info if info is not None and info.filename == name else name, raw)
        self.coordinate["archive_sha256"] = hashlib.sha256(self.archive.read_bytes()).hexdigest()
        self.coordinate["descriptor_sha256"] = hashlib.sha256(values[pc.DESCRIPTOR]).hexdigest()

    def _patch(self, change):
        raw = bytearray(self.archive.read_bytes())
        change(raw)
        self.archive.write_bytes(raw)
        self.coordinate["archive_sha256"] = hashlib.sha256(raw).hexdigest()

    def test_round_trip_checks_all_bytes_before_materializing_data(self):
        destination = self.work / "materialized"
        result = self._verify(extract_to=destination)
        self.assertEqual(result["descriptor"], self.descriptor)
        self.assertEqual({path.relative_to(destination).as_posix() for path in destination.rglob("*") if path.is_file()}, set(self.members))
        self.assertFalse((self.work / "should-never-execute").exists())
        for name, raw in self.members.items():
            self.assertEqual((destination / name).read_bytes(), raw)
        self.assertNotIn("publish_authorized", result)
        self.assertNotIn("seal", result)

    def test_builder_archive_is_reproducible_and_never_overwrites(self):
        other = self.work / "other.zip"
        pc.create_archive(other, source_root=self.source, files=list(reversed(self.files)), release_plan=self.plan, **self._bindings())
        self.assertEqual(other.read_bytes(), self.archive.read_bytes())
        with self.assertRaises(FileExistsError):
            pc.create_archive(self.archive, source_root=self.source, files=self.files, release_plan=self.plan, **self._bindings())

    def test_no_release_still_has_authenticated_descriptor(self):
        archive = self.work / "no-release.zip"
        result = pc.create_archive(archive, source_root=self.source, files=[], release_plan=None,
                                   **{**self._bindings(), "required_evidence": set()})
        result = pc.verify_archive(archive, coordinate={**result, "artifact_id": 92},
                                   **{**self._bindings(), "required_evidence": set()})
        self.assertIsNone(result["descriptor"]["release_plan"])

    def test_exact_authority_and_artifact_drift_cannot_be_rehashed_into_trust(self):
        for key, value in (("artifact_name", "publication-candidate-42-1"), ("artifact_id", True),
                           ("run_id", 43), ("run_attempt", 1), ("archive_sha256", "0" * 64),
                           ("descriptor_sha256", "0" * 64)):
            with self.subTest(key=key), self.assertRaises(pc.CandidateError):
                self._verify({**self.coordinate, key: value})
        for mutate in (lambda d: d.update(candidate="c" * 40),
                       lambda d: d["release_run"].update(run_attempt=1),
                       lambda d: d["dependency_policy"].update(commit="c" * 40),
                       lambda d: d["evaluator"]["reusable"].update(commit="c" * 40),
                       lambda d: d.update(ci_handoff_sha256="0" * 64)):
            descriptor = copy.deepcopy(self.descriptor)
            mutate(descriptor)
            self._rewrite(descriptor=descriptor)
            with self.assertRaises((pc.CandidateError, dh.HandoffError)):
                self._verify()

    def test_unapproved_or_duplicate_evaluator_rows_fail_closed(self):
        for rows in ([], self.policy["evaluator_authorizations"]["release"] * 2):
            policy = copy.deepcopy(self.policy)
            policy["evaluator_authorizations"]["release"] = rows
            with self.assertRaises(dh.HandoffError):
                pc.verify_archive(self.archive, coordinate=self.coordinate, **{**self._bindings(), "policy": policy})

    def test_integer_limits_are_exact_and_include_imported_ci_evidence(self):
        for value in (False, 0, -1, 1.0, pc.MAX_INTEGER + 1):
            for key in ("run_id", "run_attempt"):
                with self.subTest(key=key, value=value), self.assertRaises(pc.CandidateError):
                    pc.validate_release_run({**self.run, key: value})
        pc.validate_release_run({**self.run, "run_id": pc.MAX_INTEGER, "run_attempt": pc.MAX_INTEGER})
        for key in ("run_id", "run_attempt"):
            ci = copy.deepcopy(self.ci)
            ci["run"][key] = pc.MAX_INTEGER + 1
            with self.subTest(key=key), self.assertRaises(pc.CandidateError):
                pc.validate_descriptor(self.descriptor, **{key: value for key, value in {**self._bindings(), "ci_handoff_raw": dg.canonical_bytes(ci)}.items() if key != "expected_package_ids"})
        for value in (True, -1, 0.1, pc.MAX_INTEGER + 1):
            descriptor = copy.deepcopy(self.descriptor)
            descriptor["files"][0]["size"] = value
            with self.subTest(size=value), self.assertRaises(pc.CandidateError):
                self._descriptor(descriptor)

    def test_all_zero_oid_is_refused_at_each_imported_boundary(self):
        for key in ("candidate", "policy", "caller", "action", "ci-base"):
            descriptor = copy.deepcopy(self.descriptor)
            bindings = self._bindings()
            if key == "candidate":
                descriptor["candidate"] = "0" * 40
            elif key == "policy":
                descriptor["dependency_policy"]["commit"] = "0" * 40
            elif key == "caller":
                descriptor["evaluator"]["caller"]["commit"] = "0" * 40
            elif key == "action":
                descriptor["evaluator"]["actions"][0]["commit"] = "0" * 40
            else:
                ci = copy.deepcopy(self.ci)
                ci["revisions"]["base"] = ci["dependency_policy"]["commit"] = "0" * 40
                bindings["ci_handoff_raw"] = dg.canonical_bytes(ci)
            with self.subTest(key=key), self.assertRaises(pc.CandidateError):
                pc.validate_descriptor(descriptor, **{key: value for key, value in bindings.items() if key != "expected_package_ids"})

    def test_duplicate_and_unknown_json_members_are_rejected(self):
        raw = self.members[pc.DESCRIPTOR]
        members = {**self.members, pc.DESCRIPTOR: raw.replace(b'"schema":', b'"schema":"forged","schema":', 1)}
        self._rewrite(members=members)
        with self.assertRaisesRegex(pc.CandidateError, "duplicate JSON"):
            self._verify()
        descriptor = {**self.descriptor, "publish_authorized": True}
        with self.assertRaisesRegex(pc.CandidateError, "members must"):
            self._descriptor(descriptor)

    def test_destination_collisions_ignore_hash_and_size_and_include_durable_assets(self):
        for name in pc.durable_asset_names(self.run) | {self.descriptor["files"][1]["asset_name"]}:
            if name is None:
                continue
            descriptor = copy.deepcopy(self.descriptor)
            descriptor["files"][0].update(asset_name=name, sha256="f" * 64, size=17)
            with self.subTest(name=name), self.assertRaisesRegex(pc.CandidateError, "destination asset_name"):
                self._descriptor(descriptor)

    def test_asset_name_grammar_and_metadata_null_are_enforced(self):
        for name in ("../a", "a/b", ".hidden", "bad-", "a" * 129, "é", "", True, None):
            descriptor = copy.deepcopy(self.descriptor)
            descriptor["files"][0].update(asset_name=name, sha256="f" * 64, size=17)
            with self.subTest(name=name), self.assertRaises(pc.CandidateError):
                self._descriptor(descriptor)

    def test_version_subset_rejects_nuget_normalization_aliases(self):
        for value in ("1.2", "01.2.3", "1.2.3.0", "1.2.3+build", "1.2.3-RC", "1.2.3-01", "1.2.3-x..y", "2147483648.0.0", "1.2.3 ", "1.2.3-" + "x" * 123):
            with self.subTest(value=value), self.assertRaises(pc.CandidateError):
                pc.validate_version(value)
        for value in ("0.0.0", "2147483647.2147483647.2147483647", "1.2.3-0.alpha-1"):
            self.assertEqual(pc.validate_version(value), value)

    def test_missing_undeclared_or_changed_members_fail_before_destination_creation(self):
        destination = self.work / "forbidden"
        for values in ({key: value for key, value in self.members.items() if key != "evidence/tests.trx"},
                       {**self.members, "surprise.sh": b"touch should-never-execute"},
                       {**self.members, "evidence/tests.trx": b"changed bytes"}):
            self._rewrite(members=values)
            with self.assertRaises(pc.CandidateError):
                self._verify(extract_to=destination)
            self.assertFalse(destination.exists())

    def test_required_fr24_evidence_is_independent_of_descriptor_claims(self):
        descriptor = copy.deepcopy(self.descriptor)
        descriptor["files"] = [row for row in descriptor["files"] if row["path"] != "evidence/tests.trx"]
        with self.assertRaisesRegex(pc.CandidateError, "required owner-controlled evidence"):
            self._descriptor(descriptor)
        with self.assertRaisesRegex(pc.CandidateError, "independent FR-24"):
            pc.validate_descriptor(self.descriptor, **{key: value for key, value in {**self._bindings(), "required_evidence": set()}.items() if key != "expected_package_ids"})

    def test_package_version_or_inventory_drift_is_not_accepted(self):
        for expected in (self.package_ids | {"Unexpected"}, {"Unexpected"}, set()):
            with self.assertRaises(pc.CandidateError):
                pc.verify_archive(self.archive, coordinate=self.coordinate, **{**self._bindings(), "expected_package_ids": expected})
        descriptor = copy.deepcopy(self.descriptor)
        descriptor["release_plan"]["version"] = "1.2.4"
        descriptor["release_plan"]["tag"] = "v1.2.4"
        self._rewrite(descriptor=descriptor)
        with self.assertRaisesRegex(pc.CandidateError, "inventory/version/name"):
            self._verify()

    def test_release_notes_are_data_and_must_be_utf8(self):
        members = {**self.members, self.plan["release_notes_path"]: b"\xff"}
        descriptor = copy.deepcopy(self.descriptor)
        row = next(row for row in descriptor["files"] if row["role"] == "release-metadata")
        row.update(size=1, sha256=hashlib.sha256(b"\xff").hexdigest())
        self._rewrite(descriptor=descriptor, members=members)
        with self.assertRaisesRegex(pc.CandidateError, "UTF-8"):
            self._verify()

    def test_symlink_directory_and_special_archive_members_are_refused(self):
        name = "evidence/tests.trx"
        for mode in (stat.S_IFLNK | 0o777, stat.S_IFDIR | 0o755, stat.S_IFIFO | 0o600):
            info = zipfile.ZipInfo(name)
            info.create_system = 3
            info.external_attr = mode << 16
            self._rewrite(info=info)
            with self.subTest(mode=mode), self.assertRaisesRegex(pc.CandidateError, "non-regular"):
                self._verify()

    def test_builder_refuses_leaf_and_parent_symlinks(self):
        path = self.source / "evidence/tests.trx"
        path.unlink()
        path.symlink_to(self.source / "evidence/inventory.json")
        with self.assertRaises(OSError):
            pc.create_archive(self.work / "symlink.zip", source_root=self.source, files=self.files, release_plan=self.plan, **self._bindings())
        path.unlink()
        (self.source / "evidence").rename(self.source / "real-evidence")
        (self.source / "evidence").symlink_to(self.source / "real-evidence", target_is_directory=True)
        with self.assertRaises(OSError):
            pc.create_archive(self.work / "parent-symlink.zip", source_root=self.source, files=self.files, release_plan=self.plan, **self._bindings())

    def test_archive_path_and_depth_boundaries(self):
        pc._path("a/" * 7 + "b")
        pc._path("a" * 240)
        for value in ("/absolute", "../escape", "a//b", "a/./b", "a\\b", "a\x00b", "a\x7fb", "é", "a/" * 8 + "b", "a" * 241):
            with self.subTest(value=value), self.assertRaises(pc.CandidateError):
                pc._path(value)
        for value in ("../escape", "a/" * 8 + "b"):
            self._rewrite(members={**self.members, value: b"hidden"})
            with self.assertRaises(pc.CandidateError):
                self._verify()

    def test_duplicate_archive_name_is_not_hidden_by_dictionary_projection(self):
        with zipfile.ZipFile(self.archive, "a") as output:
            with self.assertWarns(UserWarning):
                output.writestr(pc.DESCRIPTOR, self.members[pc.DESCRIPTOR])
        self.coordinate["archive_sha256"] = hashlib.sha256(self.archive.read_bytes()).hexdigest()
        with self.assertRaisesRegex(pc.CandidateError, "duplicate ZIP"):
            self._verify()

    def test_crc_header_and_size_lies_are_rejected_even_with_rehashed_archive(self):
        original = self.archive.read_bytes()
        for mutate in (lambda raw: struct.pack_into("<L", raw, 14, 123),
                       lambda raw: struct.pack_into("<L", raw, 18, 0),
                       lambda raw: raw.__setitem__(35, raw[35] ^ 1)):
            self.archive.write_bytes(original)
            self._patch(mutate)
            with self.assertRaises(pc.CandidateError):
                self._verify()

    def test_overlap_encryption_and_hidden_data_are_rejected(self):
        original = self.archive.read_bytes()
        def overlap(raw):
            position = _central_start(raw)
            struct.pack_into("<L", raw, position + 42, 1)
        def encrypt(raw):
            position = _central_start(raw)
            struct.pack_into("<H", raw, position + 8, 1)
        for mutate in (overlap, encrypt, lambda raw: raw.extend(b"hidden")):
            self.archive.write_bytes(original)
            self._patch(mutate)
            with self.assertRaises(pc.CandidateError):
                self._verify()

    def test_central_directory_member_limit_is_enforced_before_opening_members(self):
        with mock.patch.object(pc, "MAX_MEMBERS", len(self.members) - 1), mock.patch.object(pc, "_stream_member", side_effect=AssertionError("must not read members")):
            with self.assertRaises(pc.CandidateError):
                self._verify()
        with mock.patch.object(pc, "MAX_MEMBERS", len(self.members)):
            self._verify()

    def test_raw_member_and_total_size_boundaries(self):
        size = self.archive.stat().st_size
        with mock.patch.object(pc, "MAX_ARCHIVE", size):
            self._verify()
        with mock.patch.object(pc, "MAX_ARCHIVE", size - 1):
            with self.assertRaisesRegex(pc.CandidateError, "raw archive"):
                self._verify()
        largest = max(map(len, self.members.values()))
        with mock.patch.object(pc, "MAX_MEMBER", largest):
            self._verify()
        with mock.patch.object(pc, "MAX_MEMBER", largest - 1):
            with self.assertRaises(pc.CandidateError):
                self._verify()
        total = sum(map(len, self.members.values()))
        with mock.patch.object(pc, "MAX_TOTAL", total):
            self._verify()
        with mock.patch.object(pc, "MAX_TOTAL", total - 1):
            with self.assertRaises(pc.CandidateError):
                self._verify()

    def test_compression_ratio_bombs_are_refused_before_inflation(self):
        self._rewrite(members={**self.members, "bomb": b"a" * 1000000}, compression=zipfile.ZIP_DEFLATED)
        with mock.patch.object(pc, "_stream_member", side_effect=AssertionError("must not inflate")):
            with self.assertRaisesRegex(pc.CandidateError, "ratio"):
                self._verify()

    def test_valid_deflated_archive_and_zip64_local_fields(self):
        self._rewrite(compression=zipfile.ZIP_DEFLATED)
        self._verify()
        with zipfile.ZipFile(self.archive, "w") as output:
            for name, raw in self.members.items():
                with output.open(name, "w", force_zip64=True) as target:
                    target.write(raw)
        self.coordinate["archive_sha256"] = hashlib.sha256(self.archive.read_bytes()).hexdigest()
        self._verify()

    def test_data_descriptors_are_validated_including_zip64(self):
        class NonSeekable(io.BytesIO):
            def seekable(self):
                return False
            def seek(self, *args):
                raise io.UnsupportedOperation("stream")
        for zip64 in (False, True):
            buffer = NonSeekable()
            with zipfile.ZipFile(buffer, "w", compression=zipfile.ZIP_DEFLATED) as output:
                for name, raw in self.members.items():
                    with output.open(name, "w", force_zip64=zip64) as target:
                        target.write(raw)
            self.archive.write_bytes(buffer.getvalue())
            self.coordinate["archive_sha256"] = hashlib.sha256(buffer.getvalue()).hexdigest()
            self._verify()
            self._patch(lambda raw: struct.pack_into("<L", raw, raw.index(b"PK\x07\x08") + 4, 123))
            with self.assertRaisesRegex(pc.CandidateError, "data descriptor"):
                self._verify()

    def test_inconsistent_zip64_extra_and_end_records_are_refused(self):
        self._patch(lambda raw: struct.pack_into("<L", raw, _central_start(raw) + 20, 0xFFFFFFFF))
        with self.assertRaisesRegex(pc.CandidateError, "ZIP64"):
            self._verify()

    def test_builder_cannot_create_a_package_under_a_diagnostic_role(self):
        descriptor = copy.deepcopy(self.descriptor)
        row = next(row for row in descriptor["files"] if row["role"] == "package")
        row["role"] = "evidence"
        self._rewrite(descriptor=descriptor)
        with self.assertRaisesRegex(pc.CandidateError, "relabelled"):
            self._verify()

    def test_builder_rejects_duplicate_destinations_before_emitting_archive(self):
        files = copy.deepcopy(self.files)
        files[1]["asset_name"] = files[0]["asset_name"]
        output = self.work / "must-not-exist.zip"
        with self.assertRaisesRegex(pc.CandidateError, "destination asset_name"):
            pc.create_archive(output, source_root=self.source, files=files, release_plan=self.plan, **self._bindings())
        self.assertFalse(output.exists())

    def test_true_zip64_central_directory_and_end_records(self):
        # Small real archives exercise exactly the same ZIP64 structures without
        # allocating multi-gigabyte fixtures.
        with mock.patch.object(zipfile, "ZIP64_LIMIT", 1):
            self._rewrite()
        self.assertIn(b"PK\x06\x06", self.archive.read_bytes())
        self._verify()
        self._patch(lambda raw: struct.pack_into("<L", raw, raw.rfind(b"PK\x06\x07") + 4, 1))
        with self.assertRaisesRegex(pc.CandidateError, "ZIP64 locator"):
            self._verify()

    def test_compression_ratio_boundary_is_inclusive_in_preflight(self):
        content = io.BytesIO()
        with zipfile.ZipFile(content, "w") as archive:
            archive.writestr("one", b"x")
        raw = bytearray(content.getvalue())
        central = _central_start(raw)
        struct.pack_into("<L", raw, central + 24, pc.MAX_RATIO)
        members, _ = pc._index(io.BytesIO(raw), len(raw))
        self.assertEqual(members[0].size, pc.MAX_RATIO)
        struct.pack_into("<L", raw, central + 24, pc.MAX_RATIO + 1)
        with self.assertRaisesRegex(pc.CandidateError, "ratio"):
            pc._index(io.BytesIO(raw), len(raw))

    def test_aggregate_ratio_is_checked_even_if_individual_limits_pass(self):
        content = io.BytesIO()
        with zipfile.ZipFile(content, "w") as archive:
            archive.writestr("one", b"")
            archive.writestr("two", b"")
        raw = bytearray(content.getvalue())
        cursor = _central_start(raw)
        for _ in range(2):
            struct.pack_into("<L", raw, cursor + 24, pc.MAX_RATIO)
            name, extra, comment = struct.unpack_from("<3H", raw, cursor + 28)
            cursor += 46 + name + extra + comment
        with self.assertRaisesRegex(pc.CandidateError, "aggregate ratio"):
            pc._index(io.BytesIO(raw), len(raw))

    def test_candidate_configuration_and_hooks_are_never_executed(self):
        self._file("candidate/.releaserc.cjs", b"throw new Error(process.env.NUGET_API_KEY)", "evidence", "candidate-config.txt")
        self._file("candidate/Directory.Build.targets", b"<Exec Command='printenv' />", "evidence", "candidate-msbuild.txt")
        output = self.work / "hostile.zip"
        with mock.patch("subprocess.run", side_effect=AssertionError("candidate execution")), \
                mock.patch("subprocess.Popen", side_effect=AssertionError("candidate process")), \
                mock.patch("os.system", side_effect=AssertionError("candidate shell")):
            coordinate = pc.create_archive(output, source_root=self.source, files=self.files, release_plan=self.plan, **self._bindings())
            pc.verify_archive(output, coordinate={**coordinate, "artifact_id": 92}, **self._bindings())

    def test_safe_integer_maximum_survives_full_archive_binding(self):
        self.run.update(run_id=pc.MAX_INTEGER, run_attempt=pc.MAX_INTEGER)
        ci = copy.deepcopy(self.ci)
        ci["run"].update(run_id=pc.MAX_INTEGER, run_attempt=pc.MAX_INTEGER)
        self.ci_raw = dg.canonical_bytes(ci)
        output = self.work / "max-coordinates.zip"
        coordinate = pc.create_archive(output, source_root=self.source, files=self.files, release_plan=self.plan, **self._bindings())
        pc.verify_archive(output, coordinate={**coordinate, "artifact_id": pc.MAX_INTEGER}, **self._bindings())

    def test_existing_extraction_destination_is_preserved(self):
        destination = self.work / "existing"
        destination.mkdir()
        (destination / "user.txt").write_text("keep")
        with self.assertRaises(FileExistsError):
            self._verify(extract_to=destination)
        self.assertEqual((destination / "user.txt").read_text(), "keep")


    def _replace_package(self, *, role="package", nuspec=None, extra=None):
        descriptor = copy.deepcopy(self.descriptor)
        row = next(item for item in descriptor["files"] if item["role"] == role)
        content = io.BytesIO()
        with zipfile.ZipFile(io.BytesIO(self.members[row["path"]])) as original, zipfile.ZipFile(content, "w") as package:
            for item in original.infolist():
                raw = nuspec if nuspec is not None and item.filename.endswith(".nuspec") else original.read(item)
                package.writestr(item, raw)
            if extra is not None:
                package.writestr(extra[0], extra[1])
        raw = content.getvalue()
        row.update(size=len(raw), sha256=hashlib.sha256(raw).hexdigest())
        self._rewrite(descriptor=descriptor, members={**self.members, row["path"]: raw})

    def _nuspec(self):
        package_id = sorted(self.package_ids)[0]
        return f"<package><metadata><id>{package_id}</id><version>{self.plan['version']}</version><description>café</description></metadata></package>"

    def test_nuspec_declarations_are_blocked_across_xml_encodings(self):
        for encoding in ("utf-8", "utf-16", "utf-16-le", "utf-16-be"):
            for declaration in ("<!DOCTYPE package>", '<!DOCTYPE package [<!ENTITY hostile "replacement">]>'):
                xml = declaration + self._nuspec()
                self._replace_package(nuspec=xml.encode(encoding))
                destination = self.work / "xml-must-not-extract"
                with self.subTest(encoding=encoding, declaration=declaration), self.assertRaisesRegex(pc.CandidateError, "declarations are forbidden"):
                    self._verify(extract_to=destination)
                self.assertFalse(destination.exists())

    def test_legal_nuspec_encodings_without_dtds_are_preserved(self):
        for encoding, declaration in (("utf-8", ""), ("utf-16", ""), ("utf-16-le", '<?xml version="1.0" encoding="UTF-16"?>'),
                                      ("utf-16-be", '<?xml version="1.0" encoding="UTF-16"?>'),
                                      ("iso-8859-1", '<?xml version="1.0" encoding="ISO-8859-1"?>')):
            self._replace_package(nuspec=(declaration + self._nuspec()).encode(encoding))
            with self.subTest(encoding=encoding):
                self._verify()

    def test_nuspec_root_and_identity_elements_must_be_well_formed_scalars(self):
        xml = self._nuspec()
        for hostile in (xml.replace("package>", "other>"),
                        xml.replace("</id>", "<nested /></id>"),
                        xml.replace("</version>", "<nested /></version>"),
                        xml.replace("</id>", "</id><id>duplicate</id>"),
                        xml.replace("</version>", "</version><version>1.0.0</version>")):
            self._replace_package(nuspec=hostile.encode())
            with self.subTest(xml=hostile), self.assertRaisesRegex(pc.CandidateError, "root must be package|scalar element"):
                self._verify()

    def test_nested_package_links_and_special_members_are_refused_before_extraction(self):
        for role in ("package", "symbol"):
            for mode in (stat.S_IFLNK, stat.S_IFIFO, stat.S_IFSOCK, stat.S_IFCHR, stat.S_IFBLK):
                item = zipfile.ZipInfo("tools/hostile")
                item.create_system = 3
                item.external_attr = (mode | 0o600) << 16
                self._replace_package(role=role, extra=(item, b"untrusted target"))
                destination = self.work / "special-must-not-extract"
                with self.subTest(role=role, mode=mode), self.assertRaisesRegex(pc.CandidateError, "non-regular NuGet"):
                    self._verify(extract_to=destination)
                self.assertFalse(destination.exists())

    def test_signed_packages_and_symbols_fail_after_all_archive_digests_are_rehashed(self):
        for role in ("package", "symbol"):
            with self.subTest(role=role):
                self._replace_package(role=role, extra=(".signature.p7s", b"candidate signature"))
                destination = self.work / f"signed-{role}-must-not-extract"
                with self.assertRaisesRegex(pc.rc.ContractError, "must not contain an author or repository signature"):
                    self._verify(extract_to=destination)
                self.assertFalse(destination.exists())

    def test_no_release_rejects_packages_relabelled_as_evidence_by_path_or_asset(self):
        for selector in ("path", "asset_name"):
            descriptor = copy.deepcopy(self.descriptor)
            descriptor["release_plan"] = None
            descriptor["files"] = [row for row in descriptor["files"] if row["role"] == "evidence"]
            row = descriptor["files"][0]
            original_path = row["path"]
            members = {path: self.members[path] for path in (item["path"] for item in descriptor["files"])}
            if selector == "path":
                row["path"] = "hidden/Package.SNUPKG"
                members[row["path"]] = members.pop(original_path)
            else:
                row["asset_name"] = "Hidden.Package.NUPKG"
            descriptor["files"].sort(key=lambda item: item["path"])
            self._rewrite(descriptor=descriptor, members=members)
            destination = self.work / "no-release-must-not-extract"
            bindings = {**self._bindings(), "required_evidence": set()}
            with self.subTest(selector=selector), self.assertRaisesRegex(pc.CandidateError, "no-release candidate"):
                pc.verify_archive(self.archive, coordinate=self.coordinate, extract_to=destination, **bindings)
            self.assertFalse(destination.exists())

    def test_raw_archive_fifo_refusal_does_not_block(self):
        fifo = self.work / "archive-fifo"
        os.mkfifo(fifo)
        code = "import pathlib,sys,tempfile; sys.path.insert(0,sys.argv[1]); import publication_candidate as pc; pc._snapshot(pathlib.Path(sys.argv[2]),tempfile.TemporaryFile())"
        completed = subprocess.run([sys.executable, "-c", code, str(ROOT / "eng"), str(fifo)], capture_output=True, timeout=3)
        self.assertNotEqual(completed.returncode, 0)
        self.assertIn(b"not a regular candidate file", completed.stderr)

    def test_raw_archive_symlink_and_device_are_refused(self):
        link = self.work / "archive-symlink"
        link.symlink_to(self.archive)
        with tempfile.TemporaryFile() as target, self.assertRaises(OSError):
            pc._snapshot(link, target)
        with tempfile.TemporaryFile() as target, self.assertRaisesRegex(pc.CandidateError, "not a regular candidate file"):
            pc._snapshot(pathlib.Path(os.devnull), target)

    def test_producer_close_failure_removes_only_the_new_archive(self):
        output = self.work / "close-failure.zip"
        original_archive = self.archive.read_bytes()
        original_open = pathlib.Path.open
        class FailingClose:
            def __init__(self, target):
                self.target = target
            def __enter__(self):
                return self.target
            def __exit__(self, *args):
                self.target.close()
                raise OSError("buffered close failed")
        def open_file(path, mode="r", *args, **kwargs):
            target = original_open(path, mode, *args, **kwargs)
            return FailingClose(target) if path == output and mode == "xb" else target
        with mock.patch.object(pathlib.Path, "open", open_file):
            with self.assertRaisesRegex(OSError, "buffered close failed"):
                pc.create_archive(output, source_root=self.source, files=self.files, release_plan=self.plan, **self._bindings())
        self.assertFalse(output.exists())
        self.assertEqual(self.archive.read_bytes(), original_archive)
        with mock.patch.object(pathlib.Path, "open", open_file), self.assertRaises(FileExistsError):
            pc.create_archive(self.archive, source_root=self.source, files=self.files, release_plan=self.plan, **self._bindings())
        self.assertEqual(self.archive.read_bytes(), original_archive)
        pc.create_archive(output, source_root=self.source, files=self.files, release_plan=self.plan, **self._bindings())

    def _cli_authority(self):
        repository = graph_fixtures.TempGitRepo(self.work / "authority")
        policy = json.loads((ROOT / "eng/dependency-graph-policy.json").read_bytes())
        policy.pop("consumer_source_projections", None)
        ci_evaluator = copy.deepcopy(self.ci["evaluator"])
        evaluator = copy.deepcopy(self.evaluator)
        for current in (ci_evaluator, evaluator):
            raw = b"name: Exact committed caller\non: workflow_dispatch\njobs: {}\n"
            repository.write_bytes(current["caller"]["workflow_path"], raw)
            current["caller"]["blob_sha256"] = hashlib.sha256(raw).hexdigest()
        fixture = handoff_fixtures.HandoffTests()
        policy["evaluator_authorizations"] = {"ci": [fixture._authorization("ci", ci_evaluator)],
                                               "release": [fixture._authorization("release", evaluator)], "post_release": []}
        raw_policy = dg.canonical_bytes(policy)
        repository.write_bytes(dg.POLICY_PATH, raw_policy)
        base = repository.commit("test: commit exact active policy")
        repository.write_text("candidate.txt", "Exact candidate\n")
        candidate = repository.commit("test: commit exact candidate")
        for current in (ci_evaluator, evaluator):
            current["caller"]["commit"] = candidate
            current["definition_digest"] = dg.canonical_digest({key: value for key, value in current.items() if key != "definition_digest"})
        ci = dh.create_ci_handoff(run_id=41, run_attempt=2, base=base, candidate=candidate,
                                 evaluator=ci_evaluator, dependency_policy=dg.policy_projection(policy, base, raw_policy),
                                 dependency_graph=dg.collect_graph(repository.root, dh.ROOT_REPOSITORY, candidate, policy), policy=policy)
        inputs = self.work / "cli-inputs"
        inputs.mkdir()
        for name, value in (("ci.json", ci), ("run.json", self.run), ("evaluator.json", evaluator),
                            ("evidence.json", []), ("files.json", []), ("plan.json", None)):
            (inputs / name).write_bytes(dg.canonical_bytes(value))
        return {"repository": repository, "inputs": inputs, "ci": ci, "evaluator": evaluator, "policy": policy}

    def _cli_arguments(self, command, authority, archive, output, extract=None):
        inputs = authority["inputs"]
        args = [command, "--archive", str(archive), "--ci-handoff", str(inputs / "ci.json"),
                "--release-run", str(inputs / "run.json"), "--release-evaluator", str(inputs / "evaluator.json"),
                "--root", str(authority["repository"].root), "--required-evidence", str(inputs / "evidence.json"),
                "--output", str(output)]
        if command == "create":
            args += ["--source-root", str(self.source), "--files", str(inputs / "files.json"), "--release-plan", str(inputs / "plan.json")]
        else:
            args += ["--coordinate", str(inputs / "coordinate.json")]
            if extract is not None:
                args += ["--extract-to", str(extract)]
        return args

    def _call_cli(self, arguments):
        stdout, stderr = io.StringIO(), io.StringIO()
        with contextlib.redirect_stdout(stdout), contextlib.redirect_stderr(stderr):
            result = pc.main(arguments)
        return result, json.loads(stdout.getvalue() if result == 0 else stderr.getvalue())

    def _cli_transfer(self, authority, archive, ci=None):
        ci = authority["ci"] if ci is None else ci
        coordinate = pc.create_archive(archive, source_root=self.source, files=[], release_plan=None,
                                       ci_handoff_raw=dg.canonical_bytes(ci), release_run=self.run,
                                       evaluator=authority["evaluator"], policy=authority["policy"],
                                       required_evidence=set(), expected_package_ids=set())
        (authority["inputs"] / "coordinate.json").write_bytes(dg.canonical_bytes({**coordinate, "artifact_id": 91}))

    def test_create_cli_uses_exact_committed_ci_graph_and_policy_authority(self):
        authority = self._cli_authority()
        archive, output = self.work / "cli-create.zip", self.work / "cli-create-result.json"
        self.assertEqual(self._call_cli(self._cli_arguments("create", authority, archive, output))[0], 0)
        for mutation in ("graph", "policy"):
            ci = self._forge_cli_handoff(authority, mutation)
            archive, output = self.work / (mutation + "-create.zip"), self.work / (mutation + "-create-result.json")
            result, error = self._call_cli(self._cli_arguments("create", authority, archive, output))
            self.assertEqual(result, 1)
            self.assertFalse(error["ok"])
            self.assertFalse(archive.exists())
            self.assertFalse(output.exists())

    def _forge_cli_handoff(self, authority, mutation):
        ci = copy.deepcopy(authority["ci"])
        if mutation == "graph":
            graph = ci["dependency_graph"]
            graph["edges"] = [{"owner_repository": dh.ROOT_REPOSITORY, "owner_commit": ci["run"]["candidate"],
                               "path": "references/Hexalith.AI.Tools", "repository": "github.com/hexalith/hexalith.ai.tools",
                               "commit": "e" * 40, "depth": 1}]
            graph["edge_count"] = 1
            graph["graph_digest"] = dg.canonical_digest({key: value for key, value in graph.items() if key != "graph_digest"})
        else:
            ci["dependency_policy"]["sha256"] = "f" * 64
        # Establish offline validity, so live exact-object validation is essential.
        dh.validate_ci_handoff(ci)
        (authority["inputs"] / "ci.json").write_bytes(dg.canonical_bytes(ci))
        return ci

    def test_verify_cli_uses_exact_committed_ci_graph_and_policy_authority(self):
        authority = self._cli_authority()
        archive = self.work / "cli-verify.zip"
        self._cli_transfer(authority, archive)
        destination = self.work / "cli-verified"
        self.assertEqual(self._call_cli(self._cli_arguments("verify", authority, archive, self.work / "verified-result.json", destination))[0], 0)
        for mutation in ("graph", "policy"):
            ci = self._forge_cli_handoff(authority, mutation)
            archive = self.work / (mutation + "-verify.zip")
            # Rebind the complete transfer to the forgery: archive/descriptor hashes
            # must not mask a missing live CI check.
            self._cli_transfer(authority, archive, ci)
            output, destination = self.work / (mutation + "-result.json"), self.work / (mutation + "-extract")
            result, error = self._call_cli(self._cli_arguments("verify", authority, archive, output, destination))
            self.assertEqual(result, 1)
            self.assertFalse(error["ok"])
            self.assertFalse(output.exists())
            self.assertFalse(destination.exists())

    def test_cli_reserves_results_before_create_or_verify_and_allows_retry(self):
        authority = self._cli_authority()
        for command in ("create", "verify"):
            archive = self.work / (command + "-retry.zip")
            output, destination = self.work / (command + "-result.json"), self.work / (command + "-extract")
            if command == "verify":
                self._cli_transfer(authority, archive)
            original_archive = archive.read_bytes() if archive.exists() else None
            output.write_bytes(b"user result must survive")
            args = self._cli_arguments(command, authority, archive, output, destination)
            self.assertEqual(self._call_cli(args)[0], 1)
            self.assertEqual(output.read_bytes(), b"user result must survive")
            self.assertFalse(destination.exists())
            if original_archive is None:
                self.assertFalse(archive.exists())
            else:
                self.assertEqual(archive.read_bytes(), original_archive)
            output.unlink()
            self.assertEqual(self._call_cli(args)[0], 0)

    def test_cli_conflicting_destinations_fail_before_materialization(self):
        authority = self._cli_authority()
        archive = self.work / "conflict.zip"
        for command in ("create", "verify"):
            if command == "verify":
                self._cli_transfer(authority, archive)
            before = archive.read_bytes() if archive.exists() else None
            for output, extract in ((archive, self.work / "conflict-extract"),
                                    (self.work / "parent", self.work / "parent/child"),
                                    (self.work / "extract/result.json", self.work / "extract")):
                if command == "create" and output != archive:
                    continue
                result, error = self._call_cli(self._cli_arguments(command, authority, archive, output, extract))
                self.assertEqual(result, 1)
                self.assertIn("destinations must not overlap", error["error"])
                self.assertFalse(extract.exists())
                self.assertEqual(archive.read_bytes() if archive.exists() else None, before)

    def test_unsupported_xml_encoding_becomes_cli_json_failure(self):
        authority = self._cli_authority()
        self.ci_raw = dg.canonical_bytes(authority["ci"])
        self.evaluator, self.policy = authority["evaluator"], authority["policy"]
        self.descriptor.update(candidate=authority["ci"]["run"]["candidate"], ci_handoff_sha256=hashlib.sha256(self.ci_raw).hexdigest(),
                               dependency_policy=authority["ci"]["dependency_policy"], evaluator=self.evaluator)
        xml = '<?xml version="1.0" encoding="UNKNOWN-NUSPEC-ENCODING"?>' + self._nuspec()
        self._replace_package(nuspec=xml.encode())
        (authority["inputs"] / "evidence.json").write_bytes(dg.canonical_bytes(sorted(self.evidence)))
        (authority["inputs"] / "coordinate.json").write_bytes(dg.canonical_bytes(self.coordinate))
        output, destination = self.work / "bad-xml-result.json", self.work / "bad-xml-extract"
        result, error = self._call_cli(self._cli_arguments("verify", authority, self.archive, output, destination))
        self.assertEqual(result, 1)
        self.assertFalse(error["ok"])
        self.assertIn("unsupported encoding", error["error"])
        self.assertFalse(output.exists())
        self.assertFalse(destination.exists())


if __name__ == "__main__":
    unittest.main()
