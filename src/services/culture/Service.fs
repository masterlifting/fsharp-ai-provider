[<RequireQualifiedAccess>]
module AIProvider.Services.Culture.Service

open Infrastructure.Domain
open Infrastructure.Prelude
open AIProvider.Services.Domain
open AIProvider.Services.DataAccess.Culture
open AIProvider.Services.Dependencies

let translate request ct =
    fun (deps: Culture.Dependencies) ->
        let resultAsync = ResultAsyncBuilder()

        resultAsync {
            let! storageResult = deps.Storage |> Response.Query.get request

            return
                match storageResult with
                | None ->
                    match deps.Provider with
                    | AIProvider.Client.Provider.OpenAI client ->
                        client
                        |> OpenAI.Service.translate request ct
                        |> ResultAsync.bindAsync (fun r -> deps.Storage |> Response.Command.set request.Culture r)
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
                        |> ResultAsync.bindAsync (fun r -> deps.Storage |> Response.Command.set request.Culture r)
        }
