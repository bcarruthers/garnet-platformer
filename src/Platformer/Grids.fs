namespace Platformer

open System
open Garnet.Numerics

type ICellAccessor<'a> =
    abstract TryGet : Vector2i -> 'a voption

type Grid<'a>(size : Vector2i, cells : 'a array) =
    member c.Size = size
    member c.Bounds = Range2i.Sized(Vector2i.Zero, size)
    member c.TryGet(p : Vector2i) =
        if size.X = 0 || size.Y = 0 then ValueNone
        else
            let b = Range2i.Sized(Vector2i.Zero, size - 1)
            let p = b.Clamp(p)
            ValueSome cells.[p.Y * size.X + p.X]
    member c.Set(p : Vector2i, cell) =
        if c.Bounds.Contains(p) then
            cells.[p.Y * size.X + p.X] <- cell
    member c.Map(f) =
        Grid<_>(size, cells |> Array.map f)
    interface ICellAccessor<'a> with
        member c.TryGet(p) = c.TryGet(p)
    static member Empty = Grid<'a>(Vector2i.Zero, Array.empty)
    
type Grid() =
    static member Parse(str : string) =
        let rows =
            str.Split([| '\n'; '\r' |], StringSplitOptions.RemoveEmptyEntries)
            |> Array.filter (fun row -> row.Length > 0)
        if rows.Length = 0 then Grid<char>.Empty
        else
            let width = rows |> Seq.map (fun row -> row.Length) |> Seq.min
            let height = rows.Length
            let cells = Array.zeroCreate (width * height)
            for y = 0 to rows.Length - 1 do
                for x = 0 to width - 1 do
                    cells.[y * width + x] <- rows.[y].[x]
            Grid<char>(Vector2i(width, height), cells)

[<AutoOpen>]        
module GridExtensions =
    type ICellAccessor<'a> with
        member c.GetOrDefault(p : Vector2i, fallback) =
            match c.TryGet(p) with
            | ValueSome cell -> cell
            | ValueNone -> fallback

module ChunkCoords =
    let getSize sizePow =
        1 <<< sizePow

    let toCellInChunk sizePow x =
        x &&& ((1 <<< sizePow) - 1)

    let toFractionalChunk sizePow x =
        let size = 1 <<< sizePow
        let rem = x &&& (size - 1)
        let chp = x >>> sizePow
        float32 chp + float32 rem / float32 size

    let toChunkVertex sizePow x =
        (x + (1 <<< (sizePow - 1))) >>> sizePow

    let toChunk sizePow x =
        x >>> sizePow

    let toCell sizePow x =
        x <<< sizePow

[<AutoOpen>]
module ChunkExtensions =
    type Rangei with
        member c.ToCell(sizePow) =
            Rangei(
                ChunkCoords.toCell sizePow c.Min,
                ChunkCoords.toCell sizePow c.Max)

        member c.ToChunk(sizePow) =
            Rangei(
                ChunkCoords.toChunk sizePow c.Min,
                ChunkCoords.toChunk sizePow (c.Max - 1) + 1)

    type Range2i with
        member c.ToCell(sizePow) =
            Range2i(
                c.X.ToCell(sizePow),
                c.Y.ToCell(sizePow))

        member c.ToChunk(sizePow) =
            Range2i(
                c.X.ToChunk(sizePow),
                c.Y.ToChunk(sizePow))
