[<AutoOpen>]
module internal AIProvider.Services.Culture.OpenAI.Domain.Request

open Infrastructure.SerDe
open AIProvider.OpenAI
open AIProvider.Services.Culture.Domain

type internal Request with
    member this.toPrompt() =
        this.Items
        |> Json.serialize
        |> Result.map (fun data ->

            let assistant =
                { Role = "assistant"
                  Content =
                    "You are a helpful translator that can translate the following values of the array into the requested language." }

            let user =
                { Role = "user"
                  Content =
                    $"Translate the following values of the array into {this.Culture.Name} language.\n\n"
                    + "Return the translation in the following format:\n"
                    + "{\n"
                    + "  \"Items\": [\n"
                    + "    { \"Id\": \"<id>\", \"Value\": \"<value>\" }\n"
                    + "  ]\n"
                    + "}\n"
                    + "\n\n"
                    + "Data to translate:\n"
                    + data }

            { Model = Model.Gpt3_5Turbo
              Store = false
              Messages = [ assistant; user ] })
