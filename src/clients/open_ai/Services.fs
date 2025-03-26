module AIProvider.Services.OpenAI

open System
open Infrastructure.Prelude
open Infrastructure.SerDe
open AIProvider.Clients.Domain
open AIProvider.Clients.DataAccess
open Web.Http.Domain

[<RequireQualifiedAccess>]
module Request =
    module Chat =

        let completions (request: OpenAI.Request) ct =
            fun client ->
                let httpRequest =
                    { Path = "/v1/chat/completions"
                      Headers = None }

                let httpContent =
                    OpenAI.RequestEntity(request)
                    |> Json.serialize' OpenAI.jsonOptions
                    |> Result.map (fun data ->
                        String
                            {| Data = data
                               Encoding = Text.Encoding.UTF8
                               MediaType = "application/json" |})

                httpContent
                |> ResultAsync.wrap (fun content ->
                    client
                    |> Web.Http.Request.post httpRequest content ct
                    |> Web.Http.Response.String.readContent ct
                    |> Web.Http.Response.String.fromJson<OpenAI.ResponseEntity>
                    |> ResultAsync.map _.ToDomain())
