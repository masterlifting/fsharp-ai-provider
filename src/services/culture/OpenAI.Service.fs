module AIProvider.Services.Culture.OpenAI.Service

open AIProvider
open AIProvider.Services
open Infrastructure.Prelude

let translate (request: Culture.Domain.Request) ct =
    fun client ->
        request
        |> Culture.OpenAI.Domain.toRequest
        |> ResultAsync.wrap (fun prompt ->
            client
            |> OpenAI.Request.Chat.completions prompt ct
            |> ResultAsync.bind Culture.OpenAI.Domain.toResponse)
