namespace Skat.SignalR.Persistence

open Skat.Game.Domain
open System.Threading.Tasks
open Microsoft.Data.Sqlite
open Dapper

module GameRoom =

    type IGameRoomRepository =
        abstract member GetRoom : RoomId: string -> Task<GameRoom option>
        abstract member GetAllRooms : unit -> Task<GameRoom list>
        abstract member InsertRoom : unit -> Task<string>

    type GameRoomRepository (connectionstring: string) =
        let mutable rooms : Map<string, GameRoom> = Map.empty
        
        interface IGameRoomRepository with
            
            member _.GetRoom (roomId: string) = task {
                use conn = new SqliteConnection(connectionstring)
                let! result = conn.ExecuteScalarAsync<GameRoom>(
                        "SELECT COUNT(*) FROM GameRoom WHERE RoomId = @roomId",
                        {| roomId = roomId |}
                    )
                return 
                    if isNull (box(result)) then None
                    else Some result
            }

            member _.GetAllRooms () = task {
                use conn = new SqliteConnection(connectionstring)
                let! result = conn.QueryAsync<GameRoom>(
                        "SELECT * FROM GameRoom ORDER BY CurrentPlayer DESC"
                    )
                printfn $"Retrieved rooms: {result |> Seq.length}"
                return result |> Seq.toList
            }
            
            member _.InsertRoom () = task {
                use conn = new SqliteConnection(connectionstring)
                let roomId = System.Guid.NewGuid().ToString().ToUpper()
                let! result = conn.ExecuteScalarAsync<int>(
                        "INSERT INTO GameRoom (RoomId, MaxPlayer, CurrentPlayer) VALUES (@roomId, @maxPlayer, @currentPlayer)",
                        {| roomId = roomId; maxPlayer = 4; currentPlayer = 0 |}
                    )
                return roomId
            }