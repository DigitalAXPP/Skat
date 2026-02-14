module SignalRClient

open Microsoft.AspNetCore.SignalR.Client
open SharedTypes
open Messages
open Transport

//type ConnectionState =
//    | HubDisconnected
//    | HubConnecting
//    | HubConnected of HubConnection

//type ServerMsg =
//    | JoinGame of string
//    | QuitGame of string
//    | MoveReceiving of string

let connect (hubUrl : string) (dispatch : ServerMsg -> unit) =
    task {
        let hub =
            HubConnectionBuilder()
                .WithUrl(hubUrl)
                .WithAutomaticReconnect()
                .Build()

        hub.On<string>("ReceiveMove", fun move ->
            printf "New move: %s" move) |> ignore

        hub.On<string seq>("PlayersUpdate", fun players ->
            printf "Players updated: %A" (players |> Seq.toList)) |> ignore

        do! hub.StartAsync()
        printf "Connected to %s" hubUrl
        return hub
    }

let disconnect (hub: HubConnection) =
    task {
        if not (isNull hub) then
            do! hub.StopAsync()
            do! hub.DisposeAsync().AsTask()
            printfn "Hub disconnected."
    }

type HubService(
    hubUrl: string,
    dispatch: DomainMsg -> unit) =

    let mutable hub: HubConnection option = None
    
    member _.IsConnected =
        hub |> Option.isSome

    member _.Connect() =
        task {
            match hub with
                | Some _ -> printfn "Already connected."
                | None ->
                    let connection = 
                        HubConnectionBuilder()
                            .WithUrl(hubUrl)
                            .WithAutomaticReconnect()
                            .Build()

                    connection.On<string>("ReceiveMove", fun move ->
                        printf "New move: %s" move) |> ignore

                    connection.On<string seq>("PlayersUpdate", fun players ->
                        printf "Players updated: %A" (players |> Seq.toList)) |> ignore

                    connection.On<ServerMsgDto>("ServerMsg", fun (dto: ServerMsgDto) ->
                        let domainMsg = Transport.toDomain dto
                        dispatch domainMsg
                    ) |> ignore

                    do! connection.StartAsync()
                    hub <- Some connection
        }

    member _.Disconnect() =
        match hub with
            | Some connection ->
                task {
                    do! connection.StopAsync()
                    do! connection.DisposeAsync().AsTask()
                    hub <- None
                    printfn "Hub disconnected."
                }
            | None -> 
                task {
                    printfn "No hub to disconnect."
                }

    member _.EnterGame(user: string) =
        task {
            match hub with
                | Some connection ->
                    do! connection.InvokeAsync("JoinGame", "game1", user)
                    printfn "Entered game as: %s" user
                | None ->
                    printfn "Not connected to hub."
        }

    member _.LeaveGame(user: string) =
        task {
            match hub with
                | Some connection ->
                    do! connection.InvokeAsync("QuitGame", "game1", user)
                    printfn "Left game as: %s" user
                | None ->
                    printfn "Not connected to hub."
        }

    member _.TellEverybody(user: string) =
        task {
            match hub with
                | Some connection ->
                    do! connection.InvokeAsync("ShareUpdate", user)
                    printfn "Told everybody: %s" user
                | None ->
                    printfn "Not connected to hub."
        }

    member _.SendMove(move: string) =
        task {
            match hub with
                | Some connection ->
                    do! connection.InvokeAsync("SendMove", move)
                    printfn "Move sent: %s" move
                | None ->
                    printfn "Not connected to hub."
        }