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

let doInTempDirectory<'a>(action: string -> 'a): 'a =
    let tempDirectory = Path.GetTempFileName()
    File.Delete tempDirectory
    Directory.CreateDirectory tempDirectory |> ignore

    try
        action tempDirectory
    finally
        Directory.Delete(tempDirectory, recursive = true)
