namespace Skat.Auth.Persistence

open System
open System.Threading.Tasks
open Microsoft.Data.Sqlite
open Dapper
open Domain


module UserRepository =


    type IUserRepository =
        abstract member CreateUser : string * string -> Task<Guid>
        abstract member UsernameExists : string -> Task<bool>
        abstract member GetUserByUsername : string -> Task<User option>

    type UserRepository (connectionString : string) =
    
        interface IUserRepository with
            member _.UsernameExists (username: string) = task {
                use conn = new SqliteConnection(connectionString)

                let! result = conn.ExecuteScalarAsync<int>(
                        "SELECT COUNT(*) FROM users WHERE username = @Username",
                        {| Username = username |}
                    )
                return result > 0
            }

            member _.CreateUser (username: string, passwordHash: string) = task {
                use conn = new SqliteConnection(connectionString)
                let userId = Guid.NewGuid()
                let! _ = conn.ExecuteAsync(
                        "INSERT INTO users (id, username, password_hash) VALUES (@Id, @Username, @PasswordHash)",
                        {| Id = userId; Username = username; PasswordHash = passwordHash |}
                    )
                return userId
            }

            member _.GetUserByUsername (username: string) = task {
                use conn = new SqliteConnection(connectionString)
                let! user = conn.QuerySingleOrDefaultAsync<User>(
                        "SELECT id AS Id, username AS Username, password_hash AS PasswordHash FROM users WHERE username = @Username",
                        {| Username = username |}
                    )
                return
                    if isNull (box user) then None
                    else Some user
            }