[<AutoOpen>]
module internal AIProvider.Services.Culture.OpenAI.Request

open Infrastructure.SerDe
open AIProvider.Domain
open AIProvider.Services.Domain

type internal Culture.Request with
    member this.ToPrompt() =
        this.Items
        |> Json.serialize
        |> Result.map (fun data ->

            let left, right = this.Placeholder.Values

            let assistant =
                { Role = "assistant"
                  Content =
                    $"You are an expert translator. Translate the provided array values into the requested language.\n\n\
                    Consider the context carefully to ensure accurate translations.\n\n\
                    Preserve placeholders like {left}<text>{right} exactly as provided. Do not remove the placeholder symbols around the <text>.\n\n\
                    Correct any messy symbols and ensure translations follow proper grammar and punctuation." }

            let user =
                { Role = "user"
                  Content =
                    $"Translate the following array values into {this.Culture.Name}.\n\n\
                    Return translations strictly in this JSON format:\n\
                    [\n  {{ \"Value\": \"<original>\", \"Result\": \"<translation>\" }}\n]\n\n\
                    Data:\n{data}" }

            { Model = Model.Gpt3_5Turbo
              Store = false
              Messages = [ assistant; user ] })
