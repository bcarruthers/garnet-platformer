namespace Platformer

open System
open Garnet.Numerics
open Veldrid
open Garnet.Composition
open Garnet.Audio
open Garnet.Graphics

module Level =
    let width = 39
    let height = 22
    
module StartupSystem =        
    type Container with
        member c.SetSystemSettings() =
            let scale = 4
            c.Set {
                WindowSettings.Default with
                    Title = "Platformer"
                    Width = 320 * scale
                    Height = 180 * scale
                    Background = HsvaFloat(0.9f, 0.5f, 0.4f, 1.0f).ToRgbaFloat()
                }
            c.Set {
                TimingSettings.Default with
                    IsRunning = false
                }
            Disposable.Null
            
        member c.LoadSettings() =
            use folder = new FileFolder(".")
            let settings = folder.LoadJson<Settings>(Resources.settings)
            let lookup = CommandKeyLookup(settings.commands)
            c.Set<CommandKeyLookup>(lookup)
            Disposable.Null
            
        member c.LoadMapPrefabs() =
            let folder = c.Get<IReadOnlyFolder>()
            let image = folder.LoadImage("levels/map-prefabs.png")
            let cells = Array.zeroCreate (image.Width * image.Height)
            for y = 0 to image.Height - 1 do
                let srcSpan = image.GetPixelRowSpan(y)
                let destSpan = cells.AsSpan(y * image.Width)
                for x = 0 to srcSpan.Length - 1 do
                    let color = srcSpan.[x]
                    let value =
                        (uint32 color.R <<< 24) |||
                        (uint32 color.G <<< 16) |||
                        (uint32 color.B <<< 8) |||
                        (uint32 color.A <<< 0)
                    let cellType = CellType.fromColor value
                    destSpan.[x] <- cellType
            let variantCount = image.Height / Level.height
            c.Set(LevelPrefabData(image.Width, Level.height, variantCount, cells))
            Disposable.Null                

        member c.LoadAssets() =
            let device = c.Get<GraphicsDevice>()
            let audioDevice = c.Get<AudioDevice>()
            let cache = c.Get<ResourceCache>()
            let folder = c.Get<IReadOnlyFolder>()
            // Need to load audio in advance, otherwise get conflict
            folder.LoadAudioFromFolder("sounds", audioDevice, cache)
            folder.LoadTextureAtlasFromFolder(device, Textures.atlas, 512, 512, cache)
            cache.LoadMonospacedFont(Textures.atlas, Fonts.mono, -1) |> ignore
            c.Set<TextureAtlas>(cache.LoadResource<TextureAtlas>(Textures.atlas))
            Disposable.Null

    let add (c : Container) =
        Disposable.Create [
            c.SetSystemSettings()
            c.AddDefaultSystems()
            c.LoadSettings()
            c.LoadAssets()
            c.LoadMapPrefabs()
            ]
        

