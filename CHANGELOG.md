<!--
SPDX-FileCopyrightText: 2021-2025 Fenrir contributors <https://github.com/ForNeVeR/Fenrir>

SPDX-License-Identifier: MIT
-->

Changelog
=========
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog][keep-a-changelog]. See [the README file][docs.readme] for more details on how the project is versioned.

## [Unreleased]
### Changed
- **Breaking change summary:**
    - most of the APIs that accept any file system paths now require them wrapped in `TruePath.LocalPath`,
    - the newly introduced type `Fenrir.Git.Sha1Hash` is now thoroughly used in the public API,
    - a lot of synchronous functions performing blocking I/O have been converted to asynchronous.

- **Breaking changes:**
    - `Commands.getRawObjectPath` now requires `LocalPath` and `Sha1Hash` as arguments;
    - `Commands.readObjectHeader` has been renamed to `ReadObjectHeader`, requires `PackIndex` (see below), `LocalPath` and `Sha1Hash` as arguments, returns a `Task` (i.e., made asynchronous);
    - `Commands.refsCommand` now requires a `LocalPath` argument;
    - `Commands.parseCommitBody` has been moved to `Commits.ReadCommit`, requires `PackIndex`, `LocalPath` and `Sha1Hash` as arguments (non-curried anymore), returns a `Task` now;
    - `Commands.parseTreeBody` has been renamed to `Commands.ParseTreeBody`, requires `PackIndex`, `LocalPath` and `Sha1Hash` as arguments, and returns a `Task` now;
    - `Commands.SHA1` returns a `Sha1Hash` now;
    - `Commands.headifyStream` returns a `Sha1Hash` now;
    - `Commands.hashOfObjectInTree` returns a `Sha1Hash` now;
    - `Commands.changeHashInTree` requires a `Sha1Hash` for the `hash` argument now;
    - `Commands.changeHashInCommit` requires a `Sha1Hash` for the `hash` argument now;
    - `Commands.commitBodyToStream` has been moved to `Commits.CommitBodyToStream`;
    - `Commands.TreeStreams.Hashes` is now an array of `Sha1Hash` objects;
    - `Commands.updateObjectInTree` now requires a `PackIndex`, `Sha1Hash`es and `LocalPath` as arguments, and returns a `Task`;
    - `Commands.writeStreamToFile` now requires `LocalPath` and `Sha1Hash` as arguments;
    - `Commands.writeTreeObjects` now requires `LocalPath` as an argument;
    - `Metadata.CommitBody.Tree` is now a `Sha1Hash`;
    - `Metadata.CommitBody.Parents` is now an array of `Sha1Hash`es;
    - `Metadata.TreeAtom.Hash` is now a `Sha1Hash`;
    - `PackVerification.BaseRef.Hash` is now a wrapper over a `Sha1Hash`;
    - `PackVerification.VerifyPackObjectInfo.Hash` and `BaseHash` are now `Sha1Hash`es;
    - `PackVerification.PackedObjectInfo` has been renamed to `PackedObject`;
    - `PackVerification.getPackedObject` has been renamed to `ReadPackedObject`, now requires `PackIndex` and `Sha1Hash` as arguments, and returns a `Task`;
    - `Ref.CommitObjectId` is now a `Sha1Hash`;
    - `Refs.isHeadDetached` now requires a `LocalPath` as an argument;
    - `Refs.readRefs` now requires a `LocalPath` as an argument;
    - `Refs.identifyRefs` now requires `LocalPath` and `Sha1Hash` as arguments;
    - `Refs.updateRef` now requires a `Sha1Hash` as an argument;
    - `Refs.updateAllRefs` now requires `LocalPath` and `Sha1Hash`es as arguments;
    - `BinaryReader.ReadHash` extension method now returns a `Sha1Hash`.

- **Other changes:**
    - [#111](https://github.com/ForNeVeR/Fenrir/issues/111): added an API and implementation for faster commit enumeration.
    - `Packing.PackedObject.Stream` is now a general `Stream` instead of a `MemoryStream`.

### Removed
- **Breaking:**
    - `Commands` functions removed from the public API:
        - `getHeadlessCommitBody`,
        - `streamToCommitBody`;
    - `PackVeritication` functions removed from the public API:
        - `getPackPath`,
        - `parseIndexOffset`,
        - `getObjectKind`,
        - `getObjectMeta`,
        - `getNegOffset`,
        - `parsePackInfo`;
    - `Tools` module is removed from the public API.

### Added
- A new `Commits` module, with several functions moved from the `Commands` module.
- `Commits.TraverseCommits` for listing all the parent commits of a certain commit.
- A new type, `Metadata.Commit` — for keeping a `CommitBody` together with its `Hash`.
- A new type, `PackIndex` — to preserve and efficiently use the pack index data between different commands — for example, for faster commit traversal ([#111](https://github.com/ForNeVeR/Fenrir/issues/111)).
- A new type, `Sha1Hash`, as a quick wrapper over an SHA-1 hash value (represented as raw bytes).

## [0.2.0] - 2025-03-17
### Added
- [#105](https://github.com/ForNeVeR/Fenrir/issues/105): `Refs.ReadHeadRef` function to read the contents of the `HEAD` file in the repository.

### Fixed
- [#106](https://github.com/ForNeVeR/Fenrir/issues/106): broken reading of objects packed using delta encoding.

### Changed
- `Ref.Name` property is now nullable (to handle detached HEAD state).

## [0.1.0] - 2025-03-15
This is the first library release. It includes various functions, mainly to be able to read the repository's reference list, access commits attached to each reference, and read and parse any Git object's contents.

[docs.readme]: README.md
[keep-a-changelog]: https://keepachangelog.com/en/1.1.0/

[0.1.0]: https://github.com/ForNeVeR/Fenrir/releases/tag/v0.1.0
[0.2.0]: https://github.com/ForNeVeR/Fenrir/compare/v0.1.0...v0.2.0
[Unreleased]: https://github.com/ForNeVeR/Fenrir/compare/v0.2.0...HEAD
