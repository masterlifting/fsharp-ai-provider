[<RequireQualifiedAccess>]
module AIProvider.Client

type Provider = OpenAI of AIProvider.Domain.OpenAI.Client

type Connection = OpenAI of AIProvider.Domain.OpenAI.Connection

let init connection =
    match connection with
    | Connection.OpenAI value -> value |> OpenAI.Client.init |> Result.map Provider.OpenAI
