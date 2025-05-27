module AIProvider.Services.DataAccess.Culture

open System
open System.Text.RegularExpressions
open Infrastructure.Domain
open Persistence

open AIProvider.Services.Domain

type Storage = Provider of Storage.Provider

type ResponseItemEntity(item: ResponseItem) =
    new() = ResponseItemEntity({ Value = String.Empty; Result = None })

    member val Value = item.Value with get, set
    member val Result = item.Result with get, set

type ResponseEntity(culture: Culture, response: Response) =

    new() =
        ResponseEntity(
            Culture.createDefault (),
            {
                Shield = Shield.create ''' '''
                Items = []
            }
        )

    member val Culture = culture.Code with get, set
    member val Items = response.Items |> Seq.map ResponseItemEntity |> Array.ofSeq with get, set

let inline private toPattern (left, right) = $"%c{left}([^%c{right}]*)'"
let inline private toValue index = $"[%d{index}]"

//TODO: Add support Result type
let inline internal serialize shield text =
    Regex.Matches(text, shield |> toPattern)
    |> List.ofSeq
    |> List.mapi (fun i x -> i, x.Value)
    |> List.fold (fun (key: string, values) (i, value) -> key.Replace(value, i |> toValue), value :: values) (text, [])
    |> fun (key, values) -> key, values |> List.rev

//TODO: Add support Result type
let inline internal deserialize values result =
    result
    |> Option.map (fun result ->
        values
        |> List.mapi (fun i x -> i, x)
        |> List.fold (fun (result: string) (i, value) -> result.Replace(i |> toValue, value)) result)
