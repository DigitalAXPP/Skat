module JsonConversion

open System.Text.Json
open System.Text.Json.Serialization
open Skat.Game.State.Domain


// let jsonOptions =
//     JsonSerializerOptions()
//     |> fun options ->
//         options.Converters.Add(JsonFSharpConverter()
//         )
//         options
        
type PositionConverter() =
    inherit JsonConverter<position>()

    override _.Write(writer, value, _options) =
        let str =
            match value with
            | ForeHand -> "ForeHand"
            | MiddleHand -> "MiddleHand"
            | RearHand -> "RearHand"
        writer.WriteStringValue(str)

    override _.Read(reader, _, _options) =
        match reader.GetString() with
        | "ForeHand" -> ForeHand
        | "MiddleHand" -> MiddleHand
        | "RearHand" -> RearHand
        | other -> failwithf "Unknown position: %s" other
        
type PhaseConverter() =
    inherit JsonConverter<phase>()

    override _.Write(writer, value, options) =
        writer.WriteStartObject()
        match value with
        | MiddlehandVSForehand ->
            writer.WriteString("Case", "MiddlehandVSForehand")
            writer.WritePropertyName("Fields")
            writer.WriteStartArray()
            writer.WriteEndArray()

        | RearhandVSMiddlehand p ->
            writer.WriteString("Case", "RearhandVSMiddlehand")
            writer.WritePropertyName("Fields")
            writer.WriteStartArray()
            JsonSerializer.Serialize(writer, p, options)
            writer.WriteEndArray()

        | RearhandVSMH (p1, p2) ->
            writer.WriteString("Case", "RearhandVSMH")
            writer.WritePropertyName("Fields")
            writer.WriteStartArray()
            JsonSerializer.Serialize(writer, p1, options)
            JsonSerializer.Serialize(writer, p2, options)
            writer.WriteEndArray()

        | RearhandVSForehand p ->
            writer.WriteString("Case", "RearhandVSForehand")
            writer.WritePropertyName("Fields")
            writer.WriteStartArray()
            JsonSerializer.Serialize(writer, p, options)
            writer.WriteEndArray()

        | RearhandVSFH (p1, p2) ->
            writer.WriteString("Case", "RearhandVSFH")
            writer.WritePropertyName("Fields")
            writer.WriteStartArray()
            JsonSerializer.Serialize(writer, p1, options)
            JsonSerializer.Serialize(writer, p2, options)
            writer.WriteEndArray()

        writer.WriteEndObject()

    override _.Read(reader, _, options) =
        use doc = JsonDocument.ParseValue(&reader)
        let root = doc.RootElement
        let case = root.GetProperty("Case").GetString()
        let fields = root.GetProperty("Fields")

        match case with
        | "MiddlehandVSForehand" ->
            MiddlehandVSForehand

        | "RearhandVSMiddlehand" ->
            let p = JsonSerializer.Deserialize<position>(fields.[0].GetRawText(), options)
            RearhandVSMiddlehand p

        | "RearhandVSMH" ->
            let p1 = JsonSerializer.Deserialize<position>(fields.[0].GetRawText(), options)
            let p2 = JsonSerializer.Deserialize<position>(fields.[1].GetRawText(), options)
            RearhandVSMH (p1, p2)

        | "RearhandVSForehand" ->
            let p = JsonSerializer.Deserialize<position>(fields.[0].GetRawText(), options)
            RearhandVSForehand p

        | "RearhandVSFH" ->
            let p1 = JsonSerializer.Deserialize<position>(fields.[0].GetRawText(), options)
            let p2 = JsonSerializer.Deserialize<position>(fields.[1].GetRawText(), options)
            RearhandVSFH (p1, p2)

        | other ->
            failwithf "Unknown phase case: %s" other
            
let jsonOptions =
    JsonSerializerOptions()
    |> fun options ->
        options.Converters.Add(PhaseConverter())
        options.Converters.Add(PositionConverter())
        options.Converters.Add(JsonFSharpConverter()
        )
        options