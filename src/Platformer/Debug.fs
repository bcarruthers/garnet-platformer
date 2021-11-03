namespace Platformer

open System
open System.Numerics
open System.Diagnostics
open ImGuiNET
open Veldrid
open Garnet.Numerics
open Garnet.Graphics
open Garnet.Input
open Garnet.Composition

[<Struct>]
type DebugMode = {
    IsDebugEnabled : bool
    }

type DebugHud() =
    let fps = FpsGauge(1.0f)
    let fixedFps = FpsGauge(1.0f)
    member _.FixedUpdate() =
        let timestamp = Stopwatch.GetTimestamp()
        fixedFps.Update(timestamp)
    member _.Update() =
        let timestamp = Stopwatch.GetTimestamp()
        fps.Update(timestamp)
    member _.Draw(c : Container) =
        let flags =
            ImGuiWindowFlags.NoBackground |||
            ImGuiWindowFlags.NoTitleBar |||
            ImGuiWindowFlags.NoResize |||
            ImGuiWindowFlags.NoMove |||
            ImGuiWindowFlags.NoFocusOnAppearing |||
            ImGuiWindowFlags.NoInputs |||
            ImGuiWindowFlags.NoNavFocus
        ImGui.SetNextWindowSize(Vector2(500.0f, 200.0f))
        ImGui.SetNextWindowPos(Vector2(0.0f, 100.0f))
        if ImGui.Begin("Hud", flags) then
            ImGui.SetWindowFontScale(1.0f)
            let inputs = c.Get<InputCollection>()
            let cameras = c.Get<CameraSet>()
            let run = c.Get<RunState>()
            let normPos = inputs.NormalizedMousePosition
            let worldPos = Vector2.Transform(normPos, cameras.[Cameras.main].GetNormalizedToWorld())
            let xf = cameras.[Cameras.hud].GetNormalizedToWorld()
            let hudPos = Vector2.Transform(normPos, xf)
            let info = GC.GetGCMemoryInfo()
            ImGui.Text $"FPS: %d{int fps.FramesPerSec}, mean: %d{int fps.MeanFrameMs} ms, max: %d{int fps.MaxFrameMs} ms, fixed FPS: %d{int fixedFps.FramesPerSec}"
            ImGui.Text $"GC pause: {info.PauseTimePercentage}%%%%, heap size: {info.HeapSizeBytes / 1024L} Kb"
            ImGui.Text $"Cursor: (%.2f{normPos.X}, %.2f{normPos.Y}), world: (%.1f{worldPos.X}, %.1f{worldPos.Y}), HUD: (%.1f{hudPos.X}, %.1f{hudPos.Y})"
            ImGui.Text $"Seed: {run.seed}, level: {run.level}, duration: {run.duration}"
            match c.TryGetPlayerEid() with
            | ValueNone -> ()
            | ValueSome eid ->
                let entity = c.Get(eid)
                let pos = entity.Get<Position>().pos
                let velocity = entity.Get<Velocity>().velocity
                let force = entity.Get<Force>().force
                ImGui.Text $"Player pos: ({pos.X}, {pos.Y}), vel: ({velocity.X}, {velocity.Y}), force: ({force.X}, {force.Y})"
            ImGui.End()

module DebugSystem =
    type Container with
        member c.AddToggle() =
            c.On<CommandState> <| fun e ->
                if CommandState.hasCommandPressed Command.Debug e then
                    let debug = &c.Get<DebugMode>()
                    debug <- { IsDebugEnabled = not debug.IsDebugEnabled }

        member c.AddHud() =
            let hud = c.Get<DebugHud>()
            Disposable.Create [
                c.On<FixedUpdate> <| fun _ ->
                    hud.FixedUpdate()
                c.On<Update> <| fun _ ->
                    hud.Update()
                c.On<Draw> <| fun _ ->
                    let debug = c.Get<DebugMode>()
                    if debug.IsDebugEnabled then
                        hud.Draw(c)
                ]               

        member c.AddEffects() =
            let particles = c.Get<ParticleSystem>()
            let mutable nextTime = 0L
            c.On<CommandState> <| fun e ->
                if e.time >= nextTime && CommandState.hasCommandDown Command.Fire e then
                    let debug = c.Get<DebugMode>()
                    if debug.IsDebugEnabled then
                        nextTime <- e.time + 200L
                        particles.Emit {
                            GroupId = ParticleGroup.explosion.GroupId
                            EmitDelay = 0.0f
                            EmitInterval = 0.0f
                            EmitCount = 1
                            Position = e.aimPos
                            Velocity = Vector2.Zero
                            Rotation = Vector2.UnitX
                            Color = RgbaFloat.White
                            Energy = 1.0f 
                            }
        
        member c.AddSetMaxVitals() =
            c.On<FixedUpdate> <| fun _ ->
                let debug = c.Get<DebugMode>()
                if debug.IsDebugEnabled then
                    match c.TryGetPlayerEid() with
                    | ValueNone -> ()
                    | ValueSome eid ->
                        let ship = c.Get(eid)
                        let hp = &ship.Get<HitPoints>()
                        hp <- { hp with hits = hp.maxHits }

        member c.AddTileDrawing() =
            c.On<Draw> <| fun _ ->
                let debug = c.Get<DebugMode>()
                if debug.IsDebugEnabled then                
                    let grid = c.Get<Grid<BlockType>>()
                    let atlas = c.Get<TextureAtlas>()
                    let layers = c.Get<SpriteRenderer>()
                    let solid = atlas.[Textures.solid]
                    let bgMesh = layers.GetVertices<PositionTextureColorVertex>(SpriteLayers.bgDebug)
                    bgMesh.DrawQuad(
                        Range2i.Sized(Vector2i.Zero, Resolution.viewSize).ToRange2(),
                        solid.NormalizedBounds,
                        RgbaFloat.Grey.MultiplyAlpha(0.7f)
                        )
                    let mesh = layers.GetVertices<PositionTextureColorVertex>(SpriteLayers.tileDebug)
                    let tileSize = Vector2i(8, 8)
                    let b = grid.Bounds.Expand(Vector2i.Zero)
                    for y = b.Min.Y to b.Max.Y - 1 do
                        for x = b.Min.X to b.Max.X - 1 do
                            let p = Vector2i(x, y)
                            let cell = grid.GetOrDefault(p, BlockType.Rock)
                            let color =
                                match cell with
                                | BlockType.Rock -> RgbaFloat.Red
                                | BlockType.Air -> RgbaFloat.Blue
                                | _ -> RgbaFloat.FromUInt32(0xff00ffffu)
                            let dest = Range2i.Sized(p * tileSize, tileSize).ToRange2()
                            mesh.DrawQuad(dest, solid.NormalizedBounds, color.MultiplyRgb(0.3f).MultiplyAlpha(0.7f))

        member c.AddBoundingBoxDrawing() =
            c.On<Draw> <| fun _ ->
                let debug = c.Get<DebugMode>()
                if debug.IsDebugEnabled then                
                    let atlas = c.Get<TextureAtlas>()
                    let layers = c.Get<SpriteRenderer>()
                    let solid = atlas.[Textures.solid]
                    let mesh = layers.GetVertices<PositionTextureColorVertex>(SpriteLayers.debug)
                    let color = RgbaFloat.Cyan.MultiplyAlpha(0.5f)
                    for r in c.Query<Position, BoundingBox>() do
                        let bounds = (r.Value2.bounds + r.Value1.pos).ToRange2() * 8.0f / float32 Block.size
                        mesh.DrawQuad(bounds, solid.NormalizedBounds, color)

    let add (c : Container) =
        Disposable.Create [
            c.AddToggle()
            c.AddHud()
            c.AddEffects()
            c.AddSetMaxVitals()
            c.AddTileDrawing()
            c.AddBoundingBoxDrawing()
            ]

