namespace Platformer

open System.Collections.Generic
open Garnet.Audio
open Garnet.Composition

module AudioSystem =
    type Container with
        member c.AddSoundPlayback() =
            let pending = List<PlaySound>()
            Disposable.Create [     
                c.On<PlaySound> <| fun e ->
                    pending.Add(e)
                c.On<Update> <| fun e ->
                    let device = c.Get<AudioDevice>()
                    device.Update(e.Time)
                    for sound in pending do
                        let soundId = c.LoadResource<SoundId>(sound.soundKey)
                        device.PlaySound(soundId, sound.playback)
                    pending.Clear()                
                ]

    let add (c : Container) =
        Disposable.Create [
            c.AddSoundPlayback()
            ]
