[<RequireQualifiedAccess>]
module Multilang.Translator

open Infrastructure.Domain
open Multilang.Domain

let translate (request: Translation.Input) : Async<Result<Translation.Item list, Error'>> =
    match request.Culture with
    | Culture.English -> "Multilang.Translator.Text.translate" |> NotSupported |> Error |> async.Return
    | Culture.Russian -> "Multilang.Translator.Text.translate" |> NotSupported |> Error |> async.Return
    