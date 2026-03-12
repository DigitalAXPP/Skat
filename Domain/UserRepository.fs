namespace Skat.UserRepository

open Skat.Data.Usermanagement
open Skat.Database

//module Generics =

//    let getUsers () = async {
//        let conn = SkatDB.getConnection ()

//        let! users = 
//            conn.Table<User>().ToListAsync()
//            |> Async.AwaitTask

//        return users |> Seq.toList
//    }

//    let insertUser user = async {
//        let conn = SkatDB.getConnection ()
//        do! conn.InsertAsync(user)
//            |> Async.AwaitTask
//            |> Async.Ignore
//        return ()
//    }

//    let deleteUser user = async {
//        let conn = SkatDB.getConnection ()
//        do! conn.DeleteAsync(user)
//            |> Async.AwaitTask
//            |> Async.Ignore
//        return ()
//    }