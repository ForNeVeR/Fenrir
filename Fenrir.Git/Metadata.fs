// SPDX-FileCopyrightText: 2020-2025 Fenrir contributors <https://github.com/ForNeVeR/Fenrir>
//
// SPDX-License-Identifier: MIT

namespace Fenrir.Git.Metadata

open Fenrir.Git

/// <summary>
/// Git object type. Read more on object types in <a href="https://git-scm.com/book/en/v2/Git-Internals-Git-Objects">the
/// documentation</a>.
/// </summary>
type GitObjectType =
    /// Commit object.
    | GitCommit = 0
    /// Tree object.
    | GitTree = 1
    /// Blob object, e.g., a file in a repository.
    | GitBlob = 2

/// Git object header.
type ObjectHeader = {
    Type: GitObjectType
    Size: uint64
}

/// Body of the Git commit. "Body" is a commit contents but without its own hash.
type CommitBody = {
    Tree: Sha1Hash
    Parents: Sha1Hash[]
    Rest: string[]
}

/// Git commit.
type Commit = {
    Hash: Sha1Hash
    Body: CommitBody
}

/// An element of a Git tree. Each tree record contains mode, name and hash of the tree item.
type TreeAtom = {
    Mode: uint64
    Name: string
    Hash: Sha1Hash
}

type TreeBody = TreeAtom[]
