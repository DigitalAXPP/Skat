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
open Microsoft.Data.Sqlite
open Dapper

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

            do! this.Clients.All.SendAsync("ServerMsg", ServerMsgDto.GetGameRoooms rooms)
        }
    
    //member this.JoinGame (gameId: string, playerName: string) =
    member this.JoinRoom (roomId: string) (user: string) =
        task {
            let dbPath = Path.Combine("C:\\Users\\apiep\\Documents\\github\\Skat\\Skat.SignalR", "game.db")
            let connectionString = $"Data Source={dbPath}"
            use conn = new SqliteConnection(connectionString)

            do! conn.OpenAsync()
            use tx = conn.BeginTransaction()

            let! result =
                task {
                    try
                        let! userId = conn.QuerySingleAsync<string>(
                            "SELECT id FROM Users WHERE username = @username",
                            {| username = user |}
                        )

                        do! conn.ExecuteAsync(
                            "UPDATE Player SET RoomId = @RoomId WHERE UserId = @UserId",
                                {| RoomId = roomId; UserId = userId |},
                                transaction = tx) :> Task

                        do! conn.ExecuteAsync(
                            """UPDATE GameRoom 
                                SET CurrentPlayer = CurrentPlayer + 1
                                WHERE RoomId = @RoomId AND CurrentPlayer < MaxPlayer""",
                            {| RoomId = roomId |},
                            transaction = tx) :> Task
                        return Ok userId
                    with ex ->
                        return Error ex.Message
                }

            match result with
            | Ok r -> 
                tx.Commit()
                do! this.Clients.All.SendAsync("ServerMsg", ServerMsgDto.JoinGame ["user"; "user2"])
                do! this.Clients.All.SendAsync("ServerMsg", ServerMsgDto.ShareClientMessage $"{user}/{r} joined room {roomId}.")
            | Error err -> 
                tx.Rollback()
                do! this.Clients.All.SendAsync("ServerMsg", ServerMsgDto.ShareClientMessage $"{err.GetType().Name}: {err}")
            //do! this.Groups.AddToGroupAsync (this.Context.ConnectionId, gameId)
            //let players = GameStore.addPlayer gameId playerName

            //do! this.Clients.Group(gameId).SendAsync("ServerMsg", ServerMsgDto.JoinGame (List.ofSeq players))
            //do! this.Clients.All.SendAsync("ServerMsg", ServerMsgDto.JoinGame [user])
            do! this.Clients.All.SendAsync("ServerMsg", ServerMsgDto.ShareClientMessage $"{user} joined room {roomId}.")
            //do! this.Clients.Group(gameId).SendAsync("PlayersUpdate", players)
        }

    member this.AddGameEvent (roomId: string) (userId: string)  =
        task {
            let dbPath = Path.Combine("C:\\Users\\apiep\\Documents\\github\\Skat\\Skat.SignalR", "game.db")
            let connectionString = $"Data Source={dbPath}"
            use conn = new SqliteConnection(connectionString)

            do! conn.OpenAsync()
            use tx = conn.BeginTransaction()

            let! result =
                task {
                    try
                        let gameId = System.Guid.NewGuid().ToString().ToUpper()
                        let! player = conn.QuerySingleAsync<string>(
                            "SELECT PlayerId FROM Player WHERE UserId = @userId",
                            {| userId = userId |}
                            )
                        let! gameEventId = conn.ExecuteAsync(
                            "INSERT INTO GameEvent (GameId, RoomId, PlayerId) VALUES (@GameId, @RoomId, @PlayerId)",
                            {| GameId = gameId; RoomId = roomId; PlayerId = player |},
                            transaction = tx)

                        return Ok gameEventId
                    with ex ->
                        return Error ex.Message
                }

            match result with
            | Ok r -> 
                tx.Commit()
                do! this.Clients.All.SendAsync("ServerMsg", ServerMsgDto.ShareClientMessage $"{r}/{roomId} created for {userId}.")
            | Error err -> 
                tx.Rollback()
                do! this.Clients.All.SendAsync("ServerMsg", ServerMsgDto.ShareClientMessage $"{err.GetType().Name}: {err}")
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

    member this.SendMove (move: string, userId: string) =
        task {
            let dbPath = Path.Combine("C:\\Users\\apiep\\Documents\\github\\Skat\\Skat.SignalR", "game.db")
            let connectionString = $"Data Source={dbPath}"
            use conn = new SqliteConnection(connectionString)

            do! conn.OpenAsync()
            use tx = conn.BeginTransaction()

            let! result =
                task {
                    try
                        let! roomId = conn.QuerySingleAsync<int>(
                            "SELECT RoomId FROM Player WHERE UserId = @userid",
                            {| userid = userId |}
                        )

                        //do! conn.ExecuteAsync(
                        //    "UPDATE Player SET RoomId = @RoomId WHERE UserId = @UserId",
                        //        {| RoomId = gameId; UserId = userId |},
                        //        transaction = tx) :> Task
                        return Ok roomId
                    with ex ->
                        return Error ex.Message
                }

            match result with
            | Ok r -> 
                tx.Commit()
                do! this.Clients.All.SendAsync("ServerMsg", ServerMsgDto.JoinGame ["user"; "user2"])
                do! this.Clients.All.SendAsync("ServerMsg", ServerMsgDto.ShareClientMessage $"{userId}/{r} joined room {r}.")
            | Error err -> 
                tx.Rollback()
                do! this.Clients.All.SendAsync("ServerMsg", ServerMsgDto.ShareClientMessage $"{err.GetType().Name}: {err}")

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
