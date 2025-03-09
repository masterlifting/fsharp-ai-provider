[<AutoOpen>]
module AIProvider.Services.Culture.Domain

open Infrastructure.Domain
open Infrastructure.SerDe

type RequestItem = { Id: string; Value: string }

type Request =
    { Culture: Culture
      Items: RequestItem seq }

    member internal this.toPrompt() =
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

            $"{agenda}\n{data}\n{prompt}")


type Response =
    { Items: RequestItem list }

    static member internal fromPrompt(value: string) = Json.deserialize<Response> value
