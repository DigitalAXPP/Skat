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
open Skat.Data
open Skat.Data.Usermanagement
open Skat.Database.SkatDB
open Skat.Data.Repository
open Skat.SignalR.Persistence.GameRoom
open Skat.SignalR.Persistence.DbInitiliaziation

module GameStore =
    let games = ConcurrentDictionary<string, ResizeArray<string>>()

    let addPlayer gameId playerName =
        let players = games.GetOrAdd(gameId, fun _ -> ResizeArray())
        if not (players.Contains playerName) then
            players.Add playerName
        players

type GameHub (
    //db: IDatabase,
    repo: IGameRoomRepository) =
    inherit Hub()

    //member this.NewAccount (Name : string, Email : string, PasswordHash : string) =
    //    task {
    //        insert db { Id = 0; Name = Name; Email = Email; PasswordHash = PasswordHash } |> Async.StartImmediate
    //        do! this.Clients.All.SendAsync("ReceiveMove", $"User added: {Name}")
    //    }

    member this.AddGameRoom () =
        task {
            let! result = repo.InsertRoom()
            do! this.Clients.All.SendAsync("CreateRoom", $"New Room created: {result}")
            return result
        }
    
    member this.JoinGame (gameId: string, playerName: string) =
        task {
            do! this.Groups.AddToGroupAsync (this.Context.ConnectionId, gameId)
            let players = GameStore.addPlayer gameId playerName

            let msg = ServerMsg.JoinGame (List.ofSeq players)
            let dto = Transport.toDto msg
            do! this.Clients.Group(gameId).SendAsync("ServerMsg", dto)

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
            let msg = ServerMsg.MoveReceiving move
            let dto = Transport.toDto msg
            do! this.Clients.All.SendAsync("ServerMsg", dto)

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
            //.AddSingleton<IDatabase, Database>()
            .AddSignalR() |> ignore

        let app = builder.Build()
        //let db = app.Services.GetRequiredService<IDatabase>()
        //db.Initialize() |> Async.StartImmediate

        initialize(connectionString)

        app.UseHttpsRedirection()
        app.UseAuthorization()
        app.MapHub<GameHub>("/gamehub") |> ignore

        app.Run()

        exitCode
