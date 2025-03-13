[<AutoOpen>]
module internal AIProvider.Services.Culture.OpenAI.Domain

open Infrastructure.Domain
open Infrastructure.SerDe
open AIProvider
open AIProvider.Services

let toRequest (request: Culture.Domain.Request) =
    request.Items
    |> Json.serialize
    |> Result.map (fun data ->
        
        let assistant: OpenAI.Domain.Message =
            { Role = "assistant"
              Content =
                "You are a helpful translator that can translate the following values of the array into the requested language." }

        let user: OpenAI.Domain.Message =
            { Role = "user"
              Content =
                $"Translate the following values of the array into {request.Culture.Name} language.\n\n"
                + "Return the translation in the following format:\n"
                + "{\n"
                + "  \"Items\": [\n"
                + "    { \"Id\": \"<id>\", \"Value\": \"<value>\" }\n"
                + "  ]\n"
                + "}\n"
                + "\n\n"
                + "Data to translate:\n"
                + data }

        let request: OpenAI.Domain.Request =
            { Model = OpenAI.Domain.Model.Gpt3_5Turbo
              Store = false
              Messages = [ assistant; user ] }

        request)

let toResponse (response: OpenAI.Domain.Response) =
    match response.Messages.Length = 1 with
    | true -> Json.deserialize<Culture.Domain.Response> response.Messages[0].Content
    | false ->
        Error
        <| Operation
            { Message = $"{response} was not recognized as a valid response."
              Code = (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) |> Line |> Some }
