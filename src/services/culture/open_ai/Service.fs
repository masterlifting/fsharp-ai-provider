module AIProvider.Services.Culture.OpenAI.Service

open Infrastructure.Prelude
open AIProvider.OpenAI.Request
open AIProvider.Services.Culture.Domain
open AIProvider.Services.Culture.OpenAI.Domain

let translate (request: Request) ct =
    fun client ->
        request.toPrompt ()
        |> ResultAsync.wrap (fun prompt -> client |> Chat.completions prompt ct |> ResultAsync.bind _.toCulture())
