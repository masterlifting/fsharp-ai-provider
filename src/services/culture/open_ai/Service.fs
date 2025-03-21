module AIProvider.Services.Culture.OpenAI.Service

open Infrastructure.Prelude
open AIProvider.Services.OpenAI
open AIProvider.Services.Domain
open AIProvider.Services.Culture.OpenAI

let translate (request: Culture.Request) ct =
    fun client ->
        request.ToPrompt()
        |> ResultAsync.wrap (fun prompt ->
            client
            |> Request.Chat.completions prompt ct
            |> ResultAsync.bind (fun x -> x.ToCulture request.Placeholder))
