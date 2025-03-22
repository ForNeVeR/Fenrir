<!--
SPDX-FileCopyrightText: 2020-2025 Fenrir contributors <https://github.com/ForNeVeR/Fenrir>

SPDX-License-Identifier: MIT
-->

Fenrir [![Status Enfer][status-enfer]][andivionian-status-classifier] [![NuGet package][nuget.badge]][nuget.page]
======
Fenrir is a .NET library to work with Git repositories. It provides functions to read Git objects, traverse the commit graph, extract trees and files from any commit, etc. Essentially, it provides tools to create your own Git client, or transform a repository in any way.

> [!CAUTION]
> Some of Fenrir's functions perform inline modifications of Git repositories and may destroy information and even commit history!
>
> Always make sure to take a backup before using Fenrir on important data!

Usage
-----
Fenrir is a .NET library, and thus you can install it into your program via NuGet. [Visit the package's page on nuget.org][nuget], or install the **Fenrir.Git** library using your preferred .NET package management tool.

[Read the API reference][docs.api] to know more of the usage patterns.

Fenrir also provides a terminal tool, **Fenrir.Application**, as a showcase of what's possible to do using the library.

Download the application from [the releases section][releases], unpack, and then run it using .NET runtime 9.0 or later.
```console
$ dotnet Fenrir.Application.dll --help

Usage:

  guillotine [<input> [<output>]]
    Read git file and write decoded content of the file without header.

    If any of the <input> or <output> parameters isn't defined, then standard input and/or standard output are used instead.

  (help | --help)
    Print this message.

  object-type [<path>]
    Prints the type of the Git raw object read from the file system.

    If <path> isn't passed, then accepts raw object contents from the standard input.

  pack [<input> [<output>]]
    Reads the object file passed as <input> and packs the results to the <output>.

    If any of the <input> or <output> parameters isn't defined, then standard input and/or standard output are used instead.

  refs [<path to .git/ directory>]
    Shows branch list of repository.

  init [<path>]
    Create an empty Git repository or reinitialize an existing one

    If <path to .git/ directory> isn't passed, then current directory are used instead.

  save [<input> [<path to repository>]]
    Read text file and save it as object file to repository.

    If <path to repository> isn't passed, then current directory are used instead.
    If the <input> isn't defined, then standard input are used instead.

  ui [<path>]
    Shows UI to select a commit from the repository identified by <path> (.git subdirectory of current directory by default).

  unpack [<input> [<output>]]
    Unpacks the object file passed as <input> and writes the results to the <output>.

    If any of the <input> or <output> parameters isn't defined, then standard input and/or standard output are used instead.

  read-commit <path to .git/ directory> <commit-hash>
    Read a commit from a repository and print its metadata.

  update-commit <id of commit> [<path to repository>] <path of file>
    Read updated text file from <path to repository>/<path of file> and save it as new object file to repository.
    Moreover, save new trees based on <id of commit> parent tree, its subtrees to repository and commit file.

    If <path to repository> isn't passed, then current directory are used instead.

  update-with-trees <id of root tree> [<path to repository>] <path of file>
    Read updated text file from <path to repository>/<path of file> and save it as new object file to repository.
    Moreover, save new trees based on <id of root tree> tree and its subtrees to repository.

    If <path to repository> isn't passed, then current directory are used instead.

  verify-pack [<path to pack file>]
    Checks pack file integrity and print info about packed object — distribution of delta chains. Use -v option to see all containing objects.

  (version | --version)
    Print the program version.
```

If you are looking for some usage examples, check [the `GitRepositoryModel.fs` file][examples.git-repository-model], or see [the API documentation][docs.api].

Versioning Policy
-----------------
This project's versioning follows the [Semantic Versioning 2.0.0][semver] specification. Only changes in the NuGet package are considered. The example application can change at any time.

Documentation
-------------
- [Changelog][docs.changelog]
- [Contributor Guide][docs.contributing]
- [Maintainer Guide][docs.maintaining]

License
-------
The project is distributed under the terms of [the MIT license][docs.license].

The license indication in the project's sources is compliant with the [REUSE specification v3.3][reuse.spec].

[andivionian-status-classifier]: https://andivionian.fornever.me/v1/#status-enfer-
[docs.api]: https://fornever.github.io/Fenrir/
[docs.changelog]: CHANGELOG.md
[docs.contributing]: CONTRIBUTING.md
[docs.license]: LICENSE.txt
[docs.maintaining]: MAINTAINING.md
[examples.git-repository-model]: Fenrir.Application/Ui/Models/GitRepositoryModel.fs
[nuget.badge]: https://img.shields.io/nuget/v/Fenrir.Git
[nuget.page]: https://www.nuget.org/packages/Fenrir.Git
[nuget]: https://www.nuget.org/packages/Fenrir.Git
[releases]: https://github.com/ForNeVeR/Fenrir/releases
[reuse.spec]: https://reuse.software/spec-3.3/
[semver]: https://semver.org/spec/v2.0.0.html
[status-enfer]: https://img.shields.io/badge/status-enfer-orange.svg


## Development

```bash
# Install dependencies
npm install || pip install -r requirements.txt
# Run tests
npm test || pytest
```
