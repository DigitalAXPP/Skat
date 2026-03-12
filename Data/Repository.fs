namespace Skat.Data

open SQLite
open Skat.Data

module Repository =
    
    let getAll<'T when 'T : (new : unit -> 'T)> (db : IDatabase) = async {
        let! result = db.Connection.Table<'T>().ToListAsync() |> Async.AwaitTask
        return result
    }

    let insert<'T> (db : IDatabase) (item : 'T) = async {
        let! _ = db.Connection.InsertAsync(item) |> Async.AwaitTask
        return ()
    }

    let update<'T> (db : IDatabase) (item : 'T) = async {
        let! _ = db.Connection.UpdateAsync(item) |> Async.AwaitTask
        return ()
    }

    let delete<'T> (db : IDatabase) (item : 'T) = async {
        let! _ = db.Connection.DeleteAsync(item) |> Async.AwaitTask
        return ()
    }