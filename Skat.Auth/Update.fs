module Skat.Auth.Update

open Domain
open Fabulous

let update (model: Model) (msg: Msg) =
    match msg with
    | SetUsername username ->
        { model with Username = username }, Cmd.none
    | SetPassword password ->
        { model with Password = password }, Cmd.none
    | LoginRequested (username, password) ->
        let cmdResponse = AuthAPI.login (username, password)
        model, cmdResponse
    | LoginSuccess user ->
        { model with Auth = LoggedIn user }, Cmd.none
    | LoginError error ->
        { model with Auth = LoginFailed error }, Cmd.none
    | Logout ->
        { model with Auth = LoggedOut }, Cmd.none