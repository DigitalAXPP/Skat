module LoginPage

open Fabulous.Avalonia
open type Fabulous.Avalonia.View
open Fabulous
open SharedTypes

type Model = { Name: string }

type Msg =
    | ChangeName of string
    | NextGamePage

let init = { Name = "Hi" }

let update msg model =
    match msg with
    | ChangeName n -> { model with Name = n }, Cmd.none, NoIntent
    | NextGamePage -> model, Cmd.none, GoToGamePage

let view model =
    VStack() {
        TextBlock($"Second Page: {model.Name}")
        //Button("Go to Page 3", fun _ -> dispatch (App.NavigateTo SharedTypes.GamePage))
        Button("Go to Game Page", NextGamePage)
    }
