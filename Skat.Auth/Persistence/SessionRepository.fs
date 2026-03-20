namespace Skat.Auth.Persistence

open System
open System.Threading.Tasks

module SessionRepository =

    type ISessionRepository =
        abstract member CreateSession : Guid -> Task<string>

    type SessionRepository (connectionString : string) =
    
        interface ISessionRepository with
            member _.CreateSession (userId: Guid) = task {
                // For simplicity, we just return a new GUID as the session token.
                // In a real application, you would want to store this in the database
                // and associate it with the user ID and an expiration time.
                return Guid.NewGuid().ToString()
            }