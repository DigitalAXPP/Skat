module Transport

open SharedTypes

type ServerMsgDto =
    {
        Type: string
        Players: string list option
        Move: string option
    }

let toDto (msg: ServerMsg) =
    match msg with
    | JoinGame players ->
        { Type = "JoinGame"
          Players = Some players
          Move = None }

    | MoveReceiving move ->
        { Type = "MoveReceiving"
          Players = None
          Move = Some move }

    | QuitGame ->
        { Type = "QuitGame"
          Players = None
          Move = None }

let fromDto (dto: ServerMsgDto) =
    match dto.Type with
    | "JoinGame" ->
        JoinGame (dto.Players |> Option.defaultValue [])
    | "MoveReceiving" ->
        SharedTypes.MoveReceiving (dto.Move |> Option.defaultValue "")
    | "QuitGame" ->
        QuitGame
    | t ->
        failwith $"Unknown ServerMsg type: {t}"