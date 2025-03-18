[<AutoOpen>]
module AIProvider.Services.DataAccess.Culture.Response

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Persistence
open AIProvider.Services.Domain

type Storage = Storage of Storage.Provider

type StorageType = FileSystem of Persistence.FileSystem.Domain.Connection

type ResponseItemEntity(item: ResponseItem) =
    new() =
        ResponseItemEntity(
            { Id = String.Empty
              Value = String.Empty
              Result = None }
        )

    member val Id = item.Id with get, set
    member val Value = item.Value with get, set
    member val Result = item.Result with get, set

    member this.ToDomain() =
        { Id = this.Id
          Value = this.Value
          Result = this.Result }

type ResponseEntity(culture: Culture, response: Response) =

    new() = ResponseEntity(Culture.createDefault (), { Items = [] })

    member val Culture = culture.Code with get, set
    member val Items = response.Items |> Seq.map ResponseItemEntity |> Array.ofSeq with get, set

let private toPersistenceStorage storage =
    storage
    |> function
        | Storage storage -> storage

let init storageType =
    match storageType with
    | FileSystem connection -> connection |> Storage.Connection.FileSystem |> Storage.init
    |> Result.map Storage

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
                    request.Items
                    |> Seq.map (fun requestItem ->
                        match x.Items |> Seq.tryFind (fun responseItem -> responseItem.Id = requestItem.Id) with
                        | None ->
                            { Id = requestItem.Id
                              Value = requestItem.Value
                              Result = None }
                        | Some itemEntity -> itemEntity.ToDomain())
                    |> Seq.toList)
            )
            |> ResultAsync.map (Option.map (fun resultItems -> { Items = resultItems }))

    module Command =
        let set (culture: Culture) (response: Response) client =
            client
            |> loadData
            |> ResultAsync.map (fun data ->
                match data |> Seq.tryFindIndex (fun x -> x.Culture = culture.Code) with
                | None -> [| ResponseEntity(culture, response) |]
                | Some index ->
                    let responseEntity = data[index]

                    let newResponseItemEntities =
                        response.Items
                        |> Seq.map (fun responseItem ->
                            match responseEntity.Items |> Seq.tryFind (fun x -> x.Id = responseItem.Id) with
                            | None -> Some <| ResponseItemEntity(responseItem)
                            | Some itemEntity -> None)
                        |> Seq.choose id
                        |> Array.ofSeq

                    responseEntity.Items <- (responseEntity.Items |> Array.append newResponseItemEntities)
                    data)
            |> ResultAsync.bindAsync (fun data -> client |> Command.Json.save data)
            |> ResultAsync.map (fun _ -> response)

module Query =
    let get request storage =
        match storage |> toPersistenceStorage with
        | Storage.FileSystem client -> client |> FileSystem.Query.get request
        | _ -> $"Storage {storage}" |> NotSupported |> Error |> async.Return

module Command =
    let set culture response storage =
        match storage |> toPersistenceStorage with
        | Storage.FileSystem client -> client |> FileSystem.Command.set culture response
        | _ -> $"Storage {storage}" |> NotSupported |> Error |> async.Return
