namespace Platformer

open Garnet.Audio
open Garnet.Composition

module EffectSystem =
    type Container with
        member c.AddSounds() =
            Disposable.Create [
                c.On<Hit> <| fun e ->
                    c.Send<PlaySound> {
                        soundKey = "sounds/hit3.wav"
                        playback = SoundPlayback.Default
                    }
                c.On<Jumped> <| fun e ->            
                    c.Send<PlaySound> {
                        soundKey = "sounds/jump.wav"
                        playback = SoundPlayback.Default
                    }
                ]
                        
    let add (c : Container) =
        Disposable.Create [
            c.AddSounds()
            ]
