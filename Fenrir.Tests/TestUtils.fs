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
    Path.Combine(Path.GetDirectoryName(location), "moreData")

let toString (arr: byte array) =
    (arr |> Encoding.ASCII.GetString).Replace(Environment.NewLine, "\n")
