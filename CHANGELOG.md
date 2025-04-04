<!--
SPDX-FileCopyrightText: 2021-2025 Fenrir contributors <https://github.com/ForNeVeR/Fenrir>

SPDX-License-Identifier: MIT
-->

Changelog
=========
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog][keep-a-changelog]. See [the README file][docs.readme] for more details on how the project is versioned.

## [1.0.0] - 2025-03-24
### Changed
- **Breaking change summary:**
    - most of the APIs that accept any file system paths now require them wrapped in `TruePath.LocalPath`,
    - the newly introduced type `Fenrir.Git.Sha1Hash` is now thoroughly used in the public API,
    - a lot of synchronous functions performing blocking I/O have been converted to asynchronous,
    - a lot of APIs have been removed from the public surface.

- **Breaking changes:**
    - `Commands.SHA1` returns a `Sha1Hash` now;
    - `Commands.TreeStreams.Hashes` is now an array of `Sha1Hash` objects;
    - `Commands.commitBodyToStream` has been moved to `Commits.WriteCommitBody`, and now now requires non-curried arguments;
    - `Commands.createEmptyRepo` has been renamed to `InitializeRepository`, and now requires a `LocalPath` as an argument;
    - `Commands.getRawObjectPath` has been renamed to `Objects.GetRawObjectPath`, now requires `LocalPath` and `Sha1Hash` as arguments;
    - `Commands.guillotineObject` has been moved to `Objects.Guillotine`, and now requires non-curried arguments;
    - `Commands.headifyStream` has been moved to `Objects.WriteObject`, and now requires non-curries arguments and returns a `Sha1Hash`;
    - `Commands.parseCommitBody` has been moved to `Commits.ReadCommit`, now requires `PackIndex`, `LocalPath` and `Sha1Hash` as arguments (non-curried anymore), and returns a `Task` (i.e., made asynchronous);
    - `Commands.parseTreeBody` has been moved to `Trees.ParseTreeBody`, now requires `PackIndex`, `LocalPath` and `Sha1Hash` as arguments, and returns a `Task`;
    - `Commands.readHeader` has been renamed to `Objects.ReadHeaderFromStream`;
    - `Commands.readObjectHeader` has been moved to `Objects.ReadHeader`, now requires `PackIndex` (see below), `LocalPath` and `Sha1Hash` as arguments, returns a `Task`;
    - `Commands.streamToCommitBody` has been moved to `Commits.ReadCommitBody`;
    - `Commands.treeBodyToStream` has been moved to `Trees.WriteTreeBody`, and now requires non-curried arguments;
    - `Commands.updateObjectInTree` now requires a `PackIndex`, `Sha1Hash`es and `LocalPath` as arguments (non-curried), and returns a `Task`;
    - `Commands.verifyPack` has been renamed to `VerifyPackFile`, now requires a `LocalPath` as an argument, and requires the arguments in non-curried form;
    - `Commands.writeObjectHeader` has been moved to `Objects.WriteHeader`, and now requires non-curried arguments;
    - `Commands.writeStreamToFile` has been moved to `Objects.WriteToFile`, and now requires `LocalPath` and `Sha1Hash` as arguments (non-curried);
    - `Commands.writeTreeObjects` has been renamed to `WriteTreeObjects`, now requires `LocalPath` as an argument, and requires non-curried arguments;
    - `Metadata.CommitBody.Parents` is now an array of `Sha1Hash`es;
    - `Metadata.CommitBody.Tree` is now a `Sha1Hash`;
    - `Metadata.TreeAtom.Hash` is now a `Sha1Hash`;
    - `Metadata` is no longer a module but now a namespace, meaning all the nested types are now top-level types in the namespace;
    - `Ref.CommitObjectId` is now a `Sha1Hash`;
    - `Refs.ReadHeadRef` has been renamed to `ReadHead`;
    - `Refs.isHeadDetached` has been renamed to `IsHeadDetached`, and now requires a `LocalPath` as an argument;
    - `Refs.readRefs` has been renamed to `ReadRefs`, and now requires a `LocalPath` as an argument;
    - `Refs.updateAllRefs` has been renamed to `UpdateAllRefs`, now requires `LocalPath` and `Sha1Hash`es as arguments;
    - `Refs.updateHead` has been renamed to `UpdateHead`, now requires `Sha1Hash` and `LocalPath` as arguments (non-curried form);
    - `Sha1.calcSHA1Hash` has been renamed to `CalculateHardened`, and now returns a `Sha1Hash`;
    - `Zlib.packObject` has been renamed to `PackObject`, and now requires the arguments in the non-curried form;
    - `Zlib.unpackObjectAndReturnPackedLength` has been renamed to `UnpackObjectAndReturnPackedLength`, and now requires the arguments in the non-curried form;
    - `Zlib.unpackObject` has been renamed to `UnpackObject`, and now requires the arguments in the non-curried form.

- **Other changes:**
    - [#111](https://github.com/ForNeVeR/Fenrir/issues/111): added an API and implementation for faster commit enumeration.
    - `Packing.PackedObject.Stream` is now a general `Stream` instead of a `MemoryStream`.

### Removed
- **Breaking:**
    - `Commands` functions have been removed from the public API:
        - `SHA1`,
        - `changeHashInCommit`,
        - `changeHashInTree`,
        - `doAndRewind`,
        - `getHeadlessCommitBody`,
        - `getHeadlessTreeBody`,
        - `hashOfObjectInTree`,
        - `refsCommand`,
        - `streamToTreeBody`;
    - `DeltaCommands` module has been removed from the public API;
    - `PackVeritication` module has been removed from the public API;
    - `Refs` functions have been removed from the public API:
        - `identifyRefs`,
        - `updateRef`;
    - everything from `Sha1` module except for the `CalculateHardened` function (see above) has been removed from the public API;
    - `Tools` module has been removed from the public API
        - (this includes all the extension methods for `BinaryReader` provided by the package);
    - `UbcCheck` module has been removed from the public API;
    - `Zlib.getDecodedStream` has been removed from the public API.

### Added
- The whole public API has been covered by documentation.

- A new `Commits` module, with several functions moved from the `Commands` module (see above).
- `Commits.TraverseCommits` for listing all the parent commits of a certain commit.
- A new `Objects` module, with several functions moved from the `Commands` module (see above).
- A new `Trees` module, with several functions moved from the `Commands` module (see above).

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
[1.0.0]: https://github.com/ForNeVeR/Fenrir/compare/v0.2.0...v1.0.0
[Unreleased]: https://github.com/ForNeVeR/Fenrir/compare/v1.0.0...HEAD
