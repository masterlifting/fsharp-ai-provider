module AIProvider.Clients.OpenAI

open System
open Web.Clients
open Web.Clients.Domain.Http.Http
open AIProvider.Clients.Domain

let private clients =OpenAI.ClientFactory()

let init (connection: OpenAI.Connection) =
    match clients.TryGetValue connection.Token with
    | true, client -> Ok client
    | _ ->
        let host = "https://api.openai.com"

        let headers =
            Map
                [ "Authorization", [ $"Bearer {connection.Token}" ]
                  "OpenAI-Project", [ connection.ProjectId ] ]
            |> Some

        { Host = host; Headers = headers }
        |> Http.Client.init
        |> Result.map (fun client ->
            clients.TryAdd(connection.Token, client) |> ignore
            client)
