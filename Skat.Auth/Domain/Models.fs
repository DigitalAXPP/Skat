module Domain

type RegisterRequest =
    { 
        username : string
        password : string
    }

type RegisterResponse =
    { token : string }

type LoginRequest =
    { 
        username : string
        password : string
    }

type LoginResponse =
    { token : string }

type UserID = System.Guid
type Username = string
type Passwordhash = string

[<CLIMutable>]
type User =
    { 
        Id : UserID
        Username : Username
        PasswordHash : Passwordhash
    }

type AuthState =
    | LoggedOut
    | LoggingIn
    | LoggedIn of User
    | LoginFailed of string

type Model = {
    Auth: AuthState
    Username: string
    Password: string
}

let init =
    { 
        Auth = LoggedOut
        Username = ""
        Password = ""
    }

type Msg =
    | SetUsername of string
    | SetPassword of string
    | LoginRequested of username:string * password:string
    | LoginSuccess of User
    | LoginError of string
    | Logout