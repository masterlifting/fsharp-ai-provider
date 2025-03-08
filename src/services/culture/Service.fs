[<RequireQualifiedAccess>]
module AIProvider.Services.Culture.Service

open AIProvider
open Infrastructure.Domain
open Infrastructure.Prelude
open AIProvider.Services.Culture.Domain

let translate (request: Request) (provider: Client.Provider) : Async<Result<Response, Error'>> =
    match provider with
    | Client.Provider.OpenAI client ->
        client
        |> OpenAI.Request.make (request.toPrompt ())
        |> ResultAsync.bind Response.tryParse
