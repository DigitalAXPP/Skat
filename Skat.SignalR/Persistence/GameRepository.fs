namespace Skat.SignalR.Persistence

open System.Data
open System.Threading.Tasks
open Dapper
open Microsoft.Data.Sqlite

module GameRepository =

    type IGameRepository =
        abstract member GetGameIdByRoomId : RoomId : string * ?tx : IDbTransaction -> Task<string option>
        abstract member InsertGame : GameId : string * RoomId : string * ?tx : IDbTransaction -> Task<int>
        
    type GameRepository (connectionstring) =
        interface IGameRepository with
            member _.GetGameIdByRoomId(RoomId , tx) = task {
                match tx with
                | Some t ->
                    let! result = t.Connection.QuerySingleOrDefaultAsync<string>(
                        "SELECT GameId FROM Game WHERE RoomId = @roomId",
                        {| roomId = RoomId |},
                        transaction = t)
                    return Option.ofObj result
                | None ->
                    use conn = new SqliteConnection(connectionstring)
                    let! result = conn.QuerySingleOrDefaultAsync<string>(
                            "SELECT GameId FROM Game WHERE RoomId = @roomId",
                            {| roomId = RoomId |}
                        )
                    return Option.ofObj result
            }
            
            member _.InsertGame(GameId, RoomId, tx) = task {
                match tx with
                | Some t ->
                    let! _ = t.Connection.ExecuteAsync(
                        "INSERT INTO Game (GameId, RoomId, HandNumber, Phase) VALUES (@GameId, @RoomId, @HandNumber, @Phase)",
                        {| GameId = GameId; RoomId = RoomId; HandNumber = 0; Phase = "Setup" |},
                        transaction = Option.toObj tx)
                    return 0
                | None ->
                    use conn = new SqliteConnection(connectionstring)
                    let! _ = conn.ExecuteAsync(
                            "INSERT INTO Game (GameId, RoomId, HandNumber, Phase) VALUES (@GameId, @RoomId, @HandNumber, @Phase)",
                            {| GameId = GameId; RoomId = RoomId; HandNumber = 0; Phase = "Setup" |},
                            transaction = Option.toObj tx)
                    return 0
            }