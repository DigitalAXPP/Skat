module LoginPage

open Fabulous.Avalonia
open type Fabulous.Avalonia.View
open Fabulous
open SharedTypes
open SignalRClient
open Fabulous.Dispatcher

type Model = { 
    UserName: string
    Hub: GameHub.Model
}

type Msg =
    | ChangeName of string
    | NextGamePage
    | HubMsg of GameHub.Msg
    | StartNewGame of string
    | EndGame of string
    | SelectCard of string
    | TellAll of string
    | RequestConnection of string

let init () =
    let hubModel, hubCmd = GameHub.init()
    { 
        UserName = ""
        Hub = hubModel
    }, Cmd.map HubMsg hubCmd

let update msg model =
    match msg with
    | ChangeName n -> { model with UserName = n }, Cmd.none, NoIntent
    | RequestConnection name ->
        model, Cmd.none, NoIntent
    | NextGamePage -> model, Cmd.none, GoToGamePage
    | StartNewGame name ->
        { model with UserName = name }, Cmd.none, StartGameRequested name
    | EndGame name ->
        model, Cmd.none, EndGameRequested name
    | SelectCard card ->
        model, Cmd.none, SendSelectedCard card
    | TellAll message ->
        model, Cmd.none, SendMessageToAll message

let view (hub: HubService option) model =
    VStack() {
        TextBlock($"Hub connection: {hub.Value.IsConnected}")
        TextBlock($"Second Page: {model.UserName}")
        TextBlock($"Hub status: {model.Hub.Status}")
        TextBlock($"Moves: {model.Hub.Moves}")
        Button("Connect to hub.", RequestConnection model.UserName)
        TextBox(model.UserName, ChangeName)
        Button("Start New Game", StartNewGame model.UserName)
        Button("Send Message to All", TellAll model.UserName)
        Button("Quit Game", EndGame model.UserName)
        Button("Add move", SelectCard "right")
        Button("Go to Game Page", NextGamePage)
    }
