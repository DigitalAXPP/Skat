module AuthenticationService

open Skat.Auth.Persistence.UserRepository
open Skat.Auth.Persistence.SessionRepository

let validateUsername (username : string) =
    if username.Length < 3 then
        Error "Username must be at least 3 characters long"
    else
        Ok username

let validateHash (password : string) (storedHash : string) =
    if BCrypt.Net.BCrypt.Verify(password, storedHash) then
        Ok ()
    else
        Error "Invalid password"

type AuthService
    (
        users : IUserRepository,
        sessions : ISessionRepository
    ) =

    member _.Register (username : string, password : string) = task {
            match validateUsername username with
            | Error msg -> return Error msg
            | Ok username ->
                let! exists = users.UsernameExists username

                if exists then
                    return Error "Username already exists"
                else
                    let hash = BCrypt.Net.BCrypt.HashPassword password
                    let! userId = users.CreateUser (username, hash)
                    let! token = sessions.CreateSession userId
                    return Ok token
        }

    member _.Login (username : string, password : string) = task {
            let! exists = users.GetUserByUsername username
            match exists with
            | None -> return Error "Invalid username"
            | Some user ->
                match validateHash password user.PasswordHash with
                | Error msg -> return Error msg
                | Ok () ->
                    let! token = sessions.CreateSession user.Id
                    return Ok token
        }
        