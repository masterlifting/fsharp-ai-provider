[<AutoOpen>]
module Multilang.Domain.Culture

type Culture =
    | English
    | Russian

    static member create value =
        match value with
        | "RU" -> Russian
        | _ -> English

    member this.Code =
        match this with
        | English -> "EN"
        | Russian -> "RU"
    
    member this.Name =
        match this with
        | English -> "English"
        | Russian -> "Russian"

    static member createDefault() = English
