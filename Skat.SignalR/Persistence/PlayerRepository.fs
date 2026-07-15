namespace Skat.SignalR.Persistence

open System
open System.Data
open System.Threading.Tasks
open Microsoft.Data.Sqlite
open Dapper

module PlayerRepository =

    type IPlayerRepository =
        abstract member GetPlayerIdByUserId : UserId : string * ?tx : IDbTransaction -> Task<string option>
        abstract member UpdatePlayerRoom : UserId : string * RoomId : string * ?tx : IDbTransaction -> Task<int>
        
    type PlayerRepository (connectionstring) =
        interface IPlayerRepository with
            member _.GetPlayerIdByUserId(UserId , tx) = task {
                match tx with
                | Some t ->
                    let! result = t.Connection.QueryFirstOrDefaultAsync<string>(
                        "SELECT PlayerId FROM Player WHERE UserId = @userId",
                        {| userId = UserId |},
                        transaction = t)
                    return Option.ofObj result
                | None ->
                    use conn = new SqliteConnection(connectionstring)
                    let! result = conn.QuerySingleOrDefaultAsync<string>(
                        "SELECT PlayerId FROM Player WHERE UserId = @userId",
                        {| userId = UserId |}
                    )
                    return Option.ofObj result
            }
            
            member _.UpdatePlayerRoom(UserId, RoomId, tx) = task {
                match tx with
                | Some t ->
                    let! _ = t.Connection.ExecuteAsync(
                        "UPDATE Player SET RoomId = @RoomId WHERE UserId = @UserId",
                        {| UserId = UserId ; RoomId = RoomId |},
                        transaction = t)
                    return 0
                | None ->
                    use conn = new SqliteConnection(connectionstring)
                    do! conn.OpenAsync()
                    let! _ = conn.ExecuteAsync(
                                "UPDATE Player SET RoomId = @RoomId WHERE UserId = @UserId",
                                {| UserId = UserId ; RoomId = RoomId |})
                    return 0
            }