﻿module Fenrir.Ui.EntryPoint

open System
open System.IO
open System.Reflection

open ConsoleFramework
open ConsoleFramework.Controls
open global.Xaml

open ConsoleFramework.Events
open Fenrir.Ui.Framework

let private loadFromXaml<'a when 'a :> Control> resourceName dataContext =
    // This function was copied from ConsoleFramework.ConsoleApplication.LoadFromXaml, but uses the current assembly
    // instead of the entry one.
    use stream = Assembly.GetExecutingAssembly().GetManifestResourceStream resourceName
    if isNull stream then failwithf "Could not load resource %s" resourceName

    use reader = new StreamReader(stream)
    let control = XamlParser.CreateFromXaml<'a>(reader.ReadToEnd(), dataContext, ResizeArray())
    control.DataContext <- dataContext
    control.Created()
    control

let private initializeViewModelOnActivate (vm: ViewModelBase) (window: Window): unit =
    EventManager.AddHandler(window, Window.ActivatedEvent, Action<obj, RoutedEventArgs>(fun _ _ ->
        vm.Initialize()
    ))

let run (path: string): unit =
    Console.initialize()

    let repository = GitRepositoryModel path
    let refs = RefsViewModel repository
    let refsWindow = loadFromXaml<Window> "Fenrir.Ui.RefsWindow.xaml" refs
    initializeViewModelOnActivate refs refsWindow

    let host = WindowsHost()
    host.Show refsWindow
    ConsoleApplication.Instance.Run host