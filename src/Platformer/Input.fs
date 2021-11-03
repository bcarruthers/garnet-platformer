namespace Platformer

open Garnet.Composition.Comparisons
open Garnet.Composition
open Garnet.Graphics
open Garnet.Input

module InputSystem =
    type Container with
        member c.ToggleMenu() =
            let menu = &c.Get<MenuState>()
            let timing = &c.Get<TimingSettings>() 
            menu <- { menu with isDisplayed = not menu.isDisplayed }
            timing <- { timing with IsRunning = not menu.isDisplayed }
        
        member c.AddInputHandler() =
            let cameras = c.Get<CameraSet>()
            let inputs = c.Get<InputCollection>()
            let handler = c.Get<InputHandler>()
            c.On<HandleInput> <| fun e ->
                let keys = c.Get<CommandKeyLookup>()
                let normToWorld = cameras.[Cameras.level].GetNormalizedToWorld()
                let commandState = handler.Handle(e.Time, inputs, keys, normToWorld)
                c.Send<CommandState>(commandState)

        member c.AddResetInput() =
            c.On<CommandState> <| fun e ->
                if CommandState.hasCommandPressed Command.Reset e then
                    c.Send(Reset())

        member c.AddMovement() =
            c.On<CommandState> <| fun e ->            
                let menu = c.Get<MenuState>()
                if not menu.isDisplayed then
                    match c.TryGetPlayerEid() with
                    | ValueNone -> ()
                    | ValueSome eid ->
                        let entity = c.Get(eid)
                        let move = &entity.Get<MoveInput>()
                        let xDir =
                            if int e.downCommands &&& int Command.MoveLeft <> 0 then -1
                            elif int e.downCommands &&& int Command.MoveRight <> 0 then 1
                            else 0
                        let isJumping = int e.pressCommands &&& int Command.Jump <> 0
                        // Accumulate into inputs since the input rate may be faster than fixed update rate
                        move <- {
                            xDir = if xDir <> 0 then xDir else move.xDir
                            isJumping = move.isJumping || isJumping
                            }

        member c.AddMenuInput() =
            c.On<CommandState> <| fun e ->
                match Command.tryGetMenuAction e.pressCommands with
                | ValueNone -> ()
                | ValueSome action ->
                    match action with
                    | CancelMenu -> c.ToggleMenu()
                    | ScrollMenu dir ->
                        let menu = &c.Get<MenuState>()
                        if menu.isDisplayed then
                            menu <- { menu with selectedMenuItem = MenuState.getNextItem menu.menuItems dir menu.selectedMenuItem }
                    | SelectMenuItem ->
                        let menu = c.Get<MenuState>()
                        if menu.isDisplayed then
                            match menu.selectedMenuItem with
                            | ResumeItem -> c.ToggleMenu() 
                            | RestartItem ->
                                c.ToggleMenu()
                                c.Send(Reset())
                            | ExitItem ->
                                let ren = c.Get<WindowRenderer>()
                                ren.Close()
            
        member c.AddFullScreen() =
            c.On<CommandState> <| fun e ->
                if CommandState.hasCommandPressed Command.FullScreen e then
                    c.Get<WindowRenderer>().ToggleFullScreen()

    let add (c : Container) =
        Disposable.Create [
            c.AddInputHandler()
            c.AddMovement()
            c.AddResetInput()
            c.AddMenuInput()
            c.AddFullScreen()
            ]
