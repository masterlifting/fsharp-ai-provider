[<RequireQualifiedAccess>]
module Multilang.Translator

open Infrastructure.Domain
open Multilang.Domain

module Text =

    let translate (culture: Culture) (text: string) =
        match culture with
        | Culture.English -> "Multilang.Translator.Text.translate" |> NotSupported |> Error |> async.Return
        | Culture.Russian -> "Multilang.Translator.Text.translate" |> NotSupported |> Error |> async.Return
    