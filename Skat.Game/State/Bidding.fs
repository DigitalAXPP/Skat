namespace Skat.Game.State

open System.Collections.Concurrent
open System.Linq

module Domain =
    type PlayerId = string
    type Duel = { Bidder: string; Responder: string; CurrentValue: int }
    type BiddingState =
        | InDuel of Duel
        | Concluded of winner: string option * winningBid: int
    type SeatAssignment = { Forehand: PlayerId; Middlehand: PlayerId; Rearhand: PlayerId }
    type GameSession = { RoomId: string; Seats: SeatAssignment; Bidding: BiddingState }
    type Decision =
        | Bid of int
        | Pass
    let a = {Forehand = "Alex"; Middlehand = "Vercxa"; Rearhand = "Theo"}
    let b = {Bidder = a.Forehand; Responder = a.Middlehand; CurrentValue= 18}
open Domain

module Bidding =
    type GameSessionStore() =
        let sessions = ConcurrentDictionary<string, GameSession>()
        let seats = ConcurrentDictionary<string, string list>()
        
        member _.AddPlayer(roomId: string, player: PlayerId) =
            // seats.[roomId].Append(player)
            match seats.TryAdd(roomId, [player]) with
            | true -> true
            | false ->
                match seats.TryGetValue(roomId) with
                | true, seat -> seats.TryUpdate(roomId, seat, List.append seat [player])
                | false, _ -> false
        member _.StartSession(roomId : string, assignment : SeatAssignment, duel : Duel) =
            let session = {RoomId = roomId; Seats = assignment; Bidding = InDuel duel}
            sessions.[roomId] <- session
            sessions
            
        member _.GetSession(roomId: string) =
            match sessions.TryGetValue(roomId) with
            | true, session -> Some session
            | false, _ -> None
            
        member _.UpdateSession(roomId: string, newBid: BiddingState) =
            sessions.[roomId] <- { sessions.[roomId] with Bidding = newBid }


open Bidding

module Test =
    let c = GameSessionStore().StartSession("abcde", a, b)
    let d = GameSessionStore().GetSession("abcd")