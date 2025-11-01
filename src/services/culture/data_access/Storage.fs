[<RequireQualifiedAccess>]
module AIProvider.Services.DataAccess.Storage.Culture

open Infrastructure.Domain
open Persistence
open Persistence.Storages
open Persistence.Storages.Domain
open AIProvider.Services.Domain
open AIProvider.Services.DataAccess

type StorageType =
    | FileSystem of FileSystem.Connection
    | Postgre of Postgre.Connection

let private toProvider =
    function
    | Culture.Provider provider -> provider

let init storageType =
    match storageType with
    | FileSystem connection -> connection |> Storage.Connection.FileSystem |> Storage.init
    | Postgre connection ->
        Storage.Connection.Database {
            Database.Database = Database.Postgre connection.String
            Database.Lifetime = connection.Lifetime
        }
        |> Storage.init
    |> Result.map Culture.Provider

module internal Query =
    let get request storage =
        let provider = storage |> toProvider
        match provider with
        | Storage.FileSystem client -> client |> FileSystem.Culture.Query.get request
        | Storage.Database database ->
            match database with
            | Database.Client.Postgre client -> client |> Postgre.Culture.Query.get request
        | _ -> $"The '{provider}' is not supported." |> NotSupported |> Error |> async.Return

    let loadData storage =
        let provider = storage |> toProvider
        match provider with
        | Storage.FileSystem client -> client |> FileSystem.Culture.Query.loadData
        | Storage.Database database ->
            match database with
            | Database.Client.Postgre client -> client |> Postgre.Culture.Query.loadData
        | _ -> $"The '{provider}' is not supported." |> NotSupported |> Error |> async.Return

module internal Command =
    let set culture response storage =
        let provider = storage |> toProvider
        match provider with
        | Storage.FileSystem client -> client |> FileSystem.Culture.Command.set culture response
        | Storage.Database database ->
            match database with
            | Database.Client.Postgre client -> client |> Postgre.Culture.Command.set culture response
        | _ -> $"The '{provider}' is not supported." |> NotSupported |> Error |> async.Return
