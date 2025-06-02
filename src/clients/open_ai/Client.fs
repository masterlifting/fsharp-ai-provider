module AIProvider.Clients.OpenAI.Client

open System
open Infrastructure.Prelude
open Infrastructure.SerDe
open Web.Clients
open Web.Clients.Domain
open AIProvider.Clients.Domain
open AIProvider.Clients.DataAccess

let private clients = OpenAI.ClientFactory()

let init (connection: OpenAI.Connection) =
    match clients.TryGetValue connection.Token with
    | true, client -> Ok client
    | _ ->
        let host = "https://api.openai.com"

        let headers =
            Map [
                "Authorization", [ $"Bearer {connection.Token}" ]
                "OpenAI-Project", [ connection.ProjectId ]
            ]
            |> Some

        {
            Http.BaseUrl = host
            Http.Headers = headers
        }
        |> Http.Client.init
        |> Result.map (fun client ->
            clients.TryAdd(connection.Token, client) |> ignore
            client)

module Request =
    module Chat =

        let completions (request: OpenAI.Request) ct =
            fun client ->
                let httpRequest: Http.Request = {
                    Path = "/v1/chat/completions"
                    Headers = None
                }

                let httpContent =
                    OpenAI.RequestEntity(request)
                    |> Json.serialize' OpenAI.JsonOptions
                    |> Result.map (fun data ->
                        Http.Content.String {|
                            Data = data
                            Encoding = Text.Encoding.UTF8
                            ContentType = "application/json"
                        |})

                httpContent
                |> ResultAsync.wrap (fun content ->
                    client
                    |> Http.Request.post httpRequest content ct
                    |> Http.Response.String.readContent ct
                    |> Http.Response.String.fromJson<OpenAI.ResponseEntity>
                    |> ResultAsync.map _.ToDomain())
