namespace Platformer

open System
open Garnet.Numerics

type BlockType =
    | Air = 0
    | Rock = 1
    | Spike = 2

module BlockType =
    let fromChar ch =
        match ch with
        | '#' -> BlockType.Rock
        | _ -> BlockType.Air
    
module Block =
    let sizePow = 8
    let size = 256

type CellType =
    | Empty = 0
    | Solid = 1
    | Enemy = 2
    | Hazard = 3
    | Powerup = 4
    | Entrance = 5
    
module CellType =
    let fromColor =
        function
        | 0x000000ffu -> CellType.Solid 
        | 0xff0000ffu -> CellType.Enemy 
        | 0xff00ffffu -> CellType.Hazard
        | 0xffff00ffu -> CellType.Powerup
        | 0x00ffffffu -> CellType.Entrance
        | _ -> CellType.Empty

    let toBlock =
        function
        | CellType.Empty
        | CellType.Enemy
        | CellType.Powerup
        | CellType.Entrance -> BlockType.Air
        | CellType.Hazard -> BlockType.Air
        | CellType.Solid -> BlockType.Rock
        | _ -> BlockType.Rock

type LevelPrefabData(width, height, variantCount : int, cells : CellType[]) =
    member _.Generate(seed : uint32, level : int) =
        let xRooms = 3
        let yRooms = 2
        let roomWidth = width / xRooms
        let roomHeight = height / yRooms
        let cellsPerLevel = width * height
        let levelCells = Array.zeroCreate cellsPerLevel
        for ry = 0 to yRooms - 1 do
            for rx = 0 to xRooms - 1 do
                let roomIndex =
                    let hash = XXHash.Hash(seed, uint level, uint ry, uint rx)
                    hash % uint variantCount |> int
                for y = 0 to roomHeight - 1 do
                    let offsetInLevel = (ry * roomHeight + y) * width + rx * roomWidth
                    let srcStart = roomIndex * cellsPerLevel + offsetInLevel 
                    let destStart = offsetInLevel
                    let src = cells.AsSpan(srcStart, roomWidth)
                    let dest = levelCells.AsSpan(destStart, roomWidth)
                    src.CopyTo(dest)
        Grid<CellType>(Vector2i(width, height), levelCells)
    