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
    if not detachedAllowed && Refs.isHeadDetached pathToDotGit then
        printfn "You are in the detached head mode. Any repository modifications may turn it FUBAR.
If you are ready to spend a fair chunk of your time on Stack Overflow or aware of what you're doing, provide the --force key.
If at any moment your repository has turned FUBAR, consider revising the results of 'git log --reflog' to locate any commits missing."
    else
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
        let newCommitHash = Commands.headifyStream Commands.GitObjectType.GitCommit inputCommit headedCommit
        Commands.writeStreamToFile pathToRepo headedCommit newCommitHash
        Commands.writeTreeObjects pathToRepo treeStreams

        if Refs.isHeadDetached pathToDotGit then
            Refs.updateHead commitHash newCommitHash pathToDotGit
        Refs.updateAllRefs commitHash newCommitHash pathToDotGit
    ExitCodes.Success
