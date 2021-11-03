namespace Platformer

open Garnet.Audio
open Garnet.Composition.Comparisons
open Garnet.Composition
open Garnet.Numerics

module SimulationSystem =
    type Container with
        member c.UpdateForcesFromInput(time) =
            let xMoveForce = 6
            let jumpForce = 80
            let brakingForce = 8
            let minJumpVelocity = 0
            let maxJumpVelocity = 16
            let maxJumpTimeSinceRest = 50L
            for r in c.Query<MoveInput, Velocity, Force, Facing, AnimationType, Resting>() do
                let moveInput = &r.Value1
                let velocity = r.Value2.velocity
                let force = &r.Value3
                let facing = &r.Value4
                let anim = &r.Value5
                let rest = r.Value6
                let forceDir = moveInput.xDir
                let xForce =
                    let dir = sign velocity.X
                    // If going in desired direction, keep adding force
                    if forceDir = dir then forceDir * xMoveForce
                    else
                        // Otherwise add braking force
                        let speed = abs velocity.X
                        let brakingForce = min speed brakingForce 
                        forceDir * xMoveForce - dir * brakingForce
                let canJump =
                    moveInput.isJumping &&
                    time - rest.restTime < maxJumpTimeSinceRest &&
                    velocity.Y >= minJumpVelocity &&
                    velocity.Y <= maxJumpVelocity
                let yForce = if canJump then -jumpForce else 0
                force <- { force = Vector2i(xForce, yForce) }
                moveInput <- { xDir = 0; isJumping = false }
                facing <-
                    if forceDir < 0 then Left
                    elif forceDir > 0 then Right
                    else facing
                anim <-
                    if forceDir <> 0 then Walking
                    else Idle
                if canJump then
                    c.Send(Jumped())
                  
        member c.TryGetNearestPowerupEid(p : Vector2i, maxDistance) =
            let maxDistSqr = maxDistance * maxDistance
            let mutable minDistSqr = maxDistSqr + 1
            let mutable minEid = Eid.Undefined
            for r in c.Query<Eid, Position, HealthPowerup>() do
                let diff = p - r.Value2.pos
                let distSqr = Vector2i.Dot(diff, diff)
                if distSqr < minDistSqr then
                    minEid <- r.Value1
                    minDistSqr <- distSqr
            if minEid.IsDefined then ValueSome minEid else ValueNone
            
        member c.UpdatePickup() =
            let maxPickupDistance = 192
            for r in c.Query<Position, HitPoints>() do
                let p = r.Value1
                let hp = &r.Value2
                match c.TryGetNearestPowerupEid(p.pos, maxPickupDistance) with
                | ValueSome powerupEid ->
                    c.Send<PlaySound> {
                        soundKey = "sounds/grab.wav"
                        playback = SoundPlayback.Default
                    }
                    c.Destroy(powerupEid)
                    hp <- { hp with hits = min hp.maxHits (hp.hits + 1) }
                | ValueNone -> ()

        member c.ApplyForces(time) =
            let gravity = Vector2i(0, 4)
            let grid = c.Get<Grid<BlockType>>()
            for r in c.Query<Position, Velocity, Force, BoundingBox, Damping, Resting>() do
                let p = &r.Value1
                let v = &r.Value2
                let f = &r.Value3
                let d = r.Value5
                let rest = &r.Value6
                let pos = p.pos
                let force = f.force + gravity
                let velocity =
                    let v = v.velocity + force
                    let friction = Vector2i.truncate d.friction v
                    let v = v - friction
                    Vector2i.truncate d.maxVelocity v
                let bounds = r.Value4.bounds
                let result = Movement.move Block.sizePow grid (pos + bounds.Min) bounds.Size velocity
                let isResting = velocity.Y > 0 && result.displacement.Y = 0
                if isResting then
                    rest <- { restTime = time}
                p <- { pos = pos + result.displacement }
                v <- { velocity = velocity + result.acceleration }
                f <- { force = Vector2i.Zero }

        member c.UpdateRun() =
            match c.TryGetPlayerEid() with
            | ValueNone -> ()
            | ValueSome eid ->
                let grid = c.Get<Grid<BlockType>>()
                let entity = c.Get(eid)
                let p = entity.Get<Position>()
                // Check for exiting right
                let xMax = (grid.Size.X - 1) * Block.size
                if p.pos.X > xMax then c.AdvanceLevel()
                else 
                    let run = &c.Get<RunState>()
                    run <- { run with duration = run.duration + 1L }
                // Check for falling through the bottom
                let yMax = grid.Size.Y * Block.size
                if p.pos.Y > yMax then
                    c.Destroy(eid)
                    
        member c.UpdateDamage(time) =
            let noHitDelay = 500L
            let minDamageSpeed = 32
            for r in c.Query<Eid, Position, Velocity, HitPoints, HitState, BoundingBox>() do
                let v = r.Value3
                if v.velocity.Y >= minDamageSpeed then
                    let eid = r.Value1
                    let p = r.Value2
                    let hp = &r.Value4
                    let hit = &r.Value5
                    let ba = r.Value6.bounds + p.pos
                    for r in c.Query<Position, BoundingBox, Damaging>() do
                        if time >= hit.nextHitTime then
                            let bb = r.Value2.bounds + r.Value1.pos
                            let overlap = Range2i.Intersection(ba, bb)
                            if not overlap.IsEmpty then
                                hp <- { hp with hits = hp.hits - r.Value3.damage }
                                hit <- { nextHitTime = time + noHitDelay }
                                if hp.hits = 0 then
                                    c.Destroy(eid)
                                c.Send(Hit())
                
    let add (c : Container) =
        c.On<FixedUpdate> <| fun e ->            
            let menu = c.Get<MenuState>()
            if not menu.isDisplayed then
                c.UpdatePickup()
                c.UpdateForcesFromInput(e.FixedTime)
                c.ApplyForces(e.FixedTime)
                c.UpdateRun()
                c.UpdateDamage(e.FixedTime)
