[<AutoOpen>]
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

type Storage = Storage of Storage.Provider

type StorageType = FileSystem of FileSystem.Connection

type ResponseItemEntity(item: ResponseItem) =
    new() = ResponseItemEntity({ Value = String.Empty; Result = None })

    member val Value = item.Value with get, set
    member val Result = item.Result with get, set

type ResponseEntity(culture: Culture, response: Response) =

    new() =
        ResponseEntity(
            Culture.createDefault (),
            { Placeholder = Placeholder.create ''' '''
              Items = [] }
        )

    member val Culture = culture.Code with get, set
    member val Items = response.Items |> Seq.map ResponseItemEntity |> Array.ofSeq with get, set

let inline private toPattern (left, right) = $"%c{left}([^%c{right}]*)'"
let inline private toValuePlaceholder index = $"[%d{index}]"

//TODO: Add support Result type
let inline private serialize placeholder text =
    Regex.Matches(text, placeholder |> toPattern)
    |> Seq.mapi (fun i x -> i, x.Value)
    |> Seq.fold
        (fun (key: string, values) (i, value) -> key.Replace(value, i |> toValuePlaceholder), value :: values)
        (text, [])

//TODO: Add support Result type
let inline private deserialize values result =
    result
    |> Option.map (fun result ->
        values
        |> Seq.mapi (fun i x -> i, x)
        |> Seq.fold (fun (result: string) (i, value) -> result.Replace(i |> toValuePlaceholder, value)) result)

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
                            requestItem.Value |> serialize request.Placeholder.Values

                        match
                            x.Items
                            |> Seq.map (fun item -> item.Value, item)
                            |> Map.ofSeq
                            |> Map.tryFind requestItemKey
                        with
                        | Some itemEntity ->
                            { Value = requestItem.Value
                              Result = itemEntity.Result |> deserialize requestItemValues }
                        | None ->
                            { Value = requestItem.Value
                              Result = None })
                    |> Seq.toList)
            )
            |> ResultAsync.map (
                Option.map (fun items ->
                    { Placeholder = request.Placeholder
                      Items = items })
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
                                    responseItem.Value |> (serialize response.Placeholder.Values >> fst)

                                let responseItemResult =
                                    responseItem.Result |> Option.map (serialize response.Placeholder.Values >> fst)

                                let responseItem =
                                    { Value = responseItemKey
                                      Result = responseItemResult }

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
