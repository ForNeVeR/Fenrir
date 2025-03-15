<!--
SPDX-FileCopyrightText: 2025 Friedrich von Never <friedrich@fornever.me>

SPDX-License-Identifier: MIT
-->

---
uid: Fenrir.Git
summary: *content
---

This namespace contains all the tools to work with a Git repository. The main entry points for the user are:
- [Commands](api/Fenrir.Git.Commands.yml) for various high-level commands,
- [Refs](api/Fenrir.Git.Refs.yml) for working with [Git references][docs.git-refs],
- [Tools](api/Fenrir.Git.Tools.yml) for some useful utility functions.

If you are looking to work with a Git commit tree, then start by calling [Refs.readRefs](xref:Fenrir.Git.Refs.readRefs(System.String)), and then call [Commands.parseCommitBody](xref:Fenrir.Git.Commands.parseCommitBody(System.String,System.String)) in loop for the [Ref.CommitObjectId](xref:Fenrir.Git.Ref.CommitObjectId) and [CommitBody.Parents](xref:Fenrir.Git.Metadata.CommitBody.Parents).

If you are looking to traverse a file tree object (obtained from [CommitBody.Tree](xref:Fenrir.Git.Metadata.CommitBody.Tree) property), then call [Commands.parseTreeBody](xref:Fenrir.Git.Commands.parseTreeBody(System.String,System.String)) on the tree objects and check the types of the returned objects via [Commands.readObjectHeader](xref:Fenrir.Git.Commands.readObjectHeader(System.String,System.String)): if their @Fenrir.Git.Metadata.ObjectHeader.Type is [GitBlob](api/Fenrir.Git.Metadata.GitObjectType.yml) then they are files, otherwise they are subtrees and should be recursively processed.

[docs.git-refs]: https://git-scm.com/book/en/v2/Git-Internals-Git-References
