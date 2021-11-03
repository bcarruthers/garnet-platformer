namespace Platformer

open Garnet.Graphics

module Resources =
    let settings = "settings.json"
    
module Textures =
    let atlas = "textures"
    let solid = "pixel.png"
    let rogue = "rogue.png"
    let heart = "heart.png"
    let heartOutline = "heart-outline.png"
    let stoneWall = "stone-wall-noborder.png"
    let spike = "spike.png"

module Fonts =
    let mono = "font-mono-outline-7x11.png"
    
module ShaderSets =
    let textureColor : ShaderSetDescriptor<PositionTextureColorVertex> = {
        VertexShader = "shaders/texture-color.vert"
        FragmentShader = "shaders/texture-color.frag"
        }
    
module Pipelines =
    let normal = {
        Blend = Blend.Alpha
        Filtering = Filtering.Point
        ShaderSet = ShaderSets.textureColor
        Texture = Textures.atlas
        }
    
    let particles = {
        Blend = Blend.Additive
        Filtering = Filtering.Point
        ShaderSet = ShaderSets.textureColor
        Texture = Textures.atlas
        }

module Cameras =
    let viewport = 0
    let main = 1
    let level = 2
    let hud = 3

module SpriteLayers =
    let init layerId cameraId pipeline = {
        LayerId = layerId
        CameraId = cameraId
        Pipeline = pipeline
        Primitive = Quad
        FlushMode = FlushOnDraw
        }
        
    let trails          = init 0  Cameras.level Pipelines.particles
    let particles       = init 1  Cameras.level Pipelines.particles
    let tiles           = init 2  Cameras.level Pipelines.normal
    let hostiles        = init 3  Cameras.level Pipelines.normal
    let neutrals        = init 4  Cameras.level Pipelines.normal
    let player          = init 5  Cameras.level Pipelines.normal
    let bullets         = init 6  Cameras.level Pipelines.particles
    let hud             = init 7  Cameras.main Pipelines.normal
    let border          = init 8  Cameras.main Pipelines.normal
    let text            = init 9  Cameras.main Pipelines.normal
    let flash           = init 10 Cameras.main Pipelines.normal
    let bgDebug         = init 11 Cameras.level Pipelines.normal
    let tileDebug       = init 12 Cameras.level Pipelines.normal
    let debug           = init 13 Cameras.level Pipelines.normal
