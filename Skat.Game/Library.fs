namespace Skat.Game

module Types =
    type Suite = 
        | Diamonds
        | Hearts
        | Clubs
        | Spades
    type Rank =
        | Seven
        | Eight
        | Nine
        | Dame
        | King
        | Ten
        | Ace
        | Jack
    type Card = { Suite: Suite; Rank: Rank }
    
    type GameSetup =
        {
            FirstPlayer: Card list
            SecondPlayer: Card list
            ThirdPlayer: Card list
            Skat: Card list
        }

    // Two variables to create the deck
    let allRanks = [Seven ; Eight ; Nine ; Dame ; King ; Ten ; Ace ; Jack]
    let allSuites = [Spades ; Clubs ; Hearts ; Diamonds]

    // loop through all suites and ranks to create the full deck
    let Deck =
        [ for suite in allSuites do
            for rank in allRanks do
                yield { Rank = rank ; Suite = suite } ]

    type PlayerId = int

    type SkatPosition =
        | Geben
        | Hoeren
        | Sagen

    type Action =
        | Bid
        | Reject
        | Undecided

    type GameType =
        | SuitGame of Suite  // One suit is trump
        | Grand              // Only Jacks are trump
        | NullGame               // No trumps at all

    let normalRankOrder = [Seven; Eight; Nine; Dame; King; Ten; Ace]

    type PlayerConfig = {
        Player: PlayerId
        Activity: Action
        Amount: int option
        Position: SkatPosition
        StartingHand: Card list
        HandsWon: (PlayerId * Card) list option
    }

    let firstPlayer = {
        Player = 1
        Activity = Undecided
        Amount = None
        Position = Geben
        StartingHand = []
        HandsWon = None
    }

    let secondPlayer = {
        Player = 2
        Activity = Undecided
        Amount = None
        Position = Geben
        StartingHand = []
        HandsWon = None
    }

    let thirdPlayer = {
        Player = 3
        Activity = Undecided
        Amount = None
        Position = Geben
        StartingHand = []
        HandsWon = None
    }

module Functions =
    open Types

    let cardToImageName =
        function
        | { Suite = Hearts; Rank = Ace } -> "hearts_ace.png"
        | { Suite = Hearts; Rank = King } -> "hearts_king.png"
        | { Suite = Hearts; Rank = Seven }  -> "hearts_seven.png"
        | { Suite = Hearts; Rank = Eight }  -> "hearts_eight.png"
        | { Suite = Hearts; Rank = Nine }  -> "hearts_nine.png"
        | { Suite = Hearts; Rank = Ten }  -> "hearts_ten.png"
        | { Suite = Hearts; Rank = Dame }  -> "hearts_dame.png"
        | { Suite = Hearts; Rank = Jack }  -> "hearts_jack.png"
        | { Suite = Clubs; Rank = Ace } -> "clubs_ace.png"
        | { Suite = Clubs; Rank = King } -> "clubs_king.png"
        | { Suite = Clubs; Rank = Seven }  -> "clubs_seven.png"
        | { Suite = Clubs; Rank = Eight }  -> "clubs_eight.png"
        | { Suite = Clubs; Rank = Nine }  -> "clubs_nine.png"
        | { Suite = Clubs; Rank = Ten }  -> "clubs_ten.png"
        | { Suite = Clubs; Rank = Dame }  -> "clubs_dame.png"
        | { Suite = Clubs; Rank = Jack }  -> "clubs_jack.png"
        | { Suite = Spades; Rank = Ace } -> "spades_ace.png"
        | { Suite = Spades; Rank = King } -> "spades_king.png"
        | { Suite = Spades; Rank = Seven }  -> "spades_seven.png"
        | { Suite = Spades; Rank = Eight }  -> "spades_eight.png"
        | { Suite = Spades; Rank = Nine }  -> "spades_nine.png"
        | { Suite = Spades; Rank = Ten }  -> "spades_ten.png"
        | { Suite = Spades; Rank = Dame }  -> "spades_dame.png"
        | { Suite = Spades; Rank = Jack }  -> "spades_jack.png"
        | { Suite = Diamonds; Rank = Ace } -> "diamonds_ace.png"
        | { Suite = Diamonds; Rank = King } -> "diamonds_king.png"
        | { Suite = Diamonds; Rank = Seven }  -> "diamonds_seven.png"
        | { Suite = Diamonds; Rank = Eight }  -> "diamonds_eight.png"
        | { Suite = Diamonds; Rank = Nine }  -> "diamonds_nine.png"
        | { Suite = Diamonds; Rank = Ten }  -> "diamonds_ten.png"
        | { Suite = Diamonds; Rank = Dame }  -> "diamonds_dame.png"
        | { Suite = Diamonds; Rank = Jack }  -> "diamonds_jack.png"

    let shuffleDeck (deck: Card list) =
        let rnd = System.Random()
        deck |> List.sortBy (fun _ -> rnd.Next())

    let dealInitialHand deck =
        let shuffled = shuffleDeck deck
        let (firstPlayerHand, rest) = List.splitAt 10 shuffled
        let (secondPlayerHand, newDeck) = List.splitAt 10 rest
        let (thirdPlayerHand, skat) = List.splitAt 10 newDeck
        { FirstPlayer = firstPlayerHand; SecondPlayer = secondPlayerHand; ThirdPlayer = thirdPlayerHand; Skat = skat }

    let cardStrength (game: GameType) (card: Card) : int =
        let jackSuitOrder = [Clubs; Spades; Hearts; Diamonds]

        match game with
        | Grand | SuitGame _ when card.Rank = Jack ->
            100 + List.findIndex ((=) card.Suite) jackSuitOrder
        | SuitGame trump when card.Suite = trump ->
            50 + List.findIndex ((=) card.Rank) normalRankOrder
        | _ ->
            List.findIndex ((=) card.Rank) normalRankOrder

    let winningHand (game: GameType) (hands: (PlayerId * Card) list) =
        hands
        |> List.map (fun (a,b) -> (a,b, (cardStrength game b)))
        |> List.maxBy (fun (_, _, v) -> v)

    let handValueGrand (hand: Card) =
        match hand.Rank with
        | Seven -> 0
        | Eight -> 0
        | Nine -> 0
        | Dame -> 3
        | King -> 4
        | Ten -> 10
        | Ace -> 11
        | Jack -> 2

    let calculateAugen (player: PlayerConfig) =
        match player.HandsWon with
        | Some v ->
            v
            |> List.map snd
            |> List.map (fun h -> handValueGrand h)
            |> List.sum
        | None -> 0