module Messages

open SharedTypes

type DomainMsg =
    | EnterGame of string
    | LeaveGame of string
    | FromServer of ServerMsg
    | ConnectionHubFailed of string