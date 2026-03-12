namespace Skat.Data

open SQLite

module Usermanagement =
    [<CLIMutable>]
    type User = {
        [<PrimaryKey; AutoIncrement>]
        Id: int
        Name: string
        Email: string
        PasswordHash: string
    }