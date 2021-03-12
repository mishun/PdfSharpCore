module internal PdfSharpCore.Drawing.Avalonia.Wrap

open Avalonia
open Avalonia.Media
open Avalonia.Media.Imaging
open Avalonia.Media.TextFormatting
open PdfSharpCore.Drawing


let wrapPoint (p : XPoint) =
    Point (p.X, p.Y)


let wrapRect (rect : XRect) =
    Rect (wrapPoint rect.TopLeft, wrapPoint rect.BottomRight)


let wrapMatrix (m : XMatrix) =
    Matrix (m.M11, m.M12, m.M21, m.M22, m.OffsetX, m.OffsetY)


let wrapColor (c : XColor) =
    let a = (255.0 * c.A) |> round |> byte
    Color (a, c.R, c.G, c.B)


let wrapFillMode (fr : XFillMode) =
    match fr with
    | XFillMode.Alternate -> FillRule.EvenOdd
    | XFillMode.Winding -> FillRule.NonZero
    | _ -> FillRule.EvenOdd


let wrapBrush (brush : XBrush) =
    match brush with
    | :? XSolidBrush as solid ->
        wrapColor solid.Color |> SolidColorBrush :> IBrush

    | :? XLinearGradientBrush as grad ->
        let res = LinearGradientBrush ()
        res.StartPoint <- RelativePoint (Point (0.0, 0.0), RelativeUnit.Absolute) // TODO: getter for _useRect
        res.EndPoint <- RelativePoint (Point (1.0, 1.0), RelativeUnit.Absolute) // TODO: getter for _useRect
        GradientStop (Colors.Red, 0.0) |> res.GradientStops.Add // TODO: getter for _color1
        GradientStop (Colors.Blue, 1.0) |> res.GradientStops.Add // TODO: getter for _color2
        res.ToImmutable ()

    | :? XRadialGradientBrush as grad ->
        let res = RadialGradientBrush ()
        res.Center <- RelativePoint (Point (0.0, 0.0), RelativeUnit.Absolute) // TODO: getter for _center
        GradientStop (Colors.Red, 0.0) |> res.GradientStops.Add // TODO: getter for _color1
        GradientStop (Colors.Blue, 1.0) |> res.GradientStops.Add // TODO: getter for _color2
        res.ToImmutable ()

    | _ -> Brushes.Black :> IBrush


let wrapPen (pen : XPen) =
    let dashStyle =
        match pen.DashStyle with
        | XDashStyle.Solid -> null
        | XDashStyle.Dash -> DashStyle.Dash
        | XDashStyle.Dot -> DashStyle.Dot
        | XDashStyle.DashDot -> DashStyle.DashDot
        | XDashStyle.DashDotDot -> DashStyle.DashDotDot
        | XDashStyle.Custom -> DashStyle (pen.DashPattern, pen.DashOffset) :> IDashStyle
        | _ -> null

    let lineCap =
        match pen.LineCap with
        | XLineCap.Flat -> PenLineCap.Flat
        | XLineCap.Round -> PenLineCap.Round
        | XLineCap.Square -> PenLineCap.Square
        | _ -> PenLineCap.Flat

    let lineJoin =
        match pen.LineJoin with
        | XLineJoin.Miter -> PenLineJoin.Miter
        | XLineJoin.Round -> PenLineJoin.Round
        | XLineJoin.Bevel -> PenLineJoin.Bevel
        | _ -> PenLineJoin.Bevel

    let brush =
        match pen.Brush with
        | null -> wrapColor pen.Color |> SolidColorBrush :> IBrush
        | brush -> wrapBrush brush

    Pen(brush = brush,
        thickness = (96.0 / 72.0) * pen.Width,
        dashStyle = dashStyle,
        lineCap = lineCap,
        lineJoin = lineJoin,
        miterLimit = pen.MiterLimit
        ).ToImmutable() :> IPen


let wrapImage (ximage : XImage) =
    use stream = ximage.AsBitmap ()
    new Bitmap (stream)


[<AllowNullLiteral>]
type WrappedFont (size : float, typeface : Typeface, metrics : FontMetrics) =
    member __.Size = size
    member __.Typeface = typeface
    member __.Metrics = metrics


let private fontTags = [|
    struct ("Black", ValueSome FontWeight.Black, ValueNone)
    struct ("Black Italic", ValueSome FontWeight.Black, ValueSome FontStyle.Italic)
    struct ("Bold", ValueSome FontWeight.Bold, ValueNone)
    struct ("Bold Italic", ValueSome FontWeight.Bold, ValueSome FontStyle.Italic)
    struct ("Light", ValueSome FontWeight.Light, ValueNone)
    struct ("Light Italic", ValueSome FontWeight.Light, ValueSome FontStyle.Italic)
    struct ("Semibold", ValueSome FontWeight.SemiBold, ValueNone)
    struct ("Semibold Italic", ValueSome FontWeight.SemiBold, ValueSome FontStyle.Italic)
    struct ("Semilight", ValueSome FontWeight.SemiLight, ValueNone)
    struct ("Semilight Italic", ValueSome FontWeight.SemiLight, ValueSome FontStyle.Italic)
|]

let wrapFont (xfont : XFont) =
    let (familyName, weight, style) =
        let name = xfont.FontFamily.Name
        match fontTags |> Array.tryFind (fun struct (suffix, _, _) -> name.EndsWith suffix) with
        | None -> (name, ValueNone, ValueNone)
        | Some struct (suffix, style, weight) ->
            let trimmed = name.Substring(0, name.Length - suffix.Length).Trim()
            (trimmed, style, weight)

    let typeface =
        let family = FontFamily (familyName)
        Typeface (
            family,
            style |> ValueOption.defaultValue (if xfont.Italic then FontStyle.Italic else FontStyle.Normal),
            weight |> ValueOption.defaultValue (if xfont.Bold then FontWeight.Bold else FontWeight.Normal)
        )

    WrappedFont (xfont.Size, typeface, FontMetrics (typeface, xfont.Size))


let makePolyLine (points : XPoint[], isClosed, fillMode) =
    let g = StreamGeometry ()
    use ctx = g.Open ()
    match fillMode with
    | ValueSome mode ->
        mode |> wrapFillMode |> ctx.SetFillRule
        ctx.BeginFigure (wrapPoint points.[0], true)
    | ValueNone ->
        ctx.BeginFigure (wrapPoint points.[0], false)
    for i in 1 .. Array.length points - 1 do
        points.[i] |> wrapPoint |> ctx.LineTo
    ctx.EndFigure isClosed
    g :> Geometry


let makeBezier (x1, y1, x2, y2, x3, y3, x4, y4) =
    let g = StreamGeometry ()
    use ctx = g.Open ()
    ctx.BeginFigure (Point (x1, y1), false)
    ctx.CubicBezierTo (Point (x2, y2), Point (x3, y3), Point (x4, y4))
    ctx.EndFigure false
    g :> Geometry


let makeBeziers (points : XPoint[]) =
    let g = StreamGeometry ()
    use ctx = g.Open ()
    ctx.BeginFigure (wrapPoint points.[0], false)
    for i in 0 .. 3 .. Array.length points - 4 do
        ctx.CubicBezierTo (wrapPoint points.[i + 1], wrapPoint points.[i + 2], wrapPoint points.[i + 3])
    ctx.EndFigure false
    g :> Geometry


let private makeCurveSegment (ctx : StreamGeometryContext, pt0 : XPoint, pt1 : XPoint, pt2 : XPoint, pt3 : XPoint, tension3 : float) =
    ctx.CubicBezierTo (
        Point(pt1.X + tension3 * (pt2.X - pt0.X), pt1.Y + tension3 * (pt2.Y - pt0.Y)),
        Point(pt2.X - tension3 * (pt3.X - pt1.X), pt2.Y - tension3 * (pt3.Y - pt1.Y)),
        Point(pt2.X, pt2.Y)
    )

let makeCurve (points : XPoint[], tension3, isClosed, fillMode) =
    let g = StreamGeometry ()
    use ctx = g.Open ()

    match fillMode with
    | ValueSome mode ->
        mode |> wrapFillMode |> ctx.SetFillRule
        ctx.BeginFigure (wrapPoint points.[0], true)
    | ValueNone ->
        ctx.BeginFigure (wrapPoint points.[0], false)

    let count = Array.length points
    if count = 2 then
        makeCurveSegment (ctx, points.[0], points.[0], points.[1], points.[1], tension3)
    else
        makeCurveSegment (ctx, (if isClosed then points.[count - 1] else points.[0]), points.[0], points.[1], points.[2], tension3)
        for idx in 1 .. count - 3 do
            makeCurveSegment (ctx, points.[idx - 1], points.[idx], points.[idx + 1], points.[idx + 2], tension3)
        makeCurveSegment (ctx, points.[count - 3], points.[count - 2], points.[count - 1], (if isClosed then points.[0] else points.[count - 1]), tension3)
        if isClosed then
            makeCurveSegment (ctx, points.[count - 2], points.[count - 1], points.[0], points.[1], tension3)

    ctx.EndFigure isClosed
    g :> Geometry


let wrapArc (x, y, width, height, startAngle, sweepAngle) =
    let mutable α = startAngle
    if α < 0.0 then
        α <- α + (1.0 + floor ((abs(α) / 360.0))) * 360.0
    elif (α > 360.0) then
        α <- α - floor(α / 360.0) * 360.0

    //if (Math.Abs(sweepAngle) >= 360.0) then
    //    sweepAngle = Math.Sign(sweepAngle) * 360.0
    let mutable β = startAngle + sweepAngle
    if β < 0.0 then
        β <- β + (1.0 + floor((abs(β) / 360.0))) * 360.0
    elif (β > 360.0) then
        β <- β - floor(β / 360.0) * 360.0

    if (α = 0.0 && β < 0.0) then
        α <- 360.0
    elif (α = 360.0 && β > 0.0) then
        α <- 0.0

    // Scanling factor.
    let δ‎x = 0.5 * width
    let δy = 0.5 * height

    // Center of ellipse.
    let x0 = x + δ‎x
    let y0 = y + δy


(*    if (width == height)
    {
        // Circular arc needs no correction.
        ? = ? * Calc.Deg2Rad;
        ? = ? * Calc.Deg2Rad;
    }
    else
    {
        // Elliptic arc needs the angles to be adjusted such that the scaling transformation is compensated.
        ? = ? * Calc.Deg2Rad;
        sin? = Math.Sin(?);
        if (Math.Abs(sin?) > 1E-10)
        {
            if (? < Math.PI)
                ? = Math.PI / 2 - Math.Atan(?y * Math.Cos(?) / (?x * sin?));
            else
                ? = 3 * Math.PI / 2 - Math.Atan(?y * Math.Cos(?) / (?x * sin?));
        }
        //? = Calc.?Half - Math.Atan(?y * Math.Cos(?) / (?x * sin?));
        ? = ? * Calc.Deg2Rad;
        sin? = Math.Sin(?);
        if (Math.Abs(sin?) > 1E-10)
        {
            if (? < Math.PI)
                ? = Math.PI / 2 - Math.Atan(?y * Math.Cos(?) / (?x * sin?));
            else
                ? = 3 * Math.PI / 2 - Math.Atan(?y * Math.Cos(?) / (?x * sin?));
        }
        //? = Calc.?Half - Math.Atan(?y * Math.Cos(?) / (?x * sin?));
    }*)

    let sinα = sin (α)
    let cosα = cos (α)
    let sinβ = sin (β)
    let cosβ = cos (β)

    let startPoint = Point(x0 + δ‎x * cosα, y0 + δy * sinα)
    let destPoint = Point(x0 + δ‎x * cosβ, y0 + δy * sinβ)
    let size = Size(δ‎x, δy)
    let isLargeArc = abs(sweepAngle) >= 180.0
    let sweepDirection = if sweepAngle > 0.0 then SweepDirection.Clockwise else SweepDirection.CounterClockwise

    let g = StreamGeometry ()
    use ctx = g.Open ()
    ctx.BeginFigure (startPoint, false)
    ctx.ArcTo (destPoint, size, sweepAngle / 180.0 * System.Math.PI, isLargeArc, sweepDirection)
    ctx.EndFigure false
    g :> Geometry
