namespace Skat

open Avalonia.Themes.Fluent
open Fabulous
open Fabulous.Avalonia

open type Fabulous.Avalonia.View
open SharedTypes

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
    //type Msg =
    //    | Increment
    //    | Decrement
    //    | Reset
    //    | SetStep of float
    //    | TimerToggled of bool
    //    | TimedTick
    //    | NavigateTo of Page

    let init () =
        {
            CurrentPage = PageHome
            Home = HomePage.init
            Login = LoginPage.init
            Game = GamePage.init}, Cmd.none

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

    let update msg model =
        match msg with
        //match model.CurrentPage with
        //| PageHome -> HomePage.view model.Home
        //| PageLogin -> LoginPage.view model.Login
        //| PageGame -> GamePage.view model.Game
        | NavigateToPage p -> { model with CurrentPage = p }, Cmd.none

        | HomeMsg m ->
            let updated, cmd, intention = HomePage.update m model.Home
            match intention with
            | GoToLoginPage ->
                { model with CurrentPage = PageLogin; Home = updated }, cmd
            | _ ->
                { model with Home = updated }, cmd

        | LoginMsg m ->
            let updated, cmd, intention = LoginPage.update m model.Login
            match intention with
            | GoToGamePage ->
                { model with CurrentPage = PageGame; Login = updated }, cmd
            | _ -> { model with Login = updated }, cmd

        | GameMsg m ->
            let updated, cmd, intention = GamePage.update m model.Game
            match intention with
            | GoToHomePage ->
                { model with CurrentPage = PageHome; Game = updated }, cmd
            | _ -> { model with Game = updated }, cmd

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
                | PageLogin -> View.map LoginMsg (LoginPage.view model.Login)
                | PageGame -> View.map GameMsg (GamePage.view model.Game)
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
