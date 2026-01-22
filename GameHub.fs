module GameHub

open System.Threading.Tasks
open SignalRClient
open Microsoft.AspNetCore.SignalR.Client
open Fabulous
open SharedTypes

type Model = { 
    Status: string
    Moves: string list
    //Connection: ConnectionState
    IsConnected: bool
}

type State =
    | Disconnected
    | Connecting
    | Connected

type Msg =
    | TestHub
    | ConnectHub
    | ConnectedHub of HubConnection
    | DisconnectHub
    | HubSingleConnecting
    | HubConnected
    | HubDisconnected
    | ServerMsg of ServerMsg
    | EnterGame of string
    //| GameJoined
    //| GameQuit
    | LeaveGame of string
    | MoveReceiving of string
    | SendMove of string
    | ConnectionHubFailed of string

let mapServerMsg msg =
    match msg with
    | ServerMsg.MoveReceiving move -> MoveReceiving move

let init() = {
    Status = Disconnected.ToString()
    Moves = []
    //Connection = HubDisconnected}, Cmd.none
    IsConnected = false}, Cmd.none

let update (hubService: HubService) msg model =
    //match msg, model.Connection with
    match msg with
    | ConnectHub ->
        let cmd =
            Cmd.ofSub (fun _ ->
                hubService.Connect() |> ignore
            )
        {model with Status = Connected.ToString()}, cmd
    | HubConnected ->
        { model with
            Status = Connected.ToString()
            IsConnected = true
        },
        Cmd.none
    | HubDisconnected ->
        { model with
            Status = Disconnected.ToString()
            IsConnected = false
        },
        Cmd.none
    | DisconnectHub ->
        let cmd =
            Cmd.ofSub (fun _ ->
                hubService.Disconnect() |> ignore
            )
        { model with Status = Disconnected.ToString(); IsConnected = false }, cmd
    | ServerMsg serverMsg ->
        // map server → UI updates here
        match serverMsg with
        | JoinGame _ ->
            { model with Status = "Game joined" }, Cmd.none
        | QuitGame _ ->
            { model with Status = "Game quit" }, Cmd.none
        | ServerMsg.MoveReceiving move ->
            { model with Moves = move :: model.Moves }, Cmd.none
    //| GameJoined, _ -> {model with Status = Connected.ToString()}, Cmd.none
    //| GameQuit, _ -> {model with Status = Disconnected.ToString()}, Cmd.none

    | DisconnectHub when model.IsConnected ->
        let cmd =
            Cmd.ofSub (fun _ ->
                hubService.Disconnect() |> ignore
            )
    //| DisconnectHub, _ ->
    //    match model.Connection with
    //    | HubConnected hub -> 
    //        let cmd = 
    //            Cmd.ofAsyncMsg (async {
    //                disconnect hub |> Async.AwaitTask |> ignore
    //                return HubDisconnected 
    //            })
                
        { model with Status = Disconnected.ToString(); IsConnected = false }, Cmd.none

    //| ConnectedHub hub, _ ->
    //    { model with
    //        Status = Connected.ToString()
    //        Connection = HubConnected hub },
    //    Cmd.none

    | EnterGame name when model.IsConnected ->
        let cmd =
            Cmd.ofSub (fun _ ->
                hubService.SendMove($"JOIN:{name}") |> ignore
            )
    //| EnterGame name, _ ->
        //match model.Connection with
        //| HubConnected hub -> 
        //    let cmd =
        //        Cmd.ofAsyncMsg (async {
        //            try
        //                do! hub.InvokeAsync("JoinGame", "game1", name) |> Async.AwaitTask
        //                return GameJoined
        //            with exn ->
        //                return ConnectionHubFailed exn.Message
        //        })
        model, cmd

    | LeaveGame name when model.IsConnected ->
        let cmd =
            Cmd.ofSub (fun _ ->
                hubService.SendMove($"QUIT:{name}") |> ignore
            )
    //| LeaveGame name, _ ->
    //    match model.Connection with
    //    | HubConnected hub ->
    //        let cmd =
    //            Cmd.ofAsyncMsg (async {
    //                try
    //                    do! hub.InvokeAsync("QuitGame", "game1", name) |> Async.AwaitTask
    //                    return GameQuit
    //                with exn ->
    //                    return ConnectionHubFailed exn.Message

    //            })
        model, cmd

    | MoveReceiving move when model.IsConnected ->
        let cmd =
            Cmd.ofSub (fun _ ->
                hubService.SendMove($"MOVE:{move}") |> ignore
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
    | ConnectionHubFailed err ->
        { model with Status = $"Connection failed: {err}" }, Cmd.none

    | _ -> model, Cmd.none
