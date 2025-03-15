[<AutoOpen>]
module internal AIProvider.Services.Culture.OpenAI.Response

open Infrastructure.SerDe
open Infrastructure.Domain
open AIProvider.Domain
open AIProvider.Services.Domain

type internal OpenAI.Response with

    member this.ToCulture() =
        match this.Messages.Length = 1 with
        | true -> Json.deserialize<Culture.Response> this.Messages[0].Content
        | false ->
            Error
            <| Operation
                { Message = $"'{this}' was not recognized as a valid response."
                  Code = (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) |> Line |> Some }
