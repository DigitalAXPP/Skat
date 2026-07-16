namespace Skat.SignalR.Persistence

open System.Data
open System.Threading.Tasks
open Dapper
open Microsoft.AspNetCore.Mvc.Diagnostics
open Microsoft.Data.Sqlite
open SharedTypes
open Skat.Game.Types

module GameEventRepository =
    type IGameEventRepository =
        abstract member NewGameEvent : GameId : string * RoomId : string * PlayerId : string * EventType : string * EventDate : string * ?tx : IDbTransaction -> Task<int>
        
    type GameEventRepository(connectionstring : string) =
        interface IGameEventRepository with
            member _.NewGameEvent(GameId, RoomId, PlayerId, EventType, EventData, tx) = task {
                let eventId = System.Guid.NewGuid().ToString().ToUpper()
                match tx with
                | Some t ->
                    let! result = t.Connection.ExecuteAsync(
                            """INSERT INTO GameEvent (EventId, GameId, RoomId, PlayerId, EventType, EventData, Sequence)
                                VALUES (@EventId, @GameId, @RoomId, @PlayerId, @EventType, @EventData, (SELECT COALESCE(MAX(Sequence), 0) + 1 FROM GameEvent WHERE GameId = @GameId))""",
                            {| EventId = eventId; GameId = GameId; RoomId = RoomId; PlayerId = PlayerId; EventType = EventType; EventData = EventData |},
                            transaction = t)
                    return result
                | None ->
                    use conn = new SqliteConnection(connectionstring)
                    do! conn.OpenAsync()
                    let! result = conn.ExecuteAsync(
                            """INSERT INTO GameEvent (EventId, GameId, RoomId, PlayerId, EventType, EventData, Sequence)
                                VALUES (@EventId, @GameId, @RoomId, @PlayerId, @EventType, @EventData, (SELECT COALESCE(MAX(Sequence), 0) + 1 FROM GameEvent WHERE GameId = @GameId))""",
                            {| EventId = eventId; GameId = GameId; RoomId = RoomId; PlayerId = PlayerId; EventType = EventType; EventData = EventData |})
                    return result
            }
