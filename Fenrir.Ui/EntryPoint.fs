module Fenrir.Ui.EntryPoint

open System.IO
open System.Reflection

open Xaml
open ConsoleFramework
open ConsoleFramework.Controls

let private loadFromXaml<'a when 'a :> Control> resourceName dataContext =
    // This function was copied from ConsoleFramework.ConsoleApplication.LoadFromXaml, but uses the current assembly
    // instead of the entry one.
    use stream = Assembly.GetExecutingAssembly().GetManifestResourceStream resourceName
    if isNull stream then failwithf "Could not load resource %s" resourceName

    use reader = new StreamReader(stream)
    let namespaces = ResizeArray<_>(seq {
        "clr-namespace:Xaml;assembly=Xaml"
        "clr-namespace:ConsoleFramework.Xaml;assembly=ConsoleFramework"
        "clr-namespace:ConsoleFramework.Controls;assembly=ConsoleFramework"
    })
    let control = XamlParser.CreateFromXaml<'a>(reader.ReadToEnd(), dataContext, namespaces)
    control.DataContext <- dataContext
    control.Created()
    control

let run path =
    let dataContext = MainDataContext()
    let window = loadFromXaml<Window> "Fenrir.Ui.MainWindow.xaml" dataContext

    let host = WindowsHost()
    host.Show window
    ConsoleApplication.Instance.Run host
