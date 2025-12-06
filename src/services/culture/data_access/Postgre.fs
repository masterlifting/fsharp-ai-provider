module AIProvider.Services.DataAccess.Postgre.Culture

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Infrastructure.SerDe
open Persistence.Storages
open Persistence.Storages.Postgre
open Persistence.Storages.Domain.Postgre
open AIProvider.Services.Domain
open AIProvider.Services.DataAccess

type private PostgreResponse() =
    member val Culture = String.Empty with get, set
    member val Items = String.Empty with get, set

    member this.map() =
        this.Items
        |> Json.deserialize<Culture.ResponseItemEntity[]>
        |> Result.map (fun items ->
            let entity = Culture.ResponseEntity()
            entity.Culture <- this.Culture
            entity.Items <- items
            entity)

module Query =

    let get (request: Request) (client: Client) =
        let sqlRequest = {
            Sql =
                """
                SELECT 
                    culture as "Culture", 
                    items::text as "Items"
                FROM cultures
                WHERE culture = @Culture
            """
            Params = Some {| Culture = request.Culture.Code |}
        }

        client
        |> Query.get<PostgreResponse> sqlRequest
        |> ResultAsync.map Seq.tryHead
        |> ResultAsync.bind (function
            | Some response -> response.map () |> Result.map Some
            | None -> Ok None)
        |> ResultAsync.map (
            Option.map (fun response ->
                request.Items
                |> Seq.map (fun requestItem ->

                    let requestItemKey, requestItemValues =
                        requestItem.Value |> Culture.serialize request.Shield.Values

                    match
                        response.Items
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

    let loadData (client: Client) =
        client
        |> Query.get<PostgreResponse> {
            Sql =
                """
                SELECT 
                    culture as "Culture", 
                    items::text as "Items" 
                FROM cultures
            """
            Params = None
        }
        |> ResultAsync.bind (Seq.map _.map() >> Result.choose)
        |> ResultAsync.map Array.ofSeq

module Command =
    // Use UTF-8 encoding for proper Cyrillic support
    let private JsonOptions =
        Text.Json.JsonSerializerOptions(Encoder = Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping)

    let private updateItems (responseEntity: Culture.ResponseEntity) (response: Response) =

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

                    let responseItemEntity = Culture.ResponseItemEntity responseItem

                    match responseEntityItemsMap |> Map.tryFind responseItemKey with
                    | Some riIndex ->
                        updatedResponseItemEntities[riIndex] <- responseItemEntity
                        acc
                    | None -> responseItemEntity :: acc)
                []
            |> List.rev
            |> Array.ofList

        Array.append updatedResponseItemEntities newResponseItemEntities

    let set (culture: Culture) (response: Response) (client: Client) =
        let request = {
            Sql =
                """
                SELECT 
                    culture as "Culture", 
                    items::text as "Items"
                FROM cultures
                WHERE culture = @Culture
            """
            Params = Some {| Culture = culture.Code |}
        }

        client
        |> Postgre.Query.get<PostgreResponse> request
        |> ResultAsync.map Seq.tryHead
        |> ResultAsync.bind (function
            | Some response -> response.map () |> Result.map Some
            | None -> Ok None)
        |> ResultAsync.bind (fun opt ->
            match opt with
            | None -> response |> updateItems (Culture.ResponseEntity(culture, response))
            | Some responseEntity -> response |> updateItems responseEntity
            |> Json.serialize' JsonOptions
            |> Result.map (fun itemsJson -> {
                Sql =
                    """
                        INSERT INTO cultures (culture, items)
                        VALUES (@Culture, @Items::jsonb)
                        ON CONFLICT (culture)
                        DO UPDATE SET items = EXCLUDED.items
                    """
                Params =
                    Some {|
                        Culture = culture.Code
                        Items = itemsJson
                    |}
            }))
        |> ResultAsync.bindAsync (fun cmd -> client |> Command.execute cmd)
        |> ResultAsync.map (fun _ -> response)

module Migrations =
    let private resultAsync = ResultAsyncBuilder()

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

    let apply (connectionString: string) =
        resultAsync {
            let! client =
                Provider.init {
                    String = connectionString
                    Lifetime = Persistence.Domain.Transient
                }
                |> async.Return

            do! client |> initial
            return client |> Provider.dispose |> Ok |> async.Return
        }
