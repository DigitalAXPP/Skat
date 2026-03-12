namespace Skat.Data

open SQLite
open Usermanagement

module Migrations =
    
    let private createTables (conn : SQLiteAsyncConnection) = async {
            let _ = conn.CreateTableAsync<User>()
                    |> Async.AwaitTask
            return ()
        }

    let run (conn : SQLiteAsyncConnection) = async {
        do! createTables conn
    }