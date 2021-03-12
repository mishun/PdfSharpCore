namespace PdfSharpCore.Drawing.Avalonia

open System
open System.Runtime.CompilerServices
open PdfSharpCore.Drawing


[<AllowNullLiteral>]
type internal GraphicsState () =
    member val Depth : int = -1 with get, set


type internal AvaloniaBackend () =
    let cache = ConditionalWeakTable<obj, obj> ()

    static let instance = AvaloniaBackend ()

    static member Instance = instance


    member __.Lookup<'K, 'V when 'K : null and 'V : null> (key : 'K, construct : 'K -> 'V) =
        match key with
        | null -> null
        | _ ->
            match cache.TryGetValue key with
            | true, (:? 'V as value) -> value
            | _ ->
                let value = construct key
                cache.AddOrUpdate (key, value) |> ignore
                value


    member this.GetBrush (xbrush : XBrush) =
        this.Lookup (xbrush, Wrap.wrapBrush)

    member this.GetPen (xpen : XPen) =
        this.Lookup (xpen, Wrap.wrapPen)

    member this.GetImage (ximage : XImage) =
        this.Lookup (ximage, Wrap.wrapImage)

    member this.GetFont (xfont : XFont) =
        this.Lookup (xfont, Wrap.wrapFont)

    member this.GetGraphicsState (xstate : XGraphicsState) =
        this.Lookup (xstate, fun _ -> GraphicsState ())
