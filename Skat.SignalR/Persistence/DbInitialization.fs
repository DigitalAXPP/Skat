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

        cmd.CommandText <- """CREATE TABLE IF NOT EXISTS GameParticipant (
            GameId TEXT NOT NULL,
            PlayerId TEXT NOT NULL,
            SeatPosition INTEGER NOT NULL,  -- 0, 1, 2 (matters for dealing order)
            Role TEXT,                       -- 'DECLARER' | 'DEFENDER' | NULL initially
            PRIMARY KEY (GameId, PlayerId),
            FOREIGN KEY (GameId) REFERENCES GameEvent (GameId),
            FOREIGN KEY (PlayerId) REFERENCES Player (PlayerId)
        )"""
        cmd.ExecuteNonQuery() |> ignore

        cmd.CommandText <- """CREATE TABLE IF NOT EXISTS Game (
            GameId TEXT PRIMARY KEY,
            RoomId TEXT NOT NULL,
            HandNumber INTEGER NOT NULL DEFAULT 1,
            Phase TEXT,                        -- 'BIDDING' | 'PICKING_SKAT' | 'PLAYING' | 'SCORING'
            CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
            FOREIGN KEY (RoomId) REFERENCES GameRoom (RoomId)
        )"""
        cmd.ExecuteNonQuery() |> ignore

        cmd.CommandText <- """CREATE TABLE IF NOT EXISTS Users (
            id TEXT PRIMARY KEY,
            username TEXT NOT NULL UNIQUE,
            password_hash TEXT NOT NULL
        )"""
        cmd.ExecuteNonQuery() |> ignore

        transaction.Commit()