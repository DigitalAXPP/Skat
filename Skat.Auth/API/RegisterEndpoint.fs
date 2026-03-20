module RegisterEndpoint

open Microsoft.AspNetCore.Builder
open System.Threading.Tasks
open System
open Domain
open AuthenticationService
open Microsoft.AspNetCore.Http

let mapRegisterEndpoint (app : WebApplication) =
    app.MapPost(
        "/register",
        Func<RegisterRequest, AuthService, Task<IResult>>(fun req auth -> task {
            let! result = auth.Register (req.username, req.password)

            match result with
            | Error msg -> return Results.BadRequest {| error = msg |}
            | Ok token -> return Results.Ok {| token = token |}
        })
    ) |> ignore

    app.MapPost(
        "/login",
        Func<LoginRequest, AuthService, Task<IResult>>(fun req auth -> task {
            let! result = auth.Login (req.username, req.password)
            match result with
            | Error msg -> return Results.BadRequest {| error = msg |}
            | Ok token -> return Results.Ok {| token = token |}
        })
    )