namespace PdfSharpCore.Drawing.Avalonia

open System
open System.Collections.Generic
open Avalonia
open Avalonia.Media
open PdfSharpCore.Drawing
open PdfSharpCore.Drawing.Avalonia.Wrap


type internal XGraphicsRendererAdapter (ctx : DrawingContext) =
    static let logger = NLog.LogManager.GetCurrentClassLogger ()

    let wrapPair (x : float, y : float) = Point (x, y)

    let stack = Stack<DrawingContext.PushedState> ()
    let gstate = Dictionary<XGraphicsState, int> ()

    interface IXGraphicsRenderer with
        member __.Close () =
            while stack.Count > 0 do
                stack.Pop().Dispose()


        member __.DrawLine(xpen : XPen, x1 : float, y1 : float, x2 : float, y2 : float) =
            ctx.DrawLine(wrapPen xpen, wrapPair (x1, y1), wrapPair (x2, y2))

        member __.DrawLines (xpen : XPen, points : XPoint[]) =
            let geom = wrapPolyLine (points, false, ValueNone)
            ctx.DrawGeometry (null, wrapPen xpen, geom)

        member __.DrawBezier (xpen : XPen, x1 : float, y1 : float, x2 : float, y2 : float, x3 : float, y3 : float, x4 : float, y4 : float) =
            let g = StreamGeometry ()
            do
                use ctx = g.Open ()
                ctx.BeginFigure (wrapPair (x1, y1), false)
                ctx.CubicBezierTo (wrapPair (x2, y2), wrapPair (x3, y3), wrapPair (x4, y4))
                ctx.EndFigure false
            ctx.DrawGeometry (null, wrapPen xpen, g)

        member __.DrawBeziers (xpen : XPen, points : XPoint[]) =
            ctx.DrawGeometry (null, wrapPen xpen, wrapBeziers points)

        member __.DrawCurve (xpen : XPen, points : XPoint[], tension : float) =
            let geom = wrapCurve (points, tension / 3.0, false, ValueNone)
            ctx.DrawGeometry (null, wrapPen xpen, geom)

        member __.DrawArc (xpen : XPen, x : float, y : float, width : float, height : float, startAngle : float, sweepAngle : float) =
            let geom = wrapArc (x, y, width, height, startAngle, sweepAngle)
            ctx.DrawGeometry (null, wrapPen xpen, geom)


        member __.DrawRectangle (xpen : XPen, xbrush : XBrush, x : float, y : float, width : float, height : float) =
            let rect = Rect (x, y, width, height)
            ctx.DrawRectangle (wrapBrush xbrush, wrapPen xpen, rect)

        member __.DrawRectangles (xpen : XPen, xbrush : XBrush, rects : XRect[]) =
            let pen = wrapPen xpen
            let brush = wrapBrush xbrush
            for rect in rects do
                ctx.DrawRectangle (brush, pen, wrapRect rect)

        member __.DrawRoundedRectangle (xpen : XPen, xbrush : XBrush, x : float, y : float, width : float, height : float, ellipseWidth : float, ellipseHeight : float) =
            let rect = Rect (x, y, width, height)
            ctx.DrawRectangle (wrapBrush xbrush, wrapPen xpen, rect, 0.5 * ellipseWidth, 0.5 * ellipseHeight)


        member __.DrawEllipse (xpen : XPen, xbrush : XBrush, x : float, y : float, width : float, height : float) =
            let geom = Rect (x, y, width, height) |> EllipseGeometry
            ctx.DrawGeometry (wrapBrush xbrush, wrapPen xpen, geom)

        member __.DrawPolygon (xpen : XPen, xbrush : XBrush, points : XPoint[], fillmode : XFillMode) =
            let geom = wrapPolyLine (points, true, ValueSome fillmode)
            ctx.DrawGeometry (wrapBrush xbrush, wrapPen xpen, geom)

        member __.DrawPie (xpen : XPen, xbrush : XBrush, x : float, y : float, width : float, height : float, startAngle : float, sweepAngle : float) =
            logger.Debug "DrawPie"

        member __.DrawClosedCurve (xpen : XPen, xbrush : XBrush, points : XPoint[], tension : float, fillmode : XFillMode) =
            let geom = wrapCurve (points, tension / 3.0, true, ValueSome fillmode)
            ctx.DrawGeometry (wrapBrush xbrush, wrapPen xpen, geom)

        member __.DrawPath (xpen : XPen, xbrush : XBrush, path : XGraphicsPath) =
            logger.Debug "DrawPath"


        member __.DrawString (text : string, xfont : XFont, xbrush : XBrush, layoutRectangle : XRect, format : XStringFormat) =
            let font = wrapFont xfont

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

            let brush = wrapBrush xbrush
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
            use bmp = wrapImage ximage
            ctx.DrawImage (bmp, Rect (x, y, width, height))

        member __.DrawImage (ximage : XImage, destRect : XRect, srcRect : XRect, srcUnit : XGraphicsUnit) =
            use bmp = wrapImage ximage
            ctx.DrawImage (bmp, wrapRect srcRect, wrapRect destRect, Visuals.Media.Imaging.BitmapInterpolationMode.HighQuality)


        member __.Save (state : XGraphicsState) =
            gstate.[state] <- stack.Count

        member __.Restore (state : XGraphicsState) =
            match gstate.TryGetValue state with
            | true, target ->
                gstate.Remove state |> ignore
                while stack.Count > target do
                    stack.Pop().Dispose()
            | _ -> logger.Error ("Bad Restore")


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
