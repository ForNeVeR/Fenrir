module Fenrir.Ui.EntryPoint

open System
open System.IO
open System.Reflection

open ConsoleFramework
open ConsoleFramework.Controls
open ConsoleFramework.Events
open global.Xaml

open Fenrir.Ui.Framework
open Fenrir.Ui.Models

let private loadFromXaml<'a when 'a :> Control> resourceName (dataContext: obj) =
    // This function was copied from ConsoleFramework.ConsoleApplication.LoadFromXaml, but uses the current assembly
    // instead of the entry one.
    use stream = Assembly.GetExecutingAssembly().GetManifestResourceStream resourceName
    if isNull stream then failwithf "Could not load resource %s" resourceName

    use reader = new StreamReader(stream)
    let control = XamlParser.CreateFromXaml<'a>(reader.ReadToEnd(), dataContext, ResizeArray())
    control.DataContext <- dataContext
    control.Created()
    control

#nowarn "40" // recursive handler object
let private initializeViewModelOnActivate (vm: ViewModelBase) (window: Window): unit =
    let rec handler = Action<obj, RoutedEventArgs>(fun _ _ ->
        vm.Initialize()
        EventManager.RemoveHandler(window, Window.ActivatedEvent, handler)
    )
    EventManager.AddHandler(window, Window.ActivatedEvent, handler)

let run (path: string): unit =
    Console.initialize()

    let repository = GitRepositoryModel path
    let refs = RefsViewModel repository
    let refsWindow = loadFromXaml<Window> "Fenrir.Application.Ui.RefsWindow.xaml" refs
    initializeViewModelOnActivate refs refsWindow

    let commits = CommitsViewModel(repository, refs)
    let commitsWindow = loadFromXaml<Window> "Fenrir.Application.Ui.CommitsWindow.xaml" commits
    initializeViewModelOnActivate commits commitsWindow

    let files = FilesViewModel(repository, commits)
    let filesWindow = loadFromXaml<Window> "Fenrir.Application.Ui.FilesWindow.xaml" files
    initializeViewModelOnActivate files filesWindow

    let host = WindowsHost()
    host.Show refsWindow
    host.Show commitsWindow
    host.Show filesWindow
    ConsoleApplication.Instance.Run host
