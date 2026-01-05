module GamePage

open Fabulous.Avalonia
open type Fabulous.Avalonia.View
open Fabulous
open SharedTypes
open Skat.Game.Types
open Skat.Game.Functions

type Model = { 
    Name: string
    CardSelected: Card option
}

type Msg =
    | ChangeName of string
    | NextHomePage

let init = { Name = "Hi 3rd Page"; CardSelected = None }, Cmd.none

let update msg model =
    match msg with
    | ChangeName n -> { model with Name = n }, Cmd.none, NoIntent
    | NextHomePage -> model, Cmd.none, GoToHomePage

let view model =
        VStack(spacing = 25.) {
            TextBlock($"Third Page: {model.Name}")
            //Button("Go to Page 3", fun _ -> dispatch (App.NavigateTo SharedTypes.HomePage))
            Button("Go to Home Page", NextHomePage)
            Image(getImageUri (cardToImageName { Suite = Clubs; Rank = King}))
                .height(200.)
                .centerHorizontal()
            let items = Deck
            ListBox(items, fun item ->
                ListBoxItem(
                    Image(getImageUri (cardToImageName item))
                        .height(15.)
                        .centerHorizontal()
                        .onTapped(fun _ -> ChangeName (sprintf "%A" item))
                )
            )
        
        //ListBox() {
        //    ListBoxItem(
        //        Image(getImageUri "clubs_eight.png")
        //            .height(100.)
        //            .centerHorizontal()
        //    )
        //    ListBoxItem(
        //        Image(getImageUri "clubs_seven.png")
        //            .height(100.)
        //            .centerHorizontal()
        //    )
        //}
    }
