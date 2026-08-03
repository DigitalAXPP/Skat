namespace Skat.Game.State

open System.Collections.Concurrent
open System.Linq

module Domain =
    type PlayerId = string
    type Duel = { Bidder: string; Responder: string; CurrentValue: float option }
    type BiddingState =
        | InDuel of Duel
        | Concluded of winner: string option * winningBid: int
    type SeatAssignment = { Forehand: PlayerId; Middlehand: PlayerId; Rearhand: PlayerId }
    type GameSession = { RoomId: string; Seats: SeatAssignment; Bidding: BiddingState }
    type Decision =
        | Bid of int
        | Pass

open Domain

module Bidding =
    
    // Verifying if the bid is a valid Skat count
    let isValid (value : int) =
        value >= 18 && value <= 264
        
    let step (seats: SeatAssignment) (state: BiddingState) (decision: Decision) : BiddingState =
        match state with
        | Concluded _ -> state   // no transitions once concluded

        | InDuel duel ->
            match decision with
            | Bid newValue when isValid newValue && newValue > int duel.CurrentValue.Value ->
                // Bidder raises; duel continues at the new value
                InDuel { duel with CurrentValue = Some (float newValue) }

            | Bid _ ->
                // invalid/non-increasing bid — ignore, state unchanged
                state

            | Pass ->
                // Responder passed — Bidder wins this duel.
                // If Bidder was already dueling Forehand (i.e. this was the SECOND duel), bidding concludes.
                if duel.Bidder = seats.Forehand || duel.Responder = seats.Forehand then
                    Concluded (Some duel.Bidder, int duel.CurrentValue.Value)
                else
                    // First duel just ended — winner now duels Forehand
                    InDuel { Bidder = duel.Bidder; Responder = seats.Forehand; CurrentValue = duel.CurrentValue }

    type GameSessionStore() =
        let sessions = ConcurrentDictionary<string, GameSession>()
        let seats = ConcurrentDictionary<string, string list>()
        
        member _.AddPlayer(roomId: string, player: PlayerId) =
            match seats.TryAdd(roomId, [player]) with
            | true -> true
            | false ->
                match seats.TryGetValue(roomId) with
                | true, seat -> seats.TryUpdate(roomId, List.append seat [player], seat)
                | false, _ -> false
        member _.GetPlayer(roomId: string) =
            match seats.TryGetValue(roomId) with
            | true, seat -> Some seat
            | false, _ -> None
        member _.AssignSeats(players : string list) =
            match List.length players with
            | x when x = 3 -> Some {Forehand = players.[0]; Middlehand = players.[1]; Rearhand = players.[2]}
            | _ ->  None
        
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