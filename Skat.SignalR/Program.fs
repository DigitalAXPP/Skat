namespace Skat.SignalR
#nowarn "20"
open System
open System.Collections.Generic
open System.IO
open System.Linq
open System.Threading.Tasks
open JsonConversion
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
open Skat.Game.State.Bidding
open Skat.Game.State.Domain
open Skat.SignalR.Persistence.GameEventRepository
open Skat.SignalR.Persistence.GameParticipantRepository
open Skat.SignalR.Persistence.GameRepository
open Skat.SignalR.Persistence.GameRoom
open Skat.SignalR.Persistence.DbInitialization
open System.Text.Json
open System.Text.Json.Serialization
open Skat.SignalR.Persistence.PlayerRepository
open Skat.SignalR.Persistence.UserRepository
open Transport
open Microsoft.Data.Sqlite
open Dapper

type GameHub (
    repo: IGameRoomRepository,
    playerRepo: IPlayerRepository,
    gameRepo: IGameRepository,
    userRepo: IUserRepository,
    participantRepo: IGameParticipantRepository,
    eventRepo: IGameEventRepository,
    sessionStore: GameSessionStore) =
    inherit Hub()

    member this.AddGameRoom () =
        task {
            let dbPath = Path.Combine("/home/mint/Documents/github/Skat/Skat.SignalR", "game.db")
            let connectionString = $"Data Source={dbPath}"
            use conn = new SqliteConnection(connectionString)

            do! conn.OpenAsync()
            use tx = conn.BeginTransaction()
            let! result =
                task {
                    try 
                        let! roomId = repo.InsertRoom(tx)
                        
                        let gameId = System.Guid.NewGuid().ToString().ToUpper()
                        let! _ = gameRepo.InsertGame(gameId, roomId, tx)
                        return Ok roomId
                    with exn ->
                        return Error exn
                }
            match result with
            | Ok r ->
                tx.Commit()
                do! this.Clients.All.SendAsync("ServerMsg", ServerMsgDto.ShareClientMessage $"Room {r} created.")
            | Error err ->
                tx.Rollback()
                do! this.Clients.All.SendAsync("ServerMsg", ServerMsgDto.ShareClientMessage $"JR = {err.GetType().Name}: {err}")
            
        }

    member this.GetGameRooms () =
        task {
            let! rooms = repo.GetAllRooms()
            do! this.Clients.All.SendAsync("ServerMsg", ServerMsgDto.ShareClientMessage $"Current rooms: {rooms |> List.length}")

            do! this.Clients.All.SendAsync("ServerMsg", ServerMsgDto.GetGameRoooms rooms)
        }
    
    member this.JoinRoom (roomId: string) (user: string) =
        task {
            let dbPath = Path.Combine("/home/mint/Documents/github/Skat/Skat.SignalR", "game.db")
            let connectionString = $"Data Source={dbPath}"
            use conn = new SqliteConnection(connectionString)

            do! conn.OpenAsync()
            use tx = conn.BeginTransaction()

            let! result =
                task {
                    try
                        let! userId = userRepo.GetUserId(user, tx)
                        do! this.Groups.AddToGroupAsync(this.Context.ConnectionId, roomId)
                        sessionStore.AddPlayer(roomId, userId.Value.ToUpper())
                        
                        match sessionStore.GetPlayer(roomId) with
                        | Some players when List.length players = 3 ->
                            do! this.Clients.Group(roomId).SendAsync("ServerMsg", ServerMsgDto.ShareClientMessage $"{userId}/{players}.")
                            match sessionStore.AssignSeats(players) with
                            | Some seats ->
                                let duel = { Bidder = seats.Middlehand; Responder = seats.Forehand; CurrentValue = Some 18.0 }
                                sessionStore.StartSession(roomId, seats, duel) |> ignore
                                do! this.Clients.Group(roomId).SendAsync("ServerMsg", ServerMsgDto.ShareClientMessage $"{seats}/{duel}.")
                                do! this.Clients.Group(roomId).SendAsync("ServerMsg", ServerMsgDto.BiddingStarted (seats, InDuel (duel, MiddlehandVSForehand), roomId))
                            | None -> ()
                        | Some players when List.length players < 3 ->
                            do! this.Clients.Group(roomId).SendAsync("ServerMsg", ServerMsgDto.ShareClientMessage $"{userId}/{players}.")
                        | _ -> do! this.Clients.Group(roomId).SendAsync("ServerMsg", ServerMsgDto.ShareClientMessage $"{userId}/ empty list in {roomId}.")
                        let! _ = playerRepo.UpdatePlayerRoom(userId.Value.ToUpper(), roomId, tx)

                        let! _ = repo.IncrementPlayerCount(roomId, tx)
                        return Ok userId
                    with ex ->
                        return Error ex.Message
                }

            match result with
            | Ok r -> 
                tx.Commit()
                do! this.Groups.AddToGroupAsync (this.Context.ConnectionId, roomId)
                // do! this.Clients.Group(roomId).SendAsync("ServerMsg", ServerMsgDto.JoinGame roomId)
                do! this.Clients.Group(roomId).SendAsync("ServerMsg", ServerMsgDto.ShareClientMessage $"{user}/{r} joined room {roomId}.")
            | Error err -> 
                tx.Rollback()
                do! this.Clients.All.SendAsync("ServerMsg", ServerMsgDto.ShareClientMessage $"JR = {err.GetType().Name}: {err}")
        }

    member this.SetGameParticipant (roomId: string) (userId: string) (seatPosition: int) (role: string) =
        task {
            let dbPath = Path.Combine("/home/mint/Documents/github/Skat/Skat.SignalR", "game.db")
            let connectionString = $"Data Source={dbPath}"
            use conn = new SqliteConnection(connectionString)

            do! conn.OpenAsync()
            use tx = conn.BeginTransaction()
        
            let! result =
                task {
                    try
                        let! playerId = playerRepo.GetPlayerIdByUserId (userId, tx)
                        let! gameId = gameRepo.GetGameIdByRoomId roomId
                        let! rowCount = participantRepo.GetParticipantCount(gameId.Value.ToUpper(), tx)

                        let seat = rowCount + 1

                        let! eventAction = participantRepo.InsertParticipant(gameId.Value.ToUpper(), playerId.Value.ToUpper(),seat, tx)
                        return Ok eventAction
                    with ex ->
                        return Error ex.Message
                }

            match result with
            | Ok r -> 
                tx.Commit()
                do! this.Clients.All.SendAsync("ServerMsg", ServerMsgDto.SetParticipant userId)
                do! this.Clients.All.SendAsync("ServerMsg", ServerMsgDto.ShareClientMessage $"{userId}/{r} added to {roomId}.")
            | Error err -> 
                tx.Rollback()
                do! this.Clients.All.SendAsync("ServerMsg", ServerMsgDto.ShareClientMessage $"SGP = {err.GetType().Name}: {err}")
        }

    member this.ShareUpdate (msg: string) =
        task {
            do! this.Clients.All.SendAsync("ReceiveMove", msg)
        }

    member this.NewGameEvent (roomId: string, userId: string, eventType: string, message: string) =
        task {
            let dbPath = Path.Combine("/home/mint/Documents/github/Skat/Skat.SignalR", "game.db")
            let connectionString = $"Data Source={dbPath}"
            use conn = new SqliteConnection(connectionString)

            do! conn.OpenAsync()
            use tx = conn.BeginTransaction()
        
            let! result =
                task {
                    try
                        let! playerId = playerRepo.GetPlayerIdByUserId userId
                        let! gameId = gameRepo.GetGameIdByRoomId roomId
                        let! _ = eventRepo.NewGameEvent(gameId.Value.ToUpper(), roomId, playerId.Value.ToUpper(), eventType, message, tx)
                        
                        return Ok message
                    with ex ->
                        return Error ex.Message
                }

            match result with
            | Ok r -> 
                tx.Commit()
                match eventType with
                | "Tender" | "ACCEPT" | "BID_WON" ->
                    let bid = JsonSerializer.Deserialize<BidEventDto>(r)
                    do! this.Clients.Group(roomId).SendAsync("ServerMsg", ServerMsgDto.BidPlaced(bid))
                    do! this.Clients.Group(roomId).SendAsync("ServerMsg", ServerMsgDto.ShareClientMessage $"{message}.")
                
                | "Withdraw" ->
                    let bid = JsonSerializer.Deserialize<BidEventDto>(r)
                    do! this.Clients.Group(roomId).SendAsync("ServerMsg", ServerMsgDto.BidPassed(roomId))
                    do! this.Clients.Group(roomId).SendAsync("ServerMsg", ServerMsgDto.ShareClientMessage $"{message}.")
                    
                | "CARD_PLAYED" ->
                    let card = JsonSerializer.Deserialize<CardPlayedDto>(r)
                    do! this.Clients.Group(roomId).SendAsync("CardPlayed", card)
                
                | _ ->
                    do! this.Clients.Group(roomId).SendAsync("ServerMsg", $"Unknown event: {eventType}") 
                    do! this.Clients.All.SendAsync("ServerMsg", ServerMsgDto.ShareClientMessage $"{roomId}/{r} added to {message}.")
            | Error err -> 
                tx.Rollback()
                do! this.Clients.All.SendAsync("ServerMsg", ServerMsgDto.ShareClientMessage $"NGE = {err.GetType().Name}: {err}")
        }
        
    member this.NewDecision (roomId : string, PlayerId : string, decision : Decision) =
        task {
            match sessionStore.GetSession(roomId) with
            | Some session ->
                try
                    let newBid = step session.Seats session.Bidding decision PlayerId
                    let json =  match newBid with
                                | InDuel (d,p) -> JsonSerializer.Serialize({| Duel = d; Phase = p |})
                                | Concluded (w,b) -> JsonSerializer.Serialize({| Winner = w; WinningBid = b |})
                    sessionStore.UpdateSession(roomId, newBid)
                    do! this.Clients.Group(roomId).SendAsync("ServerMsg", ServerMsgDto.ShareClientMessage $"{json}")
                    do! this.Clients.Group(roomId).SendAsync("ServerMsg", ServerMsgDto.BidUpdate newBid)
                    do! this.Clients.Caller.SendAsync("ServerMsg", ServerMsgDto.NewEvent (roomId, PlayerId, Tender, json))
                with exn ->
                    do! this.Clients.Group(roomId).SendAsync("ServerMsg", ServerMsgDto.ShareClientMessage $"{exn}")
            | None -> ()
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
            .AddScoped<IPlayerRepository>(fun _ ->
                PlayerRepository (connectionString) :> IPlayerRepository)
            .AddScoped<IGameRepository>(fun _ ->
                GameRepository (connectionString) :> IGameRepository)
            .AddScoped<IUserRepository>(fun _ ->
                UserRepository (connectionString) :> IUserRepository)
            .AddScoped<IGameParticipantRepository>(fun _ ->
                GameParticipantRepository (connectionString) :> IGameParticipantRepository)
            .AddScoped<IGameEventRepository>(fun _ ->
                GameEventRepository (connectionString) :> IGameEventRepository)
            .AddSingleton<GameSessionStore>()
            .AddSignalR()
            .AddJsonProtocol(fun options ->
                options.PayloadSerializerOptions.Converters.Add(PhaseConverter())
                options.PayloadSerializerOptions.Converters.Add(PositionConverter())
                options.PayloadSerializerOptions.Converters.Add(JsonFSharpConverter())
                )
                // |> ignore

        let app = builder.Build()

        initialize(connectionString)

        app.UseHttpsRedirection()
        app.UseAuthorization()
        app.MapHub<GameHub>("/gamehub") |> ignore

        app.Run()

        exitCode
