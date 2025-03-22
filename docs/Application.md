<!--
SPDX-FileCopyrightText: 2020-2025 Fenrir contributors <https://github.com/ForNeVeR/Fenrir>

SPDX-License-Identifier: MIT
-->

Fenrir.Application
==================

Download the application from [the releases section][releases], unpack, and then run it using .NET runtime 9.0 or later.

See the embedded help for a list of its abilities.

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

  print-commits <path to .git/ directory>
    Print all the commits in the repository.

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

[releases]: https://github.com/ForNeVeR/Fenrir/releases
