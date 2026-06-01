module Transport

open SharedTypes
open Skat.Game.Domain

type ServerMsgDto =
    | JoinGame of players : string list
    | MoveReceiving of move : string
    | QuitGame
    | NewGameRoom of roomid : string
    | GetGameRoooms of rooms : GameRoom list
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
    | GetGameRoooms rooms ->
        Messages.GameRoomsReceived rooms
    | ShareClientMessage msg ->
        Messages.ShareClientMsg msg