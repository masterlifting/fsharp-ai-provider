module AIProvider.OpenAI.Client

open System
open AIProvider.OpenAI
open Web.Http.Domain

let private clients = ClientFactory()

let init (connection: Domain.Connection) =
    match clients.TryGetValue connection.Token with
    | true, client -> Ok client
    | _ ->
        let baseUrl = "https://api.openai.com/v1"
        let headers = Map [ "Authorization", [ $"Bearer {connection.Token}" ] ] |> Some

        { BaseUrl = baseUrl; Headers = headers }
        |> Web.Http.Client.init
        |> Result.map (fun client ->
            clients.TryAdd(connection.Token, client) |> ignore
            client)
