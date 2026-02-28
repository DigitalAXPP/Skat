namespace Skat

open Avalonia.Themes.Fluent
open Fabulous
open Fabulous.Avalonia

open type Fabulous.Avalonia.View
open SharedTypes
open SignalRClient

module App =
    //type Page =
    //    | HomePage
    //    | LoginPage
    //    | GamePage
    
    type Model =
        {
            CurrentPage: Page
            Home: HomePage.Model
            Login: LoginPage.Model
            Game: GamePage.Model
            HubService: HubService option
        }
    //type Model =
    //    { 
    //        Count: int
    //        Step: int
    //        TimerOn: bool
    //        CurrentPage: Page
    //    }

    type Msg =
        | NavigateToPage of Page
        | HomeMsg of HomePage.Msg
        | LoginMsg of LoginPage.Msg
        | GameMsg of GamePage.Msg
        | HubServiceMsg of HubService
        | HubConnected
        | EnterGameSucceeded
        | LeaveGameSucceeded
        | MessageSendToAllSucceeded
        | HubFailure of string
    //type Msg =
    //    | Increment
    //    | Decrement
    //    | Reset
    //    | SetStep of float
    //    | TimerToggled of bool
    //    | TimedTick
    //    | NavigateTo of Page

    //let init hubService () =
    let init () =
        let hubServiceCmd = 
            Cmd.ofSub (fun dispatch ->
                let hub =
                    HubService(
                        "http://localhost:5109/gamehub",
                        fun serverMsg ->
                            //dispatch (LoginMsg (LoginPage.HubMsg (GameHub.ServerMsg serverMsg)))
                            dispatch (
                                LoginMsg (
                                    LoginPage.HubMsg (
                                            GameHub.Domain serverMsg
                                    )
                                )
                            )                        
                    )
                dispatch (HubServiceMsg hub)
                )
            //HubService(
            //    "http://localhost:5109/gamehub",
            //    fun _ -> ()
            //)
        let loginModel, loginCmd = LoginPage.init ()
        let homeModel, homeCmd = HomePage.init
        let gameModel, gameCmd = GamePage.init
        {
            CurrentPage = PageHome
            Home = homeModel
            Login = loginModel
            Game = gameModel
            HubService = None
        }, 
            Cmd.batch [
                hubServiceCmd
                Cmd.map LoginMsg loginCmd
                Cmd.map HomeMsg homeCmd
                Cmd.map GameMsg gameCmd
            ]

    //let initModel = 
    //    {
    //        Count = 0
    //        Step = 1
    //        TimerOn = false 
    //        CurrentPage = HomePage
    //    }

    //let timerCmd () =
    //    async {
    //        do! Async.Sleep 200
    //        return TimedTick
    //    }
    //    |> Cmd.ofAsyncMsg

    //let init () = initModel, Cmd.none

    //let update hubService msg model =
    let update msg model =
        match msg with
        //match model.CurrentPage with
        //| PageHome -> HomePage.view model.Home
        //| PageLogin -> LoginPage.view model.Login
        //| PageGame -> GamePage.view model.Game
        | HubServiceMsg hub ->
            { model with HubService = Some hub }, Cmd.none
        | NavigateToPage p -> { model with CurrentPage = p }, Cmd.none

        | HomeMsg m ->
            let updated, cmd, intention = HomePage.update m model.Home
            match intention with
            | GoToLoginPage ->
                { model with CurrentPage = PageLogin; Home = updated }, Cmd.map HomeMsg cmd
            | _ ->
                { model with Home = updated }, Cmd.map HomeMsg cmd

        | LoginMsg m ->
            match m with
            | LoginPage.RequestConnection ->
                match model.HubService with
                | Some hub ->
                    let cmd =
                        Cmd.ofAsyncMsg (
                            async {
                                do! hub.Connect() |> Async.AwaitTask
                                return HubConnected
                            }
                        )
                    model, cmd
                | None ->
                    model, Cmd.none
            | _ ->
                match model.HubService with
                | Some hub ->
                    let updated, cmd, intention =
                        LoginPage.update hub m model.Login
                    match intention with
                    | GoToGamePage ->
                        { model with CurrentPage = PageGame; Login = updated }, Cmd.map LoginMsg cmd
                    | StartGameRequested name ->
                        match model.HubService with
                        | Some hub ->
                            let cmdGameRequested =
                                Cmd.ofAsyncMsg (async {
                                    try
                                        do! hub.EnterGame(name) |> Async.AwaitTask
                                        return EnterGameSucceeded
                                    with exn ->
                                        return HubFailure exn.Message
                                })
                            { model with Login = updated },
                            Cmd.batch [
                                cmdGameRequested
                                Cmd.map LoginMsg cmd
                            ]
                        | None ->
                            { model with Login = updated },
                            Cmd.none
                    | EndGameRequested name ->
                        match model.HubService with
                        | Some hub ->
                            let cmdEndGame =
                                Cmd.ofAsyncMsg (async {
                                    try
                                        do! hub.LeaveGame(name) |> Async.AwaitTask
                                        return LeaveGameSucceeded
                                    with exn ->
                                        return HubFailure exn.Message
                                })
                            { model with Login = updated },
                            Cmd.batch [
                                cmdEndGame
                                Cmd.map LoginMsg cmd
                            ]
                        | None ->
                            { model with Login = updated },
                            Cmd.none
                    | SendSelectedCard card ->
                        match model.HubService with
                        | Some hub ->
                            let cmdSendCard =
                                Cmd.ofAsyncMsg (async {
                                    try
                                        do! hub.SendMove(card) |> Async.AwaitTask
                                        return MessageSendToAllSucceeded
                                    with exn ->
                                        return HubFailure exn.Message
                                })
                            { model with Login = updated },
                            Cmd.batch [
                                cmdSendCard
                                Cmd.map LoginMsg cmd
                            ]
                        | None ->
                            { model with Login = updated },
                            Cmd.none
                    | SendMessageToAll message ->
                        match model.HubService with
                        | Some hub ->
                            let cmdSendMessage =
                                Cmd.ofAsyncMsg (async {
                                    try
                                        do! hub.TellEverybody(message) |> Async.AwaitTask
                                        return MessageSendToAllSucceeded
                                    with exn ->
                                        return HubFailure exn.Message
                                })
                            { model with Login = updated },
                            Cmd.batch [
                                cmdSendMessage
                                Cmd.map LoginMsg cmd
                            ]
                        | None ->
                            { model with Login = updated },
                            Cmd.none
                    | _ -> { model with Login = updated }, Cmd.map LoginMsg cmd
                | None ->
                    model, Cmd.none
            //let updated, cmd, intention = LoginPage.update model.HubService m model.Login
            //match intention with
            //| GoToGamePage ->
            //    { model with CurrentPage = PageGame; Login = updated }, Cmd.map LoginMsg cmd
            //| _ -> { model with Login = updated }, Cmd.map LoginMsg cmd

        | GameMsg m ->
            match model.HubService with
            | Some hub ->
                let updated, cmd, intention = 
                    GamePage.update hub m model.Game
                match intention with
                | GoToHomePage ->
                    { model with CurrentPage = PageHome; Game = updated }, Cmd.map GameMsg cmd
                | SendMessageToAll message ->
                        match model.HubService with
                        | Some hub ->
                            let cmdSendMessage =
                                Cmd.ofAsyncMsg (async {
                                    try
                                        do! hub.TellEverybody(message) |> Async.AwaitTask
                                        return MessageSendToAllSucceeded
                                    with exn ->
                                        return HubFailure exn.Message
                                })
                            { model with Game = updated },
                            Cmd.batch [
                                cmdSendMessage
                                Cmd.map GameMsg cmd
                            ]
                        | None ->
                            { model with Game = updated },
                            Cmd.none 
                | _ -> { model with Game = updated }, Cmd.map GameMsg cmd
            | None ->
                model, Cmd.none

        | HubConnected ->
            model, Cmd.none

        | EnterGameSucceeded ->
            model, Cmd.none

        | LeaveGameSucceeded ->
            model, Cmd.none

        | MessageSendToAllSucceeded ->
            model, Cmd.none

        | HubFailure errorMsg ->
            model, Cmd.none

    //let update msg model =
    //    match msg with
    //    | Increment ->
    //        { model with
    //            Count = model.Count + model.Step },
    //        Cmd.none
    //    | Decrement ->
    //        { model with
    //            Count = model.Count - model.Step },
    //        Cmd.none
    //    | Reset -> initModel, Cmd.none
    //    | SetStep n -> { model with Step = int(n + 0.5) }, Cmd.none
    //    | TimerToggled on -> { model with TimerOn = on }, (if on then timerCmd() else Cmd.none)
    //    | TimedTick ->
    //        if model.TimerOn then
    //            { model with
    //                Count = model.Count + model.Step },
    //            timerCmd()
    //        else
    //            model, Cmd.none
    //    | NavigateTo page -> { model with CurrentPage = page }, Cmd.none

    let view model = //dispatch =
        DesktopApplication(
            Window(
                match model.CurrentPage with
                //| PageHome ->
                //    HomePage.view model.Home (HomeMsg >> dispatch)

                //| PageLogin ->
                //    LoginPage.view model.Login (LoginMsg >> dispatch)

                //| PageGame ->
                //    GamePage.view model.Game (GameMsg >> dispatch)
                | PageHome -> View.map HomeMsg (HomePage.view model.Home)
                | PageLogin -> View.map LoginMsg (LoginPage.view model.HubService model.Login)
                | PageGame -> View.map GameMsg (GamePage.view model.HubService model.Game)
            )
        )
    //let view model =
    //    match model.CurrentPage with
    //    | HomePage ->
    //        VStack() {
    //            TextBlock("This is Page 1")
    //            Button("Go to Page 2", NavigateTo LoginPage)
    //        }

    //    | LoginPage ->
    //        VStack() {
    //            TextBlock("This is Page 2")
    //            Button("Back to Page 1", NavigateTo HomePage)
    //            Button("Go to Page 3", NavigateTo GamePage)
    //        }

    //    | GamePage ->
    //        VStack() {
    //            TextBlock("This is Page 3")
    //            Button("Back", NavigateTo LoginPage)
    //        }
        //(VStack() {
        //    TextBlock($"%d{model.Count}").centerText()

        //    Button("Increment", Increment).centerHorizontal()

        //    Button("Decrement", Decrement).centerHorizontal()

        //    (HStack() {
        //        TextBlock("Timer").centerVertical()

        //        ToggleSwitch(model.TimerOn, TimerToggled)
        //    })
        //        .margin(20.)
        //        .centerHorizontal()

        //    Slider(1., 10, float model.Step, SetStep)

        //    TextBlock($"Step size: %d{model.Step}").centerText()

        //    Button("Reset", Reset).centerHorizontal()

        //})
        //    .center()

    
#if MOBILE
    let app model = SingleViewApplication(view model)
#else
    //let app model dispatch = DesktopApplication(Window(view model dispatch))
#endif

    
    let theme = FluentTheme()

    let program = Program.statefulWithCmd init update view //app
    //let program hubService = 
    //            Program.statefulWithCmd
    //                //(fun () -> init hubService)
    //                //(update hubService)
    //                //view
    //                (init hubService)      // unit -> Model * Cmd<Msg>
    //                (update hubService)    // Msg -> Model -> Model * Cmd<Msg>
    //                view
