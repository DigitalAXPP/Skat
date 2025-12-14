module SharedTypes

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
    /// Navigate to the login page where the user can enter credentials or authenticate.
    | GoToLoginPage
    /// Navigate to the game page where the main gameplay or game UI is presented.
    | GoToGamePage
    /// Navigate to the application's home (landing) page.
    | GoToHomePage
    /// No navigation action is required.
    | NoIntent