namespace Skat.SignalR
#nowarn "20"
open System
open System.Collections.Generic
open System.IO
open System.Linq
open System.Threading.Tasks
open Microsoft.AspNetCore
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.HttpsPolicy
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open System.Collections.Concurrent
open Microsoft.AspNetCore.SignalR
open SharedTypes
open Skat.SignalR.Persistence.GameRoom
open Skat.SignalR.Persistence.DbInitiliaziation
open System.Text.Json.Serialization
open Transport

module GameStore =
    let games = ConcurrentDictionary<string, ResizeArray<string>>()

    let addPlayer gameId playerName =
        let players = games.GetOrAdd(gameId, fun _ -> ResizeArray())
        if not (players.Contains playerName) then
            players.Add playerName
        players

type GameHub (
    repo: IGameRoomRepository) =
    inherit Hub()

    member this.AddGameRoom () =
        task {
            let! result = repo.InsertRoom()

            do! this.Clients.All.SendAsync("ServerMsg", ServerMsgDto.NewGameRoom result)
            do! this.Clients.All.SendAsync("ReceiveMove", result |> string)
        }

    member this.GetGameRooms () =
        task {
            let! rooms = repo.GetAllRooms()
            do! this.Clients.All.SendAsync("ServerMsg", ServerMsgDto.ShareClientMessage $"Current rooms: {rooms |> List.length}")
        }
    
    member this.JoinGame (gameId: string, playerName: string) =
        task {
            do! this.Groups.AddToGroupAsync (this.Context.ConnectionId, gameId)
            let players = GameStore.addPlayer gameId playerName

            do! this.Clients.Group(gameId).SendAsync("ServerMsg", ServerMsgDto.JoinGame (List.ofSeq players))

            do! this.Clients.Group(gameId).SendAsync("PlayersUpdate", players)
        }

    member this.QuitGame (gameId: string, playerName: string) =
        task {
            do! this.Groups.RemoveFromGroupAsync (this.Context.ConnectionId, gameId)
            match GameStore.games.TryGetValue gameId with
            | true, players ->
                players.Remove playerName |> ignore
                do! this.Clients.Group(gameId).SendAsync("PlayersUpdate", players)
            | _ -> ()
        }

    member this.SendMove (move: string) =
        task {
            do! this.Clients.All.SendAsync("ServerMsg", ServerMsgDto.MoveReceiving move)

            do! this.Clients.All.SendAsync("ReceiveMove", move)
        }

    member this.ShareUpdate (msg: string) =
        task {
            do! this.Clients.All.SendAsync("ReceiveMove", msg)
        }

module Program =
    let exitCode = 0

    [<EntryPoint>]
    let main args =

        let builder = WebApplication.CreateBuilder(args)

        let dbPath = Path.Combine(builder.Environment.ContentRootPath, "game.db")
        let connectionString = $"Data Source={dbPath}"
        builder.Services
            .AddScoped<IGameRoomRepository>(fun _ ->
                GameRoomRepository (connectionString) :> IGameRoomRepository)
            .AddSignalR()
            .AddJsonProtocol(fun options ->
                options.PayloadSerializerOptions.Converters.Add(JsonFSharpConverter()))
                |> ignore

        let app = builder.Build()

        initialize(connectionString)

        app.UseHttpsRedirection()
        app.UseAuthorization()
        app.MapHub<GameHub>("/gamehub") |> ignore

        app.Run()

        exitCode
