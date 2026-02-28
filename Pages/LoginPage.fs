module LoginPage

open Fabulous.Avalonia
open type Fabulous.Avalonia.View
open Fabulous
open SharedTypes
open SignalRClient
open Fabulous.Dispatcher

type Model = { 
    Name: string
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
    | RequestConnection

let init () =
    let hubModel, hubCmd = GameHub.init()
    { 
        Name = "Hi"
        Hub = hubModel
    }, Cmd.map HubMsg hubCmd

let update (hubService: HubService) msg model =
    match msg with
    | ChangeName n -> { model with Name = n }, Cmd.none, NoIntent
    | RequestConnection ->
        model, Cmd.none, NoIntent
    | NextGamePage -> model, Cmd.none, GoToGamePage
    | HubMsg hubMsg ->
        // Delegate updates to GameHub.update
        let newHub, hubCmd = GameHub.update hubService hubMsg model.Hub
        { model with Hub = newHub }, Cmd.map HubMsg hubCmd, NoIntent
    | StartNewGame name ->
        model, Cmd.none, StartGameRequested name
    | EndGame name ->
        model, Cmd.none, EndGameRequested name
    | SelectCard card ->
        model, Cmd.none, SendSelectedCard card
    | TellAll message ->
        model, Cmd.none, SendMessageToAll message

let view (hub: HubService option) model =
    VStack() {
        TextBlock($"Hub connection: {hub.Value.IsConnected}")
        TextBlock($"Second Page: {model.Name}")
        TextBlock($"Hub status: {model.Hub.Status}")
        TextBlock($"Moves: {model.Hub.Moves}")
        Button("Connect to hub.", RequestConnection)
        TextBox(model.Name, ChangeName)
        Button("Start New Game", StartNewGame model.Name)
        Button("Send Message to All", TellAll model.Name)
        Button("Quit Game", EndGame model.Name)
        Button("Add move", SelectCard "right")
        Button("Go to Game Page", NextGamePage)
    }
