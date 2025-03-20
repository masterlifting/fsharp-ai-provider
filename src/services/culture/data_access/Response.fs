﻿[<AutoOpen>]
module AIProvider.Services.DataAccess.Culture.Response

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Persistence
open AIProvider.Services.Domain

// Use UTF-8 encoding for proper Cyrillic support
let private JsonOptions =
    Text.Json.JsonSerializerOptions(Encoder = Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping)

type Storage = Storage of Storage.Provider

type StorageType = FileSystem of Persistence.FileSystem.Domain.Connection

type ResponseItemEntity(item: ResponseItem) =
    new() = ResponseItemEntity({ Value = String.Empty; Result = None })

    member val Key = item.Value with get, set
    member val Value = List.empty<string> with get, set
    member val Result = item.Result with get, set

    member this.ToDomain() =
        { Value = this.Value
          Result = this.Result }

type ResponseEntity(culture: Culture, response: Response) =

    new() = ResponseEntity(Culture.createDefault (), { Items = [] })

    member val Culture = culture.Code with get, set
    member val Items = response.Items |> Seq.map ResponseItemEntity |> Array.ofSeq with get, set


module private FileSystem =
    open Persistence.FileSystem

    let private loadData = Query.Json.get<ResponseEntity>

    module Query =

        let get (request: Request) client =
            client
            |> loadData
            |> ResultAsync.map (Seq.tryFind (fun x -> x.Culture = request.Culture.Code))
            |> ResultAsync.map (
                Option.map (fun x ->
                    let responseEntityItemsMap =
                        x.Items |> Seq.map (fun item -> item.Value, item) |> Map.ofSeq

                    request.Items
                    |> Seq.map (fun requestItem ->
                        
                        match responseEntityItemsMap |> Map.tryFind requestItem.Value with
                        | Some itemEntity -> itemEntity.ToDomain()
                        | None ->
                            { Value = requestItem.Value
                              Result = None })
                    |> Seq.toList)
            )
            |> ResultAsync.map (Option.map (fun items -> { Items = items }))

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
                                let responseItemEntity = ResponseItemEntity(responseItem)

                                match responseEntityItemsMap |> Map.tryFind responseItem.Value with
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

let private toPersistenceStorage storage =
    storage
    |> function
        | Storage storage -> storage

let init storageType =
    match storageType with
    | FileSystem connection -> connection |> Storage.Connection.FileSystem |> Storage.init
    |> Result.map Storage

module internal Query =
    let get request storage =
        match storage |> toPersistenceStorage with
        | Storage.FileSystem client -> client |> FileSystem.Query.get request
        | _ -> $"Storage {storage}" |> NotSupported |> Error |> async.Return

module internal Command =
    let set culture response storage =
        match storage |> toPersistenceStorage with
        | Storage.FileSystem client -> client |> FileSystem.Command.set culture response
        | _ -> $"Storage {storage}" |> NotSupported |> Error |> async.Return
