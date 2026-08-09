module Transport

open SharedTypes
open Skat.Game.Domain
open Skat.Game.State.Domain

type ServerMsgDto =
    | JoinGame of roomId : string
    | MoveReceiving of move : string
    | QuitGame
    // | NewGameRoom of roomid : string
    // | NewGame
    | GetGameRoooms of rooms : GameRoom list
    | NewGameEvent of roomid : string
    | NewEvent of roomId : string * userId : string * event : EventType * message : string
    | CardSelected of card : string
    | SetParticipant of player : string
    | ShareClientMessage of msg : string
    | BidPlaced of bid : BidEventDto
    | BiddingStarted of seats: SeatAssignment * duel: Duel * roomId : string
    | BidUpdate of BiddingState
    | BidPassed of roomId : string

let toDomainMsg serverMsg =
    match serverMsg with
    | JoinGame room ->
        Messages.GameJoined room
    | QuitGame ->
        Messages.GameLeft
    | MoveReceiving move ->
        Messages.MoveReceived move
    // | NewGameRoom id ->
    //     Messages.GameRoomAdded id
    // | NewGame ->
    //     Messages.NewGame
    | GetGameRoooms rooms ->
        Messages.GameRoomsReceived rooms
    | NewGameEvent roomId ->
        Messages.NewGameEvent roomId
    | NewEvent (roomId, userId, event, message) ->
        Messages.NewEvent (roomId, userId, event, message)
    | CardSelected card ->
        Messages.CardSelected card
    | SetParticipant player ->
        Messages.SetParticipant player
    | ShareClientMessage msg ->
        Messages.ShareClientMsg msg
    | BidPlaced bid ->
        Messages.BidPlaced bid
    | BidPassed roomId ->
        Messages.BidPassed roomId
    | BiddingStarted (seats, duel, roomId)->
        Messages.StartBidding (seats, duel, roomId)
    | BidUpdate (state) ->
        Messages.BiddingUpdate (state)