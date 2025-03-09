module AIProvider.OpenAI.DataAccess

open System
open AIProvider.OpenAI.Domain

type MessageEntity(role: string, content: string) =

    member val Role = role with get, set
    member val Content = content with get, set

    new() = MessageEntity(String.Empty, String.Empty)

type RequestEntity(request: Request) =

    member val Model = request.Model.Name with get, set
    member val Store = request.Store with get, set

    member val Messages =
        [| MessageEntity("system", request.Content.System)
           MessageEntity("user", request.Content.User)
           MessageEntity("assistant", request.Content.Assistant) |] with get, set

    new() =
        RequestEntity(
            { Model = Gpt4o
              Store = false
              Content =
                { System = String.Empty
                  User = String.Empty
                  Assistant = String.Empty } }
        )

type internal Request with
    member internal this.ToEntity() = RequestEntity(this)

type ChoiceEntity() =
    member val Message: MessageEntity = MessageEntity() with get, set

type ResponseEntity() =
    member val Object: string = String.Empty with get, set
    member val Choices: ChoiceEntity array = [||] with get, set

    member this.ToDomain() =
        { Role = this.Choices.[0].Message.Role
          Content = this.Choices.[0].Message.Content }
