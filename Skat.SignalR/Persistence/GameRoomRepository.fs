namespace Skat.SignalR.Persistence

open Skat.Game.Domain
open System.Threading.Tasks
open Microsoft.Data.Sqlite
open Dapper

module GameRoom =

    type IGameRoomRepository =
        abstract member GetRoom : RoomId: int -> Task<GameRoom option>
        abstract member InsertRoom : unit -> Task<int>

    type GameRoomRepository (connectionstring: string) =
        let mutable rooms : Map<int, GameRoom> = Map.empty
        
        interface IGameRoomRepository with
            
            member _.GetRoom (roomId: int) = task {
                use conn = new SqliteConnection(connectionstring)
                let! result = conn.ExecuteScalarAsync<GameRoom>(
                        "SELECT COUNT(*) FROM GameRoom WHERE RoomId = @roomId",
                        {| roomId = roomId |}
                    )
                return 
                    if isNull (box(result)) then None
                    else Some result
            }
            
            member _.InsertRoom () = task {
                use conn = new SqliteConnection(connectionstring)
                let! result = conn.ExecuteScalarAsync<int>(
                        "INSERT INTO GameRoom DEFAULT VALUES; SELECT last_insert_rowid()"
                    )
                return result
            }