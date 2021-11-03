namespace Platformer

open System
open System.Numerics
open Garnet.Composition.Comparisons
open Garnet.Composition
open Garnet.Numerics

module Bits =
    let log2 x =
        let mutable log = 0
        let mutable y = x
        while y > 1 do
            y <- y >>> 1
            log <- log + 1;
        log

    let nextLog2 x =
        let log = log2 x
        if x - (1 <<< log) > 0 then 1 + log else log

    let nextPow2 x =
        1 <<< nextLog2 x
    
module Scalar =
    let tolerance = 1e-9f

    let clamp (s0 : float32) (s1 : float32) (s : float32) =
        s |> max s0 |> min s1

    let linearStep s0 s1 s =
        let length = s1 - s0
        if abs length < tolerance then 0.0f
        else clamp 0.0f 1.0f ((s - s0) / length)

    let smoothStep s0 s1 s =
        let x = linearStep s0 s1 s
        x * x * (3.0f - 2.0f * x)

    let lerp min max (t : float32) =
        min * (1.0f - t) + max * t

module Vector2i =
    let truncate (limit : Vector2i) (v : Vector2i) =
        let sign = Vector2i(sign v.X, sign v.Y)
        let magnitude = Vector2i(abs v.X, abs v.Y)
        Vector2i(
            min magnitude.X limit.X * sign.X,
            min magnitude.Y limit.Y * sign.Y)
        
module Vector2 =
    let rotate (rot : Vector2) (a : Vector2) =
        Vector2(a.X * rot.X - a.Y * rot.Y, a.X * rot.Y + a.Y * rot.X)

    let inRange (p1 : Vector2) (p2 : Vector2) maxDist =
        let distSqr = (p2 - p1).LengthSquared()
        let maxDistSqr = maxDist * maxDist
        distSqr <= maxDistSqr

module Matrix4x4 =
    let getPixelScale desired available =
        floor (float32 available / float32 desired) |> int
        
    let getScaledSize (desiredSize : Vector2i) (availableSize : Vector2i) =
        let pixelScale =
            let xs = getPixelScale desiredSize.X availableSize.X
            let ys = getPixelScale desiredSize.Y availableSize.Y
            min xs ys
        desiredSize * pixelScale
        
    let getPixelViewportTransform (desiredSize : Vector2i) (availableSize : Vector2i) =
        // Note we don't need to apply any centering transform since origin is at center,
        // but this could mean a partial pixel offset causing blurriness in some cases
        let scaledSizeInPixels = getScaledSize desiredSize availableSize
        let scale = scaledSizeInPixels.ToVector2() / availableSize.ToVector2()
        Matrix4x4.CreateScale(scale.X, scale.Y, 1.0f)
        
    let getShakeTransform magnitude seed (time : int64) =
        let seed = float32 seed
        let time = float time / 1000.0 |> float32
        let x = Noise.Sample(Vector2(time, seed + 1.0f)) * magnitude
        let y = Noise.Sample(Vector2(time, seed + 2.0f)) * magnitude
        //let r = Noise.sample2 (Vector2(time, seed + 3.0f))
        Matrix4x4.CreateTranslation(x, y, 0.0f)
        
module Partition =
    let ships = 0
    let trails = 1
    let bullets = 2
    
module Eid =
    let isShipSegment sid =
        Eid(sid <<< Segment.SegmentBits).Partition = Partition.ships

module Command =
    let commands = [|
        Command.Cancel
        Command.Reset
        Command.FullScreen
        Command.Debug
        Command.Fire
        Command.Jump
        Command.MoveLeft
        Command.MoveRight
        Command.MoveUp
        Command.MoveDown
        |]

    let tryGetMenuAction =
        function
        | Command.Cancel -> ValueSome CancelMenu
        | Command.MoveUp -> ValueSome (ScrollMenu PreviousItem)
        | Command.MoveDown -> ValueSome (ScrollMenu NextItem)
        | Command.Fire -> ValueSome SelectMenuItem
        | _ -> ValueNone
        
module CommandState =
    let hasCommandDown (command : Command) state =
        int state.downCommands &&& int command <> 0

    let hasCommandPressed (command : Command) state =
        int state.pressCommands &&& int command <> 0

module MenuItem =
    let format item =
        match item with
        | ResumeItem -> "Resume"
        | RestartItem -> "Restart"
        | ExitItem -> "Exit"

module MenuState =
    let getNextItem items dir item =
        match dir with
        | PreviousItem ->
            let index = List.findIndex ((=)item) items
            let newIndex = if index = 0 then items.Length - 1 else index - 1
            items.[newIndex]
        | NextItem ->
            let index = List.findIndex ((=)item) items
            let newIndex = if index = items.Length - 1 then 0 else index + 1
            items.[newIndex]

    let initial = {
        isDisplayed = true
        selectedMenuItem = ResumeItem
        menuItems = [
            ResumeItem
            RestartItem
            ExitItem
            ]
        }
    
module Resolution =
    let width = 320
    let height = 180
    let viewSize = Vector2i(width, height)
    
module ScreenShakeState =
    let zero = { trauma = 0.0f }
    
    let getMagnitude s =
        s.trauma * s.trauma
        
    let add delta s =
        { trauma = (s.trauma + delta) |> min 1.0f |> max 0.0f }
        
    let getShakeTransform magnitude degrees seed (time : float32) =
        let seed = float32 seed
        let x = Noise.Sample(Vector2(time, seed + 1.0f)) * magnitude
        let y = Noise.Sample(Vector2(time, seed + 2.0f)) * magnitude
        let r = Noise.Sample(Vector2(time, seed + 3.0f)) * degrees * MathF.PI / 180.0f
        Matrix4x4.CreateTranslation(x, y, 0.0f) *
        Matrix4x4.CreateRotationZ(r)
