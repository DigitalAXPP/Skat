module Transport

open SharedTypes
open Skat.Game.Domain

type ServerMsgDto =
    | JoinGame of roomId : string
    | MoveReceiving of move : string
    | QuitGame
    | NewGameRoom of roomid : string
    | NewGame
    | GetGameRoooms of rooms : GameRoom list
    | NewGameEvent of roomid : string
    | CardSelected of card : string
    | SetParticipant of player : string
    | ShareClientMessage of msg : string
    | BidPlaced of bid : BidEventDto

let toDomainMsg serverMsg =
    match serverMsg with
    | JoinGame room ->
        Messages.GameJoined room
    | QuitGame ->
        Messages.GameLeft
    | MoveReceiving move ->
        Messages.MoveReceived move
    | NewGameRoom id ->
        Messages.GameRoomAdded id
    | NewGame ->
        Messages.NewGame
    | GetGameRoooms rooms ->
        Messages.GameRoomsReceived rooms
    | NewGameEvent roomId ->
        Messages.NewGameEvent roomId
    | CardSelected card ->
        Messages.CardSelected card
    | SetParticipant player ->
        Messages.SetParticipant player
    | ShareClientMessage msg ->
        Messages.ShareClientMsg msg
    | BidPlaced bid ->
        Messages.BidPlaced bid