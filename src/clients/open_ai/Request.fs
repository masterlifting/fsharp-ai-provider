module AIProvider.OpenAI.Request

open System
open Infrastructure.Prelude
open Infrastructure.SerDe
open AIProvider.OpenAI
open AIProvider.OpenAI.DataAccess
open Web.Http.Domain

[<RequireQualifiedAccess>]
module Chat =

    let completions (request: Domain.Request) ct =
        fun client ->
            let httpRequest =
                { Path = "/v1/chat/completions"
                  Headers = None }

            let httpContent =
                request.ToEntity()
                |> Json.serialize' jsonOptions
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
                |> Web.Http.Response.String.fromJson<ResponseEntity>
                |> ResultAsync.map _.ToDomain())
