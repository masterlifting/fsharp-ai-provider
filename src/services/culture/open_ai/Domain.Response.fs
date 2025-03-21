[<AutoOpen>]
module internal AIProvider.Services.Culture.OpenAI.Response

open Infrastructure.SerDe
open Infrastructure.Domain
open AIProvider.Domain
open AIProvider.Services.Domain

type internal OpenAI.Response with

    member this.ToCulture placeholder =
        match this.Messages.Length = 1 with
        | true ->
            this.Messages[0].Content
            |> Json.deserialize<Culture.ResponseItem array>
            |> Result.map (fun items ->
                { Placeholder = placeholder
                  Items = items |> Array.toList })
        | false ->
            Error
            <| Operation
                { Message = $"'{this}' was not recognized as a valid response."
                  Code = (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) |> Line |> Some }
