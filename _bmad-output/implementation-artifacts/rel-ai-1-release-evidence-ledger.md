---
title: REL-AI-1 Release Evidence Compliance Ledger
project: frontcomposer
created: 2026-07-15
updated: 2026-10-29
owner: Release Owner
decisionContract: frontcomposer.release-compliance-ledger.v1
sourceProposal: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-15-rel-ai-1-prepublish-enforcement.md
correctionProposal: _bmad-output/planning-artifacts/sprint-change-proposal-2026-08-03.md
status: active
---

# REL-AI-1 Release Evidence Compliance Ledger

This controlled ledger records whether released FrontComposer package bytes satisfy FR24. Workflow
success is not a compliance disposition. A release is compliant only when it was authorized before
publication and independently verified afterward against the same sealed manifest.

Historical records are not REL-AI-1 closure evidence. They document affected releases and why a
later record cannot relabel those dispositions. The current remaining blocker is in the
2026-08-14 status note.

Status note (2026-07-18): REL-4's fail-closed freeze gate and REL-3's exact-artifact pre-publication
enforcement (pack-once orchestration in `eng/release_prepublish.py`, authorized-bytes publish,
independent downloaded-byte verification in `release-evidence.yml`) are implemented in the
repository. This changes no disposition in this ledger: REL-AI-1 closes only when a real release
passes the full chain with durable evidence and downloaded NuGet/GitHub bytes matching the
authorized manifest (REL-5 owner enablement).

Status note (2026-08-03): complete historical reconciliation now covers `v3.2.1`, `v3.2.2`,
`v4.0.0`, and `v4.0.1`. Exact run topology, source and tag commits, all 64 GitHub assets, all 32
independently downloaded nuget.org packages, signatures, timestamps, symbols, original available
Actions evidence, consumer-validation lineage, and provenance were inspected. Every historical
release remains non-compliant. A later compliant release cannot relabel these dispositions.

Status note (2026-08-03 REL-5 T0): the Release Owner restored
`HEXALITH_RELEASE_PUBLISH_ENABLED` from exact `true` to exact lowercase `false` at
`2026-08-03T06:24:13Z`. The complete enabled interval was audited across Release runs, GitHub
Releases/tags, and all eight nuget.org package IDs. No partial external publication was observed.
This containment result does not authorize publication or close REL-AI-1.

Status note (2026-08-03 REL-5 T1/T2): the Release Owner approved DigiCert's production RFC 3161
authority at `http://timestamp.digicert.com` after official-documentation review, a live SHA-256
timestamp response, and verification against the .NET 10.0.302 stock NuGet timestamp trust bundle.
Repository-scoped Actions-secret custody and the rotation procedure are approved, but no production
certificate or password was supplied and the repository secret-name list remains empty. Therefore
the production signing identity is not approved, AC1/AC2 remain open, and publication remains
unauthorized.

Status note (2026-08-04 supersession): author signing, a production certificate, and an author
timestamp are no longer FrontComposer release requirements. The current manual production flow
uses exact unsigned GitHub candidates and requires NuGet.org repository-signature verification plus
normalized package-content equality after excluding only the root `.signature.p7s` added by the
repository. The certificate-oriented 2026-08-03 note and prerequisite table remain historical
audit evidence only. REL-AI-1 remains open pending confirmation that the NuGet.org owner signer
policy permits unsigned uploads and the first real operator-approved release evidence.

Status note (2026-08-14 REL-5 T4): developer-assisted independent downloaded-byte
verification of the operator-dispatched immutable GitHub Releases `v4.1.0` and `v4.1.1`
matched each sealed manifest, verified all eight NuGet.org repository signatures against
`https://api.nuget.org/v3/index.json`, and proved normalized package-content equality
after excluding only root `.signature.p7s`. `v4.1.1` is
**published-byte-verified / fallback-approved; pending owner FR24 sign-off**. That is not
an unqualified compliant disposition and does not close REL-AI-1. The current remaining
blocker is Release Owner confirmation of the downloaded-byte hashes and sign-off of this
ledger entry. Further production publication remains unauthorized until that sign-off.
Current `main` is also ineligible until push CI succeeds for that SHA.

## Required Fields

Each release record carries:

- release tag/URL and CI, Release, and Release Evidence run URLs;
- expected/observed package inventory;
- NuGet and GitHub asset identity/hashes;
- NuGet.org repository-signature verification and normalized package-content equality;
- manifest verification, readiness classification, and `publish_authorized`;
- package-consumer validation;
- durable evidence paths;
- compliance disposition, owner, remediation, and verification date.

## Summary

| Release | Inventory | Published signing | Manifest | Readiness | Consumer validation | Durable evidence | Disposition |
| --- | --- | --- | --- | --- | --- | --- | --- |
| v3.2.1 | exact 8 `.nupkg` + 8 `.snupkg` | GitHub: unsigned; NuGet: repository-signed only | invalid; rebuilt bytes | blocked; `publish_authorized=false` | passed on different `0.0.0-ci-test` bytes | no FR24 set on Release; expiring Actions artifact | **non-compliant / affected G1 release** |
| v3.2.2 | exact 8 `.nupkg` + 8 `.snupkg` | GitHub: unsigned; NuGet: repository-signed only | invalid; rebuilt bytes | blocked; `publish_authorized=false` | passed on different `0.0.0-ci-test` bytes | no FR24 set on Release; expiring Actions artifact | **non-compliant / affected G1 release** |
| v4.0.0 | exact 8 `.nupkg` + 8 `.snupkg` | GitHub: unsigned; NuGet: repository-signed only | invalid; rebuilt bytes | blocked; `publish_authorized=false` | passed on different `0.0.0-ci-test` bytes | no FR24 set on Release; expiring Actions artifact | **non-compliant / affected pre-REL-4 release** |
| v4.0.1 | exact 8 `.nupkg` + 8 `.snupkg` | GitHub: unsigned; NuGet: repository-signed only | invalid; rebuilt bytes | blocked; `publish_authorized=false` | passed on different `0.0.0-ci-test` bytes | no FR24 set on Release; expiring Actions artifact | **non-compliant / affected pre-REL-4 release** |
| v4.1.0 | exact 8 `.nupkg` + 8 `.snupkg` | GitHub: unsigned; NuGet: repository-signed only; content equal excluding `.signature.p7s` | valid `hexalith.release-evidence.v3`; seal `c1c9285ce970cf51…` | `fallback-approved`; `publish_authorized=true` | passed on sealed `nupkgs` candidates | immutable GitHub Release FR24 set | **non-compliant / published with failed Release conclusion** |
| v4.1.1 | exact 8 `.nupkg` + 8 `.snupkg` | GitHub: unsigned; NuGet: repository-signed only; content equal excluding `.signature.p7s` | valid `hexalith.release-evidence.v3`; seal `26c370411114470d…` | `fallback-approved`; `publish_authorized=true` | passed on sealed `nupkgs` candidates | immutable GitHub Release FR24 set | **published-byte-verified / fallback-approved; pending owner FR24 sign-off** |

## Complete Historical Reconciliation (2026-08-03)

The audit downloaded GitHub Release assets and nuget.org packages independently into a temporary
directory outside the repository. The directory is working material, not durable release evidence,
and no unavailable artifact was reconstructed and represented as original. SHA-256 values below
are lowercase hexadecimal over the downloaded bytes.

### Run and source topology

`Source SHA` is the source commit consumed by CI and Release. Semantic Release then created the
listed one-parent release commit and tag; Release Evidence ran at that tag commit. The
historical reusable workflow used mutable `@main`, but its log-resolved Builds commit is recorded.

| Release | Source SHA | Release/tag SHA | CI | Release | Release Evidence | Log-resolved Builds SHA |
| --- | --- | --- | --- | --- | --- | --- |
| [v3.2.1](https://github.com/Hexalith/Hexalith.FrontComposer/releases/tag/v3.2.1) | `e0a56f81d07024097db976ab20371ca8e3ca6394` | `d5935b833bc641475568aee573c584f0043afc83` | [29368280737](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/29368280737) | [29368461177](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/29368461177) | [29368682294](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/29368682294) | `9708e242e6334469f839670761fe61633dae8ce4` |
| [v3.2.2](https://github.com/Hexalith/Hexalith.FrontComposer/releases/tag/v3.2.2) | `4aa4210d4aeb066f8319ab8c7ced5bbfbe983b77` | `303715e377f7d6af24a228be0a2a06d97673c263` | [29375165477](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/29375165477) | [29375310946](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/29375310946) | [29375505915](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/29375505915) | `9708e242e6334469f839670761fe61633dae8ce4` |
| [v4.0.0](https://github.com/Hexalith/Hexalith.FrontComposer/releases/tag/v4.0.0) | `84273bac14c00e0051872d91ee9be8761317b2af` | `5eb42c701531c9207a2abbab514d3189bc2be81b` | [29458914880](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/29458914880) | [29459091412](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/29459091412) | [29459278484](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/29459278484) | `7e5c5bcb44c0a376dbcde1688f4e9b5911c82040` |
| [v4.0.1](https://github.com/Hexalith/Hexalith.FrontComposer/releases/tag/v4.0.1) | `d9c19a4fb837357af10f6f1aa630232f670557c4` | `0c873c3d0b5dd1e357887b952a8a655498fbe7ac` | [29465239941](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/29465239941) | [29465368153](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/29465368153) | [29465501315](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/29465501315) | `7e5c5bcb44c0a376dbcde1688f4e9b5911c82040` |

All twelve mapped runs completed with `success`; that conclusion is not a compliance disposition.

### Inventory, byte identity, signatures, timestamps, and symbols

Every Release contains exactly the eight expected IDs (`Hexalith.FrontComposer.Cli`,
`Hexalith.FrontComposer.Contracts`, `Hexalith.FrontComposer.Contracts.UI`,
`Hexalith.FrontComposer.Mcp`, `Hexalith.FrontComposer.Schema`,
`Hexalith.FrontComposer.Shell`, `Hexalith.FrontComposer.SourceTools`, and
`Hexalith.FrontComposer.Testing`) as both `<ID>.<version>.nupkg` and
`<ID>.<version>.snupkg`. `AppHost` and combined `UI` are the two explicit non-packable projects.

`dotnet nuget verify --all --verbosity detailed` produced the same structural result for every
row: the GitHub `.nupkg` and `.snupkg` failed with `NU3004` (unsigned; no timestamp), while the
nuget.org `.nupkg` succeeded with a NuGet.org **Repository** signature and RFC 3161 timestamp but
no Author signature. The repository signer is
`CN=NuGet.org Repository by Microsoft, O=NuGet.org Repository by Microsoft, L=Redmond,
S=Washington, C=US`, issued by DigiCert Trusted G4 Code Signing RSA4096 SHA384 2021 CA1. The
timestamp responder is DigiCert SHA256 RSA4096 Timestamp Responder 2025 1. Thus all 32 exact
GitHub/NuGet byte comparisons are `mismatch`; NuGet's repository signature changes the file bytes.
NuGet verification's content hash matches the unsigned content in all 32 pairs, but that is not
exact-byte identity and does not supply the missing Author signature.

Each of the 32 symbol assets is a valid ZIP and contains portable PDB material; the complete set is
11 PDBs per release. All symbol assets are unsigned and untimestamped.

#### v3.2.1 assets

| Package ID | GitHub nupkg SHA-256 | NuGet nupkg SHA-256 | `dotnet verify` timestamp (Europe/Paris) | GitHub snupkg SHA-256 |
| --- | --- | --- | --- | --- |
| Hexalith.FrontComposer.Cli | `f744bc138513b891c6cb9d764f62496b5705992fc8c5692c2fb106bc6cb2f0b7` | `f6ae08c19d5db6d265fa009ff493c9c22f99244fb8a5541e3282bd795b33ee92` | `2026-07-14 23:12:49` | `df9b133773b5f0ae487b3f6090724d8ecca80259313e380a67d72f58e226b5e2` |
| Hexalith.FrontComposer.Contracts | `c54a54a6cd9770b54e4e1171f2a5dfc1c8c05d8951d50076b0941b78dd1a242a` | `7201157145dc3767de227670ec31d95e78911e1a722c908f29f860f94bfdf7d3` | `2026-07-14 23:12:37` | `7dfa43ab7c792547d16a563e33bce38a49c8f3b452a52bb09b003895dfd1140a` |
| Hexalith.FrontComposer.Contracts.UI | `c7947dad12de53ff9fce33043428996776c28b6a782f7b935c3d3c94db6e8561` | `7f9cedc60834a49cb40f0e5fb3cf38460c4819dbaaa70c2de30153f08cba2828` | `2026-07-14 23:12:37` | `6186077a96e9daa9c70de663dcce9216a19ef84435c92a767e8f279be10b6dcf` |
| Hexalith.FrontComposer.Mcp | `694609ae31ef3a0db896e41d78eb556e2fb39f10286678c8b41e1151740351b2` | `527009ee1f63894ae41e2ac666eaa3a97a84df9b85303fb94e4b04f3b45e77b5` | `2026-07-14 23:12:39` | `2f99c45181af30f9fd1438aa09527de4aa6d5b6821e4e715b57f1b7c38b72d55` |
| Hexalith.FrontComposer.Schema | `c9614a00e83447f72ebf5c65224cd576c732c51be0ed9994b756cbb93a0d9a9c` | `f7bc74052fa81a50121919d6e4a3052b71c64d33462f92210106ec7cdbbe2aee` | `2026-07-14 23:12:44` | `f00cc17b3a631f417d84e88258488bf03fc3bd5240edd4dcc0d465913d49dea5` |
| Hexalith.FrontComposer.Shell | `5bf2541db86ff2108863d1b31893ae42aed7a151e2c5da94820f0358a28cf854` | `cb827a671e4e55c1c66766151591db6e195506fb07be1813225d0b52e25f2e5b` | `2026-07-14 23:12:40` | `b506fd8416fc53ca5a54efce08f80441d8d95d32ba106a6b875f733ecc639d6c` |
| Hexalith.FrontComposer.SourceTools | `0418cb9d28a48ff7d2d49142537da27745007c891ba9482153868678cbe4151b` | `149dd1663fc29e4af1ad66f7640f37f078a419d5f2ddf8222b84c45787689b94` | `2026-07-14 23:12:40` | `8532376c164a6e7ba52725dcc5b1570eb9134a15f4b90443f5dd62f5f87274b6` |
| Hexalith.FrontComposer.Testing | `adbea90ab9d8c453510ef4e498e046bff0eacc9af90c4caa9dfc093e6f1291d7` | `ad2ed055836dbb6b88f7b1be06e14e8e679fa8bdd1a948bf54fb20a38434ce50` | `2026-07-14 23:12:40` | `ed01111cd26300b064b7a3baa8a723f7918b3ad0a0b82804d33ff389cf32cf5c` |

#### v3.2.2 assets

| Package ID | GitHub nupkg SHA-256 | NuGet nupkg SHA-256 | `dotnet verify` timestamp (Europe/Paris) | GitHub snupkg SHA-256 |
| --- | --- | --- | --- | --- |
| Hexalith.FrontComposer.Cli | `2a89970bd34319d8719f02b21885db7136202110f42636ed64e29fd4410e1849` | `91cb2a40885a11c317f428b62ea7619d861eb787ad45a25d48990eceeccfb570` | `2026-07-15 01:13:52` | `38a42d05cdd1910ec32e8b28b4b9e26ec27da77c9d8e37f1765b6ee9a37b85e0` |
| Hexalith.FrontComposer.Contracts | `075ee7037385c65882a761b75b0520c9e04b94a521b46623be10c9bbc4af599f` | `380dd9ea8a311254c84a10b29efcf28034bea67db7f76138fb9846e084473d27` | `2026-07-15 01:14:01` | `b01b2bc3620ab83a81eb2235b74b5c254088ddbe2cea7e46b4062912c3740d24` |
| Hexalith.FrontComposer.Contracts.UI | `bb2327269d99c9d10db06a00e3dc59345a0d2f7e79fbe9b528a67eb4853f2658` | `0c03aa8ce4b7eedf0e2f67b8957cec8cb48e061fa9223108161bf9b782b00022` | `2026-07-15 01:13:53` | `0ad07775585e7486f98d34bf9dac50382bbb48ac3f0468f906aa25feaf6f74a0` |
| Hexalith.FrontComposer.Mcp | `8f876bc4a1ccaf050fc923ddaaf247a57358dcc60da2e59f9750cc1c7b353db2` | `368dc5a6f3da2ce2d45c4f697301a536e13bfbb9caf260b2ef9ad362dda62f64` | `2026-07-15 01:13:52` | `25914cbdb55de44658b491366d544ecdb03d60db9b08b45250d7ca2410115b7f` |
| Hexalith.FrontComposer.Schema | `5c25488e6c7c1ec32b65915c9122d4af4a0d192d433f2a06f2d1e7684bde67b7` | `d08ba5f6fa423d57b3819a10fa93ea026e5eab80278cde01cd57cd810628554f` | `2026-07-15 01:13:50` | `c76683f377ca8198c03c260297a4d97d6ccab0fcd36bbcf7973dda6da025b362` |
| Hexalith.FrontComposer.Shell | `ae37fed38c6dacd8731ed0ba0b33a6fe8067862c2c0dabb81f5d1c6d924a48a0` | `bdae1304d2e2bbe14fdcbcde819faa8e5be468161b46e3544b27c133af240308` | `2026-07-15 01:13:52` | `658fdbfc8c46e52c002a7777d2da38d3eeaedcc679ced8ec0fdb24d36178b284` |
| Hexalith.FrontComposer.SourceTools | `c19da033dad4b36a0266cc2115c98252ebc0a48483d2fd02255acc7715b8182d` | `ecf9751b19f164a219d29d4f6bfcf9f3c3d769b24894ac0888233b95d10735eb` | `2026-07-15 01:13:57` | `6baa3c947d12a7c2d0a4e3f9188583a756c307ce8649c5df20db49e4752f4388` |
| Hexalith.FrontComposer.Testing | `8e48328e9819e6e31eb5024f3620b30baa379f9078d8ead7f8bbcfbf16c1f8c0` | `44a3dd74606d707a140236fa2f4e4004818fd38b8ee5bff9a331829706fbdb54` | `2026-07-15 01:13:56` | `22e1cf8c9c1a0c82fd0000f41546660e6e2c0054aacf48a6bb63ea4bbebe329d` |

#### v4.0.0 assets

| Package ID | GitHub nupkg SHA-256 | NuGet nupkg SHA-256 | `dotnet verify` timestamp (Europe/Paris) | GitHub snupkg SHA-256 |
| --- | --- | --- | --- | --- |
| Hexalith.FrontComposer.Cli | `5f09629d80128bf38f339441b50187b224b2e201ccedf874831760d71b227356` | `34fdd742a5033ec1547cdf73d1fe66c6167144060b656ceda36b330fc7cd2acd` | `2026-07-16 01:41:03` | `8a8af248ab1e882cf774688b36d827ddf892e220ad1e1cf0a3b21655ee349a30` |
| Hexalith.FrontComposer.Contracts | `b6fad4892512275759c14e5a49b94d2a6cfce4df8ecaf7d1f1c27c49fadf7410` | `b2008a2859c80cb86c0d0f0165f87ab49861ccce3f93c33d5f1e49aca4b27130` | `2026-07-16 01:41:04` | `83591347ebc9bfac1286cb7a06be0cd2c5612eaf6dc09e2568d35395fdc92d2b` |
| Hexalith.FrontComposer.Contracts.UI | `1bd88fe44210311565e2f107448f94f9ef416709fd79658435be71309c9edbbc` | `30d06ba02edbc734532ff2a22c490ce3fdd4573b4d51e988a69ff3181f09661d` | `2026-07-16 01:41:01` | `dbf52c6e93438abeaa8b5b0170434d043292b94c5e7760286dd0ea0774914f47` |
| Hexalith.FrontComposer.Mcp | `02b98efa7e089bdf8f506ba88e2e8d0c2377f32d13370b7b90fc22942d5d73dd` | `e7a4c641fa2da979ac9393ef3ab243c6a0df9178e905435e609d48c63530e9d2` | `2026-07-16 01:41:01` | `96391139df021a95fac9dc8e0a510a83c4ef70c5d5c1200d06cffbc85a3540bf` |
| Hexalith.FrontComposer.Schema | `0f6f2bb7b00a7525cbe707a1c7672429fa54283fb53c51c43ce8c3c2347d88be` | `a0405d49c09953c8ee6c313bbb0fa697a05e4176b05fcce0b2192fff0b9fb15f` | `2026-07-16 01:41:01` | `f8de5485ed3fa4f907a2731249f1a69f0f37cb77b295f227fd94e0d20d02bf85` |
| Hexalith.FrontComposer.Shell | `61bc8c56eed04d6f6b4e86d112ad6f1a44e858e36af6a6d374b15897a5077d00` | `a07fa42a30cb6a764e7ff9d865ee1cdc061c8cb7ce0d880a3c7999b92e969842` | `2026-07-16 01:41:04` | `ca3bd579936c797b413c955910962fb8ba2055cd72f1a2e689053e302240fe94` |
| Hexalith.FrontComposer.SourceTools | `77166861dc5174cc3c1abbb20913f6d03ddb61f70b73cf7b8e3fd70e0994a64d` | `87a5cb7cd3ac4a9d8374963291d46debf3a816e83321d100c2ded36a661c637a` | `2026-07-16 01:41:02` | `7859e9f1dfd82dda675aa3202289c0679e12006c84bf4fa50e73d3f5ce8d3956` |
| Hexalith.FrontComposer.Testing | `8589c8fde667cd55b0462c69c5c598aecaf14892dd1590e3ec54bfaab2ccbc8d` | `418913e0dc32c817c5a8b73941371136545e577942f130690c396b93babc46b6` | `2026-07-16 01:41:02` | `dd5fcd5f383ca7997f5c4db759833a79018f7aef2d5eaff179fb0b7478217aec` |

#### v4.0.1 assets

| Package ID | GitHub nupkg SHA-256 | NuGet nupkg SHA-256 | `dotnet verify` timestamp (Europe/Paris) | GitHub snupkg SHA-256 |
| --- | --- | --- | --- | --- |
| Hexalith.FrontComposer.Cli | `f8bc3e8cde248a534fc57dda594f07b608a8480cb106553ba1cd7039e70e9d2c` | `9b6e001d56cd76f380bbff218417e29683d0f2ba439671505356987a853348f4` | `2026-07-16 04:00:09` | `c3b970ea97a987e5e8ad599524f4e45cc2097c3525314b16c74bb23158ce0684` |
| Hexalith.FrontComposer.Contracts | `48f6891705c1351efb5137873546282036a5b95d404bebe0c21a4edadf798e6b` | `bd49a377e1cc696d58aec7dc14bcde3203954fdf59ced4a63aa97f70f4a5f0e1` | `2026-07-16 04:00:09` | `823cf0bee5b67511b8ba239b877c6dadcc43e421d9ae141ec7920b0e647039f1` |
| Hexalith.FrontComposer.Contracts.UI | `f3b7e3864694efac009de0e6c7b867d48c9ebbfc1499cff214b5a5dc8585229a` | `dfcf2665e514128c83fdc6bb6f84d237a3c1056590f2188868146d1a3076356a` | `2026-07-16 04:00:07` | `b8dc635a034017d6b1a2f6d1ca68e7848443a8a4fcb5ad9aadcf3d95e32449e6` |
| Hexalith.FrontComposer.Mcp | `bc4382245f435da34c58b4890d3c83553288529709c323b8e8340c7556fd83d8` | `db55045a51b087147fcd71fb1ba1aed83019156ce7affb737b1d26a340f69ba0` | `2026-07-16 04:00:06` | `31f5ccbf236475aa653e6c1b8e0e3151189ebc083b2a06d905808d3debfcea51` |
| Hexalith.FrontComposer.Schema | `24b85bc03ef9bb0f8157e8697dfd118ac5f34b8968beb790cb4c47d42fd2cf56` | `28a1e50b2bfff1213b93a04bd809d73b428b5686e8c32cccb7282d4e668f9f1b` | `2026-07-16 04:00:08` | `b49837951103287afd104e645d1b8e51996a5257f2feb049ace09436ec3df6cd` |
| Hexalith.FrontComposer.Shell | `9b0e1dbf1b5cfa0b29c7f801bc2cb50004b958696494049598e731382883e7bb` | `dbbce17f308bda10ec326860b9bb107d8dcb15ffcd5d3d49d43ebe43c37f9d6b` | `2026-07-16 04:00:11` | `66567c675dc1172eb7c8e5635a050432ba4f409e90745951d65cd7c95593d043` |
| Hexalith.FrontComposer.SourceTools | `42230c379c4bf95d246439de89d3dfec54d6ee82889a3bd30a56cf5026f38e3a` | `bb7acce6a0b89e0a395b81374337b7fc41bf210d28d5d6ba65b747156347037b` | `2026-07-16 04:00:08` | `82d1cfa9960790e383ed294b220a96761bc7fd4375778c36e5c16ecadaa7c622` |
| Hexalith.FrontComposer.Testing | `c309249aea5fa42a90bc6d1786f9dfe2e7b71f14249d9592887ed6eee1b9bfba` | `076343a92756c15e57a00e43184b8690674ba542cf9572dca27e70646c57d3e0` | `2026-07-16 04:00:08` | `8d23205cfc8dce24818196f5f4f4239c72568e37483e9ff577a32ba4bb419928` |

### Original evidence availability and inspection

| Release | CI test artifact | Release artifact | Release Evidence artifact |
| --- | --- | --- | --- |
| v3.2.1 | `blocking-test-results` 8325046003 expired 2026-07-21 (7-day retention); logs available | none | `release-evidence-29368682294-1` 8325230239, available until 2026-08-13 |
| v3.2.2 | `blocking-test-results` 8327636008 expired 2026-07-21 (7-day retention); logs available | none | `release-evidence-29375505915-1` 8327787484, available until 2026-08-13 |
| v4.0.0 | `blocking-test-results` 8360459593 expired 2026-07-22 (7-day retention); logs available | none | `release-evidence-29459278484-1` 8360629193, available until 2026-08-14 |
| v4.0.1 | `blocking-test-results` 8362748585 expired 2026-07-23 (7-day retention); logs available | none | `release-evidence-29465501315-1` 8362857963, available until 2026-08-15 |

The available original Release Evidence artifacts were downloaded and inspected. Each contains
`benchmark-summary.json`, `checksums.json`, `manifest-verification.json`,
`package-inventory.json`, `pre-manifest.json`, `release-readiness.json`, `run-metadata.json`,
`sbom.json`, `sealed-manifest.json`, `signing-readiness.json`, and `test-results.json`; none contains
consumer-validation evidence. No GitHub Release contains these files, so this is expiring Actions
evidence rather than a durable release-attached FR24 set.

Common findings across all four artifacts:

- inventory is valid with eight packable and two explicit non-packable projects;
- symbols are present in the reconstructed package set, but are unsigned and untimestamped;
- `signing-readiness.json` records `signed=false`, `verified=false`, and `blocking=true`;
- `manifest-verification.json` is invalid with 40 diagnostics (five per package: missing checksum,
  signing verification, timestamp verification, concrete checksum, and sealed artifact);
- `release-readiness.json` records `classification=blocked`, `publish_authorized=false`,
  `candidate_evidence_used=false`, approval false, and 69 blocking reasons;
- the CycloneDX 1.6 SBOM parses and contains 302 components and 303 dependency entries, but its
  root version is `0.0.0` and it was generated over the post-release rebuilt set;
- tests summarize 4,122/0 failed for v3.2.1 and v3.2.2, 4,147/0 for v4.0.0, and 4,159/0 for
  v4.0.1; these are source-test summaries, not exact-candidate package-consumer results.

The invalid manifests' internal `seal.hash` values are retained only as identifiers for the
inspected blocked evidence, never as file-byte or authorization hashes: v3.2.1
`36a7dfbfae385f42bff073a640febefdd031cb7c581e4d83d126f530f8e76a6c`, v3.2.2
`4ef384ab5aae10834eabc523abdbb76a7905ae7f4975d5266c119416521bb22b`, v4.0.0
`73ab99d7f06fd1acac81213d51d59c5bae99a9f3bcb3b5370cd44fa466f3b4f1`, and v4.0.1
`41752c48c568bdd015e4484a061f62db4b6577022b4d28e6572c486772d6fd90`.

### Candidate lineage, checksums, and provenance

Consumer validation did **not** use the exact published candidates. In every CI log the workflow
removed `./nupkgs`, packed version `0.0.0-ci-test`, and restored/built consumers from that set. The
later Release workflow independently removed `./nupkgs`, packed the real version, and published it.

Release Evidence then independently packed the release version again after publication. Its
checksums match that reconstructed set 16/16 but match the GitHub Release assets 0/16 for every
release. Consequently its checksums, symbols, SBOM, inventory, manifest/readiness, and SLSA
subjects do not bind the published candidates. No separate consumer-validation, policy,
dependency-graph, or workflow-provenance file existed in the artifact. The Evidence SLSA
attestations cover only eight reconstructed `.nupkg` subjects (not symbols):

- [v3.2.1 reconstructed evidence attestation](https://github.com/Hexalith/Hexalith.FrontComposer/attestations/35340233)
- [v3.2.2 reconstructed evidence attestation](https://github.com/Hexalith/Hexalith.FrontComposer/attestations/35356284)
- [v4.0.0 reconstructed evidence attestation](https://github.com/Hexalith/Hexalith.FrontComposer/attestations/35549944)
- [v4.0.1 reconstructed evidence attestation](https://github.com/Hexalith/Hexalith.FrontComposer/attestations/35562935)

GitHub's separate release attestations do bind the exact 16 GitHub Release assets 16/16 for each
release, including symbols, but do not establish Author signing, exact NuGet byte identity,
pre-publication authorization, exact-candidate consumer validation, or the missing durable FR24
evidence set:

- [v3.2.1 release attestation](https://github.com/Hexalith/Hexalith.FrontComposer/attestations/35339693)
- [v3.2.2 release attestation](https://github.com/Hexalith/Hexalith.FrontComposer/attestations/35355836)
- [v4.0.0 release attestation](https://github.com/Hexalith/Hexalith.FrontComposer/attestations/35549398)
- [v4.0.1 release attestation](https://github.com/Hexalith/Hexalith.FrontComposer/attestations/35562697)

The historical reconciliation is complete with explicit non-closing residuals: expired CI
artifacts cannot be recovered as original evidence, Release Evidence artifacts are temporary, and
none of the four releases can retroactively acquire pre-publication authorization or exact-byte
candidate proof. All four remain non-compliant and cannot close REL-AI-1.

## Per-Release Dispositions

### v3.2.1

**Non-compliant / affected G1 release.** Inventory was complete, but published GitHub bytes were
unsigned, NuGet bytes differ and have only a repository signature, the manifest was invalid and
blocked, consumer validation and the evidence set used separately rebuilt packages, and no durable
FR24 set was attached to the release. Owner/remediation: Release Owner; retain this disclosure, do
not use the release as FR24 closure, and supersede its process with the governed exact-artifact
chain. Verified 2026-08-03 by the complete reconciliation above.

### v3.2.2

**Non-compliant / affected G1 release.** Same disposition and independently verified failure modes
as v3.2.1. Owner/remediation: Release Owner; retain this disclosure, do not use the release as FR24
closure, and supersede its process with the governed exact-artifact chain. Verified 2026-08-03 by
the complete reconciliation above.

### v4.0.0

**Non-compliant / affected pre-REL-4 release.** Run mapping and original Actions evidence confirm
the same blocked reconstructed-evidence process, unsigned GitHub packages, repository-signed-only
NuGet packages, exact-byte mismatches, and non-exact consumer validation. Owner/remediation:
Release Owner; preserve the historical classification, do not use the release as FR24 closure, and
supersede its process with the governed exact-artifact chain. Verified 2026-08-03.

### v4.0.1

**Non-compliant / affected pre-REL-4 release.** Run mapping and original Actions evidence confirm
the same blocked reconstructed-evidence process, unsigned GitHub packages, repository-signed-only
NuGet packages, exact-byte mismatches, and non-exact consumer validation. Owner/remediation:
Release Owner; preserve the historical classification, do not use the release as FR24 closure, and
supersede its process with the governed exact-artifact chain. Verified 2026-08-03.

## REL-5 T0 Enabled-Window Containment Audit

### Control evidence

| Field | Recorded evidence |
| --- | --- |
| Audit interval | `2026-08-02T08:27:15Z` through `2026-08-03T06:24:13Z` |
| Before state | repository variable `HEXALITH_RELEASE_PUBLISH_ENABLED=true`; `created_at` and `updated_at` both `2026-08-02T08:27:15Z` |
| After state | repository variable set to exact lowercase `false`; API `updated_at=2026-08-03T06:24:13Z` |
| Mutation side effect | changing the variable did not trigger a workflow or authorize a release |
| Publication status | unauthorized; retain non-`true` until the governed candidate/post-evidence authorization seam is approved |

### Release workflow audit

| Release run | Created (UTC) | Head | Execution result |
| --- | --- | --- | --- |
| [30743463963](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/30743463963) | 2026-08-02T10:17:53Z | `22c130d9` | Entered reusable job; runner-local `4.1.0` prepare failed closed at package-inventory validation before publication |
| [30757806987](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/30757806987) | 2026-08-02T16:57:48Z | `6521550a` | `freeze-guard` and release path skipped |
| [30757835682](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/30757835682) | 2026-08-02T16:58:34Z | `d9f0d526` | `freeze-guard` and release path skipped |
| [30757956331](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/30757956331) | 2026-08-02T17:01:39Z | `d9f0d526` | Entered reusable job; runner-local `4.1.0` prepare failed closed at package-inventory validation before publication |
| [30758637451](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/30758637451) | 2026-08-02T17:20:06Z | `4302301a` | Entered reusable job; runner-local `4.1.0` prepare failed closed at package-inventory validation before publication |
| [30760188983](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/30760188983) | 2026-08-02T18:01:57Z | `52f4327c` | Entered reusable job; runner-local `4.1.0` prepare failed closed at package-inventory validation before publication |
| [30785942090](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/30785942090) | 2026-08-03T05:00:32Z | `8a6a6cb3` | `freeze-guard` and release path skipped |

For all four entered jobs, `Semantic Release` stopped in its `prepare` phase with
`release_prepublish.py prepare --version 4.1.0` failing at the inventory command. The complete
release-evidence upload step was skipped. Locally generated runner candidates are not published
artifacts.

### GitHub publication surface

- No GitHub Release was created in the audit interval.
- No remote release tag is newer than `v4.0.1`.
- The latest release remains
  [v4.0.1](https://github.com/Hexalith/Hexalith.FrontComposer/releases/tag/v4.0.1), published
  `2026-07-16T02:00:00Z` with 16 package/symbol assets.

### NuGet publication surface

The nuget.org registration records contained no publication in the audit interval:

| Package registration | Latest version | Published (UTC) |
| --- | --- | --- |
| [Cli](https://api.nuget.org/v3/registration5-gz-semver2/hexalith.frontcomposer.cli/index.json) | `4.0.1` | 2026-07-16T01:59:39.51Z |
| [Contracts](https://api.nuget.org/v3/registration5-gz-semver2/hexalith.frontcomposer.contracts/index.json) | `4.0.1` | 2026-07-16T01:59:39.92Z |
| [Contracts.UI](https://api.nuget.org/v3/registration5-gz-semver2/hexalith.frontcomposer.contracts.ui/index.json) | `4.0.1` | 2026-07-16T01:59:40.26Z |
| [Mcp](https://api.nuget.org/v3/registration5-gz-semver2/hexalith.frontcomposer.mcp/index.json) | `4.0.1` | 2026-07-16T01:59:40.647Z |
| [Schema](https://api.nuget.org/v3/registration5-gz-semver2/hexalith.frontcomposer.schema/index.json) | `4.0.1` | 2026-07-16T01:59:41.013Z |
| [Shell](https://api.nuget.org/v3/registration5-gz-semver2/hexalith.frontcomposer.shell/index.json) | `4.0.1` | 2026-07-16T01:59:41.43Z |
| [SourceTools](https://api.nuget.org/v3/registration5-gz-semver2/hexalith.frontcomposer.sourcetools/index.json) | `4.0.1` | 2026-07-16T01:59:41.877Z |
| [Testing](https://api.nuget.org/v3/registration5-gz-semver2/hexalith.frontcomposer.testing/index.json) | `4.0.1` | 2026-07-16T01:59:42.237Z |

### Audit disposition

**No partial publication observed.** Neither GitHub Releases/tags nor any configured nuget.org
package ID gained a version during the enabled interval. REL-5 T0 is complete; REL-AI-1 remains
open and a future release still requires the complete FR24 prepublication and published-byte chain.

## REL-5 Current Production Prerequisites

| Field | Recorded evidence |
| --- | --- |
| Author-signing policy | **Retired 2026-08-04.** A production certificate, password, author signature, and RFC 3161 author timestamp are not FrontComposer release prerequisites. |
| Protected boundary | GitHub `production` environment configured with required review, administrator bypass disabled, and `main` branch restriction. |
| Publishing credential | The workflow exposes `NUGET_API_KEY` only to the protected release job under Release Owner custody. The secret may be organization-scoped; its value is never recorded here. |
| Immutable workflow identity | FrontComposer pins the selected reusable `domain-release.yml` and passes the identical approved 40-character commit as `builds-execution-sha`. |
| Entry point | Manual `workflow_dispatch` from the exact current `main` SHA, gated by a successful completed push CI run for that SHA before the protected job starts. |
| Package inventory | `tools/release-packages.json` declares exactly eight NuGet packages and no containers. |
| Post-publication trust | Exact GitHub candidate checksums plus `dotnet nuget verify --all`, a NuGet.org Repository-signature transcript bound to `https://api.nuget.org/v3/index.json`, and normalized package-content equality excluding only root `.signature.p7s`. |
| NuGet.org owner policy | **Confirmed 2026-08-13 by Administrator acting as Release Owner:** unsigned uploads are permitted for all eight package IDs declared by `tools/release-packages.json`. |
| Required owner action | Confirm the `v4.1.1` downloaded-byte hashes and sign off REL-AI-1 against the published-byte-verified / fallback-approved record below, or record the exact remaining blocker. This does not close REL-AI-1 by itself. |

The certificate, timestamp-authority, secret-presence, and rotation observations previously recorded
in this section are superseded requirements. Their source evidence remains in the dated historical
record in the REL-5 story and does not block the current release contract.

## First Governed Releases (2026-08-06), Independently Verified 2026-08-14

These records use independently downloaded GitHub Release and nuget.org `.nupkg` bytes. They are
not reconstructed candidates. Automatic Release Evidence did not produce a durable workflow
artifact for either release; the GitHub Release evidence set is the durable FR24 path. Working
copy `/tmp/rel5-verify` was ephemeral and is gone; do not reconstruct it as original evidence.
Hashes below for GitHub assets are the immutable Release `digest` values. NuGet `.nupkg`
SHA-256s are from the 2026-08-14 developer-assisted download. This verification is not Release
Owner confirmation.

`attestation=approved-unsupported` means GitHub artifact attestations were unavailable, so the
sealed owner-approved fallback path was used instead of `attestation_status=attested` plus an
attestation bundle. Combined with `classification=fallback-approved` and
`publish_authorized=true`, that authorizes the fallback publication model. It is not an
unqualified compliant disposition and does not close REL-AI-1.

Each GitHub Release has **29 assets**: 16 package/symbol files (8 `.nupkg` + 8 `.snupkg`) plus
13 attached evidence/other JSON files (`sealed-manifest.json`, `release-readiness.json`,
`checksums.json`, `package-inventory.json`, `dependency-release-source.json`,
`release-verification.json`, `sbom.json`, `consumer-validation.json`, `test-results.json`,
`prepared-candidate.json`, `pre-manifest.json`, `benchmark-summary.json`, and the
`partial-publish-incident.json` placeholder `frontcomposer.partial-publish-placeholder.v1`,
`classification=none`). The six named FR24 files required by `release-evidence.yml` are
`sealed-manifest.json`, `release-readiness.json`, `checksums.json`, `package-inventory.json`,
`dependency-release-source.json`, and `release-verification.json`.

`governed-release` was **skipped** on Release
[31116045352](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/31116045352).
That skip is not governed-mode completion. At `4a3e44f08308d9147ea797849451414af369551d` the
caller left `governed-release` unset/false and invoked the legacy `release` job of
`domain-release.yml@3ac633386faa2dc4c785bc1ffa06487974906d79`. Builds `governed-release` starts
only when `inputs.governed-release` is true; the sibling `release` job runs when it is false.
That is the AC6 single operator-dispatched publication path (retired transitional
`governed-release` input).

**NuGet.org `.snupkg` gap (both versions):** `v3-flatcontainer` returns HTTP 404 for every
symbol package. `https://www.nuget.org/api/v2/symbolpackage/{id}/{version}` returned HTTP 302;
the redirect was not followed, so no NuGet.org symbol bytes were persisted. NuGet.org `.snupkg`
SHA-256s and symbol content-equality are **not obtained**. GitHub `.snupkg` SHA-256s below are
the durable GitHub Release digests.

**`dotnet nuget verify` signer/timestamp summary (both versions):** all eight nuget.org
`.nupkg` files verified as Signature type Repository; subject
`CN=NuGet.org Repository by Microsoft, O=NuGet.org Repository by Microsoft, L=Redmond,
S=Washington, C=US`; issued by DigiCert Trusted G4 Code Signing RSA4096 SHA384 2021 CA1;
timestamp responder DigiCert SHA256 RSA4096 Timestamp Responder 2025 1; no Author signature.
`release_contract.py repository-signatures` bound each transcript to
`https://api.nuget.org/v3/index.json`. Retained detailed timestamp sample: Contracts `4.1.1`
`08/06/2026 18:05:45` (Europe/Paris). Per-package verify timestamps for the other nuget.org
`.nupkg` files were not retained after `/tmp/rel5-verify` was deleted and are not reconstructed.

### Run and source topology

Both tags are lightweight (`object.type=commit`), so Release/tag SHA equals Source SHA.

| Release | GitHub Release URL | Release id | Source / tag SHA | CI | Release | Release Evidence | Builds execution SHA |
| --- | --- | --- | --- | --- | --- | --- | --- |
| [v4.1.0](https://github.com/Hexalith/Hexalith.FrontComposer/releases/tag/v4.1.0) | https://github.com/Hexalith/Hexalith.FrontComposer/releases/tag/v4.1.0 | 366265698 | `ac6a9ae82eb94f72fa71280a216def7f971399aa` | [31082846751](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/31082846751) | [31107597159](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/31107597159) | [31108951693](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/31108951693) failed | `3ac633386faa2dc4c785bc1ffa06487974906d79` |
| [v4.1.1](https://github.com/Hexalith/Hexalith.FrontComposer/releases/tag/v4.1.1) | https://github.com/Hexalith/Hexalith.FrontComposer/releases/tag/v4.1.1 | 366336857 | `4a3e44f08308d9147ea797849451414af369551d` | [31113655691](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/31113655691) | [31116045352](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/31116045352) | [31119445480](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/31119445480) failed (Actions service unavailable) | `3ac633386faa2dc4c785bc1ffa06487974906d79` |

Both GitHub Releases are non-draft and `immutable=true`. Published
`2026-08-06T14:02:47Z` (`v4.1.0`) and `2026-08-06T16:05:59Z` (`v4.1.1`).

### nuget.org registrations and published timestamps (UTC)

Registration index:
`https://api.nuget.org/v3/registration5-gz-semver2/hexalith.frontcomposer.{id}/index.json`.

| Package ID | Registration | 4.1.0 published | 4.1.1 published |
| --- | --- | --- | --- |
| Hexalith.FrontComposer.Cli | [cli](https://api.nuget.org/v3/registration5-gz-semver2/hexalith.frontcomposer.cli/index.json) | 2026-08-06T14:02:00.017Z | 2026-08-06T16:05:17.1Z |
| Hexalith.FrontComposer.Contracts | [contracts](https://api.nuget.org/v3/registration5-gz-semver2/hexalith.frontcomposer.contracts/index.json) | 2026-08-06T14:02:03.63Z | 2026-08-06T16:05:20.343Z |
| Hexalith.FrontComposer.Contracts.UI | [contracts.ui](https://api.nuget.org/v3/registration5-gz-semver2/hexalith.frontcomposer.contracts.ui/index.json) | 2026-08-06T14:02:01.707Z | 2026-08-06T16:05:18.737Z |
| Hexalith.FrontComposer.Mcp | [mcp](https://api.nuget.org/v3/registration5-gz-semver2/hexalith.frontcomposer.mcp/index.json) | 2026-08-06T14:02:05.497Z | 2026-08-06T16:05:21.947Z |
| Hexalith.FrontComposer.Schema | [schema](https://api.nuget.org/v3/registration5-gz-semver2/hexalith.frontcomposer.schema/index.json) | 2026-08-06T14:02:07.267Z | 2026-08-06T16:05:23.61Z |
| Hexalith.FrontComposer.Shell | [shell](https://api.nuget.org/v3/registration5-gz-semver2/hexalith.frontcomposer.shell/index.json) | 2026-08-06T14:02:12.453Z | 2026-08-06T16:05:25.27Z |
| Hexalith.FrontComposer.SourceTools | [sourcetools](https://api.nuget.org/v3/registration5-gz-semver2/hexalith.frontcomposer.sourcetools/index.json) | 2026-08-06T14:02:14.267Z | 2026-08-06T16:05:27.06Z |
| Hexalith.FrontComposer.Testing | [testing](https://api.nuget.org/v3/registration5-gz-semver2/hexalith.frontcomposer.testing/index.json) | 2026-08-06T14:02:15.97Z | 2026-08-06T16:05:28.52Z |

### v4.1.0 assets

Sealed manifest `hexalith.release-evidence.v3`, seal
`c1c9285ce970cf513662343e8361d7f0f4996921749893b7c7ec70667bc7337f`,
file SHA-256 `8ecd260cea2de7b19427c3822ae4c8ba0af32218965fb77d35fddfb795ab327b`,
`sbom_hash=e58646b42085a3c5c834ae5dfbae1cda6eef9d176b73fa9351f297ab8cb4c82f`.
Readiness `classification=fallback-approved`, `publish_authorized=true`, attestation
`approved-unsupported`. Consumer validation `valid` against the sealed `nupkgs` directory.
Tests: **4226 passed, 0 failed**. Offline `verify-manifest` valid.
`require-published-readiness` ok. `dotnet nuget verify --all` plus
`release_contract.py repository-signatures` accepted all eight packages against
`https://api.nuget.org/v3/index.json`.

| Package ID | GitHub nupkg SHA-256 | NuGet nupkg SHA-256 | Content equal excl. `.signature.p7s` | GitHub snupkg SHA-256 | NuGet snupkg SHA-256 |
| --- | --- | --- | --- | --- | --- |
| Hexalith.FrontComposer.Cli | `9dad3d0e5192e7a8ea57ff8f491c143cec7f8def1923601b890e826ac52f1e02` | `94cc6be58e8a75f08140e64eb482c2b6ac37e27e2c43da16bcb5c71583756329` | yes | `06ff9e3ef7b52803e95a7bd8c75a24e19b89f69000433ceb4c3b47f081b1aabf` | not obtained (see gap) |
| Hexalith.FrontComposer.Contracts | `51ebddcf47c11686c2ea53a2e436c8c4b175b84a46b540d64aedd87fa3b8887e` | `1836ad44b51596e743d6c3a59ace6d5ca8d3a9d2a51c42156b7cb3f6b5b9ddcd` | yes | `9345f16bd4969e60cbbbc3553e9e9a12280fc58ade44cc8c44ac9ecff204959a` | not obtained (see gap) |
| Hexalith.FrontComposer.Contracts.UI | `a2ee65100d6ca26330a2e4ca216c4aad302964c2647d1ad34edbc6b57bad8f94` | `131bc545ef9b15d02a99778436046af05a80925d168d035126e693c7eeff80f9` | yes | `7a28dec51e9df5f0a9c1cf0961ff8458a86ce8bcbd2c82a251ce1092225f3d43` | not obtained (see gap) |
| Hexalith.FrontComposer.Mcp | `e73027e51dbc764924a6022338c5382201885106383182b0d6e85084e24a0a17` | `6cf5cb5b3a250b6ff2ec2b46f84a09d9568e3e19a216f8eb3d1307d7d8b80417` | yes | `0c57644fb8bc514cc8047cde05b2a036e9295e19e5d5a5660a0f266864afc525` | not obtained (see gap) |
| Hexalith.FrontComposer.Schema | `21bb626af99d55f1870db314db3164929ba5f232e952c112ff40d584fc4820be` | `4f34791b1b93efe2dbbfab181e30504f6bccd86ecd884961389a53cac7c0bfba` | yes | `c08a46988ba4fbb5ca725736a05130398d45f219d39fb40608b96805bbf55fc0` | not obtained (see gap) |
| Hexalith.FrontComposer.Shell | `7f579c0d9e1dcb7e75facd0ff183e1fe47477d6e2c43c7d564445a035b6c3c78` | `04d1e5353cff599085a233f693539a0ff3d3bbb34147aaf0dc34e7e7a1855cd1` | yes | `03c5f8fdd1bdf4582d6b19a2931205ab9f345f1c63d6d99f8d02893f9f1be603` | not obtained (see gap) |
| Hexalith.FrontComposer.SourceTools | `36ff12d87cf0d99271928abccc9f5a6b86c69758b9ada84734167d816595a344` | `fc5831a9876d87e27ea214f984d3ead2658252df1274d3a152be8ee5ade44221` | yes | `32f1da9624d221a00d20dbba6ed70b98c157e5ed97149a88d74c176fc121226b` | not obtained (see gap) |
| Hexalith.FrontComposer.Testing | `8a16b96263965ea07e25cf74e351cb5ccbf70b225042196f7a91c109f8a1f60d` | `aedba6e29bac85243817ef09b78a23c0b6718520b678f1b2064ffbaa05281055` | yes | `819c3a45cfca77b30eeb9c1b479d9c2636a511311c95380da4a8d310ef0f7f46` | not obtained (see gap) |

Durable GitHub package URLs:
`https://github.com/Hexalith/Hexalith.FrontComposer/releases/download/v4.1.0/<filename>`.

#### v4.1.0 FR24 evidence files (owner asked to accept)

| File | SHA-256 | Durable URL |
| --- | --- | --- |
| sealed-manifest.json | `8ecd260cea2de7b19427c3822ae4c8ba0af32218965fb77d35fddfb795ab327b` | https://github.com/Hexalith/Hexalith.FrontComposer/releases/download/v4.1.0/sealed-manifest.json |
| release-readiness.json | `153c85b1504dc95b1f747a59bbb888c208a99745b1a3294ffa9ef44186499dd6` | https://github.com/Hexalith/Hexalith.FrontComposer/releases/download/v4.1.0/release-readiness.json |
| checksums.json | `7421808299519746245d9d1edd2df3282bebf41ad75f8453c8a73e979a8e7898` | https://github.com/Hexalith/Hexalith.FrontComposer/releases/download/v4.1.0/checksums.json |
| package-inventory.json | `c1e6662f6042c6ec72165a666a51935a626867a5c2e259382a822b2530d30f0a` | https://github.com/Hexalith/Hexalith.FrontComposer/releases/download/v4.1.0/package-inventory.json |
| dependency-release-source.json | `a39d64f3c92080b31eaf708aaaf5e8b8edd7c033a6b3435d54b9ab94371a73dd` | https://github.com/Hexalith/Hexalith.FrontComposer/releases/download/v4.1.0/dependency-release-source.json |
| release-verification.json | `989ca997b87301f6a9faa7b8a34bef463d1b1e7dae8c3d8f0b0650f70530bde6` | https://github.com/Hexalith/Hexalith.FrontComposer/releases/download/v4.1.0/release-verification.json |
| sbom.json | `e58646b42085a3c5c834ae5dfbae1cda6eef9d176b73fa9351f297ab8cb4c82f` | https://github.com/Hexalith/Hexalith.FrontComposer/releases/download/v4.1.0/sbom.json |
| consumer-validation.json | `87a652c54cab11a1e2dedc1c70c1b8417bfcc3d3d79b4ce8bcedcb8571a9868c` | https://github.com/Hexalith/Hexalith.FrontComposer/releases/download/v4.1.0/consumer-validation.json |
| test-results.json | `bbe02ce00d61092bddc20c889d566161579fb54d971e2a2e97a461e069bdc257` | https://github.com/Hexalith/Hexalith.FrontComposer/releases/download/v4.1.0/test-results.json |
| prepared-candidate.json | `65213e326195b5c7ee030f8affacea20c550f3b9f6cd41ab862478f2785657f0` | https://github.com/Hexalith/Hexalith.FrontComposer/releases/download/v4.1.0/prepared-candidate.json |
| pre-manifest.json | `8ba30b774c56236901219cdfb4f1e37cce48e6d2c6be3785321b7abd4eb19e8c` | https://github.com/Hexalith/Hexalith.FrontComposer/releases/download/v4.1.0/pre-manifest.json |
| benchmark-summary.json | `639ca21a413e9cb4312f7d9f29e1cf83008b787116983fa8b2d0509f2e15d09c` | https://github.com/Hexalith/Hexalith.FrontComposer/releases/download/v4.1.0/benchmark-summary.json |
| partial-publish-incident.json | `7684cc6a6778126f7918226d885a4c72ba95bc2dddfbfa957b67003907059c81` | https://github.com/Hexalith/Hexalith.FrontComposer/releases/download/v4.1.0/partial-publish-incident.json |

### v4.1.1 assets

Sealed manifest `hexalith.release-evidence.v3`, seal
`26c370411114470d6a711fc637a3604d158e94f40adcdb8e07863f2cb84128f0`,
file SHA-256 `1f5d022a98f1d294449931b0a8f685926a76f64e8c1fde4e4bdce2056c9f389c`,
`sbom_hash=a9bc844bf67674e2b136e05768d2b7969dc7646b2040cb44f4f395e1c8ce378c`.
Readiness `classification=fallback-approved`, `publish_authorized=true`, attestation
`approved-unsupported`. Consumer validation `valid` against the sealed `nupkgs` directory.
Tests: **4226 passed, 0 failed**. Offline `verify-manifest` valid.
`require-published-readiness` ok. `dotnet nuget verify --all` plus
`release_contract.py repository-signatures` accepted all eight packages against
`https://api.nuget.org/v3/index.json`.

| Package ID | GitHub nupkg SHA-256 | NuGet nupkg SHA-256 | Content equal excl. `.signature.p7s` | GitHub snupkg SHA-256 | NuGet snupkg SHA-256 |
| --- | --- | --- | --- | --- | --- |
| Hexalith.FrontComposer.Cli | `f5c308fdbd52cf799b87031662f333f26dc1626f327da7c15ebb0cc40948b1d1` | `2d2b24b81b86b04b0bb61d7f45c2a2851a68eb7843b97f781332b45f7bc2c871` | yes | `5a3f372129b97b39b579df9c1ce5c9232197fecdae17cea512dcc71b308c59a4` | not obtained (see gap) |
| Hexalith.FrontComposer.Contracts | `a4a10876cf841e8abc1693ae1b81a873af045a0d0cd5255e18b41c3c66c723bd` | `54057fb4d2bfbdca5e8302704a7173c01bcba95177addba4bf84bbf1b6dca35f` | yes | `f83dc0cdefcf3550c4ef5ced53e879a37147656ddae1f22b9394a79d955ecaf0` | not obtained (see gap) |
| Hexalith.FrontComposer.Contracts.UI | `f129e61f5e916a7617a1e969427ef1d851374301c6d0d4746e98f58d8a7e8a3a` | `08a4c6aa6fa1dfafb3da7aaee721cc1cfd60ec83b4e5af759120522de46c247e` | yes | `3a94f3af3fbcb9ea3c5411de12be423e008ad08a37a15de10f07283ae1f3ebd2` | not obtained (see gap) |
| Hexalith.FrontComposer.Mcp | `25124f898b8ab5249130e7e8a0d6e567597cca0b6a8b98879d38378eb4033dd8` | `4e1369be558b0a2b343ed4c6695ced354cb7aacbfd4d267813363d825f42318f` | yes | `b5be3a89102591e30f5390a79cc67b8d8bd3b791fbe4152421410e6371016c17` | not obtained (see gap) |
| Hexalith.FrontComposer.Schema | `2f5a54a9e51eb4cac8e0a79d338a46254ec721b1b01a78aa1c2971aadd76da93` | `f836df5202689fad77a178beb684c7a33a17dbd6c03b04b3dd9f4b10ee583577` | yes | `af6de404c731b319c32e80d8201aaf6d022e5f1b00d4a4514f528348fa79fb66` | not obtained (see gap) |
| Hexalith.FrontComposer.Shell | `46694aec97c1e1c7ba876cc01dd4f4682c8f5f353ceaea4b41e037aa88caa9df` | `ed728b7a1b0a3de4e8d7e1a171ec2a04dc10ba7c39e75d3557bb4b5e047ec2e9` | yes | `7c24ac2c014c72974a24b34a7a95788693b060dc468e35a18ddc82e9e0c97af3` | not obtained (see gap) |
| Hexalith.FrontComposer.SourceTools | `db3d853d1cb5c7e28c8f30b285574312cd2812bf4fa823622d0fe4ed89b7cc32` | `96ae223e0ad0183e1ab89aacb73d3bbc5adfd25d210cbc1b76e973a5d17bd22f` | yes | `bd4b7fc5b856516662a1eeb9561f4a49b71b208a1ebbe5fc415447577e26bfbb` | not obtained (see gap) |
| Hexalith.FrontComposer.Testing | `1e026f736f60b219d9398b8c705a9eaeb7857bea756907a56448166957c0994e` | `e946761862c1511305f9bd83ed501d956d6f828f9e54878fdc41dab9a013fdd7` | yes | `a00a51b09664f52e0e6dd55d939ca4188c4993bac24b8c338b4ebebd79419814` | not obtained (see gap) |

Durable GitHub package URLs:
`https://github.com/Hexalith/Hexalith.FrontComposer/releases/download/v4.1.1/<filename>`.

#### v4.1.1 FR24 evidence files (owner asked to accept)

| File | SHA-256 | Durable URL |
| --- | --- | --- |
| sealed-manifest.json | `1f5d022a98f1d294449931b0a8f685926a76f64e8c1fde4e4bdce2056c9f389c` | https://github.com/Hexalith/Hexalith.FrontComposer/releases/download/v4.1.1/sealed-manifest.json |
| release-readiness.json | `040175007d6719598d2f87e412ac8ba429e859d9961989c6d9d0ee76a547409a` | https://github.com/Hexalith/Hexalith.FrontComposer/releases/download/v4.1.1/release-readiness.json |
| checksums.json | `59146695d109976a593192c4c09d91a3b01fdfa1d8cff908ec02a18037b5d3af` | https://github.com/Hexalith/Hexalith.FrontComposer/releases/download/v4.1.1/checksums.json |
| package-inventory.json | `c1e6662f6042c6ec72165a666a51935a626867a5c2e259382a822b2530d30f0a` | https://github.com/Hexalith/Hexalith.FrontComposer/releases/download/v4.1.1/package-inventory.json |
| dependency-release-source.json | `aaad2ebd5e8cb74a63377c12d940bbb1cffb4fcdde1a9ad8f3d9838405600585` | https://github.com/Hexalith/Hexalith.FrontComposer/releases/download/v4.1.1/dependency-release-source.json |
| release-verification.json | `989ca997b87301f6a9faa7b8a34bef463d1b1e7dae8c3d8f0b0650f70530bde6` | https://github.com/Hexalith/Hexalith.FrontComposer/releases/download/v4.1.1/release-verification.json |
| sbom.json | `a9bc844bf67674e2b136e05768d2b7969dc7646b2040cb44f4f395e1c8ce378c` | https://github.com/Hexalith/Hexalith.FrontComposer/releases/download/v4.1.1/sbom.json |
| consumer-validation.json | `87a652c54cab11a1e2dedc1c70c1b8417bfcc3d3d79b4ce8bcedcb8571a9868c` | https://github.com/Hexalith/Hexalith.FrontComposer/releases/download/v4.1.1/consumer-validation.json |
| test-results.json | `bbe02ce00d61092bddc20c889d566161579fb54d971e2a2e97a461e069bdc257` | https://github.com/Hexalith/Hexalith.FrontComposer/releases/download/v4.1.1/test-results.json |
| prepared-candidate.json | `015c098c87308933d5701dd6fb16b96a85e31e422bfa58615b45ad74d1741a8c` | https://github.com/Hexalith/Hexalith.FrontComposer/releases/download/v4.1.1/prepared-candidate.json |
| pre-manifest.json | `24ef5f45dc12eb5f41a538d7248293b8c232a233fd2a54ce268648d2862e1fc3` | https://github.com/Hexalith/Hexalith.FrontComposer/releases/download/v4.1.1/pre-manifest.json |
| benchmark-summary.json | `639ca21a413e9cb4312f7d9f29e1cf83008b787116983fa8b2d0509f2e15d09c` | https://github.com/Hexalith/Hexalith.FrontComposer/releases/download/v4.1.1/benchmark-summary.json |
| partial-publish-incident.json | `7684cc6a6778126f7918226d885a4c72ba95bc2dddfbfa957b67003907059c81` | https://github.com/Hexalith/Hexalith.FrontComposer/releases/download/v4.1.1/partial-publish-incident.json |

### Per-release dispositions

#### v4.1.0

**Non-compliant / published with failed Release conclusion.** Operator `workflow_dispatch` from
exact source `ac6a9ae82eb94f72fa71280a216def7f971399aa` after successful push CI published all
eight packages to nuget.org (per-package timestamps above) and created the immutable GitHub
Release. `prepare-candidate` and `release / release` succeeded; `verify-publication` failed
because that job had no checkout and could not open `eng/release_contract.py`. Automatic
Release Evidence
[31108951693](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/31108951693)
failed. Developer-assisted 2026-08-14 verification matches the sealed manifest and NuGet.org
repository signatures. Owner/remediation: Release Owner; retain this disclosure; do not use the
failed Release conclusion as REL-AI-1 closure; superseded by `v4.1.1`. Verified 2026-08-14.

#### v4.1.1

**published-byte-verified / fallback-approved; pending owner FR24 sign-off.** Operator
`workflow_dispatch` from exact source `4a3e44f08308d9147ea797849451414af369551d` after
successful push CI
[31113655691](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/31113655691),
protected `production` approval, and successful `verify-source` / `plan-release` /
`prepare-candidate` / `release / release` / `verify-publication` on
[31116045352](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/31116045352).
`governed-release` skipped as the retired transitional path (not governed-mode completion).
Published to nuget.org at the per-package timestamps above. Developer-assisted 2026-08-14
verification: 8/8 GitHub nupkg checksums, 8/8 GitHub snupkg checksums, 8/8 NuGet.org
repository signatures, 8/8 normalized nupkg content equalities, valid sealed manifest,
`fallback-approved` readiness, and durable GitHub Release evidence. This is not owner
confirmation and does not close REL-AI-1. Automatic Release Evidence
[31119445480](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/31119445480)
failed with GitHub Actions "Service Unavailable" and uploaded no artifact; that
infrastructure failure is independently superseded by the downloaded-byte record above.
Owner: Release Owner. Remediation: owner hash confirmation and REL-AI-1 sign-off remain
open. Verification date: 2026-08-14 (developer-assisted).

### Post-v4.1.1 bounded attempts (AC10)

No nuget.org version newer than `4.1.1` exists on any of the eight package IDs. Later operator
`workflow_dispatch` Release runs failed closed before the reusable publisher. `n/a` / `skipped`
means a later stage did not run. Protected-approval reviewer identities are not recorded here.

| Release run | Created (UTC) | Head SHA | Exact-source CI | Protected approval | Publication result | Verification result |
| --- | --- | --- | --- | --- | --- | --- |
| [31457030532](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/31457030532) | 2026-08-11T03:58:52Z | `36be4518fecbb334129288b863c015e788ddeb0c` | [31400882296](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/31400882296) success | skipped (`verify-source` failed) | skipped | skipped |
| [31538688047](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/31538688047) | 2026-08-11T21:36:37Z | `39303e30b143627fd4fa0ce01626915a98022c5f` | [31535696684](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/31535696684) success | skipped (`verify-source` failed) | skipped | skipped |
| [31579355332](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/31579355332) | 2026-08-12T08:39:55Z | `efa1c18f4cbc3ea060e169dcdc48e336e5b7e573` | [31568463789](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/31568463789) success | skipped (`verify-source` failed) | skipped | skipped |
| [31716385563](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/31716385563) | 2026-08-13T15:37:13Z | `d35548251aefad8e235b2fe69a4f9611fb741173` | [31715694001](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/31715694001) success | skipped (`verify-source` failed) | skipped | skipped |
| [31723965027](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/31723965027) | 2026-08-13T17:05:30Z | `7e7bd4c2af841e6d7fb0fdd41b8494336b37ab17` | [31723434672](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/31723434672) success | production job started; then `prepare-candidate` failed | skipped | skipped |
| [31779965137](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/31779965137) | 2026-08-14T07:26:59Z | `d31679c1855ccec94d82ce862ffb3a11917bab8e` | [31733278477](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/31733278477) success | production job started; then `prepare-candidate` failed | skipped | skipped |

`verify-source` / `plan-release` / `prepare-candidate` / `release` / `verify-publication` on
the first four runs: failure / skipped / skipped / skipped / skipped. On the last two:
success / success / failure / skipped / skipped. `emit-verification-handoff` succeeded on
these runs.

### REL-AI-1 remaining blocker

REL-AI-1 stays **open**. `v4.1.1` is published-byte-verified / fallback-approved; pending
owner FR24 sign-off. Closure requires Release Owner confirmation of the downloaded-byte
hashes and sign-off of this ledger entry. The developer agent cannot authorize that
sign-off. Further production publication remains unauthorized until that sign-off. Current
`main` (`f5e442c691fa8e77a155d5885622c6d65170d852` on 2026-08-14) has no successful
completed push CI, so a further dispatch from the exact current SHA is not eligible.


## Story 15.5 Compatibility Baseline Reconciliation (2026-10-26)

**Technical baseline: published `4.7.0`; no temporary lag.** Story 15.5's expected
`4.5.0` was a planning-time coordinate. The advance to `4.7.0` spans three
commits: `15577f42be42539b5e917cb7ae6716c2c11ac534` (2026-10-18) moved the
suppression ledger to `v4.7`; `ee9762e6c74914ccd33ec1d2caf322bdc21a10b4`
(2026-10-19) advanced the policy constant, shared default and Contracts.UI;
`a258b992d08e73e2d3251c865ef01a982eba31ac` (2026-10-21) advanced Quality's
package-validation gates from `4.6.0` to `4.7.0`. Retain that baseline; a tag or
the planned `currentRelease` value alone is not publication evidence.

### Technical publication evidence

**No FR24 disposition for v4.7.0 is recorded in this ledger.** The October 18
independent verifier classified the release as `compliant-candidate`; that
machine classification and the readiness fields below do not supply Release
Owner sign-off. The Summary ends at `v4.1.1`; attempt-keyed rows and dispositions
for `v4.2.0` and later remain the REL-LEDGER-2 backfill (Story 17.14). This
compatibility reconciliation does not perform that backfill or close REL-AI-1.

- [Immutable GitHub Release v4.7.0](https://github.com/Hexalith/Hexalith.FrontComposer/releases/tag/v4.7.0),
  release ID `416759469`, published `2026-10-18T22:57:16Z`, resolves to exact
  source/tag commit `cf5fe8da100bd5224a0d7f597fc92e1ebb98a5f4`.
- [Release run 39173030800, attempt 1](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/39173030800/attempts/1)
  and independent [Release Evidence run 39174645465, attempt 1](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/39174645465/attempts/1)
  both succeeded for that source. Both API records report `run_attempt=1`. The latter's immutable-release, sealed-manifest,
  downloaded NuGet byte and repository-signature verification steps succeeded.
- The published [sealed manifest](https://github.com/Hexalith/Hexalith.FrontComposer/releases/download/v4.7.0/sealed-manifest.json)
  uses `hexalith.release-evidence.v3` and binds the same source, tag and release
  run. Its SHA-256 is
  `2aa2d01e8cc6b8b489b016c2828af478192dcf68850ea9c1978d14b7d19f24ba`.
  Published [readiness](https://github.com/Hexalith/Hexalith.FrontComposer/releases/download/v4.7.0/release-readiness.json)
  is `fallback-approved` with `publish_authorized=true`; the existing attestation
  fallback expires `2026-11-04T13:31:17Z`. That expiry bounds use of the
  fallback to authorize publication; it does not retroactively invalidate this
  historical release or its use as the ApiCompat baseline. Future publication
  must meet its own then-current readiness and approval requirements. Published
  [consumer validation](https://github.com/Hexalith/Hexalith.FrontComposer/releases/download/v4.7.0/consumer-validation.json)
  and [package inventory](https://github.com/Hexalith/Hexalith.FrontComposer/releases/download/v4.7.0/package-inventory.json)
  report `valid`.
- The [October 18 publication record](spec-ci-release-publication-2026-10-18.md)
  records independent verification of all eight package contents against the
  sealed candidate, eight valid NuGet.org repository signatures, and no partial
  publication incident. Live verification-step results corroborate that record;
  this reconciliation does not claim to have repeated downloaded-byte verification.

Read-only NuGet checks on 2026-10-26 used the eight packable IDs from
`eng/release-package-inventory.json`. Every version index identified `4.7.0` as
latest stable and every `4.7.0` package endpoint returned HTTP 200:

| Package ID | Latest stable | Package endpoint |
| --- | --- | --- |
| Hexalith.FrontComposer.Cli | 4.7.0 | HTTP 200 |
| Hexalith.FrontComposer.Contracts | 4.7.0 | HTTP 200 |
| Hexalith.FrontComposer.Contracts.UI | 4.7.0 | HTTP 200 |
| Hexalith.FrontComposer.Mcp | 4.7.0 | HTTP 200 |
| Hexalith.FrontComposer.Schema | 4.7.0 | HTTP 200 |
| Hexalith.FrontComposer.Shell | 4.7.0 | HTTP 200 |
| Hexalith.FrontComposer.SourceTools | 4.7.0 | HTTP 200 |
| Hexalith.FrontComposer.Testing | 4.7.0 | HTTP 200 |

These metadata/availability checks support the existing publication record; they
are not a new package-signature or byte-equality audit. No published package was
reconstructed and no replacement evidence bundle was created.

### Compatibility and approval boundaries

`PUBLISHED_BASELINE_VERSION`, the shared `Directory.Build.targets` default,
Contracts.UI's explicit override, Quality's ApiCompat gate and existing governance
assertions already agree on `4.7.0`. The suppression ledger remains `v4.7` with no
rows. Existing ApiCompat validates seven packable libraries; the SDK excludes the
CLI's `PackAsTool` layout, whose inventory, symbols, installation and command-output
checks remain required. PublicAPI, schema/fingerprint, diagnostic, CLI JSON,
generated-output, analyzer-policy, Contracts-boundary, drift, snapshot and migration
contracts retain their existing authorities. Intentional API differences still need
the existing baseline, documentation and migration/deprecation process.

This technical baseline observation neither closes REL-AI-1 nor changes any
historical disposition, pending Release Owner FR24 sign-off, or publication
permission. Earlier "current" and "no newer version" statements above describe
their dated historical observations. No Release Owner lag decision is needed
because the selected baseline equals the latest evidence-backed stable release.
If evidence later prevents advancement, a temporary lag requires a dated owner
decision naming the reason, owner, expiry and blocking evidence; otherwise the
reconciliation stays open.

Focused commands, results and final story ownership are recorded in
[Story 15.5](spec-15-5-reconcile-the-published-compatibility-baseline.md).


### Story 15.5 follow-up observation (2026-10-29)

The original v4.7.0 package-inventory asset is `654681115`, with raw-byte
SHA-256 `c1e6662f6042c6ec72165a666a51935a626867a5c2e259382a822b2530d30f0a`.
A fresh read of the published JSON through the release-assets API confirmed
`status=valid`, eight packable rows, and that hash. The original sealed-manifest
asset `654681307` still hashes to
`2aa2d01e8cc6b8b489b016c2828af478192dcf68850ea9c1978d14b7d19f24ba`.
These observations complete the original inventory citation without changing
its publication or compliance disposition.

The earlier statement that 4.7.0 was the latest evidence-backed stable release
is dated October 26. A fresh October 29 check identifies immutable
[Release v4.7.1](https://github.com/Hexalith/Hexalith.FrontComposer/releases/tag/v4.7.1),
release `426062933`, published `2026-10-27T22:38:59Z`, at exact source/tag
`5688c3eba98567133285b0f51843d9daa8f24f0b`.
[Release attempt 1](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40593684166/attempts/1)
and independent
[Release Evidence attempt 1](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40596573358/attempts/1)
both succeeded for that source. All eight NuGet version indexes now identify
4.7.1 as latest stable; all eight historical 4.7.0 package endpoints still
return HTTP 200. This supersedes the earlier latest-release observation;
4.7.0 remains the currently configured baseline.

Story 15.5 is reopened for a human decision because its frozen intent
explicitly requires retaining 4.7.0. The proposed 4.7.1 advancement is recorded
in the [story's approved amendment](spec-15-5-reconcile-the-published-compatibility-baseline.md#approved-amendment--2026-10-29).
No advancement or bounded-lag approval is implied by this observation. The
current 4.7.0 configuration passes the existing same-release-line policy, but
that policy pass does not satisfy the story's latest-release acceptance or
supply a Release Owner lag decision. No FR24 disposition, owner sign-off or
new publication permission is supplied for either release.

### Story 15.5 Approved Compatibility Baseline Advancement (2026-10-29)

The user approved selecting published `4.7.1` on 2026-10-29. This supersedes
only the preceding observation's configured-baseline and pending-decision
statements. The dated `4.7.0` evidence, historical compliance dispositions and
independent owner approvals remain unchanged.

- [Immutable Release v4.7.1](https://github.com/Hexalith/Hexalith.FrontComposer/releases/tag/v4.7.1),
  release ID `426062933`, was published `2026-10-27T22:38:59Z`; the tag and
  original sealed manifest both bind exact source
  `5688c3eba98567133285b0f51843d9daa8f24f0b`.
- [Release 40593684166, attempt 1](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40593684166/attempts/1)
  (`workflow_dispatch`) and independent
  [Release Evidence 40596573358, attempt 1](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40596573358/attempts/1)
  (`workflow_run`) each report `run_attempt=1`, that exact source, and
  `conclusion=success`. The independent
  [verify-published-release job 122513885369](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40596573358/job/122513885369)
  passed its sealed-manifest-over-downloaded-bytes, exact NuGet publication,
  and served-byte NuGet.org repository-signature verification steps.
- The original published JSON assets were read through the release-assets API
  and hashed before parsing or reserialization:

| Original v4.7.1 asset | Asset ID | Raw-byte SHA-256 | Observed contents |
| --- | --- | --- | --- |
| [package-inventory.json](https://github.com/Hexalith/Hexalith.FrontComposer/releases/download/v4.7.1/package-inventory.json) | `683094182` | `c1e6662f6042c6ec72165a666a51935a626867a5c2e259382a822b2530d30f0a` | `status=valid`; eight packable rows. |
| [consumer-validation.json](https://github.com/Hexalith/Hexalith.FrontComposer/releases/download/v4.7.1/consumer-validation.json) | `683094079` | `87a652c54cab11a1e2dedc1c70c1b8417bfcc3d3d79b4ce8bcedcb8571a9868c` | `status=valid`; package metadata/kernel split and consumer boundaries passed. |
| [sealed-manifest.json](https://github.com/Hexalith/Hexalith.FrontComposer/releases/download/v4.7.1/sealed-manifest.json) | `683094417` | `f5ebb7d831c1a0c5b06a8f879109ab102b19c690c55f2e4ffa7af4ca64b782f8` | `hexalith.release-evidence.v3`; same source, `v4.7.1`, release run `40593684166`, and eight `4.7.1` package identities. |

All three hashes equal their original release API `sha256:` digests. Fresh
read-only checks used the existing eight-package inventory. On 2026-10-29 each
NuGet version index selected `4.7.1` as latest stable, and HTTP HEAD to each
`4.7.1` package endpoint returned 200:

| Package ID | Latest stable | Package endpoint |
| --- | --- | --- |
| Hexalith.FrontComposer.Cli | 4.7.1 | HTTP 200 |
| Hexalith.FrontComposer.Contracts | 4.7.1 | HTTP 200 |
| Hexalith.FrontComposer.Contracts.UI | 4.7.1 | HTTP 200 |
| Hexalith.FrontComposer.Mcp | 4.7.1 | HTTP 200 |
| Hexalith.FrontComposer.Schema | 4.7.1 | HTTP 200 |
| Hexalith.FrontComposer.Shell | 4.7.1 | HTTP 200 |
| Hexalith.FrontComposer.SourceTools | 4.7.1 | HTTP 200 |
| Hexalith.FrontComposer.Testing | 4.7.1 | HTTP 200 |

The policy constant, shared default, Contracts.UI override, Quality baseline and
`4.7.1-ci` pack/consumer coordinates, and focused baseline assertions now agree
on `4.7.1`; the MCP package test uses candidate `4.7.1-review.compat` and binary
`4.7.1.0`. All seven existing inventory-driven ApiCompat packs passed against
published `4.7.1`, with validation enabled and baseline skipping disabled.
The `v4.7` suppression ledger remains empty. No public contract was changed.

These observations read original published metadata and exact workflow-attempt
results. They do not repeat the independent workflow's downloaded-byte or
repository-signature audit, download its original verification artifact, or
claim new independent ledger booleans. Current ApiCompat packs build disposable
current candidates; they do not reconstruct historical published packages.
The required Contracts.UI public-surface check exposed the independently
added `WaitForSubscriptionAsync` member missing from its text inventory. Recording
that existing member through `PublicAPI.Shipped.txt` restored exact public-surface
parity; all four package-boundary tests then passed. Runtime contracts remain
unchanged. Exact fresh commands, the initial failure, its correction and the
independent review are recorded in the [story record](spec-15-5-reconcile-the-published-compatibility-baseline.md#approved-advancement-verification-2026-10-29).
This technical baseline selection grants no FR24 disposition, Release Owner
sign-off, new publication permission, or Fluent/Product approval, and performs
no REL-LEDGER-2 backfill.


Local completion after independent review also refreshes the existing derived
consumer-configuration seal for the two changed XML baseline sites. The exact
candidate's 69-edge graph validates with all dependency selections preserved;
21 focused projection checks pass. The original prior-active-policy push model
still refuses that configuration transition, so integration/activation remains
separately governed. Local compatibility completion does not claim a passing
push gate or hosted CI for this amendment; the story records the exact failed
command and retained output alongside the passing local results.


### Story 15.5 Activation Handoff Recheck (2026-10-29)

Read-only rechecks from source
`7319f44bcf02b67d7ee09a315ba8aa9c7251b1f6` still identify
[immutable v4.7.1](https://github.com/Hexalith/Hexalith.FrontComposer/releases/tag/v4.7.1)
as the latest stable GitHub release. Its ID, publication time and reported
JSON asset digests match the approved advancement above. The existing
inventory's eight NuGet version indexes each still select `4.7.1`; HEAD
requests to all eight `4.7.1` package endpoints return HTTP 200. The configured
baseline remains current; no advancement or lag exception is needed. These
are metadata/availability checks, not a repeated package-byte/signature audit.

The [later-base activation record](spec-15-5-governed-policy-transition.md)
now has an actual branch commit:
`cde40df71ebcef6c6c74c25a5a5644bebef3fc17`, whose direct parent is
`0b469e03c5ae81cf6756f7cd8f2f78df7074514c`. Fresh execution of
`python3 eng/dependency_graph.py --root . diff --event push --event-base 0b469e03c5ae81cf6756f7cd8f2f78df7074514c --candidate cde40df71ebcef6c6c74c25a5a5644bebef3fc17`
exits 0: the actual prior policy is at that event base, raw SHA-256
`49dfbe45fd1da400b8ee17815cb8272b63bf8370d14161c53e3a18381ef7391d`,
with zero graph changes and zero affected modules. This establishes that
bounded local transition under the existing contract. It does not reclassify
the original earlier-base refusal or establish hosted success.

Current checkout validation is a separate result:
`python3 eng/dependency_graph.py --root . validate --commit 7319f44bcf02b67d7ee09a315ba8aa9c7251b1f6`
exits 1: `git ls-tree a9455fb3b57a65233020f4b3fc61426514521d7c -- Props/Directory.Packages.props`
fails in the root Builds object store with `fatal: not a tree object`.
The successful activation check above must not be projected onto this later
source. No selector, dependency, policy or validator is changed by this recheck.

Exact hosted attempt-1 observations for that same later source are:

- [CI 40884129895, attempt 1](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40884129895/attempts/1)
  failed. Testing's `PackageBoundaryTests` reports two failures: the localization
  mismatch diagnostic expectation and clean consumer's catalog-version
  precondition. Its pinned expectation is `10.0.13`; the catalog selects
  `10.0.14`. The clean consumer fails before its package restore.
- [Quality 40884129344, attempt 1](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40884129344/attempts/1)
  failed its infrastructure-governance gate on the same missing catalog object
  as the local graph command. Gate 4, `Library Package Validation (ApiCompat baseline)`,
  is skipped. The successful browser jobs do not establish that skipped gate.

Fresh `python3 -m unittest tests/eng/test_pack_release_packages.py tests/eng/test_release_prepublish.py`
passes 60/60 (exit 0). The seven real ApiCompat packs and public-surface results
in the [approved implementation](spec-15-5-reconcile-the-published-compatibility-baseline.md#approved-advancement-verification-2026-10-29)
remain historical results for that implementation, not new results for this
later source. Two current blockers are handed off explicitly: the
[catalog-object lookup](deferred-work.md#story-155-current-catalog-object-blocker)
to FrontComposer dependency-governance maintainers, and the
[Testing catalog-version expectation](deferred-work.md#story-155-current-testing-catalog-version-blocker)
to package-test maintainers. Closure needs passing current-source graph and
infrastructure checks, both affected package tests with actual clean-consumer
restore, blocking hosted CI and the subsequently executed ApiCompat Gate 4.
No pipeline repair or fresh ApiCompat pass is claimed here.

The [handoff continuation](spec-15-5-reconcile-the-activation-handoff.md)
records local checks and review. Story 15.5 and REL-BASE-1 remain at review.
This append-only observation preserves original refusals, incidents and
Release Owner, publication, Fluent and Product approval states.


### Story 15.5 CI-blocker repair observation — 2026-10-29

The [authorized repair continuation](spec-15-5-repair-verification-blockers.md)
starts at 2fcb1e9b5802ed32fc387680c6e42369130e0384. Read-only hosted rechecks
before edits find that same main source:
[CI 40921392978 attempt 1](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40921392978/attempts/1)
failed on Testing's two localization expectations, and
[Quality 40921392023 attempt 1](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40921392023/attempts/1)
failed on missing Builds a9455fb3b57a65233020f4b3fc61426514521d7c; hosted Gate 4
is skipped. No repair-source hosted run is claimed.

Explicit bounded catalog preparation now acquires the exact selected objects
into the initialized root Builds store using active-base policy and canonical
identity, before ordinary read-only governance. The missing object was acquired
locally without changing catalog, gitlinks, dependency selections or nested
submodule state. Testing's expectation now matches catalog-selected 10.0.14.
The reviewed repair worktree's local checks pass 32 focused projection/acquisition/
workflow cases, 425 of 427 broader Python cases (2 skipped), 60 release-policy
cases, 5 Testing and 4 Contracts.UI
boundary cases, eight package validations and both additional fresh consumer
restore/builds. Testing's generated smoke test is compiled, not executed.

The existing Quality ApiCompat run block was executed locally unchanged:
seven inventory-derived library packs passed with baseline 4.7.1, skip=false,
validation enabled and isolated per-project output. This is fresh local
compatibility evidence, not a passing hosted Gate 4 or release/publication
approval. Exact commands, guard diagnostics and scratch log paths are in the
repair continuation.

After acquisition, current committed validate and actual-parent push diff both
exit 1: `consumer projection root source selectors changed`. The existing
projection's selector/Builds seal disagrees with current committed selections;
it is not resealed here. Infrastructure/CI governance remains 104 passed /
2 failed out of 106, including that guard and a separate local npm symlink loop.
The [deferred-work follow-up](deferred-work.md#story-155-repair-follow-up-and-preserved-gates)
keeps the remaining ownership and hosted closure conditions explicit.

The original earlier-base refusal and successful later activation retain their
original attribution. The concurrent a2ed597bfbf3e178928144963ad8c9cb63077b3c strict
attribution still fails with no matching story ID, unmapped disposition and four
listed-unowned delivery paths. No history rewrite, baseline movement or human
disposition is inferred. Story 15.5, REL-BASE-1, Release Owner, publication,
Fluent, Product and EventStore approval gates retain their prior states.


### Clean governance and source-authority continuation — 2026-10-30

The [new qualification record](spec-15-5-qualify-the-governance-transition.md)
continues this repair without changing its frozen intent, baseline, historical
results or completed bounded-repair disposition. It records every exact command,
source tuple, first/final environment result and remaining prerequisite.

HEAD/hosted main initially matched f3fef9f2dad49638521d7bc2fd3bf43c8056b516.
While this task was read-only, an external commit
e7e892b6ed584501f6dd18e11acd299948c0113f recorded the existing Platform pointer
and landed locally/remotely. Clean qualification is pinned to that source and its
actual parent f3fef9f2dad49638521d7bc2fd3bf43c8056b516. This task made no change
to dependency selections or unrelated workspace content.

The active policy still authorizes Platform b742b1b7a496ac72d6961ca6db5213d3df42d234,
root Builds 81225bcb6151c38ae891abd9110af17385e1b3b9 and root selector digest
ddf83f55971c78a6d265f46b3bc9d6364c1acf85cfc88ee5c3e60818c13f81ef. The exact
current tuple selects Platform a8e6276b512186b1e9e35160b8c150a50a17f789 and Builds
0b17f8022cdc23b8b242b000f5b2aa15580184ac, root selector digest
b6534b18c7f4462366def97b1a0e5e79d1224e17c3543833de3042895dc4a40d. Helper bytes,
configuration and .gitmodules still match, but the inventory and root tooling
source differ. No hashes were refreshed. AD-12 requires a reviewed inactive
precursor to land before a later event can use that prior policy; candidate
self-authorization and a scratch base remain forbidden. The ordinary current
validate, actual-parent diff and catalog preparation all exit 1 on the owner pin.

Clean checkout plus lockfile-only npm installation avoids the recorded self-link:
infrastructure/CI governance is 105/106, with the projection guard the sole failure.
The 427-case Python suite passes with two skips, 60 release regressions pass,
Testing/Contracts.UI boundary classes pass 5/5 and 4/4, all seven existing ApiCompat
library packs pass against 4.7.1, all eight package validations pass, and both
additional fresh package-only consumers restore/build. No new suppression, guard
change or nested initialization occurs. Exact logs are local scratch in
`/tmp/fc-15-5-governance-20261030-zqwjdxk5/`.

[CI 41023803195 attempt 1](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/41023803195/attempts/1)
passes its build/test job but fails dependency governance; [Quality 41023802429
attempt 1](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/41023802429/attempts/1)
fails catalog preparation. Both reach actual-base Platform 2cbb8a68d277bdd55e321972b8aa11ef015fc54e's
owner-pin refusal. Hosted infrastructure and ApiCompat remain skipped. These are
existing push runs of e7e892b6ed584501f6dd18e11acd299948c0113f, not PR results.

The relevant local gates remain failed, so no PR/push/merge is made under the
user's passing-gates prerequisite. Story 15.5 and REL-BASE-1 remain at review;
all independent approvals retain their dispositions. Strict attribution for
concurrent a2ed597bfbf3e178928144963ad8c9cb63077b3c was rerun and remains exit 1:
story-id=no-match, disposition=unmapped, four listed-unowned paths. No exemption
or reinterpretation is introduced.

This is a verification observation, not a release-compliance disposition. No
publication or independent approval is granted; historical release rows are unchanged.

Review-qualified observation at 2026-10-30 08:20:37 UTC: CI 41023803195
attempt 1 is completed/failure; Quality 41023802429 attempt 1 remains in_progress
without an overall conclusion, while its build-and-test job 123957050415 is
completed/failure with infrastructure and ApiCompat skipped. The current Platform
also selects nested Builds 1884a88ab80d3d0ae80c09a5ed9dfe23867d9fb9, whose catalog
object is absent locally; its digest is unavailable and no remote absence is
inferred. This remains an exact-metadata prerequisite after authorized policy
activation. The qualification record now contains full diagnostic code, actual-base
coordinates, named skip reasons and a verified archive outside temporary storage
with its checksum/retrieval path. No failed gate, old attribution failure or
independent approval is reclassified.


### Story 15.5 inactive precursor preparation — 2026-10-30

The [new bounded precursor](spec-15-5-prepare-the-inactive-policy-precursor.md)
uses starting HEAD b1d89648ae6feed1c89e48c265de013e1507fa97 and real remote-main
base e7e892b6ed584501f6dd18e11acd299948c0113f. The existing qualification commit
and unrelated dirty Platform checkout aa8c66edc782b1735539e60b2b949f7f4015e3e3
are preserved. Only four fields of the Platform projection are proposed: owner
and raw inventory for a8e6276b512186b1e9e35160b8c150a50a17f789, actual root Builds
0b17f8022cdc23b8b242b000f5b2aa15580184ac and the sealed nine-root selector tuple.
The supporting record compares all root and Platform inventory selectors, both
unchanged helper blobs, unchanged consumer configuration and substantive
25-file Builds.Tooling changes. No dependency selection, catalog, trust,
expectation, evaluator or validation guard changes.

Exact Builds 1884a88ab80d3d0ae80c09a5ed9dfe23867d9fb9 was acquired into an
isolated root Builds object store from the canonical URL with no recursive fetch,
checkout or nested initialization. Its raw catalog SHA-256 is
fa3df877f1304db565456dd6c79da7d13ad2cff600fea94331ea093523568a10. This resolves
the historical locally-missing metadata observation; it grants no projection,
source, catalog-adoption or runtime authority.

Explicit candidate-policy validation now reaches an independent Parties
committed-source refusal: at e2528a5d5853641f2b38e5e2e34bb02c0cc4be23, the
retained-source-mapping/pinned-tree/Props/Directory.Packages.props artifact
contains 307 direct PackageVersion rows, violating the existing all-tracked-file
inheritance guard. The full path, exact blob hash and command are in the new
record. No fixture exception or policy weakening is introduced. The user's
exception permits only the expected actual-base refusal; candidate validation
still blocks a draft PR. No activation or remote publication is claimed.

CI 41023803195 attempt 1 and Quality 41023802429 attempt 1 on
e7e892b6ed584501f6dd18e11acd299948c0113f are both completed/failure at the
2026-10-30 09:23:49 UTC pre-edit observation. CI's build/test job passed, but
its dependency gate failed; Quality failed catalog preparation and skipped
infrastructure/ApiCompat. These existing push results are not precursor PR
verification. All independent approvals and Story 15.5/REL-BASE-1 remain at their
previous dispositions. Concurrent a2ed597bfbf3e178928144963ad8c9cb63077b3c's
strict no-match/unmapped/four-listed-unowned failure remains explicit and open.


Local committed precursor f75d774d281aa6d4764d9576597ff98ecfbaa3a7 has policy
SHA-256 1d55bd290472beb0b8ef5fc44437c0ecea57d8df49330cf02e9e69d9832255d1.
The 427-case focused suite passes (425 passed, two optional integrations skipped),
and the clean Shell build passes with zero warnings/errors. Ordinary committed
validation fails the independent Parties retained-catalog rule; infrastructure/CI
governance is 105/106 with the same sole failure. Actual remote-main PR diff,
local direct-parent push diff and actual-base acquire-catalogs each exit 1 at the
preserved old projection owner pin. The actual-base refusal is expressly
permitted for a precursor PR and remains failed evidence; the separate candidate
failure is not permitted, so no draft PR/push is made. Strict attribution for this
new continuation passes at the local commit; that does not alter concurrent
a2ed597bfbf3e178928144963ad8c9cb63077b3c's strict failure. Full exact commands,
source review and limits are in the new record; no remote success is inferred.


A later live-base observation supersedes the initial remote-main coordinate:
at 2026-10-30 09:34:14 UTC, remote main is
65cc17f69f4d42a5a7e1a5f145c5e1c3090c4b15 and original-workspace HEAD is
12dd17c92d023735ea77fe2840bee00581b851b9. These externally created commits
record Platform aa8c66edc782b1735539e60b2b949f7f4015e3e3 and are preserved.
The local precursor remains pinned to its reviewed initial b1/a8e tuple; it does
not include the newer main commit (merge base remains e7) or authorize its owner.
The live target-base diff exits 1 at aa8c66ed's old-policy owner-pin guard, before
candidate/ancestry checks. Carrying the proposal forward requires renewed tuple
and ancestry review as well as resolving the independent Parties candidate
failure; no silent rehash or rewrite is made. CI 41033896935 and Quality
41033896567 on new main are in_progress at 09:34:15 UTC, without inferred
conclusions. No precursor PR, push, merge, activation or approval is claimed.


Independent precursor review found six documentation/receipt gaps, all addressed
without policy/guard changes. Upstream candidate prerequisite DW-1988 is open
with owner roles and explicit repair/review/validation completion criteria. Exact
receipts are retained outside /tmp at
`/home/administrator/.local/state/hexalith/frontcomposer/story-15-5-policy-precursor-20261030/precursor-receipts.tar.gz`,
SHA-256 `2c91b227148d46b97a1b0c24af2554c67e4ff95dc49b0e63011b9029b36f844f`, with a verified per-member manifest and retrieval
instructions in the new precursor record. This is machine-local retention, not
hosted release evidence. Its final-results/ annex separately records final
commit/ownership/hygiene and committed-source/live-base results. The precursor
remains unpushed and in-review; all Story 15.5 and independent approval blockers
remain explicit. The historical qualification record is unchanged.


### Story 15.5 resumed review: landing observed, current tuple refused — 2026-10-30

Local main dd575b67c82c377bd17c34fb3d14366277d2ac62 contains precursor merge
92eff2e7b1b0f53631c53fa8284b466e202ae921 and delivery commits
f75d774d281aa6d4764d9576597ff98ecfbaa3a7 / e65502da0b755e2299a32fe6c112cdb9fddff0b2.
That later ancestry observation supersedes no historical failed gate or approval
disposition and establishes no protected review or successful activation.

The three-reviewer resumed pass and 93 focused Python regressions complete;
strict current ownership and global artifact checks pass at inspected dd575.
Ordinary validation and actual-parent diff fail their owner-pin guards. Quality
41097179475 attempt 1 was in progress with Gate 2c pending at the recorded
17:17–17:19 UTC observation; no later CI conclusion is inferred.

Subsequent external commit f69562a2a1b0f11295278c4e715bbf7ecf2999f7 changes
all nine root selectors. Ordinary validation still fails selected Platform
eba576951e61e9763341463516247dcbf8fc0b90's pin. Independently the unchanged
inline-version predicate refuses selected Parties 8b0e23e437296691d0233fe86afc765063f9fef5's
root Directory.Packages.props; the old DW-1988 retained-file refusal remains
history. Current/parent nested Builds catalogs are unavailable locally, with no
invented digest. The [resumed precursor record](spec-15-5-prepare-the-inactive-policy-precursor.md#resumed-verification-and-external-landing--2026-10-30)
contains exact commands/revisions, evidence limits, review triage and renewed
source/selection prerequisites. No current semantic, infrastructure, runtime,
ApiCompat, publication or independent approval is granted. Story 15.5/REL-BASE-1
remain review. This resumed run makes no commit, push, merge or remote write.
