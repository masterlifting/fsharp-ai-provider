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
    member val Version = 0 with get, set

    member this.ToDomain() =
        { Id = this.Id
          Value = this.Value
          Result = this.Result }

type ResponseEntity(culture: Culture, response: Response) =

    new() = ResponseEntity(Culture.createDefault (), { Items = [] })

    member val Culture = culture.Code with get, set
    member val Items = response.Items with get, set

let private toPersistenceStorage storage =
    storage
    |> function
        | Storage storage -> storage

let init storageType =
    match storageType with
    | FileSystem connection -> connection |> Storage.Connection.FileSystem |> Storage.init
    |> Result.map Storage

module private Common =

    let create (culture: Culture) (response: Response) (data: ResponseEntity array) =
        match data |> Array.exists (fun x -> x.Culture = culture.Code) with
        | true -> $"{culture}" |> AlreadyExists |> Error
        | false -> data |> Array.append [| ResponseEntity(culture, response) |] |> Ok

    let update (culture: Culture) (response: Response) (data: ResponseEntity array) =
        match data |> Array.tryFindIndex (fun x -> x.Id = response.Id.Value) with
        | Some index ->
            data[index] <- response.ToEntity()
            Ok data
        | None -> $"{response.Id}" |> NotFound |> Error

    let delete (culture: Culture) (data: ResponseEntity array) =
        match data |> Array.tryFindIndex (fun x -> x.Id = culture.Value) with
        | Some index -> data |> Array.removeAt index |> Ok
        | None -> $"{culture}" |> NotFound |> Error

module private FileSystem =
    open Persistence.FileSystem

    let private loadData = Query.Json.get<ResponseEntity>

    module Query =

        let tryGet (request: Request) client =
            client
            |> loadData
            |> ResultAsync.map (Seq.tryFind (fun x -> x.Culture = request.Culture.Code))
            |> ResultAsync.map (
                Option.map (fun x ->
                    request.Items
                    |> Seq.map (fun requestItem ->
                        match
                            x.Items
                            |> Seq.filter (fun responseItem -> responseItem.Version = 0)
                            |> Seq.tryFind (fun responseItem -> responseItem.Id = requestItem.Id)
                        with
                        | None ->
                            { Id = requestItem.Id
                              Value = requestItem.Value
                              Result = None }
                        | Some responseItemEntity -> responseItemEntity.ToDomain())
                    |> Seq.toList)
            )
            |> ResultAsync.map (Option.map (fun resultItems -> { Items = resultItems }))

    module Command =
        let save culture response client =
            client
            |> loadData
            |> ResultAsync.bind (Common.create culture response)
            |> ResultAsync.bindAsync (fun data -> client |> Command.Json.save data)
            |> ResultAsync.map (fun _ -> response)

module Query =

    let tryGet request storage =
        match storage |> toPersistenceStorage with
        | Storage.FileSystem client -> client |> FileSystem.Query.tryGet request
        | _ -> $"Storage {storage}" |> NotSupported |> Error |> async.Return

module Command =
    let save culture response storage =
        match storage |> toPersistenceStorage with
        | Storage.FileSystem client -> client |> FileSystem.Command.save culture response
        | _ -> $"Storage {storage}" |> NotSupported |> Error |> async.Return
