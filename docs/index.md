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
