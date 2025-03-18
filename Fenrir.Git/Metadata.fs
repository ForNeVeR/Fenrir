// SPDX-FileCopyrightText: 2020-2025 Fenrir contributors <https://github.com/ForNeVeR/Fenrir>
//
// SPDX-License-Identifier: MIT

module Fenrir.Git.Metadata

/// <summary>
/// Git object type. Read more on object types in <a href="https://git-scm.com/book/en/v2/Git-Internals-Git-Objects">the
/// documentation</a>.
/// </summary>
type GitObjectType =
    /// Commit object.
    | GitCommit = 0
    /// Tree object.
    | GitTree = 1
    /// Blob object, e.g. a file in a repository.
    | GitBlob = 2

type ObjectHeader = {
    Type: GitObjectType
    Size: uint64
}

type CommitBody = {
    Tree: string
    Parents: string[]
    Rest: string[]
}

type Commit = {
    Hash: string
    Body: CommitBody
}

type TreeAtom = {
    Mode: uint64
    Name: string
    Hash: byte array
}

type TreeBody = TreeAtom[]
