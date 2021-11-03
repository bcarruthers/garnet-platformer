namespace Platformer

open Garnet.Graphics

module ParticleGroup =
    let explosion = {
        GroupId = 4
        Animations = [|
            let scale = 5.0f
            let speed = 1.0f
            let duration = 1.0f
            // sparks
            { ParticleAnimation.Defaults with
                Layer = SpriteLayers.particles
                TintWeight = 1.0f
                Duration = duration
                Width = 1.0f
                Height = 1.0f
                MinCount = 10
                MaxCount = 10
                MinSaturation = 0.85f
                MaxSaturation = 1.0f
                Opacity = 2.0f
                OpacityByEnergy = -2.0f / duration
                MinSpeed = 6.0f * scale * speed
                MaxSpeed = 10.0f * scale * speed
                InitialSize = 0.2f * scale
                SizeByEnergy = 0.0f * scale
                RotationAngleRange = 0.0f
                Textures = [| Textures.solid |]
                }
            // fiery center
            { ParticleAnimation.Defaults with
                Layer = SpriteLayers.particles
                TintWeight = 0.0f
                Duration = duration
                Width = 1.0f
                Height = 1.0f
                MinCount = 10
                MaxCount = 10
                MinSaturation = 0.85f
                MaxSaturation = 1.0f
                Opacity = 2.0f
                OpacityByEnergy = -2.0f / duration
                MinSpeed = 3.0f * scale * speed
                MaxSpeed = 6.0f * scale * speed
                InitialSize = 1.0f * scale
                SizeByEnergy = 2.0f * scale
                Textures = [| Textures.solid |]
                }
            |]
        }

    let all = [
        explosion
        ]



