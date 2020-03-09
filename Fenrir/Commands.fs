﻿module Fenrir.Commands

open System
open System.IO
open System.Text.RegularExpressions
open ICSharpCode.SharpZipLib.Zip.Compression.Streams

let unpackObject (input: Stream) (output: Stream): unit =
    use deflate = new InflaterInputStream(input)
    deflate.CopyTo output

type GitObjectType =
    | GitCommit = 0
    | GitTree = 1
    | GitBlob = 2

let (|TypeRegex|_|) regex str =
    let m = Regex(regex).Match(str)
    if m.Success
    then Some m.Groups.[1].Value
    else None

type GitObjectOpen(input: Stream) =
    member x.TypeChecker() =
        use stream = new StreamReader(input)

        let line = stream.ReadLine()

        match line with
            | TypeRegex "commit (\d{1,})\0" _ -> Some GitObjectType.GitCommit
            | TypeRegex "tree (\d{1,})\0" _   -> Some GitObjectType.GitTree
            | TypeRegex "blob (\d{1,})\0" _   -> Some GitObjectType.GitBlob
            | _                               -> None

let printObjectPath(input: Stream): unit =
    let obj = GitObjectOpen(input)
    Console.WriteLine ("The type of the object:")
    match obj.TypeChecker() with
            | Some x -> Console.WriteLine x
            | None   -> Console.WriteLine "ERROR: probably, it's not git object file"
