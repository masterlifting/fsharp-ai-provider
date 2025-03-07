[<RequireQualifiedAccess>]
module Multilang.Translator

open Infrastructure.Domain
open Infrastructure.Prelude
open Multilang.Domain

type Type = OpenAI of Multilang.OpenAI.Domain.Client

type Connection = OpenAI of Multilang.OpenAI.Domain.Connection

let init connection =
    match connection with
    | Connection.OpenAI value -> value |> OpenAI.Client.init |> Result.map Type.OpenAI

let translate (request: Request) (client: Type) : Async<Result<Response, Error'>> =

    match client with
    | Type.OpenAI client ->
        request.toPrompt ()
        |> OpenAI.Request.make client
        |> ResultAsync.bind Response.tryParse
