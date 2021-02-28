module PdfSharpCore.Drawing.Avalonia.XGraphicsExtensions

open Avalonia
open Avalonia.Media
open PdfSharpCore.Drawing


let FromDrawingContext (context : DrawingContext, size) =
    let adapter = XGraphicsRendererAdapter context
    XGraphics.FromRenderer(adapter, size, XGraphicsUnit.Point, XPageDirection.Downwards)


let PushPageTransform (context : DrawingContext, gfx : XGraphics) =
    let scale =
        match gfx.PageUnit with
        | XGraphicsUnit.Presentation -> 1.0
        | XGraphicsUnit.Point -> 96.0 / 72.0
        | XGraphicsUnit.Inch -> 96.0
        | XGraphicsUnit.Millimeter -> 96.0 / 25.4
        | XGraphicsUnit.Centimeter -> 96.0 / 2.54
        | _ -> 1.0
    context.PushPreTransform(Matrix.CreateScale(scale, scale))
