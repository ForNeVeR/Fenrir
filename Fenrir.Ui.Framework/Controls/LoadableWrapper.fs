namespace Fenrir.Ui.Framework.Controls

open ConsoleFramework.Controls
open ConsoleFramework.Core
open Xaml

/// This is a control that is able to show a placeholder with "Loading" word instead of its content.
[<ContentProperty("Content")>]
type LoadableWrapper() =
    inherit Control()

    let placeholder = TextBlock(Text = "Loading…",
                                VerticalAlignment = VerticalAlignment.Stretch,
                                HorizontalAlignment = HorizontalAlignment.Stretch)

    [<VolatileField>]
    let mutable loading = true
    let mutable content: Control = null

    member this.Content with get() = content
    member this.Content with set(value: Control) =
        value.Visibility <- if loading then Visibility.Visible else Visibility.Collapsed
        content <- value

    member _.IsLoading with get() = loading
    member this.IsLoading with set(value) =
        loading <- value
        placeholder.Visibility <- if value then Visibility.Visible else Visibility.Collapsed
        if not <| isNull content then
            content.Visibility <- if value then Visibility.Collapsed else Visibility.Visible
        this.Invalidate()

    member private this.ActiveControl: Control = if loading then upcast placeholder else this.Content

    override this.MeasureOverride(availableSize) =
        this.ActiveControl.Measure availableSize
        this.ActiveControl.DesiredSize

    override this.ArrangeOverride(finalSize) =
        this.ActiveControl.Arrange(Rect(finalSize))
        finalSize

    override this.Render(buffer) =
        this.ActiveControl.Render buffer
