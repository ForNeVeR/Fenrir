namespace Fenrir.Ui

open System.Collections.Generic
open Binding.Observables

open Fenrir.Ui.Framework
open Fenrir.Ui.Models

type FilesViewModel(repository: GitRepositoryModel, commits: CommitsViewModel) =
    inherit LoadableViewModelBase()

    let mutable files: IReadOnlyList<TreeItemModel> = upcast Array.empty
    member val FilePathList = ObservableList<string>(ResizeArray())

    override this.Initialize() =
        PropertyUtil.onPropertyChange commits (nameof commits.SelectedCommit) (fun () ->
            // TODO: Cancel previous request if it wasn't applied yet
            this.IsLoading <- true
            match commits.SelectedCommit with
            | None ->
                this.FilePathList.Clear()
                this.IsLoading <- false
            | Some commit ->
                Async.runTask(async {
                    let! newFiles = repository.ReadFilesAsync commit
                    files <- newFiles
                    do! Async.SwitchToContext ConsoleFrameworkSynchronizationContext.instance
                    this.FilePathList.Clear()
                    files |> Seq.iter (fun file ->
                        this.FilePathList.Add file.RootedPath
                    )
                    this.IsLoading <- false
                })
        )
