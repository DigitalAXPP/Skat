module LoginPage

open Fabulous.Avalonia
open type Fabulous.Avalonia.View
open Fabulous
open SharedTypes
open SignalRClient
open Fabulous.Dispatcher
open Skat.Database
//open Avalonia.Controls

type Model = { 
    UserName: string
    Id: int
    Email: string
    PasswordHash: string
}

type Msg =
    | ChangeUserName of string
    | ChangeId of string
    | ChangeEmail of string
    | SetPassword of string
    | NextGamePage
    | StartNewGame of string
    | EndGame of string
    | SelectCard of string
    | TellAll of string
    | RequestConnection of string
    | NewGameRoom
    | GetAllRooms

let init () =
    { 
        UserName = ""
        Id = 0
        Email = "test@test.com"
        PasswordHash = ""
    }, Cmd.none

let update msg model =
    match msg with
    | ChangeUserName n -> { model with UserName = n }, Cmd.none, NoIntent
    | ChangeId number -> { model with Id = (System.Int32.Parse(number))}, Cmd.none, NoIntent
    | ChangeEmail email -> { model with Email = email }, Cmd.none, NoIntent
    | SetPassword password -> { model with PasswordHash = password }, Cmd.none, NoIntent
    | RequestConnection name ->
        model, Cmd.none, NoIntent
    | NextGamePage -> model, Cmd.none, NavigateTo PageGame
    | StartNewGame name ->
        { model with UserName = name }, Cmd.none, StartGameRequested name
    | EndGame name ->
        model, Cmd.none, EndGameRequested name
    | SelectCard card ->
        model, Cmd.none, SendSelectedCard card
    | TellAll message ->
        model, Cmd.none, SendMessageToAll message
    | NewGameRoom ->
        model, Cmd.none, NewRoom
    | GetAllRooms ->
        model, Cmd.none, AllRooms

let view (hub: HubService option) model =
    VStack() {
        TextBlock($"Hub connection: {hub.Value.IsConnected}")
        TextBlock($"Second Page: {model.UserName}")
        Button("Connect to hub.", RequestConnection model.UserName)
        TextBox(model.UserName, ChangeUserName)
        TextBox(model.Id.ToString(), ChangeId)
        TextBox(model.Email, ChangeEmail)
        TextBox(model.PasswordHash, SetPassword)
        Button("Start New Game", StartNewGame model.UserName)
        Button("Send Message to All", TellAll model.UserName)
        Button("Quit Game", EndGame model.UserName)
        Button("Add move", SelectCard "right")
        Button("Create Game Room", NewGameRoom)
        Button("Get Game Rooms", GetAllRooms)
        Button("Go to Game Page", NextGamePage)
    }