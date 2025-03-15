// SPDX-FileCopyrightText: 2020-2025 Friedrich von Never <friedrich@fornever.me>
//
// SPDX-License-Identifier: MIT

namespace Fenrir.Ui.Framework.Controls

open ConsoleFramework.Controls
open ConsoleFramework.Core
open ConsoleFramework.Rendering
open Xaml

/// This is a control that is able to show a placeholder with "Loading" word instead of its content.
[<ContentProperty("Content")>]
type LoadableWrapper() as this =
    inherit Control()

    let loadingText = "Loading…"

    [<VolatileField>]
    let mutable loading = true
    let mutable content: Control | null = null
    let children = Control.UIElementCollection(this) // used for UI event propagation to the child control

    member this.Content with get() = content
    member this.Content with set(value: Control) =
        value.Visibility <- if loading then Visibility.Visible else Visibility.Collapsed
        content <- value
        children.Clear()
        children.Add value |> ignore

    member _.IsLoading with get() = loading
    member this.IsLoading with set value =
        loading <- value
        let content = content
        match content with
        | null -> ()
        | content ->
            content.Visibility <- if value then Visibility.Collapsed else Visibility.Visible
        this.Invalidate()

    override this.MeasureOverride(availableSize) =
        if loading then Size(loadingText.Length, 1)
        else
            let content = nullArgCheck (nameof content) content
            content.Measure availableSize
            content.DesiredSize

    override this.ArrangeOverride(finalSize) =
        if not loading then
            let content = nullArgCheck (nameof content) content
            content.Arrange(Rect(finalSize))
        finalSize

    member private this.RenderLoadingPlaceholder(buffer: RenderingBuffer) =
        let attr = Colors.Blend(Color.Black, Color.DarkYellow)
        let actualWidth = this.ActualWidth
        let actualHeight = this.ActualHeight
        buffer.FillRectangle(0, 0, actualWidth, actualHeight, ' ', attr)
        let offset =
            let x = max 0 (actualWidth - loadingText.Length) / 2
            let y = actualHeight / 2
            Vector(x, y)
        for x in 0..actualWidth do
            for y in 0..actualHeight do
                if y = 0 && x < loadingText.Length then
                    buffer.SetPixel(offset.X + x, offset.Y + y, loadingText[x], attr)

        buffer.SetOpacityRect(0, 0, actualWidth, actualHeight, 3)

    override this.Render(buffer) =
        if loading then this.RenderLoadingPlaceholder buffer
        else
            let content = nullArgCheck (nameof content) content
            content.Render buffer
