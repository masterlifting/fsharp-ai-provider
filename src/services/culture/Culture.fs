[<RequireQualifiedAccess>]
module AIProvider.Services.Culture

open Infrastructure.Domain
open Infrastructure.Prelude
open AIProvider.Services.Domain
open AIProvider.Services.DataAccess.Culture
open AIProvider.Services.Dependencies

let initDataSet dataSet ct =
    fun provider ->
        match provider with
        | AIProvider.Client.Provider.OpenAI client -> client |> OpenAI.Culture.initDataSet dataSet ct

let private requestTranslation request ct =
    fun (deps: Culture.Dependencies) ->
        match deps.Provider with
        | AIProvider.Client.Provider.OpenAI client -> client |> OpenAI.Culture.translate request ct
        |> ResultAsync.bindAsync (fun response -> deps.Storage |> Response.Command.set request.Culture response)

let translate request ct =
    fun (deps: Culture.Dependencies) ->
        let resultAsync = ResultAsyncBuilder()

        resultAsync {
            let! cache = deps.Storage |> Response.Query.get request

            return
                match cache with
                | None -> deps |> requestTranslation request ct
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
                        |> requestTranslation request ct
                        |> ResultAsync.map (fun response -> {
                            response with
                                Items = response.Items @ translatedItems
                        })
        }
