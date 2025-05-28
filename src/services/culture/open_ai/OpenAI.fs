module AIProvider.Services.OpenAI.Culture

open Infrastructure.SerDe
open Infrastructure.Prelude
open AIProvider.Clients.OpenAI
open AIProvider.Services.Domain
open AIProvider.Services.DataAccess
open AIProvider.Services.Domain.OpenAI

let setContext ct =
    fun (client, storage) ->
        storage
        |> Storage.Culture.Query.loadData
        |> ResultAsync.bindAsync (function
            | [||] ->
                // No existing translations, nothing to set as context
                Ok() |> async.Return
            | dataSet ->
                dataSet
                |> Json.serialize
                |> ResultAsync.wrap (fun data ->
                    let context = { Data = data }
                    let prompt = context.ToPrompt()
                    client |> Client.Request.Chat.completions prompt ct |> ResultAsync.map ignore))

let translate (request: Culture.Request) ct =
    fun client ->
        request.ToPrompt()
        |> ResultAsync.wrap (fun prompt ->
            client
            |> Client.Request.Chat.completions prompt ct
            |> ResultAsync.bind (fun x -> x.ToCulture request.Shield))
