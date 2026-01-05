module HomePage

open Fabulous.Avalonia
open type Fabulous.Avalonia.View
open Fabulous
open SharedTypes

type Model = { Something: int }

type Msg =
    | DoSomething
    | NextLoginPage

let init = { Something = 0 }, Cmd.none

let update msg model =
    match msg with
    | DoSomething -> { model with Something = model.Something + 1 }, Cmd.none, NoIntent
    | NextLoginPage -> model, Cmd.none, GoToLoginPage

let view model =
    VStack() {
        TextBlock($"First Page: %d{model.Something}")
        //Button("Do something", fun _ -> dispatch DoSomething)
        //Button("Go to Page 2", fun _ -> dispatch GoToLoginPage)
        
        Button("Do something", DoSomething)
        Button("Go to Page 2", NextLoginPage)
    }