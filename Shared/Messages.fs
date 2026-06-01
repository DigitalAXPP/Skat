module Messages

open SharedTypes
open Skat.Game.Domain

type DomainMsg =
    | ConnectHub
    | DisconnectHub
    
    | EnterGame of string
    | LeaveGame of string

    | GameJoined of string list
    | GameLeft
    | MoveReceived of string

    | GameRoomAdded of string
    | GameRoomsReceived of GameRoom list
    | ShareClientMsg of string

    | ConnectionHubFailed of string