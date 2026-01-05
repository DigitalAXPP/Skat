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