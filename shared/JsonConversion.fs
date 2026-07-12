module JsonConversion

open System.Text.Json
open System.Text.Json.Serialization


let jsonOptions =
    JsonSerializerOptions()
    |> fun options ->
        options.Converters.Add(JsonFSharpConverter())
        options