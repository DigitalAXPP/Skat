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
        let items = ["clubs_eight.png"; "clubs_ace.png"]
        ListBox(items, fun item ->
            ListBoxItem(
                Image(getImageUri item)
                    .height(100.)
                    .centerHorizontal()
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
