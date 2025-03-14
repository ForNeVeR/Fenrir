// SPDX-FileCopyrightText: 2020-2025 Fenrir contributors <https://github.com/ForNeVeR/Fenrir>
//
// SPDX-License-Identifier: MIT

module Fenrir.Git.Metadata

type GitObjectType =
    | GitCommit = 0
    | GitTree = 1
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

type TreeAtom = {
    Mode: uint64
    Name: string
    Hash: byte array
}

type TreeBody = TreeAtom[]
