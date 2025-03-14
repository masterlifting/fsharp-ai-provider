[<AutoOpen>]
module AIProvider.Services.Culture.DataAccess.CultureItem

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Persistence
open AIProvider.Services.Culture.Domain

type CultureItemStorage = CultureItemStorage of Storage.Provider

type StorageType = FileSystem of Persistence.FileSystem.Domain.Connection

type CultureItemEntity() =
    member val Id = String.Empty with get, set
    member val Value = String.Empty with get, set
    member val Result = String.Empty with get, set
    member val Version = 0 with get, set

    member this.ToDomain() = { Id = this.Id; Value = this.Result }

let private toPersistenceStorage storage =
    storage
    |> function
        | CultureItemStorage storage -> storage

let init storageType =
    match storageType with
    | FileSystem connection -> connection |> Storage.Connection.FileSystem |> Storage.init
    |> Result.map CultureItemStorage

module private FileSystem =
    open Persistence.FileSystem

    let private loadData = Query.Json.get<CultureItemEntity>

    module Query =

        let findManyByIds (ids: string seq) client =
            let requestIds = ids |> Set.ofSeq

            client
            |> loadData
            |> ResultAsync.map (Seq.filter (fun x -> requestIds.Contains x.Id))
            |> ResultAsync.map (Seq.map _.ToDomain())

module Query =

    let findManyByIds ids storage =
        match storage |> toPersistenceStorage with
        | Storage.FileSystem client -> client |> FileSystem.Query.findManyByIds ids
        | _ -> $"Storage {storage}" |> NotSupported |> Error |> async.Return
