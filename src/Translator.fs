[<RequireQualifiedAccess>]
module Multilang.Translator

open Infrastructure.Domain
open Multilang.Domain

let translate (request: Request) : Async<Result<Response, Error'>> =
    match request.Culture with
    | English -> "Multilang.Translator.Text.translate" |> NotSupported |> Error |> async.Return
    | Russian -> "Multilang.Translator.Text.translate" |> NotSupported |> Error |> async.Return
