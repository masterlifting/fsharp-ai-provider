module AIProvider.DataAccess.OpenAI

open System
open AIProvider.Domain

type MessageEntity(message: Message) =
    new() =
        MessageEntity(
            { Role = String.Empty
              Content = String.Empty }
        )

    member val Role = message.Role with get, set
    member val Content = message.Content with get, set

    member internal this.ToDomain() =
        { Role = this.Role
          Content = this.Content }

type RequestEntity(request: Request) =
    new() =
        RequestEntity(
            { Model = Gpt3_5Turbo
              Store = false
              Messages = [] }
        )

    member val Model = request.Model.Name with get, set
    member val Store = request.Store with get, set
    member val Messages = request.Messages |> List.map MessageEntity |> List.toArray with get, set


type ChoiceEntity() =
    member val Message: MessageEntity = MessageEntity() with get, set

type ResponseEntity() =
    member val Object: string = String.Empty with get, set
    member val Choices: ChoiceEntity array = [||] with get, set

    member this.ToDomain() =
        { Messages = this.Choices |> Array.map _.Message.ToDomain() |> Array.toList }
