module Skat.Auth.Persistence.Infrastructure.DapperConfig

open System
open Dapper

type GuidHandler() =
    inherit SqlMapper.TypeHandler<Guid>()

    override _.SetValue(parameter, value) =
        parameter.Value <- value.ToString()

    override _.Parse(value) =
        match value with
        | :? string as s -> Guid.Parse(s)
        | :? Guid as g -> g
        | _ -> failwith $"Cannot convert {value} to Guid"

let register () =
    SqlMapper.AddTypeHandler(GuidHandler())