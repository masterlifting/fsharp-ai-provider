module internal AIProvider.Services.Domain.OpenAI

open Infrastructure.Domain
open Infrastructure.SerDe
open AIProvider.Clients.Domain
open AIProvider.Services.Domain

type internal Culture.Request with
    member this.ToPrompt() =
        this.Items
        |> Json.serialize' OpenAI.JsonOptions
        |> Result.map (fun data ->

            let left, right = this.Shield.Values

            let assistant =
                { OpenAI.Role = "assistant"
                  OpenAI.Content =
                    $"You are an expert translator. Translate the provided array values into the requested language.\n\n\
                    Consider the context carefully to ensure accurate translations.\n\n\
                    The symbols are enclosed in: {left}<symbols>{right} should not be translated.\n\n\
                    Correct any messy symbols and ensure translations follow proper grammar and punctuation." }

            let user =
                { OpenAI.Role = "user"
                  OpenAI.Content =
                    $"Translate the following array values into {this.Culture.Name}.\n\n\
                    Return translations strictly in this JSON format:\n\
                    [\n  {{ \"Value\": \"<original>\", \"Result\": \"<translation>\" }}\n]\n\n\
                    Data:\n{data}" }

            { OpenAI.Model = OpenAI.Model.Gpt3_5Turbo
              OpenAI.Store = false
              OpenAI.Messages = [ assistant; user ] })

type internal OpenAI.Response with

    member this.ToCulture shield =
        match this.Messages.Length = 1 with
        | true ->
            this.Messages[0].Content
            |> Json.deserialize'<Culture.ResponseItem array> OpenAI.JsonOptions
            |> Result.map (fun items ->
                { Shield = shield
                  Items = items |> Array.toList })
        | false ->
            Error
            <| Operation
                { Message = $"The '{this}' was not recognized as a valid response."
                  Code = (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) |> Line |> Some }
