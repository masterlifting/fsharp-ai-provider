[<RequireQualifiedAccess>]
module AIProvider.OpenAI.Request

open Infrastructure.Domain

let make (prompt: string) (client: Client) : Async<Result<string, Error'>> =
    prompt |> NotSupported |> Error |> async.Return