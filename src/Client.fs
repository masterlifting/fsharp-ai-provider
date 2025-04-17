[<RequireQualifiedAccess>]
module AIProvider.Client

open AIProvider.Clients
open AIProvider.Clients.Domain

type Provider = OpenAI of OpenAI.Client
type Connection = OpenAI of OpenAI.Connection

let init connection =
    match connection with
    | Connection.OpenAI value -> value |> OpenAI.Client.init |> Result.map Provider.OpenAI
