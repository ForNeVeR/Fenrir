<!--
SPDX-FileCopyrightText: 2021-2025 Fenrir contributors <https://github.com/ForNeVeR/Fenrir>

SPDX-License-Identifier: MIT
-->

Changelog
=========
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog][keep-a-changelog]. See [the README file][docs.readme] for more details on how the project is versioned.

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
