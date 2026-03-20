namespace Skat.Auth
#nowarn "20"
open System
open System.Collections.Generic
open System.IO
open System.Linq
open System.Threading.Tasks
open Microsoft.AspNetCore
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.HttpsPolicy
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.AspNetCore.Http
open RegisterEndpoint
open AuthenticationService
open Skat.Auth.Persistence.UserRepository
open Skat.Auth.Persistence.SessionRepository
open Skat.Auth.Persistence.Infrastructure


module Program =
    let exitCode = 0

    type LoginRequest =
        { 
            username : string
            password : string 
        }



    [<EntryPoint>]
    let main args =
        DapperConfig.register()
        let builder = WebApplication.CreateBuilder(args)

        builder.Services.AddControllers()
        //builder.Services.AddScoped<IUserRepository, UserRepository>() |> ignore
        //builder.Services.AddScoped<ISessionRepository, SessionRepository>() |> ignore
        builder.Services.AddScoped<AuthService>() |> ignore

        let dbPath = Path.Combine(builder.Environment.ContentRootPath, "auth.db")
        let connectionString = $"Data Source={dbPath}"
        //let connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        builder.Services.AddScoped<IUserRepository>(fun _ ->
            UserRepository(connectionString) :> IUserRepository)
        builder.Services.AddScoped<ISessionRepository>(fun _ ->
            SessionRepository(connectionString) :> ISessionRepository)

        let app = builder.Build()

        app.UseHttpsRedirection()

        app.UseAuthorization()
        app.MapControllers()
        //app.MapPost("/login", Func<HttpContext, Task<IResult>> (fun ctx -> task {
        //    let! req = ctx.Request.ReadFromJsonAsync<LoginRequest>()

        //    match req with
        //    | r when r.username = null || r.password = null ->
        //        return Results.BadRequest()
        //    | r when r.username = "test" && r.password = "1234" -> 
        //        return Results.Ok({| token = "demo-token" |})
        //    | _ -> return Results.Unauthorized()
        //}))
        mapRegisterEndpoint app

        app.Run()

        exitCode
