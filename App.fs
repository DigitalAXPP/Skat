namespace Skat

open Avalonia.Themes.Fluent
open Fabulous
open Fabulous.Avalonia

open type Fabulous.Avalonia.View
open SharedTypes
open SignalRClient
open Domain

module App =

    type authState =
        | NotAuthenticated
        | Authenticating
        | Authenticated of User

    let canAccess page authState =
        match page, authState with
        | PageHome, _ -> true
        | PageLogin, _ -> true
        | _, Authenticated _ -> true
        | _, NotAuthenticated -> false
        | _, Authenticating -> false

    
    type Model =
        {
            CurrentPage: Page
            CurrentUser: string option
            AuthenticatedUser: User option
            Auth: authState
            Status: GameState
            Players: string list
            Moves: string list
            AuthModel: Domain.Model
            Home: HomePage.Model
            Login: LoginPage.Model
            Game: GamePage.Model
            HubService: HubService option
        }

    type Msg =
        | NavigateToPage of Page
        | HomeMsg of HomePage.Msg
        | LoginMsg of LoginPage.Msg
        | GameMsg of GamePage.Msg
        | HubServiceMsg of HubService
        | DomainMsg of Messages.DomainMsg
        | AuthMsg of Domain.Msg
        | HubConnected
        | SendJoingame of string
        | JoinGameConfirmed
        | EnterGameSucceeded
        | LeaveGameSucceeded
        | MessageSendToAllSucceeded
        | HubFailure of string

    let init () =
        let hubServiceCmd = 
            Cmd.ofSub (fun dispatch ->
                let hub =
                    HubService(
                        "http://localhost:5109/gamehub",
                        fun domainMsg ->
                            dispatch (DomainMsg domainMsg)                        
                    )
                dispatch (HubServiceMsg hub)
                )

        let loginModel, loginCmd = LoginPage.init ()
        let homeModel, homeCmd = HomePage.init
        let gameModel, gameCmd = GamePage.init
        let authModel = Domain.init
        {
            CurrentPage = PageHome
            CurrentUser = None
            AuthenticatedUser = None
            Auth = NotAuthenticated
            Status = NotInGame
            Players = []
            Moves = []
            AuthModel = authModel
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

    let navigateToPage model page authState =
        match canAccess page authState with
        | true -> { model with CurrentPage = page }
        | false -> { model with CurrentPage = PageLogin }

    let update msg model =
        match msg with
        | AuthMsg (LoginSuccess user) ->
            { model with
                AuthenticatedUser = Some user
                Auth = Authenticated user
            }, Cmd.none
        | AuthMsg authMsg ->
            let updatedAuth, cmd = Skat.Auth.Update.update model.AuthModel authMsg
            { model with AuthModel = updatedAuth }, Cmd.map AuthMsg cmd
        | HubServiceMsg hub ->
            { model with HubService = Some hub }, Cmd.none
        | NavigateToPage p -> 
            let newModel = navigateToPage model p model.Auth
            newModel, Cmd.none

        | HomeMsg m ->
            let updated, cmd, intention = HomePage.update m model.Home
            match intention with
            | NavigateTo page ->
                let newModel = 
                    model
                    |> fun m -> { m with Home = updated }
                    |> fun m -> navigateToPage m page m.Auth
                newModel, Cmd.map HomeMsg cmd
            | ForwardUsernameToAuth username ->
                let cmdAuth = Cmd.ofMsg (AuthMsg (Domain.SetUsername username))
                { model with Home = updated },
                Cmd.batch [
                    cmdAuth
                    Cmd.map HomeMsg cmd
                ]
            | ForwardPasswordToAuth password ->
                let cmdAuth = Cmd.ofMsg (AuthMsg (Domain.SetPassword password))
                { model with Home = updated },
                Cmd.batch [
                    cmdAuth
                    Cmd.map HomeMsg cmd
                ]
            | LoginToAuth (username, password) ->
                let cmdAuth = Cmd.ofMsg (AuthMsg (Domain.LoginRequested (username, password)))
                { model with Home = updated },
                Cmd.batch [
                    cmdAuth
                    Cmd.map HomeMsg cmd
                ]
            | _ ->
                { model with Home = updated }, Cmd.map HomeMsg cmd

        | LoginMsg m ->
            match m with
            | LoginPage.RequestConnection name ->
                match model.HubService with
                | Some hub ->
                    let cmd =
                        Cmd.ofAsyncMsg (
                            async {
                                do! hub.Connect() |> Async.AwaitTask
                                return HubConnected
                            }
                        )
                    { model with CurrentUser = Some name }, cmd
                | None ->
                    model, Cmd.none
            | _ ->
                match model.HubService with
                | Some _ ->
                    let updated, cmd, intention =
                        LoginPage.update m model.Login
                    match intention with
                    | NavigateTo page ->
                        let newModel =
                            model
                            |> fun m -> { m with Login = updated }
                            |> fun m -> navigateToPage m page model.Auth
                        newModel, Cmd.map LoginMsg cmd
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
                    | NewRoom ->
                        match model.HubService with
                        | Some hub ->
                            let cmdNewRoom =
                                Cmd.ofAsyncMsg (async {
                                    try
                                        do! hub.CreateRoom() |> Async.AwaitTask
                                        return EnterGameSucceeded
                                    with exn ->
                                        return HubFailure exn.Message
                                })
                            { model with Login = updated },
                            Cmd.batch [
                                cmdNewRoom
                                Cmd.map LoginMsg cmd
                            ]
                    | _ -> { model with Login = updated }, Cmd.map LoginMsg cmd
                | None ->
                    model, Cmd.none

        | GameMsg m ->
            match model.HubService with
            | Some _ ->
                let updated, cmd, intention = 
                    GamePage.update m model.Game
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
                | AppendCard card ->
                    match model.HubService with
                    | Some hub ->
                        let cmdAppendCard =
                            Cmd.ofAsyncMsg (async {
                                try
                                    do! hub.SendMove(card) |> Async.AwaitTask
                                    return MessageSendToAllSucceeded
                                with exn ->
                                    return HubFailure exn.Message
                            })
                        { model with Game = updated },
                        Cmd.batch [
                            cmdAppendCard
                            Cmd.map GameMsg cmd
                        ]
                    | None ->
                        { model with Game = updated },
                        Cmd.none
                | _ -> { model with Game = updated }, Cmd.map GameMsg cmd
            | None ->
                model, Cmd.none

        | HubConnected ->
            match model.CurrentUser with
            | Some name ->
                model, Cmd.ofMsg (SendJoingame name)
            | None ->
                model, Cmd.none

        | SendJoingame name ->
            match model.HubService with
            | Some hub ->
                let cmdJoinGame =
                    Cmd.ofAsyncMsg (async {
                        try
                            do! hub.EnterGame(name) |> Async.AwaitTask
                            return JoinGameConfirmed
                        with exn ->
                            return HubFailure exn.Message
                    })
                model, cmdJoinGame
            | None ->
                model, Cmd.none

        | JoinGameConfirmed ->
            { model with Status = InLobby }, Cmd.none

        | DomainMsg domainMsg ->
            match domainMsg with
            | Messages.GameJoined players ->
                { model with Status = InLobby; Players = players }, Cmd.none
            | Messages.GameLeft ->
                { model with Status = NotInGame; Players = [] }, Cmd.none
            | Messages.MoveReceived move ->
                // Here you would update the game state based on the received move
                { model with Moves = move :: model.Moves}, Cmd.none
            | Messages.GameRoomAdded id ->
                // Handle new game room added if needed
                model, Cmd.none

        | EnterGameSucceeded ->
            model, Cmd.none

        | LeaveGameSucceeded ->
            model, Cmd.none

        | MessageSendToAllSucceeded ->
            model, Cmd.none

        | HubFailure errorMsg ->
            model, Cmd.none

    let view model =
        DesktopApplication(
            Window(
                match model.CurrentPage with
                | PageHome -> View.map HomeMsg (HomePage.view model.Home)
                | PageLogin -> View.map LoginMsg (LoginPage.view model.HubService model.Login)
                | PageGame -> View.map GameMsg (GamePage.view model.HubService model.Game)
            )
        )
    
#if MOBILE
    let app model = SingleViewApplication(view model)
#else
    //let app model dispatch = DesktopApplication(Window(view model dispatch))
#endif

    
    let theme = FluentTheme()

    let program = Program.statefulWithCmd init update view
