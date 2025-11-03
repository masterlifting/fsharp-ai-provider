module AIProvider.Services.DataAccess.Postgre.Culture

open Infrastructure.Domain
open Infrastructure.Prelude
open System.Text.Json
open Persistence.Storages.Postgre
open Persistence.Storages.Domain.Postgre
open AIProvider.Services.Domain
open AIProvider.Services.DataAccess

module Query =

    let get (request: Request) (client: Client) =
        async {
            let sql = {
                Sql =
                    """
                        SELECT culture, items
                        FROM cultures
                        WHERE culture = @Culture
                    """
                Params = Some {| Culture = request.Culture.Code |}
            }

            let! result =
                client
                |> Persistence.Storages.Postgre.Query.get<{| Culture: string; Items: string |}> sql
                |> ResultAsync.map Seq.tryHead

            return
                result
                |> Result.bind (fun rowOption ->
                    match rowOption with
                    | None -> Ok None
                    | Some row ->
                        try
                            let deserializedItems =
                                JsonSerializer.Deserialize<Culture.ResponseItemEntity[]>(row.Items)
                            match deserializedItems with
                            | null ->
                                Error(
                                    Infrastructure.Domain.Error.Operation {
                                        Message = "Failed to deserialize culture data: null result"
                                        Code = None
                                    }
                                )
                            | items ->
                                let responseItems =
                                    items
                                    |> Array.map (fun entity -> {
                                        Value = entity.Value
                                        Result = entity.Result
                                    })
                                    |> Array.toList

                                let responseEntity =
                                    Culture.ResponseEntity(
                                        request.Culture,
                                        {
                                            Shield = request.Shield
                                            Items = responseItems
                                        }
                                    )

                                Ok(Some responseEntity)
                        with ex ->
                            Error(
                                Infrastructure.Domain.Error.Operation {
                                    Message = $"Failed to deserialize culture data: {ex.Message}"
                                    Code = None
                                }
                            ))
                |> Result.map (
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
                |> Result.map (
                    Option.map (fun items -> {
                        Shield = request.Shield
                        Items = items
                    })
                )
        }

    let loadData (client: Client) =
        client
        |> Persistence.Storages.Postgre.Query.get<{| Culture: string; Items: string |}> {
            Sql = "SELECT culture, items FROM cultures"
            Params = None
        }
        |> ResultAsync.map (
            Seq.map (fun row ->
                try
                    let deserializedItems =
                        JsonSerializer.Deserialize<Culture.ResponseItemEntity[]>(row.Items)
                    match deserializedItems with
                    | null ->
                        Culture.ResponseEntity(
                            Culture.parse row.Culture,
                            {
                                Shield = Shield.create ''' '''
                                Items = []
                            }
                        )
                    | items ->
                        let responseItems =
                            items
                            |> Array.map (fun entity -> {
                                Value = entity.Value
                                Result = entity.Result
                            })
                            |> Array.toList

                        Culture.ResponseEntity(
                            Culture.parse row.Culture,
                            {
                                Shield = Shield.create ''' '''
                                Items = responseItems
                            }
                        )
                with ex ->
                    Culture.ResponseEntity(
                        Culture.parse row.Culture,
                        {
                            Shield = Shield.create ''' '''
                            Items = []
                        }
                    ))
        )
        |> ResultAsync.map Seq.toArray

module Command =

    let set (culture: Culture) (response: Response) (client: Client) =
        async {
            // Load existing data
            let! existingResult =
                client
                |> Persistence.Storages.Postgre.Query.get<{| Culture: string; Items: string |}> {
                    Sql = "SELECT culture, items FROM cultures"
                    Params = None
                }
                |> ResultAsync.map (
                    Seq.map (fun row ->
                        try
                            let deserializedItems =
                                JsonSerializer.Deserialize<Culture.ResponseItemEntity[]>(row.Items)
                            match deserializedItems with
                            | null ->
                                Culture.ResponseEntity(
                                    Culture.parse row.Culture,
                                    { Shield = response.Shield; Items = [] }
                                )
                            | items ->
                                let responseItems =
                                    items
                                    |> Array.map (fun entity -> {
                                        Value = entity.Value
                                        Result = entity.Result
                                    })
                                    |> Array.toList

                                Culture.ResponseEntity(
                                    Culture.parse row.Culture,
                                    {
                                        Shield = response.Shield
                                        Items = responseItems
                                    }
                                )
                        with ex ->
                            Culture.ResponseEntity(Culture.parse row.Culture, { Shield = response.Shield; Items = [] }))
                )

            match existingResult with
            | Error err -> return Error err
            | Ok data ->
                let dataArray = data |> Seq.toArray
                let updatedData =
                    match dataArray |> Array.tryFindIndex (fun x -> x.Culture = culture.Code) with
                    | None -> dataArray |> Array.append [| Culture.ResponseEntity(culture, response) |]
                    | Some rIndex ->
                        // Replace the entire response for this culture
                        dataArray[rIndex] <- Culture.ResponseEntity(culture, response)
                        dataArray

                // Now save the updated data by upserting each entity
                let! saveResult =
                    updatedData
                    |> Seq.map (fun entity ->
                        async {
                            let itemsJson =
                                try
                                    JsonSerializer.Serialize(entity.Items)
                                with ex ->
                                    "[]"

                            return!
                                client
                                |> Command.execute {
                                    Sql =
                                        """
                                            INSERT INTO cultures (culture, items)
                                            VALUES (@Culture, @Items::jsonb)
                                            ON CONFLICT (culture) DO UPDATE SET
                                                items = EXCLUDED.items
                                        """
                                    Params =
                                        Some {|
                                            Culture = entity.Culture
                                            Items = itemsJson
                                        |}
                                }
                        })
                    |> Async.Sequential
                    |> Async.map (Array.toList >> Result.choose)

                return saveResult |> Result.map (fun _ -> response)
        }

module Migrations =

    let private initial (client: Client) =
        async {
            let migration = {
                Sql =
                    """
                        CREATE TABLE IF NOT EXISTS cultures (
                            culture TEXT PRIMARY KEY,
                            items JSONB NOT NULL
                        )
                    """
                Params = None
            }

            return! client |> Command.execute migration |> ResultAsync.map ignore
        }

    let private clean (client: Client) =
        async {
            client |> Provider.dispose
            return Ok()
        }

    let apply (connectionString: string) =
        {
            String = connectionString
            Lifetime = Persistence.Domain.Transient
        }
        |> Provider.init
        |> ResultAsync.wrap (fun client -> client |> initial |> ResultAsync.apply (client |> clean))
