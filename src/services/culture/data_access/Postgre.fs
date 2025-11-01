module AIProvider.Services.DataAccess.Postgre.Culture

open Infrastructure.Domain
open Infrastructure.Prelude
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

            let! result = client |> Query.get<Culture.ResponseEntity> sql |> ResultAsync.map Seq.tryHead

            return
                result
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
        |> Query.get<Culture.ResponseEntity> {
            Sql = "SELECT culture, items FROM cultures"
            Params = None
        }

module Command =

    let set (culture: Culture) (response: Response) (client: Client) =
        async {
            // Load existing data
            let! existingResult =
                client
                |> Persistence.Storages.Postgre.Query.get<Culture.ResponseEntity> {
                    Sql = "SELECT culture, items FROM cultures"
                    Params = None
                }

            match existingResult with
            | Error err -> return Error err
            | Ok data ->
                let updatedData =
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

                        data

                // Now save the updated data by upserting each entity
                let! saveResult =
                    updatedData
                    |> Seq.map (fun entity ->
                        client
                        |> Command.execute {
                            Sql =
                                """
                                    INSERT INTO cultures (culture, items)
                                    VALUES (@Culture, @Items::jsonb)
                                    ON CONFLICT (culture) DO UPDATE SET
                                        items = EXCLUDED.items
                                """
                            Params = Some entity
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
