namespace Platformer

open System.Numerics
open Garnet.Audio
open Garnet.Numerics

[<AutoOpen>]
module Components =
    [<Struct>]
    type Position = {
        pos : Vector2i
        }

    [<Struct>]
    type Velocity = {
        velocity : Vector2i
        }

    [<Struct>]
    type Force = {
        force : Vector2i
        }

    [<Struct>]
    type Resting = {
        restTime : int64
        }

    [<Struct>]
    type Damping = {
        maxVelocity : Vector2i
        friction : Vector2i
        }

    [<Struct>]
    type MoveInput = {
        xDir : int
        isJumping : bool
        }

    [<Struct>]
    type BoundingBox = {
        bounds : Range2i
        }

    [<Struct>]
    type EnemyType =
        | Spike
    
    [<Struct>]    
    type Damaging = {
        damage : int
        }
    
    type Controlled = struct end
    type HealthPowerup = struct end

    [<Struct>]
    type Facing =
        | Left
        | Right

    [<Struct>]
    type AnimationType =
        | Idle
        | Walking
        | Jumping
        | Firing

    [<Struct>]
    type Lifespan = {
        endTime : int64
        }

    [<Struct>]
    type HitState = {
        nextHitTime : int64
        }

    [<Struct>]
    type HitPoints = {
        maxHits : int
        hits : int
        }
    
[<AutoOpen>]
module Events =
    type Jumped = struct end
    type Landed = struct end
    type Hit = struct end
    
    [<Struct>]
    type PlaySound = {
        soundKey : string
        playback : SoundPlayback
        }

    [<Struct>]
    type Shake = {
        trauma : float32
        }

    type Reset = struct end

    [<Struct>]
    type Command =
        | None = 0
        | MoveLeft = 0x01
        | MoveRight = 0x02
        | MoveUp = 0x04
        | MoveDown = 0x08
        | Jump = 0x10
        | Fire = 0x20
        | Cancel = 0x40
        | Reset = 0x80
        | FullScreen = 0x100
        | Debug = 0x200

    [<Struct>]
    type CommandState = {
        aimPos : Vector2
        time : int64
        downCommands : Command
        pressCommands : Command
        }

[<AutoOpen>]
module States =
    type MenuItem =
        | ResumeItem
        | RestartItem
        | ExitItem
        
    type MenuState = {
        isDisplayed : bool
        selectedMenuItem : MenuItem
        menuItems : MenuItem list
        }
    
    type MenuScrollDirection =
        | PreviousItem
        | NextItem
    
    type MenuAction =
        | ScrollMenu of MenuScrollDirection
        | SelectMenuItem
        | CancelMenu
    
    [<Struct>]
    type ScreenShakeState = {
        trauma : float32
        }        

    [<Struct>]
    type RunState = {
        seed : uint32
        level : int
        duration : int64
        }

    [<Struct>]
    type PlayerState = {
        hp : HitPoints
        }
