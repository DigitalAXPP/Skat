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