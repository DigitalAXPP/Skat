module SharedTypes

open Skat.Game.Types

/// Represents the possible top-level pages/screens in the application.
/// Values of this type are used by the application's routing and navigation logic
/// to indicate which page should be displayed.
type Page =
    /// The application's home (landing) page.
    | PageHome
    /// The login page where a user can enter credentials or authenticate.
    | PageLogin
    /// The game page where the main gameplay UI is presented.
    | PageGame

/// Represents a navigation intent used by the application routing logic.
//
/// Use values of this type to indicate which page the application should navigate to.
type Intent =
    | StartGameRequested of string
    | EndGameRequested of string
    | SendSelectedCard of string
    | SendMessageToAll of string
    /// Navigate to the login page where the user can enter credentials or authenticate.
    | GoToLoginPage
    /// Navigate to the game page where the main gameplay or game UI is presented.
    | GoToGamePage
    /// Navigate to the application's home (landing) page.
    | GoToHomePage
    /// No navigation action is required.
    | NoIntent

type ConnectionStatus =
    | Disconnected
    | Connecting
    | Connected

type ServerMsg =
    | MoveReceiving of string
    | JoinGame of string list
    | QuitGame

type GameState =
    | InGame
    | NotInGame

type Seat =
    | Forehand
    | Middlehand
    | Rearhand

type PlayerState =
    { 
        PlayerId : int
        Seat : Seat
        CurrentBid : int option
        Hand : Card list
        TricksWon : Card list list
        IsDeclarer : bool
    }

type GamePhase =
    | Bidding
    | Playing
    | GameOver

type BiddingState =
    { 
        CurrentBidder : PlayerId
        Listener : PlayerId
        CurrentValue : int 
    }

type Trump =
    | Suit of Suite
    | Grand
    | Null

type TrickState =
    { 
        CurrentPlayer : PlayerId
        CardsOnTable : (PlayerId * Card) list
        GameType : Trump
        Declarer : PlayerId 
    }

type ScoreState =
    { 
        Declarer : PlayerId
        DeclarerScore : int
        DefendersScore : int 
    }

type GameDetails =
    { 
        Players : Map<PlayerId, PlayerState>
        CurrentTrick : Card list
        Phase : GamePhase 
    }

let getImageUri (imageName : string) =
    // Return the URI string for the embedded image resource
    $"avares://skat/Resources/Images/{imageName}"