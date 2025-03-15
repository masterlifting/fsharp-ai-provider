[<RequireQualifiedAccess>]
module AIProvider.Services.Culture.Service

open Infrastructure.Domain
open Infrastructure.Prelude
open AIProvider.Services.Domain
open AIProvider.Services.DataAccess.Culture
open AIProvider.Services.Dependencies

let tryGetFromStorage (request: Culture.Request) (storage: Response.Storage) =
    storage |> Response.Query.tryGet request

let translate request ct =
    fun (deps: Culture.Dependencies) ->
        let resultAsync = ResultAsyncBuilder()

        resultAsync {
            let! storageResult = deps.Storage |> tryGetFromStorage request

            return
                match storageResult with
                | None ->
                    match deps.Provider with
                    | AIProvider.Client.Provider.OpenAI client -> client |> OpenAI.Service.translate request ct
                | Some cached ->
                    let translatedItems = cached.Items |> List.filter _.Result.IsSome
                    let untranslatedItems = cached.Items |> List.filter _.Result.IsNone

                    let request =
                        { request with
                            Items = untranslatedItems |> List.map _.ToRequestItem() }

                    match deps.Provider with
                    | AIProvider.Client.Provider.OpenAI client ->
                        client
                        |> OpenAI.Service.translate request ct
                        |> ResultAsync.map (fun x ->
                            let items = translatedItems @ x.Items
                            { x with Items = items })
        }
