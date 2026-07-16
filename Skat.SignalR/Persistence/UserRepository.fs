namespace Skat.SignalR.Persistence

open System.Data
open Microsoft.Data.Sqlite
open System.Threading.Tasks
open Dapper
open Domain

module UserRepository =
    type IUserRepository =
        abstract member GetUserId : UserId : string * ?tx : IDbTransaction -> Task<string option>

    type UserRepository (connectionstring : string) =
        interface IUserRepository with
        
            member _.GetUserId(UserID, tx) = task {
                match tx with
                | Some t ->
                    let! result = t.Connection.QueryFirstOrDefaultAsync<string>(
                            "SELECT id FROM Users WHERE username = @username",
                            {| username = UserID |},
                            transaction = t)
                    return Option.ofObj result
                | None ->
                    use conn = new SqliteConnection(connectionstring)
                    let! result = conn.QuerySingleAsync<string>(
                            "SELECT id FROM Users WHERE username = @username",
                            {| username = UserID |}
                        )
                    return Option.ofObj result
            }