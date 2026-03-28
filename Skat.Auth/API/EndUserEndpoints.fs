module AuthAPI

open Domain
open Fabulous

let login (username: string, password: string) =
    Cmd.ofAsyncMsg (async {
        if username = "admin" && password = "password" then
            let user = { Id = System.Guid.NewGuid(); Username = username; PasswordHash = "hashed-password" }
            return LoginSuccess user
        else
            return LoginError "Invalid username or password"
    })