module Fenrir.Tests.TestUtils

open System.IO
open System.Reflection

let testDataRoot: string =
    let location = Assembly.GetExecutingAssembly().Location
    Path.Combine(Path.GetDirectoryName(location), "Data")
