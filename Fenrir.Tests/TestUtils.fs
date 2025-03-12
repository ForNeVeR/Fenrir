module Fenrir.Tests.TestUtils

open System
open System.IO
open System.Text
open System.Reflection

let executingAssemblyDirectory: string =
    let location = Assembly.GetExecutingAssembly().Location
    Path.GetDirectoryName(location)

let testDataRoot: string =
    Path.Combine(executingAssemblyDirectory, "Data")

let testMoreDateRoot: string =
    Path.Combine(executingAssemblyDirectory, "Data2")

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
