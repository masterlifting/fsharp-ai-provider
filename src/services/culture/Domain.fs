[<AutoOpen>]
module AIProvider.Services.Culture.Domain

open AIProvider
open Infrastructure.Domain
open Infrastructure.SerDe
open AIProvider.OpenAI.Domain

type RequestItem = { Id: string; Value: string }

type Request =
    { Culture: Culture
      Items: RequestItem seq }

    member internal this.toOpenAIRequest() =
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

            { Model = Gpt3_5Turbo
              Store = false
              Messages = [ assistant; user ] })

type Response =
    { Items: RequestItem list }

    static member internal fromOpenAIResponse(response: OpenAI.Domain.Response) =
        match response.Messages.Length = 1 with
        | true -> Json.deserialize<Response> response.Messages[0].Content
        | false -> $"fromOpenAIResponse -> {response}" |> NotSupported |> Error
