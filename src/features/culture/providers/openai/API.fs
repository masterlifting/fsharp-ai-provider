module AIProvider.Features.Providers.OpenAI.Culture

open Infrastructure.SerDe
open Infrastructure.Prelude
open AIProvider.Clients.OpenAI
open AIProvider.Features.Domain
open AIProvider.Features.DataAccess
open AIProvider.Features.Providers.Domain.OpenAI

let setContext ct =
    fun (client, storage) ->
        storage
        |> Storage.Culture.Query.loadData
        |> ResultAsync.bindAsync (function
            | [||] -> Ok() |> async.Return
            | dataSet ->
                dataSet
                |> Array.collect (fun x -> x.Items |> Array.truncate 30)
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
