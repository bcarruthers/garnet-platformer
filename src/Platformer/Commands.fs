namespace Platformer

open System.Numerics
open System.Collections.Generic
open Veldrid
open Garnet.Input

type KeyBinding = {
    command : Command
    key : Key
    }

type Settings = {
    commands : KeyBinding[]
    }

type CommandKeyLookup(bindings) =
    let empty = List<Key>()
    let dict =
        let dict = Dictionary<Command, List<Key>>()
        for binding in bindings do
            let list =
                match dict.TryGetValue(binding.command) with
                | true, x -> x
                | false, _ ->
                    let list = List<Key>()
                    dict.Add(binding.command, list)
                    list
            list.Add(binding.key)
        dict
    member c.GetKeys(cmd) : IReadOnlyList<Key> =
        match dict.TryGetValue(cmd) with
        | true, list -> list
        | false, _ -> empty
        :> IReadOnlyList<Key>

type InputAxis() =
    let mutable dir = 0.0f
    member c.Direction = dir
    member c.Update(input : InputCollection, posKey, negKey) =
        let neg = input.IsAnyKeyDown(negKey)
        let pos = input.IsAnyKeyDown(posKey)
        let newDir =
            if neg || pos then
                if input.IsAnyKeyPressed(negKey) then -1.0f
                elif input.IsAnyKeyPressed(posKey) then +1.0f
                elif neg <> pos then
                    if neg then -1.0f
                    else 1.0f
                else dir
            else 0.0f
        if newDir <> dir then
            dir <- newDir

type InputAxis with
    member c.Update(input, lookup : CommandKeyLookup, posCmd, negCmd) =
        let pb = lookup.GetKeys(posCmd)
        let nb = lookup.GetKeys(negCmd)
        c.Update(input, pb, nb)
        (if c.Direction > 0.0f then posCmd else Command.None) |||
        (if c.Direction < 0.0f then negCmd else Command.None)

type CommandKeyLookup with
    member c.GetKeyDownCommand(input : InputCollection, cmd) =
        let b = c.GetKeys(cmd)
        let isDown = input.IsAnyKeyDown(b)
        if isDown then cmd else Command.None        

    member c.GetKeyPressedCommand(input : InputCollection, cmd) =
        let b = c.GetKeys(cmd)
        let isDown = input.IsAnyKeyPressed(b)
        if isDown then cmd else Command.None        

    member c.GetKeyDownCommand(input, arr : _[]) =
        let mutable combined = Command.None
        for cmd in arr do
            combined <- combined ||| c.GetKeyDownCommand(input, cmd)
        combined

    member c.GetKeyPressedCommand(input, arr : _[]) =
        let mutable combined = Command.None
        for cmd in arr do
            combined <- combined ||| c.GetKeyPressedCommand(input, cmd)
        combined

type InputHandler() =
    let moveX = InputAxis()
    let moveY = InputAxis()
    member c.Handle(time, inputs : InputCollection, keys, normToWorld : Matrix4x4) = {
        time = time
        aimPos = Vector2.Transform(inputs.NormalizedMousePosition, normToWorld)
        downCommands =
            moveX.Update(inputs, keys, Command.MoveLeft, Command.MoveRight) |||
            moveY.Update(inputs, keys, Command.MoveDown, Command.MoveUp) |||
            keys.GetKeyDownCommand(inputs, Command.commands)
        pressCommands =
            keys.GetKeyPressedCommand(inputs, Command.commands)                
        }
    