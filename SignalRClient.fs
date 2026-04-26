module SignalRClient

open Microsoft.AspNetCore.SignalR.Client
open Microsoft.AspNetCore.SignalR.Protocol
open Microsoft.Extensions.DependencyInjection
open System.Text.Json.Serialization
open SharedTypes
open Messages
open Transport

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
                            .AddJsonProtocol(fun options ->
                                options.PayloadSerializerOptions.Converters.Add(JsonFSharpConverter()))
                            .Build()

                    connection.On<string>("ReceiveMove", fun move ->
                        printf "New move: %s" move) |> ignore

                    connection.On<ServerMsgDto>(HubMethods.ServerMsg, fun (dto: ServerMsgDto) ->
                        let domainMsg = Transport.toDomainMsg dto
                        dispatch domainMsg
                        printf "Received ServerMsg: %s" (dto |> string)
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
                    printfn "Entered Game1 as: %s" user
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

    member _.CreateRoom() =
        task {
            match hub with
                | Some connection ->
                    do! connection.InvokeAsync("AddGameRoom")
                    printfn "Room added."
                | None ->
                    printfn "Not connected to hub."
        }

    member _.GetRooms() =
        task {
            match hub with
                | Some connection ->
                    do! connection.InvokeAsync("GetGameRooms")
                    printfn "Requested game rooms."
                | None ->
                    printfn "Not connected to hub."
        }