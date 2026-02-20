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
    //| FromServer of ServerMsg

    | ConnectionHubFailed of string