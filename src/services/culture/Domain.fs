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
            let agenda =
                $"Translate the following values of the array into {this.Culture.Name} language:"

            let prompt =
                "Return the translation in the following format:\n"
                + "```json\n"
                + "{\n"
                + "  \"Items\": [\n"
                + "    { \"Id\": \"<id>\", \"Value\": \"<value>\" }\n"
                + "  ]\n"
                + "}\n"
                + "```"

            { Model = Gpt4o
              Store = false
              Content =
                { System = agenda
                  User = data
                  Assistant = prompt } })

type Response =
    { Items: RequestItem list }

    static member internal fromOpenAIResponse(response: OpenAI.Domain.Response) =
        Json.deserialize<Response> response.Content
