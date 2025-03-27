module AIProvider.Services.OpenAI.Culture

open Infrastructure.Prelude
open AIProvider.Clients.OpenAI
open AIProvider.Services.Domain
open AIProvider.Services.Domain.OpenAI

let translate (request: Culture.Request) ct =
    fun client ->
        request.ToPrompt()
        |> ResultAsync.wrap (fun prompt ->
            client
            |> Client.Request.Chat.completions prompt ct
            |> ResultAsync.bind (fun x -> x.ToCulture request.Shield))
