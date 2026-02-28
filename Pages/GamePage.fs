module GamePage

open Fabulous.Avalonia
open type Fabulous.Avalonia.View
open Fabulous
open SharedTypes
open Skat.Game.Types
open Skat.Game.Functions
open SignalRClient

type Model = {
    Game: GameType
    CardValue: int
    CardSelected: (PlayerId * Card) list option
    SelectedCard: Card option
    Hand: Card list option
    PlayerOne: PlayerConfig
    PlayerTwo: PlayerConfig
    PlayerThree: PlayerConfig
}

type Msg =
    | NextHomePage
    | SetCards
    | ToggleCards of (PlayerId * Card) list option
    | CompareCards
    | RemoveCard of (PlayerId * Card)
    | WinningHand
    | TellAll of string

let init = 
    {
        Game = Grand
        CardValue = 0
        CardSelected = None
        SelectedCard = None
        Hand = None
        PlayerOne = firstPlayer
        PlayerTwo = secondPlayer
        PlayerThree = thirdPlayer
    }, Cmd.none

let assignHandToPlayers model (hands: GameSetup) =
    let firstP = { model with PlayerOne.StartingHand = hands.FirstPlayer }
    let secondP = { firstP with PlayerTwo.StartingHand = hands.SecondPlayer }
    { secondP with PlayerThree.StartingHand = hands.ThirdPlayer }

let removeCardFromPlayer model (id: int) (card: Card) =
        let newHand = 
            match id with
            | 1 when model.PlayerOne.Player = 1
                -> List.filter ((<>) card) model.PlayerOne.StartingHand
            | 2 when model.PlayerTwo.Player = 2
                -> List.filter ((<>) card) model.PlayerTwo.StartingHand
            | 3 when model.PlayerThree.Player = 3
                -> List.filter ((<>) card) model.PlayerThree.StartingHand
        match id with
        | 1 when model.PlayerOne.Player = 1
            -> { model with PlayerOne.StartingHand = newHand}
        | 2 when model.PlayerTwo.Player = 2
            -> { model with PlayerTwo.StartingHand = newHand}
        | 3 when model.PlayerThree.Player = 3
            -> { model with PlayerThree.StartingHand = newHand}

let update (hubService: HubService) msg model =
    match msg with
    | NextHomePage -> model, Cmd.none, GoToHomePage
    | SetCards -> (assignHandToPlayers model (dealInitialHand Deck)), Cmd.none, NoIntent
    | ToggleCards (Some [(id, cardSelection)]) ->
        let newList =
            match model.CardSelected with
            | None -> Some [(id, cardSelection)]
            | Some _ -> Some (model.CardSelected.Value @ [(id, cardSelection)])
        let newModel = { model with CardSelected = newList }
        newModel, Cmd.batch [
            Cmd.ofMsg (TellAll (cardSelection.ToString()))
            Cmd.ofMsg CompareCards
            Cmd.ofMsg (RemoveCard (id, cardSelection))
        ], NoIntent
    | CompareCards ->
        let selectedCards = model.CardSelected
        let one = 
            match selectedCards.IsSome with
            | true -> 
                let (id, c) = selectedCards.Value.Head
                cardStrength model.Game c
            | false -> failwith "No cards selected."
        let newModel = { model with CardValue = one }
        newModel, Cmd.none, NoIntent
    | RemoveCard (id, cardOpt) ->
        let newModel = removeCardFromPlayer model id cardOpt
        newModel, Cmd.none, NoIntent
    | WinningHand -> 
        let winner = 
            match model.CardSelected with
            | Some v -> winningHand model.Game v
            | None -> failwith "No cards selected yet."
        let modelHandsWon =
            match winner with
            | (1, _, _) -> 
                let newList = 
                    match model.PlayerOne.HandsWon with
                    | Some v -> v @ model.CardSelected.Value
                    | None -> model.CardSelected.Value
                { model with PlayerOne.HandsWon = Some newList }
            | (2, _, _) -> 
                let newList = 
                    match model.PlayerTwo.HandsWon with
                    | Some v -> v @ model.CardSelected.Value
                    | None -> model.CardSelected.Value
                { model with PlayerTwo.HandsWon = Some newList }
            | (3, _, _) -> 
                let newList = 
                    match model.PlayerThree.HandsWon with
                    | Some v -> v @ model.CardSelected.Value
                    | None -> model.CardSelected.Value
                { model with PlayerThree.HandsWon = Some newList }
        let newModel = { modelHandsWon with CardSelected = None  }
        newModel, Cmd.none, NoIntent
    | TellAll message ->
        model, Cmd.none, SendMessageToAll message

let view (hub: HubService option) model =
        VStack(spacing = 25.) {
            TextBlock($"Connection: {hub.Value.IsConnected}")
            TextBlock($"Third Page: {model.PlayerOne.StartingHand}")
            TextBlock($"Third Page: {model.CardSelected}")
            TextBlock($"Card value: {model.CardValue}")
            Button("Go to Home Page", NextHomePage)
            Button("Set Cards", SetCards)
            Button("Winning Hand", WinningHand)
            HStack(spacing = 10.) {
                TextBlock($"{(calculateAugen model.PlayerOne)}")
                TextBlock($"{(calculateAugen model.PlayerTwo)}")
                TextBlock($"{(calculateAugen model.PlayerThree)}")

            }
            HStack(spacing = 25.) {
                ListBox(model.PlayerOne.StartingHand, fun item ->
                    ListBoxItem(
                        Image(getImageUri (cardToImageName item))
                            .height(15.)
                            .centerHorizontal()
                    ).onTapped(fun _ -> ToggleCards (Some [(model.PlayerOne.Player, item)]))
                )
                ListBox(model.PlayerTwo.StartingHand, fun item ->
                    ListBoxItem(
                        Image(getImageUri (cardToImageName item))
                            .height(15.)
                            .centerHorizontal()
                    ).onTapped(fun _ -> ToggleCards (Some [(model.PlayerTwo.Player, item)]))
                )
                ListBox(model.PlayerThree.StartingHand, fun item ->
                    ListBoxItem(
                        Image(getImageUri (cardToImageName item))
                            .height(15.)
                            .centerHorizontal()
                    ).onTapped(fun _ -> ToggleCards (Some [(model.PlayerThree.Player, item)]))
                )
            }
    }
