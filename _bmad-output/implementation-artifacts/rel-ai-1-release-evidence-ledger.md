---
title: REL-AI-1 Release Evidence Compliance Ledger
project: frontcomposer
created: 2026-07-15
updated: 2026-08-04
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

Historical records are not REL-AI-1 closure evidence. They document affected releases and why the
next compliant disposition requires a real operator-dispatched production release.

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
| Required owner action | Explicitly dispatch and approve one bounded production release, then retain the Release, Release Evidence, and immutable GitHub Release URLs for the compliant record. |

The certificate, timestamp-authority, secret-presence, and rotation observations previously recorded
in this section are superseded requirements. Their source evidence remains in the dated historical
record in the REL-5 story and does not block the current release contract.

## Next Compliant Release Record

Do not populate a passing disposition from a dry run or reconstructed evidence. The next record may be
marked compliant only after all of the following are durable:

- valid expected inventory, tests, and package-consumer validation against the release candidates;
- verified NuGet.org repository signatures on every downloaded `.nupkg` and normalized content
  equality with each exact unsigned GitHub candidate;
- required symbols and SBOM bound by complete checksums;
- valid sealed manifest over the exact candidate paths;
- `classify-release --require-publishable` with `classification=ready` and
  `publish_authorized=true` before publication;
- initial GitHub Release evidence assets;
- downloaded NuGet and GitHub bytes matching the authorized hashes;
- no unreconciled partial-publication incident.

REL-AI-1 remains open until the Release Owner records and signs off that real-release evidence.
