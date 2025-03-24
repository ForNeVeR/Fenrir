<!--
SPDX-FileCopyrightText: 2020-2025 Fenrir contributors <https://github.com/ForNeVeR/Fenrir>

SPDX-License-Identifier: MIT
-->

Fenrir [![Status Ventis][status-ventis]][andivionian-status-classifier]
======
Fenrir is a .NET library to work with Git repositories.
It provides functions to read Git objects, traverse the commit graph,
extract trees and files from any commit, etc. Essentially,
it provides tools to create your own Git client or transform a repository in any way.

> [!CAUTION]
> Some of Fenrir's functions perform inline modifications of Git repositories and may destroy information and even commit history!
>
> Always make sure to take a backup before using Fenrir on important data!

Usage
-----
Fenrir is a .NET library, and thus you can install it into your program via NuGet. [Visit the package's page on nuget.org][nuget], or install the **Fenrir.Git** library using your preferred .NET package management tool.

[Read the API reference][docs.api] to know more of the usage patterns. While Fenrir is written in F#, it provides an API friendly to other .NET languages.

Fenrir also provides a terminal tool, **Fenrir.Application**, as a showcase of what is possible to do using the library.
Download the application from [the releases section][releases],
and see [the separate documentation file][docs.application] on the application.

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

[andivionian-status-classifier]: https://andivionian.fornever.me/v1/#status-ventis-
[docs.api]: https://fornever.github.io/Fenrir/
[docs.application]: docs/Application.md
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
[status-ventis]: https://img.shields.io/badge/status-ventis-yellow.svg
