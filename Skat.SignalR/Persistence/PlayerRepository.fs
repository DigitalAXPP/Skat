namespace Skat.SignalR.Persistence

open System.Threading.Tasks
open Microsoft.Data.Sqlite
open Dapper

module PlayerRepository =

    type IPlayerRepository =
        abstract member GetPlayerIdByUserId : UserId : string -> Task<string option>
        
    type PlayerRepository (connectionstring) =
        interface IPlayerRepository with
            member _.GetPlayerIdByUserId(UserId : string) = task {
                use conn = new SqliteConnection(connectionstring)
                let! result = conn.QuerySingleOrDefaultAsync<string>(
                        "SELECT PlayerId FROM Player WHERE UserId = @userId",
                        {| userId = UserId |}
                    )
                return Option.ofObj result
            } 