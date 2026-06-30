module ReizenPage

open Fabulous.Avalonia
open type Fabulous.Avalonia.View
open Fabulous
open SignalRClient
open SharedTypes

type Model = 
    { 
        RoomId: string
        UserId: string
        HighestBidder: string
        Bid: string
    }

type Msg =
    | ChangeRoomId of string
    | ChangeUserId of string
    | ChangeHighestBidder of string
    | ChangeBid of string
    | SetBid of string

let init =
    { 
        RoomId = ""
        UserId = ""
        HighestBidder = ""
        Bid = "0"
    }, Cmd.none

let update msg model =
    match msg with
    | ChangeRoomId id -> { model with RoomId = id }, Cmd.none, NoIntent
    | ChangeUserId id -> { model with UserId = id }, Cmd.none, NoIntent
    | ChangeHighestBidder id -> { model with HighestBidder = id }, Cmd.none, NoIntent
    | ChangeBid bid -> { model with Bid = bid }, Cmd.none, NoIntent
    | SetBid bid -> model, Cmd.none, NewGameEvent (model.RoomId, model.UserId.ToUpper(), Bid, bid)

let view (hub: HubService option) model =
    VStack() {
        TextBlock($"Room ID: {model.RoomId}")
        TextBlock($"User ID: {model.UserId}")
        TextBlock($"Highest Bidder: {model.HighestBidder}")
        TextBlock($"Bid: {model.Bid}")
        TextBox("0", SetBid)
    }