module GamePage

open Fabulous.Avalonia
open type Fabulous.Avalonia.View
open Fabulous
open SharedTypes

type Model = { Name: string }

type Msg =
    | ChangeName of string
    | NextHomePage

let init = { Name = "Hi 3rd Page" }

let update msg model =
    match msg with
    | ChangeName n -> { model with Name = n }, Cmd.none, NoIntent
    | NextHomePage -> model, Cmd.none, GoToHomePage

let view model =
    VStack() {
        TextBlock($"Third Page: {model.Name}")
        //Button("Go to Page 3", fun _ -> dispatch (App.NavigateTo SharedTypes.HomePage))
        Button("Go to Home Page", NextHomePage)
        Image(getImageUri "clubs_nine.png")
            .height(200.)
            .centerHorizontal()
    }
