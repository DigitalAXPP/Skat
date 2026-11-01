module Tests

open System
open Skat.Game.State.Domain
open Xunit
open Skat.Game.Types
open JsonConversion
open System.Text.Json
open System.Text.Json.Serialization

[<Fact>]
let ``My test`` () =
    Assert.True(true)

[<Fact>]
let ``PlayerIDCardList JSON`` () =
    Assert.True(true)
    
let options = JsonSerializerOptions()
options.Converters.Add(PlayerCardListConverter())
options.Converters.Add(JsonFSharpConverter())

let roundTrip (v: 'a) =
    let json = JsonSerializer.Serialize(v, options)
    JsonSerializer.Deserialize<'a>(json, options)

let pid1 : PlayerId = Guid.NewGuid().ToString()
let pid2 : PlayerId = Guid.NewGuid().ToString()
let v = [ (pid1, {Suite = Hearts; Rank = Seven}); (pid2, {Suite = Clubs; Rank = Ten}) ]

[<Fact>]
let ``tuple list round-trips`` () =
    Assert.Equal<(PlayerId * Card) list>(v, roundTrip v)

[<Fact>]
let ``writes array of pairs`` () =
    let json = JsonSerializer.Serialize([ ("8dfc302d-6014-443d-b3fd-66eb33ab6cc7", {Suite = Hearts; Rank = Seven}) ], options)
    Assert.Equal("""[["8dfc302d-6014-443d-b3fd-66eb33ab6cc7",{"Suite":{"Case":"Hearts"},"Rank":{"Case":"Seven"}}]]""",
                 json)