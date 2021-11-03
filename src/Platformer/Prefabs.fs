namespace Platformer

open Garnet.Numerics
open Garnet.Graphics
open Garnet.Composition

[<AutoOpen>]
module SpawningExtensions =
    type Container with
        member c.TryGetPlayerEid() =
            let mutable eid = ValueNone 
            for r in c.Query<Eid, Controlled>() do
                eid <- ValueSome r.Value1
            eid
                
        member c.SetDefaults() =
            c.Set(MenuState.initial)
            
        member c.SpawnSpike(pos) =
            c.Create()
                .With<Position>({ pos = pos })
                .With<BoundingBox>({ bounds = Range2i(Vector2i(-64, -64), Vector2i(64, 128)) })
                .With<Damaging>({ damage = 1 })
                .Add<EnemyType>(Spike)
            
        member c.SpawnHealth(pos) =
            c.Create()
                .With<Position>({ pos = pos })
                .With<BoundingBox>({ bounds = Range2i(Vector2i(-128, -128), Vector2i(128, 128)) })
                .Add(HealthPowerup())
            
        member c.SpawnRogue(pos, state) =
            c.Create()
                .With<Position>({ pos = pos })
                .With<Velocity>({ velocity = Vector2i.Zero })
                .With<Force>({ force = Vector2i.Zero })
                // Make sure to leave room at the top of bounds to allow for
                // jumping within the same block row
                .With<BoundingBox>({ bounds = Range2i(Vector2i(-64, -64), Vector2i(64, 128)) })
                .With<HitPoints>(state.hp)
                .With<MoveInput>({ xDir = 0; isJumping = false })
                .With<Damping>({ maxVelocity = Vector2i(32, 128); friction = Vector2i(1, 1) })
                .With<Resting>({ restTime = 0L })
                .With<HitState>({ nextHitTime = 0L })
                .With<Facing>(Right)
                .With<AnimationType>(Idle)
                .Add(Controlled())
            
        member c.SpawnLevel(state, run) =
            let prefabs = c.Get<LevelPrefabData>()
            let baseGrid = prefabs.Generate(run.seed, run.level)
            // Generate terrain
            let grid = baseGrid.Map(CellType.toBlock)
            c.Set<Grid<BlockType>>(grid)
            // Populate with entities
            for y = 0 to baseGrid.Size.Y - 1 do
                for x = 0 to baseGrid.Size.X - 1 do
                    let p = Vector2i(x, y)
                    let blockPos = p * Block.size + Block.size / 2
                    match baseGrid.GetOrDefault(p, CellType.Solid) with
                    | CellType.Entrance -> c.SpawnRogue(blockPos, state)
                    | CellType.Hazard -> c.SpawnSpike(blockPos)
                    | CellType.Powerup -> c.SpawnHealth(blockPos)
                    | _ -> ()

        member c.AdvanceLevel() =
            match c.TryGetPlayerEid() with
            | ValueNone -> ()
            | ValueSome eid ->
                let entity = c.Get(eid)
                let hp = entity.Get<HitPoints>()
                let player = {
                    hp = { hp with hits = min (hp.hits + 1) hp.maxHits }
                    }
                let run = &c.Get<RunState>()
                run <- { run with level = run.level + 1 }
                c.ResetWorld(player, run)
            
        member c.ResetWorld(seed) =
            let run = {
                seed = seed
                level = 1
                duration = 0L
                }
            let player = {
                hp = { hits = 3; maxHits = 3 }
                }
            c.ResetWorld(player, run)

        member c.ResetWorld(player, run) =
            c.Set<RunState>(run) 
            c.Get<ParticleSystem>().Clear()
            c.DestroyAll()
            c.Commit()
            c.SpawnLevel(player, run)


