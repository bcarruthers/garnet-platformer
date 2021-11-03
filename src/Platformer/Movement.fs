namespace Platformer

open Garnet.Numerics

[<Struct>]
type Movement = {
    displacement : Vector2i
    acceleration : Vector2i
    }

module Movement =
    let zero = {
        displacement = Vector2i.Zero
        acceleration = Vector2i.Zero
        }

    let inline getMinBlock blockSize x = 
        if x < 0 then (x - blockSize + 1) / blockSize
        else x / blockSize

    let inline getMaxBlock blockSize x = 
        getMinBlock blockSize (x + blockSize - 1)

    let inline getAdvanceDistanceInBlock blockSize block bx x dir =
        match block with
        | BlockType.Spike
        | BlockType.Air ->
            if dir < 0 then x - bx * blockSize else (bx + 1) * blockSize - x
        | BlockType.Rock -> 0
        | _ -> 0

    [<Struct>]
    type AdvancingAxes =
        | XY
        | YX

    let getAdvanceDistanceInGrid blockSize (chunks : ICellAccessor<BlockType>) x y0 y1 axes delta =
        let dir = sign delta
        let distance = abs delta
        let by0 = getMinBlock blockSize y0
        let by1 = getMaxBlock blockSize y1
        let mutable remaining = distance
        let mutable xa = x
        while remaining > 0 do
            let bx = 
                if dir < 0 then getMinBlock blockSize (xa - 1)
                else getMinBlock blockSize xa
            let mutable advance = remaining
            let mutable by = by0
            while by < by1 && advance > 0 do
                let p = 
                    match axes with
                    | XY -> Vector2i(bx, by)
                    | YX -> Vector2i(by, bx)
                let block = chunks.GetOrDefault(p, BlockType.Rock)
                let dist = getAdvanceDistanceInBlock blockSize block bx xa dir
                advance <- min advance dist
                by <- by + 1
            if advance = 0 then remaining <- 0
            else
                xa <- xa + advance * dir
                remaining <- remaining - advance
        xa - x

    let getAdvanceDistance blockSize grid (box : Range2i) axes delta =
        match axes with
        | XY -> 
            let x = if delta > 0 then box.Max.X else box.Min.X
            let y0 = box.Min.Y
            let y1 = box.Max.Y
            getAdvanceDistanceInGrid blockSize grid x y0 y1 axes delta
        | YX -> 
            let x = if delta > 0 then box.Max.Y else box.Min.Y
            let y0 = box.Min.X
            let y1 = box.Max.X
            getAdvanceDistanceInGrid blockSize grid x y0 y1 axes delta

    let getMoveDelta blockSizePow grid p (size : Vector2i) (delta : Vector2i) =
        let start = p
        let blockSize = 1 <<< blockSizePow
        let p = 
            if delta.X = 0 then p
            else
                let bounds = Range2i.Sized(p, size)
                let dx = getAdvanceDistance blockSize grid bounds XY delta.X
                p + Vector2i(dx, 0)
        let p = 
            if delta.Y = 0 then p
            else
                let bounds = Range2i.Sized(p, size)
                let dy = getAdvanceDistance blockSize grid bounds YX delta.Y
                p + Vector2i(0, dy)
        p - start

    let move blockSizePow grid p (size : Vector2i) (velocity : Vector2i) =
        let delta = getMoveDelta blockSizePow grid p size velocity
        { 
            displacement = delta
            acceleration = delta - velocity
        }
