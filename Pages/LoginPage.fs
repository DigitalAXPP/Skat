module LoginPage

open Fabulous.Avalonia
open type Fabulous.Avalonia.View
open Fabulous
open SharedTypes
open SignalRClient
open Fabulous.Dispatcher

type Model = { 
    Name: string
    Hub: GameHub.Model
}

type Msg =
    | ChangeName of string
    | NextGamePage
    | HubMsg of GameHub.Msg
    | ConnectHub
    | NewGame of string
    | TellAll of string

//let init (_hubService: HubService) = 
let init () =
    let hubModel, hubCmd = GameHub.init()
    { 
        Name = "Hi"
        Hub = hubModel
    }, Cmd.map HubMsg hubCmd

let update (hubService: HubService) msg model =
    match msg with
    | ChangeName n -> { model with Name = n }, Cmd.none, NoIntent
    | NextGamePage -> model, Cmd.none, GoToGamePage
    | HubMsg hubMsg ->
        // You could delegate updates to GameHub.update if you have one
        let newHub, hubCmd = GameHub.update hubService hubMsg model.Hub
        { model with Hub = newHub }, Cmd.map HubMsg hubCmd, NoIntent
    | ConnectHub ->
            let cmd =
                //Cmd.ofSub (fun dispatch ->
                //    async {
                //        // This dispatcher is called by SignalR
                //        let serverDispatcher (serverMsg: ServerMsg) =
                //            dispatch (HubMsg (GameHub.mapServerMsg serverMsg))

                //        try
                //            // Connect SignalR
                //            let! hub =
                //                connect "http://localhost:5109/gamehub" serverDispatcher
                //                |> Async.AwaitTask

                //            // Notify Elmish that the connection was successful
                //            dispatch (HubMsg (GameHub.ConnectedHub hub))
                //        with exn ->
                //            // Notify Elmish that the connection failed
                //            dispatch (HubMsg (GameHub.ConnectionHubFailed exn.Message))
                //    }
                //    |> Async.StartImmediate
                //)
                //Cmd.ofSub ( fun _ -> hubService.Connect() |> ignore)
                Cmd.ofAsyncMsg (
                    async {
                        do! hubService.Connect() |> Async.AwaitTask
                        return HubMsg GameHub.HubConnected
                    }
                )
            model, cmd, NoIntent
    | NewGame gameName ->
        let cmd =
            Cmd.ofSub (fun dispatch ->
                async {
                    dispatch (HubMsg (GameHub.EnterGame gameName))
                }
                |> Async.StartImmediate
            )
        model, cmd, NoIntent
    | TellAll message ->
        let cmd =
            Cmd.ofAsyncMsg (
                async {
                    do! hubService.TellEverybody message |> Async.AwaitTask
                    return HubMsg GameHub.HubConnected
                }
            )
            //Cmd.ofSub (fun _ ->
            //    hubService.TellEverybody message |> ignore
            //)
        model, cmd, NoIntent

let view model =
    VStack() {
        TextBlock($"Second Page: {model.Name}")
        TextBlock($"Hub status: {model.Hub.Status}")
        TextBlock($"Moves: {model.Hub.Moves}")
        Button("Connect to hub.", HubMsg (GameHub.ConnectHub))
        TextBox(model.Name, ChangeName)
        Button("Start New Game", HubMsg (GameHub.EnterGame model.Name))
        Button("Send Message to All", TellAll "Hello from client!")
        Button("Quit New Game", HubMsg (GameHub.LeaveGame model.Name))
        Button("Add move", HubMsg (GameHub.MoveReceiving "left"))
        //Button("Go to Page 3", fun _ -> dispatch (App.NavigateTo SharedTypes.GamePage))
        Button("Go to Game Page", NextGamePage)
    }
