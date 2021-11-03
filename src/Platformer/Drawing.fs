namespace Platformer

open System.Numerics
open Garnet.Numerics
open Veldrid
open Garnet.Graphics
open Garnet.Composition

module DrawingSystem =
    type Range2 with
        static member Lerp(a : Range2, b : Range2) =
            let p0 = Range2.Lerp(a, b.Min)
            let p1 = Range2.Lerp(a, b.Max)
            Range2(p0, p1)
    
    type Container with                                      
        member c.AddScreenShake() =
            let rate = 1.0f
            let add delta =
                let state = c.Get<ScreenShakeState>()
                let newState = ScreenShakeState.add delta state
                c.Set<ScreenShakeState>(newState)
            Disposable.Create [
                c.On<Update> <| fun e ->
                    add -(float32 e.DeltaTime * rate / 1000.0f)
                c.On<Shake> <| fun e ->
                    add e.trauma
                ]

        member c.AddMainTransforms() =
            let zMax = 100.0f
            let tileSize = Vector2i(8, 8)
            let offset = tileSize.ToVector2() * Vector2(0.5f, 0.25f)
            Disposable.Create [
                c.On<Draw> <| fun e ->
                    let shakeXf =
                        let timeScale = 15.0f
                        let amplitude = 8.0f
                        let maxDegrees = 1.0f
                        let minMagnitude = 0.08f
                        let shake = c.Get<ScreenShakeState>()
                        let shakeMagnitude =
                            let m = ScreenShakeState.getMagnitude shake
                            if m < minMagnitude then 0.0f else m
                        let translation = shakeMagnitude * amplitude
                        let degrees = shakeMagnitude * maxDegrees
                        let time = (float e.Update.Time / 1000.0 |> float32) * timeScale
                        ScreenShakeState.getShakeTransform translation degrees 1 time
                    let cameras = c.Get<CameraSet>()
                    let projection =
                        Matrix4x4.CreateOrthographicOffCenter(
                            0.0f, float32 Resolution.width, float32 Resolution.height, 0.0f, -zMax, zMax)
                    let main = cameras.[Cameras.main]
                    main.ProjectionTransform <- projection
                    main.ViewTransform <- shakeXf
                    let level = cameras.[Cameras.level]
                    level.ProjectionTransform <- projection
                    level.ViewTransform <- Matrix4x4.CreateTranslation(offset.X, offset.Y, 0.0f) * shakeXf
                ]
                
        member c.AddHudTransforms() =
            let hudScale = 4
            c.On<Draw> <| fun e ->
                let cameras = c.Get<CameraSet>()
                let hud = cameras.[Cameras.hud] 
                let size = (e.ViewSize / hudScale).ToVector2()
                hud.ProjectionTransform <- Matrix4x4.CreateOrthographicOffCenter(0.0f, size.X, size.Y, 0.0f, -100.0f, 100.0f)
            
        member c.AddPlayerDrawing() =
            let color = HsvaFloat(0.16f, 0.5f, 1.0f, 1.0f).ToRgbaFloat()
            c.On<Draw> <| fun e ->
                let atlas = c.Get<TextureAtlas>()
                let rogue = atlas.[Textures.rogue]
                let layers = c.Get<SpriteRenderer>()
                let playerMesh = layers.GetVertices<PositionTextureColorVertex>(SpriteLayers.player)
                for r in c.Query<Position, Facing, AnimationType, HitState, Controlled>() do
                    let hit = r.Value4
                    let isVisible =
                        e.Update.Time >= hit.nextHitTime ||
                        (e.Update.Time / 50L) % 2L = 0L
                    if isVisible then
                        let p = r.Value1
                        let anim = r.Value3
                        let frame =
                            match anim with
                            | Idle -> 0
                            | Walking -> if (e.Update.Time / 500L) % 2L = 0L then 2 else 3
                            | _ -> 0
                        let frameCount = 8
                        let frameRect =
                            let rect =
                                Range2i.Sized(Vector2i(frame, 0), Vector2i.One).ToRange2() /
                                Vector2i(frameCount, 1).ToVector2()
                            let xRange =
                                match r.Value2 with
                                | Left -> Range(rect.X.Max, rect.X.Min)
                                | Right -> rect.X
                            Range2(xRange, rect.Y)
                        let p = p.pos.ToVector2() * 8.0f / float32 Block.size
                        let size = Vector2.One * 8.0f
                        playerMesh.DrawQuad {
                            Center = p 
                            Size = size
                            Rotation = Vector2.UnitX
                            TexBounds = Range2.Lerp(rogue.NormalizedBounds, frameRect)
                            Color = color
                            }
            
        member c.AddEnemyDrawing() =
            c.On<Draw> <| fun _ ->
                let atlas = c.Get<TextureAtlas>()
                let spike = atlas.[Textures.spike]
                let layers = c.Get<SpriteRenderer>()
                let mesh = layers.GetVertices<PositionTextureColorVertex>(SpriteLayers.player)
                for r in c.Query<Eid, Position, EnemyType>() do
                    let p = r.Value2
                    let enemyType = r.Value3
                    let tex =
                        match enemyType with
                        | Spike -> spike
                    let p = p.pos.ToVector2() * 8.0f / float32 Block.size
                    let size = Vector2.One * 8.0f
                    mesh.DrawQuad {
                        Center = p 
                        Size = size
                        Rotation = Vector2.UnitX
                        TexBounds = tex.NormalizedBounds
                        Color = RgbaFloat.White
                        }
        
        member c.AddPowerupDrawing() =
            c.On<Draw> <| fun _ ->
                let color = HsvaFloat(0.0f, 0.5f, 1.0f, 1.0f).ToRgbaFloat()
                let atlas = c.Get<TextureAtlas>()
                let heart = atlas.[Textures.heart]
                let layers = c.Get<SpriteRenderer>()
                let powerupMesh = layers.GetVertices<PositionTextureColorVertex>(SpriteLayers.neutrals)
                for r in c.Query<Position, HealthPowerup>() do
                    let p = r.Value1.pos.ToVector2() * 8.0f / float32 Block.size
                    let size = Vector2.One * 8.0f
                    powerupMesh.DrawQuad {
                        Center = p
                        Size = size
                        Rotation = Vector2.UnitX
                        TexBounds = heart.NormalizedBounds
                        Color = color
                        }

        member c.AddTileDrawing() =
            let color = HsvaFloat(0.9f, 0.3f, 0.9f, 1.0f).ToRgbaFloat()
            let tileSize = Vector2i(8, 8)
            c.On<Draw> <| fun _ ->
                let grid = c.Get<Grid<BlockType>>()
                let atlas = c.Get<TextureAtlas>()
                let layers = c.Get<SpriteRenderer>()
                let stoneWall = atlas.[Textures.stoneWall]
                let spike = atlas.[Textures.spike]
                let mesh = layers.GetVertices<PositionTextureColorVertex>(SpriteLayers.tiles)
                let b = grid.Bounds.Expand(Vector2i.One)
                for y = b.Min.Y to b.Max.Y - 1 do
                    for x = b.Min.X to b.Max.X - 1 do
                        let p = Vector2i(x, y)
                        let cell = grid.GetOrDefault(p, BlockType.Rock)
                        let src =
                            match cell with
                            | BlockType.Rock -> 
                                let cx0 = grid.GetOrDefault(p - Vector2i.UnitX, BlockType.Rock)
                                let cx1 = grid.GetOrDefault(p + Vector2i.UnitX, BlockType.Rock)
                                let cy0 = grid.GetOrDefault(p - Vector2i.UnitY, BlockType.Rock)
                                let cy1 = grid.GetOrDefault(p + Vector2i.UnitY, BlockType.Rock)
                                let adj =
                                    (if cell = cx0 then TileAdjacency.Left else TileAdjacency.None) |||
                                    (if cell = cx1 then TileAdjacency.Right else TileAdjacency.None) |||
                                    (if cell = cy0 then TileAdjacency.Top else TileAdjacency.None) |||
                                    (if cell = cy1 then TileAdjacency.Bottom else TileAdjacency.None)
                                let src = Range2.Lerp(stoneWall.NormalizedBounds,  TileAdjacency.getSourceRect adj)
                                ValueSome src
                            | BlockType.Spike ->
                                ValueSome spike.NormalizedBounds
                            | _ -> ValueNone
                        match src with
                        | ValueNone -> ()
                        | ValueSome src ->
                            let dest = Range2i.Sized(p * tileSize, tileSize).ToRange2()
                            mesh.DrawQuad(dest, src, color)

        member c.AddVitalsHud() =
            let color = HsvaFloat(0.0f, 0.5f, 1.0f, 1.0f).ToRgbaFloat()
            let margin = 1
            c.On<Draw> <| fun _ ->
                let atlas = c.Get<TextureAtlas>()
                let layers = c.Get<SpriteRenderer>()
                let mesh = layers.GetVertices<PositionTextureColorVertex>(SpriteLayers.text)
                for r in c.Query<HitPoints, Controlled>() do
                    let hp = r.Value1
                    // Heart icon
                    for i = 0 to hp.maxHits - 1 do
                        let x = float32 margin + float32 i * 9.0f
                        let rect = Range2.Sized(Vector2(x, 1.0f), Vector2(9.0f, 8.0f))
                        let texName = if i < hp.hits then Textures.heart else Textures.heartOutline 
                        let texBounds = atlas.[texName].NormalizedBounds
                        mesh.DrawQuad(rect, texBounds, color)

        member c.AddStatusHud() =
            let margin = 1
            c.On<Draw> <| fun _ ->
                let atlas = c.Get<TextureAtlas>()
                let layers = c.Get<SpriteRenderer>()
                let mesh = layers.GetVertices<PositionTextureColorVertex>(SpriteLayers.text)
                let font = c.LoadResource<Font>(Fonts.mono)
                let b = Range2i.Sized(Vector2i.Zero, Resolution.viewSize)
                // Menu or destroyed message
                let menu = c.Get<MenuState>()
                if menu.isDisplayed then
                    // Title
                    let text = "Platformer"
                    let size = font.Measure(text)
                    let menuHeight = size.Y * (menu.menuItems.Length + 2)
                    let textSize = Vector2i(size.X, menuHeight)
                    let p = (b.Size - textSize) / 2
                    mesh.DrawText(font, text, p, RgbaFloat.White)
                    // Menu items
                    let mutable y = p.Y + size.Y
                    for item in menu.menuItems do
                        y <- y + size.Y
                        let name = MenuItem.format item
                        let marker = if item = menu.selectedMenuItem then ">" else " "
                        mesh.DrawText(font, $"{marker}{name}", Vector2i(p.X, y), RgbaFloat.White)
                    // Menu background
                    let rect = Range2i.Sized(p, Vector2i(size.X, menuHeight)).Expand(Vector2i.One * 4)
                    let bgMesh = layers.GetVertices<PositionTextureColorVertex>(SpriteLayers.border)
                    let solidBounds = atlas.[Textures.solid].NormalizedBounds
                    bgMesh.DrawQuad(rect.ToRange2(), solidBounds, RgbaFloat.Black)
                else
                    if c.TryGetPlayerEid().IsNone then
                        // Destroyed message
                        let text = "Game Over"
                        let textSize = font.Measure(text)
                        mesh.DrawText(font, text, (b.Size - textSize) / 2, RgbaFloat.White)
                        let text = "Press R to restart"
                        let p = Vector2i(margin, b.Max.Y - margin - textSize.Y)
                        mesh.DrawText(font, text, p, RgbaFloat.White)
            
        member c.AddParticles() =
            let particles = c.Get<ParticleSystem>()
            particles.AddGroups(ParticleGroup.all)
            Disposable.Create [
                c.On<FixedUpdate> <| fun e ->
                    particles.Update(e.FixedDeltaTime)                
                c.On<Draw> <| fun _ ->
                    let atlas = c.Get<TextureAtlas>()
                    let layers = c.Get<SpriteRenderer>()
                    particles.Draw(layers, atlas)
                ]
            
    let add (c : Container) =
        Disposable.Create [
            c.AddScreenShake()
            c.AddMainTransforms()
            c.AddVitalsHud()
            c.AddHudTransforms()
            c.AddTileDrawing()
            c.AddPlayerDrawing()
            c.AddEnemyDrawing()
            c.AddPowerupDrawing()
            c.AddStatusHud()
            c.AddParticles()
            ]
