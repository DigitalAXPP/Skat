module Messages

open SharedTypes
open Skat.Game.Domain

type DomainMsg =
    | ConnectHub
    | DisconnectHub
    
    | EnterGame of string
    | LeaveGame of string

    | GameJoined of string
    | GameLeft
    | MoveReceived of string

    | GameRoomAdded of string
    | NewGame
    | GameRoomsReceived of GameRoom list
    | NewGameEvent of roomId : string
    | CardSelected of string
    | SetParticipant of player : string
    | BidPlaced of bid : BidEventDto
    | ShareClientMsg of string

    | ConnectionHubFailed of string