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
        Seats : SeatAssignment option
        Duel : Duel option
    }

type Msg =
    | ChangeRoomId of string
    | ChangeUserId of string
    | ChangeHighestBidder of string
    | ChangeBid of string
    | SetBid of float
    | DeclineBid
    | ChangeGameSession of SeatAssignment

let init =
    { 
        RoomId = ""
        Me = ""
        PlayerId = ""
        Seat = Dealer
        HighestBidder = ""
        Bid = None
        Seats = None
        Duel = None
    }, Cmd.none

let update msg model =
    match msg with
    | ChangeRoomId id -> { model with RoomId = id }, Cmd.none, NoIntent
    | ChangeUserId id -> { model with Me = id }, Cmd.none, NoIntent
    | ChangeHighestBidder id -> { model with HighestBidder = id }, Cmd.none, NoIntent
    | ChangeBid bid -> { model with Bid = Some (float bid) }, Cmd.none, NoIntent
    | SetBid bid ->
        let message = {
            PlayerId = model.Me.ToUpper()
            Value = Some (int bid)
            BidStep = Bid.ToString()
        }
        let json = JsonSerializer.Serialize(message)
        model, Cmd.none, NewGameEvent (model.RoomId, model.Me.ToUpper(), EventType.Bid, json)
    | DeclineBid ->
        let message = {
            PlayerId = model.Me.ToUpper()
            Value = None
            BidStep = Pass.ToString()
        }
        let json = JsonSerializer.Serialize(message)
        model, Cmd.none, NewGameEvent ((model.RoomId), model.Me.ToUpper(), EventType.Bid, json)
    | ChangeGameSession seat -> { model with Seats = Some seat }, Cmd.none, NoIntent

let view (hub: HubService option) model =
    VStack() {
        TextBlock($"Room ID: {model.RoomId}")
        TextBlock($"My ID: {model.Me}")
        TextBlock($"Highest Bidder: {model.HighestBidder}")
        TextBlock($"Seat: {model.Seat}")
        TextBlock($"Bid: {model.Bid}")
        match model.Seats with
        | Some d ->
            TextBlock($"Bidder: {d.Forehand}")
            TextBlock($"Bid: {d.Middlehand}")
            TextBlock($"Responder: {d.Rearhand}")
        | None -> ()
        NumericUpDown(18.0, 264.0, model.Bid, fun v -> SetBid (int (v |> Option.defaultValue 18.0)))
            .increment(1.0)
            .formatString("0")
            .clipValueToMinMax(true)
        Button("Decline", DeclineBid)
    }