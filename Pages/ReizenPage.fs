module ReizenPage

open System.Text.Json
open System.Text.Json.Serialization
open Fabulous.Avalonia
open type Fabulous.Avalonia.View
open Fabulous
open JsonConversion
open Microsoft.AspNetCore.Http
open SignalRClient
open SharedTypes
open Skat.Game.State.Bidding
open Skat.Game.State.Domain
open Skat.Game.Types
open Skat.Game.Functions

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
        Setup : DealState
        CardSelection : (PlayerId * Card) list option
    }

type Msg =
    | ChangeRoomId of string
    | ChangeUserId of string
    | ChangeHighestBidder of string
    | ChangeBid of string
    | UpdateCardSelection of cards : (PlayerId * Card) list option
    | SetBid of float
    | DeclineBid
    | RequestBid of int
    | ChangeGameSession of SeatAssignment * Duel * phase
    | UpdateGameSession of BiddingState
    | ToggleCard of pid : PlayerId * card : Card

let getRole (model : Model) : Role =
    match model.Bidding with
    | InDuel (duel, p) when duel.Bidder.ToUpper() = model.Me.ToUpper() ->
        ActiveBidder
    | InDuel (duel, p) when duel.Responder.ToUpper() = model.Me.ToUpper() ->
        ActiveResponder
    | InDuel _ -> Waiting
    | Concluded _ -> Waiting

let init(me : string) (seats : SeatAssignment) (state : BiddingState) (CardSetup : DealState) =
    { 
        RoomId = ""
        Me = me
        PlayerId = ""
        Seat = Dealer
        HighestBidder = ""
        Bid = None
        Bidding = state
        Seats = seats
        Duel = None
        Setup = CardSetup
        CardSelection = None
    }, Cmd.none

let update msg model =
    match msg with
    | ChangeRoomId id -> { model with RoomId = id }, Cmd.none, NoIntent
    | ChangeUserId id -> { model with Me = id }, Cmd.none, NoIntent
    | ChangeHighestBidder id -> { model with HighestBidder = id }, Cmd.none, NoIntent
    | ChangeBid bid -> { model with Bid = Some (float bid) }, Cmd.none, NoIntent
    | UpdateCardSelection cards -> { model with CardSelection = cards }, Cmd.none, NoIntent
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
    | ChangeGameSession (seat, duel, p) -> { model with Seats = seat ; Bidding = InDuel (duel, p)}, Cmd.none, NoIntent
    | UpdateGameSession state -> { model with Bidding = state }, Cmd.none, NoIntent
    | ToggleCard (id, c) ->
        let newList =
            match model.CardSelection with
            | None -> Some [(id, c)]
            | Some s ->
                if List.contains (id, c) s then
                    Some (List.filter ((<>) (id, c)) s)
                else
                    Some (s @ [ (id, c) ])
        let newModel = { model with CardSelection = newList }
        let json = match newList with
                    | Some s -> JsonSerializer.Serialize(s, jsonOptions)
                    | None -> "[]"
        newModel, Cmd.none, NewGameEvent (model.RoomId, model.Me.ToUpper(), CardPlayed, json)

let view (hub: HubService option) model =
    VStack() {
        VStack () {
            TextBlock($"Room ID: {model.RoomId}")
            TextBlock($"My ID: {model.Me}")
            TextBlock($"Forehand: {model.Seats.Forehand}")
            TextBlock($"Middlehand: {model.Seats.Middlehand}")
            TextBlock($"Rearhand: {model.Seats.Rearhand}")
            
        }
        HStack () {
            ListBox(
                (match model.Setup.Hands.TryFind "Skat" with
                    | Some hand -> hand
                    | None -> []),
                fun card ->
                    ListBoxItem(
                        Image(getImageUri (cardToImageName card))
                            .height(50.)
                            .centerHorizontal()
                    )
            )
        }
        HStack () {
            ListBox(
                (match model.CardSelection with
                | Some selection -> selection
                | None -> []),
                fun (p, card) ->
                    ListBoxItem(
                        Image(getImageUri (cardToImageName card))
                            .height(50.)
                            .centerHorizontal()
                    )
            )
        }
        VStack () {
            match model.Duel with
            | Some d ->
                TextBlock($"Bidder: {d.Bidder}")
                TextBlock($"Responder: {d.Responder}")
                TextBlock($"Bid: {d.CurrentValue}")
            | None -> ()
        }
        HStack () {
            match model.Bidding with
            | InDuel (duel, p) ->
                match getRole model with
                | ActiveBidder ->
                    TextBlock($"You are bidding against {duel.Responder}.")
                    TextBlock($"Bid: {duel.CurrentValue}")
                    ListBox(
                        (match model.Setup.Hands.TryFind model.Seats.Forehand with
                            | Some hand -> hand
                            //(Some [(model.PlayerOne.Player, item)]))
                            | None -> []),
                        fun card ->
                                ListBoxItem(
                                    Image(getImageUri (cardToImageName card))
                                        .height(50.)
                                        .centerHorizontal()
                                ).onTapped(fun _ -> ToggleCard (model.Seats.Forehand, card))
                    )
                | ActiveResponder ->
                    TextBlock($"{duel.Bidder} bids {duel.CurrentValue}. Accept or pass?")
                    TextBlock($"Bid: {duel.CurrentValue}")
                    ListBox(
                        (match model.Setup.Hands.TryFind model.Seats.Middlehand with
                            | Some hand -> hand
                            | None -> []),
                        fun card ->
                                ListBoxItem(
                                    Image(getImageUri (cardToImageName card))
                                        .height(50.)
                                        .centerHorizontal()
                                ).onTapped(fun _ -> ToggleCard (model.Seats.Middlehand, card))
                    )
                    
                | Waiting ->
                    TextBlock($"Waiting: {duel.Bidder} vs {duel.Responder} are bidding ({duel.CurrentValue})")
                    ListBox(
                        (match model.Setup.Hands.TryFind model.Seats.Rearhand with
                            | Some hand -> hand
                            | None -> []),
                        fun card ->
                                ListBoxItem(
                                    Image(getImageUri (cardToImageName card))
                                        .height(50.)
                                        .centerHorizontal()
                                ).onTapped(fun _ -> ToggleCard (model.Seats.Rearhand, card))
                    )
            | Concluded (Some winner, value) -> TextBlock($"{winner} won the bid at {value}")
            | Concluded (None, _) -> TextBlock("Everybody passed.")
        }
        // VStack () {
        //     NumericUpDown(18.0, 264.0, model.Bid, fun v -> RequestBid (int (v |> Option.defaultValue 18.0)))
        //         .increment(1.0)
        //         .formatString("0")
        //         .clipValueToMinMax(true)
        //     Button("Decline", DeclineBid)
        // }
    }