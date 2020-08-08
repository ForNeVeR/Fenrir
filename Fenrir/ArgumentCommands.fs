module Fenrir.ArgumentCommands

module ExitCodes =
    let Success = 0
    let UnrecognizedArguments = 1

open System.IO

let private printUnrecognizedArguments argv =
    printfn "Arguments were not recognized: %A" argv

let unrecognizedArgs(argv: string[]): int =
    printUnrecognizedArguments argv
    ExitCodes.UnrecognizedArguments

let updateCommitOp (commitHash: string) (pathToRepo: string) (filePath: string) (detachedAllowed: bool): int =
    let pathToDotGit = Path.Combine(pathToRepo, ".git")
    let fullPathToFile = Path.Combine(pathToRepo, filePath)
    let oldCommit = Commands.parseCommitBody pathToDotGit commitHash
    let oldRootTreeHash = oldCommit.Tree
    use inputBlob = new FileStream(fullPathToFile, FileMode.Open, FileAccess.Read, FileShare.Read)
    use headedBlob = new MemoryStream()
    let blobHash = Commands.headifyStream Commands.GitObjectType.GitBlob inputBlob headedBlob
    Commands.writeStreamToFile pathToRepo headedBlob blobHash
    use treeStreams = Commands.updateObjectInTree oldRootTreeHash pathToDotGit filePath blobHash
    let newRootTreeHash = treeStreams.Hashes.[0]
    let newCommit = Commands.changeHashInCommit oldCommit (newRootTreeHash |> Tools.stringToByte)
    use inputCommit = Commands.commitBodyToStream newCommit |> Commands.doAndRewind
    use headedCommit = new MemoryStream()
    let commitHash = Commands.headifyStream Commands.GitObjectType.GitCommit inputCommit headedCommit
    Commands.writeStreamToFile pathToRepo headedCommit commitHash
    Commands.writeTreeObjects pathToRepo treeStreams
    ExitCodes.Success
