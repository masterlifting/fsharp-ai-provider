[<AutoOpen>]
module internal AIProvider.Services.Culture.OpenAI.Domain.Response

open Infrastructure.Domain
open Infrastructure.SerDe
open AIProvider.Services.Culture.Domain

type internal AIProvider.OpenAI.Domain.Response with

    member this.toCulture() =
        match this.Messages.Length = 1 with
        | true -> Json.deserialize<Response> this.Messages[0].Content
        | false ->
            Error
            <| Operation
                { Message = $"'{this}' was not recognized as a valid response."
                  Code = (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) |> Line |> Some }
