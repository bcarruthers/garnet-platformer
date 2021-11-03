open Garnet.Composition
open Platformer
            
[<EntryPoint>]
let main _ =
    Container.RunLoop <| fun c ->
        c.AddSystems [
            StartupSystem.add
            InputSystem.add
            SpawningSystem.add
            SimulationSystem.add
            DrawingSystem.add
            EffectSystem.add
            AudioSystem.add
            DebugSystem.add
            ]
    0
