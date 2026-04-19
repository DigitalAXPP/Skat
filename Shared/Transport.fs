module Transport

open SharedTypes

//type ServerMsgDto =
//    {
//        Type: string
//        Players: string list option
//        Move: string option
//        RoomId: int option
//    }
type ServerMsgDto =
    | JoinGame of players : string list
    | MoveReceiving of move : string
    | QuitGame
    | NewGameRoom of roomid : int
    | ShareClientMessage of msg : string

//let toDto (msg: ServerMsg) =
//    match msg with
//    | JoinGame players ->
//        { Type = "JoinGame"
//          Players = Some players
//          Move = None 
//          RoomId = None}

//    | MoveReceiving move ->
//        { Type = "MoveReceiving"
//          Players = None
//          Move = Some move
//          RoomId = None}

//    | QuitGame ->
//        { Type = "QuitGame"
//          Players = None
//          Move = None
//          RoomId = None }

//    | NewGameRoom id ->
//        { Type = "NewGameRoom"
//          Players = None
//          Move = None
//          RoomId = Some id }

//let fromDto (dto: ServerMsgDto) =
//    match dto.Type with
//    | "JoinGame" ->
//        JoinGame (dto.Players |> Option.defaultValue [])
//    | "MoveReceiving" ->
//        SharedTypes.MoveReceiving (dto.Move |> Option.defaultValue "")
//    | "QuitGame" ->
//        QuitGame
//    | "NewGameRoom" ->
//        NewGameRoom (dto.RoomId |> Option.defaultValue 0)
//    | t ->
//        failwith $"Unknown ServerMsg type: {t}"

let toDomainMsg serverMsg =
    match serverMsg with
    | JoinGame players ->
        Messages.GameJoined players
    | QuitGame ->
        Messages.GameLeft
    | MoveReceiving move ->
        Messages.MoveReceived move
    | NewGameRoom id ->
        Messages.GameRoomAdded id
    | ShareClientMessage msg ->
        Messages.ShareClientMsg msg

//let toDomain (dto: ServerMsgDto) =
//    dto
//    |> fromDto
//    |> toDomainMsg