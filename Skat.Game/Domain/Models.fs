namespace Skat.Game.Domain

type Player = {
    Id: int
    Name: string
}

[<CLIMutable>]
type GameRoom = {
    RoomId: string
    MaxPlayer: int
    CurrentPlayer: int
}