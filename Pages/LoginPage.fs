module LoginPage

open Fabulous.Avalonia
open type Fabulous.Avalonia.View
open Fabulous
open SharedTypes
open SignalRClient
open Fabulous.Dispatcher
open Skat.Data.Usermanagement
open Skat.Database

type Model = { 
    UserName: string
    Id: int
    Email: string
    PasswordHash: string
    Users: User list option
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
    | LoadUsers
    | UsersLoaded of User list
    | CreateUser

let init () =
    { 
        UserName = ""
        Id = 0
        Email = "test@test.com"
        PasswordHash = ""
        Users = None
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
    | LoadUsers ->
        //let cmd = 
        //    Cmd.ofAsyncMsg (async {
        //        SkatDB.init () |> Async.RunSynchronously
        //        let! users = Skat.UserRepository.Generics.getUsers ()
        //        return UsersLoaded users
        //    } )
        model, Cmd.none, NoIntent
    | UsersLoaded users ->
        let updatedModel =
            match users with
            | [] -> model
            | _ -> { model with Users = Some users}
        updatedModel, Cmd.none, NoIntent
    | CreateUser ->
        //let cmd =
        //    Cmd.ofAsyncMsg (async {
        //        let newUser = { Id = model.Id; Name = model.UserName; Email = model.Email; PasswordHash = model.PasswordHash }
        //        do! Skat.UserRepository.Generics.insertUser newUser
        //        let! users = Skat.UserRepository.Generics.getUsers ()
        //        return UsersLoaded users
        //    })
        model, Cmd.none, AddUser (model.UserName, model.Email, model.PasswordHash)

let view (hub: HubService option) model =
    VStack() {
        TextBlock($"Hub connection: {hub.Value.IsConnected}")
        TextBlock($"Second Page: {model.UserName}")
        Button("Connect to hub.", RequestConnection model.UserName)
        TextBox(model.UserName, ChangeUserName)
        TextBox(model.Id.ToString(), ChangeId)
        TextBox(model.Email, ChangeEmail)
        TextBox(model.PasswordHash, SetPassword)
        Button("Load Users", LoadUsers)
        Button("Add User", CreateUser)
        Button("Start New Game", StartNewGame model.UserName)
        Button("Send Message to All", TellAll model.UserName)
        Button("Quit Game", EndGame model.UserName)
        Button("Add move", SelectCard "right")
        Button("Go to Game Page", NextGamePage)
    }