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
}

type Msg =
    | TestHub
    | ConnectHub
    | ConnectedHub of HubConnection
    | DisconnectHub
    | HubSingleConnecting
    | HubConnected
    | HubDisconnected
    | EnterGame of string
    | LeaveGame of string
    | MoveReceiving of string
    | SendMove of string
    | Domain of Messages.DomainMsg
    | EnterGameSucceeded
    | EnterGameFailed of string
    | ExitGameSucceeded
    | ExitGameFailed of string

let init() = 
    {
        Status = Disconnected
        Moves = []
        Game = NotInGame
        Players = []
    }, Cmd.none    

let update (hubService: HubService) msg model =
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

    | Domain domainMsg ->
        match domainMsg with
        | Messages.ConnectHub ->
            { model with
                Status = Connected
            }, Cmd.ofMsg (ConnectHub)

        | Messages.DisconnectHub ->
            { model with
                Status = Disconnected
            }, Cmd.ofMsg (DisconnectHub)

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
        { model with Moves = move :: model.Moves }, cmd

    | _ -> model, Cmd.none
