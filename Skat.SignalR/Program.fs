namespace Skat.SignalR
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
open System.Collections.Concurrent
open Microsoft.AspNetCore.SignalR
open SharedTypes

module GameStore =
    let games = ConcurrentDictionary<string, ResizeArray<string>>()

    let addPlayer gameId playerName =
        let players = games.GetOrAdd(gameId, fun _ -> ResizeArray())
        if not (players.Contains playerName) then
            players.Add playerName
        players

type GameHub() =
    inherit Hub()

    member this.JoinGame (gameId: string, playerName: string) =
        task {
            do! this.Groups.AddToGroupAsync (this.Context.ConnectionId, gameId)
            let players = GameStore.addPlayer gameId playerName

            let msg = ServerMsg.JoinGame (List.ofSeq players)
            let dto = Transport.toDto msg
            do! this.Clients.Group(gameId).SendAsync("ServerMsg", dto)

            do! this.Clients.Group(gameId).SendAsync("PlayersUpdate", players)
        }

    member this.QuitGame (gameId: string, playerName: string) =
        task {
            do! this.Groups.RemoveFromGroupAsync (this.Context.ConnectionId, gameId)
            match GameStore.games.TryGetValue gameId with
            | true, players ->
                players.Remove playerName |> ignore
                do! this.Clients.Group(gameId).SendAsync("PlayersUpdate", players)
            | _ -> ()
        }

    member this.SendMove (move: string) =
        task {
            do! this.Clients.All.SendAsync("ReceiveMove", move)
        }

    member this.ShareUpdate (msg: string) =
        task {
            do! this.Clients.All.SendAsync("ReceiveMove", msg)
        }

module Program =
    let exitCode = 0

    [<EntryPoint>]
    let main args =

        let builder = WebApplication.CreateBuilder(args)

        builder.Services
            .AddSignalR() |> ignore

        let app = builder.Build()

        app.UseHttpsRedirection()

        app.UseAuthorization()
        app.MapHub<GameHub>("/gamehub") |> ignore

        app.Run()

        exitCode
