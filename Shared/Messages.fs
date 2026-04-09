module Messages

open SharedTypes

type DomainMsg =
    | ConnectHub
    | DisconnectHub
    
    | EnterGame of string
    | LeaveGame of string

    | GameJoined of string list
    | GameLeft
    | MoveReceived of string

    | GameRoomAdded of int
    | ShareClientMsg of string

    | ConnectionHubFailed of string