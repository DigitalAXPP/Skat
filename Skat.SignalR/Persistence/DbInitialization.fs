namespace Skat.SignalR.Persistence

open Microsoft.Data.Sqlite

module DbInitiliaziation =
    let initialize (connectionstring : string) =
        use conn = new SqliteConnection(connectionstring)
        conn.Open()
        let transaction = conn.BeginTransaction()

        use cmd = conn.CreateCommand()
        cmd.Transaction <- transaction

        cmd.CommandText <- """CREATE TABLE IF NOT EXISTS GameRoom (
            RoomId INTEGER PRIMARY KEY
        )"""
        cmd.ExecuteNonQuery() |> ignore

        cmd.CommandText <- """CREATE TABLE IF NOT EXISTS Player (
            PlayerId INTEGER PRIMARY KEY,
            UserId TEXT NOT NULL,
            Name TEXT NOT NULL,
            RoomId INTEGER,
            FOREIGN KEY (RoomId) REFERENCES GameRoom(RoomId)
        )"""
        cmd.ExecuteNonQuery() |> ignore

        transaction.Commit()