<!--
SPDX-FileCopyrightText: 2020-2025 Fenrir contributors <https://github.com/ForNeVeR/Fenrir>

SPDX-License-Identifier: MIT
-->

Contributor Guide
=================

Prerequisites
-------------
- [.NET SDK][dotnet-sdk] 9.0 or later.

Build
-----
```console
$ dotnet build
```

### Run From Sources
```console
$ dotnet run --project Fenrir -- [arguments...]
```

### Test
```console
$ dotnet test
```

Documentation Generator
-----------------------
To open the generated project documentation site locally, use the following shell commands:
```console
$ dotnet tool restore
$ dotnet build
$ dotnet docfx docs/docfx.json --serve
```

Then, open http://localhost:8080/ and browse the documentation.

License Automation
------------------
If the CI asks you to update the file licenses, follow one of these:
1. Update the headers manually (look at the existing files), something like this:
   ```fsharp
   // SPDX-FileCopyrightText: %year% %your name% <%your contact info, e.g. email%>
   //
   // SPDX-License-Identifier: MIT
   ```
   (accommodate to the file's comment style if required).
2. Alternately, use [REUSE][reuse] tool:
   ```console
   $ reuse annotate --license MIT --copyright '%your name% <%your contact info, e.g. email%>' %file names to annotate%
   ```

(Feel free to attribute the changes to the "Fenrir contributors <https://github.com/ForNeVeR/Fenrir>" instead of your name in a multi-author file, or if you don't want your name to be mentioned in the project's source: this doesn't mean you'll lose the copyright.)

[dotnet-sdk]: https://dot.net/
[reuse]: https://reuse.software/
