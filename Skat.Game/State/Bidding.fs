namespace Skat.Game.State

open System.Collections.Concurrent
open System.Linq

module Domain =
    type PlayerId = string
    type position =
        | ForeHand
        | MiddleHand
        | RearHand
    type phase =
        | MiddlehandVSForehand
        | RearhandVSMiddlehand of position
        | RearhandVSMH of position * position
        | RearhandVSForehand of position
        | RearhandVSFH of position * position

    type DuelTest = { Bidder: position; Responder: position; CurrentValue: float option }
    type Duel = { Bidder: string; Responder: string; CurrentValue: float option }
    type BiddingState =
        | InDuel of Duel * phase
        | Concluded of winner: string option * winningBid: int option
    type SeatAssignment = { Forehand: PlayerId; Middlehand: PlayerId; Rearhand: PlayerId }
    type GameSession = { RoomId: string; Seats: SeatAssignment; Bidding: BiddingState }
    type Decision =
        | Bid of int
        | Pass

    // let s = { Forehand = "Veru"; Middlehand = "Alex"; Rearhand = "Theo" }
    // let d1 = { Bidder = "Alex"; Responder = "Veru"; CurrentValue = None }
    // let d2 = { Bidder = "Alex"; Responder = "Veru"; CurrentValue = Some 18 }
    //
    // let b = InDuel (d1,MiddlehandVSForehand)
    // let b2 = InDuel (d2,MiddlehandVSForehand)    
    // let m = test s b "Alex" // Alex passes
    // let m2 = test s b2 "Alex" // Alex passes
    // let m3 = test s b2 "Veru" // Veru passes, value 18
    // let t = test s m "Theo" // Theo passes, value 0
    // let t2 = test s m2 "Theo" // Theo passes, value 18
    // let t3 = test s m3 "Theo" // Theo passes, Alex should win at bid of 18
    // let v = test s t "Veru" // Veru passes, no winner --> correct
    // let v2 = test s t2 "Veru" // Veru passes, she wins -- should not need a third pass.
    // let v3 = test s t3 "Alex" // Alex passes, he wins -- should not need a third pass

open Domain

module Bidding =
    
    let playersPassed = ConcurrentDictionary<PlayerId, unit>()
    let tryAddPlayers (player : string) =
        match playersPassed.TryAdd(player, ()) with
        | true -> true
        | false -> false
        
    let passTheBid (seats : SeatAssignment) (state : BiddingState) (PlayerId : string) : BiddingState =
        match state with
        | InDuel (d, MiddlehandVSForehand) -> 
            if seats.Forehand = PlayerId then
                InDuel ({ d with Bidder = seats.Rearhand; Responder = seats.Middlehand }, RearhandVSMiddlehand ForeHand)
            elif seats.Middlehand = PlayerId then
                InDuel ({ d with Bidder = seats.Rearhand; Responder = seats.Forehand }, RearhandVSForehand MiddleHand)
            else
                Concluded (None, None)
        | InDuel (d, RearhandVSMiddlehand ForeHand) ->
            if seats.Middlehand = PlayerId then
                InDuel ({ d with Bidder = seats.Rearhand; Responder = seats.Middlehand }, RearhandVSMH (ForeHand, MiddleHand))
            elif seats.Rearhand = PlayerId && d.CurrentValue.IsSome then
                Concluded (Some d.Responder, Some (int d.CurrentValue.Value))
            else
                InDuel ({ d with Bidder = seats.Rearhand; Responder = seats.Middlehand }, RearhandVSMH (ForeHand, RearHand))
        | InDuel (d, RearhandVSForehand MiddleHand) ->
            if seats.Forehand = PlayerId then
                InDuel ({ d with Bidder = seats.Rearhand; Responder = seats.Forehand }, RearhandVSFH (MiddleHand, ForeHand))
            elif seats.Rearhand = PlayerId && d.CurrentValue.IsSome then
                Concluded (Some d.Responder, Some (int d.CurrentValue.Value))
            else
                InDuel ({ d with Bidder = seats.Rearhand; Responder = seats.Forehand }, RearhandVSFH (MiddleHand, RearHand))
        | InDuel (d, RearhandVSMH (MiddleHand, ForeHand)) ->
            if seats.Rearhand = PlayerId && d.CurrentValue.IsNone then
                Concluded (None, None)
            else
                Concluded (Some d.Bidder, Some (int d.CurrentValue.Value))
        | InDuel (d, RearhandVSMH (ForeHand, RearHand)) ->
            if seats.Middlehand = PlayerId && d.CurrentValue.IsNone then
                Concluded (None, None)
            else
                Concluded (Some d.Responder, Some (int d.CurrentValue.Value))
        | InDuel (d, RearhandVSFH (MiddleHand, ForeHand)) ->
            if seats.Rearhand = PlayerId && d.CurrentValue = None then
                Concluded (None, None)
            else
                Concluded (Some d.Bidder, Some (int d.CurrentValue.Value))
        | InDuel (d, RearhandVSFH (MiddleHand, RearHand)) ->
            if seats.Forehand = PlayerId && d.CurrentValue.IsNone then
                Concluded (None, None)
            else
                Concluded (Some d.Responder, Some (int d.CurrentValue.Value))
    // Verifying if the bid is a valid Skat count        
    let isValid (value : int) =
        value >= 18 && value <= 264
        
    let step (seats: SeatAssignment) (state: BiddingState) (decision: Decision) (PlayerId : string) : BiddingState =
        match state with
        | Concluded _ -> state   // no transitions once concluded

        | InDuel (duel, p) ->
            match decision with
            | Bid newValue when isValid newValue && newValue > int duel.CurrentValue.Value ->
                // Bidder raises; duel continues at the new value
                InDuel ({ duel with CurrentValue = Some (float newValue) }, p)

            | Bid _ ->
                // invalid/non-increasing bid — ignore, state unchanged
                state

            | Pass ->
                passTheBid seats state PlayerId
                // Responder passed — Bidder wins this duel.
                // If Bidder was already dueling Forehand (i.e. this was the SECOND duel), bidding concludes.
                // if duel.Bidder = seats.Middlehand then
                //     InDuel { Bidder = seats.Forehand; Responder = seats.Rearhand; CurrentValue = duel.CurrentValue }
                //     // Concluded (Some duel.Bidder, int duel.CurrentValue.Value)
                // elif duel.Responder = seats.Forehand then
                //     InDuel { duel with Responder = seats.Rearhand }
                // else
                //     // First duel just ended — winner now duels Forehand
                //     InDuel { Bidder = duel.Bidder; Responder = seats.Forehand; CurrentValue = duel.CurrentValue }

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
            let session = {RoomId = roomId; Seats = assignment; Bidding = InDuel (duel, MiddlehandVSForehand)}
            sessions.[roomId] <- session
            sessions
            
        member _.GetSession(roomId: string) =
            match sessions.TryGetValue(roomId) with
            | true, session -> Some session
            | false, _ -> None
            
        member _.UpdateSession(roomId: string, newBid: BiddingState) =
            sessions.[roomId] <- { sessions.[roomId] with Bidding = newBid }