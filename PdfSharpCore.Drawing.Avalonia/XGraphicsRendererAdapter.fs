namespace PdfSharpCore.Drawing.Avalonia

open System
open System.Collections.Generic
open Avalonia
open Avalonia.Media
open PdfSharpCore.Drawing
open PdfSharpCore.Drawing.Avalonia.Wrap


type internal XGraphicsRendererAdapter (ctx : DrawingContext) =
    static let logger = NLog.LogManager.GetCurrentClassLogger ()

    static let backend = AvaloniaBackend.Instance

    let stack = Stack<DrawingContext.PushedState> ()

    interface IXGraphicsRenderer with
        member __.Close () =
            while stack.Count > 0 do
                stack.Pop().Dispose()


        member __.DrawLine(xpen : XPen, x1 : float, y1 : float, x2 : float, y2 : float) =
            ctx.DrawLine(backend.GetPen xpen, Point (x1, y1), Point (x2, y2))

        member __.DrawLines (xpen : XPen, points : XPoint[]) =
            let g = makePolyLine (points, false, ValueNone)
            ctx.DrawGeometry (null, backend.GetPen xpen, g)

        member __.DrawBezier (xpen : XPen, x1 : float, y1 : float, x2 : float, y2 : float, x3 : float, y3 : float, x4 : float, y4 : float) =
            let g = makeBezier (x1, y1, x2, y2, x3, y3, x4, y4)
            ctx.DrawGeometry (null, backend.GetPen xpen, g)

        member __.DrawBeziers (xpen : XPen, points : XPoint[]) =
            ctx.DrawGeometry (null, backend.GetPen xpen, makeBeziers points)

        member __.DrawCurve (xpen : XPen, points : XPoint[], tension : float) =
            let g = makeCurve (points, tension / 3.0, false, ValueNone)
            ctx.DrawGeometry (null, backend.GetPen xpen, g)

        member __.DrawArc (xpen : XPen, x : float, y : float, width : float, height : float, startAngle : float, sweepAngle : float) =
            let g = wrapArc (x, y, width, height, startAngle, sweepAngle)
            ctx.DrawGeometry (null, backend.GetPen xpen, g)


        member __.DrawRectangle (xpen : XPen, xbrush : XBrush, x : float, y : float, width : float, height : float) =
            let rect = Rect (x, y, width, height)
            ctx.DrawRectangle (backend.GetBrush xbrush, backend.GetPen xpen, rect)

        member __.DrawRectangles (xpen : XPen, xbrush : XBrush, rects : XRect[]) =
            let pen = backend.GetPen xpen
            let brush = backend.GetBrush xbrush
            for rect in rects do
                ctx.DrawRectangle (brush, pen, wrapRect rect)

        member __.DrawRoundedRectangle (xpen : XPen, xbrush : XBrush, x : float, y : float, width : float, height : float, ellipseWidth : float, ellipseHeight : float) =
            let rect = Rect (x, y, width, height)
            ctx.DrawRectangle (backend.GetBrush xbrush, backend.GetPen xpen, rect, 0.5 * ellipseWidth, 0.5 * ellipseHeight)


        member __.DrawEllipse (xpen : XPen, xbrush : XBrush, x : float, y : float, width : float, height : float) =
            let g = Rect (x, y, width, height) |> EllipseGeometry
            ctx.DrawGeometry (backend.GetBrush xbrush, backend.GetPen xpen, g)

        member __.DrawPolygon (xpen : XPen, xbrush : XBrush, points : XPoint[], fillmode : XFillMode) =
            let g = makePolyLine (points, true, ValueSome fillmode)
            ctx.DrawGeometry (backend.GetBrush xbrush, backend.GetPen xpen, g)

        member __.DrawPie (xpen : XPen, xbrush : XBrush, x : float, y : float, width : float, height : float, startAngle : float, sweepAngle : float) =
            logger.Debug "DrawPie"

        member __.DrawClosedCurve (xpen : XPen, xbrush : XBrush, points : XPoint[], tension : float, fillmode : XFillMode) =
            let g = makeCurve (points, tension / 3.0, true, ValueSome fillmode)
            ctx.DrawGeometry (backend.GetBrush xbrush, backend.GetPen xpen, g)

        member __.DrawPath (xpen : XPen, xbrush : XBrush, xpath : XGraphicsPath) =
            logger.Debug "DrawPath"


        member __.DrawString (text : string, xfont : XFont, xbrush : XBrush, layoutRectangle : XRect, format : XStringFormat) =
            let font = backend.GetFont xfont

            let (textAlignment, offsetX) =
                match format.Alignment with
                    | XStringAlignment.Near -> (TextAlignment.Left, 0.0)
                    | XStringAlignment.Center -> (TextAlignment.Center, 0.5 * layoutRectangle.Width)
                    | XStringAlignment.Far -> (TextAlignment.Right, layoutRectangle.Width)
                    | _ -> (TextAlignment.Left, 0.0)

            let offsetY =
                match format.LineAlignment with
                    | XLineAlignment.Near -> 0.0
                    | XLineAlignment.Center -> (2.0 / 3.0) * font.Metrics.Ascent + 0.5 * layoutRectangle.Height;
                    | XLineAlignment.Far -> font.Metrics.Ascent - font.Metrics.Descent + layoutRectangle.Height
                    | XLineAlignment.BaseLine -> font.Metrics.Ascent
                    | _ -> 0.0

            let ft =
                FormattedText (
                    Text = text,
                    Typeface = font.Typeface,
                    FontSize = font.Size,
                    TextAlignment = textAlignment
                )

            let brush = backend.GetBrush xbrush
            let origin = Point (layoutRectangle.X + offsetX, layoutRectangle.Y + offsetY)
            ctx.DrawText (brush, origin, ft)

            if xfont.Strikeout then
                let l = Point (ft.Bounds.Left, -font.Metrics.Ascent + font.Metrics.StrikethroughPosition)
                let r = Point (ft.Bounds.Right, -font.Metrics.Ascent + font.Metrics.StrikethroughPosition)
                ctx.DrawLine (Pen (brush, font.Metrics.StrikethroughThickness), origin + l, origin + r)

            if xfont.Underline then
                let l = Point (ft.Bounds.Left, -font.Metrics.Ascent + font.Metrics.UnderlinePosition)
                let r = Point (ft.Bounds.Right, -font.Metrics.Ascent + font.Metrics.UnderlinePosition)
                ctx.DrawLine (Pen (brush, font.Metrics.UnderlineThickness), origin + l, origin + r)


        member __.DrawImage (ximage : XImage, x : float, y : float, width : float, height : float) =
            let image = backend.GetImage ximage
            ctx.DrawImage (image, Rect (x, y, width, height))

        member __.DrawImage (ximage : XImage, destRect : XRect, srcRect : XRect, srcUnit : XGraphicsUnit) =
            let image = backend.GetImage ximage
            ctx.DrawImage (image, wrapRect srcRect, wrapRect destRect, Visuals.Media.Imaging.BitmapInterpolationMode.HighQuality)


        member __.Save (xstate : XGraphicsState) =
            let state = backend.GetGraphicsState xstate
            state.Depth <- stack.Count

        member __.Restore (xstate : XGraphicsState) =
            let state = backend.GetGraphicsState xstate
            if state.Depth < 0 then
                logger.Error ("Bad Restore")
            else
                while stack.Count > state.Depth do
                    stack.Pop().Dispose()
                state.Depth <- -1


        member __.BeginContainer (container : XGraphicsContainer, dstrect : XRect, srcrect : XRect, unit : XGraphicsUnit) =
            logger.Debug "BeginContainer"

        member __.EndContainer (container : XGraphicsContainer) =
            logger.Debug "EndContainer"


        member __.AddTransform (transform : XMatrix, matrixOrder : XMatrixOrder) =
            match matrixOrder with
                | XMatrixOrder.Prepend ->
                    ctx.PushPreTransform (wrapMatrix transform) |> stack.Push
                | XMatrixOrder.Append ->
                    ctx.PushPostTransform (wrapMatrix transform) |> stack.Push
                | _ -> ()


        member __.SetClip (path : XGraphicsPath, combineMode : XCombineMode) =
            logger.Debug "SetClip"

        member __.ResetClip () =
            logger.Debug "ResetClip"


        member __.WriteComment (comment : string) =
            ()
