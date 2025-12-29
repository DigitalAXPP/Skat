module GameHub

open System.Threading.Tasks
open SignalRClient
open Microsoft.AspNetCore.SignalR.Client
open Fabulous

type Model = { 
    Status: string
    Moves: string list
    Connection: ConnectionState
}

type Msg =
    | TestHub
    | ConnectHub
    | ConnectedHub of HubConnection
    | DisconnectHub
    | EnterGame of string
    | GameJoined
    | GameQuit
    | LeaveGame of string
    | MoveReceiving of string
    | SendMove of string
    | ConnectionHubFailed of string

let mapServerMsg msg =
    match msg with
    | ServerMsg.MoveReceiving move -> MoveReceiving move

let init() = {
    Status = "Not connected"
    Moves = []
    Connection = HubDisconnected}, Cmd.none

let update msg model =
    match msg, model.Connection with
    | GameJoined, _ -> {model with Status = "Connected"}, Cmd.none
    | GameQuit, _ -> {model with Status = "Disconnected"}, Cmd.none
    //| ConnectHub, _ ->
    //    let cmd = 
    //        Cmd.ofAsyncMsg (async {
    //            try
    //                let! hub = connect "http://localhost:5000/gamehub" ignore |> Async.AwaitTask
    //                return ConnectedHub hub
    //            with exn ->
    //                return ConnectionHubFailed exn.Message
    //        })
                
    //    { model with Status = "Connecting..." }, cmd

    | DisconnectHub, _ ->
        match model.Connection with
        | HubConnected hub -> 
            let cmd = 
                Cmd.ofAsyncMsg (async {
                    disconnect hub |> Async.AwaitTask |> ignore
                    return HubDisconnected 
                })
                
            { model with Status = "Disconnected"; Connection = HubDisconnected }, Cmd.none

    | ConnectedHub hub, _ ->
        { model with
            Status = "Connected"
            Connection = HubConnected hub },
        Cmd.none

    | EnterGame name, _ ->
        match model.Connection with
        | HubConnected hub -> 
            let cmd =
                Cmd.ofAsyncMsg (async {
                    try
                        do! hub.InvokeAsync("JoinGame", "game1", name) |> Async.AwaitTask
                        return GameJoined
                    with exn ->
                        return ConnectionHubFailed exn.Message
                })
            model, cmd

    | LeaveGame name, _ ->
        match model.Connection with
        | HubConnected hub ->
            let cmd =
                Cmd.ofAsyncMsg (async {
                    try
                        do! hub.InvokeAsync("QuitGame", "game1", name) |> Async.AwaitTask
                        return GameQuit
                    with exn ->
                        return ConnectionHubFailed exn.Message

                })
            model, cmd

    | MoveReceiving move, _ ->
        match model.Connection with
        | HubConnected hub -> 
            let cmd =
                Cmd.ofAsyncMsg (async {
                    try
                        do! hub.InvokeAsync("SendMove", move) |> Async.AwaitTask
                        return GameJoined
                    with exn ->
                        return ConnectionHubFailed exn.Message
                })
            { model with Moves = move :: model.Moves }, cmd

    | SendMove move, HubConnected hub ->
        let cmd =
            Cmd.ofAsyncMsg (async {
                try
                    do! hub.InvokeAsync("SendMove", move) |> Async.AwaitTask
                    return ConnectedHub hub
                with exn ->
                    return ConnectionHubFailed exn.Message
            })
        { model with Status = $"Sent: {move}" }, cmd

    | ConnectionHubFailed err, _ ->
        { model with Status = $"Connection failed: {err}" }, Cmd.none

    | _ -> model, Cmd.none
