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
            RoomId TEXT PRIMARY KEY,
            MaxPlayer INTEGER NOT NULL DEFAULT 4,
            CurrentPlayer INTEGER NOT NULL DEFAULT 0
        )"""
        cmd.ExecuteNonQuery() |> ignore

        cmd.CommandText <- """CREATE TABLE IF NOT EXISTS Player (
            PlayerId TEXT PRIMARY KEY,
            UserId TEXT NOT NULL,
            Name TEXT NOT NULL,
            RoomId INTEGER
        )"""
        cmd.ExecuteNonQuery() |> ignore

        cmd.CommandText <- """CREATE TABLE IF NOT EXISTS GameEvent (
            GameId TEXT PRIMARY KEY,
            RoomId TEXT,
            PlayerId TEXT,
            HandNumber INTEGER,
            Phase TEXT,
            EventType TEXT,
            EventData TEXT,
            Sequence INTEGER,
            CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
            FOREIGN KEY (RoomId) REFERENCES GameRoom (RoomId),
            FOREIGN KEY (PlayerId) REFERENCES Player (PlayerId)
        )"""
        cmd.ExecuteNonQuery() |> ignore

        cmd.CommandText <- """CREATE TABLE IF NOT EXISTS Users (
            id TEXT PRIMARY KEY,
            username TEXT NOT NULL UNIQUE,
            password_hash TEXT NOT NULL
        )"""
        cmd.ExecuteNonQuery() |> ignore

        transaction.Commit()