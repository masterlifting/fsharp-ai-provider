module AIProvider.Services.OpenAI

open System
open Infrastructure.Prelude
open Infrastructure.SerDe
open Web.Clients
open Web.Clients.Domain
open AIProvider.Clients.Domain
open AIProvider.Clients.DataAccess

[<RequireQualifiedAccess>]
module Request =
    module Chat =

        let completions (request: OpenAI.Request) ct =
            fun client ->
                let httpRequest: Http.Request =
                    { Path = "/v1/chat/completions"
                      Headers = None }

                let httpContent =
                    OpenAI.RequestEntity(request)
                    |> Json.serialize' OpenAI.jsonOptions
                    |> Result.map (fun data ->
                        Http.RequestContent.String
                            {| Data = data
                               Encoding = Text.Encoding.UTF8
                               MediaType = "application/json" |})

                httpContent
                |> ResultAsync.wrap (fun content ->
                    client
                    |> Http.Request.post httpRequest content ct
                    |> Http.Response.String.readContent ct
                    |> Http.Response.String.fromJson<OpenAI.ResponseEntity>
                    |> ResultAsync.map _.ToDomain())
