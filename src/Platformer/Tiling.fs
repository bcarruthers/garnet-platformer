namespace Platformer

open Garnet.Numerics

type TileAdjacency =
    | None = 0
    | Left = 1
    | Right = 2
    | Top = 4
    | Bottom = 8

module TileAdjacency =            
    let tileRectsByAdjacency =
        [|
            3, 3 // ....
            2, 3 // ...L
            0, 3 // ..R.
            1, 3 // ..RL
            3, 2 // .T..
            2, 2 // .T.L
            0, 2 // .TR.
            1, 2 // .TRL
            3, 0 // B...
            2, 0 // B..L
            0, 0 // B.R.
            1, 0 // B.RL
            3, 1 // BT..
            2, 1 // BT.L
            0, 1 // BTR.
            1, 1 // BTRL
        |]
        |> Array.map (fun (x, y) ->
            Range2i.Sized(Vector2i(x, y), Vector2i.One).ToRange2() / 4.0f)
    
    let getSourceRect (adj : TileAdjacency) =
        tileRectsByAdjacency.[int adj]
