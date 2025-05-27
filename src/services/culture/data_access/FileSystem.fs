module internal AIProvider.Services.DataAccess.FileSystem.Culture

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Persistence.Storages.FileSystem
open AIProvider.Services.Domain
open AIProvider.Services.DataAccess

// Use UTF-8 encoding for proper Cyrillic support
let private JsonOptions =
    Text.Json.JsonSerializerOptions(Encoder = Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping)

let private loadData = Query.Json.get<Culture.ResponseEntity>

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
                        requestItem.Value |> Culture.serialize request.Shield.Values

                    match
                        x.Items
                        |> Seq.map (fun item -> item.Value, item)
                        |> Map.ofSeq
                        |> Map.tryFind requestItemKey
                    with
                    | Some itemEntity -> {
                        Value = requestItem.Value
                        Result = itemEntity.Result |> Culture.deserialize requestItemValues
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

    let getContext client = client |> loadData

module Command =
    let set (culture: Culture) (response: Response) client =
        client
        |> loadData
        |> ResultAsync.map (fun data ->
            match data |> Seq.tryFindIndex (fun x -> x.Culture = culture.Code) with
            | None -> data |> Array.append [| Culture.ResponseEntity(culture, response) |]
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
                                responseItem.Value |> (Culture.serialize response.Shield.Values >> fst)

                            let responseItemResult =
                                responseItem.Result
                                |> Option.map (Culture.serialize response.Shield.Values >> fst)

                            let responseItem = {
                                Value = responseItemKey
                                Result = responseItemResult
                            }

                            let responseItemEntity = Culture.ResponseItemEntity(responseItem)

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
