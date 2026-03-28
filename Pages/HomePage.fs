module HomePage

open Fabulous.Avalonia
open type Fabulous.Avalonia.View
open Fabulous
open SharedTypes

type Model = 
    { 
        Something: int
        Username: string
        Password: string
    }

type Msg =
    | DoSomething
    | NextLoginPage
    | AssignUsername of string
    | AssignPassword of string
    | Authenticate

let init = 
    { 
        Something = 0
        Username = ""
        Password = ""
    }, Cmd.none

let update msg model =
    match msg with
    | DoSomething -> { model with Something = model.Something + 1 }, Cmd.none, NoIntent
    | NextLoginPage -> model, Cmd.none, NavigateTo PageLogin
    | AssignUsername username -> { model with Username = username }, Cmd.none, ForwardUsernameToAuth username
    | AssignPassword password -> { model with Password = password }, Cmd.none, ForwardPasswordToAuth password
    | Authenticate -> model, Cmd.none, LoginToAuth (model.Username, model.Password)

let view model =
    VStack() {
        TextBlock($"First Page: %d{model.Something}")
        
        Button("Do something", DoSomething)
        Button("Go to Page 2", NextLoginPage)
        
        TextBox(model.Username, AssignUsername)
        TextBox(model.Password, AssignPassword)
        Button("Login", Authenticate)
    }