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
- [Commits](api/Fenrir.Git.Commits.yml) for working with Git commits.

For most of the commit enumeration functions, you'll be required to construct a @Fenrir.Git.PackIndex object first,
passing it a path to the repository.
Its reuse is important for optimization,
in case when you want to access several pieces of information in one repository.

If you are looking to work with a Git commit tree,
then start by calling [Refs.readRefs](xref:Fenrir.Git.Refs.readRefs(TruePath.LocalPath)),
and then call [Commits.ReadCommit](xref:Fenrir.Git.Commits.ReadCommit(Fenrir.Git.PackIndex,TruePath.LocalPath,Fenrir.Git.Sha1Hash)) in loop for the [Ref.CommitObjectId](xref:Fenrir.Git.Ref.CommitObjectId) and [CommitBody.Parents](xref:Fenrir.Git.Metadata.CommitBody.Parents).

If you are looking to traverse a file tree object
(obtained from [CommitBody.Tree](xref:Fenrir.Git.Metadata.CommitBody.Tree) property),
then call [Commands.ParseTreeBody](xref:Fenrir.Git.Commands.ParseTreeBody(Fenrir.Git.PackIndex,TruePath.LocalPath,Fenrir.Git.Sha1Hash)) on the tree objects
and check the types of the returned objects via [Commands.ReadObjectHeader](xref:Fenrir.Git.Commands.ReadObjectHeader(Fenrir.Git.PackIndex,TruePath.LocalPath,Fenrir.Git.Sha1Hash)):
if their @Fenrir.Git.Metadata.ObjectHeader.Type is [GitBlob](api/Fenrir.Git.Metadata.GitObjectType.yml),
then they are files,
otherwise they are subtrees and should be recursively processed.

[docs.git-refs]: https://git-scm.com/book/en/v2/Git-Internals-Git-References
