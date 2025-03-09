[<RequireQualifiedAccess>]
module AIProvider.Services.Culture.Service

open AIProvider
open Infrastructure.Domain
open Infrastructure.Prelude
open AIProvider.Services.Culture.Domain

let translate (request: Request) ct (provider: Client.Provider) : Async<Result<Response, Error'>> =
    match provider with
    | Client.Provider.OpenAI client ->
        request.toOpenAIRequest ()
        |> ResultAsync.wrap (fun prompt ->
            client
            |> OpenAI.Request.make prompt ct
            |> ResultAsync.bind Response.fromOpenAIResponse)
