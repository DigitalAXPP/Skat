namespace Skat.SignalR.Persistence

open System.Data
open System.Threading.Tasks
open Microsoft.Data.Sqlite
open Dapper

module GameParticipantRepository =
    type IGameParticipantRepository =
        abstract member GetParticipantCount : GameId : string * ?tx : IDbTransaction -> Task<int>
        abstract member InsertParticipant : GameId : string *  PlayerId : string * Seat : int * ?tx : IDbTransaction -> Task<int>
        
    type GameParticipantRepository (connectionstring)=
        interface IGameParticipantRepository with
            member _.GetParticipantCount(GameId : string, tx : IDbTransaction option) = task {
                match tx with
                | Some t ->
                    let! result = t.Connection.ExecuteScalarAsync<int>(
                            """SELECT count(*) FROM GameParticipant
                                WHERE GameId = @GameId""",
                            {| GameId = GameId |},
                            transaction = t)
                    return result
                | None ->
                    use conn = new SqliteConnection(connectionstring)
                    let! result = conn.ExecuteAsync(
                            """SELECT count(*) FROM GameParticipant
                                WHERE GameId = @GameId""",
                            {| GameId = GameId |})
                    return result
            }
            
            member _.InsertParticipant(GameId, PlayerId, Seat, tx) = task {
                match tx with
                | Some t ->
                    let! result = t.Connection.ExecuteAsync(
                            """INSERT INTO GameParticipant (ParticipantId, GameId, PlayerId, SeatPosition, Role)
                                VALUES (@ParticipantId, @GameId, @PlayerId, @SeatPosition, @Role)""",
                            {| ParticipantId = System.Guid.NewGuid().ToString().ToUpper(); GameId = GameId; PlayerId = PlayerId; SeatPosition = Seat; Role = "role" |},
                            transaction = t)
                    return result
                | None ->
                    use conn = new SqliteConnection(connectionstring)
                    let! result = conn.ExecuteAsync(
                            """INSERT INTO GameParticipant (ParticipantId, GameId, PlayerId, SeatPosition, Role)
                                VALUES (@ParticipantId, @GameId, @PlayerId, @SeatPosition, @Role)""",
                            {| ParticipantId = System.Guid.NewGuid().ToString().ToUpper(); GameId = GameId; PlayerId = PlayerId; SeatPosition = Seat; Role = "role" |})
                    return result
            }