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

            let assistant = {
                OpenAI.Role = "assistant"
                OpenAI.Content =
                    $" Forget all previous instructions.\n\n\
                    You are an expert translator with deep knowledge of technical terminology. Translate the provided array values into {this.Culture.Name}.\n\n\
                    Critical translation rules:\n\
                    1. Preserve ALL placeholders exactly as they appear: [0], [1], <variable>, etc. Never translate these.\n\
                    2. Maintain ALL formatting including line breaks, indentation, and punctuation.\n\
                    3. Any content between {left} and {right} must never be translated.\n\
                    4. Proper nouns should be translated according to their conventional name in the target language if one exists.\n\
                    5. Technical terms should be translated using their standard terminology in the target language.\n\
                    6. UI elements (buttons, menus, dialog titles) should maintain their functional meaning.\n\
                    7. If uncertain about a translation, prioritize accuracy over creativity.\n\n\
                    Examples of correct translations:\n\
                    - 'Item with id [0] processed successfully.' → 'Элемент с идентификатором [0] успешно обработан.' (Russian)\n\
                    - 'Enter your <credentials> below' → 'Введите свои <credentials> ниже' (Russian)\n\
                    - 'Status: [Processing]' → 'Статус: [Processing]' (Russian - placeholder preserved)"
            }

            let user = {
                OpenAI.Role = "user"
                OpenAI.Content =
                    $"Translate the following array values into {this.Culture.Name}.\n\n\
                    Return translations strictly in this JSON format:\n\
                    [\n  {{ \"Value\": \"<original>\", \"Result\": \"<translation>\" }}\n]\n\n\
                    Remember to preserve ALL placeholders (like [0], <variable>, etc.) and maintain all formatting including line breaks.\n\n\
                    Data:\n{data}"
            }

            {
                OpenAI.Model = OpenAI.Model.Gpt3_5Turbo
                OpenAI.Store = false
                OpenAI.Messages = [ assistant; user ]
            })

type internal OpenAI.Response with

    member this.ToCulture shield =
        match this.Messages.Length = 1 with
        | true ->
            this.Messages[0].Content
            |> Json.deserialize'<Culture.ResponseItem array> OpenAI.JsonOptions
            |> Result.map (fun items -> {
                Shield = shield
                Items = items |> Array.toList
            })
        | false ->
            Error
            <| Operation {
                Message = $"The '{this}' was not recognized as a valid response."
                Code = (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__) |> Line |> Some
            }
