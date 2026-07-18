namespace Skat.Game.State 

module Bidding =
    type Decision =
        | Bid of int
        | Pass

    type Duel = { Bidder: string; Responder: string; CurrentValue: int }

    type BiddingState =
        | InDuel of Duel
        | Concluded of winner: string option * winningBid: int
        
    let playerA = { Bidder = "Alex"; Responder = "Carlos"; CurrentValue = 18 }