namespace Skat.Auth.Persistence

module DbInitialization =
    let initialize (connectionstring : string) =
        use conn = new Microsoft.Data.Sqlite.SqliteConnection(connectionstring)
        conn.Open()
        let transaction = conn.BeginTransaction()
        use cmd = conn.CreateCommand()
        cmd.Transaction <- transaction
        cmd.CommandText <- """CREATE TABLE IF NOT EXISTS Users (
            id TEXT PRIMARY KEY,
            username TEXT NOT NULL UNIQUE,
            password_hash TEXT NOT NULL
        )"""
        cmd.ExecuteNonQuery() |> ignore
        transaction.Commit()