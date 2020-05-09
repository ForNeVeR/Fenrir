module Fenrir.Ui.EntryPoint

open System.IO
open System.Reflection

open ConsoleFramework
open ConsoleFramework.Controls
open global.Xaml

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

let run path =
    Console.initialize()

    let refs = RefsViewModel path
    let commitWindow = loadFromXaml<Window> "Fenrir.Ui.RefsWindow.xaml" refs

    let host = WindowsHost()
    host.Show commitWindow
    ConsoleApplication.Instance.Run host
