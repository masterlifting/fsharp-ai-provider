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

            let assistant =
                { Role = "assistant"
                  Content =
                    "You are a helpful translator that can translate the following values of the array into the requested language.\n\n
                    Try to analyze the context from the input data as the context may help you to provide a better translation.\n\n
                    If you see the culture-specific parameters in the input data, try to use them to provide a more accurate translation.\n\n
                    If you see some symbols and it is messy, try to clean it up.\n\n
                    Use grammar and punctuation rules to provide a more human-readable translation.\n\n
                    If you encounter the ' symbol, don't remove it, it is a part of the translation placeholders." }

            let user =
                { Role = "user"
                  Content =
                    $"Translate the following values of the array into {this.Culture.Name} language.\n\n"
                    + "Return the translation in the following format:\n"
                    + "{\n"
                    + "  \"Items\": [\n"
                    + "    { \"Id\": \"<id>\", \"Value\": \"<value>\", \"Result\": \"<translation>\" },\n"
                    + "  ]\n"
                    + "}\n"
                    + "\n\n"
                    + "Data to translate:\n"
                    + data }

            { Model = Model.Gpt3_5Turbo
              Store = true
              Messages = [ assistant; user ] })
