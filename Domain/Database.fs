namespace Skat.Database

open SQLite
open System.IO
open System
open Skat.Data.Usermanagement
open Skat.Data

module SkatDB =
    
    type Database() =
        let dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "app.db")

        let connection = lazy SQLiteAsyncConnection(dbPath)

        interface IDatabase with
            member _.Connection = connection.Value
            member _.Initialize() = async {
                do! Migrations.run connection.Value
            }

    //let getConnection () = connection.Value

    //let init () = async {
    //    let conn = getConnection ()

    //    do! conn.CreateTableAsync<User>()
    //        |> Async.AwaitTask
    //        |> Async.Ignore
    //}