namespace Platformer

open System
open Garnet.Composition
    
module SpawningSystem =
    type Container with
        member c.AddReset() =
            let reset() =
                let seed = DateTime.UtcNow.Ticks &&& 0xffffffffL |> uint32
                c.ResetWorld(seed)
            Disposable.Create [
                c.On<Start> <| fun _ ->
                    reset()
                c.On<Reset> <| fun _ ->
                    reset()
                ]

    let add (c : Container) =
        c.SetDefaults()
        Disposable.Create [
            c.AddReset()
            ]
        