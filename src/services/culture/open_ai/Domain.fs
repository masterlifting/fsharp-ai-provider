module internal AIProvider.Services.Domain.OpenAI

open Infrastructure.Domain
open Infrastructure.SerDe
open AIProvider.Clients.Domain
open AIProvider.Services.Domain

type internal Culture.Context with
    member this.ToPrompt() =

        let system = {
            OpenAI.Role = "system"
            OpenAI.Content =
                "Forget all previous instructions and context. Start with a completely clean slate. "
                + "You are an expert translator with deep knowledge of technical terminology. "
                + "Your task is to use the provided examples as a reference for all future translations. "
                + "These examples represent the preferred translation style, terminology, and formatting."
        }

        let user = {
            OpenAI.Role = "user"
            OpenAI.Content =
                "I'm providing you with translation examples to use as templates for future translations. "
                + "Please study these examples carefully, analyze the patterns and use them as a guide for consistency and terminology. "
                + $"Here are the examples in JSON format:\n\n{this.Data}"
        }

        let assistant = {
            OpenAI.Role = "assistant"
            OpenAI.Content =
                "I've cleared all previous context and studied the new translation examples. "
                + "I will use them as templates for future translations, maintaining consistency "
                + "with these examples in terminology, style, and formatting."
        }

        {
            OpenAI.Model = OpenAI.Model.Gpt3_5Turbo
            OpenAI.Store = false
            OpenAI.Messages = [ system; user; assistant ]
        }

type internal Culture.Request with
    member this.ToPrompt() =
        this.Items
        |> Json.serialize' OpenAI.JsonOptions
        |> Result.map (fun data ->

            let left, right = this.Shield.Values

            let system = {
                OpenAI.Role = "system"
                OpenAI.Content =
                    $"You are an expert translator with deep knowledge of technical terminology. "
                    + $"You will translate content into {this.Culture.Name} using the previously provided examples as guidance."
            }

            let user = {
                OpenAI.Role = "user"
                OpenAI.Content =
                    $"Please translate the following array values into {this.Culture.Name}.\n\n\
                    Critical translation rules:\n\
                    1. Preserve ALL placeholders exactly as they appear: [0], [1], <variable>, etc. NEVER translate these.\n\
                    2. ANY content between {left} and {right} MUST NOT be translated.\n\
                    3. Maintain ALL formatting including line breaks, indentation, and punctuation.\n\
                    4. Use the previously provided examples as reference for terminology and style.\n\
                    5. Technical terms should use their standard terminology in {this.Culture.Name}.\n\
                    6. UI elements should maintain their functional meaning.\n\
                    7. Prioritize accuracy over creativity for uncertain translations.\n\n\
                    Return translations strictly in this JSON format:\n\
                    [\n  {{ \"Value\": \"<original>\", \"Result\": \"<translation>\" }}\n]\n\n\
                    Data to translate:\n{data}"
            }

            {
                OpenAI.Model = OpenAI.Model.Gpt3_5Turbo
                OpenAI.Store = false
                OpenAI.Messages = [ system; user ]
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
