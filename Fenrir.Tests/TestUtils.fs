module Fenrir.Tests.TestUtils

open System.IO
open System.Reflection

let getTestDirectory(dir: string) =
    let location = Assembly.GetExecutingAssembly().Location
    Path.Combine(Path.GetDirectoryName(location), dir)

let testDataRoot: string =
    getTestDirectory("Data")

let testOutputRoot: string =
    getTestDirectory("Outputs")
