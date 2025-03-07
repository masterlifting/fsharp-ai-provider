[<AutoOpen>]
module Multilang.Domain.Translation

open Infrastructure.Domain
open Infrastructure.Prelude

type Item =
    { Id: string
      Value: string }

    member this.toPromptItem() = $"Id: {this.Id}, Value: {this.Value}"

    static member tryParse(value: string) =
        let parts = value.Split(',')

        if parts.Length = 2 then
            let id = parts.[0].Trim()
            let value = parts.[1].Trim()
            { Id = id; Value = value } |> Ok
        else
            $"Failed to parse item: {value}" |> Error

type Request =
    { Culture: Culture
      Items: Item seq }

    member this.toPrompt() =
        let prompt = $"Translate the following items into {this.Culture.Name} language:"

        let items =
            this.Items |> Seq.map (_.toPromptItem()) |> Seq.toList |> String.concat "\n"

        $"{prompt}\n{items}"

    static member tryParse(value: string) =
        let parts = value.Split('\n')

        if parts.Length > 1 then
            let culture = parts.[0].Trim() |> Culture.create
            let items = parts.[1..] |> Seq.map Item.tryParse |> Result.choose

            match items with
            | Ok items -> { Culture = culture; Items = items } |> Ok
            | Error errors -> $"Failed to parse items: {errors}" |> Error
        else
            $"Failed to parse request: {value}" |> Error

type Response =
    { Items: Item list }

    static member tryParse(value: string) =
        let parts = value.Split('\n')
        let items = parts |> Seq.map Item.tryParse |> Result.choose

        match items with
        | Ok items -> { Items = items |> Seq.toList } |> Ok
        | Error errors -> $"Failed to parse items: {errors}" |> NotSupported |> Error
