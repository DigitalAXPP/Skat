module GameHub

open System.Threading.Tasks
open SignalRClient
open Microsoft.AspNetCore.SignalR.Client
open Fabulous
open SharedTypes
open Transport

type State =
    | Disconnected
    | Connecting
    | Connected
    | Error of string

type Model = { 
    Status: State
    Moves: string list
    Game: GameState
    Players: string list
    //Connection: ConnectionState
    //IsConnected: bool
}

type Msg =
    | TestHub
    | ConnectHub
    | ConnectedHub of HubConnection
    | DisconnectHub
    | HubSingleConnecting
    | HubConnected
    | HubDisconnected
    //| FromServer of ServerMsg
    | EnterGame of string
    | LeaveGame of string
    //| GameJoined
    //| GameQuit
    | MoveReceiving of string
    | SendMove of string
    //| ConnectionHubFailed of string
    | Domain of Messages.DomainMsg
    | EnterGameSucceeded
    | EnterGameFailed of string
    | ExitGameSucceeded
    | ExitGameFailed of string

//let mapServerMsg msg =
//    match msg with
//    | ServerMsg.MoveReceiving move -> ServerMsg.MoveReceiving move
//    | ServerMsg.JoinGame name -> ServerMsg.JoinGame name
//    | ServerMsg.QuitGame name -> ServerMsg.QuitGame name

let init() = 
    {
        Status = Disconnected
        Moves = []
        Game = NotInGame
        Players = []
    }, Cmd.none
    //Connection = HubDisconnected}, Cmd.none
    //IsConnected = false}, Cmd.none
    

let update (hubService: HubService) msg model =
    //match msg, model.Connection with
    match msg with
    | ConnectHub ->
        let cmd =
            Cmd.ofAsyncMsg (
                async {
                    do! hubService.Connect() |> Async.AwaitTask
                    return HubConnected
                }
            )
        model, cmd
    | HubConnected ->
        { model with
            Status = Connected
        },
        Cmd.none
    | HubDisconnected ->
        { model with
            Status = Disconnected
        },
        Cmd.none
    | DisconnectHub ->
        let cmd =
            Cmd.ofSub (fun _ ->
                hubService.Disconnect() |> ignore
            )
        { model with Status = Disconnected }, cmd
    //| FromServer serverMsg ->
    //    // map server → UI updates here
    //    match serverMsg with
    //    | JoinGame (players) ->
    //        { model with
    //            Game = InGame
    //            Players = players
    //        }, Cmd.none
    //    | QuitGame ->
    //        { model with Game = NotInGame }, Cmd.none
    //    | ServerMsg.MoveReceiving move ->
    //        { model with Moves = move :: model.Moves }, Cmd.none

    | Domain domainMsg ->
        match domainMsg with
        //| Messages.FromServer serverMsg ->
        //    match serverMsg with
        //    | JoinGame players ->
        //        { model with Game = InGame; Players = players }, Cmd.none
        //    | QuitGame ->
        //        { model with Game = NotInGame }, Cmd.none
        //    | ServerMsg.MoveReceiving move ->
        //        { model with Moves = move :: model.Moves }, Cmd.none

        | Messages.EnterGame name ->
            model, Cmd.ofMsg (EnterGame name)

        | Messages.LeaveGame name ->
            model, Cmd.ofMsg (LeaveGame name)

        | Messages.GameJoined players ->
            { model with
                Game = InGame
                Players = players
            }, Cmd.none

        | Messages.GameLeft ->
            { model with 
                Game = NotInGame 
                Players = []
            }, Cmd.none

        | Messages.MoveReceived move ->
            { model with Moves = move :: model.Moves }, Cmd.none

        | Messages.ConnectionHubFailed err ->
            { model with Status = Error err }, Cmd.none

    //| GameJoined, _ -> {model with Status = Connected.ToString()}, Cmd.none
    //| GameQuit, _ -> {model with Status = Disconnected.ToString()}, Cmd.none

    //| DisconnectHub, _ ->
    //    match model.Connection with
    //    | HubConnected hub -> 
    //        let cmd = 
    //            Cmd.ofAsyncMsg (async {
    //                disconnect hub |> Async.AwaitTask |> ignore
    //                return HubDisconnected 
    //            })
                
        //{ model with Status = Disconnected; IsConnected = false }, Cmd.none

    //| ConnectedHub hub, _ ->
    //    { model with
    //        Status = Connected.ToString()
    //        Connection = HubConnected hub },
    //    Cmd.none

    //| EnterGame name when model.Status = Connected ->
    //    let cmd =
    //        Cmd.ofSub (fun _ ->
    //            hubService.SendMove($"JOIN:{name}") |> ignore
    //        )
    | EnterGame name ->
        match model.Status with
        | Connected -> 
            let cmd =
                Cmd.ofAsyncMsg (async {
                    try
                        do! hubService.EnterGame(name) |> Async.AwaitTask
                        return EnterGameSucceeded
                    with exn ->
                        return EnterGameFailed exn.Message
                })
            model, cmd
        | _ -> model, Cmd.ofMsg (EnterGameFailed $"Status is: {model.Status}")

    | EnterGameSucceeded ->
        model, Cmd.none

    | EnterGameFailed err ->
        model, Cmd.none
    //| LeaveGame name when model.Status = Connected ->
    //    let cmd =
    //        Cmd.ofSub (fun _ ->
    //            hubService.SendMove($"QUIT:{name}") |> ignore
    //        )
    | LeaveGame name ->
        match model.Status with
        | Connected ->
            let cmd =
                Cmd.ofAsyncMsg (async {
                    try
                        do! hubService.LeaveGame(name) |> Async.AwaitTask
                        return ExitGameSucceeded
                    with exn ->
                        return ExitGameFailed exn.Message

                })
            model, cmd
        | _ -> model, Cmd.ofMsg (ExitGameFailed $"Status is: {model.Status}")

    | ExitGameSucceeded ->
        model, Cmd.none

    | ExitGameFailed err ->
        model, Cmd.none

    | MoveReceiving move when model.Status = Connected ->
        let cmd =
            Cmd.ofSub (fun _ ->
                hubService.SendMove(move) |> ignore
            )
    //| MoveReceiving move, _ ->
    //    match model.Connection with
    //    | HubConnected hub -> 
    //        let cmd =
    //            Cmd.ofAsyncMsg (async {
    //                try
    //                    do! hub.InvokeAsync("SendMove", move) |> Async.AwaitTask
    //                    return GameJoined
    //                with exn ->
    //                    return ConnectionHubFailed exn.Message
    //            })
        { model with Moves = move :: model.Moves }, cmd

    //| ConnectionHubFailed err, _ ->
    //| ConnectionHubFailed err ->
    //    //{ model with Status = $"Connection failed: {err}" }, Cmd.none
    //    { model with Status = Error $"{err}" }, Cmd.none

    | _ -> model, Cmd.none
