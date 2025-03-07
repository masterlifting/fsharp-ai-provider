module Multilang.OpenAI.Client

open Multilang.OpenAI.Domain

let private clients = ClientFactory()

let init (connection: Connection) =
    match clients.TryGetValue connection.Token with
    | true, client -> Ok client
    | _ ->
        let client = Client(connection.Token)
        clients.TryAdd(connection.Token, client) |> ignore
        Ok client
