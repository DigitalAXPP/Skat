module ReizenPage

open System.Text.Json
open System.Text.Json.Serialization
open Fabulous.Avalonia
open type Fabulous.Avalonia.View
open Fabulous
open SignalRClient
open SharedTypes
open Skat.Game.State.Domain

type Role =
    | ActiveBidder
    | ActiveResponder
    | Waiting

type Model = 
    { 
        RoomId: string
        Me: string
        PlayerId: string
        Seat: Seat
        HighestBidder: string
        Bid: float option
        Bidding : BiddingState
        Seats : SeatAssignment
        Duel : Duel option
    }

type Msg =
    | ChangeRoomId of string
    | ChangeUserId of string
    | ChangeHighestBidder of string
    | ChangeBid of string
    | SetBid of float
    | DeclineBid
    | RequestBid of int
    | ChangeGameSession of SeatAssignment * Duel
    | UpdateGameSession of BiddingState

let getRole (model : Model) : Role =
    match model.Bidding with
    | InDuel duel when duel.Bidder.ToUpper() = model.Me.ToUpper() ->
        ActiveBidder
    | InDuel duel when duel.Responder.ToUpper() = model.Me.ToUpper() ->
        ActiveResponder
    | InDuel _ -> Waiting
    | Concluded _ -> Waiting

let init(me : string) (seats : SeatAssignment) (duel : Duel) =
    { 
        RoomId = ""
        Me = me
        PlayerId = ""
        Seat = Dealer
        HighestBidder = ""
        Bid = None
        Bidding = InDuel duel
        Seats = seats
        Duel = None
    }, Cmd.none

let update msg model =
    match msg with
    | ChangeRoomId id -> { model with RoomId = id }, Cmd.none, NoIntent
    | ChangeUserId id -> { model with Me = id }, Cmd.none, NoIntent
    | ChangeHighestBidder id -> { model with HighestBidder = id }, Cmd.none, NoIntent
    | ChangeBid bid -> { model with Bid = Some (float bid) }, Cmd.none, NoIntent
    | RequestBid bid -> model, Cmd.none, SendDecision (model.RoomId, model.Me.ToUpper(), Bid bid)
    | SetBid bid ->
        let message = {
            PlayerId = model.Me.ToUpper()
            Value = Some (int bid)
            BidStep = Tender.ToString()
        }
        let json = JsonSerializer.Serialize(message)
        model, Cmd.none, NewGameEvent (model.RoomId, model.Me.ToUpper(), Tender, json)
    | DeclineBid ->
        let message = {
            PlayerId = model.Me.ToUpper()
            Value = None
            BidStep = Pass.ToString()
        }
        let json = JsonSerializer.Serialize(message)
        model, Cmd.none, NewGameEvent (model.RoomId, model.Me.ToUpper(), Withdraw, json)
    | ChangeGameSession (seat, duel) -> { model with Seats = seat ; Bidding = InDuel duel}, Cmd.none, NoIntent
    | UpdateGameSession state -> { model with Bidding = state }, Cmd.none, NoIntent

let view (hub: HubService option) model =
    VStack() {
        TextBlock($"Room ID: {model.RoomId}")
        TextBlock($"My ID: {model.Me}")
        TextBlock($"Highest Bidder: {model.HighestBidder}")
        TextBlock($"Seat: {model.Seat}")
        TextBlock($"Forehand: {model.Seats.Forehand}")
        TextBlock($"Middlehand: {model.Seats.Middlehand}")
        TextBlock($"Rearhand: {model.Seats.Rearhand}")
        match model.Duel with
        | Some d ->
            TextBlock($"Bidder: {d.Bidder}")
            TextBlock($"Responder: {d.Responder}")
            TextBlock($"Bid: {d.CurrentValue}")
        | None -> ()
        match model.Bidding with
        | InDuel duel ->
            match getRole model with
            | ActiveBidder ->
                TextBlock($"You are bidding against {duel.Responder}.")
                TextBlock($"Bid: {duel.CurrentValue}")
            | ActiveResponder ->
                TextBlock($"{duel.Bidder} bids {duel.CurrentValue}. Accept or pass?")
                TextBlock($"Bid: {duel.CurrentValue}")
            | Waiting ->
                TextBlock($"Waiting: {duel.Bidder} vs {duel.Responder} are bidding ({duel.CurrentValue})")
        | Concluded (Some winner, value) -> TextBlock($"{winner} won the bid at {value}")
        | Concluded (None, _) -> TextBlock("Everybody passed.")

        NumericUpDown(18.0, 264.0, model.Bid, fun v -> RequestBid (int (v |> Option.defaultValue 18.0)))
            .increment(1.0)
            .formatString("0")
            .clipValueToMinMax(true)
        Button("Decline", DeclineBid)
    }