module Fenrir.Tests.TestUtils

open System
open System.IO
open System.Text
open System.Reflection

let testDataRoot: string =
    let location = Assembly.GetExecutingAssembly().Location
    Path.Combine(Path.GetDirectoryName(location), "Data")

let testMoreDateRoot: string =
    let location = Assembly.GetExecutingAssembly().Location
    Path.Combine(Path.GetDirectoryName(location), "Data2")

let toString (arr: byte array) =
    (arr |> Encoding.ASCII.GetString).Replace(Environment.NewLine, "\n")

let tempFolderForTest: string =
    let location = Assembly.GetExecutingAssembly().Location
    Path.Combine(Path.GetDirectoryName(location), "tempFolder")
