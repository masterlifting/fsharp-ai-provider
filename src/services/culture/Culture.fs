[<RequireQualifiedAccess>]
module AIProvider.Services.Culture

open Infrastructure.Domain
open Infrastructure.Prelude
open AIProvider.Services.Domain
open AIProvider.Services.DataAccess
open AIProvider.Services.Dependencies

let private resultAsync = ResultAsyncBuilder()
let setContext ct =
    fun (deps: Culture.Dependencies) ->
        match deps.Provider with
        | AIProvider.Client.Provider.OpenAI client -> (client, deps.Storage) |> OpenAI.Culture.setContext ct

let translate request ct =
    fun (deps: Culture.Dependencies) ->

        let inline perform request ct =
            fun (deps: Culture.Dependencies) ->
                match deps.Provider with
                | AIProvider.Client.Provider.OpenAI client -> client |> OpenAI.Culture.translate request ct
                |> ResultAsync.bindAsync (fun response ->
                    deps.Storage |> Storage.Culture.Command.set request.Culture response)

        resultAsync {
            let! cache = deps.Storage |> Storage.Culture.Query.get request

            return
                match cache with
                | None -> deps |> perform request ct
                | Some cached ->
                    let untranslatedItems, translatedItems =
                        cached.Items |> List.partition _.Result.IsNone

                    match untranslatedItems.Length with
                    | 0 -> cached |> Ok |> async.Return
                    | _ ->
                        let request = {
                            request with
                                Items = untranslatedItems |> List.map (fun i -> { Value = i.Value })
                        }

                        deps
                        |> perform request ct
                        |> ResultAsync.map (fun response -> {
                            response with
                                Items = response.Items @ translatedItems
                        })
        }
