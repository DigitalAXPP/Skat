module ReizenPage

open System.Text.Json
open System.Text.Json.Serialization
open Fabulous.Avalonia
open type Fabulous.Avalonia.View
open Fabulous
open SignalRClient
open SharedTypes

type Model = 
    { 
        RoomId: string
        UserId: string
        PlayerId: string
        Seat: Seat
        HighestBidder: string
        Bid: float option
    }

type Msg =
    | ChangeRoomId of string
    | ChangeUserId of string
    | ChangeHighestBidder of string
    | ChangeBid of string
    | SetBid of float
    | DeclineBid
    | ChangeSeat of Seat

let init =
    { 
        RoomId = ""
        UserId = ""
        PlayerId = ""
        Seat = Dealer
        HighestBidder = ""
        Bid = None
    }, Cmd.none

let update msg model =
    match msg with
    | ChangeRoomId id -> { model with RoomId = id }, Cmd.none, NoIntent
    | ChangeUserId id -> { model with UserId = id }, Cmd.none, NoIntent
    | ChangeHighestBidder id -> { model with HighestBidder = id }, Cmd.none, NoIntent
    | ChangeBid bid -> { model with Bid = Some (float bid) }, Cmd.none, NoIntent
    | SetBid bid ->
        let message = {
            PlayerId = model.UserId.ToUpper()
            Value = Some (int bid)
            BidStep = Bid.ToString()
        }
        let json = JsonSerializer.Serialize(message)
        model, Cmd.none, NewGameEvent (model.RoomId, model.UserId.ToUpper(), Bid, json)
    | DeclineBid ->
        let message = {
            PlayerId = model.UserId.ToUpper()
            Value = None
            BidStep = Pass.ToString()
        }
        let json = JsonSerializer.Serialize(message)
        model, Cmd.none, NewGameEvent ((model.RoomId), model.UserId.ToUpper(), Bid, json)
    | ChangeSeat seat -> { model with Seat = seat }, Cmd.none, NoIntent

let view (hub: HubService option) model =
    VStack() {
        TextBlock($"Room ID: {model.RoomId}")
        TextBlock($"User ID: {model.UserId}")
        TextBlock($"Highest Bidder: {model.HighestBidder}")
        TextBlock($"Seat: {model.Seat}")
        TextBlock($"Bid: {model.Bid}")
        NumericUpDown(18.0, 264.0, model.Bid, fun v -> SetBid (int (v |> Option.defaultValue 18.0)))
            .increment(1.0)
            .formatString("0")
            .clipValueToMinMax(true)
        Button("Decline", DeclineBid)
    }