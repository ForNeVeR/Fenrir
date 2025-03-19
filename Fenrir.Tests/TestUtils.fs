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

let doInTempDirectory<'a>(action: string -> 'a): 'a =
    let tempDirectory = Path.GetTempFileName()
    File.Delete tempDirectory
    Directory.CreateDirectory tempDirectory |> ignore

    try
        action tempDirectory
    finally
        Directory.Delete(tempDirectory, recursive = true)
