﻿[<AutoOpen>]
module AIProvider.Services.DataAccess.Culture.Response

open System
open System.Text.RegularExpressions
open Infrastructure.Domain
open Infrastructure.Prelude
open Persistence
open Persistence.Storages
open Persistence.Storages.Domain
open AIProvider.Services.Domain

// Use UTF-8 encoding for proper Cyrillic support
let private JsonOptions =
    Text.Json.JsonSerializerOptions(Encoder = Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping)

type Storage = Provider of Storage.Provider
type StorageType = FileSystem of FileSystem.Connection

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
let inline private serialize shield text =
    Regex.Matches(text, shield |> toPattern)
    |> List.ofSeq
    |> List.mapi (fun i x -> i, x.Value)
    |> List.fold (fun (key: string, values) (i, value) -> key.Replace(value, i |> toValue), value :: values) (text, [])
    |> fun (key, values) -> key, values |> List.rev

//TODO: Add support Result type
let inline private deserialize values result =
    result
    |> Option.map (fun result ->
        values
        |> List.mapi (fun i x -> i, x)
        |> List.fold (fun (result: string) (i, value) -> result.Replace(i |> toValue, value)) result)

module private FileSystem =
    open Persistence.Storages.FileSystem

    let private loadData = Query.Json.get<ResponseEntity>

    module Query =

        let get (request: Request) client =
            client
            |> loadData
            |> ResultAsync.map (Seq.tryFind (fun x -> x.Culture = request.Culture.Code))
            |> ResultAsync.map (
                Option.map (fun x ->
                    request.Items
                    |> Seq.map (fun requestItem ->

                        let requestItemKey, requestItemValues =
                            requestItem.Value |> serialize request.Shield.Values

                        match
                            x.Items
                            |> Seq.map (fun item -> item.Value, item)
                            |> Map.ofSeq
                            |> Map.tryFind requestItemKey
                        with
                        | Some itemEntity -> {
                            Value = requestItem.Value
                            Result = itemEntity.Result |> deserialize requestItemValues
                          }
                        | None -> {
                            Value = requestItem.Value
                            Result = None
                          })
                    |> Seq.toList)
            )
            |> ResultAsync.map (
                Option.map (fun items -> {
                    Shield = request.Shield
                    Items = items
                })
            )

    module Command =
        let set (culture: Culture) (response: Response) client =
            client
            |> loadData
            |> ResultAsync.map (fun data ->
                match data |> Seq.tryFindIndex (fun x -> x.Culture = culture.Code) with
                | None -> data |> Array.append [| ResponseEntity(culture, response) |]
                | Some rIndex ->
                    let responseEntity = data[rIndex]

                    let responseEntityItemsMap =
                        responseEntity.Items |> Seq.mapi (fun i item -> item.Value, i) |> Map.ofSeq

                    let updatedResponseItemEntities = Array.copy responseEntity.Items

                    let newResponseItemEntities =
                        response.Items
                        |> Seq.fold
                            (fun acc responseItem ->
                                let responseItemKey =
                                    responseItem.Value |> (serialize response.Shield.Values >> fst)

                                let responseItemResult =
                                    responseItem.Result |> Option.map (serialize response.Shield.Values >> fst)

                                let responseItem = {
                                    Value = responseItemKey
                                    Result = responseItemResult
                                }

                                let responseItemEntity = ResponseItemEntity(responseItem)

                                match responseEntityItemsMap |> Map.tryFind responseItemKey with
                                | Some riIndex ->
                                    updatedResponseItemEntities[riIndex] <- responseItemEntity
                                    acc
                                | None -> responseItemEntity :: acc)
                            []
                        |> List.rev
                        |> Array.ofList

                    responseEntity.Items <- updatedResponseItemEntities |> Array.append newResponseItemEntities

                    data)
            |> ResultAsync.bindAsync (fun data -> client |> Command.Json.save' data JsonOptions)
            |> ResultAsync.map (fun _ -> response)

let private toProvider =
    function
    | Provider provider -> provider

let init storageType =
    match storageType with
    | FileSystem connection -> connection |> Storage.Connection.FileSystem |> Storage.init
    |> Result.map Provider

module internal Query =
    let get request storage =
        let provider = storage |> toProvider
        match provider with
        | Storage.FileSystem client -> client |> FileSystem.Query.get request
        | _ -> $"The '{provider}' is not supported." |> NotSupported |> Error |> async.Return

module internal Command =
    let set culture response storage =
        let provider = storage |> toProvider
        match provider with
        | Storage.FileSystem client -> client |> FileSystem.Command.set culture response
        | _ -> $"The '{provider}' is not supported." |> NotSupported |> Error |> async.Return
