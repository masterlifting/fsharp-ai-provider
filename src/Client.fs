[<RequireQualifiedAccess>]
module AIProvider.Client

type Provider = OpenAI of AIProvider.OpenAI.Domain.Client

type Connection = OpenAI of AIProvider.OpenAI.Domain.Connection

let init connection =
    match connection with
    | Connection.OpenAI value -> value |> OpenAI.Client.init |> Result.map Provider.OpenAI
