module Transport

open SharedTypes

type ServerMsgDto =
    | JoinGame of players : string list
    | MoveReceiving of move : string
    | QuitGame
    | NewGameRoom of roomid : int
    | ShareClientMessage of msg : string

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