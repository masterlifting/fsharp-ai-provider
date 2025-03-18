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
                    let untranslatedItems = cached.Items |> List.filter _.Result.IsNone

                    match untranslatedItems.Length with
                    | 0 -> cached |> Ok |> async.Return
                    | _ ->
                        let translatedItems = cached.Items |> List.filter _.Result.IsSome

                        let request =
                            { request with
                                Items = untranslatedItems |> List.map _.ToRequestItem() }

                        match deps.Provider with
                        | AIProvider.Client.Provider.OpenAI client ->
                            client
                            |> OpenAI.Service.translate request ct
                            |> ResultAsync.bindAsync (fun r -> deps.Storage |> Response.Command.set request.Culture r)
                            |> ResultAsync.map (fun r ->
                                { r with
                                    Items = r.Items @ translatedItems })
        }
