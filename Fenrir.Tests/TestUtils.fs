// SPDX-FileCopyrightText: 2020-2025 Fenrir contributors <https://github.com/ForNeVeR/Fenrir>
//
// SPDX-License-Identifier: MIT

module Fenrir.Tests.TestUtils

open System
open System.IO
open System.Text
open System.Reflection
open TruePath

let ExecutingAssemblyDirectory: LocalPath =
    let location = Assembly.GetExecutingAssembly().Location
    (LocalPath location).Parent.Value

let TestDataRoot: LocalPath = ExecutingAssemblyDirectory / "Data"

let TestMoreDateRoot: LocalPath = ExecutingAssemblyDirectory / "Data2"

let toString (arr: byte array) =
    (arr |> Encoding.UTF8.GetString).Replace(Environment.NewLine, "\n")

let DoInTempDirectory<'a>(action: LocalPath -> 'a): 'a =
    let tempDirectory = Temporary.CreateTempFolder()
    try
        action(LocalPath tempDirectory)
    finally
        Directory.Delete(tempDirectory.Value, recursive = true)
