---
_disableBreadcrumb: true
---

<!--
SPDX-FileCopyrightText: 2024-2025 Friedrich von Never <friedrich@fornever.me>

SPDX-License-Identifier: MIT
-->

Fenrir: the Git library for .NET
================================
Fenrir is a .NET library to work with Git repositories. It provides functions to read Git objects, traverse the commit graph, extract trees and files from any commit, etc. Essentially, it provides tools to create your own Git client, or transform a repository in any way.

> [!CAUTION]
> Some of Fenrir's functions perform inline modifications of Git repositories and may destroy information and even commit history!
>
> Always make sure to take a backup before using Fenrir on important data!

(You should be safe as long as you aren't calling the modifying functions, though.)

The main thing that you should understand about a Git repository is that Git is an object database. Read [the corresponding section of the Git book][git-book.section-10] for more details on how the objects are packed.

Basically, the repository consists of [_refs_](api/Fenrir.Git.Refs.yml), each of whose are a reference to a _commit_. Every commit contains [certain metadata](api/Fenrir.Git.Metadata.yml) (among others, there are hashes of parent commits) and a corresponding _tree_. The tree may contain other trees or _files_ whose raw content is stored in objects. Any of these is identified by _hash_ and stored in the common object database.

Fenrir provides tools to navigate the commit graph and get any data on the Git objects, and some tools for working on refs.

Proceed to the documentation on @Fenrir.Git namespace to read more.

[git-book.section-10]: https://git-scm.com/book/en/v2/Git-Internals-Plumbing-and-Porcelain
